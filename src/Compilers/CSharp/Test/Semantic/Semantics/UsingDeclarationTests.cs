using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics
{
    /// <summary>
    /// Tests related to binding (but not lowering) using declarations (i.e. using var x = ...).
    /// </summary>
    public class UsingDeclarationTests : CompilingTestBase
    {
        [Fact]
        public void UsingVariableIsNotReportedAsUnused()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        using var x = (IDisposable)null;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void DisallowGoToAcrossUsingDeclarationsForward()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        goto label1;
        using var x = (IDisposable)null;

        label1:
        return;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,9): error CS8641: A goto target within the same block can not cross a using declaration.
                //         goto label1;
                Diagnostic(ErrorCode.ERR_GoToJumpOverUsingVar, "goto label1;").WithLocation(7, 9),
                // (8,9): warning CS0162: Unreachable code detected
                //         using var x = (IDisposable)null;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "using").WithLocation(8, 9)
                );
        }

        [Fact]
        public void DisallowGoToAcrossUsingDeclarationsBackwards()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        label1:
        using var x = (IDisposable)null;

        goto label1;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (10,9): error CS8641: A goto target within the same block can not cross a using declaration.
                //         goto label1;
                Diagnostic(ErrorCode.ERR_GoToJumpOverUsingVar, "goto label1;").WithLocation(10, 9)
                );
        }

        [Fact]
        public void AllowGoToAcrossUsingDeclarationsBackwardsWhenFromADifferentBlock()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        label1:
        using var x = (IDisposable)null;
        {
            goto label1;
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }
    }
}
