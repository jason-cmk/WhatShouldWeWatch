using Microsoft.WindowsAzure.MobileServices;
using WhatShouldWeWatch.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatShouldWeWatch
{
    public class AzureManager
    {

        private static AzureManager instance;
        private MobileServiceClient client;
        private IMobileServiceTable<UserResult> userResultTable;

        private AzureManager()
        {
            this.client = new MobileServiceClient("http://msaday3xamarin.azurewebsites.net");
            this.userResultTable = this.client.GetTable<UserResult>();
        }

        public MobileServiceClient AzureClient
        {
            get { return client; }
        }

        public static AzureManager AzureManagerInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AzureManager();
                }

                return instance;
            }
        }

        public async Task AddUserResult(UserResult userResult)
        {
            await this.userResultTable.InsertAsync(userResult);
        }

        public async Task<List<UserResult>> GetUserResults()
        {
            return await this.userResultTable.ToListAsync();
        }
        
        public async Task DeleteUserResult(UserResult userResult)
        {
            await this.userResultTable.DeleteAsync(userResult);
        }
    }
}