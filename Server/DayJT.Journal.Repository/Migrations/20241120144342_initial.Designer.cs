﻿// <auto-generated />
using System.Collections.Generic;
using DayJT.Journal.DataContext.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DayJTrading.Journal.DbContext.Migrations
{
    [DbContext(typeof(TradingJournalDataContext))]
    [Migration("20241120144342_initial")]
#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
    partial class initial
#pragma warning restore CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("DayJT.Journal.Data.Cell", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("ComponentType")
                        .HasColumnType("integer");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("ContentWrapperId")
                        .HasColumnType("integer");

                    b.Property<int>("CostRelevance")
                        .HasColumnType("integer");

                    b.Property<bool>("IsRelevantForOverview")
                        .HasColumnType("boolean");

                    b.Property<int>("PriceRelevance")
                        .HasColumnType("integer");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("TradeElementRefId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("ContentWrapperId");

                    b.HasIndex("TradeElementRefId");

                    b.ToTable("AllEntries");
                });

            modelBuilder.Entity("DayJT.Journal.Data.ContentRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("CellRefId")
                        .HasColumnType("integer");

                    b.Property<string>("ChangeNote")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("CellRefId");

                    b.ToTable("AllContentRecords");
                });

            modelBuilder.Entity("DayJT.Journal.Data.TradeComposite", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Sector")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("SummaryId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("SummaryId");

                    b.ToTable("AllTradeComposites");
                });

            modelBuilder.Entity("DayJT.Journal.Data.TradeElement", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("TradeActionType")
                        .HasColumnType("integer");

                    b.Property<int>("TradeCompositeRefId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("TradeCompositeRefId");

                    b.ToTable("AllTradeElements");
                });

            modelBuilder.Entity("DayJTrading.Journal.Data.JournalData", b =>
                {
                    b.PrimitiveCollection<List<string>>("SavedSectors")
                        .HasColumnType("text[]");

                    b.ToTable("JournalData");
                });

            modelBuilder.Entity("DayJT.Journal.Data.Cell", b =>
                {
                    b.HasOne("DayJT.Journal.Data.ContentRecord", "ContentWrapper")
                        .WithMany()
                        .HasForeignKey("ContentWrapperId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DayJT.Journal.Data.TradeElement", "TradeElementRef")
                        .WithMany("Entries")
                        .HasForeignKey("TradeElementRefId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ContentWrapper");

                    b.Navigation("TradeElementRef");
                });

            modelBuilder.Entity("DayJT.Journal.Data.ContentRecord", b =>
                {
                    b.HasOne("DayJT.Journal.Data.Cell", "CellRef")
                        .WithMany("History")
                        .HasForeignKey("CellRefId");

                    b.Navigation("CellRef");
                });

            modelBuilder.Entity("DayJT.Journal.Data.TradeComposite", b =>
                {
                    b.HasOne("DayJT.Journal.Data.TradeElement", "Summary")
                        .WithMany()
                        .HasForeignKey("SummaryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Summary");
                });

            modelBuilder.Entity("DayJT.Journal.Data.TradeElement", b =>
                {
                    b.HasOne("DayJT.Journal.Data.TradeComposite", "TradeCompositeRef")
                        .WithMany("TradeElements")
                        .HasForeignKey("TradeCompositeRefId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("TradeCompositeRef");
                });

            modelBuilder.Entity("DayJT.Journal.Data.Cell", b =>
                {
                    b.Navigation("History");
                });

            modelBuilder.Entity("DayJT.Journal.Data.TradeComposite", b =>
                {
                    b.Navigation("TradeElements");
                });

            modelBuilder.Entity("DayJT.Journal.Data.TradeElement", b =>
                {
                    b.Navigation("Entries");
                });
#pragma warning restore 612, 618
        }
    }
}