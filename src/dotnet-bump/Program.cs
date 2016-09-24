using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Microsoft.DotNet.ProjectModel;
using Newtonsoft.Json;
using NuGet.Versioning;

namespace DotnetBump
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string[] validParts = new[] { "major", "minor", "patch", "revision" };
            string part = "revision";
            var targetConfiguration = "";
            var configuration = "";
            var i = 0;
            if (args.Length > 0)
            {
                var idx = Array.FindIndex(validParts, x => x == args[0].ToLower());
                if (idx >= 0)
                {
                    part = validParts[idx];
                    i++;
                }
            }
            while (i < args.Length)
            {
                if (args[i] == "-t" || args[i] == "--target-configuration")
                {
                    i++;
                    targetConfiguration = args[i];
                    i++;
                }
                else if (args[i] == "-c" || args[i] == "--configuration")
                {
                    i++;
                    configuration = args[i];
                    i++;
                }
                else
                    InvalidArgs();
            }
            if (!string.IsNullOrEmpty(targetConfiguration) && configuration != targetConfiguration)
                return;
            var projectFilePath =
                    $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}{Project.FileName}";
            Console.WriteLine($"Loading project from {projectFilePath}...");
            dynamic projectFile = JsonConvert.DeserializeObject(File.ReadAllText(projectFilePath));
            var version = (string)projectFile.version;
            if (string.IsNullOrEmpty(version))
            {
                Console.WriteLine("Missing project version, left unchanged");
                return;
            }
            var suffix = version.EndsWith("-*");
            if (suffix)
            {
                version = version.Substring(0, version.Length - 2);
            }
            var oldVersion = new NuGetVersion(version);
            NuGetVersion newVersion;
            switch (part)
            {
                case "major":
                    newVersion = new NuGetVersion(oldVersion.Major + 1, oldVersion.Minor, oldVersion.Patch, oldVersion.Revision, oldVersion.ReleaseLabels, oldVersion.Metadata);
                    break;
                case "minor":
                    newVersion = new NuGetVersion(oldVersion.Major, oldVersion.Minor + 1, oldVersion.Patch, oldVersion.Revision, oldVersion.ReleaseLabels, oldVersion.Metadata);
                    break;
                case "patch":
                    newVersion = new NuGetVersion(oldVersion.Major, oldVersion.Minor, oldVersion.Patch + 1, oldVersion.Revision, oldVersion.ReleaseLabels, oldVersion.Metadata);
                    break;
                case "revision":
                    newVersion = new NuGetVersion(oldVersion.Major, oldVersion.Minor, oldVersion.Patch, oldVersion.Revision + 1, oldVersion.ReleaseLabels, oldVersion.Metadata);
                    break;
                default:
                    throw new InvalidOperationException();

            }
            Console.WriteLine($"Changing version from \"{oldVersion.ToString()}\" to \"{newVersion.ToString()}\"");
            version = newVersion.ToString();
            if (suffix)
                version = version + "-*";
            projectFile.version = version;
            Console.WriteLine($"Saving project....");
            File.WriteAllText(projectFilePath, JsonConvert.SerializeObject(projectFile, Formatting.Indented));
            Console.WriteLine($"Project saved.");
        }

        private static void InvalidArgs()
        {
            throw new ArgumentException("Usage: dotnet bump [major | minor | patch | revision] [--configuration <configuration>] [--target-configuration <target-configuration>]");
        }
    }
}
