using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Build.Construction;

namespace DotnetBump;

public class Program
{
  
    /// <summary>
    /// Entry point of application
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand($"Command line tool for version bump of dotnet applications v {Assembly.GetAssembly(typeof(Program)).GetName().Version}");

        var partOption = new Option<string>(
            name: "--part",
            description: "The part of version to be updated, supported values are major, minor, patch, revision.");

        var csprojFileOption = new Option<FileInfo>(
            name: "--csproj",
            description: "The path to C# project (.csproj) file.");

        var slnFileOption = new Option<FileInfo>(
            name: "--sln",
            description: "The path to solution (.sln) file.");

        rootCommand.AddOption(partOption);
        rootCommand.AddOption(csprojFileOption);
        rootCommand.AddOption(slnFileOption);

        rootCommand.SetHandler(async (pov, cfov) => {
            //pov contains part option value
            //cfov contains csproj file option value
            await UpdateVersionAsync(pov, cfov);
        }, partOption, csprojFileOption);

        rootCommand.SetHandler(async (pov, sfov) => {
            //pov contains part option value
            //sfov contains sln file option value
            await UpdateVersionSolutionAsync(pov, sfov);
        }, partOption, slnFileOption);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task UpdateVersionAsync(object pov, object cfov)
    {
        await Task.Run(() =>
        {
            try
            {
                //Different XML version tags for csproj file
                string[] versiontag_map = new[] { "Version", "AssemblyVersion", "FileVersion" };

                string[] validParts = new[] { "major", "minor", "patch", "revision" };
                string part = "revision";

                //Setting part
                if (pov != null)
                {
                    var idx = Array.FindIndex(validParts, x => x == pov.ToString().ToLower());
                    if (idx >= 0)
                    {
                        part = validParts[idx];
                    }
                }
                else
                {
                    Console.WriteLine($"Part is not provided, will update the {part} by default");
                }

                //csproj file
                FileInfo csprojFI = cfov as FileInfo;
                if (csprojFI == null)
                {
                    throw new ArgumentException("please provide a csproj file");
                }

                var projectFilePath = csprojFI.FullName;

                Console.WriteLine($"Loading project from {projectFilePath}...");
                XDocument projectFile;
                using (var f = File.OpenRead(projectFilePath))
                    projectFile = XDocument.Load(f);

                //first check for any possibility of available tags
                var availableTags = projectFile.Root?.Elements("PropertyGroup").Descendants().Where(s => versiontag_map.Contains(s.Name.ToString()));
                if (availableTags?.Count() <= 0)
                {
                    Console.Error.WriteLine("Missing project version, left unchanged");
                }
                else
                {
                    StringBuilder sbVersionUpdates = new StringBuilder();
                    //loop through possibilities and update
                    foreach (var versionElement in availableTags)
                    {
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
                        //Console.Error.WriteLine($"Changing version from \"{oldVersion}\" to \"{newVersion}\"");
                        version = newVersion.ToString();
                        if (suffix)
                            version = version + "-*";
                        versionElement.Value = version;


                        //update string builder to reflect version change
                        sbVersionUpdates.AppendLine($"{versionElement.Name.ToString()} updated from \"{oldVersion}\" to \"{newVersion}\"");
                    }

                    Console.Error.WriteLine($"Saving project....");
                    using (var f = File.CreateText(projectFilePath))
                    {
                        projectFile.Save(f);
                    }
                    Console.Error.WriteLine($"Project saved.");
                    Console.WriteLine(sbVersionUpdates.ToString());
                }
            }catch (Exception exp)
            {
                var old_color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(exp.Message);
                Console.WriteLine(exp.StackTrace);
                Console.ForegroundColor = old_color;
            }
        });
    }

    private static async Task UpdateVersionSolutionAsync(object pov, object sfov)
    {
        await Task.Run(async () =>
        {
            //csproj file
            FileInfo slnjFI = sfov as FileInfo;
            if (slnjFI == null)
            {
                throw new ArgumentException("please provide an sln file");
            }

            var solutionFilePath = slnjFI.FullName;

            var sfData = SolutionFile.Parse(solutionFilePath);

            var projectsInSolution = sfData.ProjectsInOrder;
            foreach (var project in projectsInSolution)
            {
                switch (project.ProjectType)
                {
                    case SolutionProjectType.KnownToBeMSBuildFormat:
                        {
                            await UpdateVersionAsync(pov, new FileInfo(project.AbsolutePath));
                            break;
                        }
                    case SolutionProjectType.SolutionFolder:
                        {
                            Console.WriteLine("Another solution file found, skipping it");
                            break;
                        }
                }
            }
        });
    }

    private static void InvalidArgs()
    {
        throw new ArgumentException("Usage: dotnet bump-version [major | minor | patch | revision] [path-to-project-file]");
    }
}
