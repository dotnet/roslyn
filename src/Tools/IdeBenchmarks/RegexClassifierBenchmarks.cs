// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace IdeBenchmarks
{
    public class RegexClassifierBenchmarks
    {
        private readonly UseExportProviderAttribute _useExportProviderAttribute = new UseExportProviderAttribute();

        [Params(0, 1000, 10000)]
        public int StringLength { get; set; }

        [Params('a', '\\')]
        public char RepeatElement { get; set; }

        [IterationSetup]
        public void IterationSetup()
            => _useExportProviderAttribute.Before(null);

        [IterationCleanup]
        public void IterationCleanup()
            => _useExportProviderAttribute.After(null);

        [Benchmark(Baseline = true, Description = "String literal")]
        public object TestStringLiteral()
        {
            var code = CreateTestInput(isRegularExpression: false, element: RepeatElement, length: StringLength);
            return GetClassificationSpansAsync(code, new TextSpan(0, code.Length), parseOptions: null).Result;
        }

        [Benchmark(Description = "Regular expression")]
        public object TestEmptyRegexStringLiteral()
        {
            var code = CreateTestInput(isRegularExpression: true, element: RepeatElement, length: StringLength);
            return GetClassificationSpansAsync(code, new TextSpan(0, code.Length), parseOptions: null).Result;
        }

        private static string CreateTestInput(bool isRegularExpression, char element, int length)
        {
            return @"
class Program
{
    void Method()
    {
        // " + (isRegularExpression ? "l" : "x") + @"anguage=regex
        _ = """ + new string(element, element == '\\' ? 2 * length : length) + @""";
    }
}
";
        }

        protected Task<ImmutableArray<ClassifiedSpan>> GetClassificationSpansAsync(string code, TextSpan span, ParseOptions parseOptions)
        {
            using (var workspace = TestWorkspace.CreateCSharp(code, parseOptions))
            {
                var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);
                return GetSemanticClassificationsAsync(document, span);
            }
        }

        protected static async Task<ImmutableArray<ClassifiedSpan>> GetSemanticClassificationsAsync(Document document, TextSpan span)
        {
            var tree = await document.GetSyntaxTreeAsync();

            var service = document.GetLanguageService<ISyntaxClassificationService>();
            var classifiers = service.GetDefaultSyntaxClassifiers();
            var extensionManager = document.Project.Solution.Workspace.Services.GetService<IExtensionManager>();

            var results = ArrayBuilder<ClassifiedSpan>.GetInstance();

            await service.AddSemanticClassificationsAsync(document, span,
                extensionManager.CreateNodeExtensionGetter(classifiers, c => c.SyntaxNodeTypes),
                extensionManager.CreateTokenExtensionGetter(classifiers, c => c.SyntaxTokenKinds),
                results,
                CancellationToken.None);

            return results.ToImmutableAndFree();
        }
    }
}
