using System;

namespace WatchHistoryRating.Models
{
    public class RatingEntry
    {
        public string UserId { get; set; }
        public string ItemId { get; set; }
        public int Rating { get; set; }
        public DateTime Updated { get; set; }
    }
}
