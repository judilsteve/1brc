using System.Globalization;

var cityStatMap = new Dictionary<string, CityStats>(10000);

var rows = 0;
foreach (var line in File.ReadLines("measurements.txt")) {
    var separatorIndex = line.IndexOf(';');
    var city = line.Substring(0, separatorIndex);
    var temp = double.Parse(line[(separatorIndex + 1)..], CultureInfo.InvariantCulture);
    if (!cityStatMap.TryGetValue(city, out var cityStats)) {
        cityStats = new CityStats(temp);
        cityStatMap[city] = cityStats;
    } else {
        cityStats.AddMeasurement(temp);
    }
    if (++rows % 10_000_000 == 0) Console.WriteLine($"Processed {rows / 1_000_000}M rows.");
}

foreach (var kvp in cityStatMap.OrderBy(kvp => kvp.Key)) {
    Console.WriteLine($"{kvp.Key}={kvp.Value}");
}

class CityStats {
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

    public void AddMeasurement(double value)  {
        if (value < min) min = value;
        else if (value > max) max = value;
        sum += value;
        count++;
    }

    public override string ToString() {
        return $"{min:F1}/{(sum / count):F1}/{max:F1}";
    }
}
