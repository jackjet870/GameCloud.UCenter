﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using GameCloud.Database.Adapters;
using GameCloud.Manager.PluginContract.Requests;
using GameCloud.Manager.PluginContract.Responses;
using GameCloud.UCenter.Api.Manager.Models;
using GameCloud.UCenter.Common.Settings;
using GameCloud.UCenter.Database;
using GameCloud.UCenter.Database.Entities;
using GameCloud.UCenter.Manager.Api.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace GameCloud.UCenter.Api.Manager.ApiControllers
{
    /// <summary>
    /// Provide a controller for users.
    /// </summary>
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class AccountEventsController : ManagerApiControllerBase
    {
        private ChartData retentionRate;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorEventsController" /> class.
        /// </summary>
        /// <param name="ucenterDb">Indicating the database context.</param>
        /// <param name="ucenterventDb">Indicating the database context.</param>
        /// <param name="settings">Indicating the settings.</param>
        [ImportingConstructor]
        public AccountEventsController(
            UCenterDatabaseContext ucenterDb,
            UCenterEventDatabaseContext ucenterventDb,
            Settings settings)
            : base(ucenterDb, ucenterventDb, settings)
        {
        }

        /// <summary>
        /// Get user list.
        /// </summary>
        /// <param name="request">Indicating the count.</param>
        /// <returns>Async return account event list.</returns>
        [Route("api/manager/accountEvents")]
        public async Task<PluginPaginationResponse<AccountEventEntity>> AccountEvents([FromBody]SearchRequestInfo request, CancellationToken token)
        {
            string keyword = request.GetParameterValue<string>("keyword");
            int page = request.GetParameterValue<int>("page", 1);
            int count = request.GetParameterValue<int>("pageSize", 10);

            Expression<Func<AccountEventEntity, bool>> filter = null;

            if (!string.IsNullOrEmpty(keyword))
            {
                filter = a => a.AccountName.Contains(keyword);
            }

            var total = await this.UCenterEventDatabase.AccountEvents.CountAsync(filter, token);

            IQueryable<AccountEventEntity> queryable = this.UCenterEventDatabase.AccountEvents.Collection.AsQueryable();
            if (filter != null)
            {
                queryable = queryable.Where(filter);
            }
            queryable = queryable.OrderByDescending(a => a.CreatedTime);

            var result = queryable.Skip((page - 1) * count).Take(count).ToList();

            // todo: add orderby support.
            var model = new PluginPaginationResponse<AccountEventEntity>
            {
                Page = page,
                PageSize = count,
                Raws = result,
                Total = total
            };

            return model;
        }

        [HttpPost, Route("api/manager/newusers")]
        public async Task<NewUserStatisticsData> NewUsers([FromBody]PluginRequestInfo request, CancellationToken token)
        {
            var startTime = request.GetParameterValue<DateTime>("startDate", DateTime.UtcNow.AddYears(-1)).ToUniversalTime();
            var endTime = request.GetParameterValue<DateTime>("endDate", DateTime.UtcNow).ToUniversalTime();

            var result = new NewUserStatisticsData();
            result.HourlyNewUsers = await this.GetHourlyNewUserChartData(startTime, endTime, token);

            return result;
        }

        [HttpPost, Route("api/manager/userstatistics")]
        public async Task<UserStatisticsData> UserStatistics([FromBody] PluginRequestInfo request, CancellationToken token)
        {
            var startDate = request.GetParameterValue<DateTime>("startDate", DateTime.UtcNow.AddYears(-1)).ToUniversalTime();
            var endDate = request.GetParameterValue<DateTime>("endDate", DateTime.UtcNow).ToUniversalTime();
            var startTime = request.GetParameterValue<DateTime>("startTime", DateTime.UtcNow.AddYears(-1)).ToUniversalTime();
            var endTime = request.GetParameterValue<DateTime>("endTime", DateTime.UtcNow).ToUniversalTime();
            string type = request.GetParameterValue<string>("type", "day");

            var startDateTime = startDate.Date + startTime.TimeOfDay;
            var endDateTime = endDate.Date + endTime.TimeOfDay;
            var loginRecords = await this.UCenterEventDatabase.AccountEvents.GetListAsync(
                e => e.EventName == "Login" && e.CreatedTime >= startDateTime && e.CreatedTime <= endDateTime,
                token);

            IEnumerable<IGrouping<DateTime, AccountEventEntity>> groups = null;
            if (type == "day")
            {
                groups = loginRecords.GroupBy(e => e.CreatedTime.Date);
            }
            else if (type == "week")
            {
                groups = loginRecords.GroupBy(e => e.CreatedTime.Date.AddDays(-1 * (int)(e.CreatedTime.Date.DayOfWeek)));
            }
            else if (type == "month")
            {
                groups = loginRecords.GroupBy(e => e.CreatedTime.Date.AddDays(-1 * e.CreatedTime.Date.Day + 1));
            }

            groups = groups.OrderBy(g => g.Key);

            List<string> previousUsers = null;
            float ratio = 1;
            var remainRate = new ChartData();
            var remainDatas = new List<float>();

            foreach (var group in groups)
            {
                if (previousUsers == null || previousUsers.Count == 0)
                {
                    ratio = 1;
                    previousUsers = group.Select(g => g.AccountId).Distinct().ToList();
                }
                else
                {
                    var currentUsers = group.Select(g => g.AccountId).Distinct().ToList();
                    var remainUserCount = currentUsers.Count - currentUsers.Except(previousUsers).Count();
                    ratio = 100 * (float)Math.Round((double)remainUserCount / previousUsers.Count, 2);
                    previousUsers = currentUsers;
                }

                remainDatas.Add(ratio);
                remainRate.Labels.Add(group.Key.ToString("yyyy/MM/dd"));
            }

            remainRate.Data.Add(remainDatas);
            remainRate.Series.Add("留存率");

            var lostRate = new ChartData()
            {
                Data = new List<List<float>>() { remainDatas.Select(d => 100 - d).ToList() },
                Labels = remainRate.Labels,
                Series = new List<string>() { "流失率" }
            };

            var lifeCycleRate = new ChartData()
            {
                Data = lostRate.Data.Select(raw => raw.Select(d => d == 0 ? 0 : 1 / d).ToList()).ToList(),
                Labels = remainRate.Labels,
                Series = new List<string>() { "生命周期" }
            };

            return new UserStatisticsData()
            {
                RemainRate = await this.GetStayLostStatisticsData(startTime, endTime, type, true, token),
                LostRate = lostRate,
                LifeCycle = lifeCycleRate
            };
        }

        [HttpPost, Route("api/manager/userstatistics2")]
        public async Task<ChartData> UserStatistics2([FromBody] PluginRequestInfo request, CancellationToken token)
        {
            var startTime = request.GetParameterValue<DateTime>("startDate", DateTime.UtcNow.AddYears(-1)).ToUniversalTime();
            var endTime = request.GetParameterValue<DateTime>("endDate", DateTime.UtcNow).ToUniversalTime();
            string type = request.GetParameterValue<string>("type", "day");
            bool isStay = request.GetParameterValue<bool>("isStay");

            return await this.GetStayLostStatisticsData(startTime, endTime, type, isStay, token);
        }


        private async Task<ChartData> GetHourlyNewUserChartData(DateTime startTime, DateTime endTime, CancellationToken token)
        {
            var users = await this.UCenterDatabase.Accounts.GetListAsync(
                u => u.CreatedTime >= startTime && u.CreatedTime < endTime,
                token);

            var groups = users
                .GroupBy(u =>
                {
                    var localTime = u.CreatedTime.ToLocalTime();
                    return localTime.Date.AddHours(localTime.Hour);
                })
                .OrderBy(g => g.Key)
                .ToList();

            var data = groups.Select(g => (float)(g.Distinct().Count())).ToList();
            var labels = groups.Select(g => g.Key.ToString("yyyy-MM-dd HH:00")).ToList();
            var series = new List<string>() { "小时新增用户" };

            return new ChartData()
            {
                Data = new List<List<float>>() { data },
                Labels = labels,
                Series = series
            };
        }

        private async Task<ChartData> GetStayLostStatisticsData(DateTime startTime, DateTime endTime, string type, bool isStay, CancellationToken token)
        {
            var loginRecords = await this.UCenterEventDatabase.AccountEvents.GetListAsync(
                e => e.EventName == "Login" && e.CreatedTime >= startTime && e.CreatedTime <= endTime,
                token);

            IEnumerable<IGrouping<DateTime, AccountEventEntity>> groups = null;
            if (type == "day")
            {
                groups = loginRecords.GroupBy(e => e.CreatedTime.Date);
            }
            else if (type == "week")
            {
                groups = loginRecords.GroupBy(e => e.CreatedTime.Date.AddDays(-1 * (int)(e.CreatedTime.Date.DayOfWeek)));
            }
            else if (type == "month")
            {
                groups = loginRecords.GroupBy(e => e.CreatedTime.Date.AddDays(-1 * e.CreatedTime.Date.Day + 1));
            }

            groups = groups.OrderBy(g => g.Key);

            List<string> previousUsers = null;
            float percent = 0;
            float count = 0;
            var chart = new ChartData();
            var percentDatas = new List<float>();
            var countDatas = new List<float>();

            foreach (var group in groups)
            {
                if (previousUsers == null || previousUsers.Count == 0)
                {
                    percent = 0;
                    count = 0;
                    previousUsers = group.Select(g => g.AccountId).Distinct().ToList();
                }
                else
                {
                    var currentUsers = group.Select(g => g.AccountId).Distinct().ToList();
                    count = currentUsers.Count - currentUsers.Except(previousUsers).Count();
                    percent = (float)Math.Round((double)count / previousUsers.Count, 4) * 100;

                    if (!isStay)
                    {
                        count = previousUsers.Count - count;
                        percent = (float)Math.Round((double)(100 - percent), 2);
                    }

                    previousUsers = currentUsers;
                }

                percentDatas.Add(percent);
                countDatas.Add(count);
                chart.Labels.Add(group.Key.ToString("yyyy/MM/dd"));
            }

            chart.Data.Add(percentDatas);
            chart.Data.Add(countDatas);
            return chart;
        }
    }
}