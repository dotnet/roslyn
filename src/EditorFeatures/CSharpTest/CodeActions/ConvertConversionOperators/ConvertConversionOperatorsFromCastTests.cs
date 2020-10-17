// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeRefactoringVerifier<Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertConversionOperators.CSharpConvertConversionOperatorsFromCastRefactoringProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.ConvertConversionOperators
{
    [Trait(Traits.Feature, Traits.Features.ConvertConversionOperators)]
    public class ConvertConversionOperatorsFromCastTests
    {


        [Fact]
        public async Task ConvertFromExplicitToAs()
        {
            const string InitialMarkup = @"
class Program
{
    public static void Main()
    {
        var x = ([||]object)1;
    }
}";
            const string ExpectedMarkup = @"
class Program
{
    public static void Main()
    {
        var x = 1 as object;
    }
}";
            await new VerifyCS.Test
            {
                TestCode = InitialMarkup,
                FixedCode = ExpectedMarkup,
                CodeActionValidationMode = CodeActionValidationMode.Full,
            }.RunAsync();
        }

        [Fact]
        public async Task ConvertFromExplicitToAs_ValueType()
        {
            const string InitialMarkup = @"
class Program
{
    public static void Main()
    {
        var x = ([||]byte)1;
    }
}";
            await new VerifyCS.Test
            {
                TestCode = InitialMarkup,
                OffersEmptyRefactoring = true, //This flag does nothing. How do I test for "Refactoring missing"?
                CodeActionValidationMode = CodeActionValidationMode.None,
            }.RunAsync();
        }

        [Fact]
        public async Task ConvertFromExplicitToAs_ValueTypeConstraint()
        {
            const string InitialMarkup = @"
public class C
{
    public void M<T>() where T: struct
    {
        var o = new object();
        var t = (T[||])o;
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = InitialMarkup,
                OffersEmptyRefactoring = true, //This flag does nothing. How do I test for "Refactoring missing"?
                CodeActionValidationMode = CodeActionValidationMode.None,
            }.RunAsync();
        }
    }
}
