# Hateoas

HAL hypermedia ("_links") for ASP.NET Core Minimal APIs, applied from one place instead of per endpoint.

| Package | Contents |
| --- | --- |
| `Hateoas` | `Link`, `LinkCollection` (HAL "_links" with CURIEs), `LinkRelations` (IANA), `LinkedResponse` / `LinkedRecord` base types |
| `Hateoas.AspNetCore` | `LinkProvider<T>`, `HateoasLinkBuilder`, the endpoint filter, `AddHateoas()`, `WithHateoas()` / `WithCreatedLocation()` / `WithoutHateoas()` |

## Setup

```csharp
builder.Services.AddHateoas(options => options.CurieName = "oc")
    .AddLinkProvidersFromAssembly(typeof(Program).Assembly);

app.MapGroup(string.Empty).WithHateoas();   // every endpoint mapped under the group
```

## Rules

1. **A resource type gets its links in exactly one place**: a `LinkProvider<T>`. The filter finds every `ILinkedResource` in a
   successful response body (single item, list, page, envelope, nested) and asks its provider. Endpoints never build links.
2. **Response DTOs opt in** by deriving from `LinkedResponse` (classes) or `LinkedRecord` (records). `_links` is left out of the JSON
   until a link is added, so a caller with nothing to do gets no empty object.
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

`test/Hateoas.Tests` (xUnit v3): `dotnet run --project test/Hateoas.Tests`.
