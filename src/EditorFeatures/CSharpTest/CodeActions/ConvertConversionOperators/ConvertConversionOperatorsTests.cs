// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertConversionOperators;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.ConvertConversionOperators
{
    [Trait(Traits.Feature, Traits.Features.ConvertConversionOperators)]
    public class ConvertConversionOperatorsTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpConvertConversionOperatorsRefactoringProvider();

        [Fact]
        public async Task ConvertFromAsToExplicit()
        {
            await TestInRegularAndScriptAsync(@"
class Program
{
    public static void Main()
    {
        var x = 1 as[||] object;
    }
}", @"
class Program
{
    public static void Main()
    {
        var x = (object)1;
    }
}");
        }

        [Fact]
        public async Task ConvertFromAsToExplicit_ValueType()
        {
            await TestInRegularAndScriptAsync(@"
class Program
{
    public static void Main()
    {
        var x = 1 as[||] byte;
    }
}", @"
class Program
{
    public static void Main()
    {
        var x = (byte)1;
    }
}");
        }

        [Fact]
        public async Task ConvertFromAsToExplicit_NoTypeSyntaxRightOfAs()
        {
            await TestMissingInRegularAndScriptAsync(@"
class Program
{
    public static void Main()
    {
        var x = 1 as[||] 1;
    }
}");
        }

        [Fact]
        public async Task ConvertFromExplicitToAs()
        {
            await TestInRegularAndScriptAsync(@"
class Program
{
    public static void Main()
    {
        var x = ([||]object)1;
    }
}", @"
class Program
{
    public static void Main()
    {
        var x = 1 as object;
    }
}");
        }

        [Fact]
        public async Task ConvertFromExplicitToAs_ValueType()
        {
            await TestMissingInRegularAndScriptAsync(@"
class Program
{
    public static void Main()
    {
        var x = ([||]byte)1;
    }
}");
        }
    }
}
