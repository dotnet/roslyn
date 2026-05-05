// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

public class SyntaxTreeGenerationBenchmark
{
    public SyntaxTreeGenerationBenchmark()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "MSN.cshtml")))
        {
            current = current.Parent;
        }

        var root = current;
        var fileSystem = RazorProjectFileSystem.Create(root.FullName);

        ProjectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, fileSystem, b => RazorExtensions.Register(b)); ;

        var projectItem = fileSystem.GetItem(Path.Combine(root.FullName, "MSN.cshtml"), RazorFileKind.Legacy);
        MSN = RazorSourceDocument.ReadFrom(projectItem);

        var directiveFeature = ProjectEngine.Engine.GetFeatures<ConfigureDirectivesFeature>().FirstOrDefault();
        Directives = directiveFeature?.GetDirectives() ?? [];
    }

    public RazorProjectEngine ProjectEngine { get; }

    public RazorSourceDocument MSN { get; }

    public ImmutableArray<DirectiveDescriptor> Directives { get; }

    [Benchmark(Description = "Razor Design Time Syntax Tree Generation of MSN.com")]
    public void SyntaxTreeGeneration_DesignTime_LargeStaticFile()
    {
        var options = RazorParserOptions.Default
            .WithDirectives(Directives)
            .WithFlags(designTime: true);

        var syntaxTree = RazorSyntaxTree.Parse(MSN, options);

        if (syntaxTree.Diagnostics.Length != 0)
        {
            throw new Exception("Error!" + Environment.NewLine + string.Join(Environment.NewLine, syntaxTree.Diagnostics));
        }
    }

    [Benchmark(Description = "Razor Runtime Syntax Tree Generation of MSN.com")]
    public void SyntaxTreeGeneration_Runtime_LargeStaticFile()
    {
        var options = RazorParserOptions.Default.WithDirectives(Directives);
        var syntaxTree = RazorSyntaxTree.Parse(MSN, options);

        if (syntaxTree.Diagnostics.Length != 0)
        {
            throw new Exception("Error!" + Environment.NewLine + string.Join(Environment.NewLine, syntaxTree.Diagnostics));
        }
    }
}
