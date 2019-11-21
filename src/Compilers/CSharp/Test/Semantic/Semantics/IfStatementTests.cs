using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
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
        [WorkItem(34726, "https://github.com/dotnet/roslyn/issues/34726")]
        public void IfStatement_ConstantCondition_ReplacedWithTheBranch(string condition, string ilConstant)
        {
            var source = @"
class TestClass
{
    int Method()
    {
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

        [Fact]
        [WorkItem(34726, "https://github.com/dotnet/roslyn/issues/34726")]
        public void IfStatement_ConstantBoolBranches_ReplacedWithTheCondition()
        {
            var source = @"
class TestClass
{
    bool Method(object o)
    {
         if (o is object)
         {
             return true;
         }
         else
         {
             return false;
         }
    }
}";

            var expectedIL = @"{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldnull
  IL_0002:  cgt.un
  IL_0004:  ret
}";

            CompileAndVerify(source)
                .VerifyIL("TestClass.Method", expectedIL);
        }
    }
}