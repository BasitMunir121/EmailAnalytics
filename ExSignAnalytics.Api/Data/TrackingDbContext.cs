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
	public DbSet<Survey> Surveys => Set<Survey>();
	public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();
	public DbSet<EmailSignature> EmailSignatures => Set<EmailSignature>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		var emailSignature = modelBuilder.Entity<EmailSignature>();
		emailSignature.ToTable("EmailSignatures");
		emailSignature.HasKey(s => s.Id);
		emailSignature.Property(s => s.Id).ValueGeneratedOnAdd();
		emailSignature.Property(s => s.Name).HasMaxLength(200).IsRequired();
		emailSignature.Property(s => s.TrackingKey).HasMaxLength(200).IsRequired();
		emailSignature.Property(s => s.HtmlBody).IsRequired();
		emailSignature.Property(s => s.CreatedAt).HasColumnType("datetime2(3)").IsRequired();
		emailSignature.Property(s => s.UpdatedAt).HasColumnType("datetime2(3)").IsRequired();
		emailSignature.HasIndex(s => s.TrackingKey).IsUnique().HasDatabaseName("UQ_EmailSignatures_TrackingKey");
		emailSignature.HasIndex(s => s.Name).HasDatabaseName("IX_EmailSignatures_Name");
		emailSignature.HasIndex(s => s.CreatedAt).HasDatabaseName("IX_EmailSignatures_CreatedAt");

		var survey = modelBuilder.Entity<Survey>();
		survey.ToTable("Surveys");
		survey.HasKey(s => s.Id);
		survey.Property(s => s.Id).ValueGeneratedOnAdd();
		survey.Property(s => s.Name).HasMaxLength(100).IsRequired();
		survey.Property(s => s.SurveyType).HasMaxLength(50).IsRequired();
		survey.Property(s => s.CreatedAt).HasColumnType("datetime2(3)").IsRequired();
		survey.HasIndex(s => s.SurveyType).IsUnique().HasDatabaseName("UQ_Surveys_SurveyType");

		var tracking = modelBuilder.Entity<Tracking>();
		tracking.ToTable("Trackings");
		tracking.HasKey(t => t.Id);
		tracking.Property(t => t.Id).ValueGeneratedOnAdd();
		tracking.Property(t => t.TrackingKey).HasMaxLength(200).IsRequired();
		tracking.Property(t => t.SenderEmail).HasMaxLength(320).IsRequired();
		tracking.Property(t => t.RecipientEmail).HasMaxLength(320);
		tracking.Property(t => t.CreatedAt).HasColumnType("datetime2(3)").IsRequired();
		tracking.Property(t => t.HasSignature).IsRequired();
		tracking.Property(t => t.HasSurvey).IsRequired();
		tracking.HasIndex(t => t.TrackingKey).IsUnique().HasDatabaseName("UQ_Trackings_TrackingKey");
		tracking.HasOne(t => t.Survey)
			.WithMany(s => s.Trackings)
			.HasForeignKey(t => t.SurveyId)
			.OnDelete(DeleteBehavior.SetNull);

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

		var response = modelBuilder.Entity<SurveyResponse>();
		response.ToTable("SurveyResponses");
		response.HasKey(r => r.Id);
		response.Property(r => r.Id).ValueGeneratedOnAdd();
		response.Property(r => r.SurveyType).HasMaxLength(50).IsRequired();
		response.Property(r => r.ChoiceKey).HasMaxLength(50);
		response.Property(r => r.ResponseToken).IsRequired();
		response.Property(r => r.Comment).HasMaxLength(2000);
		response.Property(r => r.CommentedAt).HasColumnType("datetime2(3)");
		response.Property(r => r.Timestamp).HasColumnType("datetime2(3)").IsRequired();
		response.Property(r => r.UserAgent).HasMaxLength(1000).IsRequired();
		response.Property(r => r.IpAddress).HasMaxLength(64).IsRequired();
		response.Property(r => r.Browser).HasMaxLength(200);
		response.Property(r => r.DeviceType).HasMaxLength(100);
		response.Property(r => r.OperatingSystem).HasMaxLength(100);
		response.Property(r => r.SenderEmail).HasMaxLength(320).IsRequired();
		response.Property(r => r.RecipientEmail).HasMaxLength(320);
		response.HasIndex(r => r.ResponseToken).IsUnique().HasDatabaseName("UQ_SurveyResponses_ResponseToken");
		response.HasIndex(r => r.TrackingId).HasDatabaseName("IX_SurveyResponses_TrackingId");
		response.HasIndex(r => r.Timestamp).HasDatabaseName("IX_SurveyResponses_Timestamp");
		response.HasIndex(r => r.SurveyType).HasDatabaseName("IX_SurveyResponses_SurveyType");
		response.HasOne(r => r.Tracking)
			.WithMany(t => t.SurveyResponses)
			.HasForeignKey(r => r.TrackingId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
