// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using RSS.Data;
using RSS.Properties;
using RSS.UI;

namespace RSS;

internal sealed partial class RssListPage : DynamicListPage, IDisposable
{
    private readonly RssFetcher _fetcher;
    private readonly RssRepository _repository;
    private readonly IEnumerable<RssSource> _sources;
    private readonly IDisposable _sourcesSubscription;
    private string _searchText = string.Empty;

    public RssListPage(RssRepository repository, RssFetcher fetcher)
    {
        _repository = repository;
        _fetcher = fetcher;
        Icon = new IconInfo("\ue736");
        Title = Resources.rss_subscriptions;
        _sources = _repository.AllSources;
        _sourcesSubscription = _repository.ObservableSources.Subscribe(items => RaiseItemsChanged(items.Count));
    }


    public override ICommandItem? EmptyContent => new ListItem(new AddRssPage(_fetcher, _repository))
    {
        Title = Resources.empty_subscription,
        Icon = new IconInfo("\ue736"),
        Subtitle = Resources.empty_subscription_desc
    };

    public void Dispose()
    {
        _sourcesSubscription.Dispose();
    }

    public override IListItem[] GetItems()
    {
        return _sources
            .Where(item =>
                string.IsNullOrEmpty(_searchText) ||
                item.Title.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
            .Select(item => new ListItem(new RssDetailPage(_fetcher, item.ToUi()))
            {
                Title = item.Title,
                Icon = new IconInfo(item.Favicon),
                Subtitle = item.Desc,
                MoreCommands =
                [
                    new CommandContextItem(Resources.remove,
                        name: Resources.remove,
                        action: () => _repository.Delete(item.Url),
                        result: CommandResult.KeepOpen())
                ]
            }).ToArray<IListItem>();
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        if (oldSearch == newSearch) return;
        _searchText = newSearch;
        RaiseItemsChanged();
    }
}