
using System;
using System.Linq;
using System.Collections.Generic;

public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int batchSize)
{
    if (source == null)
        return new List<List<T>>();
    if (source.Count() < batchSize)
        return new List<List<T>> { new List<T>(source) };

    return source
        .Select((x, i) => new { Index = i, Value = x })
        .GroupBy(x => x.Index / batchSize)
        .Select(x => x.Select(v => v.Value));
}
