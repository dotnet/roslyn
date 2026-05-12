// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks.Generator;

public abstract class AbstractBenchmark
{
    private ProjectSetup.RazorProject? _project;

    internal ProjectSetup.RazorProject? Project => _project;

    protected bool Cold { get; set; }

    [ModuleInitializer]
    public static void LoadMSBuild() => MSBuildLocator.RegisterDefaults();

    [GlobalSetup]
    public void Setup()
    {
        _project = ProjectSetup.GetRazorProject(Cold);
    }

    protected GeneratorDriver RunBenchmark(Func<ProjectSetup.RazorProject, GeneratorDriver> updateDriver)
    {
        var compilation = _project!.Compilation;
        var driver = updateDriver(_project!);

        driver = driver.RunGenerators(compilation);
        return driver;
    }
}
