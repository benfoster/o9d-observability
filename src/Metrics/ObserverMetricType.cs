namespace O9d.Metrics.AspNet
{
    /// <summary>
    /// Defines the type of observer metric type. 
    /// This allows both Prometheus Summaries and Histograms to be interchangeable.
    /// </summary>
    public enum ObserverMetricType
    {
        Histogram,
        Summary
    }
}
