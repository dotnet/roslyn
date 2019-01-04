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
    }
}
