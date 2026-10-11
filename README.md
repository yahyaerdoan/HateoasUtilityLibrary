# Hateoas

HAL hypermedia ("_links") for ASP.NET Core Minimal APIs, applied from one place instead of per endpoint.

| Package | Contents |
| --- | --- |
| `Hateoas` | `Link`, `LinkCollection` (HAL "_links" with CURIEs), `LinkRelations` (IANA), optional `LinkedResponse` / `LinkedRecord` base types |
| `Hateoas.AspNetCore` | `LinkProvider<T>`, `HateoasLinkBuilder`, the endpoint filter, `AddHateoas()`, `WithHateoas()` / `WithCreatedLocation()` / `WithoutHateoas()` |

## Setup

```csharp
builder.Services.AddHateoas(options => options.CurieName = "oc")
    .AddLinkProvidersFromAssembly(typeof(Program).Assembly);

app.MapGroup(string.Empty).WithHateoas();   // every endpoint mapped under the group
```

## Rules

1. **A resource type gets its links in exactly one place**: a `LinkProvider<T>`. The filter finds every resource that has a
   provider in a successful response body (single item, list, page, envelope, nested) and asks its provider. Endpoints never
   build links.
2. **Response DTOs need no base type** (2.1). Any class or record with a provider gets a `"_links"` member, written last; the
   links live in a per-request store, so one instance shared between requests never shows another caller's links and the
   DTO can stay in a layer that knows nothing about HTTP. `_links` is left out of the JSON until a link is added, so a caller
   with nothing to do gets no empty object. Deriving from `LinkedResponse` / `LinkedRecord` (`ILinkedResource`) still works and
   keeps the links on the object itself. The member is added to the JSON options of Minimal APIs and MVC by `AddHateoas()`.
3. **Links describe what this caller can do now.** Use the `...If` overloads with the resource's state (only an unpaid order gets
   "pay") and the caller's permissions (`links.User`). Never emit an action the API would refuse.
4. **Relations**: IANA names (`LinkRelations.Self`, `Edit`, `Next`...) via `Add` / `Self`; anything application-specific via
   `AddCustom`, written as `{CurieName}:{rel}` with a `curies` entry pointing at the relation's documentation (`CurieHref`,
   default `/rels/{rel}`). Keys are therefore stable across environments.
5. **Hrefs come from route names** (`.WithName(...)`). An unresolvable route name or missing route value throws; it never produces
   a silent empty link.
6. **201 Created** sets `Location` when the endpoint declares `.WithCreatedLocation("GetXById")`. **Paged GET collections** get an
   RFC 8288 `Link` header (`prev` / `next` / `first` / `last`) from `PageInfoSelector` and the `index` / `size` query names.
7. Opt an endpoint out (file download, webhook) with `.WithoutHateoas()`.

```csharp
public sealed class OrderLinks : LinkProvider<OrderResponse>
{
    protected override void AddLinks(OrderResponse order, HateoasLinkBuilder links)
        => links
            .Self("GetOrderDetail", new { id = order.Id })
            .AddCustomIf(order.PaymentStatus == "Pending", "pay", "ResumeOrderPayment", HttpMethods.Get, new { id = order.Id });
}
```

## Tests

`test/Hateoas.Tests` (xUnit v3): `dotnet run --project test/Hateoas.Tests -p:GeneratePackageOnBuild=false` (`dotnet test` fails
with a Microsoft.Testing.Platform error on the .NET 10 SDK).
