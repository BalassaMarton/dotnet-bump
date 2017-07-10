# dotnet-bump-version

A dotnet-cli command that bumps the version number of the current project. This is useful when working with multiple .NET Core projects
placed in different solutions, referencing each other as NuGet packages. Use this command before `dotnet pack` to increment a specific part of
the version number in `project.json` before pushing your project to your local NuGet feed. This ensures that NuGet will not fetch the package from cache,
and all your .NET Core projects in different solutions can reference the latest compiled version.

## Usage

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
