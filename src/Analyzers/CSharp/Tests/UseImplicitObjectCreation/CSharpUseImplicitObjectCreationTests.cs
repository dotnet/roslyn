// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.UseImplicitObjectCreation;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseImplicitObjectCreationTests
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpUseImplicitObjectCreationDiagnosticAnalyzer,
        CSharpUseImplicitObjectCreationCodeFixProvider>;

    public partial class UseImplicitObjectCreationTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitObjectCreation)]
        public async Task TestMissingBeforeCSharp9()
        {
            var source = @"
class C
{
    C c = new C();
}";
            await new VerifyCS.Test
            {
                LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp8,
                TestCode = source,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitObjectCreation)]
        public async Task TestAfterCSharp9()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
class C
{
    C c = new [|C|]();
}",
                FixedCode = @"
class C
{
    C c = new();
}",
                LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
            }.RunAsync();
        }
    }
}
