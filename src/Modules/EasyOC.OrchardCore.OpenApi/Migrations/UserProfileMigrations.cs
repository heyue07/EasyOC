﻿using EasyOC.OrchardCore.OpenApi.Indexs;
using Microsoft.Extensions.FileProviders;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.Data.Migration;
using OrchardCore.Deployment.Services;
using OrchardCore.Recipes.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using YesSql.Sql;

namespace EasyOC.OrchardCore.OpenApi.Migrations
{
    public class UserProfileMigrations : DataMigration
    {
        private readonly IDeploymentManager _deploymentManager;
        private readonly IRecipeMigrator _recipeMigrator;
        public UserProfileMigrations(IDeploymentManager deploymentManager, IRecipeMigrator recipeMigrator)
        {
            _deploymentManager = deploymentManager;
            _recipeMigrator = recipeMigrator;
        }

        public int Create()
        {


            SchemaBuilder.CreateMapIndexTable<UserProfileIndex>(table => table
                     // TODO These should have defaults. on SQL Server they will fall at 255. Exceptions are currently thrown if you go over that.
                     .Column<string>(nameof(UserProfileIndex.UserId))
                     .Column<string>(nameof(UserProfileIndex.UserName))
                     .Column<string>(nameof(UserProfileIndex.FirstName))
                     .Column<string>(nameof(UserProfileIndex.LastName))
                     .Column<string>(nameof(UserProfileIndex.Gender))
                     .Column<string>(nameof(UserProfileIndex.NickName))
                     .Column<string>(nameof(UserProfileIndex.Department))
                     .Column<string>(nameof(UserProfileIndex.Manager))
                 );
            SchemaBuilder.AlterIndexTable<UserProfileIndex>(table => table
                .CreateIndex("IDX_UserProfileIndex_DocumentId",
                    nameof(UserProfileIndex.DocumentId),
                    nameof(UserProfileIndex.UserId),
                    nameof(UserProfileIndex.UserName),
                    nameof(UserProfileIndex.FirstName),
                    nameof(UserProfileIndex.LastName),
                    nameof(UserProfileIndex.Gender),
                    nameof(UserProfileIndex.NickName),
                    nameof(UserProfileIndex.Department),
                    nameof(UserProfileIndex.Manager)
                    ));
            return 1;
        }

        public async Task<int> UpdateFrom2Async()
        {
            var str = await _recipeMigrator.ExecuteAsync("UserProfiles.json", this);
            return 2;
        }

        public async Task<int> UpdateFrom3Async()
        {
            var str = await _recipeMigrator.ExecuteAsync("UserProfiles.json", this);
     
            return 3;

        }
    }
}
