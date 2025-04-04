// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using RSS.Data;
using RSS.Properties;

namespace RSS;

public partial class RSSCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;
    private readonly RssFetcher _fetcher = new();
    private readonly RssRepository _rssRepository = RssRepository.Instance;

    public RSSCommandsProvider()
    {
        DisplayName = Resources.app_name;
        Icon = new IconInfo("\ue736");
        _commands =
        [
            new ListItem(new RssListPage(_rssRepository, _fetcher))
            {
                Title = Resources.rss_subscriptions,
                Icon = new IconInfo("\ue736")
            },
            new ListItem(new AllRssPage(_rssRepository, _fetcher))
            {
                Title = Resources.all_rss_feeds,
                Icon = new IconInfo("\ue736")
            },
            new ListItem(new AddRssPage(_fetcher, _rssRepository))
            {
                Title = Resources.add_rss_source,
                Icon = new IconInfo("\ue736")
            }
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }
}