using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Windows.Storage;
using LiteDB;

namespace RSS.Data;

public class RssSource
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Favicon { get; set; }
    public string Url { get; set; }
    public string Desc { get; set; }
    public DateTimeOffset LastUpdatedTime { get; set; }
}

public class RssRepository : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<RssSource> _sources;
    private readonly Subject<List<RssSource>> _sourceSubject = new();

    private RssRepository()
    {
        var path = Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, "rss.db");
        _db = new LiteDatabase(path);
        _sources = _db.GetCollection<RssSource>("rss_sources");

        // Create index on URL for faster search and retrieval
        _sources.EnsureIndex(x => x.Url, true);

        // Notify observers of initial data
        NotifyObservers();
    }

    public static RssRepository Instance { get; } = new();

    // Property to get all RSS sources as IEnumerable
    public IEnumerable<RssSource> AllSources => _sources.FindAll();

    // Property to get observable of all RSS sources
    public IObservable<List<RssSource>> ObservableSources =>
        Observable.Defer(() => Observable.Return(AllSources.ToList()))
            .Concat(_sourceSubject.AsObservable());

    // Implement IDisposable interface to ensure resources are properly released
    public void Dispose()
    {
        _sourceSubject?.Dispose();
        _db?.Dispose();
        GC.SuppressFinalize(this);
    }

    // Notify observers that data has changed
    private void NotifyObservers()
    {
        _sourceSubject.OnNext(AllSources.ToList());
    }

    // Add RSS source
    public bool Add(string title, string favicon, string url, string desc, DateTimeOffset lastUpdatedTime)
    {
        // Check if URL already exists
        if (_sources.Exists(x => x.Url == url)) return false;

        // Add to LiteDB
        var source = new RssSource
        {
            Title = title,
            Favicon = favicon,
            Url = url,
            Desc = desc,
            LastUpdatedTime = lastUpdatedTime
        };

        _sources.Insert(source);

        // Notify observers
        NotifyObservers();

        return true;
    }

    // Delete RSS source
    public bool Delete(string url)
    {
        // Delete from LiteDB
        var deleted = _sources.DeleteMany(x => x.Url == url) > 0;

        if (deleted)
            // Notify observers
            NotifyObservers();

        return deleted;
    }

    // Search RSS sources
    public List<RssSource> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<RssSource>();

        // Use LiteDB query capabilities, case insensitive
        var queryLower = query.ToLowerInvariant();

        return _sources.Find(x =>
            x.Title.Contains(queryLower, StringComparison.InvariantCultureIgnoreCase) ||
            x.Url.Contains(queryLower, StringComparison.InvariantCultureIgnoreCase)
        ).ToList();
    }

    // Update an existing RSS source
    public bool Update(RssSource source)
    {
        if (source.Id == 0) return false;

        var updated = _sources.Update(source);

        if (updated) NotifyObservers();

        return updated;
    }

    // Get RSS source by URL
    public RssSource GetByUrl(string url)
    {
        return _sources.FindOne(x => x.Url == url);
    }
}