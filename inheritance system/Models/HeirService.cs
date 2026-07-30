using InheritanceSystem.Data;
using InheritanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace inheritance_system.Services
{
    // ── DTOs ──────────────────────────────────────────────────

    public class PropertyOption
    {
        public int PropertyId { get; set; }
        public string Title { get; set; } = "";
        public string Location { get; set; } = "";
    }

    public class HeirRow
    {
        public int HeirId { get; set; }
        public int PropertyId { get; set; }
        public string FullName { get; set; } = "";
        public string Relation { get; set; } = "";
        public decimal SharePercent { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class HeirsByProperty
    {
        public int PropertyId { get; set; }
        public string PropertyTitle { get; set; } = "";
        public string PropertyType { get; set; } = "";
        public string Location { get; set; } = "";
        public decimal TotalShareAssigned { get; set; }
        public List<HeirRow> Heirs { get; set; } = new();
    }

    // ── Service ───────────────────────────────────────────────

    public class HeirService
    {
        private readonly AppDbContext _db;

        public HeirService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<HeirsByProperty>> GetGroupedAsync(int ownerId, string? search = null)
        {
            var properties = await _db.Properties
                .Where(p => p.OwnerId == ownerId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var groups = new List<HeirsByProperty>();

            foreach (var p in properties)
            {
                var heirsQuery = _db.Heirs.Where(h => h.PropertyId == p.PropertyId);

                if (!string.IsNullOrWhiteSpace(search))
                    heirsQuery = heirsQuery.Where(h =>
                        h.FullName.Contains(search) || h.Relation.Contains(search));

                var heirs = await heirsQuery
                    .OrderBy(h => h.FullName)
                    .Select(h => new HeirRow
                    {
                        HeirId = h.HeirId,
                        PropertyId = h.PropertyId,
                        FullName = h.FullName,
                        Relation = h.Relation ?? "",
                        SharePercent = h.SharePercent,
                        CreatedAt = h.CreatedAt
                    })
                    .ToListAsync();

                groups.Add(new HeirsByProperty
                {
                    PropertyId = p.PropertyId,
                    PropertyTitle = p.Title,
                    PropertyType = p.PropertyType ?? "",
                    Location = p.Location ?? "",
                    TotalShareAssigned = heirs.Sum(h => h.SharePercent),
                    Heirs = heirs
                });
            }

            if (!string.IsNullOrWhiteSpace(search))
                groups = groups.Where(g => g.Heirs.Count > 0).ToList();

            return groups;
        }

        public async Task<List<PropertyOption>> GetPropertyOptionsAsync(int ownerId)
        {
            return await _db.Properties
                .Where(p => p.OwnerId == ownerId)
                .OrderBy(p => p.Title)
                .Select(p => new PropertyOption
                {
                    PropertyId = p.PropertyId,
                    Title = p.Title,
                    Location = p.Location ?? ""
                }).ToListAsync();
        }

        public async Task<HeirRow?> GetByIdAsync(int heirId, int ownerId)
        {
            return await (from h in _db.Heirs
                          join p in _db.Properties on h.PropertyId equals p.PropertyId
                          where h.HeirId == heirId && p.OwnerId == ownerId
                          select new HeirRow
                          {
                              HeirId = h.HeirId,
                              PropertyId = h.PropertyId,
                              FullName = h.FullName,
                              Relation = h.Relation ?? "",
                              SharePercent = h.SharePercent,
                              CreatedAt = h.CreatedAt
                          }).FirstOrDefaultAsync();
        }

        // CREATE
        public async Task<(bool Success, string Message, int HeirId)> CreateAsync(
            int ownerId, int propertyId, string fullName, string relation, decimal sharePercent)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return (false, "Full name is required.", 0);
            if (sharePercent < 0 || sharePercent > 100)
                return (false, "Share % must be between 0 and 100.", 0);

            var owns = await _db.Properties
                .AnyAsync(p => p.PropertyId == propertyId && p.OwnerId == ownerId);
            if (!owns) return (false, "Selected property not found.", 0);

            var existingTotal = await _db.Heirs
                .Where(h => h.PropertyId == propertyId)
                .SumAsync(h => (decimal?)h.SharePercent) ?? 0m;

            if (existingTotal + sharePercent > 100m)
                return (false,
                    $"Total share would exceed 100%. Already assigned: {existingTotal:0.##}%, available: {(100m - existingTotal):0.##}%.",
                    0);

            var heir = new Heir
            {
                PropertyId = propertyId,
                FullName = fullName.Trim(),
                Relation = relation?.Trim() ?? "",
                SharePercent = sharePercent,
                CreatedAt = DateTime.Now
            };
            _db.Heirs.Add(heir);
            await _db.SaveChangesAsync();
            return (true, "Heir added successfully!", heir.HeirId);
        }

        // UPDATE
        public async Task<(bool Success, string Message)> UpdateAsync(
            int heirId, int ownerId, int propertyId, string fullName, string relation, decimal sharePercent)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return (false, "Full name is required.");
            if (sharePercent < 0 || sharePercent > 100)
                return (false, "Share % must be between 0 and 100.");

            var heir = await (from h in _db.Heirs
                              join p in _db.Properties on h.PropertyId equals p.PropertyId
                              where h.HeirId == heirId && p.OwnerId == ownerId
                              select h).FirstOrDefaultAsync();

            if (heir == null) return (false, "Heir not found.");

            var owns = await _db.Properties
                .AnyAsync(p => p.PropertyId == propertyId && p.OwnerId == ownerId);
            if (!owns) return (false, "Selected property not found.");

            var existingTotal = await _db.Heirs
                .Where(h => h.PropertyId == propertyId && h.HeirId != heirId)
                .SumAsync(h => (decimal?)h.SharePercent) ?? 0m;

            if (existingTotal + sharePercent > 100m)
                return (false,
                    $"Total share would exceed 100%. Already assigned (excluding this heir): {existingTotal:0.##}%, available: {(100m - existingTotal):0.##}%.");

            heir.PropertyId = propertyId;
            heir.FullName = fullName.Trim();
            heir.Relation = relation?.Trim() ?? "";
            heir.SharePercent = sharePercent;

            await _db.SaveChangesAsync();
            return (true, "Heir updated successfully!");
        }

        // DELETE single
        public async Task<(bool Success, string Message)> DeleteAsync(int heirId, int ownerId)
        {
            var heir = await (from h in _db.Heirs
                              join p in _db.Properties on h.PropertyId equals p.PropertyId
                              where h.HeirId == heirId && p.OwnerId == ownerId
                              select h).FirstOrDefaultAsync();

            if (heir == null) return (false, "Heir not found.");

            _db.Heirs.Remove(heir);
            await _db.SaveChangesAsync();
            return (true, "Heir removed successfully.");
        }

        // DELETE ALL heirs for a user — called when FRC is rejected
        public async Task DeleteAllByOwnerAsync(int ownerId)
        {
            var propertyIds = await _db.Properties
                .Where(p => p.OwnerId == ownerId)
                .Select(p => p.PropertyId)
                .ToListAsync();

            if (!propertyIds.Any()) return;

            var heirs = await _db.Heirs
                .Where(h => propertyIds.Contains(h.PropertyId))
                .ToListAsync();

            if (!heirs.Any()) return;

            _db.Heirs.RemoveRange(heirs);
            await _db.SaveChangesAsync();
        }
    }
}