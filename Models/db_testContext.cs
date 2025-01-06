using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace dbFinal.Models;

public partial class db_testContext : DbContext
{
    public db_testContext(DbContextOptions<db_testContext> options)
        : base(options)
    {
    }

    public virtual DbSet<favorite> favorite { get; set; }

    public virtual DbSet<feedback> feedback { get; set; }

    public virtual DbSet<ingredient> ingredient { get; set; }

    public virtual DbSet<meal> meal { get; set; }

    public virtual DbSet<restaurant> restaurant { get; set; }

    public virtual DbSet<review> review { get; set; }

    public virtual DbSet<user> user { get; set; }

    public virtual DbSet<announcement> announcement { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<favorite>(entity =>
        {
            entity.HasKey(e => e.FAV_ID).HasName("PRIMARY");

            entity.HasIndex(e => e.USER_ID, "USER_ID");

            entity.Property(e => e.FAV_ALLERGIES).HasMaxLength(200);
            entity.Property(e => e.FAV_CUISINE).HasMaxLength(100);
            entity.Property(e => e.FAV_KIND).HasMaxLength(100);

            entity.HasOne(d => d.USER).WithMany(p => p.favorite)
                .HasForeignKey(d => d.USER_ID)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("favorite_ibfk_1");
        });

        modelBuilder.Entity<feedback>(entity =>
        {
            entity.HasKey(e => e.FEEDBACK_ID).HasName("PRIMARY");

            entity.HasIndex(e => e.USER_ID, "USER_ID");

            entity.Property(e => e.FEEDBACK_WORD).HasColumnType("text");

            entity.HasOne(d => d.USER).WithMany(p => p.feedback)
                .HasForeignKey(d => d.USER_ID)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("feedback_ibfk_1");
        });

        modelBuilder.Entity<ingredient>(entity =>
        {
            entity.HasKey(e => e.ING_ID).HasName("PRIMARY");

            entity.Property(e => e.ING_ALLERGENINFO).HasColumnType("text");
            entity.Property(e => e.ING_NAME).HasMaxLength(100);
        });

        modelBuilder.Entity<meal>(entity =>
        {
            entity.HasKey(e => e.MEAL_ID).HasName("PRIMARY");

            entity.HasIndex(e => e.REST_ID, "REST_ID");

            entity.Property(e => e.MEAL_ALLERGENINFO).HasColumnType("text");
            entity.Property(e => e.MEAL_DESCRIPTION).HasColumnType("text");
            entity.Property(e => e.MEAL_INGREDIENTS).HasColumnType("text");
            entity.Property(e => e.MEAL_NAME).HasMaxLength(100);

            entity.HasOne(d => d.REST).WithMany(p => p.meal)
                .HasForeignKey(d => d.REST_ID)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("meal_ibfk_1");

            entity.HasMany(d => d.ING).WithMany(p => p.MEAL)
                .UsingEntity<Dictionary<string, object>>(
                    "meal_ingredient",
                    r => r.HasOne<ingredient>().WithMany()
                        .HasForeignKey("ING_ID")
                        .HasConstraintName("meal_ingredient_ibfk_2"),
                    l => l.HasOne<meal>().WithMany()
                        .HasForeignKey("MEAL_ID")
                        .HasConstraintName("meal_ingredient_ibfk_1"),
                    j =>
                    {
                        j.HasKey("MEAL_ID", "ING_ID").HasName("PRIMARY");
                        j.HasIndex(new[] { "ING_ID" }, "ING_ID");
                    });
        });

        modelBuilder.Entity<restaurant>(entity =>
        {
            entity.HasKey(e => e.REST_ID).HasName("PRIMARY");

            entity.Property(e => e.REST_ADDRESS).HasMaxLength(200);
            entity.Property(e => e.REST_BUSINESSHOURS).HasMaxLength(100);
            entity.Property(e => e.REST_MENU).HasColumnType("text");
            entity.Property(e => e.REST_NAME).HasMaxLength(100);
            entity.Property(e => e.REST_PASSWORD).HasMaxLength(100);
        });

        modelBuilder.Entity<review>(entity =>
        {
            entity.HasKey(e => e.REVIEW_ID).HasName("PRIMARY");

            entity.HasIndex(e => e.MEAL_ID, "MEAL_ID");

            entity.HasIndex(e => e.USER_ID, "USER_ID");

            entity.Property(e => e.REVIEW_DATE).HasColumnType("date");

            entity.HasOne(d => d.MEAL).WithMany(p => p.review)
                .HasForeignKey(d => d.MEAL_ID)
                .HasConstraintName("review_ibfk_2");

            entity.HasOne(d => d.USER).WithMany(p => p.review)
                .HasForeignKey(d => d.USER_ID)
                .HasConstraintName("review_ibfk_1");
        });

        modelBuilder.Entity<user>(entity =>
        {
            entity.HasKey(e => e.USER_ID).HasName("PRIMARY");

            entity.HasIndex(e => e.USER_EMAIL, "USER_EMAIL").IsUnique();

            entity.Property(e => e.USER_EMAIL).HasMaxLength(100);
            entity.Property(e => e.USER_LOCATION).HasMaxLength(100);
            entity.Property(e => e.USER_NAME).HasMaxLength(100);
            entity.Property(e => e.USER_PASSWORD).HasMaxLength(100);

            entity.HasMany(d => d.REST).WithMany(p => p.USER)
                .UsingEntity<Dictionary<string, object>>(
                    "user_restaurant",
                    r => r.HasOne<restaurant>().WithMany()
                        .HasForeignKey("REST_ID")
                        .HasConstraintName("user_restaurant_ibfk_2"),
                    l => l.HasOne<user>().WithMany()
                        .HasForeignKey("USER_ID")
                        .HasConstraintName("user_restaurant_ibfk_1"),
                    j =>
                    {
                        j.HasKey("USER_ID", "REST_ID").HasName("PRIMARY");
                        j.HasIndex(new[] { "REST_ID" }, "REST_ID");
                    });
        });

        modelBuilder.Entity<announcement>(entity =>
        {
            // 設定主鍵
            entity.HasKey(e => e.ANNO_ID).HasName("PRIMARY");

            // 設定欄位屬性
            entity.Property(e => e.ANNO_TITLE).HasMaxLength(255).IsRequired(); // 公告標題
            entity.Property(e => e.ANNO_CONTENT).HasColumnType("text").IsRequired(); // 公告內容
            entity.Property(e => e.ANNO_IS_EMERGENCY).HasDefaultValue(false).IsRequired(); // 是否為緊急公告
            entity.Property(e => e.ANNO_CREATED_AT).HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired(); // 公告創建時間

            // 設定外鍵關聯到 restaurant
            entity.HasOne(d => d.RESTAURANT)
                .WithMany(p => p.ANNOUNCEMENTS) // 關聯到餐廳的公告集合
                .HasForeignKey(d => d.REST_ID)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("announcement_ibfk_1");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
