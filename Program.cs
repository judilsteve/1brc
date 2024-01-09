using System.Globalization;
using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;

using var memoryMappedMeasurements = MemoryMappedFile.CreateFromFile("measurements.txt");
var chunkRanges = new List<(long, long)>();
long nextChunkStart = 0;
using (var accessor = memoryMappedMeasurements.CreateViewStream()) {
    var desiredChunkLength = accessor.Length / Environment.ProcessorCount;
    while (accessor.Length - accessor.Position > desiredChunkLength) {
        accessor.Seek(desiredChunkLength, SeekOrigin.Current);
        while(accessor.ReadByte() != '\n') { } // Seek to start of next line
        chunkRanges.Add((nextChunkStart, accessor.Position - 1));
        nextChunkStart = accessor.Position;
    }
    chunkRanges.Add((nextChunkStart, accessor.Length - 1));
}

var chunkResults = new ConcurrentBag<Dictionary<string, CityStats>>();
Parallel.ForEach(chunkRanges, range => {
    var cities = new Dictionary<string, CityStats>(10000);
    var (startIndex, endIndex) = range;
    using var accessor = memoryMappedMeasurements.CreateViewStream(startIndex, endIndex - startIndex);
    using var reader = new StreamReader(accessor);
    string? line;
    while((line = reader.ReadLine()) is not null) {
        var separatorIndex = line.IndexOf(';');
        var city = line[..separatorIndex];
        var temp = double.Parse(line.AsSpan(separatorIndex + 1), CultureInfo.InvariantCulture);
        if (!cities.TryGetValue(city, out var cityStats)) {
            cityStats = new CityStats(temp);
            cities[city] = cityStats;
        } else {
            cityStats.AddMeasurement(temp);
        }
    }
    chunkResults.Add(cities);
});

var combined = new Dictionary<string, CityStats>(10000);
foreach(var result in chunkResults) {
    foreach(var (city, cityStatsChunk) in result) {
        if(combined.TryGetValue(city, out var cityStatsCombined)) {
            combined[city] = new CityStats(combined[city], cityStatsChunk);
        } else {
            combined[city] = cityStatsChunk;
        }
    }
}

foreach (var (city, stats) in combined.OrderBy(kvp => kvp.Key)) {
    Console.WriteLine($"{city}={stats}");
}

class CityStats
{
    private double min;
    private double max;
    private double sum;
    private int count;

    public CityStats(double firstValue) {
        min = firstValue;
        max = firstValue;
        sum = firstValue;
        count = 1;
    }

    public CityStats(CityStats a, CityStats b) {
        min = Math.Min(a.min, b.min);
        max = Math.Max(a.max, b.max);
        sum = a.sum + b.sum;
        count = a.count + b.count;
    }

    public void AddMeasurement(double value)  {
        if (value < min) min = value;
        else if (value > max) max = value;
        sum += value;
        count++;
    }

    public override string ToString() => $"{min:F1}/{sum / count:F1}/{max:F1}";
}
