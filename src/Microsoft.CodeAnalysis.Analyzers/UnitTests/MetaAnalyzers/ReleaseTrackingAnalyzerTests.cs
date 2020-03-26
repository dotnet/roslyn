// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.VisualBasic.Testing;
using Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests.MetaAnalyzers
{
    public class ReleaseTrackingAnalyzerTests
    {
        [Fact]
        public async Task TestNoDeclaredAnalyzers()
        {
            var source = @"";

            var shippedText = @"";
            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [InlineData(@"""Id1""", null, null)]
        [InlineData(@"""Id1""", "", null)]
        [InlineData(@"""Id1""", null, "")]
        [InlineData(@"{|RS2000:""Id1""|}", "", "")]
        [Theory]
        public async Task TestMissingReleasesFiles(string id, string shippedText, string unshippedText)
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

        [InlineData("", DefaultUnshippedHeader + "Id1 | Category1 | Warning |")]
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |", "")]
        [Theory]
        public async Task TestReleasesFileAlreadyHasEntry(string shippedText, string unshippedText)
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
        public async Task TestRemoveUnshippedDeletedDiagnosticIdRule()
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
        public async Task TestRemoveShippedDeletedDiagnosticIdRule()
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
        public async Task TestCodeFixToAddUnshippedEntries()
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
@"Rule ID | Category | Severity | HelpLink (optional)
--------|----------|----------|--------------------
Id1 | Category1 | Warning |
Id2 | Category2 | Disabled |
Id3 | Category3 | Warning | [Documentation](Dummy)";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestCodeFixToAddUnshippedEntries_DiagnosticDescriptorHelper()
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
@"Rule ID | Category | Severity | HelpLink (optional)
--------|----------|----------|--------------------
Id1 | Category1 | Warning |
Id2 | Category2 | Disabled |
Id3 | Category3 | Warning | [Documentation](Dummy)";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        private const string BlankLine = @"
";

        [InlineData(DefaultUnshippedHeader + @"; Comments are preserved")]
        [InlineData(DefaultUnshippedHeader + BlankLine)] // Blank lines are preserved
        [InlineData(DefaultUnshippedHeader + BlankLine + @"; Comments are preserved" + BlankLine)] // Mixed
        [Theory]
        public async Task TestCodeFixToAddUnshippedEntries_TriviaIsPreserved(string unshippedText)
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
            var fixedUnshippedText = unshippedText + @"
Id1 | Category1 | Warning |";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        // Added after current entry.
        [InlineData("Id0", DefaultUnshippedHeader + @"Id0 | DifferentCategory | Warning |",
                           DefaultUnshippedHeader + @"Id0 | DifferentCategory | Warning |" + BlankLine + @"Id1 | Category1 | Warning |")]
        // Added before current entry.
        [InlineData("Id2", DefaultUnshippedHeader + @"Id2 | DifferentCategory | Warning |",
                           DefaultUnshippedHeader + @"Id1 | Category1 | Warning |" + BlankLine + @"Id2 | DifferentCategory | Warning |")]
        [Theory]
        public async Task TestCodeFixToAddUnshippedEntries_AlreadyHasDifferentUnshippedEntries(string differentRuleId, string unshippedText, string fixedUnshippedText)
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

            var shippedText = @"";
            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [InlineData("", "RS2000")]
        [InlineData(DefaultShippedHeader + "Id1 | DifferentCategory | Warning |", "RS2001")]
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Disabled |", "RS2001")]
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Info |" + BlankLine + DefaultShippedHeader2 + "*REMOVED*Id1 | Category1 | Info |", "RS2000")]
        [Theory]
        public async Task TestCodeFixToAddUnshippedEntries_AlreadyHasDifferentShippedEntry(string shippedText, string expectedDiagnosticId)
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
            var fixedUnshippedText =
@"Rule ID | Category | Severity | HelpLink (optional)
--------|----------|----------|--------------------
Id1 | Category1 | Warning |";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestCodeFixToUpdateMultipleUnshippedEntries()
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
@"Rule ID | Category | Severity | HelpLink (optional)
--------|----------|----------|--------------------
Id1 | DifferentCategory | Warning |
Id2 | Category2 | Warning |
Id3 | Category3 | Warning |";

            var fixedUnshippedText =
@"Rule ID | Category | Severity | HelpLink (optional)
--------|----------|----------|--------------------
Id1 | Category1 | Warning |
Id2 | Category2 | Disabled |
Id3 | Category3 | Warning |";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestCodeFixToAddUnshippedEntries_UndetectedFields()
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
            var fixedUnshippedText =
@"Rule ID | Category | Severity | HelpLink (optional)
--------|----------|----------|--------------------
Id1 | <Undetected> | <Undetected> |";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText, additionalExpectedDiagnosticsInInput: ImmutableArray<DiagnosticResult>.Empty,
                additionalExpectedDiagnosticsInResult: ImmutableArray.Create(
                    GetAdditionalFileResultAt(3, 1,
                        DiagnosticDescriptorCreationAnalyzer.UnshippedFileName,
                        DiagnosticDescriptorCreationAnalyzer.InvalidUndetectedEntryInAnalyzerReleasesFileRule,
                        DiagnosticDescriptorCreationAnalyzer.UnshippedFileName,
                        "Id1 | <Undetected> | <Undetected> |")));
        }

        [Fact]
        public async Task TestNoCodeFixToAddUnshippedEntries_UndetectedFields()
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
            var unshippedText =
@"Rule ID | Category | Severity | HelpLink (optional)
--------|----------|----------|--------------------
Id1 | CustomCategory | Warning |";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        // No header in unshipped
        [InlineData("", "Id1 | Category1 | Warning |")]
        // No header in shipped
        [InlineData("Id1 | Category1 | Warning |", "")]
        // Missing ReleaseHeaderLine1 in unshipped
        [InlineData("", DiagnosticDescriptorCreationAnalyzer.ReleaseHeaderLine2 + BlankLine + "Id1 | Category1 | Warning |")]
        // Missing ReleaseHeaderLine2 in unshipped
        [InlineData("", DiagnosticDescriptorCreationAnalyzer.ReleaseHeaderLine1 + BlankLine + "Id1 | Category1 | Warning |", 2)]
        // Missing Release Version line in shipped
        [InlineData(DefaultUnshippedHeader + "Id1 | Category1 | Warning |", "")]
        // Missing Release Version in shipped
        [InlineData(DiagnosticDescriptorCreationAnalyzer.ReleasePrefix + BlankLine + DefaultUnshippedHeader + "Id1 | Category1 | Warning |", "")]
        // Invalid Release Version in shipped
        [InlineData(DiagnosticDescriptorCreationAnalyzer.ReleasePrefix + " InvalidVersion" + BlankLine + DefaultUnshippedHeader + "Id1 | Category1 | Warning |", "")]
        // Missing ReleaseHeaderLine1 in shipped
        [InlineData(DiagnosticDescriptorCreationAnalyzer.ReleasePrefix + "1.0" + BlankLine + DiagnosticDescriptorCreationAnalyzer.ReleaseHeaderLine2 + BlankLine + "Id1 | Category1 | Warning |", "", 2)]
        // Missing ReleaseHeaderLine2 in shipped
        [InlineData(DiagnosticDescriptorCreationAnalyzer.ReleasePrefix + " 1.0" + BlankLine + DiagnosticDescriptorCreationAnalyzer.ReleaseHeaderLine1 + BlankLine + "Id1 | Category1 | Warning |", "", 3)]
        // Invalid Release Version line in unshipped
        [InlineData("", DefaultShippedHeader + "Id1 | Category1 | Warning |")]
        [Theory]
        public async Task TestInvalidHeaderDiagnostic(string shippedText, string unshippedText, int line = 1)
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

            var fileWithDiagnostics = shippedText.Length > 0 ? DiagnosticDescriptorCreationAnalyzer.ShippedFileName : DiagnosticDescriptorCreationAnalyzer.UnshippedFileName;
            var diagnosticText = (shippedText.Length > 0 ? shippedText : unshippedText).Split(new[] { Environment.NewLine }, StringSplitOptions.None).ElementAt(line - 1);
            await VerifyCSharpAsync(source, shippedText, unshippedText,
                    GetAdditionalFileResultAt(line, 1,
                        fileWithDiagnostics,
                        DiagnosticDescriptorCreationAnalyzer.InvalidHeaderInAnalyzerReleasesFileRule,
                        fileWithDiagnostics,
                        diagnosticText));
        }

        // Undetected category
        [InlineData("Id1 | <Undetected> | Warning |", true)]
        // Undetected severity
        [InlineData("Id1 | Category1 | <Undetected> |", true)]
        // Undetected category and severity
        [InlineData("Id1 | <Undetected> | <Undetected> |", true)]
        // Invalid severity
        [InlineData("Id1 | Category1 | Invalid |", false)]
        // Missing required fields - category + severity
        [InlineData("Id1", false)]
        // Missing required field - category
        [InlineData("Id1 | Warning ", false)]
        // Missing required field - severity
        [InlineData("Id1 | Category1 |", false)]
        // Extra fields
        [InlineData("Id1 | Category1 | Warning | HelpLink | InvalidField", false)]
        [Theory]
        public async Task TestInvalidEntryDiagnostic(string entry, bool hasUndetectedField)
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
                    GetAdditionalFileResultAt(3, 1,
                        DiagnosticDescriptorCreationAnalyzer.UnshippedFileName,
                        rule,
                        DiagnosticDescriptorCreationAnalyzer.UnshippedFileName,
                        entry));
        }

        // Duplicate entries in shipped.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |" + BlankLine + "{|RS2005:Id1 | Category1 | Warning ||}", "")]
        // Duplicate entries in unshipped.
        [InlineData("", DefaultUnshippedHeader + "Id1 | Category1 | Warning |" + BlankLine + "{|RS2005:Id1 | Category1 | Warning ||}")]
        // Duplicate entries with changed field in shipped.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |" + BlankLine + "{|RS2005:Id1 | Category2 | Warning ||}", "")]
        // Duplicate entries with changed field in unshipped.
        [InlineData("", DefaultUnshippedHeader + "Id1 | Category1 | Warning |" + BlankLine + "{|RS2005:Id1 | Category1 | Info ||}")]
        // Duplicate entries with in shipped with first removed entry.
        [InlineData(DefaultShippedHeader + "*REMOVED*Id1 | Category1 | Warning |" + BlankLine + "{|RS2005:Id1 | Category2 | Warning ||}", "")]
        // Duplicate entries with in shipped with second removed entry.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |" + BlankLine + "{|RS2005:*REMOVED*Id1 | Category2 | Warning ||}", "")]
        [Theory]
        public async Task TestDuplicateEntryInReleaseDiagnostic(string shippedText, string unshippedText)
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
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |" + BlankLine + DefaultShippedHeader2 + "*REMOVED*Id1 | Category1 | Warning |", DefaultUnshippedHeader + "Id1 | Category1 | Warning |")]
        [Theory]
        public async Task TestDuplicateEntryBetweenReleasesDiagnostic(string shippedText, string unshippedText)
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
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |", DefaultUnshippedHeader + "*REMOVED*Id1 | Category1 | Warning |", "RS2004")]
        // Remove entry in shipped for a prior shipped release.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |" + BlankLine + DefaultShippedHeader2 + "*REMOVED*Id1 | Category1 | Warning |", "", "RS2000")]
        // Remove entry with changed severity in shipped for a prior shipped release.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |" + BlankLine + DefaultShippedHeader2 + "*REMOVED*Id1 | Category1 | Info |", "", "RS2000")]
        [Theory]
        public async Task TestRemoveEntryInReleaseFile_DiagnosticCases(string shippedText, string unshippedText, string expectedDiagnosticId)
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
        [InlineData(DefaultShippedHeader + "*REMOVED*Id1 | Category1 | Warning |", "")]
        // Invalid remove entry without prior shipped entry in unshipped.
        [InlineData("", DefaultUnshippedHeader + "*REMOVED*Id1 | Category1 | Warning |")]
        [Theory]
        public async Task TestInvalidRemoveWithoutShippedEntryInReleaseFile(string shippedText, string unshippedText)
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
            var fileWithDiagnostics = shippedText.Length > 0 ? DiagnosticDescriptorCreationAnalyzer.ShippedFileName : DiagnosticDescriptorCreationAnalyzer.UnshippedFileName;
            var lineCount = (shippedText.Length > 0 ? shippedText : unshippedText).Split(new[] { Environment.NewLine }, StringSplitOptions.None).Length;
            await VerifyCSharpAsync(source, shippedText, unshippedText,
                    GetAdditionalFileResultAt(lineCount, 1,
                        fileWithDiagnostics,
                        DiagnosticDescriptorCreationAnalyzer.InvalidRemovedWithoutShippedEntryInAnalyzerReleasesFileRule,
                        fileWithDiagnostics,
                        "Id1"));
        }

        // Invalid remove entry without prior shipped entry in shipped, followed by a shipped entry.
        [InlineData(DefaultShippedHeader + "*REMOVED*Id1 | Category1 | Warning |" + BlankLine + DefaultShippedHeader2 + "Id1 | Category1 | Warning |", "")]
        // Invalid remove entry without prior shipped entry in shipped, followed by an unshipped entry.
        [InlineData(DefaultShippedHeader + "*REMOVED*Id1 | Category1 | Warning |", DefaultUnshippedHeader + "Id1 | Category1 | Warning |")]
        [Theory]
        public async Task TestInvalidRemoveWithoutShippedEntryInReleaseFile_02(string shippedText, string unshippedText)
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
            var fileWithDiagnostics = shippedText.Length > 0 ? DiagnosticDescriptorCreationAnalyzer.ShippedFileName : DiagnosticDescriptorCreationAnalyzer.UnshippedFileName;
            await VerifyCSharpAsync(source, shippedText, unshippedText,
                    GetAdditionalFileResultAt(5, 1,
                        fileWithDiagnostics,
                        DiagnosticDescriptorCreationAnalyzer.InvalidRemovedWithoutShippedEntryInAnalyzerReleasesFileRule,
                        fileWithDiagnostics,
                        "Id1"));
        }

        // Remove entry in unshipped for already shipped release.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |", DefaultUnshippedHeader + "*REMOVED*Id1 | Category1 | Warning |")]
        // Remove entry in shipped for a prior shipped release.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |" + BlankLine + DefaultShippedHeader2 + "*REMOVED*Id1 | Category1 | Warning |", "")]
        // Remove entry with changed severity in shipped for a prior shipped release.
        [InlineData(DefaultShippedHeader + "Id1 | Category1 | Warning |" + BlankLine + DefaultShippedHeader2 + "*REMOVED*Id1 | Category1 | Info |", "")]
        [Theory]
        public async Task TestRemoveEntryInReleaseFile_NoDiagnosticCases(string shippedText, string unshippedText)
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
        #region Helpers

        private const string DefaultUnshippedHeader =
@"Rule ID | Category | Severity | HelpLink (optional)
--------|----------|----------|--------------------
";

        private const string DefaultShippedHeader =
@"## Release 1.0

Rule ID | Category | Severity | HelpLink (optional)
--------|----------|----------|--------------------
";

        private const string DefaultShippedHeader2 =
@"## Release 2.0

Rule ID | Category | Severity | HelpLink (optional)
--------|----------|----------|--------------------
";

        private const string DefaultShippedHeader3 =
@"## Release 3.0

Rule ID | Category | Severity | HelpLink (optional)
--------|----------|----------|--------------------
";

        private static DiagnosticResult GetAdditionalFileResultAt(int line, int column, string path, DiagnosticDescriptor descriptor, params object[] arguments)
        {
            return new DiagnosticResult(descriptor)
                .WithLocation(path, line, column)
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
            var test = new CSharpCodeFixTest<DiagnosticDescriptorCreationAnalyzer, AnalyzerReleaseTrackingFix, XUnitVerifier>
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { },
                },
                ReferenceAssemblies = AdditionalMetadataReferences.Default,
                TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck,
            };

            if (shippedText != null)
            {
                test.TestState.AdditionalFiles.Add((DiagnosticDescriptorCreationAnalyzer.ShippedFileName, shippedText));
            }

            if (unshippedText != null)
            {
                test.TestState.AdditionalFiles.Add((DiagnosticDescriptorCreationAnalyzer.UnshippedFileName, unshippedText));
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
                ? new CSharpCodeFixTest<DiagnosticDescriptorCreationAnalyzer, AnalyzerReleaseTrackingFix, XUnitVerifier>()
                : (CodeFixTest<XUnitVerifier>)new VisualBasicCodeFixTest<DiagnosticDescriptorCreationAnalyzer, AnalyzerReleaseTrackingFix, XUnitVerifier>();

            test.ReferenceAssemblies = AdditionalMetadataReferences.Default;
            test.TestBehaviors |= TestBehaviors.SkipGeneratedCodeCheck;
            test.SolutionTransforms.Add(DisableNonReleaseTrackingWarnings);

            test.TestState.Sources.Add(source);
            test.TestState.AdditionalFiles.Add((DiagnosticDescriptorCreationAnalyzer.ShippedFileName, shippedText));
            test.TestState.AdditionalFiles.Add((DiagnosticDescriptorCreationAnalyzer.UnshippedFileName, oldUnshippedText));
            test.TestState.ExpectedDiagnostics.AddRange(additionalExpectedDiagnosticsInInput);

            test.FixedState.AdditionalFiles.Add((DiagnosticDescriptorCreationAnalyzer.ShippedFileName, shippedText));
            test.FixedState.AdditionalFiles.Add((DiagnosticDescriptorCreationAnalyzer.UnshippedFileName, newUnshippedText));
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
