using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace takip.Migrations
{
    /// <inheritdoc />
    public partial class AddMoldProductions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Admixture2Aliases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slot = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admixture2Aliases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdmixtureAliases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slot = table.Column<short>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmixtureAliases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Aggregate2Aliases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slot = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aggregate2Aliases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AggregateAliases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slot = table.Column<short>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AggregateAliases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cement2Aliases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slot = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cement2Aliases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CementAliases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slot = table.Column<short>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CementAliases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CementSilos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SiloNumber = table.Column<int>(type: "integer", nullable: false),
                    CementType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CurrentAmount = table.Column<double>(type: "double precision", nullable: false),
                    LastRefillDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Capacity = table.Column<double>(type: "double precision", nullable: false),
                    MinLevel = table.Column<double>(type: "double precision", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CementSilos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConcreteBatch2s",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlantCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OperatorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RecipeCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsSimulated = table.Column<bool>(type: "boolean", nullable: false),
                    MoisturePercent = table.Column<double>(type: "double precision", nullable: true),
                    LoadcellWaterKg = table.Column<double>(type: "double precision", nullable: false),
                    PulseWaterKg = table.Column<double>(type: "double precision", nullable: false),
                    Pigment1Kg = table.Column<double>(type: "double precision", nullable: false),
                    Pigment2Kg = table.Column<double>(type: "double precision", nullable: false),
                    Pigment3Kg = table.Column<double>(type: "double precision", nullable: false),
                    Pigment4Kg = table.Column<double>(type: "double precision", nullable: false),
                    TotalCementKg = table.Column<double>(type: "double precision", nullable: false),
                    TotalAggregateKg = table.Column<double>(type: "double precision", nullable: false),
                    TotalAdmixtureKg = table.Column<double>(type: "double precision", nullable: false),
                    TotalPigmentKg = table.Column<double>(type: "double precision", nullable: false),
                    EffectiveWaterKg = table.Column<double>(type: "double precision", nullable: false),
                    WaterCementRatio = table.Column<double>(type: "double precision", nullable: true),
                    RawPayloadJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "yatay_kovada")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConcreteBatch2s", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConcreteBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlantCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OperatorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RecipeCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsSimulated = table.Column<bool>(type: "boolean", nullable: false),
                    MoisturePercent = table.Column<double>(type: "double precision", nullable: true),
                    LoadcellWaterKg = table.Column<double>(type: "double precision", nullable: false),
                    PulseWaterKg = table.Column<double>(type: "double precision", nullable: false),
                    PigmentKg = table.Column<double>(type: "double precision", nullable: false),
                    TotalCementKg = table.Column<double>(type: "double precision", nullable: false),
                    TotalAggregateKg = table.Column<double>(type: "double precision", nullable: false),
                    TotalAdmixtureKg = table.Column<double>(type: "double precision", nullable: false),
                    TotalPigmentKg = table.Column<double>(type: "double precision", nullable: false),
                    EffectiveWaterKg = table.Column<double>(type: "double precision", nullable: false),
                    WaterCementRatio = table.Column<double>(type: "double precision", nullable: true),
                    RawPayloadJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConcreteBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Molds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TotalPrints = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Molds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Operators",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Operators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pigment2Aliases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slot = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pigment2Aliases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PigmentAliases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slot = table.Column<short>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PigmentAliases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlcDataSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Operator = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RecipeCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AggregateGroupActive = table.Column<bool>(type: "boolean", nullable: false),
                    WaterGroupActive = table.Column<bool>(type: "boolean", nullable: false),
                    CementGroupActive = table.Column<bool>(type: "boolean", nullable: false),
                    AdmixtureGroupActive = table.Column<bool>(type: "boolean", nullable: false),
                    PigmentGroupActive = table.Column<bool>(type: "boolean", nullable: false),
                    Aggregate1Active = table.Column<bool>(type: "boolean", nullable: false),
                    Aggregate1Amount = table.Column<double>(type: "double precision", nullable: false),
                    Aggregate1TartimOk = table.Column<bool>(type: "boolean", nullable: false),
                    Aggregate2Active = table.Column<bool>(type: "boolean", nullable: false),
                    Aggregate2Amount = table.Column<double>(type: "double precision", nullable: false),
                    Aggregate2TartimOk = table.Column<bool>(type: "boolean", nullable: false),
                    Aggregate3Active = table.Column<bool>(type: "boolean", nullable: false),
                    Aggregate3Amount = table.Column<double>(type: "double precision", nullable: false),
                    Aggregate3TartimOk = table.Column<bool>(type: "boolean", nullable: false),
                    Aggregate4Active = table.Column<bool>(type: "boolean", nullable: false),
                    Aggregate4Amount = table.Column<double>(type: "double precision", nullable: false),
                    Aggregate4TartimOk = table.Column<bool>(type: "boolean", nullable: false),
                    Aggregate5Active = table.Column<bool>(type: "boolean", nullable: false),
                    Aggregate5Amount = table.Column<double>(type: "double precision", nullable: false),
                    Aggregate5TartimOk = table.Column<bool>(type: "boolean", nullable: false),
                    Aggregate6Active = table.Column<bool>(type: "boolean", nullable: false),
                    Aggregate6Amount = table.Column<double>(type: "double precision", nullable: false),
                    Aggregate6TartimOk = table.Column<bool>(type: "boolean", nullable: false),
                    Aggregate7Active = table.Column<bool>(type: "boolean", nullable: false),
                    Aggregate7Amount = table.Column<double>(type: "double precision", nullable: false),
                    Aggregate7TartimOk = table.Column<bool>(type: "boolean", nullable: false),
                    Aggregate8Active = table.Column<bool>(type: "boolean", nullable: false),
                    Aggregate8Amount = table.Column<double>(type: "double precision", nullable: false),
                    Aggregate8TartimOk = table.Column<bool>(type: "boolean", nullable: false),
                    Water1Active = table.Column<bool>(type: "boolean", nullable: false),
                    Water1Amount = table.Column<double>(type: "double precision", nullable: false),
                    Water2Active = table.Column<bool>(type: "boolean", nullable: false),
                    Water2Amount = table.Column<double>(type: "double precision", nullable: false),
                    Cement1Active = table.Column<bool>(type: "boolean", nullable: false),
                    Cement1Amount = table.Column<double>(type: "double precision", nullable: false),
                    Cement1TartimOk = table.Column<bool>(type: "boolean", nullable: false),
                    Cement2Active = table.Column<bool>(type: "boolean", nullable: false),
                    Cement2Amount = table.Column<double>(type: "double precision", nullable: false),
                    Cement2TartimOk = table.Column<bool>(type: "boolean", nullable: false),
                    Cement3Active = table.Column<bool>(type: "boolean", nullable: false),
                    Cement3Amount = table.Column<double>(type: "double precision", nullable: false),
                    Cement3TartimOk = table.Column<bool>(type: "boolean", nullable: false),
                    Admixture1Active = table.Column<bool>(type: "boolean", nullable: false),
                    Admixture1TartimOk = table.Column<bool>(type: "boolean", nullable: false),
                    Admixture1WaterTartimOk = table.Column<bool>(type: "boolean", nullable: false),
                    Admixture1ChemicalAmount = table.Column<double>(type: "double precision", nullable: false),
                    Admixture1WaterAmount = table.Column<double>(type: "double precision", nullable: false),
                    Admixture2Active = table.Column<bool>(type: "boolean", nullable: false),
                    Admixture2TartimOk = table.Column<bool>(type: "boolean", nullable: false),
                    Admixture2WaterTartimOk = table.Column<bool>(type: "boolean", nullable: false),
                    Admixture2ChemicalAmount = table.Column<double>(type: "double precision", nullable: false),
                    Admixture2WaterAmount = table.Column<double>(type: "double precision", nullable: false),
                    Admixture3Active = table.Column<bool>(type: "boolean", nullable: false),
                    Admixture3TartimOk = table.Column<bool>(type: "boolean", nullable: false),
                    Admixture3WaterTartimOk = table.Column<bool>(type: "boolean", nullable: false),
                    Admixture3ChemicalAmount = table.Column<double>(type: "double precision", nullable: false),
                    Admixture3WaterAmount = table.Column<double>(type: "double precision", nullable: false),
                    Admixture4Active = table.Column<bool>(type: "boolean", nullable: false),
                    Admixture4TartimOk = table.Column<bool>(type: "boolean", nullable: false),
                    Admixture4WaterTartimOk = table.Column<bool>(type: "boolean", nullable: false),
                    Admixture4ChemicalAmount = table.Column<double>(type: "double precision", nullable: false),
                    Admixture4WaterAmount = table.Column<double>(type: "double precision", nullable: false),
                    Pigment1Active = table.Column<bool>(type: "boolean", nullable: false),
                    Pigment1TartimOk = table.Column<bool>(type: "boolean", nullable: false),
                    Pigment1Amount = table.Column<double>(type: "double precision", nullable: false),
                    Pigment2Active = table.Column<bool>(type: "boolean", nullable: false),
                    Pigment2TartimOk = table.Column<bool>(type: "boolean", nullable: false),
                    Pigment2Amount = table.Column<double>(type: "double precision", nullable: false),
                    Pigment3Active = table.Column<bool>(type: "boolean", nullable: false),
                    Pigment3TartimOk = table.Column<bool>(type: "boolean", nullable: false),
                    Pigment3Amount = table.Column<double>(type: "double precision", nullable: false),
                    Pigment4Active = table.Column<bool>(type: "boolean", nullable: false),
                    Pigment4TartimOk = table.Column<bool>(type: "boolean", nullable: false),
                    Pigment4Amount = table.Column<double>(type: "double precision", nullable: false),
                    MoisturePercent = table.Column<double>(type: "double precision", nullable: false),
                    BatchReadySignal = table.Column<bool>(type: "boolean", nullable: false),
                    HorizontalHasMaterial = table.Column<bool>(type: "boolean", nullable: false),
                    VerticalHasMaterial = table.Column<bool>(type: "boolean", nullable: false),
                    WaitingBunkerHasMaterial = table.Column<bool>(type: "boolean", nullable: false),
                    MixerHasAggregate = table.Column<bool>(type: "boolean", nullable: false),
                    MixerHasCement = table.Column<bool>(type: "boolean", nullable: false),
                    MixerHasAdmixture = table.Column<bool>(type: "boolean", nullable: false),
                    MixerHasWaterLoadcell = table.Column<bool>(type: "boolean", nullable: false),
                    MixerHasWaterPulse = table.Column<bool>(type: "boolean", nullable: false),
                    RawDataJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlcDataSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Productions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OperatorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StoneName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShiftRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShiftStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ShiftEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OperatorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TotalProduction = table.Column<int>(type: "integer", nullable: false),
                    ProductionStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ShiftDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    ProductionDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    StoneProductionJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    FireProductCount = table.Column<int>(type: "integer", nullable: false),
                    Mixer1BatchCount = table.Column<int>(type: "integer", nullable: false),
                    Mixer1CementTotal = table.Column<double>(type: "double precision", nullable: false),
                    Mixer1CementTypesJson = table.Column<string>(type: "text", nullable: false),
                    Mixer2BatchCount = table.Column<int>(type: "integer", nullable: false),
                    Mixer2CementTotal = table.Column<double>(type: "double precision", nullable: false),
                    Mixer2CementTypesJson = table.Column<string>(type: "text", nullable: false),
                    MoldProductionJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    OperatorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CementConsumptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SiloId = table.Column<int>(type: "integer", nullable: false),
                    ConsumedAmount = table.Column<double>(type: "double precision", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BatchId = table.Column<int>(type: "integer", nullable: true),
                    MixerId = table.Column<int>(type: "integer", nullable: false),
                    RemainingAmount = table.Column<double>(type: "double precision", nullable: false),
                    ConsumptionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CementConsumptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CementConsumptions_CementSilos_SiloId",
                        column: x => x.SiloId,
                        principalTable: "CementSilos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CementRefills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SiloId = table.Column<int>(type: "integer", nullable: false),
                    AddedAmount = table.Column<double>(type: "double precision", nullable: false),
                    RefilledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreviousAmount = table.Column<double>(type: "double precision", nullable: false),
                    NewAmount = table.Column<double>(type: "double precision", nullable: false),
                    OperatorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ShipmentNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Supplier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CementRefills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CementRefills_CementSilos_SiloId",
                        column: x => x.SiloId,
                        principalTable: "CementSilos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConcreteBatch2Admixtures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchId = table.Column<int>(type: "integer", nullable: false),
                    Slot = table.Column<short>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ChemicalKg = table.Column<double>(type: "double precision", nullable: false),
                    WaterKg = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConcreteBatch2Admixtures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConcreteBatch2Admixtures_ConcreteBatch2s_BatchId",
                        column: x => x.BatchId,
                        principalTable: "ConcreteBatch2s",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConcreteBatch2Aggregates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchId = table.Column<int>(type: "integer", nullable: false),
                    Slot = table.Column<short>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    WeightKg = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConcreteBatch2Aggregates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConcreteBatch2Aggregates_ConcreteBatch2s_BatchId",
                        column: x => x.BatchId,
                        principalTable: "ConcreteBatch2s",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConcreteBatch2Cements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchId = table.Column<int>(type: "integer", nullable: false),
                    Slot = table.Column<short>(type: "smallint", nullable: false),
                    CementType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WeightKg = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConcreteBatch2Cements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConcreteBatch2Cements_ConcreteBatch2s_BatchId",
                        column: x => x.BatchId,
                        principalTable: "ConcreteBatch2s",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConcreteBatchAdmixtures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchId = table.Column<int>(type: "integer", nullable: false),
                    Slot = table.Column<short>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ChemicalKg = table.Column<double>(type: "double precision", nullable: false),
                    WaterKg = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConcreteBatchAdmixtures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConcreteBatchAdmixtures_ConcreteBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "ConcreteBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConcreteBatchAggregates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchId = table.Column<int>(type: "integer", nullable: false),
                    Slot = table.Column<short>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WeightKg = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConcreteBatchAggregates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConcreteBatchAggregates_ConcreteBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "ConcreteBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConcreteBatchCements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchId = table.Column<int>(type: "integer", nullable: false),
                    Slot = table.Column<short>(type: "smallint", nullable: false),
                    CementType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WeightKg = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConcreteBatchCements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConcreteBatchCements_ConcreteBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "ConcreteBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConcreteBatchPigments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchId = table.Column<int>(type: "integer", nullable: false),
                    Slot = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Kg = table.Column<double>(type: "double precision", nullable: false),
                    WeightKg = table.Column<double>(type: "double precision", nullable: false),
                    Percent = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConcreteBatchPigments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConcreteBatchPigments_ConcreteBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "ConcreteBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductionId = table.Column<int>(type: "integer", nullable: false),
                    StoneName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    TotalCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionLogs_Productions_ProductionId",
                        column: x => x.ProductionId,
                        principalTable: "Productions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MoldProductions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShiftRecordId = table.Column<int>(type: "integer", nullable: false),
                    MoldId = table.Column<int>(type: "integer", nullable: false),
                    MoldName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProductionCount = table.Column<int>(type: "integer", nullable: false),
                    OperatorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoldProductions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MoldProductions_Molds_MoldId",
                        column: x => x.MoldId,
                        principalTable: "Molds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MoldProductions_ShiftRecords_ShiftRecordId",
                        column: x => x.ShiftRecordId,
                        principalTable: "ShiftRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShiftId = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false),
                    FireProductCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionNotes_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Admixture2Alias_Slot",
                table: "Admixture2Aliases",
                column: "Slot");

            migrationBuilder.CreateIndex(
                name: "IX_AdmixtureAlias_Slot",
                table: "AdmixtureAliases",
                column: "Slot");

            migrationBuilder.CreateIndex(
                name: "IX_Aggregate2Alias_Slot",
                table: "Aggregate2Aliases",
                column: "Slot");

            migrationBuilder.CreateIndex(
                name: "IX_AggregateAlias_Slot",
                table: "AggregateAliases",
                column: "Slot");

            migrationBuilder.CreateIndex(
                name: "IX_Cement2Alias_Slot",
                table: "Cement2Aliases",
                column: "Slot");

            migrationBuilder.CreateIndex(
                name: "IX_CementAlias_Slot",
                table: "CementAliases",
                column: "Slot");

            migrationBuilder.CreateIndex(
                name: "IX_CementConsumption_ConsumedAt",
                table: "CementConsumptions",
                column: "ConsumedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CementConsumption_MixerId",
                table: "CementConsumptions",
                column: "MixerId");

            migrationBuilder.CreateIndex(
                name: "IX_CementConsumptions_SiloId",
                table: "CementConsumptions",
                column: "SiloId");

            migrationBuilder.CreateIndex(
                name: "IX_CementRefill_RefilledAt",
                table: "CementRefills",
                column: "RefilledAt");

            migrationBuilder.CreateIndex(
                name: "IX_CementRefills_SiloId",
                table: "CementRefills",
                column: "SiloId");

            migrationBuilder.CreateIndex(
                name: "IX_CementSilo_CementType",
                table: "CementSilos",
                column: "CementType");

            migrationBuilder.CreateIndex(
                name: "IX_CementSilo_SiloNumber",
                table: "CementSilos",
                column: "SiloNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CB2M_Batch_Slot",
                table: "ConcreteBatch2Admixtures",
                columns: new[] { "BatchId", "Slot" });

            migrationBuilder.CreateIndex(
                name: "IX_CB2A_Batch_Slot",
                table: "ConcreteBatch2Aggregates",
                columns: new[] { "BatchId", "Slot" });

            migrationBuilder.CreateIndex(
                name: "IX_ConcreteBatch2Cements_BatchId",
                table: "ConcreteBatch2Cements",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_CB2_OccurredAt",
                table: "ConcreteBatch2s",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_CB2_RecipeCode",
                table: "ConcreteBatch2s",
                column: "RecipeCode");

            migrationBuilder.CreateIndex(
                name: "IX_CB2_Status",
                table: "ConcreteBatch2s",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CBM_Batch_Slot",
                table: "ConcreteBatchAdmixtures",
                columns: new[] { "BatchId", "Slot" });

            migrationBuilder.CreateIndex(
                name: "IX_CBA_Batch_Slot",
                table: "ConcreteBatchAggregates",
                columns: new[] { "BatchId", "Slot" });

            migrationBuilder.CreateIndex(
                name: "IX_ConcreteBatchCements_BatchId",
                table: "ConcreteBatchCements",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ConcreteBatch_OccurredAt",
                table: "ConcreteBatches",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_ConcreteBatch_RecipeCode",
                table: "ConcreteBatches",
                column: "RecipeCode");

            migrationBuilder.CreateIndex(
                name: "IX_CBP_Batch_Slot",
                table: "ConcreteBatchPigments",
                columns: new[] { "BatchId", "Slot" });

            migrationBuilder.CreateIndex(
                name: "IX_MoldProductions_MoldId",
                table: "MoldProductions",
                column: "MoldId");

            migrationBuilder.CreateIndex(
                name: "IX_MoldProductions_ShiftRecordId",
                table: "MoldProductions",
                column: "ShiftRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_Molds_Code",
                table: "Molds",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Operator_CreatedAt",
                table: "Operators",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Operator_IsActive",
                table: "Operators",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Operator_Name",
                table: "Operators",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Pigment2Alias_Slot",
                table: "Pigment2Aliases",
                column: "Slot");

            migrationBuilder.CreateIndex(
                name: "IX_PigmentAlias_Slot",
                table: "PigmentAliases",
                column: "Slot");

            migrationBuilder.CreateIndex(
                name: "IX_PlcDataSnapshot_Operator",
                table: "PlcDataSnapshots",
                column: "Operator");

            migrationBuilder.CreateIndex(
                name: "IX_PlcDataSnapshot_Timestamp",
                table: "PlcDataSnapshots",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionLog_CreatedAt",
                table: "ProductionLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionLog_ProductionId",
                table: "ProductionLogs",
                column: "ProductionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionLog_StoneName",
                table: "ProductionLogs",
                column: "StoneName");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionNotes_ShiftId",
                table: "ProductionNotes",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Production_CreatedAt",
                table: "Productions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Production_IsActive",
                table: "Productions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Production_OperatorName",
                table: "Productions",
                column: "OperatorName");

            migrationBuilder.CreateIndex(
                name: "IX_Production_StartTime",
                table: "Productions",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftRecord_CreatedAt",
                table: "ShiftRecords",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftRecord_OperatorName",
                table: "ShiftRecords",
                column: "OperatorName");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftRecord_ShiftStartTime",
                table: "ShiftRecords",
                column: "ShiftStartTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Admixture2Aliases");

            migrationBuilder.DropTable(
                name: "AdmixtureAliases");

            migrationBuilder.DropTable(
                name: "Aggregate2Aliases");

            migrationBuilder.DropTable(
                name: "AggregateAliases");

            migrationBuilder.DropTable(
                name: "Cement2Aliases");

            migrationBuilder.DropTable(
                name: "CementAliases");

            migrationBuilder.DropTable(
                name: "CementConsumptions");

            migrationBuilder.DropTable(
                name: "CementRefills");

            migrationBuilder.DropTable(
                name: "ConcreteBatch2Admixtures");

            migrationBuilder.DropTable(
                name: "ConcreteBatch2Aggregates");

            migrationBuilder.DropTable(
                name: "ConcreteBatch2Cements");

            migrationBuilder.DropTable(
                name: "ConcreteBatchAdmixtures");

            migrationBuilder.DropTable(
                name: "ConcreteBatchAggregates");

            migrationBuilder.DropTable(
                name: "ConcreteBatchCements");

            migrationBuilder.DropTable(
                name: "ConcreteBatchPigments");

            migrationBuilder.DropTable(
                name: "MoldProductions");

            migrationBuilder.DropTable(
                name: "Operators");

            migrationBuilder.DropTable(
                name: "Pigment2Aliases");

            migrationBuilder.DropTable(
                name: "PigmentAliases");

            migrationBuilder.DropTable(
                name: "PlcDataSnapshots");

            migrationBuilder.DropTable(
                name: "ProductionLogs");

            migrationBuilder.DropTable(
                name: "ProductionNotes");

            migrationBuilder.DropTable(
                name: "CementSilos");

            migrationBuilder.DropTable(
                name: "ConcreteBatch2s");

            migrationBuilder.DropTable(
                name: "ConcreteBatches");

            migrationBuilder.DropTable(
                name: "Molds");

            migrationBuilder.DropTable(
                name: "ShiftRecords");

            migrationBuilder.DropTable(
                name: "Productions");

            migrationBuilder.DropTable(
                name: "Shifts");
        }
    }
}
