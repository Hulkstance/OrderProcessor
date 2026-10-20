using Microsoft.EntityFrameworkCore;
using MigrationService;
using MigrationService.Initializers;
using OrderService.Application.Common.Interfaces;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Persistence.Interceptors;
using OrderService.Infrastructure.Security;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<Worker>();

builder.Services.AddScoped<ApplicationDbContextInitializer>();
builder.Services.AddScoped<EntitySaveChangesInterceptor>();
builder.Services.AddScoped<ICurrentUserService, MigrationUserService>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services
    .AddOptions<SecurityOptions>()
    .BindConfiguration(SecurityOptions.SectionName)
    .Validate(options => !string.IsNullOrWhiteSpace(options.EncryptionKey),
        "Security:EncryptionKey must be configured")
    .ValidateOnStart();

builder.Services.AddSingleton<IEncryptionService, EncryptionService>();

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("Database");
    options.UseSqlServer(connectionString);
    options.AddInterceptors(sp.GetRequiredService<EntitySaveChangesInterceptor>());
});

var host = builder.Build();
host.Run();
