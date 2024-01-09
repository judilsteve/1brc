using System.Globalization;
using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;

var cityStatMap = new ConcurrentDictionary<string, CityStats>(-1, 10000);

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

    Console.WriteLine($"Length: {accessor.Length}");
    foreach(var (startIndex, endIndex) in chunkRanges) Console.WriteLine($"Chunk: {startIndex} -> {endIndex}");
}

Parallel.ForEach(chunkRanges, range => {
    var (startIndex, endIndex) = range;
    using var accessor = memoryMappedMeasurements.CreateViewStream(startIndex, endIndex - startIndex);
    using var reader = new StreamReader(accessor);
    string? line;
    while((line = reader.ReadLine()) is not null) {
        var separatorIndex = line.IndexOf(';');
        var city = line[..separatorIndex];
        var temp = double.Parse(line[(separatorIndex + 1)..], CultureInfo.InvariantCulture);
        var cityStats = cityStatMap.GetOrAdd(city, new CityStats());
        cityStats.AddMeasurement(temp);
    }
});

foreach (var kvp in cityStatMap.OrderBy(kvp => kvp.Key)) {
    Console.WriteLine($"{kvp.Key}={kvp.Value}");
}

class CityStats
{
    private double min;
    private double max;
    private double sum;
    private int count = 1;

    public void AddMeasurement(double value)  {
        lock(this) {
            if (value < min) min = value;
            else if (value > max) max = value;
            sum += value;
            count++;
        }
    }

    public override string ToString() {
        return $"{min:F1}/{sum / count:F1}/{max:F1}";
    }
}
