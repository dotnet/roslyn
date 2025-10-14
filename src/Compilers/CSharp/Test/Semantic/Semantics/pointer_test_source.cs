using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class PointerNullConditionalTest : CSharpTestBase
    {
        [Fact]
        public void Test1()
        {
            var source = @"
public unsafe class A
{
    public byte* Ptr;
}
class Test
{
    void M()
    {
        var x = new A();
        byte* ptr = x?.Ptr;
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
            // Print all diagnostics
            comp.VerifyEmitDiagnostics();
        }
    }
}
