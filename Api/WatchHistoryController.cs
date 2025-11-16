using System;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.Shows;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Mvc;
using WatchHistoryRating.Services;

namespace WatchHistoryRating.Api
{
    [ApiController]
    [Route("/WatchHistoryRating")]
    public class WatchHistoryController : ControllerBase
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ISessionManager _sessionManager;

        public WatchHistoryController(ILibraryManager libraryManager, ISessionManager sessionManager)
        {
            _libraryManager = libraryManager;
            _sessionManager = sessionManager;
        }

        // Returns history for the current user if userId omitted.
        // Supports query params: userId, filter (text), type (Movie|Episode|MusicAlbum), sort (name|date|rating), order (asc|desc)
        [HttpGet("/WatchHistoryRating/History")]
        public async Task<IActionResult> GetHistory([FromQuery] string userId = null, [FromQuery] string filter = null, [FromQuery] string type = null, [FromQuery] string sort = null, [FromQuery] string order = null)
        {
            // If userId is not provided, try to use the authenticated user id (if available)
            if (string.IsNullOrEmpty(userId))
                userId = User?.Identity?.Name; // may be null in some contexts

            // Gather play sessions and played items for the user
            var sessions = await _sessionManager.GetSessionsAsync().ConfigureAwait(false);
            var items = sessions.Where(s => s.UserId == userId)
                .SelectMany(s => s.PlayState?.Items ?? Enumerable.Empty<PlaybackInfo>())
                .Select(pi => pi.ItemId?.ToString())
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            // Fallback: if sessions approach fails, attempt to use library's last played metadata (best-effort)
            if (!items.Any())
            {
                var all = _libraryManager.GetAllItems();
                items = all.Where(i => i.UserData?.LastPlayedDate.HasValue == true && i.UserData.LastPlayedDate.Value != default)
                           .Where(i => i.UserData?.LastPlayedDate != null && i.UserData?.UserId == userId)
                           .Select(i => i.Id.ToString())
                           .Distinct()
                           .ToList();
            }

            var results = items.Select(id => _libraryManager.GetItemById(new Guid(id)))
                .Where(i => i != null)
                .Select(i => new {
                    Id = i.Id.ToString(),
                    Name = i.Name,
                    Type = i.GetType().Name,
                    OfficialRating = i.CommunityRating,
                    Image = i.GetProviderImageTag(),
                    LastPlayed = i.UserData?.LastPlayedDate,
                    UserRating = RatingRepository.Instance.Get(i.UserData?.UserId ?? userId, i.Id.ToString())?.Rating
                })
                .ToList();

            // Apply filter
            if (!string.IsNullOrEmpty(filter))
                results = results.Where(r => r.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            // Apply type filter
            if (!string.IsNullOrEmpty(type))
                results = results.Where(r => string.Equals(r.Type, type, StringComparison.OrdinalIgnoreCase)).ToList();

            // Sorting
            bool asc = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(order);
            results = sort switch
            {
                "name" => (asc ? results.OrderBy(r => r.Name) : results.OrderByDescending(r => r.Name)).ToList(),
                "date" => (asc ? results.OrderBy(r => r.LastPlayed) : results.OrderByDescending(r => r.LastPlayed)).ToList(),
                "rating" => (asc ? results.OrderBy(r => r.UserRating) : results.OrderByDescending(r => r.UserRating)).ToList(),
                _ => results
            };

            return Ok(results);
        }

        [HttpPost("/WatchHistoryRating/Rate")]
        public IActionResult Rate([FromQuery] string userId, [FromQuery] string itemId, [FromQuery] int rating)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(itemId))
                return BadRequest("userId and itemId are required.");
            if (rating < 1 || rating > 10)
                return BadRequest("rating must be 1-10.");

            RatingRepository.Instance.SetRating(userId, itemId, rating);
            return Ok();
        }
    }
}
