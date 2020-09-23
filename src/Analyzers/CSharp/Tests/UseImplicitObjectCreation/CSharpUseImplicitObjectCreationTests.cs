// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.UseImplicitObjectCreation;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitObjectCreation)]
        public async Task TestWithObjectInitializer()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
class C
{
    C c = new [|C|]() { };
}",
                FixedCode = @"
class C
{
    C c = new() { };
}",
                LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitObjectCreation)]
        public async Task TestWithObjectInitializerWithoutArguments()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
class C
{
    C c = new [|C|] { };
}",
                FixedCode = @"
class C
{
    C c = new() { };
}",
                LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitObjectCreation)]
        public async Task TestWithTriviaAfterNew()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
class C
{
    C c = new /*x*/ [|C|]();
}",
                FixedCode = @"
class C
{
    C c = new /*x*/ ();
}",
                LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitObjectCreation)]
        public async Task TestNotWithDifferentTypes()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
class C
{
    object c = new C();
}",
                LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitObjectCreation)]
        public async Task TestNotWithErrorTypes()
        {
            await new VerifyCS.Test
            {
                TestState = {
                    Sources =
                    {
                        @"
class C
{
    E c = new E();
}"
                    },
                    ExpectedDiagnostics =
                    {
                        // /0/Test0.cs(4,5): error CS0246: The type or namespace name 'E' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(4, 5, 4, 6).WithArguments("E"),
                        // /0/Test0.cs(4,15): error CS0246: The type or namespace name 'E' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(4, 15, 4, 16).WithArguments("E"),
                    }
                },
                LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitObjectCreation)]
        public async Task TestNotWithArrayTypes()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
class C
{
    int[] c = new int[0];
}",
                LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
            }.RunAsync();
        }
    }
}
