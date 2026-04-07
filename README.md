# ErpEcommerceIntegration

A production-grade sample demonstrating an ERP → E-Commerce integration pipeline using .NET, MassTransit, Quartz.NET, and Polly.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         ERP System (Legacy)                         │
│                                                                     │
│   VW_PRODUCTS   VW_INVENTORY   VW_COLORS   VW_SIZES                │
│        │               │            │           │                   │
└────────┼───────────────┼────────────┼───────────┼───────────────────┘
         │               │            │           │
         ▼               ▼            ▼           ▼
┌─────────────────────────────────────────────────────────────────────┐
│                   IERPDatabaseService (Dapper mock)                 │
└─────────────────────────────┬───────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      ISyncOrchestrator                              │
│          SynchronizeProducts / Inventory / Colors / Sizes           │
└─────────────────────────────┬───────────────────────────────────────┘
                              │  publishes messages
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    IMessageBusService (MassTransit)                 │
│                                                                     │
│   ProductSyncMessage   InventorySyncMessage   Color/SizeSyncMessage │
└──────────────┬──────────────────────┬──────────────────────────────┘
               │  consumed by         │
               ▼                      ▼
┌──────────────────────────────────────────────────────────────────── ┐
│          MassTransit Consumers (in-memory or Azure Service Bus)     │
│                                                                     │
│   SyncProductsConsumer   SyncInventoryConsumer   SyncColors/Sizes   │
└─────────────────────────────┬───────────────────────────────────────┘
                              │  calls
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│               IECommerceEndPoint (mock HTTP client)                 │
│                                                                     │
│   PUT /api/v1/products/{sku}     PUT /api/v1/inventory/{sku}        │
│   POST /api/v1/attributes/colors  POST /api/v1/attributes/sizes     │
└─────────────────────────────────────────────────────────────────────┘

Scheduling layer (Quartz.NET):
  ┌─────────────────┐  triggers  ┌──────────────────────────────────┐
  │   Cron triggers │ ─────────► │  SyncProducts/Inventory/Colors/  │
  │  (configurable) │            │  SizesJob → ISyncOrchestrator    │
  └─────────────────┘            └──────────────────────────────────┘
```

---

## Prerequisites

- [.NET 8+ SDK](https://dotnet.microsoft.com/download) (tested with .NET 10)

No Azure subscription, database, or external services required to run locally.

---

## How to Run Locally

```bash
cd ErpEcommerceIntegration
dotnet build ErpEcommerceIntegration.sln
dotnet run --project SRC/ErpEcommerceIntegration.Workers
```

The worker starts, registers Quartz jobs, and waits for cron triggers to fire. All messaging runs in-memory via MassTransit's in-memory transport. Log output will show `[ERP]`, `[BUS]`, `[CONSUMER]`, and `[ECOMMERCE]` prefixed messages as each sync cycle runs.

---

## How to Switch to Azure Service Bus

1. Edit `SRC/ErpEcommerceIntegration.Workers/appsettings.json`:

```json
"MessageBus": {
  "UseInMemory": false,
  "ConnectionString": "Endpoint=sb://<your-namespace>.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=<your-key>"
}
```

2. The IoC configuration in `DependencyInjectionConfig.cs` automatically switches to `UsingAzureServiceBus` when `UseInMemory` is `false`, creating four named receive endpoints with retry and circuit-breaker policies.

---

## Project Structure

| Project | Role |
|---|---|
| `Domain` | Contracts only: models (`ProductSync`, `InventorySync`, etc.), message records, and service interfaces. No dependencies. |
| `Infra.ERPDatabase` | Mock implementation of `IERPDatabaseService` simulating Dapper queries against ERP SQL views. |
| `Infra.ECommerce` | Mock implementation of `IECommerceEndPoint` simulating HTTP calls to the e-commerce REST API, with ~5% transient error simulation. |
| `Infra.MessageBus` | `MessageBusService` (MassTransit `IBus` wrapper) and all four MassTransit consumers. |
| `Infra.Sync` | `SyncOrchestrator` — fetches data from ERP and publishes messages to the bus. |
| `Infra.IoC` | Composition root: registers all services, MassTransit (in-memory or Azure Service Bus), and Polly retry policy. |
| `Workers` | .NET Worker Service entry point: Quartz job definitions and `Program.cs`. |
| `IntegrationTests` *(TESTS/)* | xUnit + NSubstitute + MassTransit test harness — 51 integration tests covering all layers. |

---

## Cron Schedule

| Job | Default Cron | Frequency | Bypass Config Key |
|---|---|---|---|
| `SyncProductsJob` | `0 0 0/2 * * ?` | Every 2 hours | `Jobs:BypassSyncProducts` |
| `SyncInventoryJob` | `0 0/5 * * * ?` | Every 5 minutes | `Jobs:BypassSyncInventory` |
| `SyncColorsJob` | `0 0 0/6 * * ?` | Every 6 hours | `Jobs:BypassSyncColors` |
| `SyncSizesJob` | `0 0 0/6 * * ?` | Every 6 hours | `Jobs:BypassSyncSizes` |

All cron expressions are overridable in `appsettings.json` under `Jobs:ProductsCron`, `Jobs:InventoryCron`, `Jobs:ColorsCron`, `Jobs:SizesCron`.

To bypass a job without removing it, set the corresponding `Bypass*` key to `true` in configuration.

---

## Replacing the Mock ERP Data with Real Dapper Queries

Open `SRC/ErpEcommerceIntegration.Infra.ERPDatabase/ERPDatabaseService.cs`. Replace each `Task.Delay` + in-memory list access with a real Dapper query:

```csharp
// 1. Inject IDbConnection (or IDbConnectionFactory) via constructor
// 2. Replace Task.Delay with:
return await connection.QueryAsync<ProductSync>(
    "SELECT SKU, NAME, PRICE, ... FROM VW_PRODUCTS WHERE UPDATED_AT >= @since",
    new { since = updatedSince ?? DateTime.MinValue });
```

Register your `IDbConnection` (e.g., `SqlConnection` for SQL Server or `FbConnection` for Firebird) in `DependencyInjectionConfig.cs` with your connection string from `IConfiguration["ERP:ConnectionString"]`. The `ERPMockData` static class can then be deleted entirely.

---

## Replacing the Mock E-Commerce Client with Real HTTP Calls

Open `SRC/ErpEcommerceIntegration.Infra.ECommerce/ECommerceEndPoint.cs`. The production replacement steps are:

1. Inject `IHttpClientFactory` (or a typed `HttpClient`) instead of the mock logger-only constructor.
2. In `DependencyInjectionConfig.cs`, replace `services.AddScoped<IECommerceEndPoint, ECommerceEndPoint>()` with:

```csharp
services.AddHttpClient<IECommerceEndPoint, ECommerceEndPoint>(client =>
{
    client.BaseAddress = new Uri(configuration["ECommerce:BaseUrl"]!);
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", configuration["ECommerce:ApiToken"]);
})
.AddPolicyHandler(GetRetryPolicy());
```

3. Implement `UpsertProductAsync` and friends using `await _httpClient.PutAsJsonAsync(...)` and map domain models to the platform-specific DTO shapes.
4. Remove `SimulateHttpCallAsync` — the Polly policy registered on the `HttpClient` handles retries automatically.
