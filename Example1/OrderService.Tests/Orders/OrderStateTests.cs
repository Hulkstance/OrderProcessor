using FluentAssertions;
using OrderService.Domain.Orders;

namespace OrderService.Tests.Orders;

public class OrderStateTests
{
    // -------------------------------------------------------------------------
    // StartSubmitting
    // -------------------------------------------------------------------------

    [Fact]
    public void StartSubmitting_FromCreated_TransitionsToSubmitting()
    {
        var state = new OrderState(OrderStatus.Created);

        var result = state.StartSubmitting();

        result.Value.Should().Be(OrderStatus.Submitting);
    }

    [Theory]
    [InlineData(OrderStatus.Submitting)]
    [InlineData(OrderStatus.Submitted)]
    [InlineData(OrderStatus.Accepted)]
    [InlineData(OrderStatus.PartiallyFilled)]
    [InlineData(OrderStatus.Filled)]
    [InlineData(OrderStatus.CancelRequested)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Rejected)]
    [InlineData(OrderStatus.Expired)]
    public void StartSubmitting_FromInvalidStatus_Throws(OrderStatus status)
    {
        var state = new OrderState(status);

        var act = () => state.StartSubmitting();

        act.Should().Throw<InvalidOperationException>();
    }

    // -------------------------------------------------------------------------
    // ConfirmSubmitted
    // -------------------------------------------------------------------------

    [Fact]
    public void ConfirmSubmitted_FromSubmitting_TransitionsToSubmitted()
    {
        var state = new OrderState(OrderStatus.Submitting);

        var result = state.ConfirmSubmitted();

        result.Value.Should().Be(OrderStatus.Submitted);
    }

    [Theory]
    [InlineData(OrderStatus.Created)]
    [InlineData(OrderStatus.Submitted)]
    [InlineData(OrderStatus.Accepted)]
    [InlineData(OrderStatus.PartiallyFilled)]
    [InlineData(OrderStatus.Filled)]
    [InlineData(OrderStatus.CancelRequested)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Rejected)]
    [InlineData(OrderStatus.Expired)]
    public void ConfirmSubmitted_FromInvalidStatus_Throws(OrderStatus status)
    {
        var state = new OrderState(status);

        var act = () => state.ConfirmSubmitted();

        act.Should().Throw<InvalidOperationException>();
    }

    // -------------------------------------------------------------------------
    // Accept
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(OrderStatus.Created)]
    [InlineData(OrderStatus.Submitting)]
    [InlineData(OrderStatus.Submitted)]
    public void Accept_FromValidStatus_TransitionsToAccepted(OrderStatus status)
    {
        var state = new OrderState(status);

        var result = state.Accept();

        result.Value.Should().Be(OrderStatus.Accepted);
    }

    [Theory]
    [InlineData(OrderStatus.Accepted)]
    [InlineData(OrderStatus.PartiallyFilled)]
    [InlineData(OrderStatus.Filled)]
    [InlineData(OrderStatus.CancelRequested)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Rejected)]
    [InlineData(OrderStatus.Expired)]
    public void Accept_FromInvalidStatus_Throws(OrderStatus status)
    {
        var state = new OrderState(status);

        var act = () => state.Accept();

        act.Should().Throw<InvalidOperationException>();
    }

    // -------------------------------------------------------------------------
    // PartialFill
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(OrderStatus.Created)]
    [InlineData(OrderStatus.Submitting)]
    [InlineData(OrderStatus.Submitted)]
    [InlineData(OrderStatus.Accepted)]
    [InlineData(OrderStatus.PartiallyFilled)]
    public void PartialFill_FromValidStatus_TransitionsToPartiallyFilled(OrderStatus status)
    {
        var state = new OrderState(status);

        var result = state.PartialFill();

        result.Value.Should().Be(OrderStatus.PartiallyFilled);
    }

    [Theory]
    [InlineData(OrderStatus.Filled)]
    [InlineData(OrderStatus.CancelRequested)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Rejected)]
    [InlineData(OrderStatus.Expired)]
    public void PartialFill_FromInvalidStatus_Throws(OrderStatus status)
    {
        var state = new OrderState(status);

        var act = () => state.PartialFill();

        act.Should().Throw<InvalidOperationException>();
    }

    // -------------------------------------------------------------------------
    // Fill
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(OrderStatus.Created)]
    [InlineData(OrderStatus.Submitting)]
    [InlineData(OrderStatus.Submitted)]
    [InlineData(OrderStatus.Accepted)]
    [InlineData(OrderStatus.PartiallyFilled)]
    [InlineData(OrderStatus.CancelRequested)]
    public void Fill_FromValidStatus_TransitionsToFilled(OrderStatus status)
    {
        var state = new OrderState(status);

        var result = state.Fill();

        result.Value.Should().Be(OrderStatus.Filled);
    }

    [Theory]
    [InlineData(OrderStatus.Filled)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Rejected)]
    [InlineData(OrderStatus.Expired)]
    public void Fill_FromInvalidStatus_Throws(OrderStatus status)
    {
        var state = new OrderState(status);

        var act = () => state.Fill();

        act.Should().Throw<InvalidOperationException>();
    }

    // -------------------------------------------------------------------------
    // RequestCancel
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(OrderStatus.Accepted)]
    [InlineData(OrderStatus.PartiallyFilled)]
    [InlineData(OrderStatus.Submitted)]
    public void RequestCancel_FromValidStatus_TransitionsToCancelRequested(OrderStatus status)
    {
        var state = new OrderState(status);

        var result = state.RequestCancel();

        result.Value.Should().Be(OrderStatus.CancelRequested);
    }

    [Theory]
    [InlineData(OrderStatus.Created)]
    [InlineData(OrderStatus.Submitting)]
    [InlineData(OrderStatus.CancelRequested)]
    [InlineData(OrderStatus.Filled)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Rejected)]
    [InlineData(OrderStatus.Expired)]
    public void RequestCancel_FromInvalidStatus_Throws(OrderStatus status)
    {
        var state = new OrderState(status);

        var act = () => state.RequestCancel();

        act.Should().Throw<InvalidOperationException>();
    }

    // -------------------------------------------------------------------------
    // Cancel
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(OrderStatus.CancelRequested)]
    [InlineData(OrderStatus.Accepted)]
    [InlineData(OrderStatus.PartiallyFilled)]
    public void Cancel_FromValidStatus_TransitionsToCancelled(OrderStatus status)
    {
        var state = new OrderState(status);

        var result = state.Cancel();

        result.Value.Should().Be(OrderStatus.Cancelled);
    }

    [Theory]
    [InlineData(OrderStatus.Created)]
    [InlineData(OrderStatus.Submitting)]
    [InlineData(OrderStatus.Submitted)]
    [InlineData(OrderStatus.Filled)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Rejected)]
    [InlineData(OrderStatus.Expired)]
    public void Cancel_FromInvalidStatus_Throws(OrderStatus status)
    {
        var state = new OrderState(status);

        var act = () => state.Cancel();

        act.Should().Throw<InvalidOperationException>();
    }

    // -------------------------------------------------------------------------
    // Expire
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(OrderStatus.Accepted)]
    [InlineData(OrderStatus.PartiallyFilled)]
    [InlineData(OrderStatus.CancelRequested)]
    public void Expire_FromValidStatus_TransitionsToExpired(OrderStatus status)
    {
        var state = new OrderState(status);

        var result = state.Expire();

        result.Value.Should().Be(OrderStatus.Expired);
    }

    [Theory]
    [InlineData(OrderStatus.Created)]
    [InlineData(OrderStatus.Submitting)]
    [InlineData(OrderStatus.Submitted)]
    [InlineData(OrderStatus.Filled)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Rejected)]
    [InlineData(OrderStatus.Expired)]
    public void Expire_FromInvalidStatus_Throws(OrderStatus status)
    {
        var state = new OrderState(status);

        var act = () => state.Expire();

        act.Should().Throw<InvalidOperationException>();
    }

    // -------------------------------------------------------------------------
    // Reject
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(OrderStatus.Created)]
    [InlineData(OrderStatus.Submitting)]
    [InlineData(OrderStatus.Submitted)]
    [InlineData(OrderStatus.Accepted)]
    [InlineData(OrderStatus.PartiallyFilled)]
    [InlineData(OrderStatus.CancelRequested)]
    public void Reject_FromNonTerminalStatus_TransitionsToRejected(OrderStatus status)
    {
        var state = new OrderState(status);

        var result = state.Reject();

        result.Value.Should().Be(OrderStatus.Rejected);
    }

    [Theory]
    [InlineData(OrderStatus.Filled)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Rejected)]
    [InlineData(OrderStatus.Expired)]
    public void Reject_FromTerminalStatus_Throws(OrderStatus status)
    {
        var state = new OrderState(status);

        var act = () => state.Reject();

        act.Should().Throw<InvalidOperationException>();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(OrderStatus.Filled)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Rejected)]
    [InlineData(OrderStatus.Expired)]
    public void IsTerminal_ForTerminalStatuses_ReturnsTrue(OrderStatus status)
    {
        var state = new OrderState(status);

        state.IsTerminal().Should().BeTrue();
    }

    [Theory]
    [InlineData(OrderStatus.Created)]
    [InlineData(OrderStatus.Submitting)]
    [InlineData(OrderStatus.Submitted)]
    [InlineData(OrderStatus.Accepted)]
    [InlineData(OrderStatus.PartiallyFilled)]
    [InlineData(OrderStatus.CancelRequested)]
    public void IsTerminal_ForNonTerminalStatuses_ReturnsFalse(OrderStatus status)
    {
        var state = new OrderState(status);

        state.IsTerminal().Should().BeFalse();
    }

    [Fact]
    public void Is_ReturnsTrueForMatchingStatus()
    {
        var state = new OrderState(OrderStatus.Accepted);

        state.Is(OrderStatus.Accepted).Should().BeTrue();
        state.Is(OrderStatus.Created).Should().BeFalse();
    }

    [Fact]
    public void ToString_ReturnsStatusName()
    {
        var state = new OrderState(OrderStatus.PartiallyFilled);

        state.ToString().Should().Be("PartiallyFilled");
    }

    // -------------------------------------------------------------------------
    // Immutability — transitions return new instances
    // -------------------------------------------------------------------------

    [Fact]
    public void Transitions_ReturnNewInstance_OriginalUnchanged()
    {
        var original = new OrderState(OrderStatus.Created);

        var next = original.StartSubmitting();

        original.Value.Should().Be(OrderStatus.Created);
        next.Value.Should().Be(OrderStatus.Submitting);
        next.Should().NotBeSameAs(original);
    }
}
