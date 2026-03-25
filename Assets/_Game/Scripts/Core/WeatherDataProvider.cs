using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Whisperer
{
    public class WeatherDataProvider : MonoBehaviour
    {
        [Header("Data Source")]
        public TextAsset weatherCsv;
        public string fallbackCsvRelativePath = "WindamClimateData.csv";

        [Header("Approximate Location")]
        public string defaultStationKeyword = "BELLOWS FALLS";

        [Header("Debug")]
        public bool debugLogWeather;
        [TextArea(4, 12)]
        [SerializeField] string lastWeatherReport = "";

        readonly Dictionary<string, List<WeatherObservation>> observationsByStation = new Dictionary<string, List<WeatherObservation>>(StringComparer.OrdinalIgnoreCase);
        bool loaded;

        static readonly Regex MonthRegex = new Regex(@"\b(january|february|march|april|may|june|july|august|september|october|november|december)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex MonthYearRegex = new Regex(@"\b(january|february|march|april|may|june|july|august|september|october|november|december)\s+(19\d{2}|20\d{2})\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex YearRegex = new Regex(@"\b(19\d{2}|20\d{2})\b", RegexOptions.Compiled);
        static readonly Regex WeatherIntentRegex = new Regex(@"\b(weather|climate|rain|raining|snow|snowfall|storm|temperature|temperatures|cold|warm|frost|precipitation)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex HistoricalCueRegex = new Regex(@"\b(last month|previous month|earlier|before|back in|during|what was it like|do you remember|remember when)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string LastWeatherReport => lastWeatherReport;

        public string BuildCurrentWeatherContext(DateTime replyDate, string contextHint)
        {
            EnsureLoaded();
            if (observationsByStation.Count == 0)
            {
                return "Current local weather context: historical station data is unavailable.";
            }

            StationSelection station = SelectStation(replyDate, contextHint);
            DateTime from = replyDate.AddDays(-14);
            WeatherSummary summary = BuildSummary(station, from, replyDate, replyDate);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Current local weather context (historical proxy):");
            builder.Append("- Approximate station: ");
            builder.Append(station.stationName);
            builder.AppendLine(".");
            builder.Append("- ");
            builder.Append(summary.text);

            string block = builder.ToString().TrimEnd();
            lastWeatherReport = block;
            if (debugLogWeather)
            {
                Debug.Log($"[Whisperer] {block}");
            }

            return block;
        }

        public string BuildHistoricalWeatherContext(DateTime replyDate, string playerBody, string contextHint)
        {
            if (string.IsNullOrWhiteSpace(playerBody)) return "";
            if (!ShouldInjectHistoricalWeather(playerBody)) return "";

            EnsureLoaded();
            if (observationsByStation.Count == 0) return "";

            if (!TryResolveHistoricalRange(replyDate, playerBody, out DateTime from, out DateTime to, out string label))
            {
                return "";
            }

            StationSelection station = SelectStation(replyDate, contextHint);
            WeatherSummary summary = BuildSummary(station, from, to, replyDate);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Historical weather recall (optional context):");
            builder.Append("- ");
            builder.Append(label);
            builder.Append(": ");
            builder.Append(summary.text);

            string block = builder.ToString().TrimEnd();
            if (debugLogWeather)
            {
                Debug.Log($"[Whisperer] {block}");
            }

            return block;
        }

        void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;

            string csvText = null;
            if (weatherCsv != null && !string.IsNullOrWhiteSpace(weatherCsv.text))
            {
                csvText = weatherCsv.text;
            }
            else if (!string.IsNullOrWhiteSpace(fallbackCsvRelativePath))
            {
                string fullPath = Path.Combine(Directory.GetCurrentDirectory(), fallbackCsvRelativePath);
                if (File.Exists(fullPath))
                {
                    csvText = File.ReadAllText(fullPath);
                }
            }

            if (string.IsNullOrWhiteSpace(csvText))
            {
                lastWeatherReport = "Weather provider could not load CSV data.";
                return;
            }

            ParseCsv(csvText);
        }

        void ParseCsv(string csvText)
        {
            string[] lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= 1) return;

            List<string> header = ParseCsvLine(lines[0]);
            Dictionary<string, int> columns = BuildColumnMap(header);
            if (!columns.TryGetValue("DATE", out int dateIndex)) return;
            if (!columns.TryGetValue("NAME", out int nameIndex)) return;
            if (!columns.TryGetValue("STATION", out int stationIndex)) return;

            columns.TryGetValue("PRCP", out int prcpIndex);
            columns.TryGetValue("TAVG", out int tavgIndex);
            columns.TryGetValue("TMAX", out int tmaxIndex);
            columns.TryGetValue("TMIN", out int tminIndex);

            for (int i = 1; i < lines.Length; i++)
            {
                List<string> values = ParseCsvLine(lines[i]);
                if (values.Count <= dateIndex || values.Count <= nameIndex || values.Count <= stationIndex) continue;

                if (!DateTime.TryParseExact(values[dateIndex], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                    continue;

                WeatherObservation observation = new WeatherObservation
                {
                    stationId = SafeGet(values, stationIndex),
                    stationName = SafeGet(values, nameIndex),
                    date = date,
                    prcp = ParseNullableFloat(SafeGet(values, prcpIndex)),
                    tavg = ParseNullableFloat(SafeGet(values, tavgIndex)),
                    tmax = ParseNullableFloat(SafeGet(values, tmaxIndex)),
                    tmin = ParseNullableFloat(SafeGet(values, tminIndex))
                };

                if (string.IsNullOrWhiteSpace(observation.stationName)) continue;

                string key = observation.stationId;
                if (string.IsNullOrWhiteSpace(key)) key = observation.stationName;

                if (!observationsByStation.TryGetValue(key, out List<WeatherObservation> stationData))
                {
                    stationData = new List<WeatherObservation>();
                    observationsByStation[key] = stationData;
                }

                stationData.Add(observation);
            }

            foreach (var pair in observationsByStation)
            {
                pair.Value.Sort((a, b) => a.date.CompareTo(b.date));
            }
        }

        StationSelection SelectStation(DateTime referenceDate, string contextHint)
        {
            string preferredKeyword = ResolvePreferredStationKeyword(contextHint);
            StationSelection best = default;
            best.score = int.MinValue;

            foreach (var pair in observationsByStation)
            {
                List<WeatherObservation> stationData = pair.Value;
                if (stationData == null || stationData.Count == 0) continue;

                string stationName = stationData[0].stationName;
                int score = ScoreStation(stationName, stationData, referenceDate, preferredKeyword);
                if (score <= best.score) continue;

                best.score = score;
                best.stationKey = pair.Key;
                best.stationName = stationName;
                best.observations = stationData;
            }

            return best;
        }

        static int ScoreStation(string stationName, List<WeatherObservation> stationData, DateTime referenceDate, string preferredKeyword)
        {
            int score = 0;
            if (!string.IsNullOrWhiteSpace(preferredKeyword) && stationName.IndexOf(preferredKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 5000;
            }

            int nearestDays = int.MaxValue;
            int sameYearMonthSamples = 0;
            for (int i = 0; i < stationData.Count; i++)
            {
                WeatherObservation row = stationData[i];
                int distance = Math.Abs((row.date - referenceDate).Days);
                if (distance < nearestDays) nearestDays = distance;
                if (row.date.Year == referenceDate.Year && row.date.Month == referenceDate.Month)
                {
                    sameYearMonthSamples++;
                }
            }

            score += Mathf.Clamp(3650 - nearestDays, 0, 3650);
            score += sameYearMonthSamples * 4;
            return score;
        }

        string ResolvePreferredStationKeyword(string contextHint)
        {
            string hint = (contextHint ?? string.Empty).ToLowerInvariant();

            if (hint.Contains("brattleboro")) return "BRATTLEBORO";
            if (hint.Contains("townshend") || hint.Contains("windham") || hint.Contains("newfane")) return "BELLOWS FALLS";
            if (hint.Contains("west river") || hint.Contains("dark mountain")) return "BELLOWS FALLS";

            return string.IsNullOrWhiteSpace(defaultStationKeyword) ? "BELLOWS FALLS" : defaultStationKeyword;
        }

        WeatherSummary BuildSummary(StationSelection station, DateTime from, DateTime to, DateTime referenceDate)
        {
            List<WeatherObservation> rows = new List<WeatherObservation>();
            if (station.observations != null)
            {
                for (int i = 0; i < station.observations.Count; i++)
                {
                    WeatherObservation row = station.observations[i];
                    if (row.date < from || row.date > to) continue;
                    rows.Add(row);
                }
            }

            if (rows.Count == 0 && station.observations != null)
            {
                for (int i = 0; i < station.observations.Count; i++)
                {
                    WeatherObservation row = station.observations[i];
                    if (row.date.Year == referenceDate.Year && row.date.Month == referenceDate.Month)
                    {
                        rows.Add(row);
                    }
                }
            }

            if (rows.Count == 0)
            {
                return new WeatherSummary
                {
                    text = $"No station rows were found for {from:yyyy-MM-dd} to {to:yyyy-MM-dd}; treat weather references as uncertain."
                };
            }

            float totalPrecip = 0f;
            int precipSamples = 0;
            int wetDays = 0;
            float avgTempSum = 0f;
            int avgTempSamples = 0;
            float maxTemp = float.MinValue;
            float minTemp = float.MaxValue;
            bool hasTempRange = false;

            for (int i = 0; i < rows.Count; i++)
            {
                WeatherObservation row = rows[i];
                if (row.prcp.HasValue)
                {
                    precipSamples++;
                    totalPrecip += row.prcp.Value;
                    if (row.prcp.Value > 0.0f) wetDays++;
                }

                if (row.tavg.HasValue)
                {
                    avgTempSamples++;
                    avgTempSum += row.tavg.Value;
                }

                if (row.tmax.HasValue)
                {
                    hasTempRange = true;
                    maxTemp = Mathf.Max(maxTemp, row.tmax.Value);
                }

                if (row.tmin.HasValue)
                {
                    hasTempRange = true;
                    minTemp = Mathf.Min(minTemp, row.tmin.Value);
                }
            }

            StringBuilder summary = new StringBuilder();
            summary.Append($"Records between {from:MMM d, yyyy} and {to:MMM d, yyyy} show ");

            if (precipSamples > 0)
            {
                summary.Append($"precipitation on {wetDays} of {precipSamples} sampled days (cumulative {totalPrecip:0.0} station units)");
            }
            else
            {
                summary.Append("no sampled precipitation values");
            }

            if (avgTempSamples > 0)
            {
                float avgTemp = avgTempSum / avgTempSamples;
                summary.Append($", average temperature {avgTemp:0.0}");
            }

            if (hasTempRange)
            {
                summary.Append($", and observed range {minTemp:0.0} to {maxTemp:0.0}");
            }

            summary.Append(".");
            summary.Append(" Use this as county-level proxy weather, not exact point measurement.");

            return new WeatherSummary { text = summary.ToString() };
        }

        static bool ShouldInjectHistoricalWeather(string playerBody)
        {
            bool hasWeatherIntent = WeatherIntentRegex.IsMatch(playerBody);
            bool hasHistoricalCue = HistoricalCueRegex.IsMatch(playerBody) || MonthRegex.IsMatch(playerBody);
            return hasWeatherIntent && hasHistoricalCue;
        }

        static bool TryResolveHistoricalRange(DateTime replyDate, string playerBody, out DateTime from, out DateTime to, out string label)
        {
            string text = playerBody ?? string.Empty;

            if (Regex.IsMatch(text, @"\b(last|previous)\s+month\b", RegexOptions.IgnoreCase))
            {
                DateTime prevMonth = new DateTime(replyDate.Year, replyDate.Month, 1).AddMonths(-1);
                from = new DateTime(prevMonth.Year, prevMonth.Month, 1);
                to = new DateTime(prevMonth.Year, prevMonth.Month, DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month));
                label = "Last month";
                return true;
            }

            Match monthYearMatch = MonthYearRegex.Match(text);
            if (monthYearMatch.Success)
            {
                int month = DateTime.ParseExact(monthYearMatch.Groups[1].Value, "MMMM", CultureInfo.InvariantCulture).Month;
                int year = int.Parse(monthYearMatch.Groups[2].Value, CultureInfo.InvariantCulture);
                from = new DateTime(year, month, 1);
                to = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                label = $"{CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month)} {year}";
                return true;
            }

            Match monthMatch = MonthRegex.Match(text);
            if (monthMatch.Success)
            {
                int month = DateTime.ParseExact(monthMatch.Groups[1].Value, "MMMM", CultureInfo.InvariantCulture).Month;
                int year = replyDate.Year;

                Match explicitYear = YearRegex.Match(text);
                if (explicitYear.Success)
                {
                    year = int.Parse(explicitYear.Value, CultureInfo.InvariantCulture);
                }
                else if (month > replyDate.Month)
                {
                    year -= 1;
                }

                from = new DateTime(year, month, 1);
                to = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                label = $"{CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month)} {year}";
                return true;
            }

            from = default;
            to = default;
            label = "";
            return false;
        }

        static Dictionary<string, int> BuildColumnMap(List<string> header)
        {
            Dictionary<string, int> map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < header.Count; i++)
            {
                string key = header[i].Trim().Trim('"');
                if (!map.ContainsKey(key)) map[key] = i;
            }

            return map;
        }

        static List<string> ParseCsvLine(string line)
        {
            List<string> fields = new List<string>();
            if (line == null)
            {
                fields.Add(string.Empty);
                return fields;
            }

            StringBuilder current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                    continue;
                }

                if (ch == ',' && !inQuotes)
                {
                    fields.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(ch);
            }

            fields.Add(current.ToString());
            return fields;
        }

        static float? ParseNullableFloat(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            if (float.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                return parsed;
            }

            return null;
        }

        static string SafeGet(List<string> values, int index)
        {
            if (index < 0 || index >= values.Count) return "";
            return values[index]?.Trim();
        }

        struct StationSelection
        {
            public string stationKey;
            public string stationName;
            public List<WeatherObservation> observations;
            public int score;
        }

        struct WeatherSummary
        {
            public string text;
        }

        struct WeatherObservation
        {
            public string stationId;
            public string stationName;
            public DateTime date;
            public float? prcp;
            public float? tavg;
            public float? tmax;
            public float? tmin;
        }
    }
}