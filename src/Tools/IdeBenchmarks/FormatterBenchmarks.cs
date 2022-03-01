// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.IO;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace IdeBenchmarks
{
    [GcServer(true)]
    public class FormatterBenchmarks
    {
        private readonly UseExportProviderAttribute _useExportProviderAttribute = new UseExportProviderAttribute();

        [Params(
            "BoundNodes.xml.Generated",
            "ErrorFacts.Generated",
            "Syntax.xml.Internal.Generated",
            "Syntax.xml.Main.Generated",
            "Syntax.xml.Syntax.Generated")]
        public string Document { get; set; }

        [IterationSetup]
        public void IterationSetup()
            => _useExportProviderAttribute.Before(null);

        [IterationCleanup]
        public void IterationCleanup()
            => _useExportProviderAttribute.After(null);

        [Benchmark]
        public object FormatCSharp()
        {
            var path = Path.Combine(Path.GetFullPath(@"..\..\..\..\..\src\Compilers\CSharp\Portable\Generated"), Document + ".cs");
            var text = File.ReadAllText(path);

            using var workspace = TestWorkspace.CreateCSharp(text);
            var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);
            var root = document.GetSyntaxRootSynchronously(CancellationToken.None);
            var options = workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService<ISyntaxFormattingService>().GetFormattingOptions(DictionaryAnalyzerConfigOptions.Empty);
            return Formatter.GetFormattedTextChanges(root, workspace.Services, options, CancellationToken.None);
        }

        [Benchmark]
        public object FormatVisualBasic()
        {
            var path = Path.Combine(Path.GetFullPath(@"..\..\..\..\..\src\Compilers\VisualBasic\Portable\Generated"), Document + ".vb");
            var text = File.ReadAllText(path);

            using var workspace = TestWorkspace.CreateVisualBasic(text);
            var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);
            var root = document.GetSyntaxRootSynchronously(CancellationToken.None);
            var options = workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService<ISyntaxFormattingService>().GetFormattingOptions(DictionaryAnalyzerConfigOptions.Empty);
            return Formatter.GetFormattedTextChanges(root, workspace.Services, options, CancellationToken.None);
        }
    }
}
