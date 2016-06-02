// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ExtensionEverythingTests : CompilingTestBase
    {
        private static readonly CSharpParseOptions parseOptions = TestOptions.Regular.WithExtensionEverythingFeature();

        // PROTOTYPE: Test extending struct/interface/other odd types (types that aren't NamedTypeSymbol? - array, pointer, type parameter, dynamic). Also test no "base type" at all.
        // PROTOTYPE: "extension struct" etc. syntax
        // PROTOTYPE: extend static class (e.g. Math). Test both calling and defining (e.g. define instance method)
        // PROTOTYPE: Extending a COM class (LocalRewriter.MakeArguments special-cases it)
        // PROTOTYPE: Extension .Add collection initializer, foreach and other duck typing calls
        // PROTOTYPE: Extension method/indexer with params array
        // PROTOTYPE: Extension method converted to delegate
        // PROTOTYPE: `using` all the things
        // PROTOTYPE: DllImport/extern on things in ext class
        // PROTOTYPE: Call extension methods directly (so priority to ext method). Both on imported and source methods.
        // PROTOTYPE: Extension methods/indexers (static/instance) with argument order oddness and default params

        [Fact]
        public void SuccessTest()
        {
            var text = @"
#define __DEMO__

class BaseClass
{
}

extension class ExtClass : BaseClass
{
    public void Ext()
    {
        System.Console.WriteLine(""Hello, world!"");
    }
}

class Program
{
    static void Main(string[] args)
    {
        new BaseClass().Ext();
    }
}
";

            var comp = CreateCompilationWithMscorlibAndSystemCore(text, parseOptions: parseOptions);
            CompileAndVerify(source: text,
                parseOptions: parseOptions,
                additionalRefs: new[] { SystemCoreRef }, expectedOutput: "Hello, world!");
        }

        [Fact]
        public void VariousMemberTests()
        {
            var text = @"
#define __DEMO__

using System;

class BaseClass
{
}

extension class ExtClass : BaseClass
{
    public int ExtMethod()
    {
        return 2;
    }
    public int ExtProp
    {
        get { return 2; }
        set { Console.Write(value); }
    }
    public static int ExtStaticMethod()
    {
        return 2;
    }
    public static int ExtStaticProp
    {
        get { return 2; }
        set { Console.Write(value); }
    }
}

class Program
{
    static void Main(string[] args)
    {
        var obj = new BaseClass();
        Console.Write(obj.ExtMethod());
        Console.Write(obj.ExtProp);
        obj.ExtProp = 2;
        Console.Write(BaseClass.ExtStaticMethod());
        Console.Write(BaseClass.ExtStaticProp);
        BaseClass.ExtStaticProp = 2;
    }
}
";

            var comp = CreateCompilationWithMscorlibAndSystemCore(text, parseOptions: parseOptions);
            CompileAndVerify(source: text,
                parseOptions: parseOptions,
                additionalRefs: new[] { SystemCoreRef }, expectedOutput: "222222")
                .VerifyIL("Program.Main", @"{
  // Code size       60 (0x3c)
  .maxstack  2
  IL_0000:  newobj     ""BaseClass..ctor()""
  IL_0005:  dup
  IL_0006:  call       ""int ExtClass.ExtMethod(BaseClass)""
  IL_000b:  call       ""void System.Console.Write(int)""
  IL_0010:  dup
  IL_0011:  call       ""int ExtClass.get_ExtProp(BaseClass)""
  IL_0016:  call       ""void System.Console.Write(int)""
  IL_001b:  ldc.i4.2
  IL_001c:  call       ""void ExtClass.set_ExtProp(BaseClass, int)""
  IL_0021:  call       ""int ExtClass.ExtStaticMethod()""
  IL_0026:  call       ""void System.Console.Write(int)""
  IL_002b:  call       ""int ExtClass.ExtStaticProp.get""
  IL_0030:  call       ""void System.Console.Write(int)""
  IL_0035:  ldc.i4.2
  IL_0036:  call       ""void ExtClass.ExtStaticProp.set""
  IL_003b:  ret
}");
        }

        [Fact]
        public void ExtensionMethodInExtensionClass()
        {
            var text = @"
class Base
{
}

extension class Ext : Base {
    public static void ExtMethod(this Base param)
    {
    }
}
";

            var comp = CreateCompilationWithMscorlibAndSystemCore(text, parseOptions: parseOptions);
            comp.VerifyDiagnostics(
                // (7,24): error CS8207: An extension method cannot be defined in an extension class.
                //     public static void ExtMethod(this Base param)
                Diagnostic(ErrorCode.ERR_ExtensionMethodInExtensionClass, "ExtMethod").WithLocation(7, 24)
            );
        }
    }
}
