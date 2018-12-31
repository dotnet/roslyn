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
    }
}
