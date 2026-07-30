using Microsoft.Data.SqlClient;

namespace inheritance_system.Services
{
    public class DashboardStats
    {
        public int TotalProperties { get; set; }
        public int TotalHeirs { get; set; }
        public int ActiveCases { get; set; }
        public int OpenDisputes { get; set; }
        public int PendingTransfers { get; set; }
        public int DocumentsUploaded { get; set; }
        public decimal TotalEstimatedValue { get; set; }
    }

    public class RecentProperty
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

    public class DashboardService
    {
        private readonly string _connectionString;

        public DashboardService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<DashboardStats> GetStatsAsync(int ownerId)
        {
            var s = new DashboardStats();
            const string sql = @"
SELECT
  (SELECT COUNT(*) FROM Properties WHERE OwnerId=@O),
  (SELECT COUNT(*) FROM Heirs h INNER JOIN Properties p ON h.PropertyId=p.PropertyId WHERE p.OwnerId=@O),
  (SELECT COUNT(*) FROM InheritanceCases ic INNER JOIN Properties p ON ic.PropertyId=p.PropertyId WHERE p.OwnerId=@O AND ic.Status='Active'),
  (SELECT COUNT(*) FROM Disputes d INNER JOIN Properties p ON d.PropertyId=p.PropertyId WHERE p.OwnerId=@O AND d.Status IN ('Pending','Under Review')),
  (SELECT COUNT(*) FROM Transfers t INNER JOIN Properties p ON t.PropertyId=p.PropertyId WHERE p.OwnerId=@O AND t.Status='Pending'),
  (SELECT COUNT(*) FROM Documents d INNER JOIN Properties p ON d.PropertyId=p.PropertyId WHERE p.OwnerId=@O),
  (SELECT ISNULL(SUM(EstimatedValue),0) FROM Properties WHERE OwnerId=@O);";
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@O", ownerId);
            using var rd = await cmd.ExecuteReaderAsync();
            if (await rd.ReadAsync())
            {
                s.TotalProperties = rd.GetInt32(0);
                s.TotalHeirs = rd.GetInt32(1);
                s.ActiveCases = rd.GetInt32(2);
                s.OpenDisputes = rd.GetInt32(3);
                s.PendingTransfers = rd.GetInt32(4);
                s.DocumentsUploaded = rd.GetInt32(5);
                s.TotalEstimatedValue = rd.GetDecimal(6);
            }
            return s;
        }

        public async Task<List<RecentProperty>> GetRecentPropertiesAsync(int ownerId, int top = 5)
        {
            var list = new List<RecentProperty>();
            const string sql = @"
SELECT TOP (@T) p.PropertyId, p.Title, ISNULL(p.PropertyType,''), ISNULL(p.Location,''),
  ISNULL(p.EstimatedValue,0),
  (SELECT COUNT(*) FROM Heirs h WHERE h.PropertyId=p.PropertyId),
  ISNULL((SELECT TOP 1 ic.Status FROM InheritanceCases ic WHERE ic.PropertyId=p.PropertyId ORDER BY ic.CreatedAt DESC),'Active'),
  p.CreatedAt
FROM Properties p WHERE p.OwnerId=@O ORDER BY p.CreatedAt DESC;";
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@O", ownerId);
            cmd.Parameters.AddWithValue("@T", top);
            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(new RecentProperty
                {
                    PropertyId = rd.GetInt32(0),
                    Title = rd.GetString(1),
                    PropertyType = rd.GetString(2),
                    Location = rd.GetString(3),
                    EstimatedValue = rd.GetDecimal(4),
                    HeirCount = rd.GetInt32(5),
                    Status = rd.GetString(6),
                    CreatedAt = rd.GetDateTime(7)
                });
            }
            return list;
        }
    }
}
