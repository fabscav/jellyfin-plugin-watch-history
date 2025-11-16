using System;
using System.Linq;
using System.Collections.Generic;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Mvc;
using WatchHistoryRating.Services;
using Jellyfin.Data.Enums;

namespace WatchHistoryRating.Api
{
    [ApiController]
    [Route("/WatchHistoryRating")]
    public class WatchHistoryController : ControllerBase
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserDataManager _userDataManager;
        private readonly IUserManager _userManager;

        public WatchHistoryController(ILibraryManager libraryManager, IUserDataManager userDataManager, IUserManager userManager)
        {
            _libraryManager = libraryManager;
            _userDataManager = userDataManager;
            _userManager = userManager;
        }

        private class HistoryDto
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public DateTime? LastPlayed { get; set; }
            public int? UserRating { get; set; }
        }

        [HttpGet("/WatchHistoryRating/History")]
        public IActionResult GetHistory([FromQuery] string userId = null, [FromQuery] string filter = null, [FromQuery] string type = null, [FromQuery] string sort = null, [FromQuery] string order = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Ok(Array.Empty<HistoryDto>());
            }

            if (!Guid.TryParse(userId, out var userGuid))
            {
                return BadRequest("Invalid userId");
            }

            var user = _userManager.GetUserById(userGuid);
            if (user == null)
            {
                return BadRequest("User not found");
            }

            var query = new InternalItemsQuery(user)
            {
                IsPlayed = true,
                Recursive = true
            };

            if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<BaseItemKind>(type, true, out var kind))
            {
                query.IncludeItemTypes = new[] { kind };
            }

            var items = _libraryManager.GetItemList(query);

            // Map to DTOs with per-user fields
            var list = new List<HistoryDto>();
            foreach (var item in items)
            {
                var ud = _userDataManager.GetUserData(user, item);
                var dto = new HistoryDto
                {
                    Id = item.Id.ToString("N"),
                    Name = item.Name,
                    Type = item.GetType().Name,
                    LastPlayed = ud?.LastPlayedDate,
                    UserRating = RatingRepository.Instance.Get(userId, item.Id.ToString("N"))?.Rating
                };
                list.Add(dto);
            }

            // Additional filtering (client-driven text filter)
            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Trim();
                list = list.Where(x => x.Name != null && x.Name.Contains(f, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Client-side sorting
            var sortKey = (sort ?? "date").ToLowerInvariant();
            var desc = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(order);

            IOrderedEnumerable<HistoryDto> ordered;
            switch (sortKey)
            {
                case "name":
                    ordered = desc ? list.OrderByDescending(x => x.Name) : list.OrderBy(x => x.Name);
                    break;
                case "rating":
                    ordered = desc ? list.OrderByDescending(x => x.UserRating ?? -1) : list.OrderBy(x => x.UserRating ?? int.MaxValue);
                    break;
                case "date":
                default:
                    ordered = desc ? list.OrderByDescending(x => x.LastPlayed ?? DateTime.MinValue) : list.OrderBy(x => x.LastPlayed ?? DateTime.MaxValue);
                    break;
            }

            return Ok(ordered.ToList());
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
