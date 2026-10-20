using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "import");

            migrationBuilder.CreateTable(
                name: "ExchangeAssets",
                schema: "import",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(71)", maxLength: 71, nullable: false),
                    Exchange = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Asset = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AssetType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    Volume = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    ChangePercentage = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: true),
                    TickerType = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Day24H"),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeSymbols",
                schema: "import",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(92)", maxLength: 92, nullable: false),
                    Exchange = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    BaseAsset = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    QuoteAsset = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BaseAssetExchangeId = table.Column<string>(type: "nvarchar(71)", maxLength: 71, nullable: false),
                    QuoteAssetExchangeId = table.Column<string>(type: "nvarchar(71)", maxLength: 71, nullable: false),
                    BaseAssetType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuoteAssetType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MinTradeQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MinNotionalValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    QuantityStep = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PriceStep = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    QuantityDecimals = table.Column<int>(type: "int", nullable: true),
                    PriceDecimals = table.Column<int>(type: "int", nullable: true),
                    PriceSignificantFigures = table.Column<int>(type: "int", nullable: true),
                    Enabled = table.Column<bool>(type: "bit", nullable: true),
                    LastPrice = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    HighPrice = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    LowPrice = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    Volume = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    QuoteVolume = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    UsdVolume = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: true),
                    ChangePercentage = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: true),
                    TickerType = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Day24H"),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeSymbols", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FiatPrices",
                schema: "import",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiatPrices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Exchange = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InternalId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1000, 1"),
                    ApiKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApiSecret = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Passphrase = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExchangeOrderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ClientOrderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    MessageType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RequestPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponsePayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExchangeLogs_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exchange = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SymbolId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Side = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OrderType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TimeInForce = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: true),
                    State = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExecutedQuantity = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    AveragePrice = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: true),
                    ExternalId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InternalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderFills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FillPrice = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    FillQuantity = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    FeeAmount = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: true),
                    FeeAsset = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FilledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderFills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderFills_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_UserId_Exchange",
                table: "Accounts",
                columns: new[] { "UserId", "Exchange" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeAssets_Exchange",
                schema: "import",
                table: "ExchangeAssets",
                column: "Exchange");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeLogs_AccountId",
                table: "ExchangeLogs",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeLogs_AccountId_CreatedAt",
                table: "ExchangeLogs",
                columns: new[] { "AccountId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeLogs_AccountId_MessageType",
                table: "ExchangeLogs",
                columns: new[] { "AccountId", "MessageType" });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeLogs_ClientOrderId",
                table: "ExchangeLogs",
                column: "ClientOrderId",
                filter: "[ClientOrderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeLogs_ExchangeOrderId",
                table: "ExchangeLogs",
                column: "ExchangeOrderId",
                filter: "[ExchangeOrderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeSymbols_BaseAssetExchangeId",
                schema: "import",
                table: "ExchangeSymbols",
                column: "BaseAssetExchangeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeSymbols_Exchange",
                schema: "import",
                table: "ExchangeSymbols",
                column: "Exchange");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeSymbols_Exchange_Enabled",
                schema: "import",
                table: "ExchangeSymbols",
                columns: new[] { "Exchange", "Enabled" });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeSymbols_QuoteAssetExchangeId",
                schema: "import",
                table: "ExchangeSymbols",
                column: "QuoteAssetExchangeId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderFills_ExternalId",
                table: "OrderFills",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderFills_OrderId",
                table: "OrderFills",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_AccountId",
                table: "Orders",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ExternalId",
                table: "Orders",
                column: "ExternalId",
                unique: true,
                filter: "[ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_InternalId",
                table: "Orders",
                column: "InternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ExternalId",
                table: "Users",
                column: "ExternalId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExchangeAssets",
                schema: "import");

            migrationBuilder.DropTable(
                name: "ExchangeLogs");

            migrationBuilder.DropTable(
                name: "ExchangeSymbols",
                schema: "import");

            migrationBuilder.DropTable(
                name: "FiatPrices",
                schema: "import");

            migrationBuilder.DropTable(
                name: "OrderFills");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
