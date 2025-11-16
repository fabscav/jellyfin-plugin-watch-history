using System.Collections.Generic;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using WatchHistoryRating.Services;

namespace WatchHistoryRating
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public override string Name => "Watch History + Ratings (Enhanced)";
        public override string Description => "Shows watch history, ratings (1-10) with filtering, sorting and per-user tabs.";

        private readonly IApplicationPaths _appPaths;

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer)
        {
            _appPaths = applicationPaths;
            // Initialize the singleton RatingRepository using Jellyfin's data path.
            var dataPath = Path.Combine(_appPaths.DataPath, "WatchHistoryRating");
            if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath);
            RatingRepository.Initialize(dataPath);
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[] {
                new PluginPageInfo {
                    Name = "watchhistory",
                    EmbeddedResourcePath = GetType().Namespace + ".UI.index.html"
                }
            };
        }
    }
}
