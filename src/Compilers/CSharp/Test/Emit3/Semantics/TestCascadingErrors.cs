using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class CascadingRecordErrorsTest : CSharpTestBase
    {
        [Fact]
        public void RecordInheritingFromNonRecord_OnlyReportsBaseError()
        {
            var source = @"
class Base { }

record Derived : Base { }
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,18): error CS8864: Records may only inherit from object or another record
                // record Derived : Base { }
                Diagnostic(ErrorCode.ERR_BadRecordBase, "Base").WithLocation(4, 18));
        }
    }
}
