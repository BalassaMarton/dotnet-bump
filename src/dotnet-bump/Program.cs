using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace DotnetBump
{
    public class Program
    {
        public static int Main(string[] args)
        {
            string[] validParts = new[] { "major", "minor", "patch", "revision" };
            string part = "revision";
            var i = 0;
            var projectFilePath = string.Empty;
            if (args.Length > 0)
            {
                var idx = Array.FindIndex(validParts, x => x == args[0].ToLower());
                if (idx >= 0)
                {
                    part = validParts[idx];
                    i++;
                }
            }
            try
            {
                while (i < args.Length)
                {
                    if (File.Exists(args[i]))
                    {
                        projectFilePath = args[i];
                        i++;
                    }
                    else
                        InvalidArgs();
                }
                if (string.IsNullOrEmpty(projectFilePath))
                    projectFilePath = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj").FirstOrDefault();
                if (string.IsNullOrEmpty(projectFilePath))
                {
                    Console.Error.WriteLine("Cannot find project file");
                    InvalidArgs();
                }
            }
            catch (ArgumentException e)
            {
                Console.Error.WriteLine(e.Message);
                return -1;
            }
            Console.Error.WriteLine($"Loading project from {projectFilePath}...");
            XDocument projectFile;
            using (var f = File.OpenRead(projectFilePath))
                projectFile = XDocument.Load(f);
            var versionElement=projectFile.Root?.Elements("PropertyGroup").Select(x=>x.Element("Version")).FirstOrDefault();
            if (string.IsNullOrEmpty(versionElement?.Value))
            {
                Console.Error.WriteLine("Missing project version, left unchanged");
                return -1;
            }
            var version = versionElement.Value;
            var suffix = version.EndsWith("-*");
            if (suffix)
            {
                version = version.Substring(0, version.Length - 2);
            }
            var oldVersion = new SemVer(version);
            SemVer newVersion;
            switch (part)
            {
                case "major":
                    newVersion = new SemVer(oldVersion.Major + 1, oldVersion.Minor, oldVersion.Build, oldVersion.Fix, oldVersion.Suffix, oldVersion.Buildvars);
                    break;
                case "minor":
                    newVersion = new SemVer(oldVersion.Major, oldVersion.Minor + 1, oldVersion.Build, oldVersion.Fix, oldVersion.Suffix, oldVersion.Buildvars);
                    break;
                case "patch":
                    newVersion = new SemVer(oldVersion.Major, oldVersion.Minor, oldVersion.Build + 1, oldVersion.Fix, oldVersion.Suffix, oldVersion.Buildvars);
                    break;
                case "revision":
                    newVersion = new SemVer(oldVersion.Major, oldVersion.Minor, oldVersion.Build, oldVersion.Fix + 1, oldVersion.Suffix, oldVersion.Buildvars);
                    break;
                default:
                    throw new InvalidOperationException();

            }
            Console.Error.WriteLine($"Changing version from \"{oldVersion}\" to \"{newVersion}\"");
            version = newVersion.ToString();
            if (suffix)
                version = version + "-*";
            versionElement.Value = version;
            Console.Error.WriteLine($"Saving project....");
            using(var f=File.CreateText(projectFilePath))projectFile.Save(f);
            Console.Error.WriteLine($"Project saved.");
            Console.WriteLine($"\"{oldVersion}\" to \"{newVersion}\"");
            return 0;
        }

        private static void InvalidArgs()
        {
            throw new ArgumentException("Usage: dotnet bump-version [major | minor | patch | revision] [path-to-project-file]");
        }
    }
}
