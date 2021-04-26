# Examples

This directory contains a number of example applications that demonstrate the features of the o9d Observability libraries.

## Viewing Metrics in Grafana

You can view metrics from the example applications in Grafana. To launch Grafana/Prometheus, run:

```
docker-compose up --build
```

This will automatically provision the standard dashboards and assumes the ASP.NET Example application is running on https://localhost:5001.

## Generate Load

To generate load for the example ASP.NET application, install [K6](https://k6.io/) and run:

```
k6 run --duration 5m --vus 1 k6/aspnet-example.js
```

You can adjust the duration and number of virtual users accordingly.
