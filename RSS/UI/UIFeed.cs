using System;
using Humanizer;
using RSS.Data;

namespace RSS.UI;

public record UiFeedMetaData(
    string Title,
    string Description,
    string ImageUrl,
    DateTimeOffset LastUpdatedTime,
    string FeedUrl);

public static class RssSourceExtensions
{
    public static UiFeedMetaData ToUi(this RssSource source)
    {
        return new UiFeedMetaData(
            source.Title,
            source.Desc,
            source.Favicon,
            source.LastUpdatedTime,
            source.Url
        );
    }
}

public record UiFeed(UiFeedMetaData MetaData, UiFeedItem[] Items);

public record UiFeedItem(string Title, string Description, string Link, DateTimeOffset PublishDate)
{
    public string PublishDateHumanized => PublishDate.Humanize();
}