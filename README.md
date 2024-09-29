# .NET project template

<!-- markdownlint-disable MD033 -->
<p align="center">
  <a href="https://github.com/pleonex/explitwiser/workflows/Build%20and%20release">
    <img alt="Build and release" src="https://github.com/pleonex/explitwiser/workflows/Build%20and%20release/badge.svg?branch=main&event=push" />
  </a>
  &nbsp;
  <a href="https://choosealicense.com/licenses/mit/">
    <img alt="MIT License" src="https://img.shields.io/badge/license-MIT-blue.svg?style=flat" />
  </a>
  &nbsp;
</p>

> [!WARNING]  
> I have no intention to maintain this project.

Export your account data from Splitwise.

## Get started

Run the console application with the `--help` to get information on the
supported parameters.

## Build

The project requires the .NET 8.0 SDK.

To build, test and generate artifacts run:

```sh
# Build
dotnet run --project build/orchestrator

# (Optional) Create bundles (nuget, zips, docs)
dotnet run --project build/orchestrator -- --target=Bundle
```

## Release

Create a new GitHub release with a tag `v{Version}` (e.g. `v2.4`) and that's it!
This triggers a pipeline that builds and deploy the project.
