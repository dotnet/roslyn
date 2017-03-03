// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.ReadonlyReferences)]
    public class CodeGenInParametersTests : CompilingTestBase
    {
        [Fact]
        public void RefReturnParamAccess()
        {
            var text = @"
class Program
{
    static ref readonly int M(in int x)
    {
        return ref x;
    }
}
";

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: false);

            comp.VerifyIL("Program.M(ref readonly int)", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
        }

        [Fact]
        public void RefReturnParamAccess1()
        {
            var text = @"
class Program
{
    static ref readonly int M(ref readonly int x)
    {
        return ref x;
    }
}
";

            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular, verify: false);

            comp.VerifyIL("Program.M(ref readonly int)", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
        }

        [Fact]
        public void BindingInvalidRefInCombination()
        {
            var text = @"
class Program
{
    // should be a syntax error
    // just make sure binder is ok with this
    static ref readonly int M(in ref readonly int x)
    {
        return ref x;
    }

    // should be a syntax error
    // just make sure binder is ok with this
    static ref readonly int M1( ref in readonly int x)
    {
        return ref x;
    }
}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,34): error CS8404:  The parameter modifier 'ref' cannot be used with 'in' 
                //     static ref readonly int M(in ref readonly int x)
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "ref").WithArguments("ref", "in").WithLocation(6, 34),
                // (6,38): error CS8404:  The parameter modifier 'readonly' cannot be used with 'in' 
                //     static ref readonly int M(in ref readonly int x)
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "readonly").WithArguments("readonly", "in").WithLocation(6, 38),
                // (13,37): error CS8404:  The parameter modifier 'in' cannot be used with 'ref' 
                //     static ref readonly int M1( ref in readonly int x)
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "in").WithArguments("in", "ref").WithLocation(13, 37)
            );
        }

        [Fact]
        public void ReadonlyParamCannotAssign()
        {
            var text = @"
class Program
{
    static void M(in int arg1, in (int Alice, int Bob) arg2)
    {
        arg1 = 1;
        arg2.Alice = 2;

        arg1 ++;
        arg2.Alice --;

        arg1 += 1;
        arg2.Alice -= 2;
    }
}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,9): error CS8208: A readonly parameter cannot be assigned to
                //         arg1 = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyParam, "arg1").WithLocation(6, 9),
                // (7,9): error CS8209: Members of readonly parameter 'ref readonly (int Alice, int Bob)' cannot be assigned to
                //         arg2.Alice = 2;
                Diagnostic(ErrorCode.ERR_AssignReadonlyParam2, "arg2.Alice").WithArguments("ref readonly (int Alice, int Bob)").WithLocation(7, 9),
                // (9,9): error CS8208: A readonly parameter cannot be assigned to
                //         arg1 ++;
                Diagnostic(ErrorCode.ERR_AssignReadonlyParam, "arg1").WithLocation(9, 9),
                // (10,9): error CS8209: Members of readonly parameter 'ref readonly (int Alice, int Bob)' cannot be assigned to
                //         arg2.Alice --;
                Diagnostic(ErrorCode.ERR_AssignReadonlyParam2, "arg2.Alice").WithArguments("ref readonly (int Alice, int Bob)").WithLocation(10, 9),
                // (12,9): error CS8208: A readonly parameter cannot be assigned to
                //         arg1 += 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyParam, "arg1").WithLocation(12, 9),
                // (13,9): error CS8209: Members of readonly parameter 'ref readonly (int Alice, int Bob)' cannot be assigned to
                //         arg2.Alice -= 2;
                Diagnostic(ErrorCode.ERR_AssignReadonlyParam2, "arg2.Alice").WithArguments("ref readonly (int Alice, int Bob)").WithLocation(13, 9)
            );
        }

        [Fact]
        public void ReadonlyParamCannotAssignByref()
        {
            var text = @"
class Program
{
    static void M(in int arg1, in (int Alice, int Bob) arg2)
    {
        ref var y = ref arg1;
        ref int a = ref arg2.Alice;
    }
}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,25): error CS8206: A readonly parameter cannot be used as a ref or out value
                //         ref var y = ref arg1;
                Diagnostic(ErrorCode.ERR_RefReadonlyParam, "arg1").WithLocation(6, 25),
                // (7,25): error CS8207: Members of readonly parameter 'ref readonly (int Alice, int Bob)' cannot be used as a ref or out value
                //         ref int a = ref arg2.Alice;
                Diagnostic(ErrorCode.ERR_RefReadonlyParam2, "arg2.Alice").WithArguments("ref readonly (int Alice, int Bob)").WithLocation(7, 25)
            );
        }

        [Fact]
        public void ReadonlyParamCannotReturnByOrdinaryRef()
        {
            var text = @"
class Program
{
    static ref int M(in int arg1, in (int Alice, int Bob) arg2)
    {
        bool b = true;

        if (b)
        {
            return ref arg1;
        }
        else
        {
            return ref arg2.Alice;
        }
    }
}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (10,24): error CS8206: A readonly parameter cannot be used as a ref or out value
                //             return ref arg1;
                Diagnostic(ErrorCode.ERR_RefReadonlyParam, "arg1").WithLocation(10, 24),
                // (14,24): error CS8207: Members of readonly parameter 'ref readonly (int Alice, int Bob)' cannot be used as a ref or out value
                //             return ref arg2.Alice;
                Diagnostic(ErrorCode.ERR_RefReadonlyParam2, "arg2.Alice").WithArguments("ref readonly (int Alice, int Bob)").WithLocation(14, 24)
            );
        }

        [Fact]
        public void ReadonlyParamCanReturnByRefReadonly()
        {
            var text = @"
class Program
{
    static ref readonly int M(in int arg1, in (int Alice, int Bob) arg2)
    {
        bool b = true;

        if (b)
        {
            return ref arg1;
        }
        else
        {
            return ref arg2.Alice;
        }
    }
}
";

            var comp = CompileAndVerify(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular, verify: false);

            comp.VerifyIL("Program.M", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  brfalse.s  IL_0005
  IL_0003:  ldarg.0
  IL_0004:  ret
  IL_0005:  ldarg.1
  IL_0006:  ldflda     ""int System.ValueTuple<int, int>.Item1""
  IL_000b:  ret
}");
        }
    }
}
