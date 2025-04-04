using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Web.Syndication;
using RSS.UI;

namespace RSS.Data;

public class RssFetcher
{
    public async Task<UiFeed?> FetchRssFeedAsync(string url, CancellationToken token)
    {
        var response = await new SyndicationClient().RetrieveFeedAsync(new Uri(url));
        if (response == null) return null;

        var image = response.ImageUri switch
        {
            null => new Uri(url).GetLeftPart(UriPartial.Authority) + "/favicon.ico",
            _ => response.ImageUri.ToString()
        };

        return new UiFeed(
            new UiFeedMetaData(response.Title.Text,
                response.Subtitle.Text,
                image,
                response.LastUpdatedTime,
                url
            ),
            response.Items.Select(item => new UiFeedItem(item.Title.Text,
                item.Summary?.Text ?? string.Empty,
                item.Links?.FirstOrDefault()?.Uri.ToString() ?? string.Empty,
                item.PublishedDate.DateTime)).ToArray());
    }
}