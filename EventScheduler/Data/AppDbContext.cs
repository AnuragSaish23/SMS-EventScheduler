using Microsoft.EntityFrameworkCore;
using EventScheduler.Models;

namespace EventScheduler.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<SignalConfig> SignalConfigs { get; set; }
        public DbSet<RawSignalLog> RawSignalLogs { get; set; }
        public DbSet<TimingEvent> TimingEvents { get; set; }
        public DbSet<Classification> Classifications { get; set; }
    }
}
