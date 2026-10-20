using System.Diagnostics.CodeAnalysis;

namespace OrderService.Domain.Orders;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum TimeInForce
{
    GTC,
    IOC,
    FOK
}
