// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppressions;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UpdateLegacySuppressions;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UpdateLegacySuppressions;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpRemoveUnnecessaryAttributeSuppressionsDiagnosticAnalyzer,
    UpdateLegacySuppressionsCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUpdateLegacySuppressions)]
[WorkItem("https://github.com/dotnet/roslyn/issues/44362")]
public sealed class UpdateLegacySuppressionsTests
{
    [Theory, CombinatorialData]
    public void TestStandardProperty(AnalyzerProperty property)
        => VerifyCS.VerifyStandardProperty(property);

    // Namespace
    [InlineData("namespace", "N", "~N:N")]
    // Type
    [InlineData("type", "N.C+D", "~T:N.C.D")]
    // Field
    [InlineData("member", "N.C.#F", "~F:N.C.F")]
    // Property
    [InlineData("member", "N.C.#P", "~P:N.C.P")]
    // Method
    [InlineData("member", "N.C.#M", "~M:N.C.M")]
    // Generic method with parameters
    [InlineData("member", "N.C.#M2(!!0)", "~M:N.C.M2``1(``0)~System.Int32")]
    // Event
    [InlineData("member", "e:N.C.#E", "~E:N.C.E")]
    [Theory]
    public async Task LegacySuppressions(string scope, string target, string fixedTarget)
    {
        var expectedDiagnostic = VerifyCS.Diagnostic(AbstractRemoveUnnecessaryAttributeSuppressionsDiagnosticAnalyzer.LegacyFormatTargetDescriptor)
                                    .WithLocation(0)
                                    .WithArguments(target);
        await VerifyCS.VerifyCodeFixAsync($$"""
            [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "Id: Title", Scope = "{{scope}}", Target = {|#0:"{{target}}"|})]

            namespace N
            {
                class C
                {
                    private int F;
                    public int P { get; set; }
                    public void M() { }
                    public int M2<T>(T t) => 0;
                    public event System.EventHandler<int> E;

                    class D
                    {
                    }
                }
            }
            """, expectedDiagnostic, $$"""
            [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "Id: Title", Scope = "{{scope}}", Target = "{{fixedTarget}}")]

            namespace N
            {
                class C
                {
                    private int F;
                    public int P { get; set; }
                    public void M() { }
                    public int M2<T>(T t) => 0;
                    public event System.EventHandler<int> E;

                    class D
                    {
                    }
                }
            }
            """);
    }
}
