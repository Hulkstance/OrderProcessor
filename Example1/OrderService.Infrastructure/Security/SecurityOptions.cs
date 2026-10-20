namespace OrderService.Infrastructure.Security;

public class SecurityOptions
{
    public const string SectionName = "Security";

    public required string EncryptionKey { get; init; }
}
