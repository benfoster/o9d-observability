using System.Diagnostics;
using Xunit;

namespace O9d.Observability.Tests
{
    public class PropertyFetcherTest
    {
        [Fact]
        public void FetchValidProperty()
        {
            var activity = new Activity("test");
            var fetch = new PropertyFetcher<string>("DisplayName");
            Assert.True(fetch.TryFetch(activity, out string? result));
            Assert.Equal(activity.DisplayName, result);
        }

        [Fact]
        public void FetchInvalidProperty()
        {
            var activity = new Activity("test");
            var fetch = new PropertyFetcher<string>("DisplayName2");
            Assert.False(fetch.TryFetch(activity, out string? result));

            var fetchInt = new PropertyFetcher<int>("DisplayName2");
            Assert.False(fetchInt.TryFetch(activity, out int resultInt));

            Assert.Equal(default, result);
            Assert.Equal(default, resultInt);
        }

        [Fact]
        public void FetchNullProperty()
        {
            var fetch = new PropertyFetcher<string>("null");
            Assert.False(fetch.TryFetch(null, out _));
        }
    }
}