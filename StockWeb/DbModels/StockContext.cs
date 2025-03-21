﻿using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace StockWeb.DbModels;

public partial class StockContext : DbContext
{
    public StockContext()
    {
    }

    public StockContext(DbContextOptions<StockContext> options)
        : base(options)
    {
    }

    public virtual DbSet<DividendYield> DividendYields { get; set; }

    public virtual DbSet<MarketDayInfo> MarketDayInfos { get; set; }

    public virtual DbSet<StockBaseInfo> StockBaseInfos { get; set; }

    public virtual DbSet<StockDayInfo> StockDayInfos { get; set; }

    public virtual DbSet<StockMa60breakoutDay> StockMa60breakoutDays { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<月營收> 月營收s { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=.;User ID=sa;Password=Test.123;Initial Catalog=Stock;TrustServerCertificate=true");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DividendYield>(entity =>
        {
            entity.HasKey(e => new { e.StockId, e.PayDate });

            entity.ToTable("DividendYield");

            entity.HasOne(d => d.Stock).WithMany(p => p.DividendYields)
                .HasForeignKey(d => d.StockId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DividendYield_StockBaseInfo");
        });

        modelBuilder.Entity<MarketDayInfo>(entity =>
        {
            entity.HasKey(e => e.Date);

            entity.ToTable("MarketDayInfo");
        });

        modelBuilder.Entity<StockBaseInfo>(entity =>
        {
            entity.HasKey(e => e.StockId);

            entity.ToTable("StockBaseInfo");

            entity.Property(e => e.StockId)
                .ValueGeneratedNever()
                .HasColumnName("StockID");
            entity.Property(e => e.Category).HasMaxLength(20);
            entity.Property(e => e.StockName).HasMaxLength(15);
            entity.Property(e => e.StockType)
                .HasMaxLength(3)
                .IsUnicode(false);
        });

        modelBuilder.Entity<StockDayInfo>(entity =>
        {
            entity.HasKey(e => new { e.StockId, e.Date });

            entity.ToTable("StockDayInfo");

            entity.HasIndex(e => new { e.Date, e.StockId }, "NonClusteredIndex-20240225-001850");

            entity.Property(e => e.Date).HasComment("測試測試");

            entity.HasOne(d => d.Stock).WithMany(p => p.StockDayInfos)
                .HasForeignKey(d => d.StockId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockDayInfo_StockBaseInfo");
        });

        modelBuilder.Entity<StockMa60breakoutDay>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("StockMA60BreakoutDays");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.Property(e => e.UserId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Account)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.Password)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.UserName).HasMaxLength(15);
        });

        modelBuilder.Entity<月營收>(entity =>
        {
            entity.ToTable("月營收");

            entity.Property(e => e.累計yoY).HasColumnName("累計YoY");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
