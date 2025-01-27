// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppressions.CSharpRemoveUnnecessaryAttributeSuppressionsDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions.RemoveUnnecessaryAttributeSuppressionsCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessarySuppressions;

[Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessarySuppressions)]
[WorkItem("https://github.com/dotnet/roslyn/issues/44176")]
public class RemoveUnnecessaryAttributeSuppressionsTests
{
    [Theory, CombinatorialData]
    public void TestStandardProperty(AnalyzerProperty property)
        => VerifyCS.VerifyStandardProperty(property);

    [Theory]
    // Field
    [InlineData(@"Scope = ""member""", @"Target = ""~F:N.C.F""", "assembly")]
    // Property
    [InlineData(@"Scope = ""member""", @"Target = ""~P:N.C.P""", "assembly")]
    // Method
    [InlineData(@"Scope = ""member""", @"Target = ""~M:N.C.M()""", "assembly")]
    // Type
    [InlineData(@"Scope = ""member""", @"Target = ""~T:N.C""", "assembly")]
    // Namespace
    [InlineData(@"Scope = ""namespace""", @"Target = ""~N:N""", "assembly")]
    // NamespaceAndDescendants
    [InlineData(@"Scope = ""namespaceanddescendants""", @"Target = ""~N:N""", "assembly")]
    // Module - no scope, no target
    [InlineData(null, null, "assembly")]
    // Module - no target
    [InlineData(@"Scope = ""module""", null, "assembly")]
    // Module - null target
    [InlineData(@"Scope = ""module""", @"Target = null", "assembly")]
    // Resource - not handled
    [InlineData(@"Scope = ""resource""", @"Target = """"", "assembly")]
    // 'module' attribute target
    [InlineData(@"Scope = ""member""", @"Target = ""~M:N.C.M()""", "module")]
    // Member with non-matching scope (seems to be respected by suppression decoder)
    [InlineData(@"Scope = ""type""", @"Target = ""~M:N.C.M()""", "assembly")]
    [InlineData(@"Scope = ""namespace""", @"Target = ""~F:N.C.F""", "assembly")]
    // Case insensitive scope
    [InlineData(@"Scope = ""Member""", @"Target = ""~F:N.C.F""", "assembly")]
    [InlineData(@"Scope = ""MEMBER""", @"Target = ""~F:N.C.F""", "assembly")]
    public async Task ValidSuppressions(string? scope, string? target, string attributeTarget)
    {
        var scopeString = scope != null ? $@", {scope}" : string.Empty;
        var targetString = target != null ? $@", {target}" : string.Empty;

        var input = $@"
[{attributeTarget}: System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""Id: Title"", Justification = ""Pending""{scopeString}{targetString})]

namespace N
{{
    class C
    {{
        public int F;
        public int P {{ get; }}
        public void M() {{ }}
    }}
}}";
        await VerifyCS.VerifyCodeFixAsync(input, input);
    }

    [Theory]
    // Field - no matching symbol
    [InlineData(@"Scope = ""member""", @"Target = ""~F:N.C.F2""", "assembly")]
    // Field - no matching symbol (case insensitive)
    [InlineData(@"Scope = ""Member""", @"Target = ""~F:N.C.F2""", "assembly")]
    [InlineData(@"Scope = ""MEMBER""", @"Target = ""~F:N.C.F2""", "assembly")]
    // Property - invalid scope
    [InlineData(@"Scope = ""invalid""", @"Target = ""~P:N.C.P""", "assembly")]
    // Method - wrong signature
    [InlineData(@"Scope = ""member""", @"Target = ""~M:N.C.M(System.Int32)""", "assembly")]
    // Method - module scope
    [InlineData(@"Scope = ""module""", @"Target = ""~M:N.C.M()""", "assembly")]
    // Method - null scope
    [InlineData(@"Scope = null", @"Target = ""~M:N.C.M()""", "assembly")]
    // Method - no scope
    [InlineData(null, @"Target = ""~M:N.C.M()""", "assembly")]
    // Member scope - null target
    [InlineData(@"Scope = ""member""", @"Target = null", "assembly")]
    // Member scope - no target
    [InlineData(@"Scope = ""member""", null, "assembly")]
    // Type - no matching namespace
    [InlineData(@"Scope = ""type""", @"Target = ""~T:N2.C""", "assembly")]
    // Namespace - extra namespace qualification
    [InlineData(@"Scope = ""namespace""", @"Target = ""~N:N.N2""", "assembly")]
    // NamespaceAndDescendants - empty target
    [InlineData(@"Scope = ""namespaceanddescendants""", @"Target = """"", "assembly")]
    // Module - no scope, empty target
    [InlineData(null, @"Target = """"", "assembly")]
    // Module - no scope, non-empty target
    [InlineData(null, @"Target = ""~T:N.C""", "assembly")]
    // Module scope, empty target
    [InlineData(@"Scope = ""module""", @"Target = """"", "assembly")]
    // Module no scope, non-empty target
    [InlineData(@"Scope = ""module""", @"Target = ""~T:N.C""", "assembly")]
    public async Task InvalidSuppressions(string? scope, string? target, string attributeTarget)
    {
        var scopeString = scope != null ? $@", {scope}" : string.Empty;
        var targetString = target != null ? $@", {target}" : string.Empty;

        var input = $@"
[{attributeTarget}: [|System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""Id: Title"", Justification = ""Pending""{scopeString}{targetString})|]]

namespace N
{{
    class C
    {{
        public int F;
        public int P {{ get; }}
        public void M() {{ }}
    }}
}}";

        var fixedCode = $@"

namespace N
{{
    class C
    {{
        public int F;
        public int P {{ get; }}
        public void M() {{ }}
    }}
}}";
        await VerifyCS.VerifyCodeFixAsync(input, fixedCode);
    }

    [Fact]
    public async Task ValidAndInvalidSuppressions()
    {
        var attributePrefix = @"System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""Id: Title"", Justification = ""Pending""";
        var validSuppression = $@"{attributePrefix}, Scope = ""member"", Target = ""~T:C"")";
        var invalidSuppression = $@"[|{attributePrefix}, Scope = ""member"", Target = """")|]";

        var input = $@"
[assembly: {validSuppression}]
[assembly: {invalidSuppression}]
[assembly: {validSuppression}, {validSuppression}]
[assembly: {invalidSuppression}, {invalidSuppression}]
[assembly: {validSuppression}, {invalidSuppression}]
[assembly: {invalidSuppression}, {validSuppression}]
[assembly: {invalidSuppression}, {validSuppression}, {invalidSuppression}, {validSuppression}]

class C {{ }}
";

        var fixedCode = $@"
[assembly: {validSuppression}]
[assembly: {validSuppression}, {validSuppression}]
[assembly: {validSuppression}]
[assembly: {validSuppression}]
[assembly: {validSuppression}, {validSuppression}]

class C {{ }}
";
        await VerifyCS.VerifyCodeFixAsync(input, fixedCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData(@", Scope = ""member"", Target = ""~M:C.M()""")]
    [InlineData(@", Scope = ""invalid"", Target = ""invalid""")]
    public async Task LocalSuppressions(string scopeAndTarget)
    {
        var input = $@"
[System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""Id: Title"", Justification = ""Pending""{scopeAndTarget})]
class C
{{
    public void M() {{ }}
}}";
        await VerifyCS.VerifyCodeFixAsync(input, input);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45465")]
    public async Task LegacyModeGlobalSuppressionWithNamespaceAndDescendantsScope()
    {
        var target = "N:N.InvalidChild";
        var input = $@"
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""Category"", ""Id: Title"", Scope = ""namespaceanddescendants"", Target = {{|#0:""{target}""|}})]

namespace N
{{
    class C {{ }}
}}";
        var expectedDiagnostic = VerifyCS.Diagnostic(AbstractRemoveUnnecessaryAttributeSuppressionsDiagnosticAnalyzer.LegacyFormatTargetDescriptor)
                                    .WithLocation(0)
                                    .WithArguments(target);

        await VerifyCS.VerifyCodeFixAsync(input, expectedDiagnostic, input);
    }
}
