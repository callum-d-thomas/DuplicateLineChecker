using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace DuplicateLineChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("\nUsage: dotnet run --project DuplicateLineChecker.csproj <path_to_csproj_file>\n");
                return;
            }

            string filePath = args[0];
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"\nFile not found: {filePath}\n");
                return;
            }

            var totalStopwatch = Stopwatch.StartNew();
            var searchStopwatch = new Stopwatch();

            var duplicates = FindDuplicateIncludesWithConfirmation(filePath, searchStopwatch);
            
            totalStopwatch.Stop();

            if (duplicates.Any())
            {
                Console.WriteLine("\nConfirmed duplicate <Include> statements found:");
                foreach (var include in duplicates)
                {
                    Console.WriteLine(include);
                }

                WriteDuplicatesToFile(duplicates);
            }
            else
            {
                Console.WriteLine("\nNo confirmed duplicate <Include> statements found.\n");
            }

            Console.WriteLine($"\nTotal execution time: {totalStopwatch.Elapsed.TotalSeconds:F2} seconds.");
            Console.WriteLine($"Search duration: {searchStopwatch.Elapsed.TotalSeconds:F2} seconds.");
        }

        static IEnumerable<string> FindDuplicateIncludesWithConfirmation(string filePath, Stopwatch searchStopwatch)
        {
            var doc = XDocument.Load(filePath);
            var includes = doc.Descendants()
                              .Where(e => e.Attribute("Include") != null)
                              .Select(e => e.Attribute("Include").Value)
                              .ToList();

            var seen = new HashSet<string>();
            var duplicates = new List<string>();
            int total = includes.Count;

            for (int i = 0; i < includes.Count; i++)
            {
                for (int j = i + 1; j < includes.Count; j++)
                {
                    searchStopwatch.Start();
                    if (includes[i] == includes[j] && !seen.Contains(includes[i]))
                    {
                        searchStopwatch.Stop();
                        Console.WriteLine($"\nPotential duplicate found:\n1: {includes[i]}\n2: {includes[j]}");
                        Console.Write("\nDo you consider these <Include> statements duplicates? (y/n): ");
                        var response = Console.ReadLine();
                        if (response?.ToLower() == "y")
                        {
                            duplicates.Add(includes[i]);
                            seen.Add(includes[i]);
                        }
                        else if (response?.ToLower() == "cancel")
                        {
                            Console.WriteLine("\nOperation cancelled.\n");
                            Environment.Exit(0);
                        }
                    }
                    else
                    {
                        searchStopwatch.Stop();
                    }
                }
                DisplayProgress(i + 1, total);
            }

            return duplicates;
        }

        static void DisplayProgress(int current, int total)
        {
            int progressWidth = 50;
            double progress = (double)current / total;
            int progressChars = (int)(progress * progressWidth);

            Console.CursorLeft = 0;
            Console.Write("[");
            Console.Write(new string('#', progressChars));
            Console.Write(new string(' ', progressWidth - progressChars));
            Console.Write($"] {current}/{total} ({progress:P0})\n");
        }

        static void WriteDuplicatesToFile(IEnumerable<string> duplicates)
        {
            string outputDir = "output";
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            while (true)
            {
                Console.Write("\nEnter the output filename (without extension): ");
                string filename = Console.ReadLine();
                if (filename?.ToLower() == "cancel")
                {
                    Console.WriteLine("\nOperation cancelled.\n");
                    return;
                }

                string filePath = Path.Combine(outputDir, $"{filename}.txt");

                if (File.Exists(filePath))
                {
                    Console.Write($"\nFile {filePath} already exists. Overwrite? (y/n): ");
                    var response = Console.ReadLine();
                    if (response?.ToLower() == "y")
                    {
                        File.WriteAllLines(filePath, duplicates);
                        Console.WriteLine($"\nDuplicates written to {filePath}\n");
                        break;
                    }
                    else if (response?.ToLower() == "cancel")
                    {
                        Console.WriteLine("\nOperation cancelled.\n");
                        return;
                    }
                }
                else
                {
                    File.WriteAllLines(filePath, duplicates);
                    Console.WriteLine($"\nDuplicates written to {filePath}\n");
                    break;
                }
            }
        }
    }
}