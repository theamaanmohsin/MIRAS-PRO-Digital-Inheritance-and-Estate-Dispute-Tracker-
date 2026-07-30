using InheritanceSystem.Data;
using InheritanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace inheritance_system.Services
{
    public class FrcService
    {
        private readonly AppDbContext _db;
        private readonly AdminService _admin;
        private readonly IWebHostEnvironment _env;
        private readonly HeirService _heirService;

        public FrcService(AppDbContext db, AdminService admin, IWebHostEnvironment env, HeirService heirService)
        {
            _db = db;
            _admin = admin;
            _env = env;
            _heirService = heirService;
        }

        // ── Queries ───────────────────────────────────────────────
        public async Task<List<FrcDocument>> GetAllAsync(string? status = null)
        {
            var q = _db.FrcDocuments.Include(f => f.User).AsQueryable();
            if (!string.IsNullOrEmpty(status))
                q = q.Where(f => f.Status == status);
            return await q.OrderByDescending(f => f.UploadedAt).ToListAsync();
        }

        public async Task<FrcDocument?> GetByIdAsync(int id) =>
            await _db.FrcDocuments
                .Include(f => f.User)
                .Include(f => f.ReviewedByAdmin)
                .FirstOrDefaultAsync(f => f.Id == id);

        public async Task<FrcDocument?> GetLatestByUserAsync(int userId) =>
            await _db.FrcDocuments
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.UploadedAt)
                .FirstOrDefaultAsync();

        public async Task<int> CountByStatusAsync(string status) =>
            await _db.FrcDocuments.CountAsync(f => f.Status == status);

        // ── User submits FRC ──────────────────────────────────────
        public async Task<(bool Ok, string Message)> SubmitAsync(
            int userId, string frcNumber, Stream fileStream,
            string fileName, long fileSize)
        {
            var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(fileName).ToLowerInvariant();

            if (!allowed.Contains(ext))
                return (false, "Only PDF, JPG, and PNG files are accepted.");

            if (fileSize > 10 * 1024 * 1024)
                return (false, "File must not exceed 10 MB.");

            var dir = Path.Combine(_env.WebRootPath, "uploads", "frc_vault");
            Directory.CreateDirectory(dir);

            var safeName = $"frc_{userId}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
            var fullPath = Path.Combine(dir, safeName);

            await using (var fs = new FileStream(fullPath, FileMode.Create))
                await fileStream.CopyToAsync(fs);

            _db.FrcDocuments.Add(new FrcDocument
            {
                UserId = userId,
                FrcNumber = frcNumber.Trim(),
                DocumentFilePath = $"/uploads/frc_vault/{safeName}",
                OriginalFileName = Path.GetFileName(fileName),
                FileSizeBytes = fileSize,
                Status = "Pending",
                UploadedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();
            return (true, "FRC submitted successfully. Awaiting admin review.");
        }

        // ── Admin approves ────────────────────────────────────────
        public async Task<(bool Ok, string Message)> ApproveAsync(
            int frcId, int adminId, string adminName, string? notes)
        {
            var frc = await _db.FrcDocuments.FindAsync(frcId);
            if (frc == null) return (false, "FRC not found.");

            frc.Status = "Approved";
            frc.AdminNotes = notes;
            frc.ReviewedByAdminId = adminId;
            frc.ReviewedAt = DateTime.Now;
            frc.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            await _admin.LockUserAsync(frc.UserId);
            await _admin.LogAsync(adminId, adminName, "FRC_Approved",
                "FrcDocument", frcId,
                $"Approved FRC #{frc.FrcNumber} — profile locked.");

            return (true, "FRC approved. User profile has been locked.");
        }

        // ── Admin rejects ─────────────────────────────────────────
        public async Task<(bool Ok, string Message)> RejectAsync(
            int frcId, int adminId, string adminName, string notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
                return (false, "Rejection notes are required.");

            var frc = await _db.FrcDocuments.FindAsync(frcId);
            if (frc == null) return (false, "FRC not found.");

            frc.Status = "Rejected";
            frc.AdminNotes = notes;
            frc.ReviewedByAdminId = adminId;
            frc.ReviewedAt = DateTime.Now;
            frc.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            await _admin.UnlockUserAsync(frc.UserId);

            // Remove all heirs since the FRC is no longer valid
            await _heirService.DeleteAllByOwnerAsync(frc.UserId);

            await _admin.LogAsync(adminId, adminName, "FRC_Rejected",
                "FrcDocument", frcId,
                $"Rejected FRC #{frc.FrcNumber}. All heirs removed. Notes: {notes}");

            return (true, "FRC rejected. All heirs have been removed. User can resubmit.");
        }
    }
}