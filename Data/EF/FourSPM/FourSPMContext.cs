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
        DELIVERABLEs = Set<DELIVERABLE>();
        PROGRESSes = Set<PROGRESS>();
        CLIENTs = Set<CLIENT>();
        DISCIPLINEs = Set<DISCIPLINE>();
        DOCUMENT_TYPEs = Set<DOCUMENT_TYPE>();
        AREAs = Set<AREA>();
        DELIVERABLE_GATEs = Set<DELIVERABLE_GATE>();
        VARIATIONs = Set<VARIATION>();
        ROLEs = Set<ROLE>();
        ROLE_PERMISSIONs = Set<ROLE_PERMISSION>();
    }

    public FourSPMContext(DbContextOptions<FourSPMContext> options)
        : base(options)
    {
        PROJECTs = Set<PROJECT>();
        USERs = Set<USER>();
        DELIVERABLEs = Set<DELIVERABLE>();
        PROGRESSes = Set<PROGRESS>();
        CLIENTs = Set<CLIENT>();
        DISCIPLINEs = Set<DISCIPLINE>();
        DOCUMENT_TYPEs = Set<DOCUMENT_TYPE>();
        AREAs = Set<AREA>();
        DELIVERABLE_GATEs = Set<DELIVERABLE_GATE>();
        VARIATIONs = Set<VARIATION>();
        ROLEs = Set<ROLE>();
        ROLE_PERMISSIONs = Set<ROLE_PERMISSION>();
    }

    public virtual DbSet<PROJECT> PROJECTs { get; set; }
    public virtual DbSet<USER> USERs { get; set; }
    public virtual DbSet<DELIVERABLE> DELIVERABLEs { get; set; }
    public virtual DbSet<PROGRESS> PROGRESSes { get; set; }
    public virtual DbSet<CLIENT> CLIENTs { get; set; }
    public virtual DbSet<DISCIPLINE> DISCIPLINEs { get; set; }
    public virtual DbSet<DOCUMENT_TYPE> DOCUMENT_TYPEs { get; set; }
    public virtual DbSet<AREA> AREAs { get; set; }
    public virtual DbSet<DELIVERABLE_GATE> DELIVERABLE_GATEs { get; set; }
    public virtual DbSet<VARIATION> VARIATIONs { get; set; }
    public virtual DbSet<ROLE> ROLEs { get; set; }
    public virtual DbSet<ROLE_PERMISSION> ROLE_PERMISSIONs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=100.88.88.80;Initial Catalog=FourSPM;User ID=GuestUser01;Password=Mynpw4PG;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CLIENT>(entity =>
        {
            entity.HasKey(e => e.GUID);

            entity.ToTable("CLIENT");

            entity.Property(e => e.GUID).ValueGeneratedNever();
            entity.Property(e => e.NUMBER).HasMaxLength(3).IsRequired();
            entity.Property(e => e.DESCRIPTION).HasMaxLength(500);
            entity.Property(e => e.CLIENT_CONTACT_NAME).HasMaxLength(500);
            entity.Property(e => e.CLIENT_CONTACT_NUMBER).HasMaxLength(100);
            entity.Property(e => e.CLIENT_CONTACT_EMAIL).HasMaxLength(100);
            entity.Property(e => e.CREATED).HasColumnType("datetime").IsRequired();
            entity.Property(e => e.CREATEDBY).IsRequired();
            entity.Property(e => e.UPDATED).HasColumnType("datetime");
            entity.Property(e => e.UPDATEDBY);
            entity.Property(e => e.DELETED).HasColumnType("datetime");
            entity.Property(e => e.DELETEDBY);
        });

        modelBuilder.Entity<PROJECT>(entity =>
        {
            entity.HasKey(e => e.GUID);
            entity.ToTable("PROJECT");

            entity.HasIndex(e => e.GUID_CLIENT).HasDatabaseName("IX_PROJECT_CLIENT_ID");
            entity.HasIndex(e => e.PROJECT_NUMBER).HasDatabaseName("IX_PROJECT_NUMBER");
            entity.HasIndex(e => e.NAME).HasDatabaseName("IX_PROJECT_NAME");
            entity.HasIndex(e => e.DELETED).HasDatabaseName("IX_PROJECT_DELETED");

            entity.Property(e => e.GUID).ValueGeneratedNever();
            entity.Property(e => e.GUID_CLIENT);
            entity.Property(e => e.PROJECT_NUMBER).HasMaxLength(2).IsRequired();
            entity.Property(e => e.NAME);
            entity.Property(e => e.PURCHASE_ORDER_NUMBER);
            entity.Property(e => e.PROJECT_STATUS)
                .HasConversion<int>()
                .HasDefaultValue(ProjectStatus.TenderInProgress);
            entity.Property(e => e.PROGRESS_START).HasColumnType("datetime");
            entity.Property(e => e.CREATED).HasColumnType("datetime").IsRequired();
            entity.Property(e => e.CREATEDBY).IsRequired();
            entity.Property(e => e.UPDATED).HasColumnType("datetime");
            entity.Property(e => e.UPDATEDBY);
            entity.Property(e => e.DELETED).HasColumnType("datetime");
            entity.Property(e => e.DELETEDBY);

            entity.HasOne(d => d.Client)
                .WithMany(p => p.Projects)
                .HasForeignKey(d => d.GUID_CLIENT);

            // Add the relationship between PROJECT and AREA
            entity.HasMany(p => p.Areas)
                .WithOne(a => a.Project)
                .HasForeignKey(a => a.GUID_PROJECT)
                .OnDelete(DeleteBehavior.NoAction);
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

        modelBuilder.Entity<DELIVERABLE>(entity =>
        {
            entity.HasKey(e => e.GUID);
            entity.ToTable("DELIVERABLE");

            entity.HasIndex(e => e.INTERNAL_DOCUMENT_NUMBER).HasDatabaseName("IX_DELIVERABLES_INTERNAL_DOC_NUM");
            entity.HasIndex(e => e.DEPARTMENT_ID).HasDatabaseName("IX_DELIVERABLES_DEPARTMENT_ID");
            entity.HasIndex(e => e.DELIVERABLE_TYPE_ID).HasDatabaseName("IX_DELIVERABLES_DELIVERABLE_TYPE_ID");
            entity.HasIndex(e => e.GUID_PROJECT).HasDatabaseName("IX_DELIVERABLES_PROJECT_ID");
            entity.HasIndex(e => e.GUID_DELIVERABLE_GATE).HasDatabaseName("IX_DELIVERABLES_GATE_ID");
            entity.HasIndex(e => e.DELETED).HasDatabaseName("IX_DELIVERABLES_DELETED");

            entity.Property(e => e.GUID).HasDefaultValueSql("NEWID()");
            entity.Property(e => e.GUID_PROJECT).IsRequired();
            entity.Property(e => e.DISCIPLINE).HasMaxLength(2).IsRequired();
            entity.Property(e => e.DOCUMENT_TYPE).HasMaxLength(3).IsRequired();
            entity.Property(e => e.DEPARTMENT_ID).IsRequired();
            entity.Property(e => e.DELIVERABLE_TYPE_ID).IsRequired();
            entity.Property(e => e.INTERNAL_DOCUMENT_NUMBER).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CLIENT_DOCUMENT_NUMBER).HasMaxLength(100);
            entity.Property(e => e.DOCUMENT_TITLE).HasMaxLength(255).IsRequired();
            entity.Property(e => e.BUDGET_HOURS).HasColumnType("decimal(10,2)").IsRequired().HasDefaultValue(0);
            entity.Property(e => e.VARIATION_HOURS).HasColumnType("decimal(10,2)").IsRequired().HasDefaultValue(0);
            entity.Property(e => e.TOTAL_COST).HasColumnType("decimal(15,2)").IsRequired().HasDefaultValue(0);
            entity.Property(e => e.CREATED).HasColumnType("datetime").IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.CREATEDBY).IsRequired();
            entity.Property(e => e.UPDATED).HasColumnType("datetime");
            entity.Property(e => e.UPDATEDBY);
            entity.Property(e => e.DELETED).HasColumnType("datetime");
            entity.Property(e => e.DELETEDBY);
            entity.Property(e => e.GUID_DELIVERABLE_GATE);

            // Configure the enum properties
            entity.Property(e => e.DELIVERABLE_TYPE_ID)
                .HasColumnType("int")
                .IsRequired();

            entity.Property(e => e.DEPARTMENT_ID)
                .HasColumnType("int")
                .IsRequired();

            // Foreign key relationships
            entity.HasOne(d => d.Project)
                .WithMany(p => p.Deliverables)
                .HasForeignKey(d => d.GUID_PROJECT)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(d => d.DeliverableGate)
                .WithMany(p => p.Deliverables)
                .HasForeignKey(d => d.GUID_DELIVERABLE_GATE)
                .OnDelete(DeleteBehavior.NoAction);
                
            entity.HasOne(d => d.Variation)
                .WithMany(v => v.Deliverables)
                .HasForeignKey(d => d.GUID_VARIATION)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<PROGRESS>(entity =>
        {
            entity.HasKey(e => e.GUID);
            entity.ToTable("PROGRESS");

            entity.HasIndex(e => e.GUID_DELIVERABLE).HasDatabaseName("IX_PROGRESS_DELIVERABLE_ID");
            entity.HasIndex(e => e.PERIOD).HasDatabaseName("IX_PROGRESS_PERIOD");
            entity.HasIndex(e => e.DELETED).HasDatabaseName("IX_PROGRESS_DELETED");
            
            // Add filtered unique index for active progress records
            entity.HasIndex(e => new { e.GUID_DELIVERABLE, e.PERIOD })
                .HasDatabaseName("IX_PROGRESS_DELIVERABLE_PERIOD_UNIQUE_ACTIVE")
                .IsUnique()
                .HasFilter("[DELETED] IS NULL");

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

        modelBuilder.Entity<DISCIPLINE>(entity =>
        {
            entity.HasKey(e => e.GUID);
            entity.ToTable("DISCIPLINE");

            entity.HasIndex(e => e.CODE).HasDatabaseName("IX_DISCIPLINE_CODE");
            entity.HasIndex(e => e.DELETED).HasDatabaseName("IX_DISCIPLINE_DELETED");

            entity.Property(e => e.GUID).ValueGeneratedNever();
            entity.Property(e => e.CODE).HasMaxLength(2).IsRequired();
            entity.Property(e => e.NAME).HasMaxLength(500);
            entity.Property(e => e.CREATED).HasColumnType("datetime").IsRequired();
            entity.Property(e => e.CREATEDBY).IsRequired();
            entity.Property(e => e.UPDATED).HasColumnType("datetime");
            entity.Property(e => e.UPDATEDBY);
            entity.Property(e => e.DELETED).HasColumnType("datetime");
            entity.Property(e => e.DELETEDBY);
        });

        modelBuilder.Entity<DOCUMENT_TYPE>(entity =>
        {
            entity.HasKey(e => e.GUID);
            entity.ToTable("DOCUMENT_TYPE");

            entity.HasIndex(e => e.CODE).HasDatabaseName("IX_DOCUMENT_TYPE_CODE");
            entity.HasIndex(e => e.DELETED).HasDatabaseName("IX_DOCUMENT_TYPE_DELETED");

            entity.Property(e => e.GUID).ValueGeneratedNever();
            entity.Property(e => e.CODE).HasMaxLength(3).IsRequired();
            entity.Property(e => e.NAME).HasMaxLength(500);
            entity.Property(e => e.CREATED).HasColumnType("datetime").IsRequired();
            entity.Property(e => e.CREATEDBY).IsRequired();
            entity.Property(e => e.UPDATED).HasColumnType("datetime");
            entity.Property(e => e.UPDATEDBY);
            entity.Property(e => e.DELETED).HasColumnType("datetime");
            entity.Property(e => e.DELETEDBY);
        });

        modelBuilder.Entity<AREA>(entity =>
        {
            entity.HasKey(e => e.GUID);
            entity.ToTable("AREA");

            entity.Property(e => e.GUID).ValueGeneratedNever();
            entity.Property(e => e.GUID_PROJECT).IsRequired();
            entity.Property(e => e.NUMBER).HasMaxLength(2).IsRequired();
            entity.Property(e => e.DESCRIPTION).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CREATED).HasColumnType("datetime").IsRequired();
            entity.Property(e => e.CREATEDBY).IsRequired();
            entity.Property(e => e.UPDATED).HasColumnType("datetime");
            entity.Property(e => e.UPDATEDBY);
            entity.Property(e => e.DELETED).HasColumnType("datetime");
            entity.Property(e => e.DELETEDBY);

            // Configure the foreign key relationship
            entity.HasOne(d => d.Project)
                .WithMany(p => p.Areas)
                .HasForeignKey(d => d.GUID_PROJECT)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<DELIVERABLE_GATE>(entity =>
        {
            entity.HasKey(e => e.GUID);
            entity.ToTable("DELIVERABLE_GATE");

            entity.HasIndex(e => e.DELETED).HasDatabaseName("IX_DELIVERABLE_GATE_DELETED");

            entity.Property(e => e.GUID).ValueGeneratedNever();
            entity.Property(e => e.NAME).HasMaxLength(100).IsRequired();
            entity.Property(e => e.MAX_PERCENTAGE).HasColumnType("decimal(5, 2)").IsRequired();
            entity.Property(e => e.AUTO_PERCENTAGE).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.CREATED).HasColumnType("datetime").IsRequired();
            entity.Property(e => e.CREATEDBY).IsRequired();
            entity.Property(e => e.UPDATED).HasColumnType("datetime");
            entity.Property(e => e.UPDATEDBY);
            entity.Property(e => e.DELETED).HasColumnType("datetime");
            entity.Property(e => e.DELETEDBY);
        });

        modelBuilder.Entity<VARIATION>(entity =>
        {
            entity.HasKey(e => e.GUID);
            entity.ToTable("VARIATION");

            entity.HasIndex(e => e.GUID_PROJECT).HasDatabaseName("IX_VARIATION_PROJECT_ID");
            entity.HasIndex(e => e.DELETED).HasDatabaseName("IX_VARIATION_DELETED");

            entity.Property(e => e.GUID).ValueGeneratedNever();
            entity.Property(e => e.GUID_PROJECT).IsRequired();
            entity.Property(e => e.NAME).HasMaxLength(500).IsRequired();
            entity.Property(e => e.COMMENTS).HasMaxLength(1000);
            entity.Property(e => e.SUBMITTED).HasColumnType("datetime");
            entity.Property(e => e.SUBMITTEDBY);
            entity.Property(e => e.CLIENT_APPROVED).HasColumnType("datetime");
            entity.Property(e => e.CLIENT_APPROVEDBY);
            entity.Property(e => e.CREATED).HasColumnType("datetime").IsRequired();
            entity.Property(e => e.CREATEDBY).IsRequired();
            entity.Property(e => e.UPDATED).HasColumnType("datetime");
            entity.Property(e => e.UPDATEDBY);
            entity.Property(e => e.DELETED).HasColumnType("datetime");
            entity.Property(e => e.DELETEDBY);

            // Configure the foreign key relationship
            entity.HasOne(d => d.Project)
                .WithMany(p => p.Variations)
                .HasForeignKey(d => d.GUID_PROJECT)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ROLE>(entity =>
        {
            entity.HasKey(e => e.GUID);
            entity.ToTable("ROLE");

            entity.HasIndex(e => e.NAME).HasDatabaseName("IX_ROLE_NAME");
            entity.HasIndex(e => e.DELETED).HasDatabaseName("IX_ROLE_DELETED");

            entity.Property(e => e.GUID).ValueGeneratedOnAdd();
            entity.Property(e => e.NAME).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DISPLAY_NAME).HasMaxLength(100).IsRequired();
            entity.Property(e => e.DESCRIPTION).HasMaxLength(500);
            entity.Property(e => e.IS_SYSTEM_ROLE).HasDefaultValue(false);
            entity.Property(e => e.CREATED).HasColumnType("datetime").IsRequired();
            entity.Property(e => e.CREATEDBY).IsRequired();
            entity.Property(e => e.UPDATED).HasColumnType("datetime");
            entity.Property(e => e.UPDATEDBY);
            entity.Property(e => e.DELETED).HasColumnType("datetime");
            entity.Property(e => e.DELETEDBY);
        });

        modelBuilder.Entity<ROLE_PERMISSION>(entity =>
        {
            entity.HasKey(e => e.GUID);
            entity.ToTable("ROLE_PERMISSION");

            entity.HasIndex(e => e.GUID_ROLE).HasDatabaseName("IX_ROLE_PERMISSION_ROLE_ID");
            entity.HasIndex(e => e.PERMISSION).HasDatabaseName("IX_ROLE_PERMISSION_PERMISSION");
            entity.HasIndex(e => e.DELETED).HasDatabaseName("IX_ROLE_PERMISSION_DELETED");

            entity.Property(e => e.GUID).ValueGeneratedOnAdd();
            entity.Property(e => e.GUID_ROLE).IsRequired();
            entity.Property(e => e.PERMISSION).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CREATED).HasColumnType("datetime").IsRequired();
            entity.Property(e => e.CREATEDBY).IsRequired();
            entity.Property(e => e.UPDATED).HasColumnType("datetime");
            entity.Property(e => e.UPDATEDBY);
            entity.Property(e => e.DELETED).HasColumnType("datetime");
            entity.Property(e => e.DELETEDBY);

            // Configure the foreign key relationship
            entity.HasOne(d => d.ROLE)
                .WithMany(p => p.ROLE_PERMISSIONs)
                .HasForeignKey(d => d.GUID_ROLE)
                .OnDelete(DeleteBehavior.Cascade);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
