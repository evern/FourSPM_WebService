using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using FourSPM_WebService.Models.Shared.Enums;

namespace FourSPM_WebService.Data.EF.FourSPM;

public partial class FourSPMContext : DbContext
{
    public FourSPMContext()
    {
        PROJECTs = Set<PROJECT>();
        USERs = Set<USER>();
        DEPARTMENTs = Set<DEPARTMENT>();
        DELIVERABLEs = Set<DELIVERABLE>();
        PROGRESSes = Set<PROGRESS>();
    }

    public FourSPMContext(DbContextOptions<FourSPMContext> options)
        : base(options)
    {
        PROJECTs = Set<PROJECT>();
        USERs = Set<USER>();
        DEPARTMENTs = Set<DEPARTMENT>();
        DELIVERABLEs = Set<DELIVERABLE>();
        PROGRESSes = Set<PROGRESS>();
    }

    public virtual DbSet<PROJECT> PROJECTs { get; set; }
    public virtual DbSet<USER> USERs { get; set; }
    public virtual DbSet<DEPARTMENT> DEPARTMENTs { get; set; }
    public virtual DbSet<DELIVERABLE> DELIVERABLEs { get; set; }
    public virtual DbSet<PROGRESS> PROGRESSes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=100.88.88.80;Initial Catalog=FourSPM;User ID=GuestUser01;Password=Mynpw4PG;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PROJECT>(entity =>
        {
            entity.HasKey(e => e.GUID);

            entity.ToTable("PROJECT");

            entity.Property(e => e.GUID).ValueGeneratedNever();
            entity.Property(e => e.CLIENT_NUMBER).HasMaxLength(3).IsRequired();
            entity.Property(e => e.PROJECT_NUMBER).HasMaxLength(2).IsRequired();
            entity.Property(e => e.CLIENT_CONTACT);
            entity.Property(e => e.PURCHASE_ORDER_NUMBER);
            entity.Property(e => e.PROJECT_STATUS)
                .HasConversion<int>()
                .HasDefaultValue(ProjectStatus.TenderInProgress);
            entity.Property(e => e.CREATED).HasColumnType("datetime").IsRequired();
            entity.Property(e => e.CREATEDBY).IsRequired();
            entity.Property(e => e.NAME);
            entity.Property(e => e.UPDATED).HasColumnType("datetime");
            entity.Property(e => e.UPDATEDBY);
            entity.Property(e => e.DELETED).HasColumnType("datetime");
            entity.Property(e => e.DELETEDBY);

            // Add test data
            entity.HasData(
                new PROJECT
                {
                    GUID = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    CLIENT_NUMBER = "001",
                    PROJECT_NUMBER = "01",
                    NAME = "Test Project 1",
                    CLIENT_CONTACT = "John Doe",
                    PURCHASE_ORDER_NUMBER = "PO001",
                    PROJECT_STATUS = ProjectStatus.TenderInProgress,
                    CREATED = DateTime.Now,
                    CREATEDBY = Guid.Parse("00000000-0000-0000-0000-000000000001")
                },
                new PROJECT
                {
                    GUID = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    CLIENT_NUMBER = "002",
                    PROJECT_NUMBER = "02",
                    NAME = "Test Project 2",
                    CLIENT_CONTACT = "Jane Smith",
                    PURCHASE_ORDER_NUMBER = "PO002",
                    PROJECT_STATUS = ProjectStatus.Awarded,
                    CREATED = DateTime.Now,
                    CREATEDBY = Guid.Parse("00000000-0000-0000-0000-000000000001")
                }
            );
        });

        modelBuilder.Entity<USER>(entity =>
        {
            entity.HasKey(e => e.GUID).HasName("PK_TEST");

            entity.ToTable("USER");

            entity.Property(e => e.GUID).ValueGeneratedNever();
            entity.Property(e => e.CREATED).HasColumnType("datetime");
            entity.Property(e => e.DELETED).HasColumnType("datetime");
            entity.Property(e => e.FIRST_NAME).HasMaxLength(100);
            entity.Property(e => e.LAST_NAME).HasMaxLength(100);
            entity.Property(e => e.UPDATED).HasColumnType("datetime");
        });

        modelBuilder.Entity<DEPARTMENT>(entity =>
        {
            entity.HasKey(e => e.GUID);
            entity.ToTable("DEPARTMENTS");

            entity.Property(e => e.GUID).HasDefaultValueSql("NEWID()");
            entity.Property(e => e.NAME).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DESCRIPTION).HasMaxLength(500);
            entity.Property(e => e.CREATED).HasColumnType("datetime").IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.CREATEDBY).IsRequired();
            entity.Property(e => e.UPDATED).HasColumnType("datetime");
            entity.Property(e => e.UPDATEDBY);
            entity.Property(e => e.DELETED).HasColumnType("datetime");
            entity.Property(e => e.DELETEDBY);
        });

        modelBuilder.Entity<DELIVERABLE>(entity =>
        {
            entity.HasKey(e => e.GUID);
            entity.ToTable("DELIVERABLES");

            entity.HasIndex(e => e.BOOKING_CODE).HasDatabaseName("IX_DELIVERABLES_BOOKING_CODE");
            entity.HasIndex(e => e.INTERNAL_DOCUMENT_NUMBER).HasDatabaseName("IX_DELIVERABLES_INTERNAL_DOC_NUM");
            entity.HasIndex(e => e.DEPARTMENT_ID).HasDatabaseName("IX_DELIVERABLES_DEPARTMENT_ID");
            entity.HasIndex(e => e.DELIVERABLE_TYPE_ID).HasDatabaseName("IX_DELIVERABLES_DELIVERABLE_TYPE_ID");
            entity.HasIndex(e => e.PROJECT_GUID).HasDatabaseName("IX_DELIVERABLES_PROJECT_ID");
            entity.HasIndex(e => e.DELETED).HasDatabaseName("IX_DELIVERABLES_DELETED");

            entity.Property(e => e.GUID).HasDefaultValueSql("NEWID()");
            entity.Property(e => e.PROJECT_GUID).IsRequired();
            entity.Property(e => e.AREA_NUMBER).HasMaxLength(2);
            entity.Property(e => e.DISCIPLINE).HasMaxLength(2).IsRequired();
            entity.Property(e => e.DOCUMENT_TYPE).HasMaxLength(3).IsRequired();
            entity.Property(e => e.DEPARTMENT_ID).IsRequired();
            entity.Property(e => e.DELIVERABLE_TYPE_ID).IsRequired();
            entity.Property(e => e.INTERNAL_DOCUMENT_NUMBER).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CLIENT_DOCUMENT_NUMBER).HasMaxLength(100);
            entity.Property(e => e.DOCUMENT_TITLE).HasMaxLength(255).IsRequired();
            entity.Property(e => e.BUDGET_HOURS).HasColumnType("decimal(10,2)").IsRequired().HasDefaultValue(0);
            entity.Property(e => e.VARIATION_HOURS).HasColumnType("decimal(10,2)").IsRequired().HasDefaultValue(0);
            entity.Property(e => e.TOTAL_HOURS).HasColumnType("decimal(10,2)").IsRequired().HasDefaultValue(0);
            entity.Property(e => e.TOTAL_COST).HasColumnType("decimal(15,2)").IsRequired().HasDefaultValue(0);
            entity.Property(e => e.BOOKING_CODE).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CREATED).HasColumnType("datetime").IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.CREATEDBY).IsRequired();
            entity.Property(e => e.UPDATED).HasColumnType("datetime");
            entity.Property(e => e.UPDATEDBY);
            entity.Property(e => e.DELETED).HasColumnType("datetime");
            entity.Property(e => e.DELETEDBY);

            // Configure the enum property
            entity.Property(e => e.DELIVERABLE_TYPE_ID)
                .HasColumnType("int")
                .IsRequired();

            // Foreign key relationships
            entity.HasOne(d => d.Department)
                .WithMany()
                .HasForeignKey(d => d.DEPARTMENT_ID)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(d => d.Project)
                .WithMany(p => p.Deliverables)
                .HasForeignKey(d => d.PROJECT_GUID)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<PROGRESS>(entity =>
        {
            entity.HasKey(e => e.GUID);
            entity.ToTable("PROGRESS");

            entity.HasIndex(e => e.GUID_DELIVERABLE).HasDatabaseName("IX_PROGRESS_DELIVERABLE_ID");
            entity.HasIndex(e => e.PERIOD).HasDatabaseName("IX_PROGRESS_PERIOD");
            entity.HasIndex(e => e.DELETED).HasDatabaseName("IX_PROGRESS_DELETED");

            entity.Property(e => e.GUID).HasDefaultValueSql("NEWID()");
            entity.Property(e => e.GUID_DELIVERABLE).IsRequired();
            entity.Property(e => e.PERIOD).IsRequired();
            entity.Property(e => e.UNITS).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(e => e.CREATED).HasColumnType("datetime").IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.CREATEDBY).IsRequired();
            entity.Property(e => e.UPDATED).HasColumnType("datetime");
            entity.Property(e => e.UPDATEDBY);
            entity.Property(e => e.DELETED).HasColumnType("datetime");
            entity.Property(e => e.DELETEDBY);

            // Foreign key relationship
            entity.HasOne(d => d.Deliverable)
                .WithMany(p => p.ProgressItems)
                .HasForeignKey(d => d.GUID_DELIVERABLE)
                .OnDelete(DeleteBehavior.NoAction);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
