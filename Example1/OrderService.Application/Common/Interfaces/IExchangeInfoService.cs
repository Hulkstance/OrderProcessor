namespace OrderService.Application.Common.Interfaces;

public record ExchangeStaticInfo(string[] Environments, string? LogoUrl, string? Url, string? Type);

public interface IExchangeInfoService
{
    string[] GetAllNames();
    ExchangeStaticInfo? GetInfo(string exchange);
}
