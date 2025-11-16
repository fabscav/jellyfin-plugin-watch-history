# WatchHistoryRating - Jellyfin plugin (Enhanced)

This plugin provides a UI page to view users' watch history and rate items on a scale of 1-10.
Features included in this package:
- Per-user tabs (select different users)
- Filtering by title and type
- Sorting by last played, name, or your rating
- JSON-backed rating store (data/ratings.json)
- Front-end that attempts to use Jellyfin's ApiClient when available

How to install (quick):
1. Build with `dotnet build -c Release` (you may need Jellyfin assembly references compatible with your server).
2. Copy the compiled plugin directory (DLL and embedded UI resources) into `<Jellyfin data dir>/plugins/WatchHistoryRating/`.
3. Restart Jellyfin server.
4. Open the plugin page via the Plugins area or by visiting `/plugins/watchhistory` in server UI.

Caveats:
- The controller uses best-effort approaches to obtain history. Depending on your Jellyfin version, library/session APIs may differ and small adjustments may be required.
- For production, consider migrating the ratings storage from JSON to SQLite for concurrency and robustness.
- The UI is written to blend with Jellyfin's native theme but may need slight CSS tweaks depending on your Jellyfin theme/version.
