package main

import (
	"bufio"
	"fmt"
	"io"
	"math"
	"os"
	"slices"
	"strconv"
	"strings"
)

func main() {
	err := run()
	if err != nil {
		fmt.Println(err)
		os.Exit(1)
	}
}

type Stats struct {
	min   float64
	max   float64
	count int
	sum   float64
}

func run() error {
	weatherstations, err := os.Open("data/weather_stations.csv")
	if err != nil {
		return err
	}
	defer weatherstations.Close()

	// Create a buffered reader
	reader := bufio.NewReader(weatherstations)
	var cityBuilder strings.Builder
	var valueBuilder strings.Builder

	cityStats := make(map[string]Stats, 10000)
	readingCity := true
	for {
		rune, _, err := reader.ReadRune()
		if err != nil {
			if err == io.EOF {
				break
			}
			return err
		}

		if rune == ';' {
			readingCity = false
			continue
		}
		if rune == '\n' {
			stat := cityStats[cityBuilder.String()]
			value, err := strconv.ParseFloat(valueBuilder.String(), 32)
			if err != nil {
				return err
			}
			stat.count += 1
			stat.sum += value
			stat.min = math.Min(stat.min, value)
			stat.max = math.Max(stat.max, value)
			cityStats[cityBuilder.String()] = stat

			cityBuilder.Reset()
			valueBuilder.Reset()
			readingCity = true
			continue
		}
		if readingCity {
			// Add the character to the builder
			cityBuilder.WriteRune(rune)
		} else {
			valueBuilder.WriteRune(rune)
		}
	}
	cities := make([]string, 0, len(cityStats))
	for k := range cityStats {
		cities = append(cities, k)
	}
	slices.Sort(cities)
	var outputBuilder strings.Builder
	outputBuilder.WriteString("{")
	for _, city := range cities {
		stats := cityStats[city]
		outputBuilder.WriteString(fmt.Sprintf("%s=%v/%v/%v", city, stats.min, math.Round(stats.sum/float64(stats.count)*10)/10, stats.max))
	}
	outputBuilder.WriteString("}")
	fmt.Println(outputBuilder.String())

	return nil
}
