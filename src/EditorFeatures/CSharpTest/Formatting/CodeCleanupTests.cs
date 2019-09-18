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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
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

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine();
    }
}
";
            return AssertCodeCleanupResult(expected, code);
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

internal class Program
{
    private static void Main(string[] args)
    {
        List<int> list = new List<int>();
        Console.WriteLine(list.Count);
    }
}
";
            return AssertCodeCleanupResult(expected, code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeCleanup)]
        public Task GroupUsings()
        {
            var code = @"using M;
using System;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine(""Hello World!"");

        new Goo();
    }
}

namespace M
{
    public class Goo { }
}
";

            var expected = @"using M;

using System;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine(""Hello World!"");

        new Goo();
    }
}

namespace M
{
    public class Goo { }
}
";
            return AssertCodeCleanupResult(expected, code, systemUsingsFirst: false, separateUsingGroups: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeCleanup)]
        public Task SortAndGroupUsings()
        {
            var code = @"using M;
using System;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine(""Hello World!"");

        new Goo();
    }
}

namespace M
{
    public class Goo { }
}
";

            var expected = @"using System;

using M;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine(""Hello World!"");

        new Goo();
    }
}

namespace M
{
    public class Goo { }
}
";
            return AssertCodeCleanupResult(expected, code, systemUsingsFirst: true, separateUsingGroups: true);
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
            var expected = @"internal class Program
{
    private void Method()
    {
        int a = 0;
        if (a > 0)
        {
            a++;
        }
    }
}
";
            return AssertCodeCleanupResult(expected, code);
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
            var expected = @"internal class Program
{
    private void Method()
    {
    }
}
";
            return AssertCodeCleanupResult(expected, code);
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
    }
}
";
            return AssertCodeCleanupResult(expected, code);
        }

        protected static async Task AssertCodeCleanupResult(string expected, string code, bool systemUsingsFirst = true, bool separateUsingGroups = false)
        {
            var exportProvider = ExportProviderCache
                .GetOrCreateExportProviderFactory(
                    TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithParts(typeof(CodeCleanupAnalyzerProviderService)))
                .CreateExportProvider();

            using var workspace = TestWorkspace.CreateCSharp(code, exportProvider: exportProvider);
            workspace.Options = workspace.Options.WithChangedOption(GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.CSharp, systemUsingsFirst);
            workspace.Options = workspace.Options.WithChangedOption(GenerationOptions.SeparateImportDirectiveGroups, LanguageNames.CSharp, separateUsingGroups);

            // register this workspace to solution crawler so that analyzer service associate itself with given workspace
            var incrementalAnalyzerProvider = workspace.ExportProvider.GetExportedValue<IDiagnosticAnalyzerService>() as IIncrementalAnalyzerProvider;
            incrementalAnalyzerProvider.CreateIncrementalAnalyzer(workspace);

            var hostdoc = workspace.Documents.Single();
            var document = workspace.CurrentSolution.GetDocument(hostdoc.Id);

            var codeCleanupService = document.GetLanguageService<ICodeCleanupService>();

            var enabledDiagnostics = codeCleanupService.GetAllDiagnostics();

            var newDoc = await codeCleanupService.CleanupAsync(
                document, enabledDiagnostics, new ProgressTracker(), CancellationToken.None);

            var actual = await newDoc.GetTextAsync();

            Assert.Equal(expected, actual.ToString());
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
