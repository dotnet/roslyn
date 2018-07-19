// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBody;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting
{
    [UseExportProvider]
    public class CodeCleanupTests
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeCleanup)]
        public Task RemoveUsings()
        {
            var code = @"using System;
using System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine();
    }
}
";

            var expected = @"using System;
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine();
    }
}
";
            return AssertCodeCleanupResult(expected, code,
                (CodeCleanupOptions.PerformAdditionalCodeCleanupDuringFormatting, enabled: true),
                (CodeCleanupOptions.RemoveUnusedImports, enabled: true),
                (CodeCleanupOptions.AddAccessibilityModifiers, enabled: false));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeCleanup)]
        public Task SortUsings()
        {
            var code = @"using System.Collections.Generic;
using System;
class Program
{
    static void Main(string[] args)
    {
        var list = new List<int>();
        Console.WriteLine(list.Count);
    }
}
";

            var expected = @"using System;
using System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        var list = new List<int>();
        Console.WriteLine(list.Count);
    }
}
";
            return AssertCodeCleanupResult(expected, code,
                (CodeCleanupOptions.PerformAdditionalCodeCleanupDuringFormatting, enabled: true),
                (CodeCleanupOptions.SortImports, enabled: true),
                (CodeCleanupOptions.ApplyImplicitExplicitTypePreferences, enabled: false),
                (CodeCleanupOptions.AddAccessibilityModifiers, enabled: false));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeCleanup)]
        public Task FixAddRemoveBraces()
        {
            var code = @"class Program
{
    void Method()
    {
        int a = 0;
        if (a > 0)
            a ++;
    }
}
";
            var expected = @"class Program
{
    void Method()
    {
        int a = 0;
        if (a > 0)
        {
            a++;
        }
    }
}
";
            return AssertCodeCleanupResult(expected, code,
                (CodeCleanupOptions.PerformAdditionalCodeCleanupDuringFormatting, enabled: true),
                (CodeCleanupOptions.AddRemoveBracesForSingleLineControlStatements, enabled: true),
                (CodeCleanupOptions.AddAccessibilityModifiers, enabled: false));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeCleanup)]
        public Task RemoveUnusedVariable()
        {
            var code = @"class Program
{
    void Method()
    {
        int a;
    }
}
";
            var expected = @"class Program
{
    void Method()
    {
    }
}
";
            return AssertCodeCleanupResult(expected, code,
                (CodeCleanupOptions.PerformAdditionalCodeCleanupDuringFormatting, enabled: true),
                (CodeCleanupOptions.RemoveUnusedVariables, enabled: true),
                (CodeCleanupOptions.AddAccessibilityModifiers, enabled: false));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeCleanup)]
        public Task FixAccessibilityModifiers()
        {
            var code = @"class Program
{
    void Method()
    {
        int a;
    }
}
";
            var expected = @"internal class Program
{
    private void Method()
    {
        int a;
    }
}
";
            return AssertCodeCleanupResult(expected, code,
                (CodeCleanupOptions.PerformAdditionalCodeCleanupDuringFormatting, enabled: true),
                (CodeCleanupOptions.AddAccessibilityModifiers, enabled: true));
        }

        protected static async Task AssertCodeCleanupResult(string expected, string code, params (PerLanguageOption<bool> option, bool enabled)[] options)
        {
            var exportProvider = ExportProviderCache
                .GetOrCreateExportProviderFactory(
                    TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithParts(typeof(CodeCleanupAnalyzerProviderService)))
                .CreateExportProvider();

            using (var workspace = TestWorkspace.CreateCSharp(code, exportProvider: exportProvider))
            {
                if (options != null)
                {
                    foreach (var option in options)
                    {
                        workspace.Options = workspace.Options.WithChangedOption(option.option, LanguageNames.CSharp, option.enabled);
                    }
                }

                // register this workspace to solution crawler so that analyzer service associate itself with given workspace
                var incrementalAnalyzerProvider = workspace.ExportProvider.GetExportedValue<IDiagnosticAnalyzerService>() as IIncrementalAnalyzerProvider;
                incrementalAnalyzerProvider.CreateIncrementalAnalyzer(workspace);

                var hostdoc = workspace.Documents.Single();
                var document = workspace.CurrentSolution.GetDocument(hostdoc.Id);

                var codeCleanupService = document.GetLanguageService<ICodeCleanupService>();
                var newDoc = await codeCleanupService.CleanupAsync(
                    document, new ProgressTracker(), CancellationToken.None);

                var actual = await newDoc.GetTextAsync();

                Assert.Equal(expected, actual.ToString());
            }
        }

        [Export(typeof(IWorkspaceDiagnosticAnalyzerProviderService))]
        private class CodeCleanupAnalyzerProviderService : IWorkspaceDiagnosticAnalyzerProviderService
        {
            private readonly HostDiagnosticAnalyzerPackage _info;

            [ImportingConstructor]
            public CodeCleanupAnalyzerProviderService()
            {
                _info = new HostDiagnosticAnalyzerPackage("CodeCleanup", GetCompilerAnalyzerAssemblies().Distinct().ToImmutableArray());
            }

            private static IEnumerable<string> GetCompilerAnalyzerAssemblies()
            {
                yield return typeof(CSharpCompilerDiagnosticAnalyzer).Assembly.Location;
                yield return typeof(UseExpressionBodyDiagnosticAnalyzer).Assembly.Location;
            }

            public IAnalyzerAssemblyLoader GetAnalyzerAssemblyLoader()
            {
                return FromFileLoader.Instance;
            }

            public IEnumerable<HostDiagnosticAnalyzerPackage> GetHostDiagnosticAnalyzerPackages()
            {
                yield return _info;
            }

            public class FromFileLoader : IAnalyzerAssemblyLoader
            {
                public static FromFileLoader Instance = new FromFileLoader();

                public void AddDependencyLocation(string fullPath)
                {
                }

                public Assembly LoadFromPath(string fullPath)
                {
                    return Assembly.LoadFrom(fullPath);
                }
            }
        }
    }
}
