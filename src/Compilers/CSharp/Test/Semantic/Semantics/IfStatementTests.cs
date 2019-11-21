using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics
{
    public class IfStatementTests : CSharpTestBase
    {
        [Theory]
        [InlineData("true", "1")]
        [InlineData("trueConstant", "1")]
        [InlineData("false", "2")]
        [InlineData("falseConstant", "2")]
        public void IfStatement_ConstantCondition_ReplacedWithTheBranch(string condition, string ilConstant)
        {
            var source = @"
class TestClass
{
    int Method()
    {
        var trueVariable = true;
        var falseVariable = false;

        const bool trueConstant = true;
        const bool falseConstant = false;

        if (" + condition + @")
        {
            return 1;
        }
        else
        {
            return 2;
        }
    }
}";

            var expectedIL = @"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4." + ilConstant + @"
  IL_0001:  ret
}";

            CompileAndVerify(source)
                .VerifyIL("TestClass.Method", expectedIL);
        }
    }
}