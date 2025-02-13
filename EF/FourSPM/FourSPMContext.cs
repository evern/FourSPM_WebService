using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FourSPM_WebService.EF.FourSPM;

public partial class FourSPMContext : DbContext
{
    public FourSPMContext()
    {
    }

    public FourSPMContext(DbContextOptions<FourSPMContext> options)
        : base(options)
    {
    }

    public virtual DbSet<PROJECT> PROJECTs { get; set; }

    public virtual DbSet<USER> USERs { get; set; }

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
            entity.Property(e => e.CLIENT).HasMaxLength(100);
            entity.Property(e => e.CREATED).HasColumnType("datetime");
            entity.Property(e => e.DELETED).HasColumnType("datetime");
            entity.Property(e => e.NAME).HasMaxLength(100);
            entity.Property(e => e.NUMBER).HasMaxLength(100);
            entity.Property(e => e.UPDATED).HasColumnType("datetime");
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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
