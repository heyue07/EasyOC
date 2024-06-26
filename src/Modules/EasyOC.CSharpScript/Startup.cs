﻿using EasyOC.CSharpScript.Services;
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;
using System.Threading.Tasks;
// ReSharper disable Mvc.ControllerNotResolved Mvc.AreaNotResolved

namespace EasyOC.CSharpScript
{
    public class Startup : StartupBase
    {
        public override async void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ICSharpScriptProvider, CSharpScriptProvider>();
        }

        public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes,
            IServiceProvider serviceProvider)
        {
            routes.MapAreaControllerRoute(
                name: "Home",
                areaName: "EasyOC.CSharpScript",
                pattern: "Home/Index",
                defaults: new { controller = "Home", action = "Index" }
            );
        }
    }
}
