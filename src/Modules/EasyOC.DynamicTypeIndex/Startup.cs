﻿using EasyOC.ContentExtensions.Handlers;
using EasyOC.DynamicTypeIndex.Handlers;
using EasyOC.DynamicTypeIndex.Indexing;
using EasyOC.DynamicTypeIndex.Migrations;
using EasyOC.DynamicTypeIndex.Service;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentFields.Indexing.SQL;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.Data;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using YesSql.Indexes;

namespace EasyOC.DynamicTypeIndex
{
    [RequireFeatures("EasyOC.ContentExtensions")]
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            // NatashaInitializer.Preheating();
            services.AddAutoMapper(this.GetType().Assembly);
            services.AddScoped<IDataMigration, DynamicIndexDataMigration>();
            services.AddScoped<IDynamicIndexAppService, DynamicIndexAppService>();
            services.AddContentPart<DynamicIndexConfigSetting>();
            services.AddSingleton<IIndexProvider, DynamicIndexConfigDataIndexProvider>();
            services.AddScoped<IContentHandler, DynamicIndexTableHandler>();
            services.AddScoped<IBatchImportEventHandler, DynamicIndexTableHandler>();

            
            services.AddScoped<IDataMigration, ContentPickerFieldIndexMigration>();
            services.AddScoped<IScopedIndexProvider, ContentPickerFieldIndexProvider>();
        }
    }
}
