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

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics
{
    public class ParamsTests : CSharpTestBase
    {
        [Fact]
        public void Test()
        {
            var source = @"
using System;

unsafe class C
{
	static void M(params Span<C> a) {}

	public static void Main()
	{
		var x = new C();
		M(x, x, x);
	}
}";

            CreateCompilationWithMscorlibAndSpan(source, options: TestOptions.DebugExe.WithAllowUnsafe(true))
                .VerifyEmitDiagnostics();
        }

        [Fact]
        public void TestGoodParams()
        {
            var source = @"
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

static class C
{
    static void Dump(this IEnumerable p)
    {
        var e = p?.Cast<object>();
        Console.WriteLine(e == null ? ""<null>"" : !e.Any() ? ""<empty>"" : string.Join("", "", e.Select(i => i ?? ""null"")));
    }

    static void M1(params IList<string> p) => p.Dump();
    static void M2(params ICollection<string> p) => p.Dump();
    static void M3(params IEnumerable<string> p) => p.Dump();
    static void M4(params IReadOnlyList<string> p) => p.Dump();
    static void M5(params IReadOnlyCollection<string> p) => p.Dump();

    static void Main()
    {
        M1();
        M1(null);
        M1(null, null);
        M1(""1"", ""2"", ""3"");

        M2();
        M2(null);
        M2(null, null);
        M2(""1"", ""2"", ""3"");

        M3();
        M3(null);
        M3(null, null);
        M3(""1"", ""2"", ""3"");

        M4();
        M4(null);
        M4(null, null);
        M4(""1"", ""2"", ""3"");

        M5();
        M5(null);
        M5(null, null);
        M5(""1"", ""2"", ""3"");
    }
}
";
            var comp = CreateCompilationWithMscorlib45(source, references: new[] { LinqAssemblyRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"
<empty>
<null>
null, null
1, 2, 3
<empty>
<null>
null, null
1, 2, 3
<empty>
<null>
null, null
1, 2, 3
<empty>
<null>
null, null
1, 2, 3
<empty>
<null>
null, null
1, 2, 3
");
        }
    }
}
