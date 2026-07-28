// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using Microsoft.NET.Sdk.Razor.SourceGenerators;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks.Generator;
public static class ProjectSetup
{
    internal static async Task<RazorProject> GetRazorProjectAsync(bool cold = true)
    {
        var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync("SampleApp/SampleApp.csproj");

        if (workspace.Diagnostics.Count != 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, workspace.Diagnostics));
        }

        // remove any generators from the project as we don't want generated files in our initial compilation
        foreach (var analyzerRef in project.AnalyzerReferences)
        {
            project = project.RemoveAnalyzerReference(analyzerRef);
        }

        // get the constituent parts
        var compilation = await project.GetCompilationAndCheckSuccess();

        List<AdditionalText> additionalTexts = new List<AdditionalText>();
        foreach (var additionalDocument in project.AdditionalDocuments)
        {
            var text = await additionalDocument.GetTextAsync();
            additionalTexts.Add(new InMemoryAdditionalText(text, additionalDocument.FilePath!));
        }

        var parseOptions = (CSharpParseOptions)project.ParseOptions!;

        var optionsProvider = new TargetPathAnalyzerConfigOptionsProvider(project.AnalyzerOptions.AnalyzerConfigOptionsProvider);

        // create the generator driver we'll use for the tests
        // the generator we use will be dependent on the build configuration the benchmark is built in
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generators: new[] { new RazorSourceGenerator().AsSourceGenerator() },
                                                              additionalTexts: additionalTexts,
                                                              parseOptions: parseOptions,
                                                              optionsProvider: optionsProvider);

        // if we request a warm project, run the driver once through to start with, priming the caches
        if (!cold)
        {
            driver = driver.RunGenerators(compilation);
        }

        return new(driver, compilation, additionalTexts.ToImmutableArray(), parseOptions, optionsProvider);
    }

    internal static RazorProject GetRazorProject(bool cold = true) => Task.Run(() => GetRazorProjectAsync(cold)).GetAwaiter().GetResult();

    public static async Task<Compilation> GetCompilationAndCheckSuccess(this Project project)
    {
        var comp = await project.GetCompilationAsync();
        var diagnostics = comp!.GetDiagnostics();
        if (diagnostics.Any(d => d.Severity != DiagnosticSeverity.Hidden))
        {
            //Debug.Fail("Compilation contained non-hidden diagnostics");
        }
        return comp;
    }

    public record RazorProject(GeneratorDriver GeneratorDriver, Compilation Compilation, ImmutableArray<AdditionalText> AdditionalTexts, CSharpParseOptions ParseOptions, AnalyzerConfigOptionsProvider OptionsProvider);

    internal sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText _text;

        public InMemoryAdditionalText(SourceText text, string path)
        {
            _text = text;
            Path = path;
        }

        public InMemoryAdditionalText(string text, string path)
        {
            _text = SourceText.From(text, Encoding.UTF8);
            Path = path;
        }

        public override string Path { get; }

        public override SourceText? GetText(CancellationToken cancellationToken = default) => _text;
    }

    /// <summary>
    /// An options provider that will add the required razor metadata if it's missing.
    /// </summary>
    internal sealed class TargetPathAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptionsProvider _provider;

        public TargetPathAnalyzerConfigOptionsProvider(AnalyzerConfigOptionsProvider provider)
        {
            _provider = provider;
        }

        public override AnalyzerConfigOptions GlobalOptions { get => _provider.GlobalOptions; }

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _provider.GetOptions(tree);

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return new TargetPathAnalyzerOptions(textFile.Path, _provider.GetOptions(textFile));
        }

        internal class TargetPathAnalyzerOptions : AnalyzerConfigOptions
        {
            private readonly string _targetPath;

            private readonly AnalyzerConfigOptions _baseOptions;

            public TargetPathAnalyzerOptions(string name, AnalyzerConfigOptions baseOptions)
            {
                _targetPath = Convert.ToBase64String(Encoding.UTF8.GetBytes(name));
                _baseOptions = baseOptions;
            }

            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            {
                if (!_baseOptions.TryGetValue(key, out value))
                {
                    value = key == "build_metadata.AdditionalFiles.TargetPath" ? _targetPath : string.Empty;
                }
                return true;
            }
        }
    }
}
