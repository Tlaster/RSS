using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using RSS.Data;
using RSS.Properties;

namespace RSS;

public class AddRssPage : DynamicListPage, IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Subject<string> _searchTextSubject = new();
    private IReadOnlyList<ListItem> _results = [];

    public AddRssPage(RssFetcher fetcher, RssRepository repository)
    {
        Title = Resources.add_rss_source;
        Icon = new IconInfo("\ue736");
        _searchTextSubject
            .AsObservable()
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(300))
            .Select(searchText => Observable.FromAsync(ct => fetcher.FetchRssFeedAsync(searchText, ct)))
            .Switch()
            .Subscribe(
                result =>
                {
                    if (result != null)
                    {
                        var added = repository.AllSources.Any(x => x.Url == result.MetaData.FeedUrl);
                        _results =
                        [
                            new ListItem(new AnonymousCommand(() =>
                            {
                                if (added)
                                    repository.Delete(result.MetaData.FeedUrl);
                                else
                                    repository.Add(result.MetaData.Title, result.MetaData.ImageUrl,
                                        result.MetaData.FeedUrl,
                                        result.MetaData.Description,
                                        result.MetaData.LastUpdatedTime);

                                _results = [];
                                RaiseItemsChanged();
                            })
                            {
                                Icon = added ? new IconInfo("\uecc9") : new IconInfo("\ued0e"),
                                Name = added ? Resources.remove : Resources.add,
                                Result = CommandResult.GoBack()
                            })
                            {
                                Title = result.MetaData.Title,
                                Icon = new IconInfo(result.MetaData.ImageUrl),
                                Subtitle = result.MetaData.Description
                            }
                        ];
                    }
                    else
                    {
                        _results = [];
                    }

                    RaiseItemsChanged(_results.Count);
                    IsLoading = false;
                },
                error =>
                {
                    _results = [];
                    RaiseItemsChanged(_results.Count);
                    IsLoading = false;
                },
                () =>
                {
                    _results = [];
                    RaiseItemsChanged(_results.Count);
                    IsLoading = false;
                },
                _cancellationTokenSource.Token);
    }

    public override ICommandItem? EmptyContent => new ListItem(new NoOpCommand())
    {
        Title = Resources.add_rss_error,
        Icon = new IconInfo("\ue736")
    };

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _searchTextSubject.Dispose();
        GC.SuppressFinalize(this);
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        if (oldSearch == newSearch) return;
        if (!newSearch.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
            !newSearch.StartsWith("https", StringComparison.OrdinalIgnoreCase)) return;
        // If the search text starts with http or https, we assume it's a URL
        // and we don't want to show the empty content.
        IsLoading = true;
        _searchTextSubject.OnNext(newSearch);
    }

    public override IListItem[] GetItems()
    {
        return _results.ToArray<IListItem>();
    }
}