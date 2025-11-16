using MediaBrowser.Model.Plugins;

namespace WatchHistoryRating
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        // future config options (e.g. enable public ratings, use sqlite, etc)
        public bool UseSqlite { get; set; } = false;
    }
}
