// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.JsonStringDetector;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.JsonStringDetector
{
    public class JsonStringDetectorTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpJsonStringDetectorDiagnosticAnalyzer(), new CSharpEnableIDEJsonFeaturesCodeFixProvider());
         
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
