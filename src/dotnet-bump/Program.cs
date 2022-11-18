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

        var csprojFileOption = new Option<FileInfo>(
            name: "--csproj",
            description: "The path to C# project (.csproj) file. This option will be given precedence over --sln if both are provided at same time.");

        var slnFileOption = new Option<FileInfo>(
            name: "--sln",
            description: "The path to solution (.sln) file. If --csproj is provided, this option will be ignored.");

        var partArgument = new Argument<string>(
            name: "part",
            description: "The part of version to be updated, supported values are major, minor, patch, revision.");

        var suffixOption = new Option<string>(
            name: "--suffix",
            description: "The suffix to be appended to version, it would be appended to version with leading -. e.g. if suffix is set to 'rc1' final version would be x.x.x.x-rc1"
            );

        rootCommand.AddArgument(partArgument);
        rootCommand.AddOption(suffixOption);
        rootCommand.AddOption(csprojFileOption);
        rootCommand.AddOption(slnFileOption);

        rootCommand.SetHandler(async (pov, sfov, cfov, sfxov) => {
            //pov contains part option value
            //sfov contains sln file option value
            //cfov contains csproj file option value
            //sfxov contains suffix option value
            await UpdateVersionSolutionAsync(pov, sfov, cfov, sfxov);
        }, partArgument, slnFileOption, csprojFileOption, suffixOption);

        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Update a csproj file
    /// </summary>
    /// <param name="pov"></param>
    /// <param name="cfov"></param>
    /// <returns></returns>
    private static async Task UpdateProjectVersionAsync(object pov, object cfov, object sfxov)
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
                        
                        //check for suffix, if that is provided give precidence to it
                        string newSuffix = sfxov?.ToString() ?? oldVersion.Suffix;

                        SemVer newVersion;
                        switch (part)
                        {
                            case "major":
                                newVersion = new SemVer(oldVersion.Major + 1, oldVersion.Minor, oldVersion.Build, oldVersion.Fix, newSuffix, oldVersion.Buildvars);
                                break;
                            case "minor":
                                newVersion = new SemVer(oldVersion.Major, oldVersion.Minor + 1, oldVersion.Build, oldVersion.Fix, newSuffix, oldVersion.Buildvars);
                                break;
                            case "patch":
                                newVersion = new SemVer(oldVersion.Major, oldVersion.Minor, oldVersion.Build + 1, oldVersion.Fix, newSuffix, oldVersion.Buildvars);
                                break;
                            case "revision":
                                newVersion = new SemVer(oldVersion.Major, oldVersion.Minor, oldVersion.Build, oldVersion.Fix + 1, newSuffix, oldVersion.Buildvars);
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

    /// <summary>
    /// Iterate a solution file and update all included csproj files
    /// </summary>
    /// <param name="pov"></param>
    /// <param name="sfov"></param>
    /// <returns></returns>
    private static async Task UpdateVersionSolutionAsync(object pov, object sfov, object cfov, object sfxov)
    {
        await Task.Run(async () =>
        {
            //Check if csproj file is provided, give precedence to it.
            FileInfo csprojFI = cfov as FileInfo;
            if (csprojFI != null)
            {
                if (csprojFI != null)
                {
                    await UpdateProjectVersionAsync(pov, csprojFI, sfxov);
                }
            }
            else
            {
                //sln file
                FileInfo slnjFI = sfov as FileInfo;
                if (slnjFI != null)
                {
                    //throw new ArgumentException("please provide a sln file");
                    var solutionFilePath = slnjFI.FullName;

                    var sfData = SolutionFile.Parse(solutionFilePath);

                    var projectsInSolution = sfData.ProjectsInOrder;
                    foreach (var project in projectsInSolution)
                    {
                        switch (project.ProjectType)
                        {
                            case SolutionProjectType.KnownToBeMSBuildFormat:
                                {
                                    await UpdateProjectVersionAsync(pov, new FileInfo(project.AbsolutePath), sfxov);
                                    break;
                                }
                            case SolutionProjectType.SolutionFolder:
                                {
                                    Console.WriteLine("Another solution file found, skipping it");
                                    break;
                                }
                        }
                    }
                }
            }
        });
    }
}
