﻿using System.Net.Http;
using System.Threading.Tasks;
using UCenter.Common.Models;
using UCenter.Common.SDK;

namespace UCenter.SDK.AppClient
{
    public class UCenterClient
    {
        private readonly UCenterHttpClient httpClient;
        private readonly string host;

        public UCenterClient(string host)
        {
            this.httpClient = new UCenterHttpClient();
            this.host = host;
        }

        public async Task<AccountRegisterResponse> AccountRegisterAsync(AccountRegisterInfo info)
        {
            string url = GenerateApiEndpoint("account", "register");
            var response = await httpClient.SendAsyncWithException<AccountRegisterInfo, AccountRegisterResponse>(HttpMethod.Post, url, info);
            return response;
        }

        public async Task<AccountLoginResponse> AccountLoginAsync(AccountLoginInfo info)
        {
            string url = GenerateApiEndpoint("account", "login");
            var response = await httpClient.SendAsyncWithException<AccountLoginInfo, AccountLoginResponse>(HttpMethod.Post, url, info);
            return response;
        }

        // TODO: Not need pass parameter
        public async Task<AccountLoginResponse> AccountGuestLoginAsync(AccountLoginInfo info)
        {
            string url = GenerateApiEndpoint("account", "guest");
            var response = await httpClient.SendAsyncWithException<AccountLoginInfo, AccountLoginResponse>(HttpMethod.Post, url, info);
            return response;
        }

        public async Task<AccountChangePasswordResponse> AccountChangePassword(AccountChangePasswordInfo info)
        {
            string url = this.GenerateApiEndpoint("account", "changepassword");
            return await httpClient.SendAsyncWithException<AccountChangePasswordInfo, AccountChangePasswordResponse>(HttpMethod.Post, url, info);
        }

        private string GenerateApiEndpoint(string controller, string route, string queryString = null)
        {
            var url = $"{this.host}/api/{controller}/{route}";
            if (!string.IsNullOrEmpty(queryString))
            {
                url = $"{url}/?{queryString}";
            }

            return url;
        }
    }
}