// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using System.Threading;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LocalFunctionTests : CSharpTestBase
    {
        [Fact]
        public void LocalFunctionEndToEnd()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        void Local()
        {
            Console.WriteLine(""Hello, world!"");
        }
        Local();
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"
Hello, world!
");
        }

        [Fact]
        public void LocalFunctionEnumerator()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        IEnumerable<int> Local()
        {
            yield return 2;
        }
        Console.WriteLine(string.Join("","", Local()));
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"
2
");
        }
    }
}
