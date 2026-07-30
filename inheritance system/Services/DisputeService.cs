using InheritanceSystem.Data;
using InheritanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace inheritance_system.Services
{
    public class DisputeListItem
    {
        public int DisputeId { get; set; }
        public int PropertyId { get; set; }
        public int FiledBy { get; set; }
        public string FiledByName { get; set; } = "";
        public string PropertyTitle { get; set; } = "";
        public string Location { get; set; } = "";
        public string DisputeType { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "";
        public string? AdminRejectionReason { get; set; }
        public bool AllowUserEdit { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DisputeService
    {
        private readonly AppDbContext _db;

        public DisputeService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<DisputeListItem>> GetDisputesAsync(
            int ownerId, string? search = null, string? statusFilter = null)
        {
            var query = from d in _db.Disputes
                        join p in _db.Properties on d.PropertyId equals p.PropertyId
                        where p.OwnerId == ownerId
                        select new { d, p };

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(x =>
                    x.p.Title.Contains(search) ||
                    x.d.Description.Contains(search) ||
                    x.d.DisputeType.Contains(search));

            if (!string.IsNullOrWhiteSpace(statusFilter))
                query = query.Where(x => x.d.Status == statusFilter);

            var rows = await query.OrderByDescending(x => x.d.CreatedAt).ToListAsync();

            return rows.Select(x => MapItem(x.d, x.p.Title, x.p.Location ?? "")).ToList();
        }

        public async Task<List<DisputeListItem>> GetAllDisputesForAdminAsync(
            string? search = null, string? statusFilter = null)
        {
            var query = from d in _db.Disputes
                        join p in _db.Properties on d.PropertyId equals p.PropertyId
                        join u in _db.Users on d.FiledBy equals u.UserId
                        select new { d, p, u };

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(x =>
                    x.p.Title.Contains(search) ||
                    x.d.Description.Contains(search) ||
                    x.u.FullName.Contains(search));

            if (!string.IsNullOrWhiteSpace(statusFilter))
                query = query.Where(x => x.d.Status == statusFilter);

            var rows = await query.OrderByDescending(x => x.d.CreatedAt).ToListAsync();

            return rows.Select(x => MapItem(x.d, x.p.Title, x.p.Location ?? "", x.u.FullName, x.d.FiledBy)).ToList();
        }

        public async Task<Dispute?> GetByIdAsync(int disputeId, int ownerId) =>
            await (from d in _db.Disputes
                   join p in _db.Properties on d.PropertyId equals p.PropertyId
                   where d.DisputeId == disputeId && p.OwnerId == ownerId
                   select d).FirstOrDefaultAsync();

        public async Task<Dispute?> GetByIdForAdminAsync(int disputeId) =>
            await _db.Disputes
                .Include(d => d.Property)
                .FirstOrDefaultAsync(d => d.DisputeId == disputeId);

        public async Task<(bool Success, string Message)> CreateAsync(
            int ownerId, int propertyId, string disputeType, string description)
        {
            try
            {
                var prop = await _db.Properties
                    .FirstOrDefaultAsync(p => p.PropertyId == propertyId && p.OwnerId == ownerId);
                if (prop == null)
                    return (false, "Property not found.");

                if (string.IsNullOrWhiteSpace(description))
                    return (false, "Description is required.");

                _db.Disputes.Add(new Dispute
                {
                    PropertyId = propertyId,
                    DisputeType = disputeType,
                    Description = description.Trim(),
                    Status = "Pending",
                    CreatedAt = DateTime.Now,
                    FiledBy = ownerId,
                    AllowUserEdit = false
                });
                await _db.SaveChangesAsync();
                return (true, "Application submitted. An administrator will review your case.");
            }
            catch (Exception ex)
            {
                return (false, ex.InnerException?.Message ?? ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> UpdateByUserAsync(
            int disputeId, int ownerId, int propertyId, string disputeType, string description)
        {
            var dispute = await GetByIdAsync(disputeId, ownerId);
            if (dispute == null)
                return (false, "Dispute not found.");

            if (!dispute.AllowUserEdit)
                return (false, "You cannot edit this application unless the administrator allows resubmission.");

            if (dispute.Status is not ("Rejected" or "Pending"))
                return (false, "This application cannot be edited in its current status.");

            var prop = await _db.Properties
                .FirstOrDefaultAsync(p => p.PropertyId == propertyId && p.OwnerId == ownerId);
            if (prop == null)
                return (false, "Property not found.");

            dispute.PropertyId = propertyId;
            dispute.DisputeType = disputeType;
            dispute.Description = description.Trim();
            dispute.Status = "Pending";
            dispute.AllowUserEdit = false;
            dispute.AdminRejectionReason = null;

            await _db.SaveChangesAsync();
            return (true, "Application updated and resubmitted for review.");
        }

        public async Task<(bool Success, string Message)> ReviewByAdminAsync(
            int disputeId, int adminId, string status, string? rejectionReason, bool allowUserEdit)
        {
            var dispute = await _db.Disputes.FindAsync(disputeId);
            if (dispute == null)
                return (false, "Dispute not found.");

            if (!AppConstants.DisputeStatuses.Contains(status))
                return (false, "Invalid status.");

            if (status == "Rejected" && string.IsNullOrWhiteSpace(rejectionReason))
                return (false, "Rejection reason is required when rejecting an application.");

            dispute.Status = status;
            dispute.ReviewedByAdminId = adminId;
            dispute.ReviewedAt = DateTime.Now;
            dispute.AdminRejectionReason = status == "Rejected" ? rejectionReason?.Trim() : null;
            dispute.AllowUserEdit = status == "Rejected" && allowUserEdit;

            await _db.SaveChangesAsync();
            return (true, $"Application marked as {status}.");
        }

        public async Task<(bool Success, string Message)> DeleteByAdminAsync(int disputeId)
        {
            var dispute = await _db.Disputes.FindAsync(disputeId);
            if (dispute == null)
                return (false, "Dispute not found.");

            _db.Disputes.Remove(dispute);
            await _db.SaveChangesAsync();
            return (true, "Dispute removed.");
        }

        public async Task<(int Total, int Pending, int UnderReview, int Approved, int Rejected)> GetStatsAsync(int? ownerId = null)
        {
            var query = from d in _db.Disputes
                        join p in _db.Properties on d.PropertyId equals p.PropertyId
                        select d;

            if (ownerId.HasValue)
                query = from d in _db.Disputes
                        join p in _db.Properties on d.PropertyId equals p.PropertyId
                        where p.OwnerId == ownerId.Value
                        select d;

            var statuses = await query.Select(d => d.Status).ToListAsync();

            return (
                statuses.Count,
                statuses.Count(s => s?.Equals("Pending", StringComparison.OrdinalIgnoreCase) == true),
                statuses.Count(s => s?.Equals("Under Review", StringComparison.OrdinalIgnoreCase) == true),
                statuses.Count(s => s?.Equals("Approved", StringComparison.OrdinalIgnoreCase) == true),
                statuses.Count(s => s?.Equals("Rejected", StringComparison.OrdinalIgnoreCase) == true)
            );
        }

        private static DisputeListItem MapItem(
            Dispute d, string title, string location, string? filedByName = null, int filedBy = 0) =>
            new()
            {
                DisputeId = d.DisputeId,
                PropertyId = d.PropertyId,
                FiledBy = filedBy > 0 ? filedBy : d.FiledBy,
                FiledByName = filedByName ?? "",
                PropertyTitle = title,
                Location = location,
                DisputeType = d.DisputeType ?? "",
                Description = d.Description ?? "",
                Status = d.Status ?? "Pending",
                AdminRejectionReason = d.AdminRejectionReason,
                AllowUserEdit = d.AllowUserEdit,
                CreatedAt = d.CreatedAt
            };
    }
}
