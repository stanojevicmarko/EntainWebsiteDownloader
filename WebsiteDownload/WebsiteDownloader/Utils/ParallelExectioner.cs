using System.Collections.Concurrent;

namespace WebsiteDownloader.Utils;

internal static class ParallelExectioner
{
    internal static Task ParallelForEachAsync<T>(this IEnumerable<T> source, int partitionCount, Func<T, Task> body)
    {
        async Task AwaitPartition(IEnumerator<T> partition)
        {
            using (partition)
            {
                while (partition.MoveNext())
                { await body(partition.Current); }
            }
        }
        return Task.WhenAll(
            Partitioner
                .Create(source)
                .GetPartitions(partitionCount)
                .AsParallel()
                .Select(p => AwaitPartition(p)));
    }
}
