using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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

        [Fact]
        public void UsingVariableCanBeInitializedWithExistingDisposable()
        {
            var source = @"
using System;
class C2
{
    public void Dispose()
    {
        Console.Write(""Disposed; ""); 
    }
}
class C
{
    static void Main()
    {
        var x = new C2();
        using var x2 = x;
        using var x3 = x2;
        x = null;
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "Disposed; Disposed; ");
        }

        [Fact]
        public void UsingVariableCanBeInitializedWithExistingDisposableInASingleStatement()
        {
            var source = @"
using System;
class C2
{
    public void Dispose()
    {
        Console.Write(""Disposed; ""); 
    }
}
class C
{
    static void Main()
    {
        using C2 x = new C2(), x2 = x;
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "Disposed; Disposed; ");
        }

        [Fact]
        public void UsingVariableCannotBeReAssigned()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        using var x = (IDisposable)null;
        x = null;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,9): error CS1656: Cannot assign to 'x' because it is a 'using variable'
                //         x = null;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "x").WithArguments("x", "using variable").WithLocation(8, 9)
                );
        }

        [Fact]
        public void VariableCannotBeReAssignedAsUsingVariable()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var x = (IDisposable)null;
        using var x2 = x;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingVariableCannotBeUsedAsOutVariable()
        {
            var source = @"
using System;
class C
{
    static void Consume(out IDisposable x) 
    {
        x = null;
    }

    static void Main()
    {
        using var x = (IDisposable)null;
        Consume(out x);
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (13,21): error CS1657: Cannot use 'x' as a ref or out value because it is a 'using variable'
                //         Consume(out x);
                Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "x").WithArguments("x", "using variable").WithLocation(13, 21)
                );
        }

        [Fact]
        public void UsingVariableMustHaveInitializer()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        using IDisposable x;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,27): error CS0210: You must provide an initializer in a fixed or using statement declaration
                //         using IDisposable x;
                Diagnostic(ErrorCode.ERR_FixedMustInit, "x").WithLocation(7, 27)
                );
        }

        [Fact]
        public void UsingVariableFromExpression()
        {
            var source = @"
using System;
class C
{
    static IDisposable GetDisposable()
    {
        return null;
    }

    static void Main()
    {
        using IDisposable x = GetDisposable();
    }
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingVariableFromAsyncExpression()
        {
            var source = @"
using System;
using System.Threading.Tasks;
class C
{
    static async Task<IDisposable> GetDisposable()
    {
        await Task.CompletedTask;
        return null;
    }

    static async Task Main()
    {
        using IDisposable x = await GetDisposable();
    }
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }
    }
}
