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
                // (6,34): error CS8205:  The parameter modifier 'ref' cannot be used with 'in' 
                //     static ref readonly int M(in ref readonly int x)
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "ref").WithArguments("ref", "in").WithLocation(6, 34),
                // (6,38): error CS8205:  The parameter modifier 'readonly' cannot be used with 'in' 
                //     static ref readonly int M(in ref readonly int x)
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "readonly").WithArguments("readonly", "in").WithLocation(6, 38),
                // (13,37): error CS8205:  The parameter modifier 'in' cannot be used with 'ref' 
                //     static ref readonly int M1( ref in readonly int x)
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "in").WithArguments("in", "ref").WithLocation(13, 37)
            );
        }
    }
}
