// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace IdeBenchmarks
{
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
            return Formatter.GetFormattedTextChanges(document.GetSyntaxRootSynchronously(CancellationToken.None), workspace);
        }

        [Benchmark]
        public object FormatVisualBasic()
        {
            var path = Path.Combine(Path.GetFullPath(@"..\..\..\..\..\src\Compilers\VisualBasic\Portable\Generated"), Document + ".vb");
            var text = File.ReadAllText(path);

            using var workspace = TestWorkspace.CreateVisualBasic(text);
            var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);
            return Formatter.GetFormattedTextChanges(document.GetSyntaxRootSynchronously(CancellationToken.None), workspace);
        }
    }
}
