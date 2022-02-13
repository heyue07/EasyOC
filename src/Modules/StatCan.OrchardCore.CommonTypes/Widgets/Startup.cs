﻿using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;

namespace StatCan.OrchardCore.CommonTypes.Widgets
{
    [Feature(FeatureIds.HtmlWidget)]
    public class HtmlStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection serviceCollection) => serviceCollection.AddScoped<IDataMigration, HtmlMigrations>();
    }
    [Feature(FeatureIds.MarkdownWidget)]
    public class MarkdownStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection serviceCollection) => serviceCollection.AddScoped<IDataMigration, MarkdownMigrations>();
    }
    [Feature(FeatureIds.LiquidWidget)]
    public class LiquidStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection serviceCollection) => serviceCollection.AddScoped<IDataMigration, LiquidMigrations>();
    }
}


