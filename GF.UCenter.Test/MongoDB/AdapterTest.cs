﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GF.UCenter.Common;
using GF.UCenter.Common.Settings;
using GF.UCenter.MongoDB;
using GF.UCenter.MongoDB.Adapters;
using GF.UCenter.MongoDB.Entity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GF.UCenter.Test.MongoDB
{
    [TestClass]
    public class AdapterTest : UCenterTestBase
    {
        [TestMethod]
        public async Task Adapter_EntityCRUD_Test()
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            var adapter = ExportProvider.GetExportedValue<ICollectionAdapter<Account>>();
            await adapter.CreateIfNotExistsAsync(token);

            var account = new Account()
            {
                Id = Guid.NewGuid().ToString(),
                AccountName = GenerateRandomString(),
                Name = GenerateRandomString(),
                CreatedTime = DateTime.UtcNow,
                Email = "abc@ab.com",
                PhoneNum = "12345678",
                Sex = Common.Portable.Sex.Male,
                IsGuest = false,
            };

            await adapter.InsertAsync(account, token);

            var entityFromServer = await adapter.GetSingleAsync(account.Id, token);

            this.CheckEquals(account, entityFromServer);

            account.Name = GenerateRandomString();

            await adapter.UpdateAsync(account, token);

            var list = await adapter.GetListAsync(e => e.Name == account.Name, token);
            Assert.AreEqual(1, list.Count);
            this.CheckEquals(account, list.First());

            await adapter.DeleteAsync(account, token);

            list = await adapter.GetListAsync(e => e.AccountName == account.AccountName, token);

            Assert.AreEqual(0, list.Count);
        }

        private void CheckEquals(Account entity1, Account entity2)
        {
            Assert.AreEqual(entity1.Id, entity2.Id);
            Assert.AreEqual(entity1.AccountName, entity2.AccountName);
            Assert.AreEqual(entity1.Name, entity2.Name);
            Assert.AreEqual(entity1.CreatedTime.ToString("s"), entity2.CreatedTime.ToString("s"));
            Assert.AreEqual(entity1.Email, entity2.Email);
            Assert.AreEqual(entity1.PhoneNum, entity2.PhoneNum);
            Assert.AreEqual(entity1.Sex, entity2.Sex);
            Assert.AreEqual(entity1.IsGuest, entity2.IsGuest);
        }
    }
}