// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertConversionOperators;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.ConvertConversionOperators
{
    [Trait(Traits.Feature, Traits.Features.ConvertConversionOperators)]
    public class AddAwaitTests : AbstractCSharpCodeActionTest
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
    }
}
