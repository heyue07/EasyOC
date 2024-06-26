﻿using System;
using OrchardCore.ContentManagement.Handlers;
using System.Threading.Tasks;
using EasyOC.ContentExtensions.Handlers;
using EasyOC.DynamicTypeIndex.Service;
using System.Collections.Generic;
using System.Linq;
using OrchardCore.DisplayManagement.Notify;
using Microsoft.AspNetCore.Mvc.Localization;
using OrchardCore.ContentManagement;
using YesSql;
using Microsoft.Extensions.Logging;

namespace EasyOC.DynamicTypeIndex.Handlers
{
    public class DynamicIndexTableHandler : ContentHandlerBase, IBatchImportEventHandler
    {
        private readonly IDynamicIndexAppService _dynamicIndexAppService;
        private readonly IFreeSql _fsql;
        private readonly INotifier notifier;
        private readonly ILogger _logger;
        private readonly IHtmlLocalizer H;
        private readonly ISession _session;
        public virtual int DefaultPageSize
        {
            get => _dynamicIndexAppService.RebuildIndexPageSize;
        }

        public DynamicIndexTableHandler(IDynamicIndexAppService dynamicIndexAppService, IFreeSql fsql,
            INotifier notifier,
            IHtmlLocalizer<DynamicIndexTableHandler> localizer, ISession session,
            ILogger<DynamicIndexTableHandler> logger)
        {
            _dynamicIndexAppService = dynamicIndexAppService;
            _fsql = fsql;
            this.notifier = notifier;
            this.H = localizer;
            _session = session;
            _logger = logger;
        }

        public Task BeforeImportAsync(IEnumerable<ImportContentContext> contentItems)
        {
            return Task.CompletedTask;
        }

        public async Task AfterImportAsync(IEnumerable<ImportContentContext> contentItems)
        {
            var contentTypes = contentItems.Select(x => x.ContentItem.ContentType).Distinct();
            var totalUpdated = new Dictionary<string, int>();
            foreach (var typeName in contentTypes)
            {
                var config = await _dynamicIndexAppService.GetDynamicIndexConfigAsync(typeName, true);
                if (config != null)
                {
                    var type = await _dynamicIndexAppService.GetDynamicIndexTypeAsync(config.EntityInfo);
                    var table = _fsql.CodeFirst.GetTableByEntity(type);
                    var contentQuery = contentItems.Where(x => x.ContentItem.ContentType == typeName)
                        .Select(x => x.ContentItem);
                    var penddingUpdateList = contentQuery.Take(DefaultPageSize);

                    var pageIndex = 0;
                    totalUpdated[typeName] = 0;
                    while (penddingUpdateList.Any())
                    {
                        var dictList = penddingUpdateList.ToModel(config, type, table);
                        var tsFsql = _fsql.InsertOrUpdate<object>().AsType(type).SetSource(dictList);
                        if (_session.CurrentTransaction != null)
                        {
                            tsFsql.WithTransaction(_session.CurrentTransaction);
                        }
                        totalUpdated[typeName] += await tsFsql.ExecuteAffrowsAsync();
                        pageIndex++;
                        penddingUpdateList = contentQuery.Skip(DefaultPageSize * pageIndex).Take(DefaultPageSize);
                    }

                    await notifier.SuccessAsync(H["{0} 更新成功，更新数量：{1}.", typeName, totalUpdated[typeName]]);
                }
            }
        }

        //public override async Task PublishedAsync(PublishContentContext context)
        //{
        //    await UpdateIndex(context);
        //}
        public override async Task UpdatedAsync(UpdateContentContext context)
        {
            await UpdateIndex(context);
        }

        private async Task UpdateIndex(ContentContextBase context)
        {
            var config = await _dynamicIndexAppService.GetDynamicIndexConfigAsync(context.ContentItem.ContentType);
            if (config != null)
            {
                try
                {
                    var type = await _dynamicIndexAppService.GetDynamicIndexTypeAsync(config.EntityInfo);
                    var table = _fsql.CodeFirst.GetTableByEntity(type);
                    var entity = context.ContentItem.ToModel(config, type, table);
                    var tsFsql = _fsql.InsertOrUpdate<object>().AsType(type).SetSource(entity);
                    if (_session.CurrentTransaction != null)
                    {
                        tsFsql.WithTransaction(_session.CurrentTransaction);
                    }

                    await tsFsql.ExecuteAffrowsAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError("索引更新失败,{0}" + e.Message);
                    throw;
                }
            }
        }
    }
}
