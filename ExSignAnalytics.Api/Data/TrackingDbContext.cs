using ExSignAnalytics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ExSignAnalytics.Api.Data;

public class TrackingDbContext : DbContext
{
	public TrackingDbContext(DbContextOptions<TrackingDbContext> options)
		: base(options)
	{
	}

	public DbSet<Tracking> Trackings => Set<Tracking>();
	public DbSet<Open> Opens => Set<Open>();
	public DbSet<Click> Clicks => Set<Click>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		var tracking = modelBuilder.Entity<Tracking>();
		tracking.ToTable("Trackings");
		tracking.HasKey(t => t.Id);
		tracking.Property(t => t.Id).ValueGeneratedOnAdd();
		tracking.Property(t => t.TrackingKey).HasMaxLength(200).IsRequired();
		tracking.Property(t => t.SenderEmail).HasMaxLength(320).IsRequired();
		tracking.Property(t => t.RecipientEmail).HasMaxLength(320);
		tracking.Property(t => t.CreatedAt).HasColumnType("datetime2(3)").IsRequired();
		tracking.HasIndex(t => t.TrackingKey).IsUnique().HasDatabaseName("UQ_Trackings_TrackingKey");

		var open = modelBuilder.Entity<Open>();
		open.ToTable("Opens");
		open.HasKey(o => o.Id);
		open.Property(o => o.Id).ValueGeneratedOnAdd();
		open.Property(o => o.Timestamp).HasColumnType("datetime2(3)").IsRequired();
		open.Property(o => o.UserAgent).HasMaxLength(1000).IsRequired();
		open.Property(o => o.IpAddress).HasMaxLength(64).IsRequired();
		open.Property(o => o.EmailClient).HasMaxLength(200);
		open.Property(o => o.DeviceType).HasMaxLength(100);
		open.Property(o => o.OperatingSystem).HasMaxLength(100);
		open.Property(o => o.Country).HasMaxLength(100);
		open.Property(o => o.City).HasMaxLength(100);
		open.HasOne(o => o.Tracking)
			.WithMany(t => t.Opens)
			.HasForeignKey(o => o.TrackingId)
			.OnDelete(DeleteBehavior.Cascade);
		open.HasIndex(o => o.TrackingId).HasDatabaseName("IX_Opens_TrackingId");
		open.HasIndex(o => o.Timestamp).HasDatabaseName("IX_Opens_Timestamp");

		var click = modelBuilder.Entity<Click>();
		click.ToTable("Clicks");
		click.HasKey(c => c.Id);
		click.Property(c => c.Id).ValueGeneratedOnAdd();
		click.Property(c => c.LinkType).HasMaxLength(100).IsRequired();
		click.Property(c => c.Timestamp).HasColumnType("datetime2(3)").IsRequired();
		click.Property(c => c.UserAgent).HasMaxLength(1000).IsRequired();
		click.Property(c => c.IpAddress).HasMaxLength(64).IsRequired();
		click.Property(c => c.Browser).HasMaxLength(200);
		click.Property(c => c.SourceEmailClient).HasMaxLength(200);
		click.Property(c => c.DeviceType).HasMaxLength(100);
		click.Property(c => c.OperatingSystem).HasMaxLength(100);
		click.Property(c => c.Country).HasMaxLength(100);
		click.Property(c => c.City).HasMaxLength(100);
		click.HasOne(c => c.Tracking)
			.WithMany(t => t.Clicks)
			.HasForeignKey(c => c.TrackingId)
			.OnDelete(DeleteBehavior.Cascade);
		click.HasIndex(c => c.TrackingId).HasDatabaseName("IX_Clicks_TrackingId");
		click.HasIndex(c => c.Timestamp).HasDatabaseName("IX_Clicks_Timestamp");
		click.HasIndex(c => c.LinkType).HasDatabaseName("IX_Clicks_LinkType");
	}
}
