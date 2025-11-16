using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using WatchHistoryRating.Models;

namespace WatchHistoryRating.Services
{
    // Simple JSON-backed singleton repository.
    // NOTE: For production consider migrating to SQLite for concurrency and query performance.
    public class RatingRepository
    {
        private readonly string _path;
        private List<RatingEntry> _cache = new List<RatingEntry>();

        private static RatingRepository _instance;

        public static RatingRepository Instance
        {
            get {
                if (_instance == null) throw new InvalidOperationException("RatingRepository not initialized. Call Initialize(dataPath) from Plugin.cs constructor.");
                return _instance;
            }
        }

        public static void Initialize(string dataPath)
        {
            if (_instance == null)
            {
                var dir = Path.Combine(dataPath);
                Directory.CreateDirectory(dir);
                _instance = new RatingRepository(Path.Combine(dir, "ratings.json"));
            }
        }

        private RatingRepository(string filePath)
        {
            _path = filePath;
            Load();
        }

        private void Load()
        {
            try
            {
                if (File.Exists(_path))
                    _cache = JsonSerializer.Deserialize<List<RatingEntry>>(File.ReadAllText(_path)) ?? new List<RatingEntry>();
            }
            catch
            {
                _cache = new List<RatingEntry>();
            }
        }

        private void Save()
        {
            try
            {
                File.WriteAllText(_path, JsonSerializer.Serialize(_cache));
            }
            catch
            {
                // swallow for now - consider logging
            }
        }

        public IEnumerable<RatingEntry> GetAll() => _cache;

        public IEnumerable<RatingEntry> GetRatingsForUser(string userId) => _cache.Where(x => x.UserId == userId);

        public RatingEntry Get(string userId, string itemId) => _cache.FirstOrDefault(x => x.UserId == userId && x.ItemId == itemId);

        public void SetRating(string userId, string itemId, int rating)
        {
            var entry = Get(userId, itemId);
            if (entry == null)
            {
                entry = new RatingEntry { UserId = userId, ItemId = itemId, Rating = rating, Updated = DateTime.UtcNow };
                _cache.Add(entry);
            }
            else
            {
                entry.Rating = rating;
                entry.Updated = DateTime.UtcNow;
            }
            Save();
        }
    }
}
