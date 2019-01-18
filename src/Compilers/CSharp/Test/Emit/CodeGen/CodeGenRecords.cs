// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    [CompilerTrait(CompilerFeature.Records)]
    public sealed class CodeGenRecords : EmitMetadataTestBase
    {
        [Fact]
        public void SimpleRecordWithProperties()
        {
            CompileAndVerify(@"
using System;
class Point(int x, int y);
class C
{

    public static void Main()
    {
        var p = new Point(1, 3);
        Console.WriteLine(p.x + "" "" + p.y);
    }
}", expectedOutput: "1 3");
        }

        [Fact]
        public void RecordWithInitializer()
        {
            CompileAndVerify(@"
using System;
class Point(int x, int y)
{
    public int Z { get; } = 5;
}
class C
{

    public static void Main()
    {
        var p = new Point(1, 3);
        Console.WriteLine(p.x + "" "" + p.y + "" "" + p.Z);
    }
}", expectedOutput: "1 3 5");
        }

        [Fact]
        public void RecordEquality()
        {
            CompileAndVerify(@"
using System;
class Point(int x, int y);
class C
{
    public static void Main()
    {
        var p1 = new Point(1, 3);
        var p2 = new Point(1, 3);
        var p3 = new Point(1, 5);
        Console.WriteLine(object.ReferenceEquals(p1, p2));
        Console.WriteLine(p1.Equals(p2));
        Console.WriteLine(p1.Equals(p3));
    }
}", expectedOutput: @"
False
True
False");
        }

        [Fact]
        public void RecordObjEquality()
        {
            CompileAndVerify(@"
using System;
class Point(int x, int y);
class C
{
    public static void Main()
    {
        var p1 = new Point(1, 3);
        object p2 = new Point(1, 3);
        object p3 = new Point(1, 5);
        Console.WriteLine(object.ReferenceEquals(p1, p2));
        Console.WriteLine(p1.Equals(p2));
        Console.WriteLine(p1.Equals(p3));
    }
}", expectedOutput: @"
False
True
False");
        }

        [Fact]
        public void RecordGetHashCode()
        {
            var verifier = CompileAndVerify(@"class Point(int x, int y);");
            verifier.VerifyIL("Point.GetHashCode", @"
{
  // Code size       52 (0x34)
  .maxstack  3
  IL_0000:  ldc.i4     0x66610aa6
  IL_0005:  ldc.i4     0xa5555529
  IL_000a:  mul
  IL_000b:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0010:  ldarg.0
  IL_0011:  call       ""int Point.x.get""
  IL_0016:  callvirt   ""int System.Collections.Generic.EqualityComparer<int>.GetHashCode(int)""
  IL_001b:  add
  IL_001c:  ldc.i4     0xa5555529
  IL_0021:  mul
  IL_0022:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0027:  ldarg.0
  IL_0028:  call       ""int Point.y.get""
  IL_002d:  callvirt   ""int System.Collections.Generic.EqualityComparer<int>.GetHashCode(int)""
  IL_0032:  add
  IL_0033:  ret
}");
        }
    }
}
