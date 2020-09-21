// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UsePatternMatching;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UsePatternMatching
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpUseNotPatternDiagnosticAnalyzer,
        CSharpUseNotPatternCodeFixProvider>;

    public partial class CSharpUseNotPatternTests
    {
#if !CODE_STYLE

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNotPattern)]
        [WorkItem(46699, "https://github.com/dotnet/roslyn/issues/46699")]
        public async Task UseNotPattern()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"class C
{
    void M(object x)
    {
        if (!(x [|is|] string s))
        {
        }
    }
}",
                FixedCode =
@"class C
{
    void M(object x)
    {
        if (x is not string s)
        {
        }
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNotPattern)]
        public async Task UnavailableInCSharp8()
        {
            await new VerifyCS.Test
            {
                TestCode =
@"class C
{
    void M(object x)
    {
        if (!(x is string s))
        {
        }
    }
}",
                LanguageVersion = LanguageVersion.CSharp8,
            }.RunAsync();
        }

#endif
    }
}
