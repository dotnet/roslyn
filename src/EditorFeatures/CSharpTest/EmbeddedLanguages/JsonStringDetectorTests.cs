// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages;
using Microsoft.CodeAnalysis.CSharp.Features.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EmbeddedLanguages
{
    public class JsonStringDetectorTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpEmbeddedLanguageDiagnosticAnalyzer(), new CSharpEmbeddedLanguageCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsDetectJsonString)]
        public async Task TestStrict()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void Goo()
    {
        var j = [||]""{ \""a\"": 0 }"";
    }
}",
@"
class C
{
    void Goo()
    {
        var j = /*lang=json,strict*/ ""{ \""a\"": 0 }"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsDetectJsonString)]
        public async Task TestNonStrict()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void Goo()
    {
        var j = [||]""{ 'a': 00 }"";
    }
}",
@"
class C
{
    void Goo()
    {
        var j = /*lang=json*/ ""{ 'a': 00 }"";
    }
}");
        }
    }
}
