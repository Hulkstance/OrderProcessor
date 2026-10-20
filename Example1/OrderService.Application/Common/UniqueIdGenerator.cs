namespace OrderService.Application.Common;

public static class UniqueIdGenerator
{
    public static string Generate(int characterCount)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, characterCount)
            .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
    }
}