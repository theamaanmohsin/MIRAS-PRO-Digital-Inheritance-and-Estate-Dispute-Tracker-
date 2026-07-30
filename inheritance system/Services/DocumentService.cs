using InheritanceSystem.Data;
using InheritanceSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;

namespace inheritance_system.Services
{
    public class DocumentListItem
    {
        public int DocumentId { get; set; }
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string FileType { get; set; } = "";
        public string PropertyTitle { get; set; } = "";
        public string Location { get; set; } = "";
        public string Status { get; set; } = "Pending";   // Pending | Approved | Rejected
        public string? AdminNotes { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class DocumentService
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public DocumentService(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // ── LIST ──────────────────────────────────────────────
        public async Task<List<DocumentListItem>> GetDocumentsAsync(
            int ownerId, string? search = null, string? propertyFilter = null)
        {
            var query = from d in _db.Documents
                        join p in _db.Properties on d.PropertyId equals p.PropertyId
                        where p.OwnerId == ownerId
                        select new { d, p };

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(x => x.d.FileName.Contains(search) || x.p.Title.Contains(search));

            if (!string.IsNullOrWhiteSpace(propertyFilter))
                query = query.Where(x => x.p.PropertyId.ToString() == propertyFilter);

            var rows = await query.OrderByDescending(x => x.d.UploadedAt).ToListAsync();

            return rows.Select(x => new DocumentListItem
            {
                DocumentId = x.d.DocumentId,
                FileName = x.d.FileName ?? "",
                FilePath = x.d.FilePath ?? "",
                FileType = GetFileType(x.d.FileName),
                PropertyTitle = x.p.Title,
                Location = x.p.Location ?? "",
                Status = x.d.Status ?? "Pending",
                AdminNotes = x.d.AdminNotes,
                UploadedAt = x.d.UploadedAt
            }).ToList();
        }

        // ── UPLOAD ────────────────────────────────────────────
        public async Task<(bool Success, string Message)> UploadAsync(
            int ownerId, int propertyId, IBrowserFile file)
        {
            try
            {
                var prop = await _db.Properties
                    .FirstOrDefaultAsync(p => p.PropertyId == propertyId && p.OwnerId == ownerId);
                if (prop == null)
                    return (false, "Property not found.");

                if (file.Size > 10 * 1024 * 1024)
                    return (false, "File size must be under 10 MB.");

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "documents");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueName = $"{Guid.NewGuid()}_{file.Name}";
                var filePath = Path.Combine(uploadsFolder, uniqueName);

                await using var stream = new FileStream(filePath, FileMode.Create);
                await file.OpenReadStream(10 * 1024 * 1024).CopyToAsync(stream);

                var document = new Document
                {
                    PropertyId = propertyId,
                    UploadedBy = ownerId,
                    FileName = file.Name,
                    FilePath = $"/uploads/documents/{uniqueName}",
                    Status = "Pending",
                    UploadedAt = DateTime.Now
                };
                _db.Documents.Add(document);
                await _db.SaveChangesAsync();
                return (true, "Document uploaded successfully. Awaiting admin review.");
            }
            catch (Exception ex)
            {
                return (false, ex.InnerException?.Message ?? ex.Message);
            }
        }

        // ── DELETE ────────────────────────────────────────────
        public async Task<(bool Success, string Message)> DeleteAsync(int documentId, int ownerId)
        {
            var doc = await (from d in _db.Documents
                             join p in _db.Properties on d.PropertyId equals p.PropertyId
                             where d.DocumentId == documentId && p.OwnerId == ownerId
                             select d).FirstOrDefaultAsync();

            if (doc == null)
                return (false, "Document not found.");

            try
            {
                var fullPath = Path.Combine(_env.WebRootPath, doc.FilePath.TrimStart('/'));
                if (File.Exists(fullPath)) File.Delete(fullPath);
            }
            catch { }

            _db.Documents.Remove(doc);
            await _db.SaveChangesAsync();
            return (true, "Document deleted successfully.");
        }

        // ── STATS ─────────────────────────────────────────────
        public async Task<(int Total, int Properties)> GetStatsAsync(int ownerId)
        {
            var docs = await (from d in _db.Documents
                              join p in _db.Properties on d.PropertyId equals p.PropertyId
                              where p.OwnerId == ownerId
                              select d.PropertyId).ToListAsync();

            return (docs.Count, docs.Distinct().Count());
        }

        // ── HELPER ────────────────────────────────────────────
        private static string GetFileType(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName) || !fileName.Contains('.'))
                return "FILE";
            return fileName.Substring(fileName.LastIndexOf('.') + 1).ToUpper();
        }
    }
}