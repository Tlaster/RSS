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
using RSS.Properties;
using RSS.UI;

namespace RSS;

public class AllRssPage : DynamicListPage
{
    private readonly CancellationTokenSource _cts = new();
    private readonly RssFetcher _fetcher;
    private readonly Subject<Unit> _loadFeedSubject = new();
    private readonly RssRepository _repository;
    private IReadOnlyList<KeyValuePair<UiFeedMetaData, UiFeedItem>> _results = [];
    private string _searchText = string.Empty;

    public AllRssPage(RssRepository repository, RssFetcher fetcher)
    {
        _repository = repository;
        _fetcher = fetcher;

        Title = Resources.all_rss_feeds;
        Icon = new IconInfo("\ue736");

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

    private async Task<KeyValuePair<UiFeedMetaData, UiFeedItem>[]> LoadFeedItemsAsync(CancellationToken token)
    {
        IsLoading = true;
        var tasks = _repository.AllSources.Select(source =>
        {
            return Task.Run(async () =>
            {
                var result = await _fetcher.FetchRssFeedAsync(source.Url, token);
                if (result != null)
                    return result.Items
                        .Select(item => new KeyValuePair<UiFeedMetaData, UiFeedItem>(result.MetaData, item)).ToArray();

                return [];
            }, token);
        });
        var result = await Task.WhenAll(tasks);
        return result
            .SelectMany(x => x)
            .OrderByDescending(it => it.Value.PublishDate)
            .ToArray();
    }


    public override IListItem[] GetItems()
    {
        return _results
            .Where(item =>
                string.IsNullOrEmpty(_searchText) ||
                item.Value.Title.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
            .Select(item => new ListItem(new OpenUrlCommand(item.Value.Link))
            {
                Title = item.Value.Title,
                Icon = new IconInfo(item.Key.ImageUrl),
                Tags =
                [
                    new Tag(item.Key.Title),
                    new Tag(item.Value.PublishDateHumanized)
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