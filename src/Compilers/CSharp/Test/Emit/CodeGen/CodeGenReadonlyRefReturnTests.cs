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
    public class CodeGenReadonlyRefReturnTests : CompilingTestBase
    {
        [Fact]
        public void RefReturnArrayAccess()
        {
            var text = @"
class Program
{
    static readonly ref int M()
    {
        return ref (new int[1])[0];
    }
}
";

            //PROTOTYPE(readonlyRefs): this should work for now because readonly is treated as regular ref
            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular);

            comp.VerifyIL("Program.M()", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     ""int""
  IL_0006:  ldc.i4.0
  IL_0007:  ldelema    ""int""
  IL_000c:  ret
}");
        }
    }
}
