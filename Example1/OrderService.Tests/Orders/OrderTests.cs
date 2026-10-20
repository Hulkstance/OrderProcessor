using FluentAssertions;
using OrderService.Domain.Orders;

namespace OrderService.Tests.Orders;

public class OrderTests
{
    private static Order CreateMarketOrder(decimal quantity = 10m) =>
        Order.Create(
            accountId: Guid.NewGuid(),
            exchange: "Binance",
            symbolId: "BTCUSDT",
            side: OrderSide.Buy,
            orderType: OrderType.Market,
            internalId: "int-001",
            quantity: quantity);

    private static Order CreateLimitOrder(
        decimal quantity = 10m,
        decimal price = 50_000m,
        TimeInForce tif = TimeInForce.GTC) =>
        Order.Create(
            accountId: Guid.NewGuid(),
            exchange: "Binance",
            symbolId: "BTCUSDT",
            side: OrderSide.Sell,
            orderType: OrderType.Limit,
            internalId: "int-002",
            quantity: quantity,
            price: price,
            timeInForce: tif);

    // =========================================================================
    // Create — factory validation
    // =========================================================================

    [Fact]
    public void Create_MarketOrder_SetsCorrectDefaults()
    {
        var order = CreateMarketOrder();

        order.Id.Should().NotBeEmpty();
        order.OrderType.Should().Be(OrderType.Market);
        order.Price.Should().BeNull();
        order.TimeInForce.Should().BeNull();
        order.State.Value.Should().Be(OrderStatus.Created);
        order.ExternalId.Should().BeNull();
        order.ExecutedQuantity.Should().Be(0);
        order.AveragePrice.Should().BeNull();
    }

    [Fact]
    public void Create_LimitOrder_SetsCorrectDefaults()
    {
        var order = CreateLimitOrder();

        order.OrderType.Should().Be(OrderType.Limit);
        order.Price.Should().Be(50_000m);
        order.TimeInForce.Should().Be(TimeInForce.GTC);
        order.State.Value.Should().Be(OrderStatus.Created);
    }

    [Fact]
    public void Create_LimitOrder_WithoutPrice_Throws()
    {
        var act = () => Order.Create(
            Guid.NewGuid(), "Binance", "BTCUSDT",
            OrderSide.Buy, OrderType.Limit, "int-1", 10m,
            price: null, timeInForce: TimeInForce.GTC);

        act.Should().Throw<ArgumentException>().WithParameterName("price");
    }

    [Fact]
    public void Create_LimitOrder_WithoutTimeInForce_Throws()
    {
        var act = () => Order.Create(
            Guid.NewGuid(), "Binance", "BTCUSDT",
            OrderSide.Buy, OrderType.Limit, "int-1", 10m,
            price: 100m, timeInForce: null);

        act.Should().Throw<ArgumentException>().WithParameterName("timeInForce");
    }

    [Fact]
    public void Create_MarketOrder_WithPrice_Throws()
    {
        var act = () => Order.Create(
            Guid.NewGuid(), "Binance", "BTCUSDT",
            OrderSide.Buy, OrderType.Market, "int-1", 10m,
            price: 100m);

        act.Should().Throw<ArgumentException>().WithParameterName("price");
    }

    [Fact]
    public void Create_MarketOrder_WithTimeInForce_Throws()
    {
        var act = () => Order.Create(
            Guid.NewGuid(), "Binance", "BTCUSDT",
            OrderSide.Buy, OrderType.Market, "int-1", 10m,
            timeInForce: TimeInForce.GTC);

        act.Should().Throw<ArgumentException>().WithParameterName("timeInForce");
    }

    [Fact]
    public void Create_RaisesOrderCreatedDomainEvent()
    {
        var order = CreateMarketOrder();

        var events = order.PopDomainEvents();

        events.Should().ContainSingle()
            .Which.Should().BeOfType<OrderCreatedDomainEvent>()
            .Which.OrderId.Should().Be(order.Id);
    }

    [Fact]
    public void Create_WithZeroQuantity_Throws()
    {
        var act = () => CreateMarketOrder(quantity: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithNegativeQuantity_Throws()
    {
        var act = () => CreateMarketOrder(quantity: -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // =========================================================================
    // Internal lifecycle: StartSubmitting → ConfirmSubmitted
    // =========================================================================

    [Fact]
    public void StartSubmitting_FromCreated_TransitionsToSubmitting()
    {
        var order = CreateMarketOrder();

        order.StartSubmitting();

        order.State.Value.Should().Be(OrderStatus.Submitting);
    }

    [Fact]
    public void ConfirmSubmitted_SetsExternalIdAndTransitions()
    {
        var order = CreateMarketOrder();
        order.StartSubmitting();

        order.ConfirmSubmitted("ext-123");

        order.State.Value.Should().Be(OrderStatus.Submitted);
        order.ExternalId.Should().Be("ext-123");
    }

    // =========================================================================
    // Accept
    // =========================================================================

    [Fact]
    public void Accept_SetsExternalIdWhenMissing()
    {
        var order = CreateMarketOrder();

        order.Accept("ext-abc");

        order.State.Value.Should().Be(OrderStatus.Accepted);
        order.ExternalId.Should().Be("ext-abc");
    }

    [Fact]
    public void Accept_DoesNotOverwriteExistingExternalId()
    {
        var order = CreateMarketOrder();
        order.StartSubmitting();
        order.ConfirmSubmitted("ext-original");

        order.Accept("ext-new");

        order.ExternalId.Should().Be("ext-original");
    }

    // =========================================================================
    // PartialFill
    // =========================================================================

    [Fact]
    public void PartialFill_UpdatesExecutedQuantityAndAveragePrice()
    {
        var order = CreateMarketOrder(quantity: 100m);
        order.Accept("ext-1");

        order.PartialFill("ext-1", executedQuantity: 30m, averagePrice: 49_500m);

        order.State.Value.Should().Be(OrderStatus.PartiallyFilled);
        order.ExecutedQuantity.Should().Be(30m);
        order.AveragePrice.Should().Be(49_500m);
    }

    [Fact]
    public void PartialFill_WithNullAveragePrice_DoesNotOverwrite()
    {
        var order = CreateMarketOrder(quantity: 100m);
        order.Accept("ext-1");
        order.PartialFill("ext-1", executedQuantity: 30m, averagePrice: 49_500m);

        order.PartialFill("ext-1", executedQuantity: 60m, averagePrice: null);

        order.AveragePrice.Should().Be(49_500m);
    }

    // =========================================================================
    // Fill
    // =========================================================================

    [Fact]
    public void Fill_TransitionsToFilled()
    {
        var order = CreateMarketOrder(quantity: 100m);
        order.Accept("ext-1");

        order.Fill("ext-1", executedQuantity: 100m, averagePrice: 50_000m);

        order.State.Value.Should().Be(OrderStatus.Filled);
        order.ExecutedQuantity.Should().Be(100m);
        order.AveragePrice.Should().Be(50_000m);
    }

    [Fact]
    public void Fill_FromCancelRequested_FillWins()
    {
        var order = CreateLimitOrder();
        order.Accept("ext-1");
        order.RequestCancel();

        order.Fill("ext-1", executedQuantity: 10m, averagePrice: 50_000m);

        order.State.Value.Should().Be(OrderStatus.Filled);
    }

    // =========================================================================
    // Cancel
    // =========================================================================

    [Fact]
    public void RequestCancel_FromAccepted_TransitionsToCancelRequested()
    {
        var order = CreateLimitOrder();
        order.Accept("ext-1");

        order.RequestCancel();

        order.State.Value.Should().Be(OrderStatus.CancelRequested);
    }

    [Fact]
    public void Cancel_FromCancelRequested_TransitionsToCancelled()
    {
        var order = CreateLimitOrder();
        order.Accept("ext-1");
        order.RequestCancel();

        order.Cancel();

        order.State.Value.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_FromAccepted_ExchangeInitiatedCancel()
    {
        var order = CreateLimitOrder();
        order.Accept("ext-1");

        order.Cancel();

        order.State.Value.Should().Be(OrderStatus.Cancelled);
    }

    // =========================================================================
    // Expire
    // =========================================================================

    [Fact]
    public void Expire_FromAccepted_TransitionsToExpired()
    {
        var order = CreateLimitOrder();
        order.Accept("ext-1");

        order.Expire();

        order.State.Value.Should().Be(OrderStatus.Expired);
    }

    [Fact]
    public void Expire_FromCancelRequested_TransitionsToExpired()
    {
        var order = CreateLimitOrder();
        order.Accept("ext-1");
        order.RequestCancel();

        order.Expire();

        order.State.Value.Should().Be(OrderStatus.Expired);
    }

    // =========================================================================
    // Reject
    // =========================================================================

    [Fact]
    public void Reject_FromCreated_TransitionsToRejected()
    {
        var order = CreateMarketOrder();

        order.Reject();

        order.State.Value.Should().Be(OrderStatus.Rejected);
    }

    [Fact]
    public void Reject_FromFilled_Throws()
    {
        var order = CreateMarketOrder();
        order.Accept("ext-1");
        order.Fill("ext-1", 10m, 50_000m);

        var act = () => order.Reject();

        act.Should().Throw<InvalidOperationException>();
    }

    // =========================================================================
    // Query helpers
    // =========================================================================

    [Fact]
    public void IsOnStatus_ReturnsTrueForCurrentStatus()
    {
        var order = CreateMarketOrder();

        order.IsOnStatus(OrderStatus.Created).Should().BeTrue();
        order.IsOnStatus(OrderStatus.Accepted).Should().BeFalse();
    }

    [Fact]
    public void IsOnAnyStatus_ReturnsTrueIfAnyMatch()
    {
        var order = CreateMarketOrder();

        order.IsOnAnyStatus(OrderStatus.Created, OrderStatus.Accepted).Should().BeTrue();
        order.IsOnAnyStatus(OrderStatus.Filled, OrderStatus.Cancelled).Should().BeFalse();
    }

    [Fact]
    public void IsTerminalState_ReturnsTrueForFilledOrder()
    {
        var order = CreateMarketOrder();
        order.Accept("ext-1");
        order.Fill("ext-1", 10m, 50_000m);

        order.IsTerminalState().Should().BeTrue();
    }

    [Fact]
    public void IsTerminalState_ReturnsFalseForActiveOrder()
    {
        var order = CreateMarketOrder();

        order.IsTerminalState().Should().BeFalse();
    }

    // =========================================================================
    // Pre-flight guards
    // =========================================================================

    [Theory]
    [InlineData(OrderStatus.Created)]
    public void CanBeAccepted_FromCreated_ReturnsTrue(OrderStatus _)
    {
        var order = CreateMarketOrder();

        order.CanBeAccepted().Should().BeTrue();
    }

    [Fact]
    public void CanBeAccepted_FromFilled_ReturnsFalse()
    {
        var order = CreateMarketOrder();
        order.Accept("ext-1");
        order.Fill("ext-1", 10m, 50_000m);

        order.CanBeAccepted().Should().BeFalse();
    }

    [Fact]
    public void CanBeFilled_ReturnsFalseForTerminalState()
    {
        var order = CreateMarketOrder();
        order.Accept("ext-1");
        order.Fill("ext-1", 10m, 50_000m);

        order.CanBeFilled().Should().BeFalse();
    }

    [Fact]
    public void CanBeFilled_ReturnsTrueForActiveOrder()
    {
        var order = CreateMarketOrder();
        order.Accept("ext-1");

        order.CanBeFilled().Should().BeTrue();
    }

    [Fact]
    public void CanBeCancelled_FromAccepted_ReturnsTrue()
    {
        var order = CreateLimitOrder();
        order.Accept("ext-1");

        order.CanBeCancelled().Should().BeTrue();
    }

    [Fact]
    public void CanBeCancelled_FromCreated_ReturnsFalse()
    {
        var order = CreateMarketOrder();

        order.CanBeCancelled().Should().BeFalse();
    }

    // =========================================================================
    // Fills collection
    // =========================================================================

    [Fact]
    public void AddFill_AddsToFillsCollection()
    {
        var order = CreateMarketOrder();
        var fill = OrderFill.Create(order.Id, "trade-1", 50_000m, 5m, DateTime.UtcNow);

        order.AddFill(fill);

        order.Fills.Should().ContainSingle();
    }

    [Fact]
    public void GetFilledQuantity_SumsFillQuantities()
    {
        var order = CreateMarketOrder(quantity: 100m);
        order.AddFill(OrderFill.Create(order.Id, "t1", 50_000m, 30m, DateTime.UtcNow));
        order.AddFill(OrderFill.Create(order.Id, "t2", 50_100m, 20m, DateTime.UtcNow));

        order.GetFilledQuantity().Should().Be(50m);
    }

    [Fact]
    public void GetRemainingQuantity_ReturnsQuantityMinusFilled()
    {
        var order = CreateMarketOrder(quantity: 100m);
        order.AddFill(OrderFill.Create(order.Id, "t1", 50_000m, 30m, DateTime.UtcNow));

        order.GetRemainingQuantity().Should().Be(70m);
    }

    // =========================================================================
    // Full lifecycle scenarios
    // =========================================================================

    [Fact]
    public void FullLifecycle_MarketOrder_HappyPath()
    {
        var order = CreateMarketOrder(quantity: 10m);

        order.StartSubmitting();
        order.ConfirmSubmitted("ext-1");
        order.Accept("ext-1");
        order.PartialFill("ext-1", executedQuantity: 5m, averagePrice: 49_000m);
        order.Fill("ext-1", executedQuantity: 10m, averagePrice: 49_500m);

        order.State.Value.Should().Be(OrderStatus.Filled);
        order.ExecutedQuantity.Should().Be(10m);
        order.AveragePrice.Should().Be(49_500m);
        order.IsTerminalState().Should().BeTrue();
    }

    [Fact]
    public void FullLifecycle_LimitOrder_CancelledAfterPartialFill()
    {
        var order = CreateLimitOrder(quantity: 100m);

        order.StartSubmitting();
        order.ConfirmSubmitted("ext-1");
        order.Accept("ext-1");
        order.PartialFill("ext-1", executedQuantity: 40m, averagePrice: 50_000m);
        order.RequestCancel();
        order.Cancel();

        order.State.Value.Should().Be(OrderStatus.Cancelled);
        order.ExecutedQuantity.Should().Be(40m);
        order.IsTerminalState().Should().BeTrue();
    }

    [Fact]
    public void FullLifecycle_RejectedAtSubmission()
    {
        var order = CreateMarketOrder();

        order.StartSubmitting();
        order.Reject();

        order.State.Value.Should().Be(OrderStatus.Rejected);
        order.IsTerminalState().Should().BeTrue();
    }

    [Fact]
    public void RaceCondition_FillArrivesBeforeAccept()
    {
        var order = CreateMarketOrder(quantity: 10m);

        order.Fill("ext-1", executedQuantity: 10m, averagePrice: 50_000m);

        order.State.Value.Should().Be(OrderStatus.Filled);
        order.ExternalId.Should().Be("ext-1");
    }

    [Fact]
    public void RaceCondition_PartialFillArrivesBeforeAccept()
    {
        var order = CreateMarketOrder(quantity: 10m);

        order.PartialFill("ext-1", executedQuantity: 5m, averagePrice: 49_000m);

        order.State.Value.Should().Be(OrderStatus.PartiallyFilled);
        order.ExternalId.Should().Be("ext-1");
    }
}
