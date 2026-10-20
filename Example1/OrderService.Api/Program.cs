using Hangfire;
using OrderService.Api;
using OrderService.Api.Extensions;
using OrderService.Application;
using OrderService.Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddCustomProblemDetails();

builder.Services.AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddWebApi(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
        options.RoutePrefix = "";
    });
}

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.UseEventualConsistencyMiddleware();
app.UseHangfireDashboard();

app.MapControllers();

app.UseExceptionHandler();

app.Run();
