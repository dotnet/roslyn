// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class RazorToolingProjectEngineTestBase : ToolingTestBase
{
    protected abstract RazorLanguageVersion Version { get; }

    protected RazorToolingProjectEngineTestBase(ITestOutputHelper testOutput)
        : base(testOutput)
    {
    }

    protected virtual void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
    {
    }

    protected RazorEngine CreateEngine() => CreateProjectEngine().Engine;

    protected RazorProjectEngine CreateProjectEngine()
    {
        var configuration = new RazorConfiguration(Version, "test", Extensions: []);
        return RazorProjectEngine.Create(configuration, RazorProjectFileSystem.Empty, b =>
        {
            EnableMarkupSplit(b);
            ConfigureProjectEngine(b);
        });
    }

    protected RazorProjectEngine CreateProjectEngine(Action<RazorProjectEngineBuilder> configure)
    {
        var configuration = new RazorConfiguration(Version, "test", Extensions: []);
        return RazorProjectEngine.Create(configuration, RazorProjectFileSystem.Empty, b =>
        {
            EnableMarkupSplit(b);
            ConfigureProjectEngine(b);
            configure?.Invoke(b);
        });
    }

    /// <summary>
    ///  Opts the engine into the decl/impl markup split. The split is off by default in the compiler
    ///  (only the source generator consumes both halves), but tests assert against split output.
    /// </summary>
    private static void EnableMarkupSplit(RazorProjectEngineBuilder builder)
        => builder.ConfigureCodeGenerationOptions(static options => options.EnableMarkupSplit = true);
}
