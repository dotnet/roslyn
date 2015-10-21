// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenUtf8Tests : CSharpTestBase
    {
        [Fact]
        public void TestUtf8Literal001
            ()
        {
            var source = @"

class Program
{
    static void Main()
    {
        var ut8 = ""hello utf8""u8;
        System.Console.WriteLine(ut8.ToString());
    }
}

namespace System.Text.Utf8
{
    class Utf8String
    {
        private byte[] arr;

        public Utf8String(byte[] arr)
        {
            this.arr = arr;
        }

        public override string ToString()
        {
            var s = System.Text.UTF8Encoding.UTF8.GetString(arr);
            return s;
        }
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: "hello utf8");
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main", @"
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldc.i4.s   10
  IL_0002:  newarr     ""byte""
  IL_0007:  dup
  IL_0008:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=10 <PrivateImplementationDetails>.329D7DB2011B5BC1330304286AC52613D8603CDA""
  IL_000d:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0012:  newobj     ""System.Text.Utf8.Utf8String..ctor(byte[])""
  IL_0017:  callvirt   ""string object.ToString()""
  IL_001c:  call       ""void System.Console.WriteLine(string)""
  IL_0021:  ret
}
");
        }

        [Fact]
        public void TestUtf8Literal002
    ()
        {
            var source = @"

class Program
{
    static void Main()
    {
        var ut8 = ""hello utf8""u8;
        System.Console.WriteLine(ut8.ToString());
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
    // (7,19): error CS0518: Predefined type 'System.Text.Utf8.Utf8String' is not defined or imported
    //         var ut8 = "hello utf8"u8;
    Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"""hello utf8""u8").WithArguments("System.Text.Utf8.Utf8String").WithLocation(7, 19),
    // (7,19): error CS0656: Missing compiler required member 'System.Text.Utf8.Utf8String..ctor'
    //         var ut8 = "hello utf8"u8;
    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"""hello utf8""u8").WithArguments("System.Text.Utf8.Utf8String", ".ctor").WithLocation(7, 19)
);
        }

    }
}
