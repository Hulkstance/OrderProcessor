using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace OrderService.Infrastructure.Security;

internal sealed class EncryptedConverter(
    IEncryptionService encryptionService,
    ConverterMappingHints? mappingHints = null)
    : ValueConverter<string, string>(
        v => encryptionService.Encrypt(v),
        v => encryptionService.Decrypt(v),
        mappingHints);