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

        [Fact]
        [WorkItem(34726, "https://github.com/dotnet/roslyn/issues/34726")]
        public void IfStatement_LocalInABranch_PreservesLocalDuringLowering()
        {
            var source = @"
class TestClass
{
    public static string Method(object obj)
    {
        if (obj != null)
        {
            return obj is string str ? str : ""not string"";
        }
        else
        {
            return ""null"";
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        System.Console.Write(TestClass.Method(null));
        System.Console.Write(';');
        System.Console.Write(TestClass.Method(1));
        System.Console.Write(';');
        System.Console.Write(TestClass.Method(""string""));
     }
}";

            var expectedIL = @"{
  // Code size       27 (0x1b)
  .maxstack  1
  .locals init (string V_0) //str
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0009
  IL_0003:  ldstr      ""null""
  IL_0008:  ret
  IL_0009:  ldarg.0
  IL_000a:  isinst     ""string""
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  brtrue.s   IL_0019
  IL_0013:  ldstr      ""not string""
  IL_0018:  ret
  IL_0019:  ldloc.0
  IL_001a:  ret
}";

            CompileAndVerify(source, expectedOutput: "null;not string;string")
                .VerifyIL("TestClass.Method", expectedIL);
        }
    }
}