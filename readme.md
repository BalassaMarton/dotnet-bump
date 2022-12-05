# dotnet-bump-version

A dotnet-cli command that bumps the version number of the current project. This is useful when working with multiple .NET Core projects
placed in different solutions, referencing each other as NuGet packages. Use this command before `dotnet pack` to increment a specific part of
the version number in `project.json` before pushing your project to your local NuGet feed. This ensures that NuGet will not fetch the package from cache,
and all your .NET Core projects in different solutions can reference the latest compiled version.

## Whats new in v 2.0.0
- Upgraded to dotnet 6.0
- Interface for command line is upgraded to `System.CommandLine`, for pretty command line options
- Solution file support added, use `--sln` option with `.sln` file and it will update all available `.csproj` files 
- `Version`, `AssemblyVersion` and `FileVersion` is now searched and updated in `.csproj` file
- `Docker` support added, can be handy to be used in `CI/CD` pipelines

## Whats new in v 2.0.1
- Bugfix, when `--csproj` is provided it doesn't work
- `--suffix` command line option is added

## Usage

### Version 2.0.1

```sh
Description:
  Command line tool for version bump of dotnet applications v 2.0.1.0

Usage:
  dotnet-bump-version <part> [options]

Arguments:
  <part>  The part of version to be updated, supported values are major, minor, patch, revision.

Options:
  --suffix <suffix>  The suffix to be appended to version, it would be appended to version with leading -. e.g. if
                     suffix is set to 'rc1' final version would be x.x.x.x-rc1
  --csproj <csproj>  The path to C# project (.csproj) file. This option will be given precedence over --sln if both are
                     provided at same time.
  --sln <sln>        The path to solution (.sln) file. If --csproj is provided, this option will be ignored.
  --version          Show version information
  -?, -h, --help     Show help and usage information
```

### Older versions
Add `dotnet bump` as a tool to your project by including the following into your `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <DotNetCliToolReference Include="dotnet-bump-2" Version="1.2.0" />
  </ItemGroup>
</Project>
```

Run `dotnet restore` to fetch bump-version binaries, after that you may use `dotnet bump-version` command to maintain version.

The command will increment a part of the version number of your `.csproj` according to the argument passed to it (`major`, `minor`, `patch` or `revision`).
When this argument is ommited, the revision number is bumped. You may specify path to `.csproj` on the command line as nameless argument or rely on automatic discovery which would look for first `.csproj` file in the current directory.
