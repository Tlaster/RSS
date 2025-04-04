using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using RSS.Data;
using RSS.UI;
using Resources = RSS.Properties.Resources;

namespace RSS;

public class RssDetailPage : DynamicListPage, IDisposable
{
    private readonly CancellationTokenSource _cts = new();

    private readonly RssFetcher _fetcher;

    // Subject that triggers loading feed items
    private readonly Subject<Unit> _loadFeedSubject = new();
    private readonly UiFeedMetaData _metaData;
    private IReadOnlyList<UiFeedItem> _results = [];
    private string _searchText = string.Empty;

    public RssDetailPage(RssFetcher fetcher, UiFeedMetaData metaData)
    {
        _fetcher = fetcher;
        _metaData = metaData;
        Title = _metaData.Title;
        Icon = new IconInfo(_metaData.ImageUrl);

        // Setup the Rx subscription for loading feed items
        _loadFeedSubject
            .Select(_ => Observable.FromAsync(LoadFeedItemsAsync))
            .Switch() // Cancel previous request when a new one comes in
            .Subscribe(
                result =>
                {
                    _results = result;
                    RaiseItemsChanged(_results.Count);
                    IsLoading = false;
                },
                ex =>
                {
                    _results = [];
                    RaiseItemsChanged(_results.Count);
                    IsLoading = false;
                },
                () => { IsLoading = false; },
                _cts.Token
            );

        // Initial load
        _loadFeedSubject.OnNext(Unit.Default);
    }

    public override ICommandItem? EmptyContent => new ListItem(new AnonymousCommand(() =>
    {
        // Trigger feed loading when the command is executed
        _loadFeedSubject.OnNext(Unit.Default);
    })
    {
        Icon = new IconInfo("\ue72c"),
        Name = Resources.refresh,
        Result = CommandResult.KeepOpen()
    })
    {
        Title = Resources.click_to_refresh,
        Icon = new IconInfo("\ue72c")
    };

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _loadFeedSubject.Dispose();
        GC.SuppressFinalize(this);
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        if (oldSearch == newSearch) return;
        _searchText = newSearch;
        RaiseItemsChanged();
    }

    private async Task<UiFeedItem[]> LoadFeedItemsAsync(CancellationToken token)
    {
        IsLoading = true;
        var feed = await _fetcher.FetchRssFeedAsync(_metaData.FeedUrl, token);
        if (feed != null) return feed.Items;

        return [];
    }

    public override IListItem[] GetItems()
    {
        return _results
            .Where(item =>
                string.IsNullOrEmpty(_searchText) ||
                item.Title.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
            .Select(item => new ListItem(new OpenUrlCommand(item.Link))
            {
                Title = item.Title,
                Icon = new IconInfo(_metaData.ImageUrl),
                Tags = [new Tag(item.PublishDateHumanized)]
            }).ToArray<IListItem>();
    }
}