// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.ReleaseTracking;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic.Testing;
using Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class ReleaseTrackingAnalyzerTests
    {
        [Fact]
        public async Task TestNoDeclaredAnalyzersAsync()
        {
            var source = @"";

            var shippedText = @"";
            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [InlineData(@"{|RS2008:""Id1""|}", null, null)]
        [InlineData(@"""Id1""", "", null)]
        [InlineData(@"""Id1""", null, "")]
        [InlineData(@"{|RS2000:""Id1""|}", "", "")]
        [Theory]
        public async Task TestMissingReleasesFilesAsync(string id, string shippedText, string unshippedText)
        {
            var source = $@"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{{
    // Enabled by default descriptor.
    private static readonly DiagnosticDescriptor descriptor1 =
        new DiagnosticDescriptor({id}, ""Title1"", ""Message1"", ""Category1"", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor1);
    public override void Initialize(AnalysisContext context) {{ }}
}}";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        public async Task TestCodeFixToEnableAnalyzerReleaseTrackingAsync()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor descriptor1 =
        new DiagnosticDescriptor({|RS2008:""Id1""|}, ""Title1"", ""Message1"", ""Category1"", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor1);
    public override void Initialize(AnalysisContext context) { }
}";

            var test = new CSharpCodeFixVerifier<DiagnosticDescriptorCreationAnalyzer, AnalyzerReleaseTrackingFix>.Test()
            {
                ReferenceAssemblies = AdditionalMetadataReferences.Default,
                NumberOfIncrementalIterations = 2,
                NumberOfFixAllIterations = 2,
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { source },
                    AdditionalFiles = {
                        (ReleaseTrackingHelper.ShippedFileName,
                            AnalyzerReleaseTrackingFix.ShippedAnalyzerReleaseTrackingFileDefaultContent),
                        (ReleaseTrackingHelper.UnshippedFileName,
                            AnalyzerReleaseTrackingFix.UnshippedAnalyzerReleaseTrackingFileDefaultContent + DefaultUnshippedHeader + "Id1 | Category1 | Warning | MyAnalyzer")
                    }
                },
            };

            test.SolutionTransforms.Add(DisableNonReleaseTrackingWarnings);
            await test.RunAsync();
        }

        // Unshipped release with existing new rules table.
        [InlineData("", DefaultUnshippedHeader + "Id1 | Category1 | Warning |")]
        // Shipped release with existing new rules table.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |", "")]
        // Releases with separate new rules and changed rules table.
        [InlineData(DefaultShippedHeader + "Id1 | Category0 | Warning |" + BlankLine +
                    DefaultChangedShippedHeader2 + "Id1 | Category1 | Warning | Category0 | Warning |", "")]
        // Releases with separate new rules and removed rules table.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |" + BlankLine + "Id2 | Category1 | Warning |" + BlankLine +
                    DefaultRemovedShippedHeader2 + "Id2 | Category1 | Warning ", "")]
        // Release with new rules and changed rules table.
        [InlineData(DefaultShippedHeader + "Id2 | Category0 | Warning |" + BlankLine +
                    DefaultShippedHeader2 + "Id1 | Category1 | Warning |" + BlankLine + DefaultChangedUnshippedHeader + "Id2 | Category1 | Warning | Category0 | Warning |",
                    DefaultRemovedUnshippedHeader + "Id2 | Category1 | Warning |")]
        // Release with new rules and removed rules table.
        [InlineData(DefaultShippedHeader + "Id2 | Category0 | Warning |" + BlankLine +
                    DefaultShippedHeader2 + "Id1 | Category1 | Warning |" + BlankLine + DefaultRemovedUnshippedHeader + "Id2 | Category0 | Warning |", "")]
        // Release with all 3 tables
        [InlineData(DefaultShippedHeader + "Id3 | Category1 | Warning |" + BlankLine + "Id2 | Category0 | Warning |" + BlankLine +
                    DefaultShippedHeader2 + "Id1 | Category1 | Warning |" + BlankLine + DefaultChangedUnshippedHeader + "Id3 | Category2 | Warning | Category1 | Warning |" + BlankLine + DefaultRemovedUnshippedHeader + "Id2 | Category1 | Warning |",
                    DefaultRemovedUnshippedHeader + "Id3 | Category2 | Warning |")]
        [Theory]
        public async Task TestReleasesFileAlreadyHasEntryAsync(string shippedText, string unshippedText)
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    // Enabled by default descriptor.
    private static readonly DiagnosticDescriptor descriptor1 =
        new DiagnosticDescriptor(""Id1"", ""Title1"", ""Message1"", ""Category1"", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor1);
    public override void Initialize(AnalysisContext context) {{ }}
}";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        public async Task TestRemoveUnshippedDeletedDiagnosticIdRuleAsync()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray<DiagnosticDescriptor>.Empty;
    public override void Initialize(AnalysisContext context) {{ }} 
}";
            var shippedText = @"";
            var unshippedText = DefaultUnshippedHeader + "Id1 | Category1 | Warning |";

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                new DiagnosticResult(DiagnosticDescriptorCreationAnalyzer.RemoveUnshippedDeletedDiagnosticIdRule).WithArguments("Id1"));
        }

        [Fact]
        public async Task TestRemoveShippedDeletedDiagnosticIdRuleAsync()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray<DiagnosticDescriptor>.Empty;
    public override void Initialize(AnalysisContext context) {{ }} 
}";
            var shippedText = DefaultShippedHeader + "Id1 | Category1 | Warning |";
            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                new DiagnosticResult(DiagnosticDescriptorCreationAnalyzer.RemoveShippedDeletedDiagnosticIdRule).WithArguments("Id1", "1.0"));
        }

        [Fact]
        public async Task TestCodeFixToAddUnshippedEntriesAsync()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    // Enabled by default descriptor.
    private static readonly DiagnosticDescriptor descriptor1 =
        new DiagnosticDescriptor({|RS2000:""Id1""|}, ""Title1"", ""Message1"", ""Category1"", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    // Duplicate descriptor with different message.
    private static readonly DiagnosticDescriptor descriptor1_dupe =
        new DiagnosticDescriptor({|RS2000:""Id1""|}, ""Title1"", ""DifferentMessage"", ""Category1"", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    // Disabled by default descriptor.
    private static readonly DiagnosticDescriptor descriptor2 =
        new DiagnosticDescriptor({|RS2000:""Id2""|}, ""Title2"", ""Message2"", ""Category2"", DiagnosticSeverity.Warning, isEnabledByDefault: false);

    // Descriptor with help link.
    private static readonly DiagnosticDescriptor descriptor3 =
        new DiagnosticDescriptor({|RS2000:""Id3""|}, ""Title3"", ""Message3"", ""Category3"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""Dummy"");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor1, descriptor1_dupe, descriptor2, descriptor3);
    public override void Initialize(AnalysisContext context) { }
}";

            var shippedText = @"";
            var unshippedText = @"";
            var fixedUnshippedText =
$@"{DefaultUnshippedHeader}Id1 | Category1 | Warning | MyAnalyzer
Id2 | Category2 | Disabled | MyAnalyzer
Id3 | Category3 | Warning | MyAnalyzer, [Documentation](Dummy)";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestCodeFixToAddUnshippedEntries_DiagnosticDescriptorHelperAsync()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    // Enabled by default descriptor.
    private static readonly DiagnosticDescriptor descriptor1 =
        DiagnosticDescriptorHelper.Create({|RS2000:""Id1""|}, ""Title1"", ""Message1"", ""Category1"", RuleLevel.BuildWarning);

    // Duplicate descriptor with different message.
    private static readonly DiagnosticDescriptor descriptor1_dupe =
        DiagnosticDescriptorHelper.Create({|RS2000:""Id1""|}, ""Title1"", ""DifferentMessage"", ""Category1"", RuleLevel.BuildWarning);

    // Disabled by default descriptor.
    private static readonly DiagnosticDescriptor descriptor2 =
        DiagnosticDescriptorHelper.Create({|RS2000:""Id2""|}, ""Title2"", ""Message2"", ""Category2"", RuleLevel.Disabled);

    // Descriptor with help link.
    private static readonly DiagnosticDescriptor descriptor3 =
        DiagnosticDescriptorHelper.Create({|RS2000:""Id3""|}, ""Title3"", ""Message3"", ""Category3"", RuleLevel.BuildWarning, helpLinkUri: ""Dummy"");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor1, descriptor1_dupe, descriptor2, descriptor3);
    public override void Initialize(AnalysisContext context) { }
}" + CSharpDiagnosticDescriptorCreationHelper;

            var shippedText = @"";
            var unshippedText = @"";
            var fixedUnshippedText =
$@"{DefaultUnshippedHeader}Id1 | Category1 | Warning | MyAnalyzer
Id2 | Category2 | Disabled | MyAnalyzer
Id3 | Category3 | Warning | MyAnalyzer, [Documentation](Dummy)";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        private const string BlankLine = @"
";

        // Comments
        [InlineData(DefaultUnshippedHeader + @"; Comments are preserved" + BlankLine,
                    DefaultUnshippedHeader + @"; Comments are preserved" + BlankLine + @"Id1 | Category1 | Warning | MyAnalyzer" + BlankLine)]
        // Blank lines are preserved
        [InlineData(DefaultUnshippedHeader + BlankLine,
                    DefaultUnshippedHeader + @"Id1 | Category1 | Warning | MyAnalyzer" + BlankLine + BlankLine)]
        // Mixed
        [InlineData(DefaultUnshippedHeader + BlankLine + @"; Comments are preserved" + BlankLine,
                    DefaultUnshippedHeader + @"Id1 | Category1 | Warning | MyAnalyzer" + BlankLine + BlankLine + @"; Comments are preserved" + BlankLine)]
        [Theory]
        public async Task TestCodeFixToAddUnshippedEntries_TriviaIsPreservedAsync(string unshippedText, string fixedUnshippedText)
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor descriptor1 =
        new DiagnosticDescriptor({|RS2000:""Id1""|}, ""Title1"", ""Message1"", ""Category1"", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor1);
    public override void Initialize(AnalysisContext context) { }
}";

            var shippedText = @"";
            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        // Added after current entry.
        [InlineData("Id0", DefaultUnshippedHeader + @"Id0 | DifferentCategory | Warning | MyAnalyzer",
                           DefaultUnshippedHeader + @"Id0 | DifferentCategory | Warning | MyAnalyzer" + BlankLine + @"Id1 | Category1 | Warning | MyAnalyzer")]
        // Added before current entry.
        [InlineData("Id2", DefaultUnshippedHeader + @"Id2 | DifferentCategory | Warning | MyAnalyzer",
                           DefaultUnshippedHeader + @"Id1 | Category1 | Warning | MyAnalyzer" + BlankLine + @"Id2 | DifferentCategory | Warning | MyAnalyzer")]
        // Has existing changed rules table.
        [InlineData("Id2", DefaultChangedUnshippedHeader + @"Id2 | DifferentCategory | Warning | Category | Warning | MyAnalyzer",
                           DefaultUnshippedHeader + @"Id1 | Category1 | Warning | MyAnalyzer" + BlankLine + BlankLine + DefaultChangedUnshippedHeader + @"Id2 | DifferentCategory | Warning | Category | Warning | MyAnalyzer",
                           DefaultShippedHeader + @"Id2 | Category | Warning | MyAnalyzer")]
        // Has existing removed rules table.
        [InlineData("Id2", DefaultRemovedUnshippedHeader + @"Id3 | Category | Warning | MyAnalyzer",
                           DefaultUnshippedHeader + @"Id1 | Category1 | Warning | MyAnalyzer" + BlankLine + BlankLine + DefaultRemovedUnshippedHeader + @"Id3 | Category | Warning | MyAnalyzer",
                           DefaultShippedHeader + @"Id2 | DifferentCategory | Warning | MyAnalyzer" + BlankLine + @"Id3 | Category | Warning | MyAnalyzer")]
        [Theory]
        public async Task TestCodeFixToAddUnshippedEntries_AlreadyHasDifferentUnshippedEntriesAsync(string differentRuleId, string unshippedText, string fixedUnshippedText, string shippedText = "")
        {
            var source = $@"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{{
    private static readonly DiagnosticDescriptor descriptor1 =
        new DiagnosticDescriptor({{|RS2000:""Id1""|}}, ""Title1"", ""Message1"", ""Category1"", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor descriptor2 =
        new DiagnosticDescriptor(""{differentRuleId}"", ""DifferentTitle"", ""DifferentMessage"", ""DifferentCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor1, descriptor2);
    public override void Initialize(AnalysisContext context) {{ }}
}}";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        // Adds to existing new rules table and creates a new changed rules table.
        [InlineData(DefaultUnshippedHeader + @"Id0 | Category0 | Warning | MyAnalyzer",
                    DefaultUnshippedHeader + @"Id0 | Category0 | Warning | MyAnalyzer" + BlankLine + @"Id1 | Category1 | Warning | MyAnalyzer" + BlankLine + BlankLine + DefaultChangedUnshippedHeader + @"Id2 | DifferentCategory | Warning | Category | Warning | MyAnalyzer",
                    DefaultShippedHeader + @"Id2 | Category | Warning | MyAnalyzer")]
        // Adds to existing new rules table and changed rules table.
        [InlineData(DefaultChangedUnshippedHeader + @"Id0 | Category0 | Warning | Category | Warning | MyAnalyzer",
                    DefaultUnshippedHeader + @"Id1 | Category1 | Warning | MyAnalyzer" + BlankLine + BlankLine + DefaultChangedUnshippedHeader + @"Id0 | Category0 | Warning | Category | Warning | MyAnalyzer" + BlankLine + @"Id2 | DifferentCategory | Warning | Category | Warning | MyAnalyzer",
                    DefaultShippedHeader + @"Id0 | Category | Warning | MyAnalyzer" + BlankLine + @"Id2 | Category | Warning | MyAnalyzer")]
        [Theory]
        public async Task TestCodeFixToAddUnshippedEntriesToMultipleTablesAsync(string unshippedText, string fixedUnshippedText, string shippedText = "")
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor descriptor0 =
        new DiagnosticDescriptor(""Id0"", ""Title0"", ""Message0"", ""Category0"", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor descriptor1 =
        new DiagnosticDescriptor({|RS2000:""Id1""|}, ""Title1"", ""Message1"", ""Category1"", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor descriptor2 =
        new DiagnosticDescriptor({|RS2001:""Id2""|}, ""DifferentTitle"", ""DifferentMessage"", ""DifferentCategory"", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor0, descriptor1, descriptor2);
    public override void Initialize(AnalysisContext context) { }
}";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [InlineData("",
                    DefaultUnshippedHeader + "Id1 | Category1 | Warning | MyAnalyzer",
                    "RS2000")]
        [InlineData(DefaultShippedHeader + "Id1 | DifferentCategory | Warning | MyAnalyzer",
                    DefaultChangedUnshippedHeader + "Id1 | Category1 | Warning | DifferentCategory | Warning | MyAnalyzer",
                    "RS2001")]
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Disabled | MyAnalyzer",
                    DefaultChangedUnshippedHeader + "Id1 | Category1 | Warning | Category1 | Disabled | MyAnalyzer",
                    "RS2001")]
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Info |" + BlankLine + DefaultRemovedShippedHeader2 + "Id1 | Category1 | Info | MyAnalyzer",
                    DefaultUnshippedHeader + "Id1 | Category1 | Warning | MyAnalyzer",
                    "RS2000")]
        [Theory]
        public async Task TestCodeFixToAddUnshippedEntries_AlreadyHasDifferentShippedEntryAsync(string shippedText, string fixedUnshippedText, string expectedDiagnosticId)
        {
            var source = $@"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{{
    // Enabled by default descriptor.
    private static readonly DiagnosticDescriptor descriptor1 =
        new DiagnosticDescriptor({{|{expectedDiagnosticId}:""Id1""|}}, ""Title1"", ""Message1"", ""Category1"", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor1);
    public override void Initialize(AnalysisContext context) {{ }}
}}";

            var unshippedText = @"";
            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestCodeFixToUpdateMultipleUnshippedEntriesAsync()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    // Enabled by default descriptor.
    private static readonly DiagnosticDescriptor descriptor1 =
        new DiagnosticDescriptor({|RS2001:""Id1""|}, ""Title1"", ""Message1"", ""Category1"", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    // Disable by default descriptor.
    private static readonly DiagnosticDescriptor descriptor2 =
        new DiagnosticDescriptor({|RS2001:""Id2""|}, ""Title2"", ""Message2"", ""Category2"", DiagnosticSeverity.Warning, isEnabledByDefault: false);

    // Descriptor with help - ensure that just adding a help link does not require a new analyzer release entry.
    private static readonly DiagnosticDescriptor descriptor3 =
        new DiagnosticDescriptor(""Id3"", ""Title3"", ""Message3"", ""Category3"", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: ""Dummy"");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor1, descriptor2, descriptor3);
    public override void Initialize(AnalysisContext context) { }
}";

            var shippedText = @"";

            var unshippedText =
$@"{DefaultUnshippedHeader}Id1 | DifferentCategory | Warning | MyAnalyzer
Id2 | Category2 | Warning | MyAnalyzer
Id3 | Category3 | Warning | MyAnalyzer";

            var fixedUnshippedText =
$@"{DefaultUnshippedHeader}Id1 | Category1 | Warning | MyAnalyzer
Id2 | Category2 | Disabled | MyAnalyzer
Id3 | Category3 | Warning | MyAnalyzer";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestCodeFixToAddUnshippedEntries_UndetectedFieldsAsync()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public static class DiagnosticDescriptorHelper
{
    public static DiagnosticDescriptor Create(
        string id,
        LocalizableString title,
        LocalizableString messageFormat)
    => null;
}

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor descriptor1 =
        DiagnosticDescriptorHelper.Create({|RS2000:""Id1""|}, ""Title1"", ""Message1"");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor1);
    public override void Initialize(AnalysisContext context) { }
}";

            var shippedText = @"";
            var unshippedText = @"";
            var entry = $@"Id1 | {ReleaseTrackingHelper.UndetectedText} | {ReleaseTrackingHelper.UndetectedText} | MyAnalyzer";
            var fixedUnshippedText = $@"{DefaultUnshippedHeader}{entry}";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText, additionalExpectedDiagnosticsInInput: ImmutableArray<DiagnosticResult>.Empty,
                additionalExpectedDiagnosticsInResult: ImmutableArray.Create(
                    GetAdditionalFileResultAt(5, 1,
                        ReleaseTrackingHelper.UnshippedFileName,
                        DiagnosticDescriptorCreationAnalyzer.InvalidUndetectedEntryInAnalyzerReleasesFileRule,
                        ReleaseTrackingHelper.UnshippedFileName,
                        entry)));
        }

        [Fact]
        public async Task TestNoCodeFixToAddUnshippedEntries_UndetectedFieldsAsync()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public static class DiagnosticDescriptorHelper
{
    public static DiagnosticDescriptor Create(
        string id,
        LocalizableString title,
        LocalizableString messageFormat)
    => null;
}

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor descriptor1 =
        DiagnosticDescriptorHelper.Create(""Id1"", ""Title1"", ""Message1"");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor1);
    public override void Initialize(AnalysisContext context) { }
}";

            var shippedText = @"";
            var unshippedText = $@"{DefaultUnshippedHeader}Id1 | CustomCategory | Warning |";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        // No header in unshipped
        [InlineData("", "Id1 | Category1 | Warning |")]
        // No header in shipped
        [InlineData("Id1 | Category1 | Warning |", "")]
        // Missing TableTitle in unshipped
        [InlineData("", ReleaseTrackingHelper.TableHeaderNewOrRemovedRulesLine1 + BlankLine + "Id1 | Category1 | Warning |")]
        // Missing TableHeaderLine1 in unshipped
        [InlineData("", ReleaseTrackingHelper.TableTitleNewRules + BlankLine + ReleaseTrackingHelper.TableHeaderNewOrRemovedRulesLine2 + BlankLine + "Id1 | Category1 | Warning |", 2)]
        // Missing TableHeaderLine1 with minimum markdown column hyphens in unshipped
        [InlineData("", ReleaseTrackingHelper.TableTitleNewRules + BlankLine + @"---|---|---|---" + BlankLine + "Id1 | Category1 | Warning |", 2)]
        // Missing TableHeaderLine2 in unshipped
        [InlineData("", ReleaseTrackingHelper.TableTitleNewRules + BlankLine + ReleaseTrackingHelper.TableHeaderNewOrRemovedRulesLine1 + BlankLine + "Id1 | Category1 | Warning |", 3)]
        // Missing TableHeaderLine2 with extra column header text spaces in unshipped
        [InlineData("", ReleaseTrackingHelper.TableTitleNewRules + BlankLine + @"Rule ID  | Category  | Severity  | Notes " + BlankLine + "Id1 | Category1 | Warning |", 3)]
        // Missing Release Version line in shipped
        [InlineData(DefaultUnshippedHeader + "Id1 | Category1 | Warning |", "")]
        // Missing Release Version in shipped
        [InlineData(ReleaseTrackingHelper.ReleasePrefix + BlankLine + DefaultUnshippedHeader + "Id1 | Category1 | Warning |", "")]
        // Invalid Release Version in shipped
        [InlineData(ReleaseTrackingHelper.ReleasePrefix + " InvalidVersion" + BlankLine + DefaultUnshippedHeader + "Id1 | Category1 | Warning |", "")]
        // Missing TableTitle in shipped
        [InlineData(ReleaseTrackingHelper.ReleasePrefix + "1.0" + BlankLine + ReleaseTrackingHelper.TableHeaderChangedRulesLine1 + BlankLine + "Id1 | Category1 | Warning |", "", 2)]
        // Missing TableHeaderLine1 in shipped
        [InlineData(ReleaseTrackingHelper.ReleasePrefix + "1.0" + BlankLine + ReleaseTrackingHelper.TableTitleChangedRules + BlankLine + ReleaseTrackingHelper.TableHeaderChangedRulesLine2 + BlankLine + "Id1 | Category1 | Warning |", "", 3)]
        // Missing TableHeaderLine1 with minimum markdown column hyphens in shipped
        [InlineData(ReleaseTrackingHelper.ReleasePrefix + "1.0" + BlankLine + ReleaseTrackingHelper.TableTitleChangedRules + BlankLine + @"---|---|---|---|---|---" + BlankLine + "Id1 | Category1 | Warning |", "", 3)]
        // Missing TableHeaderLine2 in shipped
        [InlineData(ReleaseTrackingHelper.ReleasePrefix + " 1.0" + BlankLine + ReleaseTrackingHelper.TableTitleChangedRules + BlankLine + ReleaseTrackingHelper.TableHeaderChangedRulesLine1 + BlankLine + "Id1 | Category1 | Warning |", "", 4)]
        // Missing TableHeaderLine2 in with extra column header text spaces shipped
        [InlineData(ReleaseTrackingHelper.ReleasePrefix + " 1.0" + BlankLine + ReleaseTrackingHelper.TableTitleChangedRules + BlankLine + @"Rule ID  | New Category  | New Severity  | Old Category  | Old Severity  | Notes" + BlankLine + "Id1 | Category1 | Warning |", "", 4)]
        // Invalid Release Version line in unshipped
        [InlineData("", DefaultShippedHeader + "Id1 | Category1 | Warning |")]
        // Mismatch Table title and TableHeaderLine1 in unshipped
        [InlineData("", ReleaseTrackingHelper.TableTitleNewRules + BlankLine + ReleaseTrackingHelper.TableHeaderChangedRulesLine1 + BlankLine + ReleaseTrackingHelper.TableHeaderChangedRulesLine2 + BlankLine + "Id1 | Category1 | Warning |", 2)]
        // Mismatch Table title and TableHeaderLine2 in unshipped
        [InlineData("", ReleaseTrackingHelper.TableTitleChangedRules + BlankLine + ReleaseTrackingHelper.TableHeaderChangedRulesLine1 + BlankLine + ReleaseTrackingHelper.TableHeaderNewOrRemovedRulesLine2 + BlankLine + "Id1 | Category1 | Warning |", 3)]
        [Theory]
        public async Task TestInvalidHeaderDiagnosticAsync(string shippedText, string unshippedText, int line = 1)
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor descriptor1 =
        new DiagnosticDescriptor(""Id1"", ""Title1"", ""Message1"", ""Category1"", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor1);
    public override void Initialize(AnalysisContext context) { }
}";

            var fileWithDiagnostics = shippedText.Length > 0 ? ReleaseTrackingHelper.ShippedFileName : ReleaseTrackingHelper.UnshippedFileName;
            var diagnosticText = (shippedText.Length > 0 ? shippedText : unshippedText).Split(new[] { Environment.NewLine }, StringSplitOptions.None).ElementAt(line - 1);
            await VerifyCSharpAsync(source, shippedText, unshippedText,
                    GetAdditionalFileResultAt(line, 1,
                        fileWithDiagnostics,
                        DiagnosticDescriptorCreationAnalyzer.InvalidHeaderInAnalyzerReleasesFileRule,
                        fileWithDiagnostics,
                        diagnosticText));
        }

        // Undetected category
        [InlineData("Id1 | " + ReleaseTrackingHelper.UndetectedText + " | Warning |", true)]
        // Undetected severity
        [InlineData("Id1 | Category1 | " + ReleaseTrackingHelper.UndetectedText + " |", true)]
        // Undetected category and severity
        [InlineData("Id1 | " + ReleaseTrackingHelper.UndetectedText + " | " + ReleaseTrackingHelper.UndetectedText + " |", true)]
        // Invalid severity
        [InlineData("Id1 | Category1 | Invalid |", false)]
        // Missing required fields - category + severity
        [InlineData("Id1", false)]
        // Missing required field - category
        [InlineData("Id1 | Warning ", false)]
        // Missing required field - severity
        [InlineData("Id1 | Category1 |", false)]
        // Extra fields
        [InlineData("Id1 | Category1 | Warning | Notes | InvalidField", false)]
        [Theory]
        public async Task TestInvalidEntryDiagnosticAsync(string entry, bool hasUndetectedField)
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor descriptor1 =
        new DiagnosticDescriptor(""Id1"", ""Title1"", ""Message1"", ""Category1"", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor1);
    public override void Initialize(AnalysisContext context) { }
}";
            var rule = hasUndetectedField ?
                DiagnosticDescriptorCreationAnalyzer.InvalidUndetectedEntryInAnalyzerReleasesFileRule :
                DiagnosticDescriptorCreationAnalyzer.InvalidEntryInAnalyzerReleasesFileRule;

            var shippedText = @"";
            var unshippedText = DefaultUnshippedHeader + entry;

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                    GetAdditionalFileResultAt(5, 1,
                        ReleaseTrackingHelper.UnshippedFileName,
                        rule,
                        ReleaseTrackingHelper.UnshippedFileName,
                        entry));
        }

        // Undetected category
        [InlineData("Id1 | " + ReleaseTrackingHelper.UndetectedText + " | Warning | Category1 | Warning |", true)]
        // Undetected old category
        [InlineData("Id1 | Category1 | Warning | " + ReleaseTrackingHelper.UndetectedText + " | Warning |", true)]
        // Undetected severity
        [InlineData("Id1 | Category1 | " + ReleaseTrackingHelper.UndetectedText + " | Category1 | Warning |", true)]
        // Undetected old severity
        [InlineData("Id1 | Category1 | Warning | Category1 | " + ReleaseTrackingHelper.UndetectedText + " |", true)]
        // Undetected category and severity
        [InlineData("Id1 | " + ReleaseTrackingHelper.UndetectedText + " | " + ReleaseTrackingHelper.UndetectedText + " | Category1 | Warning |", true)]
        // Invalid severity
        [InlineData("Id1 | Category1 | Invalid | Category1 | Warning |", false)]
        // Invalid old severity
        [InlineData("Id1 | Category1 | Warning | Category1 | Invalid |", false)]
        // Unchanged old category and old severity
        [InlineData("Id1 | Category1 | Warning | Category1 | Warning |", false)]
        // Missing required fields - category + severity + old category + old severity
        [InlineData("Id1", false)]
        // Missing required fields - category + old category + old severity
        [InlineData("Id1 | Warning ", false)]
        // Missing required fields - old category + old severity
        [InlineData("Id1 | Category1 | Warning ", false)]
        // Missing required fields - old severity
        [InlineData("Id1 | Category1 | Warning | OldCategory ", false)]
        // Missing required field - severity
        [InlineData("Id1 | Category1 |", false)]
        // Extra fields
        [InlineData("Id1 | Category1 | Warning | Category2 | Warning | Notes | InvalidField", false)]
        [Theory]
        public async Task TestInvalidEntryDiagnostic_ChangedRulesAsync(string entry, bool hasUndetectedField)
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor descriptor1 =
        new DiagnosticDescriptor(""Id1"", ""Title1"", ""Message1"", ""Category1"", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor1);
    public override void Initialize(AnalysisContext context) { }
}";
            var rule = hasUndetectedField ?
                DiagnosticDescriptorCreationAnalyzer.InvalidUndetectedEntryInAnalyzerReleasesFileRule :
                DiagnosticDescriptorCreationAnalyzer.InvalidEntryInAnalyzerReleasesFileRule;

            var shippedText = @"";
            var unshippedText = DefaultChangedUnshippedHeader + entry;

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                    GetAdditionalFileResultAt(5, 1,
                        ReleaseTrackingHelper.UnshippedFileName,
                        rule,
                        ReleaseTrackingHelper.UnshippedFileName,
                        entry));
        }

        // Duplicate entries in shipped.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |" + BlankLine + "{|RS2005:Id1 | Category1 | Warning ||}", "")]
        // Duplicate entries in unshipped.
        [InlineData("", DefaultUnshippedHeader + "Id1 | Category1 | Warning |" + BlankLine + "{|RS2005:Id1 | Category1 | Warning ||}")]
        // Duplicate changed entries in unshipped.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |", DefaultChangedUnshippedHeader + "Id1 | Category1 | Info | Category1 | Warning |" + BlankLine + DefaultChangedUnshippedHeader + "{|RS2005:Id1 | Category1 | Info | Category1 | Warning ||}")]
        // Duplicate entries with changed field in shipped.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |" + BlankLine + "{|RS2005:Id1 | Category2 | Warning ||}", "")]
        // Duplicate entries with changed field in unshipped.
        [InlineData("", DefaultUnshippedHeader + "Id1 | Category1 | Warning |" + BlankLine + "{|RS2005:Id1 | Category1 | Info ||}")]
        // Duplicate entries with in shipped with first removed entry.
        [InlineData(DefaultRemovedShippedHeader + "Id1 | Category1 | Warning |" + BlankLine + DefaultUnshippedHeader + "{|RS2005:Id1 | Category2 | Warning ||}", "")]
        // Duplicate entries with in shipped with second removed entry.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |" + BlankLine + DefaultRemovedUnshippedHeader + "{|RS2005:Id1 | Category2 | Warning ||}", "")]
        [Theory]
        public async Task TestDuplicateEntryInReleaseDiagnosticAsync(string shippedText, string unshippedText)
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor descriptor1 =
        new DiagnosticDescriptor(""Id1"", ""Title1"", ""Message1"", ""Category1"", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor1);
    public override void Initialize(AnalysisContext context) { }
}";
            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        // Duplicate entries across shipped and unshipped.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |", DefaultUnshippedHeader + "{|RS2006:Id1 | Category1 | Warning ||}")]
        // Duplicate entries across consecutive shipped releases.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |" + BlankLine + DefaultShippedHeader2 + "{|RS2006:Id1 | Category1 | Warning ||}", "")]
        // Duplicate entries across non-consecutive shipped releases.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |" + BlankLine + DefaultShippedHeader2 + BlankLine + DefaultShippedHeader3 + "{|RS2006:Id1 | Category1 | Warning ||}", "")]
        // Duplicate entries across shipped and unshipped, but with an intermediate entry with changed category - no diagnostic expected.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |" + BlankLine + DefaultShippedHeader2 + "Id1 | Category2 | Warning |", DefaultUnshippedHeader + "Id1 | Category1 | Warning |")]
        // Duplicate entries across shipped and unshipped, but with an intermediate entry with changed severity - no diagnostic expected.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |" + BlankLine + DefaultShippedHeader2 + "Id1 | Category1 | Info |", DefaultUnshippedHeader + "Id1 | Category1 | Warning |")]
        // Duplicate entries across shipped and unshipped, but with an intermediate entry with removed prefix - no diagnostic expected.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |" + BlankLine + DefaultRemovedShippedHeader2 + "Id1 | Category1 | Warning |", DefaultUnshippedHeader + "Id1 | Category1 | Warning |")]
        // Duplicate changed entries across consecutive shipped releases.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |" + BlankLine + DefaultChangedShippedHeader2 + "Id1 | Category1 | Warning | Category1 | Info |" + BlankLine + DefaultChangedShippedHeader3 + "{|RS2006:Id1 | Category1 | Warning | Category1 | Info ||}", "")]
        [Theory]
        public async Task TestDuplicateEntryBetweenReleasesDiagnosticAsync(string shippedText, string unshippedText)
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor descriptor1 =
        new DiagnosticDescriptor(""Id1"", ""Title1"", ""Message1"", ""Category1"", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor1);
    public override void Initialize(AnalysisContext context) { }
}";
            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        // Remove entry in unshipped for already shipped release.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |", DefaultRemovedUnshippedHeader + "Id1 | Category1 | Warning |", "RS2004")]
        // Remove entry in shipped for a prior shipped release.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |" + BlankLine + DefaultRemovedShippedHeader2 + "Id1 | Category1 | Warning |", "", "RS2000")]
        // Remove entry with changed severity in shipped for a prior shipped release.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |" + BlankLine + DefaultRemovedShippedHeader2 + "Id1 | Category1 | Info |", "", "RS2000")]
        [Theory]
        public async Task TestRemoveEntryInReleaseFile_DiagnosticCasesAsync(string shippedText, string unshippedText, string expectedDiagnosticId)
        {
            var source = $@"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{{
    private static readonly DiagnosticDescriptor descriptor1 =
        new DiagnosticDescriptor({{|{expectedDiagnosticId}:""Id1""|}}, ""Title1"", ""Message1"", ""Category1"", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor1);
    public override void Initialize(AnalysisContext context) {{ }}
}}";
            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        // Invalid remove entry without prior shipped entry in shipped.
        [InlineData(DefaultRemovedShippedHeader + "Id1 | Category1 | Warning |", "")]
        // Invalid remove entry without prior shipped entry in unshipped.
        [InlineData("", DefaultRemovedUnshippedHeader + "Id1 | Category1 | Warning |")]
        [Theory]
        public async Task TestInvalidRemoveWithoutShippedEntryInReleaseFileAsync(string shippedText, string unshippedText)
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray<DiagnosticDescriptor>.Empty;
    public override void Initialize(AnalysisContext context) { }
}";
            var fileWithDiagnostics = shippedText.Length > 0 ? ReleaseTrackingHelper.ShippedFileName : ReleaseTrackingHelper.UnshippedFileName;
            var lineCount = (shippedText.Length > 0 ? shippedText : unshippedText).Split(new[] { Environment.NewLine }, StringSplitOptions.None).Length;
            await VerifyCSharpAsync(source, shippedText, unshippedText,
                    GetAdditionalFileResultAt(lineCount, 1,
                        fileWithDiagnostics,
                        DiagnosticDescriptorCreationAnalyzer.InvalidRemovedOrChangedWithoutPriorNewEntryInAnalyzerReleasesFileRule,
                        fileWithDiagnostics,
                        "Removed",
                        "Id1"));
        }

        // Invalid changed entry without prior shipped entry in shipped.
        [InlineData(DefaultChangedShippedHeader + "Id1 | Category1 | Hidden | Category1 | Warning |", "")]
        // Invalid changed entry without prior shipped entry in unshipped.
        [InlineData("", DefaultChangedUnshippedHeader + "Id1 | Category1 | Hidden | Category1 | Warning |")]
        [Theory]
        public async Task TestInvalidChangedWithoutShippedEntryInReleaseFileAsync(string shippedText, string unshippedText)
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor descriptor1 =
        new DiagnosticDescriptor(""Id1"", ""Title1"", ""Message1"", ""Category1"", DiagnosticSeverity.Hidden, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor1);
    public override void Initialize(AnalysisContext context) { }
}";
            var fileWithDiagnostics = shippedText.Length > 0 ? ReleaseTrackingHelper.ShippedFileName : ReleaseTrackingHelper.UnshippedFileName;
            var lineCount = (shippedText.Length > 0 ? shippedText : unshippedText).Split(new[] { Environment.NewLine }, StringSplitOptions.None).Length;
            await VerifyCSharpAsync(source, shippedText, unshippedText,
                    GetAdditionalFileResultAt(lineCount, 1,
                        fileWithDiagnostics,
                        DiagnosticDescriptorCreationAnalyzer.InvalidRemovedOrChangedWithoutPriorNewEntryInAnalyzerReleasesFileRule,
                        fileWithDiagnostics,
                        "Changed",
                        "Id1"));
        }

        // Invalid remove entry without prior shipped entry in shipped, followed by a shipped entry.
        [InlineData(DefaultRemovedShippedHeader + "Id1 | Category1 | Warning |" + BlankLine + DefaultShippedHeader2 + "Id1 | Category1 | Warning |", "")]
        // Invalid remove entry without prior shipped entry in shipped, followed by an unshipped entry.
        [InlineData(DefaultRemovedShippedHeader + "Id1 | Category1 | Warning |", DefaultUnshippedHeader + "Id1 | Category1 | Warning |")]
        [Theory]
        public async Task TestInvalidRemoveWithoutShippedEntryInReleaseFile_02Async(string shippedText, string unshippedText)
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor descriptor1 =
        new DiagnosticDescriptor(""Id1"", ""Title1"", ""Message1"", ""Category1"", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor1);
    public override void Initialize(AnalysisContext context) { }
}";
            var fileWithDiagnostics = shippedText.Length > 0 ? ReleaseTrackingHelper.ShippedFileName : ReleaseTrackingHelper.UnshippedFileName;
            await VerifyCSharpAsync(source, shippedText, unshippedText,
                    GetAdditionalFileResultAt(7, 1,
                        fileWithDiagnostics,
                        DiagnosticDescriptorCreationAnalyzer.InvalidRemovedOrChangedWithoutPriorNewEntryInAnalyzerReleasesFileRule,
                        fileWithDiagnostics,
                        "Removed",
                        "Id1"));
        }

        // Remove entry in unshipped for already shipped release.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |", DefaultRemovedUnshippedHeader + "Id1 | Category1 | Warning |")]
        // Remove entry in shipped for a prior shipped release.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |" + BlankLine + DefaultRemovedShippedHeader2 + "Id1 | Category1 | Warning |", "")]
        // Remove entry with changed severity in shipped for a prior shipped release.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |" + BlankLine + DefaultRemovedShippedHeader2 + "Id1 | Category1 | Info |", "")]
        [Theory]
        public async Task TestRemoveEntryInReleaseFile_NoDiagnosticCasesAsync(string shippedText, string unshippedText)
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray<DiagnosticDescriptor>.Empty;
    public override void Initialize(AnalysisContext context) { }
}";
            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact, WorkItem(5828, "https://github.com/dotnet/roslyn-analyzers/issues/5828")]
        public async Task TestTargetTypedNew()
        {
            var source = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor descriptor1 =
        new(""Id1"", ""Title1"", ""Message1"", ""Category1"", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor1);
    public override void Initialize(AnalysisContext context) { }
}";

            var shippedText = @"";
            var unshippedText = $@"{DefaultUnshippedHeader}Id1 | Category1 | Warning |";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }
        #region Helpers

        private const string DefaultUnshippedHeader = ReleaseTrackingHelper.TableTitleNewRules + BlankLine + BlankLine +
            ReleaseTrackingHelper.TableHeaderNewOrRemovedRulesLine1 + BlankLine +
            ReleaseTrackingHelper.TableHeaderNewOrRemovedRulesLine2 + BlankLine;

        private const string DefaultRemovedUnshippedHeader = ReleaseTrackingHelper.TableTitleRemovedRules + BlankLine + BlankLine +
            ReleaseTrackingHelper.TableHeaderNewOrRemovedRulesLine1 + BlankLine +
            ReleaseTrackingHelper.TableHeaderNewOrRemovedRulesLine2 + BlankLine;

        private const string DefaultChangedUnshippedHeader = ReleaseTrackingHelper.TableTitleChangedRules + BlankLine + BlankLine +
            ReleaseTrackingHelper.TableHeaderChangedRulesLine1 + BlankLine +
            ReleaseTrackingHelper.TableHeaderChangedRulesLine2 + BlankLine;

        private const string DefaultShippedHeader = ReleaseTrackingHelper.ReleasePrefix + " 1.0" + BlankLine + BlankLine + DefaultUnshippedHeader;
        private const string DefaultRemovedShippedHeader = ReleaseTrackingHelper.ReleasePrefix + " 1.0" + BlankLine + BlankLine + DefaultRemovedUnshippedHeader;
        private const string DefaultChangedShippedHeader = ReleaseTrackingHelper.ReleasePrefix + " 1.0" + BlankLine + BlankLine + DefaultChangedUnshippedHeader;

        private const string DefaultShippedHeader2 = ReleaseTrackingHelper.ReleasePrefix + " 2.0" + BlankLine + BlankLine + DefaultUnshippedHeader;
        private const string DefaultRemovedShippedHeader2 = ReleaseTrackingHelper.ReleasePrefix + " 2.0" + BlankLine + BlankLine + DefaultRemovedUnshippedHeader;
        private const string DefaultChangedShippedHeader2 = ReleaseTrackingHelper.ReleasePrefix + " 2.0" + BlankLine + BlankLine + DefaultChangedUnshippedHeader;

        private const string DefaultShippedHeader3 = ReleaseTrackingHelper.ReleasePrefix + " 3.0" + BlankLine + BlankLine + DefaultUnshippedHeader;
        private const string DefaultChangedShippedHeader3 = ReleaseTrackingHelper.ReleasePrefix + " 3.0" + BlankLine + BlankLine + DefaultChangedUnshippedHeader;

        private static DiagnosticResult GetAdditionalFileResultAt(int line, int column, string path, DiagnosticDescriptor descriptor, params object[] arguments)
        {
#pragma warning disable RS0030 // Do not use banned APIs
            return new DiagnosticResult(descriptor)
                .WithLocation(path, line, column)
#pragma warning restore RS0030 // Do not use banned APIs
                .WithArguments(arguments);
        }

        private static readonly ImmutableDictionary<string, ReportDiagnostic> s_nonReleaseTrackingWarningsDisabled = ImmutableDictionary<string, ReportDiagnostic>.Empty
            .Add(DiagnosticDescriptorCreationAnalyzer.DiagnosticIdMustBeInSpecifiedFormatRule.Id, ReportDiagnostic.Suppress)
            .Add(DiagnosticDescriptorCreationAnalyzer.DoNotUseReservedDiagnosticIdRule.Id, ReportDiagnostic.Suppress)
            .Add(DiagnosticDescriptorCreationAnalyzer.ProvideCustomTagsInDescriptorRule.Id, ReportDiagnostic.Suppress)
            .Add(DiagnosticDescriptorCreationAnalyzer.ProvideHelpUriInDescriptorRule.Id, ReportDiagnostic.Suppress)
            .Add(DiagnosticDescriptorCreationAnalyzer.UseCategoriesFromSpecifiedRangeRule.Id, ReportDiagnostic.Suppress)
            .Add(DiagnosticDescriptorCreationAnalyzer.UseLocalizableStringsInDescriptorRule.Id, ReportDiagnostic.Suppress)
            .Add(DiagnosticDescriptorCreationAnalyzer.UseUniqueDiagnosticIdRule.Id, ReportDiagnostic.Suppress);

        private static Solution DisableNonReleaseTrackingWarnings(Solution solution, ProjectId projectId)
        {
            var compilationOptions = solution.GetProject(projectId)!.CompilationOptions!;
            compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(compilationOptions.SpecificDiagnosticOptions.SetItems(s_nonReleaseTrackingWarningsDisabled));
            return solution.WithProjectCompilationOptions(projectId, compilationOptions);
        }

        private async Task VerifyCSharpAsync(string source, string shippedText, string unshippedText, params DiagnosticResult[] expected)
        {
            var test = new CSharpCodeFixTest<DiagnosticDescriptorCreationAnalyzer, AnalyzerReleaseTrackingFix, DefaultVerifier>
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { },
                },
                ReferenceAssemblies = AdditionalMetadataReferences.Default,
            };

            if (shippedText != null)
            {
                test.TestState.AdditionalFiles.Add((ReleaseTrackingHelper.ShippedFileName, shippedText));
            }

            if (unshippedText != null)
            {
                test.TestState.AdditionalFiles.Add((ReleaseTrackingHelper.UnshippedFileName, unshippedText));
            }

            test.SolutionTransforms.Add(DisableNonReleaseTrackingWarnings);
            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        private async Task VerifyCSharpAdditionalFileFixAsync(string source, string shippedText, string oldUnshippedText, string newUnshippedText)
        {
            await VerifyAdditionalFileFixAsync(LanguageNames.CSharp, source, shippedText, oldUnshippedText, newUnshippedText, ImmutableArray<DiagnosticResult>.Empty, ImmutableArray<DiagnosticResult>.Empty);
        }

        private async Task VerifyCSharpAdditionalFileFixAsync(string source, string shippedText, string oldUnshippedText, string newUnshippedText,
            ImmutableArray<DiagnosticResult> additionalExpectedDiagnosticsInInput, ImmutableArray<DiagnosticResult> additionalExpectedDiagnosticsInResult)
        {
            await VerifyAdditionalFileFixAsync(LanguageNames.CSharp, source, shippedText, oldUnshippedText, newUnshippedText, additionalExpectedDiagnosticsInInput, additionalExpectedDiagnosticsInResult);
        }

        private async Task VerifyAdditionalFileFixAsync(string language, string source, string shippedText, string oldUnshippedText, string newUnshippedText,
            ImmutableArray<DiagnosticResult> additionalExpectedDiagnosticsInInput, ImmutableArray<DiagnosticResult> additionalExpectedDiagnosticsInResult)
        {
            var test = language == LanguageNames.CSharp
                ? new CSharpCodeFixTest<DiagnosticDescriptorCreationAnalyzer, AnalyzerReleaseTrackingFix, DefaultVerifier>()
                : (CodeFixTest<DefaultVerifier>)new VisualBasicCodeFixTest<DiagnosticDescriptorCreationAnalyzer, AnalyzerReleaseTrackingFix, DefaultVerifier>();

            test.ReferenceAssemblies = AdditionalMetadataReferences.Default;
            test.SolutionTransforms.Add(DisableNonReleaseTrackingWarnings);

            test.TestState.Sources.Add(source);
            test.TestState.AdditionalFiles.Add((ReleaseTrackingHelper.ShippedFileName, shippedText));
            test.TestState.AdditionalFiles.Add((ReleaseTrackingHelper.UnshippedFileName, oldUnshippedText));
            test.TestState.ExpectedDiagnostics.AddRange(additionalExpectedDiagnosticsInInput);

            test.FixedState.AdditionalFiles.Add((ReleaseTrackingHelper.ShippedFileName, shippedText));
            test.FixedState.AdditionalFiles.Add((ReleaseTrackingHelper.UnshippedFileName, newUnshippedText));
            test.FixedState.ExpectedDiagnostics.AddRange(additionalExpectedDiagnosticsInResult);

            await test.RunAsync();
        }

        private const string CSharpDiagnosticDescriptorCreationHelper = @"
internal static class DiagnosticDescriptorHelper
{
    // Dummy DiagnosticDescriptor creation helper.
    public static DiagnosticDescriptor Create(
        string id,
        LocalizableString title,
        LocalizableString messageFormat,
        string category,
        RuleLevel ruleLevel,
        string helpLinkUri = null)
    => null;
}

namespace Microsoft.CodeAnalysis
{
    internal enum RuleLevel
    {
        BuildWarning = 1,
        IdeSuggestion = 2,
        IdeHidden_BulkConfigurable = 3,
        Disabled = 4,
        CandidateForRemoval = 5,
    }
}";
        #endregion
    }
}
