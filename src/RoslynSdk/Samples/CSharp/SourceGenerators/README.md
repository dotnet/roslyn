🚧 Work In Progress 
========

These samples are for an in-progress feature of Roslyn. As such they may change or break as the feature is developed, and no level of support is implied.

For more infomation on the Source Generators feature, see the [design document](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.md).

Prerequisites
-----

These samples require Visual Studio 16.6 or higher.

Building the samples
-----
Open `SourceGenerators.sln` in Visual Studio or run `dotnet build` from the `\SourceGenerators` directory.

Running the samples
-----

The generators must be run as part of another build, as they inject source into the project being built. This repo contains a sample project `GeneratorDemo` that relies of the sample generators to add code to it's compilation. 

Run `GeneratedDemo` in Visual studio or run `dotnet run` from the `GeneratorDemo` directory.

Using the samples in your project
-----

You can add the sample generators to your own project by adding an item group containing an analyzer reference:

```xml
<ItemGroup>
    <Analyzer Include="path\to\SourceGeneratorSamples.dll"/>
</ItemGroup>
```

You may need to close and reopen the solution in Visual Studio for the change to take effect.
