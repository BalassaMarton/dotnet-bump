# dotnet-bump

A dotnet-cli command that bumps the version number of the current project. This is useful when working with multiple .NET Core projects
placed in different solutions, referencing each other as NuGet packages. Use this command before `dotnet pack` to increment a specific part of
the version number in `project.json` before pushing your project to your local NuGet feed. This ensures that NuGet will not fetch the package from cache,
and all your .NET Core projects in different solutions can reference the latest compiled version.

## Usage

Add `dotnet bump` as a tool to your project by including the following into your `project.json`:

```json
{
	"tools":
		{
			"dotnet-bump": "1.0.0"
		}
}
```

Update the `scripts` section of `project.json` so that the version number is incremented before `dotnet pack`:

```json
{
	"scripts": {
		"postcompile": [
			"dotnet bump revision",
			"dotnet pack ..."
		]
	}
}
```

The command will increment a part of the version number of your `project.json` according to the argument passed to it (`major`, `minor`, `patch` or `revision`).
When this argument is ommited, the revision number is bumped.

Alternatively, you can limit bumping the version number to a specific configuration by adding a `-t` (or `--target-configuration`) option
paired with a `-c` (or `--configuration`) option that gets its value from a macro:

```
dotnet bump revision -t Release -c %compile:Configuration%
```