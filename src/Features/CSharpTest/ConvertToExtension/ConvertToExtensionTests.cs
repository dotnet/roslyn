// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.ConvertToExtension;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.ConvertToExtension;

using VerifyCS = CSharpCodeRefactoringVerifier<ConvertToExtensionCodeRefactoringProvider>;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.CodeActionsConvertToExtension)]
public sealed class ConvertToExtensionTests
{
    [Fact]
    public async Task TestBaseCase()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            static class C
            {
                [||]public static void M(this int i) { }
            }
            """,
            FixedCode = """
            static class C
            {
                extension(int i)
                {
                    public void M() { }
                }
            }
            """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }
}
