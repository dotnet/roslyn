// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBody;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.CSharp;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.CodeCleanup)]
    public partial class CodeCleanupTests
    {
        [Fact]
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
        List<int> list = new();
        Console.WriteLine(list.Count);
    }
}
";
            return AssertCodeCleanupResult(expected, code);
        }

        [Fact]
        public Task SortGlobalUsings()
        {
            var code = @"using System.Threading.Tasks;
using System.Threading;
global using System.Collections.Generic;
global using System;
class Program
{
    static Task Main(string[] args)
    {
        Barrier b = new Barrier(0);
        var list = new List<int>();
        Console.WriteLine(list.Count);
        b.Dispose();
    }
}
";

            var expected = @"global using System;
global using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

internal class Program
{
    private static Task Main(string[] args)
    {
        Barrier b = new(0);
        List<int> list = new();
        Console.WriteLine(list.Count);
        b.Dispose();
    }
}
";
            return AssertCodeCleanupResult(expected, code);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36984")]
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

        _ = new Goo();
    }
}

namespace M
{
    public class Goo { }
}
";
            return AssertCodeCleanupResult(expected, code, systemUsingsFirst: false, separateUsingGroups: true);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36984")]
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

        _ = new Goo();
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
        public Task FixAddRemoveBraces()
        {
            var code = @"class Program
{
    int Method()
    {
        int a = 0;
        if (a > 0)
            a ++;

        return a;
    }
}
";
            var expected = @"internal class Program
{
    private int Method()
    {
        int a = 0;
        if (a > 0)
        {
            a++;
        }

        return a;
    }
}
";
            return AssertCodeCleanupResult(expected, code);
        }

        [Fact]
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

        [Fact]
        public Task FixUsingPlacementPreferOutside()
        {
            var code = @"namespace A
{
    using System;

    internal class Program
    {
        private void Method()
        {
            Console.WriteLine();
        }
    }
}
";

            var expected = @"using System;

namespace A
{
    internal class Program
    {
        private void Method()
        {
            Console.WriteLine();
        }
    }
}
";

            return AssertCodeCleanupResult(expected, code);
        }

        [Fact]
        public Task FixUsingPlacementPreferInside()
        {
            var code = @"using System;

namespace A
{
    internal class Program
    {
        private void Method()
        {
            Console.WriteLine();
        }
    }
}
";

            var expected = @"namespace A
{
    using System;

    internal class Program
    {
        private void Method()
        {
            Console.WriteLine();
        }
    }
}
";

            return AssertCodeCleanupResult(expected, code, InsideNamespaceOption);
        }

        [Fact]
        public Task FixUsingPlacementPreferInsidePreserve()
        {
            var code = @"using System;

namespace A
{
    internal class Program
    {
        private void Method()
        {
            Console.WriteLine();
        }
    }
}
";

            var expected = code;

            return AssertCodeCleanupResult(expected, code, InsidePreferPreservationOption);
        }

        [Fact]
        public Task FixUsingPlacementPreferOutsidePreserve()
        {
            var code = @"namespace A
{
    using System;

    internal class Program
    {
        private void Method()
        {
            Console.WriteLine();
        }
    }
}
";

            var expected = code;

            return AssertCodeCleanupResult(expected, code, OutsidePreferPreservationOption);
        }

        [Fact]
        public Task FixUsingPlacementMixedPreferOutside()
        {
            var code = @"using System;

namespace A
{
    using System.Collections.Generic;
    
    internal class Program
    {
        private void Method()
        {
            Console.WriteLine();
            List<int> list = new List<int>();
            Console.WriteLine(list.Length);
        }
    }
}
";

            var expected = @"using System;
using System.Collections.Generic;

namespace A
{
    internal class Program
    {
        private void Method()
        {
            Console.WriteLine();
            List<int> list = new();
            Console.WriteLine(list.Length);
        }
    }
}
";

            return AssertCodeCleanupResult(expected, code, OutsideNamespaceOption);
        }

        [Fact]
        public Task FixUsingPlacementMixedPreferInside()
        {
            var code = @"using System;

namespace A
{
    using System.Collections.Generic;
    
    internal class Program
    {
        private void Method()
        {
            Console.WriteLine();
            List<int> list = new();
            Console.WriteLine(list.Length);
        }
    }
}
";

            var expected = @"namespace A
{
    using System;
    using System.Collections.Generic;


    internal class Program
    {
        private void Method()
        {
            Console.WriteLine();
            List<int> list = new();
            Console.WriteLine(list.Length);
        }
    }
}
";

            return AssertCodeCleanupResult(expected, code, InsideNamespaceOption);
        }

        [Fact]
        public Task FixUsingPlacementMixedPreferInsidePreserve()
        {
            var code = @"using System;

namespace A
{
    using System.Collections.Generic;

    internal class Program
    {
        private void Method()
        {
            Console.WriteLine();
            List<int> list = new();
            Console.WriteLine(list.Length);
        }
    }
}
";

            var expected = code;

            return AssertCodeCleanupResult(expected, code, InsidePreferPreservationOption);
        }

        [Fact]
        public Task FixUsingPlacementMixedPreferOutsidePreserve()
        {
            var code = @"using System;

namespace A
{
    using System.Collections.Generic;

    internal class Program
    {
        private void Method()
        {
            Console.WriteLine();
            List<int> list = new();
            Console.WriteLine(list.Length);
        }
    }
}
";

            var expected = code;

            return AssertCodeCleanupResult(expected, code, OutsidePreferPreservationOption);
        }

        [Theory]
        [InlineData(LanguageNames.CSharp)]
        [InlineData(LanguageNames.VisualBasic)]
        public void VerifyAllCodeStyleFixersAreSupportedByCodeCleanup(string language)
        {
            var supportedDiagnostics = GetSupportedDiagnosticIdsForCodeCleanupService(language);

            // No Duplicates
            Assert.Equal(supportedDiagnostics, supportedDiagnostics.Distinct());

            // Exact Number of Unsupported Diagnostic Ids
            var ideDiagnosticIds = typeof(IDEDiagnosticIds).GetFields().Select(f => f.GetValue(f) as string).ToArray();
            var unsupportedDiagnosticIds = ideDiagnosticIds.Except(supportedDiagnostics).ToArray();

            var expectedNumberOfUnsupportedDiagnosticIds =
                language switch
                {
                    LanguageNames.CSharp => 42,
                    LanguageNames.VisualBasic => 79,
                    _ => throw ExceptionUtilities.UnexpectedValue(language),
                };

            Assert.Equal(expectedNumberOfUnsupportedDiagnosticIds, unsupportedDiagnosticIds.Length);
        }

        private const string _code = @"
class C
{
    public void M1(int x, int y)
    {
        switch (x)
        {
            case 1:
            case 10:
                break;
            default:
                break;
        }

        switch (y)
        {
            case 1:
                break;
            case 1000:
            default:
                break;
        }

        switch (x)
        {
            case 1:
                break;
            case 1000:
                break;
        }

        switch (y)
        {
            default:
                break;
        }

        switch (y) { }

        switch (x)
        {
            case :
            case 1000:
                break;
        }
    }
}
";

        private const string _fixed = @"
class C
{
    public void M1(int x, int y)
    {
        switch (x)
        {
            case 1:
            case 10:
                break;
        }

        switch (y)
        {
            case 1:
                break;
        }

        switch (x)
        {
            case 1:
                break;
            case 1000:
                break;
        }

        switch (y)
        {
        }

        switch (y) { }

        switch (x)
        {
            case :
            case 1000:
                break;
        }
    }
}
";

        [Fact]
        public async Task RunThirdPartyFixer()
        {
            await TestThirdPartyCodeFixerApplied<TestThirdPartyCodeFixWithFixAll, CaseTestAnalyzer>(_code, _fixed);
        }

        [Fact]
        public async Task DoNotRunThirdPartyFixerWithNoFixAll()
        {
            await TestThirdPartyCodeFixerNoChanges<TestThirdPartyCodeFixWithOutFixAll, CaseTestAnalyzer>(_code);
        }

        [Theory]
        [InlineData(DiagnosticSeverity.Warning)]
        [InlineData(DiagnosticSeverity.Error)]
        public async Task RunThirdPartyFixerWithSeverityOfWarningOrHigher(DiagnosticSeverity severity)
        {
            await TestThirdPartyCodeFixerApplied<TestThirdPartyCodeFixWithFixAll, CaseTestAnalyzer>(_code, _fixed, severity);
        }

        [Theory]
        [InlineData(DiagnosticSeverity.Hidden)]
        [InlineData(DiagnosticSeverity.Info)]
        public async Task DoNotRunThirdPartyFixerWithSeverityLessThanWarning(DiagnosticSeverity severity)
        {
            await TestThirdPartyCodeFixerNoChanges<TestThirdPartyCodeFixWithOutFixAll, CaseTestAnalyzer>(_code, severity);
        }

        [Fact]
        public async Task DoNotRunThirdPartyFixerIfItDoesNotSupportDocumentScope()
        {
            await TestThirdPartyCodeFixerNoChanges<TestThirdPartyCodeFixDoesNotSupportDocumentScope, CaseTestAnalyzer>(_code);
        }

        [Fact]
        public async Task DoNotApplyFixerIfChangesAreMadeOutsideDocument()
        {
            await TestThirdPartyCodeFixerNoChanges<TestThirdPartyCodeFixModifiesSolution, CaseTestAnalyzer>(_code);
        }

        private static Task TestThirdPartyCodeFixerNoChanges<TCodefix, TAnalyzer>(string code, DiagnosticSeverity severity = DiagnosticSeverity.Warning)
            where TAnalyzer : DiagnosticAnalyzer, new()
            where TCodefix : CodeFixProvider, new()
        {
            return TestThirdPartyCodeFixer<TCodefix, TAnalyzer>(code, code, severity);
        }

        private static Task TestThirdPartyCodeFixerApplied<TCodefix, TAnalyzer>(string code, string expected, DiagnosticSeverity severity = DiagnosticSeverity.Warning)
            where TAnalyzer : DiagnosticAnalyzer, new()
            where TCodefix : CodeFixProvider, new()
        {
            return TestThirdPartyCodeFixer<TCodefix, TAnalyzer>(code, expected, severity);
        }

        private static async Task TestThirdPartyCodeFixer<TCodefix, TAnalyzer>(string code = null, string expected = null, DiagnosticSeverity severity = DiagnosticSeverity.Warning)
            where TAnalyzer : DiagnosticAnalyzer, new()
            where TCodefix : CodeFixProvider, new()
        {

            using var workspace = TestWorkspace.CreateCSharp(code, composition: EditorTestCompositions.EditorFeaturesWpf.AddParts(typeof(TCodefix)));

            var options = CodeActionOptions.DefaultProvider;

            var project = workspace.CurrentSolution.Projects.Single();
            var analyzer = (DiagnosticAnalyzer)new TAnalyzer();
            var diagnosticIds = analyzer.SupportedDiagnostics.SelectAsArray(d => d.Id);

            var editorconfigText = "is_global = true";
            foreach (var diagnosticId in diagnosticIds)
            {
                editorconfigText += $"\ndotnet_diagnostic.{diagnosticId}.severity = {severity.ToEditorConfigString()}";
            }

            var map = new Dictionary<string, ImmutableArray<DiagnosticAnalyzer>>{
                { LanguageNames.CSharp, ImmutableArray.Create(analyzer) }
            };

            project = project.AddAnalyzerReference(new TestAnalyzerReferenceByLanguage(map));
            project = project.Solution.WithProjectFilePath(project.Id, @$"z:\\{project.FilePath}").GetProject(project.Id);
            project = project.AddAnalyzerConfigDocument(".editorconfig", SourceText.From(editorconfigText), filePath: @"z:\\.editorconfig").Project;
            workspace.TryApplyChanges(project.Solution);

            // register this workspace to solution crawler so that analyzer service associate itself with given workspace
            var incrementalAnalyzerProvider = workspace.ExportProvider.GetExportedValue<IDiagnosticAnalyzerService>() as IIncrementalAnalyzerProvider;
            incrementalAnalyzerProvider.CreateIncrementalAnalyzer(workspace);

            var hostdoc = workspace.Documents.Single();
            var document = workspace.CurrentSolution.GetDocument(hostdoc.Id);

            var codeCleanupService = document.GetLanguageService<ICodeCleanupService>();

            var enabledDiagnostics = codeCleanupService.GetAllDiagnostics();

            var newDoc = await codeCleanupService.CleanupAsync(
                document, enabledDiagnostics, new ProgressTracker(), options, CancellationToken.None);

            var actual = await newDoc.GetTextAsync();
            Assert.Equal(expected, actual.ToString());
        }

        private static string[] GetSupportedDiagnosticIdsForCodeCleanupService(string language)
        {
            using var workspace = GetTestWorkspaceForLanguage(language);
            var hostdoc = workspace.Documents.Single();
            var document = workspace.CurrentSolution.GetDocument(hostdoc.Id);

            var codeCleanupService = document.GetLanguageService<ICodeCleanupService>();

            var enabledDiagnostics = codeCleanupService.GetAllDiagnostics();
            var supportedDiagnostics = enabledDiagnostics.Diagnostics.SelectMany(x => x.DiagnosticIds).ToArray();
            return supportedDiagnostics;

            TestWorkspace GetTestWorkspaceForLanguage(string language)
            {
                if (language == LanguageNames.CSharp)
                {
                    return TestWorkspace.CreateCSharp(string.Empty, composition: EditorTestCompositions.EditorFeaturesWpf);
                }

                if (language == LanguageNames.VisualBasic)
                {
                    return TestWorkspace.CreateVisualBasic(string.Empty, composition: EditorTestCompositions.EditorFeaturesWpf);
                }

                return null;
            }
        }

        /// <summary>
        /// Assert the expected code value equals the actual processed input <paramref name="code"/>.
        /// </summary>
        /// <param name="expected">The actual processed code to verify against.</param>
        /// <param name="code">The input code to be processed and tested.</param>
        /// <param name="systemUsingsFirst">Indicates whether <c><see cref="System"/>.*</c> '<c>using</c>' directives should preceed others. Default is <c>true</c>.</param>
        /// <param name="separateUsingGroups">Indicates whether '<c>using</c>' directives should be organized into separated groups. Default is <c>true</c>.</param>
        /// <returns>The <see cref="Task"/> to test code cleanup.</returns>
        private protected static Task AssertCodeCleanupResult(string expected, string code, bool systemUsingsFirst = true, bool separateUsingGroups = false)
            => AssertCodeCleanupResult(expected, code, new(AddImportPlacement.OutsideNamespace, NotificationOption2.Silent), systemUsingsFirst, separateUsingGroups);

        /// <summary>
        /// Assert the expected code value equals the actual processed input <paramref name="code"/>.
        /// </summary>
        /// <param name="expected">The actual processed code to verify against.</param>
        /// <param name="code">The input code to be processed and tested.</param>
        /// <param name="preferredImportPlacement">Indicates the code style option for the preferred 'using' directives placement.</param>
        /// <param name="systemUsingsFirst">Indicates whether <c><see cref="System"/>.*</c> '<c>using</c>' directives should preceed others. Default is <c>true</c>.</param>
        /// <param name="separateUsingGroups">Indicates whether '<c>using</c>' directives should be organized into separated groups. Default is <c>true</c>.</param>
        /// <returns>The <see cref="Task"/> to test code cleanup.</returns>
        private protected static async Task AssertCodeCleanupResult(string expected, string code, CodeStyleOption2<AddImportPlacement> preferredImportPlacement, bool systemUsingsFirst = true, bool separateUsingGroups = false)
        {
            using var workspace = TestWorkspace.CreateCSharp(code, composition: EditorTestCompositions.EditorFeaturesWpf);

            // must set global options since incremental analyzer infra reads from global options
            var globalOptions = workspace.GlobalOptions;
            globalOptions.SetGlobalOption(GenerationOptions.SeparateImportDirectiveGroups, LanguageNames.CSharp, separateUsingGroups);
            globalOptions.SetGlobalOption(GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.CSharp, systemUsingsFirst);
            globalOptions.SetGlobalOption(CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, preferredImportPlacement);

            var solution = workspace.CurrentSolution.WithAnalyzerReferences(new[]
            {
                new AnalyzerFileReference(typeof(CSharpCompilerDiagnosticAnalyzer).Assembly.Location, TestAnalyzerAssemblyLoader.LoadFromFile),
                new AnalyzerFileReference(typeof(UseExpressionBodyDiagnosticAnalyzer).Assembly.Location, TestAnalyzerAssemblyLoader.LoadFromFile)
            });

            workspace.TryApplyChanges(solution);

            // register this workspace to solution crawler so that analyzer service associate itself with given workspace
            var incrementalAnalyzerProvider = workspace.ExportProvider.GetExportedValue<IDiagnosticAnalyzerService>() as IIncrementalAnalyzerProvider;
            incrementalAnalyzerProvider.CreateIncrementalAnalyzer(workspace);

            var hostdoc = workspace.Documents.Single();
            var document = workspace.CurrentSolution.GetDocument(hostdoc.Id);

            var codeCleanupService = document.GetLanguageService<ICodeCleanupService>();

            var enabledDiagnostics = codeCleanupService.GetAllDiagnostics();

            var newDoc = await codeCleanupService.CleanupAsync(
                document, enabledDiagnostics, new ProgressTracker(), globalOptions.CreateProvider(), CancellationToken.None);

            var actual = await newDoc.GetTextAsync();

            Assert.Equal(expected, actual.ToString());
        }

        private static readonly CodeStyleOption2<AddImportPlacement> InsideNamespaceOption =
            new CodeStyleOption2<AddImportPlacement>(AddImportPlacement.InsideNamespace, NotificationOption2.Error);

        private static readonly CodeStyleOption2<AddImportPlacement> OutsideNamespaceOption =
            new CodeStyleOption2<AddImportPlacement>(AddImportPlacement.OutsideNamespace, NotificationOption2.Error);

        private static readonly CodeStyleOption2<AddImportPlacement> InsidePreferPreservationOption =
            new CodeStyleOption2<AddImportPlacement>(AddImportPlacement.InsideNamespace, NotificationOption2.None);

        private static readonly CodeStyleOption2<AddImportPlacement> OutsidePreferPreservationOption =
            new CodeStyleOption2<AddImportPlacement>(AddImportPlacement.OutsideNamespace, NotificationOption2.None);
    }
}
