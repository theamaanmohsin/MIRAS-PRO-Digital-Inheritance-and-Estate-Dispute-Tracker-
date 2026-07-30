using InheritanceSystem.Data;
using InheritanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace inheritance_system.Services
{
    // ── DTOs (view models used by Razor pages) ────────────────

    public class PropertyListItem
    {
        public int PropertyId { get; set; }
        public string Title { get; set; } = "";
        public string PropertyType { get; set; } = "";
        public string Location { get; set; } = "";
        public decimal EstimatedValue { get; set; }
        public int HeirCount { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; }
    }

    public class PropertyDetail
    {
        public int PropertyId { get; set; }
        public string Title { get; set; } = "";
        public string PropertyType { get; set; } = "";
        public string Location { get; set; } = "";
        public decimal EstimatedValue { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<HeirItem> Heirs { get; set; } = new();
        public List<CaseItem> Cases { get; set; } = new();
        public List<TransferItem> Transfers { get; set; } = new();
        public List<DocumentItem> Documents { get; set; } = new();
    }

    public class HeirItem
    {
        public int HeirId { get; set; }
        public string FullName { get; set; } = "";
        public string Relationship { get; set; } = "";   // mapped from DB column: Relation
        public string NationalId { get; set; } = "";     // not in DB — kept for UI compatibility
        public decimal SharePercentage { get; set; }     // mapped from DB column: SharePercent
    }

    public class CaseItem
    {
        public int CaseId { get; set; }
        public string CaseNumber { get; set; } = "";     // not in DB — auto-generated as "CASE-{id}"
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public class TransferItem
    {
        public int TransferId { get; set; }
        public string TransferTo { get; set; } = "";     // mapped from DB column: ToUserId (int → string)
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public class DocumentItem
    {
        public int DocumentId { get; set; }
        public string Title { get; set; } = "";          // mapped from DB column: FileName
        public string FileType { get; set; } = "";       // derived from FileName extension
        public DateTime UploadedAt { get; set; }
    }

    // ── Service ───────────────────────────────────────────────

    public class PropertyService
    {
        private readonly AppDbContext _db;

        public PropertyService(AppDbContext db)
        {
            _db = db;
        }

        // ── LIST ──────────────────────────────────────────────
        public async Task<List<PropertyListItem>> GetPropertiesAsync(
            int ownerId, string? search = null, string? typeFilter = null)
        {
            var query = _db.Properties
                .Where(p => p.OwnerId == ownerId);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Title.Contains(search) || p.Location.Contains(search));

            if (!string.IsNullOrWhiteSpace(typeFilter))
                query = query.Where(p => p.PropertyType == typeFilter);

            var props = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();

            var result = new List<PropertyListItem>();
            foreach (var p in props)
            {
                var heirCount = await _db.Heirs.CountAsync(h => h.PropertyId == p.PropertyId);

                // ⚠️ CHECK YOUR AppDbContext.cs — find the DbSet for InheritanceCases.
                // If it's declared as:  public DbSet<InheritanceCase> Cases { get; set; }
                // → use _db.Cases below.
                // If it's declared as:  public DbSet<InheritanceCase> InheritanceCases { get; set; }
                // → use _db.InheritanceCases below.
                var status = await _db.Cases
                    .Where(ic => ic.PropertyId == p.PropertyId)
                    .OrderByDescending(ic => ic.CreatedAt)
                    .Select(ic => ic.Status)
                    .FirstOrDefaultAsync() ?? "Active";

                result.Add(new PropertyListItem
                {
                    PropertyId = p.PropertyId,
                    Title = p.Title,
                    PropertyType = p.PropertyType ?? "",
                    Location = p.Location ?? "",
                    EstimatedValue = p.EstimatedValue,
                    HeirCount = heirCount,
                    Status = status,
                    CreatedAt = p.CreatedAt
                });
            }
            return result;
        }

        // ── DETAIL ────────────────────────────────────────────
        public async Task<PropertyDetail?> GetPropertyDetailAsync(int propertyId, int ownerId)
        {
            var p = await _db.Properties
                .FirstOrDefaultAsync(x => x.PropertyId == propertyId && x.OwnerId == ownerId);

            if (p == null) return null;

            // DB column: Relation (not Relationship), SharePercent (not SharePercentage)
            // NationalId does not exist in the DB — returns empty string for UI compatibility
            var heirs = await _db.Heirs
                .Where(h => h.PropertyId == propertyId)
                .Select(h => new HeirItem
                {
                    HeirId = h.HeirId,
                    FullName = h.FullName,
                    Relationship = h.Relation ?? "",
                    NationalId = "",
                    SharePercentage = h.SharePercent
                }).ToListAsync();

            // DB has no CaseNumber column — auto-generate "CASE-{id}" for display
            // ⚠️ Use the same DbSet name you verified above (_db.Cases or _db.InheritanceCases)
            var cases = await _db.Cases
                .Where(ic => ic.PropertyId == propertyId)
                .OrderByDescending(ic => ic.CreatedAt)
                .Select(ic => new CaseItem
                {
                    CaseId = ic.CaseId,
                    CaseNumber = "CASE-" + ic.CaseId.ToString(),
                    Status = ic.Status ?? "",
                    CreatedAt = ic.CreatedAt
                }).ToListAsync();

            // DB has ToUserId (int FK → Users). We convert to "User #N" here.
            // If you want the actual name, join to Users: see comment below.
            var transfers = await _db.Transfers
                .Where(t => t.PropertyId == propertyId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TransferItem
                {
                    TransferId = t.TransferId,
                    TransferTo = "User #" + t.ToUserId.ToString(),   // basic fallback
                    Status = t.Status ?? "",
                    CreatedAt = t.CreatedAt
                }).ToListAsync();

            // ── Optional: join to Users table to show a real name instead ──
            // var transfers = await (
            //     from t in _db.Transfers
            //     join u in _db.Users on t.ToUserId equals u.UserId into uj
            //     from u in uj.DefaultIfEmpty()
            //     where t.PropertyId == propertyId
            //     orderby t.CreatedAt descending
            //     select new TransferItem
            //     {
            //         TransferId = t.TransferId,
            //         TransferTo = u != null ? u.FullName : "Unknown",
            //         Status = t.Status ?? "",
            //         CreatedAt = t.CreatedAt
            //     }).ToListAsync();

            // DB column: FileName (not Title). FileType derived from extension.
            var documents = await _db.Documents
                .Where(d => d.PropertyId == propertyId)
                .OrderByDescending(d => d.UploadedAt)
                .Select(d => new DocumentItem
                {
                    DocumentId = d.DocumentId,
                    Title = d.FileName ?? "",
                    FileType = d.FileName != null && d.FileName.Contains('.')
                        ? d.FileName.Substring(d.FileName.LastIndexOf('.') + 1).ToUpper()
                        : "",
                    UploadedAt = d.UploadedAt
                }).ToListAsync();

            return new PropertyDetail
            {
                PropertyId = p.PropertyId,
                Title = p.Title,
                PropertyType = p.PropertyType ?? "",
                Location = p.Location ?? "",
                EstimatedValue = p.EstimatedValue,
                CreatedAt = p.CreatedAt,
                Heirs = heirs,
                Cases = cases,
                Transfers = transfers,
                Documents = documents
            };
        }

        // ── GET SINGLE (for edit form) ─────────────────────────
        public async Task<Property?> GetByIdAsync(int propertyId, int ownerId)
        {
            return await _db.Properties
                .FirstOrDefaultAsync(p => p.PropertyId == propertyId && p.OwnerId == ownerId);
        }

        // ── CREATE ────────────────────────────────────────────
        public async Task<(bool Success, string Message, int PropertyId)> CreateAsync(
            int ownerId, string title, string propertyType, string location, decimal estimatedValue)
        {
            if (string.IsNullOrWhiteSpace(title))
                return (false, "Property title is required.", 0);

            var prop = new Property
            {
                OwnerId = ownerId,
                Title = title.Trim(),
                PropertyType = propertyType,
                Location = location.Trim(),
                EstimatedValue = estimatedValue,
                CreatedAt = DateTime.Now
            };
            _db.Properties.Add(prop);
            await _db.SaveChangesAsync();
            return (true, "Property registered successfully!", prop.PropertyId);
        }

        // ── UPDATE ────────────────────────────────────────────
        public async Task<(bool Success, string Message)> UpdateAsync(
            int propertyId, int ownerId, string title, string propertyType,
            string location, decimal estimatedValue)
        {
            var prop = await _db.Properties
                .FirstOrDefaultAsync(p => p.PropertyId == propertyId && p.OwnerId == ownerId);

            if (prop == null)
                return (false, "Property not found.");

            prop.Title = title.Trim();
            prop.PropertyType = propertyType;
            prop.Location = location.Trim();
            prop.EstimatedValue = estimatedValue;

            await _db.SaveChangesAsync();
            return (true, "Property updated successfully!");
        }

        // ── DELETE ────────────────────────────────────────────
        public async Task<(bool Success, string Message)> DeleteAsync(int propertyId, int ownerId)
        {
            var prop = await _db.Properties
                .FirstOrDefaultAsync(p => p.PropertyId == propertyId && p.OwnerId == ownerId);

            if (prop == null)
                return (false, "Property not found.");

            _db.Properties.Remove(prop);
            await _db.SaveChangesAsync();
            return (true, "Property deleted successfully.");
        }

        public async Task<List<(PropertyListItem Item, string OwnerName)>> GetAllForAdminAsync()
        {
            var rows = await (from p in _db.Properties
                              join u in _db.Users on p.OwnerId equals u.UserId
                              orderby p.CreatedAt descending
                              select new { p, u.FullName }).ToListAsync();

            var result = new List<(PropertyListItem, string)>();
            foreach (var row in rows)
            {
                var heirCount = await _db.Heirs.CountAsync(h => h.PropertyId == row.p.PropertyId);
                result.Add((new PropertyListItem
                {
                    PropertyId = row.p.PropertyId,
                    Title = row.p.Title,
                    PropertyType = row.p.PropertyType ?? "",
                    Location = row.p.Location ?? "",
                    EstimatedValue = row.p.EstimatedValue,
                    HeirCount = heirCount,
                    CreatedAt = row.p.CreatedAt
                }, row.FullName));
            }
            return result;
        }

        public async Task<(bool Success, string Message)> DeleteByAdminAsync(int propertyId)
        {
            var prop = await _db.Properties.FindAsync(propertyId);
            if (prop == null) return (false, "Property not found.");
            _db.Properties.Remove(prop);
            await _db.SaveChangesAsync();
            return (true, "Property deleted.");
        }
    }
}
