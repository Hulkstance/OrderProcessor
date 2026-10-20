# OrderService Migrations

**Add Migration:**
```bash
dotnet ef migrations add InitialCreate --project Example1/OrderService.Infrastructure --startup-project Example1/OrderService.Api --context AppDbContext --output-dir Persistence/Migrations
```

**Apply Migration:**
```bash
dotnet ef database update --project Example1/OrderService.Infrastructure --startup-project Example1/OrderService.Api --context AppDbContext
```
