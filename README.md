<img alt="Observability Icon" src="src/shared/icon.png" width="64px" />

# Observability

[![NuGet](https://img.shields.io/nuget/v/O9d.Observability.svg)](https://www.nuget.org/packages/O9d.Observability) 
[![NuGet](https://img.shields.io/nuget/dt/O9d.Observability.svg)](https://www.nuget.org/packages/O9d.Observability)
[![License](https://img.shields.io/:license-mit-blue.svg)](https://benfoster.mit-license.org/)

![Build](https://github.com/benfoster/o9d-observability/workflows/Build/badge.svg)
[![Coverage Status](https://coveralls.io/repos/github/benfoster/o9d-observability/badge.svg?branch=main)](https://coveralls.io/github/benfoster/o9d-observability?branch=main)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=benfoster_o9d-observability&metric=alert_status)](https://sonarcloud.io/dashboard?id=benfoster_o9d-observability)

O[pinionate]d Observability Extensions for .NET.

## Quick Start

In order to make use of the Observability libraries you need to initialize the Observability Host. Currently, only ASP.NET Core hosts are supported.

Add the O9d.Observability.Hosting.AspNet package from [NuGet](https://www.nuget.org/packages/O9d.Observability.Hosting.AspNet)

```
dotnet add package O9d.Observability.Hosting.AspNet
```

You can then update your `Startup.cs` file to initialize the host:

```c#
services.AddObservability(builder =>
{

});
```

Internally this will initialize an ASP.NET Core Hosted Service that keeps track of all registered instrumentation components.

To start instrumenting your application you need to add one of the relevant instrumentation packages (discussed in more detail below), for example, to add ASP.NET Core metrics (using Prometheus), add the [09d.AspNet package](O9d.Metrics.AspNet):

```
dotnet add package O9d.Metrics.AspNet
```

Then update your Observability startup code:

```c#
services.AddObservability(builder =>
{
    builder.AddAspNetMetrics(options => {});
});
```

One of the design goals of this library is that it should be as unobtrusive as possible, leveraging the built-in diagnostic and activity components of the Core CLR so that adding instrumentation doesn't interfere with other application code or middleware.


## Instrumentation Libraries

### ASP.NET Core Metrics

The [09d.Metrics.AspNet package](O9d.Metrics.AspNet) adds specific Prometheus metrics that we have found to be the most useful when operationalising HTTP services in production.

After installing the Observability Hosting and ASP.NET Metrics Packages to your application, update your `Startup.cs` as follows:

```c#
services.AddObservability(builder =>
{
    builder.AddAspNetMetrics(options => {});
});
```

By default the library adds the following Prometheus metrics:

**`http_server_request_duration_seconds`**

A histogram (default) or summary Tracks the duration in seconds that HTTP requests take to process. 

Labels:

| Name | Description  |  Example  |
|---|---|---|
| `operation`  | A descriptor for the operation and endpoint that was requested  | `get_customers`  |
| `status_code` | The status code returned by your service  | `200`  |

**`http_server_requests_in_progress`**

A gauge that tracks the number of requests in progress. 

Labels:

| Name | Description  |  Example  |
|---|---|---|
| `operation`  | A descriptor for the operation and endpoint that was requested  | `get_customers`  |

**`http_server_errors_total`**

A counter that tracks the number of HTTP requests resulting in an error.

Labels:

| Name | Description  |  Example  |
|---|---|---|
| `operation`  | A descriptor for the operation and endpoint that was requested  | `get_customers`  |
| `sli_error_type` | The service level indicator error type | `external_dependency` |
| `sli_dependency` | For dependency error types, the name of the causing dependency | `skynet` |

#### Calculating Service Availability

With these metrics we can easily calculate both internal and external service availability. To calculate our client facing availability:

```
Availability = successful_requests / (total_requests - client_failures)
```

For example:

```
Given 100 requests
of which
    70 returned HTTP 200
    10 returned HTTP 500 (Server Error)
    20 returned HTTP 422 (Invalid Client Request)

Availability = (100 - 30) / (100 - 20)
= 87.5%
```

To calculate this in Prometheus/Grafana:

```
(sum(rate(http_server_request_duration_seconds_count[10m])) - sum(rate(http_request_error_total[10m]))) / 
( 
    sum(rate(http_server_request_duration_seconds_count[10m])) - 
    sum(rate(http_request_error_total{sli_error_type="InvalidRequestError"}[10m]) OR on() vector(0))
)
```

#### Resolving the Operation

The default Prometheus libraries for ASP.NET are quite verbose and can result in a large number of series or high-cardinality labels.

By design this library only tracks _genuine_ endpoints of your application since generally, metrics about non-existent endpoints offer little value (e.g. bots trying to hit `/phpmyadmin`). Note that a metric for unmatched paths is something we're thinking about.

By default the library uses the following approach to resolve the operation name

1. The name of the route if set on your controller action, for example:
    ```
    [HttpGet("status/{code:int}", Name = "get_status")]
    ```
2. Or, use a combination of the HTTP verb and route template e.g. `PUT /customers/{id}`

In general we recommend explicitly naming your route to avoid your metrics changing if your URI structure is updated.

#### Tracking Errors

By default the following status codes are determined to be an error:

- `400 - 499` - Error Type: Invalid Request
- `>500` - Error Type: Internal

What we can't track automatically are errors that are the result of internal or external dependencies. For these you have two options:

1. Set the SLI error using `HttpContext.SetSliError()`, for example:

    ```c#
    HttpContext.SetSliError(ErrorType.ExternalDependency, "skynet");
    ```
2. Throw an `SliException` (or any derived type), for example:
    ```c#
    throw new SliException(ErrorType.ExternalDependency, "skynet");
    ```

## Extending O9d.Observability

This project was heavily inspired by the [Open Telemetry Libraries for .NET](https://github.com/open-telemetry/opentelemetry-dotnet).

We wanted to make it easy to plug in additional instrumentation without a lot of ceremony. Suppose you want to add instrument operations in the DazzleDB .NET client. Fortunately the client already emits events to a Diagnostic Source and the Observability library makes it easy to tap into them. 

### Create an observer

Create a class that implements `IObserver<KeyValuePair<string, object?>>` to receive Diagnostic Listener events:

```c#
internal class DazzleDbMetricsObserver : IObserver<KeyValuePair<string, object?>>
{
}
```

### Add the `O9d.Observability` package

```
dotnet add package O9d.Observability
```

### Create an extension for Observability Builder

```c#
public static class DazzleDbObservabilityBuilderExtensions
{
    public static IObservabilityBuilder AddDazzleDbMetrics(this IObservabilityBuilder builder)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));

        return builder.AddDiagnosticSource("DazzleDb", new DazzleDbMetricsObserver());
    }
}
```

The above code makes use of the `AddDiagnosticSource` extension to handle the boilerplate `DiagnosticSource` subscription logic and ensure subscribers are tracked.

### Package your library and update your applications

```c#
services.AddObservability(builder =>
{
    builder.AddDazzleDbMetrics();
});
```
