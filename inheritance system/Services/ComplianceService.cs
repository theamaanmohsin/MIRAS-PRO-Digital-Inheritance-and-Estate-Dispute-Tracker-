using InheritanceSystem.Data;
using InheritanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace inheritance_system.Services
{
    public class MissingRequirement
    {
        public string Code { get; set; } = "";
        public string Message { get; set; } = "";
        public string Severity { get; set; } = "warning"; // warning | error
    }

    public class UserComplianceReport
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool HasApprovedFrc { get; set; }
        public bool IsComplete => !Missing.Any();
        public List<MissingRequirement> Missing { get; set; } = new();
    }

    public class AdminDocumentRow
    {
        public int DocumentId { get; set; }
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string FileType { get; set; } = "";
        public string PropertyTitle { get; set; } = "";
        public string Location { get; set; } = "";
        public string OwnerName { get; set; } = "";
        public string OwnerEmail { get; set; } = "";
        public int OwnerId { get; set; }
        public string Status { get; set; } = "Pending";
        public string? AdminNotes { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class ComplianceService
    {
        private readonly AppDbContext _db;

        public ComplianceService(AppDbContext db) => _db = db;

        public async Task<UserComplianceReport> GetUserComplianceAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            var report = new UserComplianceReport
            {
                UserId = userId,
                UserName = user?.FullName ?? "User",
                Email = user?.Email ?? ""
            };

            var latestFrc = await _db.FrcDocuments
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.UploadedAt)
                .FirstOrDefaultAsync();

            report.HasApprovedFrc = latestFrc?.Status == "Approved";
            if (latestFrc == null)
            {
                report.Missing.Add(new MissingRequirement
                {
                    Code = "FRC_MISSING",
                    Message = "You have not uploaded your FRC (Family Registration Certificate). Upload it from the Heirs page.",
                    Severity = "error"
                });
            }
            else if (latestFrc.Status == "Pending")
            {
                report.Missing.Add(new MissingRequirement
                {
                    Code = "FRC_PENDING",
                    Message = "Your FRC is pending admin review. You will be notified once it is approved.",
                    Severity = "warning"
                });
            }
            else if (latestFrc.Status == "Rejected")
            {
                report.Missing.Add(new MissingRequirement
                {
                    Code = "FRC_REJECTED",
                    Message = "Your FRC was rejected. Please upload a corrected FRC from the Heirs page.",
                    Severity = "error"
                });
            }

            var properties = await _db.Properties
                .Where(p => p.OwnerId == userId)
                .Select(p => new { p.PropertyId, p.Title })
                .ToListAsync();

            if (!properties.Any())
            {
                report.Missing.Add(new MissingRequirement
                {
                    Code = "NO_PROPERTY",
                    Message = "Register at least one property and upload supporting documents (title deed, ownership proof, etc.).",
                    Severity = "warning"
                });
            }
            else
            {
                var docPropertyIds = await _db.Documents
                    .Where(d => properties.Select(p => p.PropertyId).Contains(d.PropertyId))
                    .Select(d => d.PropertyId)
                    .Distinct()
                    .ToListAsync();

                foreach (var prop in properties)
                {
                    if (!docPropertyIds.Contains(prop.PropertyId))
                    {
                        report.Missing.Add(new MissingRequirement
                        {
                            Code = $"DOC_{prop.PropertyId}",
                            Message = $"Property \"{prop.Title}\" has no uploaded documents. Add title deed or ownership proof under Documents.",
                            Severity = "warning"
                        });
                    }
                }
            }

            return report;
        }

        public async Task<List<UserComplianceReport>> GetAllUsersComplianceAsync()
        {
            var userIds = await _db.Users
                .Where(u => u.Role == AppConstants.RoleUser || u.Role == "Owner")
                .Select(u => u.UserId)
                .ToListAsync();

            var list = new List<UserComplianceReport>();
            foreach (var id in userIds)
                list.Add(await GetUserComplianceAsync(id));
            return list.OrderBy(r => r.IsComplete).ThenBy(r => r.UserName).ToList();
        }

        public async Task<List<AdminDocumentRow>> GetAllDocumentsForAdminAsync(string? search = null)
        {
            var query = from d in _db.Documents
                        join p in _db.Properties on d.PropertyId equals p.PropertyId
                        join u in _db.Users on p.OwnerId equals u.UserId
                        select new { d, p, u };

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(x =>
                    x.d.FileName.Contains(s) ||
                    x.p.Title.Contains(s) ||
                    x.u.FullName.Contains(s) ||
                    x.u.Email.Contains(s));
            }

            var rows = await query.OrderByDescending(x => x.d.UploadedAt).ToListAsync();

            return rows.Select(x => new AdminDocumentRow
            {
                DocumentId = x.d.DocumentId,
                FileName = x.d.FileName ?? "",
                FilePath = x.d.FilePath ?? "",
                FileType = GetFileType(x.d.FileName),
                PropertyTitle = x.p.Title,
                Location = x.p.Location ?? "",
                OwnerName = x.u.FullName,
                OwnerEmail = x.u.Email,
                OwnerId = x.u.UserId,
                Status = x.d.Status ?? "Pending",
                AdminNotes = x.d.AdminNotes,
                UploadedAt = x.d.UploadedAt
            }).ToList();
        }

        // ── Admin document review ─────────────────────────────────
        public async Task<(bool Success, string Message)> ApproveDocumentAsync(
            int documentId, int adminId, string adminName, string? notes)
        {
            var doc = await _db.Documents.FindAsync(documentId);
            if (doc == null) return (false, "Document not found.");
            doc.Status = "Approved";
            doc.AdminNotes = notes;
            doc.ReviewedByAdminId = adminId;
            doc.ReviewedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            return (true, "Document approved.");
        }

        public async Task<(bool Success, string Message)> RejectDocumentAsync(
            int documentId, int adminId, string adminName, string? notes)
        {
            var doc = await _db.Documents.FindAsync(documentId);
            if (doc == null) return (false, "Document not found.");
            doc.Status = "Rejected";
            doc.AdminNotes = notes;
            doc.ReviewedByAdminId = adminId;
            doc.ReviewedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            return (true, "Document rejected.");
        }

        public async Task<(bool Success, string Message)> DeleteDocumentByAdminAsync(int documentId)
        {
            var doc = await _db.Documents.FindAsync(documentId);
            if (doc == null) return (false, "Document not found.");
            _db.Documents.Remove(doc);
            await _db.SaveChangesAsync();
            return (true, "Document deleted.");
        }

        private static string GetFileType(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName) || !fileName.Contains('.'))
                return "FILE";
            return fileName[(fileName.LastIndexOf('.') + 1)..].ToUpperInvariant();
        }
    }
}