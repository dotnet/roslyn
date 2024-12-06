// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.Patterns)]
    public class PatternMatchingTests4 : PatternMatchingTestBase
    {
        [Fact]
        [WorkItem(34980, "https://github.com/dotnet/roslyn/issues/34980")]
        public void PatternMatchOpenTypeCaseDefault()
        {
            var comp = CreateCompilation(@"
class C
{
    public void M<T>(T t)
    {
        switch (t)
        {
            case default:
                break;
        }
    }
}");
            comp.VerifyDiagnostics(
                // (8,18): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //             case default:
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(8, 18));
        }

        [Fact]
        [WorkItem(34980, "https://github.com/dotnet/roslyn/issues/34980")]
        public void PatternMatchOpenTypeCaseDefaultT()
        {
            var comp = CreateCompilation(@"
class C
{
    public void M<T>(T t)
    {
        switch (t)
        {
            case default(T):
                break;
        }
    }
}");
            comp.VerifyDiagnostics(
                // (8,18): error CS0150: A constant value is expected
                //             case default(T):
                Diagnostic(ErrorCode.ERR_ConstantExpected, "default(T)").WithLocation(8, 18));
        }

        [Fact]
        [WorkItem(34980, "https://github.com/dotnet/roslyn/issues/34980")]
        public void PatternMatchGenericParameterToMethodGroup()
        {
            var source = @"
class C
{
    public void M1(object o)
    {
        _ = o is M1;
        switch (o)
        {
            case M1:
                break;
        }
    }
    public void M2<T>(T t)
    {
        _ = t is M2;
        switch (t)
        {
            case M2:
                break;
        }
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (6,18): error CS0428: Cannot convert method group 'M1' to non-delegate type 'object'. Did you intend to invoke the method?
                //         _ = o is M1;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "M1").WithArguments("M1", "object").WithLocation(6, 18),
                // (9,18): error CS0428: Cannot convert method group 'M1' to non-delegate type 'object'. Did you intend to invoke the method?
                //             case M1:
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "M1").WithArguments("M1", "object").WithLocation(9, 18),
                // (15,18): error CS0150: A constant value is expected
                //         _ = t is M2;
                Diagnostic(ErrorCode.ERR_ConstantExpected, "M2").WithLocation(15, 18),
                // (18,18): error CS0150: A constant value is expected
                //             case M2:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "M2").WithLocation(18, 18));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,18): warning CS8974: Converting method group 'M1' to non-delegate type 'object'. Did you intend to invoke the method?
                //         _ = o is M1;
                Diagnostic(ErrorCode.WRN_MethGrpToNonDel, "M1").WithArguments("M1", "object").WithLocation(6, 18),
                // (6,18): error CS0150: A constant value is expected
                //         _ = o is M1;
                Diagnostic(ErrorCode.ERR_ConstantExpected, "M1").WithLocation(6, 18),
                // (9,18): warning CS8974: Converting method group 'M1' to non-delegate type 'object'. Did you intend to invoke the method?
                //             case M1:
                Diagnostic(ErrorCode.WRN_MethGrpToNonDel, "M1").WithArguments("M1", "object").WithLocation(9, 18),
                // (9,18): error CS0150: A constant value is expected
                //             case M1:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "M1").WithLocation(9, 18),
                // (15,18): error CS0150: A constant value is expected
                //         _ = t is M2;
                Diagnostic(ErrorCode.ERR_ConstantExpected, "M2").WithLocation(15, 18),
                // (18,18): error CS0150: A constant value is expected
                //             case M2:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "M2").WithLocation(18, 18));
        }

        [Fact, WorkItem(34980, "https://github.com/dotnet/roslyn/issues/34980")]
        public void PatternMatchGenericParameterToNonConstantExprs()
        {
            var comp = CreateCompilation(@"
class C
{
    public void M<T>(T t)
    {
        switch (t)
        {
            case (() => 0):
                break;
            case stackalloc int[1] { 0 }:
                break;
            case new { X = 0 }:
                break;
        }
    }
}");
            comp.VerifyDiagnostics(
                // (8,18): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'T', with 2 out parameters and a void return type.
                //             case (() => 0):
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(() => 0)").WithArguments("T", "2").WithLocation(8, 18),
                // (8,22): error CS1003: Syntax error, ',' expected
                //             case (() => 0):
                Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",").WithLocation(8, 22),
                // (8,25): error CS1003: Syntax error, ',' expected
                //             case (() => 0):
                Diagnostic(ErrorCode.ERR_SyntaxError, "0").WithArguments(",").WithLocation(8, 25),
                // (10,18): error CS0518: Predefined type 'System.Span`1' is not defined or imported
                //             case stackalloc int[1] { 0 }:
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "stackalloc int[1] { 0 }").WithArguments("System.Span`1").WithLocation(10, 18),
                // (10,18): error CS0150: A constant value is expected
                //             case stackalloc int[1] { 0 }:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "stackalloc int[1] { 0 }").WithLocation(10, 18),
                // (12,18): error CS0150: A constant value is expected
                //             case new { X = 0 }:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "new { X = 0 }").WithLocation(12, 18)
            );
        }

        [Fact]
        public void TestPresenceOfITuple()
        {
            var source =
@"public class C : System.Runtime.CompilerServices.ITuple
{
    public int Length => 1;
    public object this[int i] => null;
    public static void Main()
    {
        System.Runtime.CompilerServices.ITuple t = new C();
        if (t.Length != 1) throw null;
        if (t[0] != null) throw null;
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "");
        }

        [Fact]
        public void ITupleFromObject()
        {
            // - should match when input type is object
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        object t = new C();
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
        Console.WriteLine(new object() is (3, 4, 5)); // false
        Console.WriteLine((null as object) is (3, 4, 5)); // false
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITupleMissing()
        {
            // - should not match when ITuple is missing
            var source =
@"using System;
public class C
{
    public static void Main()
    {
        object t = new C();
        Console.WriteLine(t is (3, 4, 5));
    }
}
";
            // Use a version of the platform APIs that lack ITuple
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (7,32): error CS1061: 'object' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //         Console.WriteLine(t is (3, 4, 5));
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(3, 4, 5)").WithArguments("object", "Deconstruct").WithLocation(7, 32),
                // (7,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'object', with 3 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4, 5));
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4, 5)").WithArguments("object", "3").WithLocation(7, 32)
                );
        }

        [Fact]
        public void ITupleIsClass()
        {
            // - should not match when ITuple is a class
            var source =
@"using System;
namespace System.Runtime.CompilerServices
{
    public class ITuple
    {
        public int Length => 3;
        public object this[int index] => index + 3;
    }
}
public class C : System.Runtime.CompilerServices.ITuple
{
    public static void Main()
    {
        object t = new C();
        Console.WriteLine(t is (3, 4, 5));
    }
}
";
            // Use a version of the platform APIs that lack ITuple
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (15,32): error CS1061: 'object' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //         Console.WriteLine(t is (3, 4, 5));
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(3, 4, 5)").WithArguments("object", "Deconstruct").WithLocation(15, 32),
                // (15,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'object', with 3 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4, 5));
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4, 5)").WithArguments("object", "3").WithLocation(15, 32)
                );
        }

        [Fact]
        public void ITupleFromDynamic()
        {
            // - should match when input type is dynamic
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        dynamic t = new C();
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITupleFromITuple()
        {
            // - should match when input type is ITuple
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        ITuple t = new C();
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITuple_01()
        {
            // - should match when input type extends ITuple and has no Deconstruct (struct)
            var source =
@"using System;
using System.Runtime.CompilerServices;
public struct C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        var t = new C();
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITuple_02()
        {
            // - should match when input type extends ITuple and has inapplicable Deconstruct (struct)
            var source =
@"using System;
using System.Runtime.CompilerServices;
public struct C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        var t = new C();
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
    public void Deconstruct() {}
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITuple_03()
        {
            // - should match when input type extends ITuple and has no Deconstruct (class)
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        var t = new C();
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITuple_04()
        {
            // - should match when input type extends ITuple and has inapplicable Deconstruct (class)
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        var t = new C();
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
    public void Deconstruct() {}
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITuple_05()
        {
            // - should match when input type extends ITuple and has no Deconstruct (type parameter)
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        M(new C());
    }
    public static void M<T>(T t) where T: C
    {
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITuple_10()
        {
            // - should match when input type extends ITuple and has no Deconstruct (type parameter)
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        M(new C());
    }
    public static void M<T>(T t) where T: ITuple
    {
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITuple_11()
        {
            // - should not match when input type is an unconstrained type parameter
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        M(new C());
    }
    public static void M<T>(T t)
    {
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (13,32): error CS1061: 'T' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'T' could be found (are you missing a using directive or an assembly reference?)
                //         Console.WriteLine(t is (3, 4)); // false
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(3, 4)").WithArguments("T", "Deconstruct").WithLocation(13, 32),
                // (13,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'T', with 2 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4)); // false
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4)").WithArguments("T", "2").WithLocation(13, 32),
                // (14,32): error CS1061: 'T' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'T' could be found (are you missing a using directive or an assembly reference?)
                //         Console.WriteLine(t is (3, 4, 5)); // TRUE
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(3, 4, 5)").WithArguments("T", "Deconstruct").WithLocation(14, 32),
                // (14,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'T', with 3 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4, 5)); // TRUE
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4, 5)").WithArguments("T", "3").WithLocation(14, 32),
                // (15,32): error CS1061: 'T' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'T' could be found (are you missing a using directive or an assembly reference?)
                //         Console.WriteLine(t is (3, 0, 5)); // false
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(3, 0, 5)").WithArguments("T", "Deconstruct").WithLocation(15, 32),
                // (15,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'T', with 3 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 0, 5)); // false
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 0, 5)").WithArguments("T", "3").WithLocation(15, 32),
                // (16,32): error CS1061: 'T' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'T' could be found (are you missing a using directive or an assembly reference?)
                //         Console.WriteLine(t is (3, 4, 5, 6)); // false
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(3, 4, 5, 6)").WithArguments("T", "Deconstruct").WithLocation(16, 32),
                // (16,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'T', with 4 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4, 5, 6)); // false
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4, 5, 6)").WithArguments("T", "4").WithLocation(16, 32)
                );
        }

        [Fact]
        public void ITuple_06()
        {
            // - should match when input type extends ITuple and has inapplicable Deconstruct (type parameter)
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        M(new C());
    }
    public static void M<T>(T t) where T: C
    {
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
    public void Deconstruct() {}
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITuple_12()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        M(new C());
    }
    public static void M<T>(T t) where T: C
    {
        Console.WriteLine(t is (3, 4)); // false via ITuple
        Console.WriteLine(t is (3, 4, 5)); // true via ITuple
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
    public int Deconstruct() => 0;
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITuple_12b()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        M(new C());
    }
    public static void M<T>(T t) where T: C
    {
        Console.WriteLine(t is ());
    }
    public int Deconstruct() => 0; // this is applicable, so prevents ITuple, but it has the wrong return type
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (13,32): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'T', with 0 out parameters and a void return type.
                //         Console.WriteLine(t is ());
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "()").WithArguments("T", "0").WithLocation(13, 32)
                );
        }

        [Fact]
        public void ITuple_07()
        {
            // - should match when input type extends ITuple and has inapplicable Deconstruct (inherited)
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class B
{
    public void Deconstruct() {}
}
public class C : B, ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        M(new C());
    }
    public static void M<T>(T t) where T: C
    {
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITuple_08()
        {
            // - should match when input type extends ITuple and has an inapplicable Deconstruct (static)
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class B
{
    public static void Deconstruct() {}
}
public class C : B, ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        M(new C());
    }
    public static void M<T>(T t) where T: C
    {
        Console.WriteLine(t is (3, 4)); // false
        Console.WriteLine(t is (3, 4, 5)); // TRUE
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"False
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITuple_09()
        {
            // - should match when input type extends ITuple and has an extension Deconstruct
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        var t = new C();
        Console.WriteLine(t is (7, 8)); // true (Extensions.Deconstruct)
        Console.WriteLine(t is (3, 4, 5)); // true via ITuple
        Console.WriteLine(t is (3, 0, 5)); // false
        Console.WriteLine(t is (3, 4, 5, 6)); // false
    }
}
static class Extensions
{
    public static void Deconstruct(this C c, out int X, out int Y) => (X, Y) = (7, 8);
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"True
True
False
False";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITuple_09b()
        {
            // - An extension Deconstruct hides ITuple
            var source =
@"using System;
using System.Runtime.CompilerServices;
public class C : ITuple
{
    int ITuple.Length => 4;
    object ITuple.this[int i] => i + 3;
    public static void Main()
    {
        var t = new C();
        Console.WriteLine(t is (7, 8)); // true (Extensions.Deconstruct)
        Console.WriteLine(t is (3, 4, 5)); // false (ITuple hidden by extension method)
        Console.WriteLine(t is (1, 2, 3)); // true via extension Deconstruct
        Console.WriteLine(t is (3, 4, 5, 6)); // true (via ITuple)
    }
}
static class Extensions
{
    public static void Deconstruct(this C c, out int X, out int Y) => (X, Y) = (7, 8);
    public static void Deconstruct(this ITuple c, out int X, out int Y, out int Z) => (X, Y, Z) = (1, 2, 3);
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"True
False
True
True";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ITupleLacksLength()
        {
            // - should give an error when ITuple is missing required member (Length)
            var source =
@"using System;
namespace System.Runtime.CompilerServices
{
    public interface ITuple
    {
        // int Length { get; }
        object this[int index] { get; }
    }
}
public class C : System.Runtime.CompilerServices.ITuple
{
    // int System.Runtime.CompilerServices.ITuple.Length => 3;
    object System.Runtime.CompilerServices.ITuple.this[int i] => i + 3;
    public static void Main()
    {
        object t = new C();
        Console.WriteLine(t is (3, 4, 5));
    }
}
";
            // Use a version of the platform APIs that lack ITuple
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (17,32): error CS1061: 'object' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //         Console.WriteLine(t is (3, 4, 5));
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(3, 4, 5)").WithArguments("object", "Deconstruct").WithLocation(17, 32),
                // (17,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'object', with 3 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4, 5));
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4, 5)").WithArguments("object", "3").WithLocation(17, 32)
                );
        }

        [Fact]
        public void ITupleLacksIndexer()
        {
            // - should give an error when ITuple is missing required member (indexer)
            var source =
@"using System;
namespace System.Runtime.CompilerServices
{
    public interface ITuple
    {
        int Length { get; }
        // object this[int index] { get; }
    }
}
public class C : System.Runtime.CompilerServices.ITuple
{
    int System.Runtime.CompilerServices.ITuple.Length => 3;
    // object System.Runtime.CompilerServices.ITuple.this[int i] => i + 3;
    public static void Main()
    {
        object t = new C();
        Console.WriteLine(t is (3, 4, 5));
    }
}
";
            // Use a version of the platform APIs that lack ITuple
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (17,32): error CS1061: 'object' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //         Console.WriteLine(t is (3, 4, 5));
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(3, 4, 5)").WithArguments("object", "Deconstruct").WithLocation(17, 32),
                // (17,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'object', with 3 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4, 5));
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4, 5)").WithArguments("object", "3").WithLocation(17, 32)
                );
        }

        [Fact]
        public void ObsoleteITuple()
        {
            var source =
@"using System;
namespace System.Runtime.CompilerServices
{
    [Obsolete(""WarningOnly"")]
    public interface ITuple
    {
        int Length { get; }
        object this[int index] { get; }
    }
}
public class C : System.Runtime.CompilerServices.ITuple
{
    int System.Runtime.CompilerServices.ITuple.Length => 3;
    object System.Runtime.CompilerServices.ITuple.this[int i] => i + 3;
    public static void Main()
    {
        object t = new C();
        Console.WriteLine(t is (3, 4, 5));
    }
}
";
            // Use a version of the platform APIs that lack ITuple
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe);

            compilation.VerifyDiagnostics(
                // (11,18): warning CS0618: 'ITuple' is obsolete: 'WarningOnly'
                // public class C : System.Runtime.CompilerServices.ITuple
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "System.Runtime.CompilerServices.ITuple").WithArguments("System.Runtime.CompilerServices.ITuple", "WarningOnly").WithLocation(11, 18)
                );
            var expectedOutput = @"True";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ArgumentNamesInITuplePositional()
        {
            var source =
@"public class Program
{
    public static void Main()
    {
        object t = null;
        var r = t is (X: 3, Y: 4, Z: 5);
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,23): error CS8422: Element names are not permitted when pattern-matching via 'System.Runtime.CompilerServices.ITuple'.
                //         var r = t is (X: 3, Y: 4, Z: 5);
                Diagnostic(ErrorCode.ERR_ArgumentNameInITuplePattern, "X:").WithLocation(6, 23),
                // (6,29): error CS8422: Element names are not permitted when pattern-matching via 'System.Runtime.CompilerServices.ITuple'.
                //         var r = t is (X: 3, Y: 4, Z: 5);
                Diagnostic(ErrorCode.ERR_ArgumentNameInITuplePattern, "Y:").WithLocation(6, 29),
                // (6,35): error CS8422: Element names are not permitted when pattern-matching via 'System.Runtime.CompilerServices.ITuple'.
                //         var r = t is (X: 3, Y: 4, Z: 5);
                Diagnostic(ErrorCode.ERR_ArgumentNameInITuplePattern, "Z:").WithLocation(6, 35)
                );
        }

        [Fact]
        public void SymbolInfoForPositionalSubpattern()
        {
            var source =
@"using C2 = System.ValueTuple<int, int>;
public class Program
{
    public static void Main()
    {
        C1 c1 = null;
        if (c1 is (1, 2)) {}       // [0]
        if (c1 is (1, 2) Z1) {}    // [1]
        if (c1 is (1, 2) {}) {}    // [2]
        if (c1 is C1(1, 2) {}) {}  // [3]

        (int X, int Y) c2 = (1, 2);
        if (c2 is (1, 2)) {}       // [4]
        if (c2 is (1, 2) Z2) {}    // [5]
        if (c2 is (1, 2) {}) {}    // [6]
        if (c2 is C2(1, 2) {}) {}  // [7]
    }
}
class C1
{
    public void Deconstruct(out int X, out int Y) => X = Y = 0;
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var dpcss = tree.GetRoot().DescendantNodes().OfType<PositionalPatternClauseSyntax>().ToArray();
            for (int i = 0; i < dpcss.Length; i++)
            {
                var dpcs = dpcss[i];
                var symbolInfo = model.GetSymbolInfo(dpcs);
                if (i <= 3)
                {
                    Assert.Equal("void C1.Deconstruct(out System.Int32 X, out System.Int32 Y)", symbolInfo.Symbol.ToTestDisplayString());
                }
                else
                {
                    Assert.Null(symbolInfo.Symbol);
                }

                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
                Assert.Empty(symbolInfo.CandidateSymbols);
            }
        }

        [Fact]
        [WorkItem(30906, "https://github.com/dotnet/roslyn/issues/30906")]
        public void NullableTupleWithTuplePattern_01()
        {
            var source = @"using System;
class C
{
    static (int, int)? Get(int i)
    {
        switch (i)
        {
            case 1:
                return (1, 2);
            case 2:
                return (3, 4);
            default:
                return null;
        }
    }

    static void Main()
    {
        for (int i = 0; i < 6; i++)
        {
            if (Get(i) is var (x, y))
                Console.Write($""{i} {x} {y}; "");
        }
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"1 1 2; 2 3 4; ";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        [WorkItem(30906, "https://github.com/dotnet/roslyn/issues/30906")]
        public void NullableTupleWithTuplePattern_01b()
        {
            var source = @"using System;
class C
{
    static ((int, int)?, int) Get(int i)
    {
        switch (i)
        {
            case 1:
                return ((1, 2), 1);
            case 2:
                return ((3, 4), 1);
            default:
                return (null, 1);
        }
    }

    static void Main()
    {
        for (int i = 0; i < 6; i++)
        {
            if (Get(i) is var ((x, y), z))
                Console.Write($""{i} {x} {y}; "");
        }
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"1 1 2; 2 3 4; ";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        [WorkItem(30906, "https://github.com/dotnet/roslyn/issues/30906")]
        public void NullableTupleWithTuplePattern_02()
        {
            var source = @"using System;
class C
{
    static object Get(int i)
    {
        switch (i)
        {
            case 0:
                return ('a', 'b');
            case 1:
                return (1, 2);
            case 2:
                return (3, 4);
            case 3:
                return new object();
            default:
                return null;
        }
    }

    static void Main()
    {
        for (int i = 0; i < 6; i++)
        {
            if (Get(i) is var (x, y))
                Console.Write($""{i} {x} {y}; "");
        }
    }
}

// Provide a ValueTuple that implements ITuple
namespace System
{
    using ITuple = System.Runtime.CompilerServices.ITuple;
    public struct ValueTuple<T1, T2>: ITuple
    {
        public T1 Item1;
        public T2 Item2;
        public ValueTuple(T1 item1, T2 item2) => (Item1, Item2) = (item1, item2);
        int ITuple.Length => 2;
        object ITuple.this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return Item1;
                    case 1: return Item2;
                    default: throw new System.ArgumentException(""index"");
                }
            }
        }
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"0 a b; 1 1 2; 2 3 4; ";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        [WorkItem(30906, "https://github.com/dotnet/roslyn/issues/30906")]
        public void NullableTupleWithTuplePattern_02b()
        {
            var source = @"using System;
class C
{
    static object Get(int i)
    {
        switch (i)
        {
            case 0:
                return (('a', 'b'), 1);
            case 1:
                return ((1, 2), 1);
            case 2:
                return ((3, 4), 1);
            case 3:
                return new object();
            default:
                return null;
        }
    }

    static void Main()
    {
        for (int i = 0; i < 6; i++)
        {
            if (Get(i) is var ((x, y), z))
                Console.Write($""{i} {x} {y}; "");
        }
    }
}

// Provide a ValueTuple that implements ITuple
namespace System
{
    using ITuple = System.Runtime.CompilerServices.ITuple;
    public struct ValueTuple<T1, T2>: ITuple
    {
        public T1 Item1;
        public T2 Item2;
        public ValueTuple(T1 item1, T2 item2) => (Item1, Item2) = (item1, item2);
        int ITuple.Length => 2;
        object ITuple.this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return Item1;
                    case 1: return Item2;
                    default: throw new System.ArgumentException(""index"");
                }
            }
        }
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"0 a b; 1 1 2; 2 3 4; ";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        [WorkItem(30906, "https://github.com/dotnet/roslyn/issues/30906")]
        public void NullableTupleWithTuplePattern_03()
        {
            var source = @"using System;
class C
{
    static object Get(int i)
    {
        switch (i)
        {
            case 0:
                return ('a', 'b');
            case 1:
                return (1, 2);
            case 2:
                return (3, 4);
            case 3:
                return new object();
            default:
                return null;
        }
    }

    static void Main()
    {
        for (int i = 0; i < 6; i++)
        {
            if (Get(i) is var (x, y))
                Console.WriteLine($""{i} {x} {y}"");
        }
    }
}

// Provide a ValueTuple that DOES NOT implements ITuple or have a Deconstruct method
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;
        public ValueTuple(T1 item1, T2 item2) => (Item1, Item2) = (item1, item2);
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        [WorkItem(30906, "https://github.com/dotnet/roslyn/issues/30906")]
        public void NullableTupleWithTuplePattern_04()
        {
            var source = @"using System;
struct C
{
    static C? Get(int i)
    {
        switch (i)
        {
            case 1:
                return new C(1, 2);
            case 2:
                return new C(3, 4);
            default:
                return null;
        }
    }

    static void Main()
    {
        for (int i = 0; i < 6; i++)
        {
            if (Get(i) is var (x, y))
                Console.Write($""{i} {x} {y}; "");
        }
    }

    public int Item1;
    public int Item2;
    public C(int item1, int item2) => (Item1, Item2) = (item1, item2);
    public void Deconstruct(out int Item1, out int Item2) => (Item1, Item2) = (this.Item1, this.Item2);
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"1 1 2; 2 3 4; ";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void DiscardVsConstantInCase_01()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        const int _ = 3;
        for (int i = 0; i < 6; i++)
        {
            switch (i)
            {
                case _:
                    Console.Write(i);
                    break;
            }
        }
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (11,22): warning CS8512: The name '_' refers to the constant, not the discard pattern. Use 'var _' to discard the value, or '@_' to refer to a constant by that name.
                //                 case _:
                Diagnostic(ErrorCode.WRN_CaseConstantNamedUnderscore, "_").WithLocation(11, 22)
                );
            CompileAndVerify(compilation, expectedOutput: "3");
        }

        [Fact]
        public void DiscardVsConstantInCase_02()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        const int _ = 3;
        for (int i = 0; i < 6; i++)
        {
            switch (i)
            {
                case _ when true:
                    Console.Write(i);
                    break;
            }
        }
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (11,22): warning CS8512: The name '_' refers to the constant, not the discard pattern. Use 'var _' to discard the value, or '@_' to refer to a constant by that name.
                //                 case _ when true:
                Diagnostic(ErrorCode.WRN_CaseConstantNamedUnderscore, "_").WithLocation(11, 22)
                );
            CompileAndVerify(compilation, expectedOutput: "3");
        }

        [Fact]
        public void DiscardVsConstantInCase_03()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        const int _ = 3;
        for (int i = 0; i < 6; i++)
        {
            switch (i)
            {
                case var _:
                    Console.Write(i);
                    break;
            }
        }
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,19): warning CS0219: The variable '_' is assigned but its value is never used
                //         const int _ = 3;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "_").WithArguments("_").WithLocation(6, 19)
                );
            CompileAndVerify(compilation, expectedOutput: "012345");
        }

        [Fact]
        public void DiscardVsConstantInCase_04()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        const int _ = 3;
        for (int i = 0; i < 6; i++)
        {
            switch (i)
            {
                case var _ when true:
                    Console.Write(i);
                    break;
            }
        }
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,19): warning CS0219: The variable '_' is assigned but its value is never used
                //         const int _ = 3;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "_").WithArguments("_").WithLocation(6, 19)
                );
            CompileAndVerify(compilation, expectedOutput: "012345");
        }

        [Fact]
        public void DiscardVsConstantInCase_05()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        const int _ = 3;
        for (int i = 0; i < 6; i++)
        {
            switch (i)
            {
                case @_:
                    Console.Write(i);
                    break;
            }
        }
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "3");
        }

        [Fact]
        public void DiscardVsConstantInCase_06()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        const int _ = 3;
        for (int i = 0; i < 6; i++)
        {
            switch (i)
            {
                case @_ when true:
                    Console.Write(i);
                    break;
            }
        }
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "3");
        }

        [Fact]
        public void DiscardVsTypeInCase_01()
        {
            var source = @"
class Program
{
    static void Main()
    {
        object o = new _();
        switch (o)
        {
            case _ x: break;
        }
    }
}
class _
{
}";
            var compilation = CreatePatternCompilation(source);
            // Diagnostics are not ideal here.  On the other hand, this is not likely to be a frequent occurrence except in test code
            // so any effort at improving the diagnostics would not likely be well spent.
            compilation.VerifyDiagnostics(
                // (9,20): error CS1003: Syntax error, ':' expected
                //             case _ x: break;
                Diagnostic(ErrorCode.ERR_SyntaxError, "x").WithArguments(":").WithLocation(9, 20),
                // (9,20): warning CS0164: This label has not been referenced
                //             case _ x: break;
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "x").WithLocation(9, 20)
                );
        }

        [Fact]
        public void DiscardVsTypeInCase_02()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        object o = new _();
        foreach (var e in new[] { null, o, null })
        {
            switch (e)
            {
                case @_ x: Console.WriteLine(""3""); break;
            }
        }
    }
}
class _
{
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "3");
        }

        [Fact]
        public void DiscardVsTypeInIs_01()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        object o = new _();
        foreach (var e in new[] { null, o, null })
        {
            Console.Write(e is _);
        }
    }
}
class _
{
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (9,32): warning CS8513: The name '_' refers to the type '_', not the discard pattern. Use '@_' for the type, or 'var _' to discard.
                //             Console.Write(e is _);
                Diagnostic(ErrorCode.WRN_IsTypeNamedUnderscore, "_").WithArguments("_").WithLocation(9, 32)
                );
            CompileAndVerify(compilation, expectedOutput: "FalseTrueFalse");
        }

        [Fact]
        public void DiscardVsTypeInIs_02()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        object o = new _();
        foreach (var e in new[] { null, o, null })
        {
            Console.Write(e is _ x);
        }
    }
}
class _
{
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (9,32): warning CS8513: The name '_' refers to the type '_', not the discard pattern. Use '@_' for the type, or 'var _' to discard.
                //             Console.Write(e is _ x);
                Diagnostic(ErrorCode.WRN_IsTypeNamedUnderscore, "_").WithArguments("_").WithLocation(9, 32),
                // (9,34): error CS1003: Syntax error, ',' expected
                //             Console.Write(e is _ x);
                Diagnostic(ErrorCode.ERR_SyntaxError, "x").WithArguments(",").WithLocation(9, 34),
                // (9,34): error CS0103: The name 'x' does not exist in the current context
                //             Console.Write(e is _ x);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(9, 34)
                );
        }

        [Fact]
        public void DiscardVsTypeInIs_03()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        object o = new _();
        foreach (var e in new[] { null, o, null })
        {
            Console.Write(e is var _);
        }
    }
}
class _
{
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "TrueTrueTrue");
        }

        [Fact]
        public void DiscardVsTypeInIs_04()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        object o = new _();
        foreach (var e in new[] { null, o, null })
        {
            if (e is @_)
            {
                Console.Write(""3"");
            }
        }
    }
}
class _
{
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "3");
        }

        [Fact]
        public void DiscardVsDeclarationInNested_01()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        const int _ = 3;
        (object, object) o = (4, 4);
        foreach (var e in new[] { ((object, object)?)null, o, null })
        {
            if (e is (_, _))
            {
                Console.Write(""5"");
            }
        }
    }
}
class _
{
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,19): warning CS0219: The variable '_' is assigned but its value is never used
                //         const int _ = 3;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "_").WithArguments("_").WithLocation(6, 19)
                );
            CompileAndVerify(compilation, expectedOutput: "5");
        }

        [Fact]
        public void DiscardVsDeclarationInNested_02()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        const int _ = 3;
        (object, object) o = (4, 4);
        foreach (var e in new[] { ((object, object)?)null, o, null })
        {
            if (e is (_ x, _))
            {
                Console.Write(""5"");
            }
        }
    }
}
class _
{
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,19): warning CS0219: The variable '_' is assigned but its value is never used
                //         const int _ = 3;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "_").WithArguments("_").WithLocation(6, 19),
                // (10,22): error CS8502: Matching the tuple type '(object, object)' requires '2' subpatterns, but '3' subpatterns are present.
                //             if (e is (_ x, _))
                Diagnostic(ErrorCode.ERR_WrongNumberOfSubpatterns, "(_ x, _)").WithArguments("(object, object)", "2", "3").WithLocation(10, 22),
                // (10,25): error CS1003: Syntax error, ',' expected
                //             if (e is (_ x, _))
                Diagnostic(ErrorCode.ERR_SyntaxError, "x").WithArguments(",").WithLocation(10, 25),
                // (10,25): error CS0103: The name 'x' does not exist in the current context
                //             if (e is (_ x, _))
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(10, 25)
                );
        }

        [Fact]
        public void DiscardVsDeclarationInNested_03()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        const int _ = 3;
        (object, object) o = (new _(), 4);
        foreach (var e in new[] { ((object, object)?)null, o, (_, 8) })
        {
            if (e is (@_ x, var y))
            {
                Console.Write(y);
            }
        }
    }
}
class _
{
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "4");
        }

        [Fact]
        public void DiscardVsDeclarationInNested_04()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        const int _ = 3;
        (object, object) o = (new _(), 4);
        foreach (var e in new[] { ((object, object)?)null, o, (_, 8) })
        {
            if (e is (@_, var y))
            {
                Console.Write(y);
            }
        }
    }
}
class _
{
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "8");
        }

        [Fact]
        public void IgnoreNullInExhaustiveness_01()
        {
            var source =
@"class Program
{
    static void Main() {}
    static int M1(bool? b1, bool? b2)
    {
        return (b1, b2) switch {
            (false, false) => 1,
            (false, true) => 2,
            // (true, false) => 3,
            (true, true) => 4,
            };
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,25): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(true, false)' is not covered.
                //         return (b1, b2) switch {
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(true, false)").WithLocation(6, 25)
                );
        }

        [Fact]
        public void IgnoreNullInExhaustiveness_02()
        {
            var source =
@"class Program
{
    static void Main() {}
    static int M1(bool? b1, bool? b2)
    {
        return (b1, b2) switch {
            (false, false) => 1,
            (false, true) => 2,
            (true, false) => 3,
            (true, true) => 4,
            };
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
        }

        [Fact]
        public void IgnoreNullInExhaustiveness_03()
        {
            var source =
@"class Program
{
    static void Main() {}
    static int M1(bool? b1, bool? b2)
    {
        (bool? b1, bool? b2)? cond = (b1, b2);
        return cond switch {
            (false, false) => 1,
            (false, true) => 2,
            (true, false) => 3,
            (true, true) => 4,
            (null, true) => 5
            };
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
        }

        [Fact]
        public void IgnoreNullInExhaustiveness_04()
        {
            var source =
@"class Program
{
    static void Main() {}
    static int M1(bool? b1, bool? b2)
    {
        (bool? b1, bool? b2)? cond = (b1, b2);
        return cond switch {
            (false, false) => 1,
            (false, true) => 2,
            (true, false) => 3,
            (true, true) => 4,
            _ => 5,
            (null, true) => 6,
            };
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (13,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             (null, true) => 6,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "(null, true)").WithLocation(13, 13)
                );
        }

        [Fact]
        public void DeconstructVsITuple_01()
        {
            // From LDM 2018-11-05:
            // 1. If the type is a tuple type (any arity >= 0; see below), then use the tuple semantics
            // 2. If "binding" a Deconstruct invocation would find one or more applicable methods, use Deconstruct.
            // 3. If the type satisfies the ITuple deconstruct constraints, use ITuple semantics
            // Here we test the relative priority of steps 2 and 3.
            // - Found one applicable Deconstruct method (even though the type implements ITuple): use it
            var source = @"using System;
using System.Runtime.CompilerServices;
class Program
{
    static void Main()
    {
        IA a = new A();
        if (a is (var x, var y))  // tuple pattern containing var patterns
            Console.Write($""{x} {y}"");
    }
}
interface IA : ITuple
{
    void Deconstruct(out int X, out int Y);
}
class A: IA, ITuple
{
    void IA.Deconstruct(out int X, out int Y) => (X, Y) = (3, 4);
    int ITuple.Length => throw null;
    object ITuple.this[int i] => throw null;
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "3 4");
        }

        [Fact]
        public void DeconstructVsITuple_01b()
        {
            // From LDM 2018-11-05:
            // 1. If the type is a tuple type (any arity >= 0; see below), then use the tuple semantics
            // 2. If "binding" a Deconstruct invocation would find one or more applicable methods, use Deconstruct.
            // 3. If the type satisfies the ITuple deconstruct constraints, use ITuple semantics
            // Here we test the relative priority of steps 2 and 3.
            // - Found one applicable Deconstruct method (even though the type implements ITuple): use it
            var source = @"using System;
using System.Runtime.CompilerServices;
class Program
{
    static void Main()
    {
        IA a = new A();
        if (a is var (x, y))  // var pattern containing tuple designator
            Console.Write($""{x} {y}"");
    }
}
interface IA : ITuple
{
    void Deconstruct(out int X, out int Y);
}
class A: IA, ITuple
{
    void IA.Deconstruct(out int X, out int Y) => (X, Y) = (3, 4);
    int ITuple.Length => throw null;
    object ITuple.this[int i] => throw null;
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "3 4");
        }

        [Fact]
        public void DeconstructVsITuple_02()
        {
            // From LDM 2018-11-05:
            // 1. If the type is a tuple type (any arity >= 0; see below), then use the tuple semantics
            // 2. If "binding" a Deconstruct invocation would find one or more applicable methods, use Deconstruct.
            // 3. If the type satisfies the ITuple deconstruct constraints, use ITuple semantics
            // Here we test the relative priority of steps 2 and 3.
            // - Found more than one applicable Deconstruct method (even though the type implements ITuple): error

            // var pattern with tuple designator
            var source = @"using System;
using System.Runtime.CompilerServices;
class Program
{
    static void Main()
    {
        IA a = new A();
        if (a is var (x, y)) Console.Write($""{x} {y}"");
    }
}
interface I1
{
    void Deconstruct(out int X, out int Y);
}
interface I2
{
    void Deconstruct(out int X, out int Y);
}
interface IA: I1, I2 {}
class A: IA, I1, I2, ITuple
{
    void I1.Deconstruct(out int X, out int Y) => (X, Y) = (3, 4);
    void I2.Deconstruct(out int X, out int Y) => (X, Y) = (7, 8);
    int ITuple.Length => 2;
    object ITuple.this[int i] => i + 5;
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (8,22): error CS0121: The call is ambiguous between the following methods or properties: 'I1.Deconstruct(out int, out int)' and 'I2.Deconstruct(out int, out int)'
                //         if (a is var (x, y)) Console.Write($"{x} {y}");
                Diagnostic(ErrorCode.ERR_AmbigCall, "(x, y)").WithArguments("I1.Deconstruct(out int, out int)", "I2.Deconstruct(out int, out int)").WithLocation(8, 22)
                );
        }

        [Fact]
        public void DeconstructVsITuple_02b()
        {
            // From LDM 2018-11-05:
            // 1. If the type is a tuple type (any arity >= 0; see below), then use the tuple semantics
            // 2. If "binding" a Deconstruct invocation would find one or more applicable methods, use Deconstruct.
            // 3. If the type satisfies the ITuple deconstruct constraints, use ITuple semantics
            // Here we test the relative priority of steps 2 and 3.
            // - Found more than one applicable Deconstruct method (even though the type implements ITuple): error

            // tuple pattern with var subpatterns
            var source = @"using System;
using System.Runtime.CompilerServices;
class Program
{
    static void Main()
    {
        IA a = new A();
        if (a is (var x, var y)) Console.Write($""{x} {y}"");
    }
}
interface I1
{
    void Deconstruct(out int X, out int Y);
}
interface I2
{
    void Deconstruct(out int X, out int Y);
}
interface IA: I1, I2 {}
class A: IA, I1, I2, ITuple
{
    void I1.Deconstruct(out int X, out int Y) => (X, Y) = (3, 4);
    void I2.Deconstruct(out int X, out int Y) => (X, Y) = (7, 8);
    int ITuple.Length => 2;
    object ITuple.this[int i] => i + 5;
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (8,18): error CS0121: The call is ambiguous between the following methods or properties: 'I1.Deconstruct(out int, out int)' and 'I2.Deconstruct(out int, out int)'
                //         if (a is (var x, var y)) Console.Write($"{x} {y}");
                Diagnostic(ErrorCode.ERR_AmbigCall, "(var x, var y)").WithArguments("I1.Deconstruct(out int, out int)", "I2.Deconstruct(out int, out int)").WithLocation(8, 18)
                );
        }

        [Fact]
        public void UnmatchedInput_01()
        {
            var source =
@"using System;
public class C
{
    static void Main()
    {
        var t = (1, 2);
        try
        {
            _ = t switch { (3, 4) => 1 };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.GetType().Name);
        }
    }
}
";
            var compilation = CreatePatternCompilation(source);

            var ctorObject = compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_SwitchExpressionException__ctorObject);
            Assert.Null(ctorObject);

            var ctor = compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_SwitchExpressionException__ctor);
            Assert.Null(ctor);

            var invalidOperationExceptionCtor = compilation.GetWellKnownTypeMember(WellKnownMember.System_InvalidOperationException__ctor);
            Assert.NotNull(invalidOperationExceptionCtor);

            compilation.VerifyDiagnostics(
                // (9,19): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(0, _)' is not covered.
                //             _ = t switch { (3, 4) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(0, _)").WithLocation(9, 19)
                );
            CompileAndVerify(compilation, expectedOutput: "InvalidOperationException").VerifyIL("C.Main", @"
{
  // Code size       83 (0x53)
  .maxstack  3
  .locals init (System.ValueTuple<int, int> V_0, //t
                int V_1,
                int V_2,
                System.Exception V_3) //ex
  // sequence point: {
  IL_0000:  nop
  // sequence point: var t = (1, 2);
  IL_0001:  ldloca.s   V_0
  IL_0003:  ldc.i4.1
  IL_0004:  ldc.i4.2
  IL_0005:  call       ""System.ValueTuple<int, int>..ctor(int, int)""
  .try
  {
    // sequence point: {
    IL_000a:  nop
    // sequence point: _ = t switch { (3, 4) => 1 };
    IL_000b:  ldc.i4.1
    IL_000c:  brtrue.s   IL_000f
    // sequence point: switch { (3, 4) => 1 }
    IL_000e:  nop
    // sequence point: <hidden>
    IL_000f:  ldloc.0
    IL_0010:  ldfld      ""int System.ValueTuple<int, int>.Item1""
    IL_0015:  stloc.1
    // sequence point: <hidden>
    IL_0016:  ldloc.1
    IL_0017:  ldc.i4.3
    IL_0018:  bne.un.s   IL_002b
    IL_001a:  ldloc.0
    IL_001b:  ldfld      ""int System.ValueTuple<int, int>.Item2""
    IL_0020:  stloc.2
    // sequence point: <hidden>
    IL_0021:  ldloc.2
    IL_0022:  ldc.i4.4
    IL_0023:  beq.s      IL_0027
    IL_0025:  br.s       IL_002b
    // sequence point: 1
    IL_0027:  ldc.i4.1
    IL_0028:  pop
    IL_0029:  br.s       IL_0035
    IL_002b:  ldc.i4.1
    IL_002c:  brtrue.s   IL_002f
    // sequence point: switch { (3, 4) => 1 }
    IL_002e:  nop
    // sequence point: <hidden>
    IL_002f:  call       ""void <PrivateImplementationDetails>.ThrowInvalidOperationException()""
    IL_0034:  nop
    // sequence point: <hidden>
    IL_0035:  ldc.i4.1
    IL_0036:  brtrue.s   IL_0039
    // sequence point: _ = t switch { (3, 4) => 1 };
    IL_0038:  nop
    // sequence point: }
    IL_0039:  nop
    IL_003a:  leave.s    IL_0052
  }
  catch System.Exception
  {
    // sequence point: catch (Exception ex)
    IL_003c:  stloc.3
    // sequence point: {
    IL_003d:  nop
    // sequence point: Console.WriteLine(ex.GetType().Name);
    IL_003e:  ldloc.3
    IL_003f:  callvirt   ""System.Type System.Exception.GetType()""
    IL_0044:  callvirt   ""string System.Reflection.MemberInfo.Name.get""
    IL_0049:  call       ""void System.Console.WriteLine(string)""
    IL_004e:  nop
    // sequence point: }
    IL_004f:  nop
    IL_0050:  leave.s    IL_0052
  }
  // sequence point: }
  IL_0052:  ret
}
", sequencePoints: "C.Main", source: source).VerifyIL("<PrivateImplementationDetails>.ThrowInvalidOperationException", @"
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  newobj     ""System.InvalidOperationException..ctor()""
  IL_0005:  throw
}
", sequencePoints: "<PrivateImplementationDetails>.ThrowInvalidOperationException", source: source);
        }

        [Fact]
        public void UnmatchedInput_02()
        {
            var source =
@"using System; using System.Runtime.CompilerServices;
public class C
{
    static void Main()
    {
        var t = (1, 2);
        try
        {
            _ = t switch { (3, 4) => 1 };
        }
        catch (SwitchExpressionException ex)
        {
            Console.WriteLine($""{ex.GetType().Name}({ex.UnmatchedValue})"");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.GetType().Name);
        }
    }
}
namespace System.Runtime.CompilerServices
{
    public class SwitchExpressionException : InvalidOperationException
    {
        public SwitchExpressionException() {}
        // public SwitchExpressionException(object unmatchedValue) => UnmatchedValue = unmatchedValue;
        public object UnmatchedValue { get; }
    }
}
";
            var compilation = CreatePatternCompilation(source);

            var ctorObject = compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_SwitchExpressionException__ctorObject);
            Assert.Null(ctorObject);

            var ctor = compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_SwitchExpressionException__ctor);
            Assert.NotNull(ctor);

            compilation.VerifyDiagnostics(
                // (9,19): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(0, _)' is not covered.
                //             _ = t switch { (3, 4) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(0, _)").WithLocation(9, 19)
                );
            compilation.VerifyEmitDiagnostics(
                // (9,19): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(0, _)' is not covered.
                //             _ = t switch { (3, 4) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(0, _)").WithLocation(9, 19)
                );
            CompileAndVerify(compilation, expectedOutput: "SwitchExpressionException()").VerifyIL("C.Main", @"
{
  // Code size      123 (0x7b)
  .maxstack  3
  .locals init (System.ValueTuple<int, int> V_0, //t
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.SwitchExpressionException V_3, //ex
                System.Exception V_4) //ex
  // sequence point: {
  IL_0000:  nop
  // sequence point: var t = (1, 2);
  IL_0001:  ldloca.s   V_0
  IL_0003:  ldc.i4.1
  IL_0004:  ldc.i4.2
  IL_0005:  call       ""System.ValueTuple<int, int>..ctor(int, int)""
  .try
  {
    // sequence point: {
    IL_000a:  nop
    // sequence point: _ = t switch { (3, 4) => 1 };
    IL_000b:  ldc.i4.1
    IL_000c:  brtrue.s   IL_000f
    // sequence point: switch { (3, 4) => 1 }
    IL_000e:  nop
    // sequence point: <hidden>
    IL_000f:  ldloc.0
    IL_0010:  ldfld      ""int System.ValueTuple<int, int>.Item1""
    IL_0015:  stloc.1
    // sequence point: <hidden>
    IL_0016:  ldloc.1
    IL_0017:  ldc.i4.3
    IL_0018:  bne.un.s   IL_002b
    IL_001a:  ldloc.0
    IL_001b:  ldfld      ""int System.ValueTuple<int, int>.Item2""
    IL_0020:  stloc.2
    // sequence point: <hidden>
    IL_0021:  ldloc.2
    IL_0022:  ldc.i4.4
    IL_0023:  beq.s      IL_0027
    IL_0025:  br.s       IL_002b
    // sequence point: 1
    IL_0027:  ldc.i4.1
    IL_0028:  pop
    IL_0029:  br.s       IL_0035
    IL_002b:  ldc.i4.1
    IL_002c:  brtrue.s   IL_002f
    // sequence point: switch { (3, 4) => 1 }
    IL_002e:  nop
    // sequence point: <hidden>
    IL_002f:  call       ""void <PrivateImplementationDetails>.ThrowSwitchExpressionExceptionParameterless()""
    IL_0034:  nop
    // sequence point: <hidden>
    IL_0035:  ldc.i4.1
    IL_0036:  brtrue.s   IL_0039
    // sequence point: _ = t switch { (3, 4) => 1 };
    IL_0038:  nop
    // sequence point: }
    IL_0039:  nop
    IL_003a:  leave.s    IL_007a
  }
  catch System.Runtime.CompilerServices.SwitchExpressionException
  {
    // sequence point: catch (SwitchExpressionException ex)
    IL_003c:  stloc.3
    // sequence point: {
    IL_003d:  nop
    // sequence point: Console.WriteLine($""{ex.GetType().Name}({ex.UnmatchedValue})"");
    IL_003e:  ldstr      ""{0}({1})""
    IL_0043:  ldloc.3
    IL_0044:  callvirt   ""System.Type System.Exception.GetType()""
    IL_0049:  callvirt   ""string System.Reflection.MemberInfo.Name.get""
    IL_004e:  ldloc.3
    IL_004f:  callvirt   ""object System.Runtime.CompilerServices.SwitchExpressionException.UnmatchedValue.get""
    IL_0054:  call       ""string string.Format(string, object, object)""
    IL_0059:  call       ""void System.Console.WriteLine(string)""
    IL_005e:  nop
    // sequence point: }
    IL_005f:  nop
    IL_0060:  leave.s    IL_007a
  }
  catch System.Exception
  {
    // sequence point: catch (Exception ex)
    IL_0062:  stloc.s    V_4
    // sequence point: {
    IL_0064:  nop
    // sequence point: Console.WriteLine(ex.GetType().Name);
    IL_0065:  ldloc.s    V_4
    IL_0067:  callvirt   ""System.Type System.Exception.GetType()""
    IL_006c:  callvirt   ""string System.Reflection.MemberInfo.Name.get""
    IL_0071:  call       ""void System.Console.WriteLine(string)""
    IL_0076:  nop
    // sequence point: }
    IL_0077:  nop
    IL_0078:  leave.s    IL_007a
  }
  // sequence point: }
  IL_007a:  ret
}
", sequencePoints: "C.Main", source: source).VerifyIL("<PrivateImplementationDetails>.ThrowSwitchExpressionExceptionParameterless", @"
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  newobj     ""System.Runtime.CompilerServices.SwitchExpressionException..ctor()""
  IL_0005:  throw
}
", sequencePoints: "<PrivateImplementationDetails>.ThrowSwitchExpressionExceptionParameterless", source: source);
        }

        [Fact]
        public void UnmatchedInput_03()
        {
            var source =
@"using System; using System.Runtime.CompilerServices;
public class C
{
    static void Main()
    {
        var t = (1, 2);
        try
        {
            _ = t switch { (3, 4) => 1 };
        }
        catch (SwitchExpressionException ex)
        {
            Console.WriteLine($""{ex.GetType().Name}({ex.UnmatchedValue})"");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.GetType().Name);
        }
    }
}
namespace System.Runtime.CompilerServices
{
    public class SwitchExpressionException : InvalidOperationException
    {
        public SwitchExpressionException() => throw null;
        public SwitchExpressionException(object unmatchedValue) => UnmatchedValue = unmatchedValue;
        public object UnmatchedValue { get; }
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (9,19): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(0, _)' is not covered.
                //             _ = t switch { (3, 4) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(0, _)").WithLocation(9, 19)
                );
            CompileAndVerify(compilation, expectedOutput: "SwitchExpressionException((1, 2))");
        }

        [Fact]
        public void UnmatchedInput_04()
        {
            var source =
@"using System; using System.Runtime.CompilerServices;
public class C
{
    static void Main()
    {
        try
        {
            _ = (1, 2) switch { (3, 4) => 1 };
        }
        catch (SwitchExpressionException ex)
        {
            Console.WriteLine($""{ex.GetType().Name}({ex.UnmatchedValue})"");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.GetType().Name);
        }
    }
}
namespace System.Runtime.CompilerServices
{
    public class SwitchExpressionException : InvalidOperationException
    {
        public SwitchExpressionException() => throw null;
        public SwitchExpressionException(object unmatchedValue) => UnmatchedValue = unmatchedValue;
        public object UnmatchedValue { get; }
    }
}
";
            var compilation = CreatePatternCompilation(source);
            var ctorObject = compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_SwitchExpressionException__ctorObject);
            Assert.NotNull(ctorObject);

            compilation.VerifyDiagnostics(
                // (8,24): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(0, _)' is not covered.
                //             _ = (1, 2) switch { (3, 4) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(0, _)").WithLocation(8, 24)
                );
            compilation.VerifyEmitDiagnostics(
                // (8,24): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(0, _)' is not covered.
                //             _ = (1, 2) switch { (3, 4) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(0, _)").WithLocation(8, 24)
                );
            CompileAndVerify(compilation, expectedOutput: "SwitchExpressionException((1, 2))").VerifyIL("C.Main", @"
{
  // Code size      114 (0x72)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.SwitchExpressionException V_2, //ex
                System.Exception V_3) //ex
  // sequence point: {
  IL_0000:  nop
  .try
  {
    // sequence point: {
    IL_0001:  nop
    // sequence point: _ = (1, 2) switch { (3, 4) => 1 };
    IL_0002:  ldc.i4.1
    IL_0003:  stloc.0
    IL_0004:  ldc.i4.2
    IL_0005:  stloc.1
    IL_0006:  ldc.i4.1
    IL_0007:  brtrue.s   IL_000a
    // sequence point: switch { (3, 4) => 1 }
    IL_0009:  nop
    // sequence point: <hidden>
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.3
    IL_000c:  bne.un.s   IL_0018
    IL_000e:  ldloc.1
    IL_000f:  ldc.i4.4
    IL_0010:  beq.s      IL_0014
    IL_0012:  br.s       IL_0018
    // sequence point: 1
    IL_0014:  ldc.i4.1
    IL_0015:  pop
    IL_0016:  br.s       IL_002e
    IL_0018:  ldc.i4.1
    IL_0019:  brtrue.s   IL_001c
    // sequence point: switch { (3, 4) => 1 }
    IL_001b:  nop
    // sequence point: <hidden>
    IL_001c:  ldloc.0
    IL_001d:  ldloc.1
    IL_001e:  newobj     ""System.ValueTuple<int, int>..ctor(int, int)""
    IL_0023:  box        ""System.ValueTuple<int, int>""
    IL_0028:  call       ""void <PrivateImplementationDetails>.ThrowSwitchExpressionException(object)""
    IL_002d:  nop
    // sequence point: <hidden>
    IL_002e:  ldc.i4.1
    IL_002f:  brtrue.s   IL_0032
    // sequence point: _ = (1, 2) switch { (3, 4) => 1 };
    IL_0031:  nop
    // sequence point: }
    IL_0032:  nop
    IL_0033:  leave.s    IL_0071
  }
  catch System.Runtime.CompilerServices.SwitchExpressionException
  {
    // sequence point: catch (SwitchExpressionException ex)
    IL_0035:  stloc.2
    // sequence point: {
    IL_0036:  nop
    // sequence point: Console.WriteLine($""{ex.GetType().Name}({ex.UnmatchedValue})"");
    IL_0037:  ldstr      ""{0}({1})""
    IL_003c:  ldloc.2
    IL_003d:  callvirt   ""System.Type System.Exception.GetType()""
    IL_0042:  callvirt   ""string System.Reflection.MemberInfo.Name.get""
    IL_0047:  ldloc.2
    IL_0048:  callvirt   ""object System.Runtime.CompilerServices.SwitchExpressionException.UnmatchedValue.get""
    IL_004d:  call       ""string string.Format(string, object, object)""
    IL_0052:  call       ""void System.Console.WriteLine(string)""
    IL_0057:  nop
    // sequence point: }
    IL_0058:  nop
    IL_0059:  leave.s    IL_0071
  }
  catch System.Exception
  {
    // sequence point: catch (Exception ex)
    IL_005b:  stloc.3
    // sequence point: {
    IL_005c:  nop
    // sequence point: Console.WriteLine(ex.GetType().Name);
    IL_005d:  ldloc.3
    IL_005e:  callvirt   ""System.Type System.Exception.GetType()""
    IL_0063:  callvirt   ""string System.Reflection.MemberInfo.Name.get""
    IL_0068:  call       ""void System.Console.WriteLine(string)""
    IL_006d:  nop
    // sequence point: }
    IL_006e:  nop
    IL_006f:  leave.s    IL_0071
  }
  // sequence point: }
  IL_0071:  ret
}
", sequencePoints: "C.Main", source: source).VerifyIL("<PrivateImplementationDetails>.ThrowSwitchExpressionException", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""System.Runtime.CompilerServices.SwitchExpressionException..ctor(object)""
  IL_0006:  throw
}
", sequencePoints: "<PrivateImplementationDetails>.ThrowSwitchExpressionException", source: source);
        }

        [Fact]
        public void UnmatchedInput_05()
        {
            var source =
@"using System; using System.Runtime.CompilerServices;
public class C
{
    static void Main()
    {
        try
        {
            R r = new R();
            _ = r switch { (3, 4) => 1 };
        }
        catch (SwitchExpressionException ex)
        {
            Console.WriteLine($""{ex.GetType().Name}({ex.UnmatchedValue})"");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.GetType().Name);
        }
    }
}
ref struct R
{
    public void Deconstruct(out int X, out int Y) => (X, Y) = (1, 2);
}
namespace System.Runtime.CompilerServices
{
    public class SwitchExpressionException : InvalidOperationException
    {
        public SwitchExpressionException() {}
        public SwitchExpressionException(object unmatchedValue) => throw null;
        public object UnmatchedValue { get; }
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (9,19): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(0, _)' is not covered.
                //             _ = r switch { (3, 4) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(0, _)").WithLocation(9, 19)
                );
            CompileAndVerify(compilation, expectedOutput: "SwitchExpressionException()");
        }

        [Fact]
        public void UnmatchedInput_08()
        {
            var source =
@"using System;
public class C
{
    static void Main()
    {
        var t = (1, 2);
        try
        {
            _ = t switch { (3, 4) => 1 };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.GetType().Name);
        }
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.MakeTypeMissing(WellKnownType.System_InvalidOperationException);

            var ctorObject = compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_SwitchExpressionException__ctorObject);
            Assert.Null(ctorObject);

            var ctor = compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_SwitchExpressionException__ctor);
            Assert.Null(ctor);

            var invalidOperationExceptionCtor = compilation.GetWellKnownTypeMember(WellKnownMember.System_InvalidOperationException__ctor);
            Assert.Null(invalidOperationExceptionCtor);

            compilation.VerifyDiagnostics(
                // (9,19): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(0, _)' is not covered.
                //             _ = t switch { (3, 4) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(0, _)").WithLocation(9, 19)
                );
            compilation.VerifyEmitDiagnostics(
                // (9,17): error CS0656: Missing compiler required member 'System.InvalidOperationException..ctor'
                //             _ = t switch { (3, 4) => 1 };
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "t switch { (3, 4) => 1 }").WithArguments("System.InvalidOperationException", ".ctor").WithLocation(9, 17),
                // (9,19): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(0, _)' is not covered.
                //             _ = t switch { (3, 4) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(0, _)").WithLocation(9, 19)
            );
        }

        [Fact]
        public void DeconstructVsITuple_03()
        {
            // From LDM 2018-11-05:
            // 1. If the type is a tuple type (any arity >= 0; see below), then use the tuple semantics
            // 2. If "binding" a Deconstruct invocation would find one or more applicable methods, use Deconstruct.
            // 3. If the type satisfies the ITuple deconstruct constraints, use ITuple semantics
            // Here we test the relative priority of steps 2 and 3.
            // - Found inapplicable Deconstruct method; use ITuple
            var source = @"using System;
using System.Runtime.CompilerServices;
class Program
{
    static void Main()
    {
        IA a = new A();
        if (a is (var x, var y)) Console.Write($""{x} {y}"");
    }
}
interface IA : ITuple
{
    void Deconstruct(out int X, out int Y, out int Z);
}
class A: IA, ITuple
{
    void IA.Deconstruct(out int X, out int Y, out int Z) => throw null;
    int ITuple.Length => 2;
    object ITuple.this[int i] => i + 5;
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "5 6");
        }

        [Fact]
        public void DeconstructVsITuple_03b()
        {
            // From LDM 2018-11-05:
            // 1. If the type is a tuple type (any arity >= 0; see below), then use the tuple semantics
            // 2. If "binding" a Deconstruct invocation would find one or more applicable methods, use Deconstruct.
            // 3. If the type satisfies the ITuple deconstruct constraints, use ITuple semantics
            // Here we test the relative priority of steps 2 and 3.
            // - Found inapplicable Deconstruct method; use ITuple
            var source = @"using System;
using System.Runtime.CompilerServices;
class Program
{
    static void Main()
    {
        IA a = new A();
        if (a is var (x, y)) Console.Write($""{x} {y}"");
    }
}
interface IA : ITuple
{
    void Deconstruct(out int X, out int Y, out int Z);
}
class A: IA, ITuple
{
    void IA.Deconstruct(out int X, out int Y, out int Z) => throw null;
    int ITuple.Length => 2;
    object ITuple.this[int i] => i + 5;
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "5 6");
        }

        [Fact]
        public void ShortTuplePattern_01()
        {
            // test 0-element tuple pattern via ITuple
            var source = @"using System;
using System.Runtime.CompilerServices;

class Program
{
    static void Main()
    {
#pragma warning disable CS0436
        var data = new object[] { null, new ValueTuple(), new C(), new object() };
        for (int i = 0; i < data.Length; i++)
        {
            var datum = data[i];
            if (datum is ()) Console.Write(i);
        }
    }
}

public class C : ITuple
{
    int ITuple.Length => 0;
    object ITuple.this[int i] => throw new NotImplementedException();
}
namespace System
{
    struct ValueTuple : ITuple
    {
        int ITuple.Length => 0;
        object ITuple.this[int i] => throw new NotImplementedException();
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "12");
        }

        [Fact]
        public void ShortTuplePattern_02()
        {
            // test 1-element tuple pattern via ITuple
            var source = @"using System;
using System.Runtime.CompilerServices;

class Program
{
    static void Main()
    {
#pragma warning disable CS0436
        var data = new object[] { null, new ValueTuple<char>('a'), new C(), new object() };
        for (int i = 0; i < data.Length; i++)
        {
            var datum = data[i];
            if (datum is (var x) _) Console.Write($""{i} {x} "");
        }
    }
}

public class C : ITuple
{
    int ITuple.Length => 1;
    object ITuple.this[int i] => 'b';
}
namespace System
{
    struct ValueTuple<TItem1> : ITuple
    {
        public TItem1 Item1;
        public ValueTuple(TItem1 item1) => this.Item1 = item1;
        int ITuple.Length => 1;
        object ITuple.this[int i] => this.Item1;
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "1 a 2 b");
        }

        [Fact]
        public void ShortTuplePattern_03()
        {
            // test 0-element tuple pattern via Deconstruct
            var source = @"using System;

class Program
{
    static void Main()
    {
        var data = new C[] { null, new C() };
        for (int i = 0; i < data.Length; i++)
        {
            var datum = data[i];
            if (datum is ()) Console.Write(i);
        }
    }
}

public class C
{
    public void Deconstruct() {}
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "1");
        }

        [Fact]
        public void ShortTuplePattern_03b()
        {
            // test 0-element tuple pattern via extension Deconstruct
            var source = @"using System;

class Program
{
    static void Main()
    {
        var data = new C[] { null, new C() };
        for (int i = 0; i < data.Length; i++)
        {
            var datum = data[i];
            if (datum is ()) Console.Write(i);
        }
    }
}

public class C
{
}
public static class Extension
{
    public static void Deconstruct(this C self) {}
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "1");
        }

        [Fact]
        public void ShortTuplePattern_04()
        {
            // test 1-element tuple pattern via Deconstruct
            var source = @"using System;

class Program
{
    static void Main()
    {
        var data = new C[] { null, new C() };
        for (int i = 0; i < data.Length; i++)
        {
            var datum = data[i];
            if (datum is (var x) _) Console.Write($""{i} {x} "");
        }
    }
}

public class C
{
    public void Deconstruct(out char a) => a = 'a';
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "1 a");
        }

        [Fact]
        public void ShortTuplePattern_04b()
        {
            // test 1-element tuple pattern via extension Deconstruct
            var source = @"using System;

class Program
{
    static void Main()
    {
        var data = new C[] { null, new C() };
        for (int i = 0; i < data.Length; i++)
        {
            var datum = data[i];
            if (datum is (var x) _) Console.Write($""{i} {x} "");
        }
    }
}

public class C
{
}
public static class Extension
{
    public static void Deconstruct(this C self, out char a) => a = 'a';
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "1 a");
        }

        [Fact]
        public void ShortTuplePattern_05()
        {
            // test 0-element tuple pattern via System.ValueTuple
            var source = @"using System;

class Program
{
    static void Main()
    {
#pragma warning disable CS0436
        var data = new ValueTuple[] { new ValueTuple() };
        for (int i = 0; i < data.Length; i++)
        {
            var datum = data[i];
            if (datum is ()) Console.Write(i);
        }
    }
}

namespace System
{
    struct ValueTuple
    {
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "0");
        }

        [Fact]
        public void ShortTuplePattern_06()
        {
            // test 1-element tuple pattern via System.ValueTuple
            var source = @"using System;

class Program
{
    static void Main()
    {
#pragma warning disable CS0436
        var data = new ValueTuple<char>[] { new ValueTuple<char>('a') };
        for (int i = 0; i < data.Length; i++)
        {
            var datum = data[i];
            if (datum is (var x) _) Console.Write($""{i} {x} "");
        }
    }
}

namespace System
{
    struct ValueTuple<TItem1>
    {
        public TItem1 Item1;
        public ValueTuple(TItem1 item1) => this.Item1 = item1;
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "0 a");
        }

        [Fact]
        public void ShortTuplePattern_06b()
        {
            // test 1-element tuple pattern via System.ValueTuple
            var source = @"using System;

class Program
{
    static void Main()
    {
#pragma warning disable CS0436
        var data = new ValueTuple<char>[] { new ValueTuple<char>('a') };
        for (int i = 0; i < data.Length; i++)
        {
            var datum = data[i];
            if (datum is var (x)) Console.Write($""{i} {x} "");
        }
    }
}

namespace System
{
    struct ValueTuple<TItem1>
    {
        public TItem1 Item1;
        public ValueTuple(TItem1 item1) => this.Item1 = item1;
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: "0 a");
        }

        [Fact]
        public void WrongNumberOfDesignatorsForTuple()
        {
            var source =
@"class Program
{
    static void Main()
    {
        _ = (1, 2) is var (_, _, _);
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (5,27): error CS8502: Matching the tuple type '(int, int)' requires '2' subpatterns, but '3' subpatterns are present.
                //         _ = (1, 2) is var (_, _, _);
                Diagnostic(ErrorCode.ERR_WrongNumberOfSubpatterns, "(_, _, _)").WithArguments("(int, int)", "2", "3").WithLocation(5, 27)
                );
        }

        [Fact]
        public void PropertyNameMissing()
        {
            var source =
@"class Program
{
    static void Main()
    {
        _ = (1, 2) is { 1, 2 };
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (5,25): error CS8503: A property subpattern requires a reference to the property or field to be matched, e.g. '{ Name: 1 }'
                //         _ = (1, 2) is { 1, 2 };
                Diagnostic(ErrorCode.ERR_PropertyPatternNameMissing, "1").WithArguments("1").WithLocation(5, 25)
                );
        }

        [Fact]
        public void IndexedProperty_01()
        {
            var source1 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface I
    Property P(x As Object, Optional y As Object = Nothing) As Object
End Interface";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"class C
{
    static void Main(I i)
    {
        _ = i is { P: 1 };
    }
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (5,20): error CS0857: Indexed property 'I.P' must have all arguments optional
                //         _ = i is { P: 1 };
                Diagnostic(ErrorCode.ERR_IndexedPropertyMustHaveAllOptionalParams, "P").WithArguments("I.P").WithLocation(5, 20)
                );
        }

        [Fact, WorkItem(31209, "https://github.com/dotnet/roslyn/issues/31209")]
        public void IndexedProperty_02()
        {
            var source1 =
@"Imports System
Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)> 
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")> 
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface I
    Property P(Optional x As Object = Nothing, Optional y As Object = Nothing) As Object
End Interface";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);
            var source2 =
@"class C
{
    static void Main(I i)
    {
        _ = i is { P: 1 };
    }
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            // https://github.com/dotnet/roslyn/issues/31209 asks what the desired behavior is for this case.
            // This test demonstrates that we at least behave rationally and do not crash.
            compilation2.VerifyDiagnostics(
                // (5,20): error CS0154: The property or indexer 'P' cannot be used in this context because it lacks the get accessor
                //         _ = i is { P: 1 };
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "P").WithArguments("P").WithLocation(5, 20)
                );
        }

        [Fact]
        public void TestMissingIntegralTypes()
        {
            var source =
@"public class C
{
    public static void Main()
    {
        M(1U);
        M(2UL);
        M(1);
        M(2);
        M(3);
    }
    static void M(object o)
    {
        System.Console.Write(o switch {
            (uint)1 => 1,
            (ulong)2 => 2,
            1 => 3,
            2 => 4,
            _ => 5 });
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "12345");
        }

        [Fact]
        public void TestConvertInputTupleToInterface()
        {
            var source =
@"#pragma warning disable CS0436 // The type 'ValueTuple<T1, T2>' conflicts with the imported type
using System.Runtime.CompilerServices;
using System;
public class C
{
    public static void Main()
    {
        Console.Write((1, 2) switch
        {
            ITuple t => 3
        });
    }
}
namespace System
{
    struct ValueTuple<T1, T2> : ITuple
    {
        int ITuple.Length => 2;
        object ITuple.this[int i] => i switch { 0 => (object)Item1, 1 => (object)Item2, _ => throw null };
        public T1 Item1;
        public T2 Item2;
        public ValueTuple(T1 item1, T2 item2) => (Item1, Item2) = (item1, item2);
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "3");
        }

        [Fact]
        public void TestUnusedTupleInput()
        {
            var source =
@"using System;
public class C
{
    public static void Main()
    {
        Console.Write((M(1), M(2)) switch { _ => 3 });
    }
    static int M(int x) { Console.Write(x); return x; }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "123");
        }

        [Fact]
        public void TestNestedTupleOpt()
        {
            var source =
@"using System;
public class C
{
    public static void Main()
    {
        var x = (1, 20);
        Console.Write((x, 300) switch  { ((1, int x2), int y) => x2+y });
    }
}
";
            var compilation = CreatePatternCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (7,32): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '((0, _), _)' is not covered.
                //         Console.Write((x, 300) switch  { ((1, int x2), int y) => x2+y });
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("((0, _), _)").WithLocation(7, 32)
                );
            CompileAndVerify(compilation, expectedOutput: "320");
        }

        [Fact]
        public void TestGotoCaseTypeMismatch()
        {
            var source =
@"public class C
{
    public static void Main()
    {
        int i = 1;
        switch (i)
        {
            case 1:
                if (i == 1)
                    goto case string.Empty;
                break;
        }
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (10,21): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //                     goto case string.Empty;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "goto case string.Empty;").WithArguments("string", "int").WithLocation(10, 21)
                );
        }

        [Fact]
        public void TestGotoCaseNotConstant()
        {
            var source =
@"public class C
{
    public static void Main()
    {
        int i = 1;
        switch (i)
        {
            case 1:
                if (i == 1)
                    goto case string.Empty.Length;
                break;
        }
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (10,21): error CS0150: A constant value is expected
                //                     goto case string.Empty.Length;
                Diagnostic(ErrorCode.ERR_ConstantExpected, "goto case string.Empty.Length;").WithLocation(10, 21)
                );
        }

        [Fact]
        public void TestExhaustiveWithNullTest()
        {
            var source =
@"public class C
{
    public static void Main()
    {
        object o = null;
        _ = o switch { null => 1 };
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,15): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'not null' is not covered.
                //         _ = o switch { null => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("not null").WithLocation(6, 15)
                );
        }

        [Fact, WorkItem(31167, "https://github.com/dotnet/roslyn/issues/31167")]
        public void NonExhaustiveBoolSwitchExpression()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        new Program().Start();
    }
    void Start()
    {
        Console.Write(M(true));
        try
        {
            Console.Write(M(false));
        }
        catch (Exception)
        {
            Console.Write("" throw"");
        }
    }
    public int M(bool b) 
    {
        return b switch
        {
           true => 1
        }; 
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (22,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'false' is not covered.
                //         return b switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("false").WithLocation(22, 18)
                );
            CompileAndVerify(compilation, expectedOutput: "1 throw");
        }

        [Fact]
        public void PointerAsInput_01()
        {
            var source =
@"public class C
{
    public unsafe static void Main()
    {
        int x = 0;
        M(1, null);
        M(2, &x);
    }
    static unsafe void M(int i, int* p)
    {
        if (p is var x)
            System.Console.Write(i);
    }
}
";
            var compilation = CreatePatternCompilation(source, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            compilation.VerifyDiagnostics(
                );
            var expectedOutput = @"12";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput, verify: Verification.Skipped);
        }

        // https://github.com/dotnet/roslyn/issues/35032: Handle switch expressions correctly
        [Fact]
        public void PointerAsInput_02()
        {
            var source =
@"public class C
{
    public unsafe static void Main()
    {
        int x = 0;
        M(1, null);
        M(2, &x);
    }
    static unsafe void M(int i, int* p)
    {
        if (p switch { _ => true })
            System.Console.Write(i);
    }
}
";
            var compilation = CreatePatternCompilation(source, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            compilation.VerifyDiagnostics(
                );
            var expectedOutput = @"12";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput, verify: Verification.Skipped);
        }

        [Fact]
        public void PointerAsInput_03()
        {
            var source =
@"public class C
{
    public unsafe static void Main()
    {
        int x = 0;
        M(1, null);
        M(2, &x);
    }
    static unsafe void M(int i, int* p)
    {
        if (p is null)
            System.Console.Write(i);
    }
}
";
            var compilation = CreatePatternCompilation(source, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            compilation.VerifyDiagnostics(
                );
            var expectedOutput = @"1";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput, verify: Verification.Skipped);
        }

        [Fact]
        public void PointerAsInput_04()
        {
            var source =
@"public class C
{
    static unsafe void M(int* p)
    {
        if (p is {}) { }
        if (p is 1) { }
        if (p is var (x, y)) { }
    }
}
";
            var compilation = CreatePatternCompilation(source, options: TestOptions.DebugDll.WithAllowUnsafe(true));
            compilation.VerifyDiagnostics(
                // 0.cs(5,18): error CS8521: Pattern-matching is not permitted for pointer types.
                //         if (p is {}) { }
                Diagnostic(ErrorCode.ERR_PointerTypeInPatternMatching, "{}").WithLocation(5, 18),
                // 0.cs(6,18): error CS0266: Cannot implicitly convert type 'int' to 'int*'. An explicit conversion exists (are you missing a cast?)
                //         if (p is 1) { }
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1").WithArguments("int", "int*").WithLocation(6, 18),
                // 0.cs(6,18): error CS9133: A constant value of type 'int*' is expected
                //         if (p is 1) { }
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, "1").WithArguments("int*").WithLocation(6, 18),
                // 0.cs(7,18): error CS8521: Pattern-matching is not permitted for pointer types.
                //         if (p is var (x, y)) { }
                Diagnostic(ErrorCode.ERR_PointerTypeInPatternMatching, "var (x, y)").WithLocation(7, 18)
                );
        }

        [Fact, WorkItem(48591, "https://github.com/dotnet/roslyn/issues/48591")]
        public void PointerAsInput_05()
        {
            var source =
@"public class C
{
    unsafe static void F2<T>(nint i) where T : unmanaged
    {
        T* p = (T*)i;
        _ = p == null;
        _ = p != null;
        _ = p is null;
        _ = p is not null;
        _ = p switch { not null => true, null => false };
        _ = p switch { { } => true, null => false }; // 1
    }
}
";
            var compilation = CreatePatternCompilation(source, options: TestOptions.DebugDll.WithAllowUnsafe(true));
            compilation.VerifyDiagnostics(
                // (11,24): error CS8521: Pattern-matching is not permitted for pointer types.
                //         _ = p switch { { } => true, null => false }; // 1
                Diagnostic(ErrorCode.ERR_PointerTypeInPatternMatching, "{ }").WithLocation(11, 24)
                );
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/70048")]
        public void Pointer_Pattern_Comparison([CombinatorialValues("<", ">", "<=", ">=")] string op, bool not)
        {
            var source = $$"""
                class C
                {
                    unsafe void M(void* p)
                    {
                        if (p is {{(not ? "not " : "    ") + op}} null) { }
                    }
                }
                """;
            CreateCompilation(source, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
                // (5,22): error CS8781: Relational patterns may not be used for a value of type 'void*'.
                //         if (p is     < null) { }
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForRelationalPattern, $"{op} null").WithArguments("void*").WithLocation(5, 22));
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/70048")]
        public void Pointer_Pattern_Equality([CombinatorialValues("==", "!=")] string op, bool not)
        {
            var source = $$"""
                class C
                {
                    unsafe void M(void* p)
                    {
                        if (p is {{(not ? "not " : "    ") + op}} null) { }
                    }
                }
                """;
            CreateCompilation(source, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
                // (5,22): error CS1525: Invalid expression term '=='
                //         if (p is     == null) { }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, op).WithArguments(op).WithLocation(5, 22));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70048")]
        public void Pointer_Pattern_Complex()
        {
            var source = """
                class C
                {
                    unsafe void M(void* p)
                    {
                        if (p is < null or > null) { }
                        if (p is < null or null) { }
                    }
                }
                """;
            CreateCompilation(source, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
                // (5,18): error CS8781: Relational patterns may not be used for a value of type 'void*'.
                //         if (p is < null or > null) { }
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForRelationalPattern, "< null").WithArguments("void*").WithLocation(5, 18),
                // (5,28): error CS8781: Relational patterns may not be used for a value of type 'void*'.
                //         if (p is < null or > null) { }
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForRelationalPattern, "> null").WithArguments("void*").WithLocation(5, 28),
                // (6,18): error CS8781: Relational patterns may not be used for a value of type 'void*'.
                //         if (p is < null or null) { }
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForRelationalPattern, "< null").WithArguments("void*").WithLocation(6, 18));
        }

        [Fact]
        public void UnmatchedInput_06()
        {
            var source =
@"using System; using System.Runtime.CompilerServices;
public class C
{
    static void Main()
    {
        Console.WriteLine(M(1, 2));
        try
        {
            Console.WriteLine(M(1, 3));
        }
        catch (SwitchExpressionException ex)
        {
            Console.WriteLine($""{ex.GetType().Name}({ex.UnmatchedValue})"");
        }
    }
    public static int M(int x, int y) {
        return (x, y) switch { (1, 2) => 3 };
    }
}
namespace System.Runtime.CompilerServices
{
    public class SwitchExpressionException : InvalidOperationException
    {
        public SwitchExpressionException() => throw null;
        public SwitchExpressionException(object unmatchedValue) => UnmatchedValue = unmatchedValue;
        public object UnmatchedValue { get; }
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (17,23): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(0, _)' is not covered.
                //         return (x, y) switch { (1, 2) => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(0, _)").WithLocation(17, 23)
                );
            CompileAndVerify(compilation, expectedOutput: @"3
SwitchExpressionException((1, 3))");
        }

        [Fact]
        public void RecordOrderOfEvaluation()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        var data = new A(new A(1, new A(2, 3)), new A(4, new A(5, 6)));
        Console.WriteLine(data switch
            {
            A(A(1, A(2, 1)), _) => 3,
            A(A(1, 2), _) { X: 1 } => 2,
            A(1, _) => 1,
            A(A(1, A(2, 3) { X: 1 }), A(4, A(5, 6))) => 5,
            A(_, A(4, A(5, 1))) => 4,
            A(A(1, A(2, 3)), A(4, A(5, 6) { Y: 5 })) => 6,
            A(A(1, A(2, 3) { Y: 5 }), A(4, A(5, 6))) => 7,
            A(A(1, A(2, 3)), A(4, A(5, 6))) => 8,
            _ => 9
            });
    }
}
class A
{
    public A(object x, object y)
    {
        (_x, _y) = (x, y);
    }
    public void Deconstruct(out object x, out object y)
    {
        Console.WriteLine($""{this}.Deconstruct"");
        (x, y) = (_x, _y);
    }
    private object _x;
    public object X
    {
        get
        {
            Console.WriteLine($""{this}.X"");
            return _x;
        }
    }
    private object _y;
    public object Y
    {
        get
        {
            Console.WriteLine($""{this}.Y"");
            return _y;
        }
    }
    public override string ToString() => $""A({_x}, {_y})"";
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput:
@"A(A(1, A(2, 3)), A(4, A(5, 6))).Deconstruct
A(1, A(2, 3)).Deconstruct
A(2, 3).Deconstruct
A(2, 3).X
A(4, A(5, 6)).Deconstruct
A(5, 6).Deconstruct
A(5, 6).Y
A(2, 3).Y
8");
        }

        [Fact]
        public void MissingValueTuple()
        {
            var source = @"
class Program
{
    static void Main()
    {
    }
    int M(int x, int y)
    {
        return (x, y) switch { (1, 2) => 1, _ => 2 };
    }
}
";
            var compilation = CreateCompilationWithMscorlib40(source);
            compilation.VerifyDiagnostics(
                // (9,16): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported
                //         return (x, y) switch { (1, 2) => 1, _ => 2 };
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(x, y)").WithArguments("System.ValueTuple`2").WithLocation(9, 16)
                );
        }

        [Fact]
        public void UnmatchedInput_07()
        {
            var source =
@"using System; using System.Runtime.CompilerServices;
public class C
{
    static void Main()
    {
        Console.WriteLine(M(1, 2));
        try
        {
            Console.WriteLine(M(1, 3));
        }
        catch (SwitchExpressionException ex)
        {
            Console.WriteLine($""{ex.GetType().Name}({ex.UnmatchedValue})"");
        }
    }
    public static int M(int x, int y, int a = 3, int b = 4, int c = 5, int d = 6, int e = 7, int f = 8, int g = 9) {
        return (x, y, a, b, c, d, e, f, g) switch { (1, 2, _, _, _, _, _, _, _) => 3 };
    }
}
namespace System.Runtime.CompilerServices
{
    public class SwitchExpressionException : InvalidOperationException
    {
        public SwitchExpressionException() => throw null;
        public SwitchExpressionException(object unmatchedValue) => UnmatchedValue = unmatchedValue;
        public object UnmatchedValue { get; }
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (17,44): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(0, _, _, _, _, _, _, _, _)' is not covered.
                //         return (x, y, a, b, c, d, e, f, g) switch { (1, 2, _, _, _, _, _, _, _) => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(0, _, _, _, _, _, _, _, _)").WithLocation(17, 44)
                );
            CompileAndVerify(compilation, expectedOutput: @"3
SwitchExpressionException((1, 3, 3, 4, 5, 6, 7, 8, 9))");
        }

        [Fact]
        public void NullableArrayDeclarationPattern_Good_01()
        {
            var source =
@"#nullable enable
public class A
{
    static void M(object o, bool c)
    {
        if (o is A[]? c && c : c) { }    // ok 3 (for compat)
        if (o is A[][]? c : c) { }       // ok 4 (for compat)
    }
}
";
            var compilation = CreatePatternCompilation(source, options: TestOptions.DebugDll);
            compilation.VerifyDiagnostics(
                );
        }

        [Fact]
        public void NullableArrayDeclarationPattern_Good_02()
        {
            var source =
@"#nullable enable
public class A
{
    static void M(object o, bool c)
    {
        if (o is A[]?[,] b3) { }
        if (o is A[,]?[] b4 && c) { }
        if (o is A[,]?[]?[] b5 && c) { }
    }
}
";
            var compilation = CreatePatternCompilation(source, options: TestOptions.DebugDll);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void NullableArrayDeclarationPattern_Bad_02()
        {
            var source =
@"#nullable enable
public class A
{
    public static bool b1, b2, b5, b6, b7, b8;
    static void M(object o, bool c)
    {
        if (o is A?) { }              // error 1 (can't test for is nullable reference type)
        if (o is A? b1) { }           // error 2 (can't test for is nullable reference type)
        if (o is A? b2 && c) { }      // error 3 (missing :)
        if (o is A[]? b5) { }         // error 4 (can't test for is nullable reference type)
        if (o is A[]? b6 && c) { }    // error 5 (missing :)
        if (o is A[][]? b7) { }       // error 6 (can't test for is nullable reference type)
        if (o is A[][]? b8 && c) { }  // error 7 (missing :)
        if (o is A? && c) { }         // error 8 (can't test for is nullable reference type)
        _ = o is A[][]?;              // error 9 (can't test for is nullable reference type)
        _ = o as A[][]?;              // error 10 (can't 'as' nullable reference type)
    }
}
";
            var compilation = CreatePatternCompilation(source, options: TestOptions.DebugDll);
            compilation.VerifyDiagnostics(
                // (7,18): error CS8650: It is not legal to use nullable reference type 'A?' in an is-type expression; use the underlying type 'A' instead.
                //         if (o is A?) { }              // error 1 (can't test for is nullable reference type)
                Diagnostic(ErrorCode.ERR_IsNullableType, "A?").WithArguments("A").WithLocation(7, 18),
                // 0.cs(8,18): error CS8116: It is not legal to use nullable reference type 'A?' in an is-type expression; use the underlying type 'A' instead.
                //         if (o is A? b1) { }           // error 2 (can't test for is nullable reference type)
                Diagnostic(ErrorCode.ERR_PatternNullableType, "A?").WithArguments("A").WithLocation(8, 18),
                // 0.cs(9,28): error CS1003: Syntax error, ':' expected
                //         if (o is A? b2 && c) { }      // error 3 (missing :)
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(":").WithLocation(9, 28),
                // (9,28): error CS1525: Invalid expression term ')'
                //         if (o is A? b2 && c) { }      // error 3 (missing :)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(9, 28),
                // (10,18): error CS8116: It is not legal to use nullable reference type 'A[]?' in an is-type expression; use the underlying type 'A[]' instead.
                //         if (o is A[]? b5) { }         // error 4 (can't test for is nullable reference type)
                Diagnostic(ErrorCode.ERR_PatternNullableType, "A[]?").WithArguments("A[]").WithLocation(10, 18),
                // (11,30): error CS1003: Syntax error, ':' expected
                //         if (o is A[]? b6 && c) { }    // error 5 (missing :)
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(":").WithLocation(11, 30),
                // (11,30): error CS1525: Invalid expression term ')'
                //         if (o is A[]? b6 && c) { }    // error 5 (missing :)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(11, 30),
                // (12,18): error CS8116: It is not legal to use nullable reference type 'A[][]?' in an is-type expression; use the underlying type 'A[][]' instead.
                //         if (o is A[][]? b7) { }       // error 6 (can't test for is nullable reference type)
                Diagnostic(ErrorCode.ERR_PatternNullableType, "A[][]?").WithArguments("A[][]").WithLocation(12, 18),
                // (13,32): error CS1003: Syntax error, ':' expected
                //         if (o is A[][]? b8 && c) { }  // error 7 (missing :)
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(":").WithLocation(13, 32),
                // (13,32): error CS1525: Invalid expression term ')'
                //         if (o is A[][]? b8 && c) { }  // error 7 (missing :)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(13, 32),
                // (14,18): error CS8650: It is not legal to use nullable reference type 'A?' in an is-type expression; use the underlying type 'A' instead.
                //         if (o is A? && c) { }         // error 8 (can't test for is nullable reference type)
                Diagnostic(ErrorCode.ERR_IsNullableType, "A?").WithArguments("A").WithLocation(14, 18),
                // (15,18): error CS8650: It is not legal to use nullable reference type 'A[][]?' in an is-type expression; use the underlying type 'A[][]' instead.
                //         _ = o is A[][]?;              // error 9 (can't test for is nullable reference type)
                Diagnostic(ErrorCode.ERR_IsNullableType, "A[][]?").WithArguments("A[][]").WithLocation(15, 18),
                // (16,18): error CS8651: It is not legal to use nullable reference type 'A[][]?' in an as expression; use the underlying type 'A[][]' instead.
                //         _ = o as A[][]?;              // error 10 (can't 'as' nullable reference type)
                Diagnostic(ErrorCode.ERR_AsNullableType, "A[][]?").WithArguments("A[][]").WithLocation(16, 18)
                );
        }

        [Fact]
        public void IsPatternOnPointerTypeIn7_3()
        {
            var source = @"
unsafe class C
{
    static void Main()
    {
        int* ptr = null;
        _ = ptr is var v;
    }
}";

            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (7,20): error CS8521: Pattern-matching is not permitted for pointer types.
                //         _ = ptr is var v;
                Diagnostic(ErrorCode.ERR_PointerTypeInPatternMatching, "var v").WithLocation(7, 20)
            );
        }

        [Fact, WorkItem(43960, "https://github.com/dotnet/roslyn/issues/43960")]
        public void NamespaceQualifiedEnumConstantInSwitchCase()
        {
            var source =
@"enum E
{
    A, B, C
}

class Class1
{
    void M(E e)
    {
        switch (e)
        {
            case global::E.A: break;
            case global::E.B: break;
            case global::E.C: break;
        }
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular7, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                );
            CreatePatternCompilation(source, options: TestOptions.ReleaseDll).VerifyDiagnostics();
        }

        [Fact, WorkItem(44019, "https://github.com/dotnet/roslyn/issues/44019")]
        public void NamespaceQualifiedEnumConstantInIsPattern_01()
        {
            var source =
@"enum E
{
    A, B, C
}

class Class1
{
    void M(object e)
    {
        if (e is global::E.A) { }
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular7, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                );
            CreatePatternCompilation(source, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                );
        }

        [Fact, WorkItem(44019, "https://github.com/dotnet/roslyn/issues/44019")]
        public void NamespaceQualifiedTypeInIsType_02()
        {
            var source =
@"enum E
{
    A, B, C
}

class Class1
{
    void M(object e)
    {
        if (e is global::E) { }
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular7, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                );
            CreatePatternCompilation(source, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                );
        }

        [Fact, WorkItem(44019, "https://github.com/dotnet/roslyn/issues/44019")]
        public void NamespaceQualifiedTypeInIsType_03()
        {
            var source =
@"namespace E
{
    public class A { }
}

class Class1
{
    void M(object e)
    {
        if (e is global::E.A) { }
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular7, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                );
            CreatePatternCompilation(source, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                );
        }

        [Fact, WorkItem(44019, "https://github.com/dotnet/roslyn/issues/44019")]
        public void NamespaceQualifiedTypeInIsType_04()
        {
            var source =
@"namespace E
{
    public class A<T> { }
}

class Class1
{
    void M<T>(object e)
    {
        if (e is global::E.A<int>) { }
        if (e is global::E.A<object>) { }
        if (e is global::E.A<T>) { }
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular7, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                );
            CreatePatternCompilation(source, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                );
        }

        [Fact, WorkItem(44019, "https://github.com/dotnet/roslyn/issues/44019")]
        public void NamespaceQualifiedTypeInIsType_05()
        {
            var source =
@"namespace E
{
    public class A<T>
    {
        public class B { }
    }
}

class Class1
{
    void M<T>(object e)
    {
        if (e is global::E.A<int>.B) { }
        if (e is global::E.A<object>.B) { }
        if (e is global::E.A<T>.B) { }
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular7, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                );
            CreatePatternCompilation(source, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                );
        }

#if DEBUG
        [Fact, WorkItem(53868, "https://github.com/dotnet/roslyn/issues/53868")]
        public void DecisionDag_Dump_SwitchStatement_01()
        {
            var source = @"
using System;

class C
{
    void M(object obj)
    {
        switch (obj)
        {
            case ""a"":
                Console.Write(""b"");
                break;
            case string { Length: 1 } s:
                Console.Write(s);
                break;
            case int and < 42:
                Console.Write(43);
                break;
            case int i when (i % 2) == 0:
                obj = i + 1;
                break;
            default:
                Console.Write(false);
                break;
        }
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var @switch = tree.GetRoot().DescendantNodes().OfType<SwitchStatementSyntax>().Single();
            var model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            var binder = model.GetEnclosingBinder(@switch.SpanStart);
            var boundSwitch = (BoundSwitchStatement)binder.BindStatement(@switch, BindingDiagnosticBag.Discarded);
            AssertEx.Equal(
@"[0]: t0 is string ? [1] : [8]
[1]: t1 = (string)t0; [2]
[2]: t1 == ""a"" ? [3] : [4]
[3]: leaf `case ""a"":`
[4]: t2 = t1.Length; [5]
[5]: t2 == 1 ? [6] : [13]
[6]: when <true> ? [7] : <unreachable>
[7]: leaf `case string { Length: 1 } s:`
[8]: t0 is int ? [9] : [13]
[9]: t3 = (int)t0; [10]
[10]: t3 < 42 ? [11] : [12]
[11]: leaf `case int and < 42:`
[12]: when ((i % 2) == 0) ? [14] : [13]
[13]: leaf `default`
[14]: leaf `case int i when (i % 2) == 0:`
", boundSwitch.ReachabilityDecisionDag.Dump());
        }

        [Fact, WorkItem(53868, "https://github.com/dotnet/roslyn/issues/53868")]
        public void DecisionDag_Dump_SwitchStatement_02()
        {
            var source = @"
using System;

class C
{
    void Deconstruct(out int i1, out string i2, out int? i3)
    {
        i1 = 1;
        i2 = ""a"";
        i3 = null;
    }

    void M(C c)
    {
        switch (c)
        {
            case null:
                Console.Write(0);
                break;
            case (42, ""b"", 43):
                Console.Write(1);
                break;
            case (< 10, { Length: 0 }, { }):
                Console.Write(2);
                break;
            case (< 10, object): // 1, 2
                Console.Write(3);
                break;
        }
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (26,18): error CS7036: There is no argument given that corresponds to the required parameter 'i3' of 'C.Deconstruct(out int, out string, out int?)'
                //             case (< 10, object): // 1, 2
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "(< 10, object)").WithArguments("i3", "C.Deconstruct(out int, out string, out int?)").WithLocation(26, 18),
                // (26,18): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'C', with 2 out parameters and a void return type.
                //             case (< 10, object): // 1, 2
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(< 10, object)").WithArguments("C", "2").WithLocation(26, 18)
            );

            var tree = comp.SyntaxTrees.Single();
            var @switch = tree.GetRoot().DescendantNodes().OfType<SwitchStatementSyntax>().Single();
            var model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            var binder = model.GetEnclosingBinder(@switch.SpanStart);
            var boundSwitch = (BoundSwitchStatement)binder.BindStatement(@switch, BindingDiagnosticBag.Discarded);
            AssertEx.Equal(
@"[0]: t0 == null ? [1] : [2]
[1]: leaf `case null:`
[2]: (Item1, Item2, Item3) t1 = t0; [3]
[3]: t1.Item1 == 42 ? [4] : [9]
[4]: t1.Item2 == ""b"" ? [5] : [15]
[5]: t1.Item3 != null ? [6] : [15]
[6]: t2 = (int)t1.Item3; [7]
[7]: t2 == 43 ? [8] : [15]
[8]: leaf `case (42, ""b"", 43):`
[9]: t1.Item1 < 10 ? [10] : [15]
[10]: t1.Item2 != null ? [11] : [15]
[11]: t3 = t1.Item2.Length; [12]
[12]: t3 == 0 ? [13] : [15]
[13]: t1.Item3 != null ? [14] : [15]
[14]: leaf `case (< 10, { Length: 0 }, { }):`
[15]: t0 is <error type> ? [16] : [17]
[16]: leaf `case (< 10, object):`
[17]: leaf <break> `switch (c)
        {
            case null:
                Console.Write(0);
                break;
            case (42, ""b"", 43):
                Console.Write(1);
                break;
            case (< 10, { Length: 0 }, { }):
                Console.Write(2);
                break;
            case (< 10, object): // 1, 2
                Console.Write(3);
                break;
        }`
", boundSwitch.ReachabilityDecisionDag.Dump());
        }

        [Fact, WorkItem(53868, "https://github.com/dotnet/roslyn/issues/53868")]
        public void DecisionDag_Dump_SwitchStatement_03()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;

class C : ITuple
{
    int ITuple.Length => 3;
    object ITuple.this[int i] => i + 3;

    void M(C c)
    {
        switch (c)
        {
            case (3, 4, 4):
                Console.Write(0);
                break;
            case (3, 4, 5):
                Console.Write(1);
                break;
            case (int x, 4, 5):
                Console.Write(2);
                break;
        }
    }
}
";
            var comp = CreatePatternCompilation(source, TestOptions.DebugDll);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var @switch = tree.GetRoot().DescendantNodes().OfType<SwitchStatementSyntax>().Single();
            var model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            var binder = model.GetEnclosingBinder(@switch.SpanStart);
            var boundSwitch = (BoundSwitchStatement)binder.BindStatement(@switch, BindingDiagnosticBag.Discarded);
            AssertEx.Equal(
@"[0]: t0 is System.Runtime.CompilerServices.ITuple ? [1] : [28]
[1]: t1 = t0.Length; [2]
[2]: t1 == 3 ? [3] : [28]
[3]: t2 = t0[0]; [4]
[4]: t2 is int ? [5] : [28]
[5]: t3 = (int)t2; [6]
[6]: t3 == 3 ? [7] : [18]
[7]: t4 = t0[1]; [8]
[8]: t4 is int ? [9] : [28]
[9]: t5 = (int)t4; [10]
[10]: t5 == 4 ? [11] : [28]
[11]: t6 = t0[2]; [12]
[12]: t6 is int ? [13] : [28]
[13]: t7 = (int)t6; [14]
[14]: t7 == 4 ? [15] : [16]
[15]: leaf `case (3, 4, 4):`
[16]: t7 == 5 ? [17] : [28]
[17]: leaf `case (3, 4, 5):`
[18]: t4 = t0[1]; [19]
[19]: t4 is int ? [20] : [28]
[20]: t5 = (int)t4; [21]
[21]: t5 == 4 ? [22] : [28]
[22]: t6 = t0[2]; [23]
[23]: t6 is int ? [24] : [28]
[24]: t7 = (int)t6; [25]
[25]: t7 == 5 ? [26] : [28]
[26]: when <true> ? [27] : <unreachable>
[27]: leaf `case (int x, 4, 5):`
[28]: leaf <break> `switch (c)
        {
            case (3, 4, 4):
                Console.Write(0);
                break;
            case (3, 4, 5):
                Console.Write(1);
                break;
            case (int x, 4, 5):
                Console.Write(2);
                break;
        }`
", boundSwitch.ReachabilityDecisionDag.Dump());
        }

        [Fact, WorkItem(53868, "https://github.com/dotnet/roslyn/issues/53868")]
        public void DecisionDag_Dump_IsPattern()
        {
            var source = @"
using System;

class C
{
    void M(object obj)
    {
        if (obj
            is < 5
                or string { Length: 1 }
                or bool)
        {
            Console.Write(1);
        }
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var @is = tree.GetRoot().DescendantNodes().OfType<IsPatternExpressionSyntax>().Single();
            var model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            var binder = model.GetEnclosingBinder(@is.SpanStart);
            var boundIsPattern = (BoundIsPatternExpression)binder.BindExpression(@is, BindingDiagnosticBag.Discarded);
            AssertEx.Equal(
@"[0]: t0 is int ? [1] : [3]
[1]: t1 = (int)t0; [2]
[2]: t1 < 5 ? [8] : [9]
[3]: t0 is string ? [4] : [7]
[4]: t2 = (string)t0; [5]
[5]: t3 = t2.Length; [6]
[6]: t3 == 1 ? [8] : [9]
[7]: t0 is bool ? [8] : [9]
[8]: leaf <isPatternSuccess> `< 5
                or string { Length: 1 }
                or bool`
[9]: leaf <isPatternFailure> `< 5
                or string { Length: 1 }
                or bool`
", boundIsPattern.ReachabilityDecisionDag.Dump());
        }

        [Fact, WorkItem(53868, "https://github.com/dotnet/roslyn/issues/53868")]
        public void DecisionDag_Dump_SwitchExpression()
        {
            var source = @"
class C
{
    void M(object obj)
    {
        var x = obj switch
        {
            < 5 => 1,
            string { Length: 1 } => 2,
            bool => 3,
            _ => 4
        };
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var @switch = tree.GetRoot().DescendantNodes().OfType<SwitchExpressionSyntax>().Single();
            var model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            var binder = model.GetEnclosingBinder(@switch.SpanStart);
            var boundSwitch = (BoundSwitchExpression)binder.BindExpression(@switch, BindingDiagnosticBag.Discarded);
            AssertEx.Equal(
@"[0]: t0 is int ? [1] : [4]
[1]: t1 = (int)t0; [2]
[2]: t1 < 5 ? [3] : [11]
[3]: leaf <arm> `< 5 => 1`
[4]: t0 is string ? [5] : [9]
[5]: t2 = (string)t0; [6]
[6]: t3 = t2.Length; [7]
[7]: t3 == 1 ? [8] : [11]
[8]: leaf <arm> `string { Length: 1 } => 2`
[9]: t0 is bool ? [10] : [11]
[10]: leaf <arm> `bool => 3`
[11]: leaf <arm> `_ => 4`
", boundSwitch.ReachabilityDecisionDag.Dump());
        }

        [Fact, WorkItem(62241, "https://github.com/dotnet/roslyn/issues/62241")]
        public void DisableBalancedSwitchDispatchOptimization_Double()
        {
            var source = """
C.M(double.NaN);

public class C
{
    public static void M(double x)
    {
        string msg = x switch
        {
            < -40.0 => "Too low",
            >= -40.0 and < 0 => "Low",
            >= 0 and < 10.0 => "Acceptable",
            >= 10.0 => "High",
            double.NaN => "NaN",
        };
        System.Console.Write(msg);
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "NaN");

            var tree = comp.SyntaxTrees.First();
            var @switch = tree.GetRoot().DescendantNodes().OfType<SwitchExpressionSyntax>().Single();
            var model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            var binder = model.GetEnclosingBinder(@switch.SpanStart);
            var boundSwitch = (BoundSwitchExpression)binder.BindExpression(@switch, BindingDiagnosticBag.Discarded);
            AssertEx.AssertEqualToleratingWhitespaceDifferences("""
[0]: t0 < -40 ? [1] : [2]
[1]: leaf <arm> `< -40.0 => "Too low"`
[2]: t0 >= -40 ? [3] : [8]
[3]: t0 < 0 ? [4] : [5]
[4]: leaf <arm> `>= -40.0 and < 0 => "Low"`
[5]: t0 < 10 ? [6] : [7]
[6]: leaf <arm> `>= 0 and < 10.0 => "Acceptable"`
[7]: leaf <arm> `>= 10.0 => "High"`
[8]: leaf <arm> `double.NaN => "NaN"`
""", boundSwitch.ReachabilityDecisionDag.Dump());

            verifier.VerifyIL("C.M", """
{
  // Code size       95 (0x5f)
  .maxstack  2
  .locals init (string V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.r8     -40
  IL_000a:  blt.s      IL_0032
  IL_000c:  ldarg.0
  IL_000d:  ldc.r8     -40
  IL_0016:  blt.un.s   IL_0052
  IL_0018:  ldarg.0
  IL_0019:  ldc.r8     0
  IL_0022:  blt.s      IL_003a
  IL_0024:  ldarg.0
  IL_0025:  ldc.r8     10
  IL_002e:  blt.s      IL_0042
  IL_0030:  br.s       IL_004a
  IL_0032:  ldstr      "Too low"
  IL_0037:  stloc.0
  IL_0038:  br.s       IL_0058
  IL_003a:  ldstr      "Low"
  IL_003f:  stloc.0
  IL_0040:  br.s       IL_0058
  IL_0042:  ldstr      "Acceptable"
  IL_0047:  stloc.0
  IL_0048:  br.s       IL_0058
  IL_004a:  ldstr      "High"
  IL_004f:  stloc.0
  IL_0050:  br.s       IL_0058
  IL_0052:  ldstr      "NaN"
  IL_0057:  stloc.0
  IL_0058:  ldloc.0
  IL_0059:  call       "void System.Console.Write(string)"
  IL_005e:  ret
}
""");
        }

        [Fact, WorkItem(62241, "https://github.com/dotnet/roslyn/issues/62241")]
        public void DisableBalancedSwitchDispatchOptimization_Single()
        {
            var source = """
C.M(float.NaN);

public class C
{
    public static void M(float x)
    {
        string msg = x switch
        {
            < -40.0f => "Too low",
            >= -40.0f and < 0f => "Low",
            >= 0f and < 10.0f => "Acceptable",
            >= 10.0f => "High",
            float.NaN => "NaN",
        };
        System.Console.Write(msg);
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "NaN");

            var tree = comp.SyntaxTrees.First();
            var @switch = tree.GetRoot().DescendantNodes().OfType<SwitchExpressionSyntax>().Single();
            var model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            var binder = model.GetEnclosingBinder(@switch.SpanStart);
            var boundSwitch = (BoundSwitchExpression)binder.BindExpression(@switch, BindingDiagnosticBag.Discarded);
            AssertEx.AssertEqualToleratingWhitespaceDifferences("""
[0]: t0 < -40 ? [1] : [2]
[1]: leaf <arm> `< -40.0f => "Too low"`
[2]: t0 >= -40 ? [3] : [8]
[3]: t0 < 0 ? [4] : [5]
[4]: leaf <arm> `>= -40.0f and < 0f => "Low"`
[5]: t0 < 10 ? [6] : [7]
[6]: leaf <arm> `>= 0f and < 10.0f => "Acceptable"`
[7]: leaf <arm> `>= 10.0f => "High"`
[8]: leaf <arm> `float.NaN => "NaN"`
""", boundSwitch.ReachabilityDecisionDag.Dump());

            verifier.VerifyIL("C.M", """
{
  // Code size       79 (0x4f)
  .maxstack  2
  .locals init (string V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.r4     -40
  IL_0006:  blt.s      IL_0022
  IL_0008:  ldarg.0
  IL_0009:  ldc.r4     -40
  IL_000e:  blt.un.s   IL_0042
  IL_0010:  ldarg.0
  IL_0011:  ldc.r4     0
  IL_0016:  blt.s      IL_002a
  IL_0018:  ldarg.0
  IL_0019:  ldc.r4     10
  IL_001e:  blt.s      IL_0032
  IL_0020:  br.s       IL_003a
  IL_0022:  ldstr      "Too low"
  IL_0027:  stloc.0
  IL_0028:  br.s       IL_0048
  IL_002a:  ldstr      "Low"
  IL_002f:  stloc.0
  IL_0030:  br.s       IL_0048
  IL_0032:  ldstr      "Acceptable"
  IL_0037:  stloc.0
  IL_0038:  br.s       IL_0048
  IL_003a:  ldstr      "High"
  IL_003f:  stloc.0
  IL_0040:  br.s       IL_0048
  IL_0042:  ldstr      "NaN"
  IL_0047:  stloc.0
  IL_0048:  ldloc.0
  IL_0049:  call       "void System.Console.Write(string)"
  IL_004e:  ret
}
""");
        }

        [Fact, WorkItem(62241, "https://github.com/dotnet/roslyn/issues/62241")]
        public void DisableBalancedSwitchDispatchOptimization_Double_StartingWithHigh()
        {
            var source = """
C.M(double.NaN);

public class C
{
    public static void M(double x)
    {
        string msg = x switch
        {
            >= 10.0 => "High",
            >= 0 and < 10.0 => "Acceptable",
            >= -40.0 and < 0 => "Low",
            < -40.0 => "Too low",
            double.NaN => "NaN",
        };
        System.Console.Write(msg);
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "NaN");

            var tree = comp.SyntaxTrees.First();
            var @switch = tree.GetRoot().DescendantNodes().OfType<SwitchExpressionSyntax>().Single();
            var model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            var binder = model.GetEnclosingBinder(@switch.SpanStart);
            var boundSwitch = (BoundSwitchExpression)binder.BindExpression(@switch, BindingDiagnosticBag.Discarded);
            AssertEx.AssertEqualToleratingWhitespaceDifferences("""
[0]: t0 >= 10 ? [1] : [2]
[1]: leaf <arm> `>= 10.0 => "High"`
[2]: t0 >= 0 ? [3] : [4]
[3]: leaf <arm> `>= 0 and < 10.0 => "Acceptable"`
[4]: t0 >= -40 ? [5] : [6]
[5]: leaf <arm> `>= -40.0 and < 0 => "Low"`
[6]: t0 < -40 ? [7] : [8]
[7]: leaf <arm> `< -40.0 => "Too low"`
[8]: leaf <arm> `double.NaN => "NaN"`
""", boundSwitch.ReachabilityDecisionDag.Dump());

            verifier.VerifyIL("C.M", """
{
  // Code size       95 (0x5f)
  .maxstack  2
  .locals init (string V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.r8     10
  IL_000a:  bge.s      IL_0032
  IL_000c:  ldarg.0
  IL_000d:  ldc.r8     0
  IL_0016:  bge.s      IL_003a
  IL_0018:  ldarg.0
  IL_0019:  ldc.r8     -40
  IL_0022:  bge.s      IL_0042
  IL_0024:  ldarg.0
  IL_0025:  ldc.r8     -40
  IL_002e:  blt.s      IL_004a
  IL_0030:  br.s       IL_0052
  IL_0032:  ldstr      "High"
  IL_0037:  stloc.0
  IL_0038:  br.s       IL_0058
  IL_003a:  ldstr      "Acceptable"
  IL_003f:  stloc.0
  IL_0040:  br.s       IL_0058
  IL_0042:  ldstr      "Low"
  IL_0047:  stloc.0
  IL_0048:  br.s       IL_0058
  IL_004a:  ldstr      "Too low"
  IL_004f:  stloc.0
  IL_0050:  br.s       IL_0058
  IL_0052:  ldstr      "NaN"
  IL_0057:  stloc.0
  IL_0058:  ldloc.0
  IL_0059:  call       "void System.Console.Write(string)"
  IL_005e:  ret
}
""");
        }

        [Fact, WorkItem(62241, "https://github.com/dotnet/roslyn/issues/62241")]
        public void DisableBalancedSwitchDispatchOptimization_Double_StartingWithNaN()
        {
            var source = """
C.M(double.NaN);

public class C
{
    public static void M(double x)
    {
        string msg = x switch
        {
            double.NaN => "NaN",
            < -40.0 => "Too low",
            >= -40.0 and < 0 => "Low",
            >= 0 and < 10.0 => "Acceptable",
            >= 10.0 => "High",
        };
        System.Console.Write(msg);
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "NaN");

            var tree = comp.SyntaxTrees.First();
            var @switch = tree.GetRoot().DescendantNodes().OfType<SwitchExpressionSyntax>().Single();
            var model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            var binder = model.GetEnclosingBinder(@switch.SpanStart);
            var boundSwitch = (BoundSwitchExpression)binder.BindExpression(@switch, BindingDiagnosticBag.Discarded);
            AssertEx.AssertEqualToleratingWhitespaceDifferences("""
[0]: t0 == NaN ? [1] : [2]
[1]: leaf <arm> `double.NaN => "NaN"`
[2]: t0 < -40 ? [3] : [4]
[3]: leaf <arm> `< -40.0 => "Too low"`
[4]: t0 < 0 ? [5] : [6]
[5]: leaf <arm> `>= -40.0 and < 0 => "Low"`
[6]: t0 < 10 ? [7] : [8]
[7]: leaf <arm> `>= 0 and < 10.0 => "Acceptable"`
[8]: leaf <arm> `>= 10.0 => "High"`
""", boundSwitch.ReachabilityDecisionDag.Dump());

            verifier.VerifyIL("C.M", """
{
  // Code size       91 (0x5b)
  .maxstack  2
  .locals init (string V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "bool double.IsNaN(double)"
  IL_0006:  brtrue.s   IL_002e
  IL_0008:  ldarg.0
  IL_0009:  ldc.r8     -40
  IL_0012:  blt.s      IL_0036
  IL_0014:  ldarg.0
  IL_0015:  ldc.r8     0
  IL_001e:  blt.s      IL_003e
  IL_0020:  ldarg.0
  IL_0021:  ldc.r8     10
  IL_002a:  blt.s      IL_0046
  IL_002c:  br.s       IL_004e
  IL_002e:  ldstr      "NaN"
  IL_0033:  stloc.0
  IL_0034:  br.s       IL_0054
  IL_0036:  ldstr      "Too low"
  IL_003b:  stloc.0
  IL_003c:  br.s       IL_0054
  IL_003e:  ldstr      "Low"
  IL_0043:  stloc.0
  IL_0044:  br.s       IL_0054
  IL_0046:  ldstr      "Acceptable"
  IL_004b:  stloc.0
  IL_004c:  br.s       IL_0054
  IL_004e:  ldstr      "High"
  IL_0053:  stloc.0
  IL_0054:  ldloc.0
  IL_0055:  call       "void System.Console.Write(string)"
  IL_005a:  ret
}
""");
        }

        [Fact, WorkItem(62241, "https://github.com/dotnet/roslyn/issues/62241")]
        public void DisableBalancedSwitchDispatchOptimization_Double_DefaultCase()
        {
            var source = """
C.M(double.NaN);

public class C
{
    public static void M(double x)
    {
        string msg = x switch
        {
            < -40.0 => "Too low",
            >= -40.0 and < 0 => "Low",
            >= 0 and < 10.0 => "Acceptable",
            >= 10.0 => "High",
            _ => "NaN",
        };
        System.Console.Write(msg);
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "NaN");

            var tree = comp.SyntaxTrees.First();
            var @switch = tree.GetRoot().DescendantNodes().OfType<SwitchExpressionSyntax>().Single();
            var model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            var binder = model.GetEnclosingBinder(@switch.SpanStart);
            var boundSwitch = (BoundSwitchExpression)binder.BindExpression(@switch, BindingDiagnosticBag.Discarded);
            AssertEx.AssertEqualToleratingWhitespaceDifferences("""
[0]: t0 < -40 ? [1] : [2]
[1]: leaf <arm> `< -40.0 => "Too low"`
[2]: t0 >= -40 ? [3] : [8]
[3]: t0 < 0 ? [4] : [5]
[4]: leaf <arm> `>= -40.0 and < 0 => "Low"`
[5]: t0 < 10 ? [6] : [7]
[6]: leaf <arm> `>= 0 and < 10.0 => "Acceptable"`
[7]: leaf <arm> `>= 10.0 => "High"`
[8]: leaf <arm> `_ => "NaN"`
""", boundSwitch.ReachabilityDecisionDag.Dump());

            verifier.VerifyIL("C.M", """
{
  // Code size       95 (0x5f)
  .maxstack  2
  .locals init (string V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.r8     -40
  IL_000a:  blt.s      IL_0032
  IL_000c:  ldarg.0
  IL_000d:  ldc.r8     -40
  IL_0016:  blt.un.s   IL_0052
  IL_0018:  ldarg.0
  IL_0019:  ldc.r8     0
  IL_0022:  blt.s      IL_003a
  IL_0024:  ldarg.0
  IL_0025:  ldc.r8     10
  IL_002e:  blt.s      IL_0042
  IL_0030:  br.s       IL_004a
  IL_0032:  ldstr      "Too low"
  IL_0037:  stloc.0
  IL_0038:  br.s       IL_0058
  IL_003a:  ldstr      "Low"
  IL_003f:  stloc.0
  IL_0040:  br.s       IL_0058
  IL_0042:  ldstr      "Acceptable"
  IL_0047:  stloc.0
  IL_0048:  br.s       IL_0058
  IL_004a:  ldstr      "High"
  IL_004f:  stloc.0
  IL_0050:  br.s       IL_0058
  IL_0052:  ldstr      "NaN"
  IL_0057:  stloc.0
  IL_0058:  ldloc.0
  IL_0059:  call       "void System.Console.Write(string)"
  IL_005e:  ret
}
""");
        }

        [Fact, WorkItem(62241, "https://github.com/dotnet/roslyn/issues/62241")]
        public void DisableBalancedSwitchDispatchOptimization_Double_WhenClause()
        {
            var source = """
C.M(double.NaN);

public class C
{
    public static void M(double x)
    {
        bool b = true;
        string msg = x switch
        {
            < -40.0 => "Too low",
            >= -40.0 and < 0 => "Low",
            >= 0 and < 10.0 => "Acceptable",
            >= 10.0 => "High",
            double.NaN when b => "NaN",
            _ => "Other",
        };
        System.Console.Write(msg);
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "NaN");

            var tree = comp.SyntaxTrees.First();
            var @switch = tree.GetRoot().DescendantNodes().OfType<SwitchExpressionSyntax>().Single();
            var model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            var binder = model.GetEnclosingBinder(@switch.SpanStart);
            var boundSwitch = (BoundSwitchExpression)binder.BindExpression(@switch, BindingDiagnosticBag.Discarded);
            AssertEx.AssertEqualToleratingWhitespaceDifferences("""
[0]: t0 < -40 ? [1] : [2]
[1]: leaf <arm> `< -40.0 => "Too low"`
[2]: t0 >= -40 ? [3] : [8]
[3]: t0 < 0 ? [4] : [5]
[4]: leaf <arm> `>= -40.0 and < 0 => "Low"`
[5]: t0 < 10 ? [6] : [7]
[6]: leaf <arm> `>= 0 and < 10.0 => "Acceptable"`
[7]: leaf <arm> `>= 10.0 => "High"`
[8]: when (b) ? [10] : [9]
[9]: leaf <arm> `_ => "Other"`
[10]: leaf <arm> `double.NaN when b => "NaN"`
""", boundSwitch.ReachabilityDecisionDag.Dump());

            verifier.VerifyIL("C.M", """
{
  // Code size      110 (0x6e)
  .maxstack  2
  .locals init (bool V_0, //b
                string V_1,
                double V_2)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  stloc.2
  IL_0004:  ldloc.2
  IL_0005:  ldc.r8     -40
  IL_000e:  blt.s      IL_0036
  IL_0010:  ldloc.2
  IL_0011:  ldc.r8     -40
  IL_001a:  blt.un.s   IL_0056
  IL_001c:  ldloc.2
  IL_001d:  ldc.r8     0
  IL_0026:  blt.s      IL_003e
  IL_0028:  ldloc.2
  IL_0029:  ldc.r8     10
  IL_0032:  blt.s      IL_0046
  IL_0034:  br.s       IL_004e
  IL_0036:  ldstr      "Too low"
  IL_003b:  stloc.1
  IL_003c:  br.s       IL_0067
  IL_003e:  ldstr      "Low"
  IL_0043:  stloc.1
  IL_0044:  br.s       IL_0067
  IL_0046:  ldstr      "Acceptable"
  IL_004b:  stloc.1
  IL_004c:  br.s       IL_0067
  IL_004e:  ldstr      "High"
  IL_0053:  stloc.1
  IL_0054:  br.s       IL_0067
  IL_0056:  ldloc.0
  IL_0057:  brfalse.s  IL_0061
  IL_0059:  ldstr      "NaN"
  IL_005e:  stloc.1
  IL_005f:  br.s       IL_0067
  IL_0061:  ldstr      "Other"
  IL_0066:  stloc.1
  IL_0067:  ldloc.1
  IL_0068:  call       "void System.Console.Write(string)"
  IL_006d:  ret
}
""");
        }
#endif

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67923")]
        public void VarPatternCapturingAfterDisjunctiveTypeTest()
        {
            var source = """
A a = new B();

if (a is (B or C) and var x)
{
    if (x is null)
        throw null;
    else
        System.Console.WriteLine("OK");
}

class A { }
class B : A { }
class C : A { }
class D : A { }
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "OK");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var x = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().First();
            Assert.Equal("x", x.ToString());
            Assert.Equal("A? x", model.GetDeclaredSymbol(x).ToTestDisplayString());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67923")]
        public void DiscardPatternAfterDisjunctiveTypeTest()
        {
            var source = """
A a = new B();

if (a is (B or C) and _)
{
}

class A { }
class B : A { }
class C : A { }
class D : A { }
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,23): warning CS9268: The pattern is redundant.
                // if (a is (B or C) and _)
                Diagnostic(ErrorCode.WRN_RedundantPattern, "_").WithLocation(3, 23));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotAOrBPattern()
        {
            var source = """
object o = null;
_ = o is not A or B; // 1
_ = o is not (A and not B); // 2
_ = o is not (A or B);
_ = o is (not A) or B; // 3
_ = o is (not A) or (not B); // 4 TODO2
_ = o is A or B;
_ = o is A or not B;

_ = o switch
{
    not A => 42,
    B => 43, // 5
    _ => 44
};

switch (o)
{
    case not A: break;
    case B: break; // 6
    default: break;
};

_ = o switch
{
    A and not B => 42, // 7
    _ => 44
};

_ = o switch
{
    not A => 42,
    not B => 43,
    _ => 44 // 8
};

class A { }
class B { }
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,19): warning CS9268: The pattern is redundant.
                // _ = o is not A or B; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "B").WithLocation(2, 19),
                // (3,25): error CS8121: An expression of type 'A' cannot be handled by a pattern of type 'B'.
                // _ = o is not (A and not B); // 2
                Diagnostic(ErrorCode.ERR_PatternWrongType, "B").WithArguments("A", "B").WithLocation(3, 25),
                // (5,21): warning CS9268: The pattern is redundant.
                // _ = o is (not A) or B; // 3
                Diagnostic(ErrorCode.WRN_RedundantPattern, "B").WithLocation(5, 21),
                // (6,5): warning CS8794: An expression of type 'object' always matches the provided pattern.
                // _ = o is (not A) or (not B); // 4 TODO2
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "o is (not A) or (not B)").WithArguments("object").WithLocation(6, 5),
                // (6,10): warning CS9268: The pattern is redundant.
                // _ = o is (not A) or (not B); // 4 TODO2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "(not A) or (not B)").WithLocation(6, 10),
                // (13,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     B => 43, // 5
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "B").WithLocation(13, 5),
                // (20,10): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //     case B: break; // 6
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "B").WithLocation(20, 10),
                // (26,15): error CS8121: An expression of type 'A' cannot be handled by a pattern of type 'B'.
                //     A and not B => 42, // 7
                Diagnostic(ErrorCode.ERR_PatternWrongType, "B").WithArguments("A", "B").WithLocation(26, 15),
                // (34,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     _ => 44 // 8
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "_").WithLocation(34, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotAOrBPattern_Interfaces()
        {
            var source = """
object o = null;
_ = o is not A or B;
_ = o is not (A or B);
_ = o is (not A) or B;
_ = o is (not A) or (not B);
_ = o is A or B;
_ = o is A or not B;

_ = o switch
{
    not A => 42,
    B => 43,
    _ => 44
};

_ = o switch
{
    not A => 42,
    not B => 43,
    _ => 44
};

interface A { }
interface B { }
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotAOrDerivedPattern()
        {
            var source = """
object o = null;
_ = o is not A or Derived;
_ = o is not Derived or A; // 1
_ = o is not (A or Derived); // 2
_ = o is (not A) or Derived;
_ = o is (not A) or (not Derived);
_ = o is A or Derived; // 3
_ = o is Derived or A;
_ = o is A or not Derived; // 4
_ = o is Derived or not A;

_ = o switch
{
    not Derived => 42,
    A => 43,
    _ => 44 // 5
};

_ = o switch
{
    not A => 42,
    Derived => 43,
    _ => 44
};

_ = o switch
{
    A => 42,
    Derived => 43, // 6
    _ => 44
};

_ = o switch
{
    A => 42,
    not Derived => 43,
    _ => 44 // 7
};

class A { }
class Derived : A { }
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,5): warning CS8794: An expression of type 'object' always matches the provided pattern.
                // _ = o is not Derived or A; // 1
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "o is not Derived or A").WithArguments("object").WithLocation(3, 5),
                // (4,20): warning CS9268: The pattern is redundant.
                // _ = o is not (A or Derived); // 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "Derived").WithLocation(4, 20),
                // (7,15): warning CS9268: The pattern is redundant.
                // _ = o is A or Derived; // 3
                Diagnostic(ErrorCode.WRN_RedundantPattern, "Derived").WithLocation(7, 15),
                // (9,5): warning CS8794: An expression of type 'object' always matches the provided pattern.
                // _ = o is A or not Derived; // 4
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "o is A or not Derived").WithArguments("object").WithLocation(9, 5),
                // (16,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     _ => 44 // 5
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "_").WithLocation(16, 5),
                // (29,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     Derived => 43, // 6
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "Derived").WithLocation(29, 5),
                // (37,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     _ => 44 // 7
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "_").WithLocation(37, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_VarOrNotAOrBPattern()
        {
            var source = """
object o = null;
_ = o is var x1 or not A or B; // 1, 2, 3, 4
_ = o is var x2 or not (A or B); // 3, 4, 5, 6, 7

_ = o switch
{
    var x3 => 42,
    not A or B => 43, // 8, TODO2 handle switches
    _ => 44 // 5
};

_ = o switch
{
    var x4 => 42,
    not (A or B) => 43, // 6
    _ => 44 // 7
};

class A { }
class B { }
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,5): warning CS8794: An expression of type 'object' always matches the provided pattern.
                // _ = o is var x1 or not A or B; // 1, 2, 3, 4
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "o is var x1 or not A or B").WithArguments("object").WithLocation(2, 5),
                // (2,14): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                // _ = o is var x1 or not A or B; // 1, 2, 3, 4
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "x1").WithLocation(2, 14),
                // (2,24): warning CS9268: The pattern is redundant.
                // _ = o is var x1 or not A or B; // 1, 2, 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "A").WithLocation(2, 24),
                // (2,29): warning CS9268: The pattern is redundant.
                // _ = o is var x1 or not A or B; // 1, 2, 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "B").WithLocation(2, 29),
                // (3,5): warning CS8794: An expression of type 'object' always matches the provided pattern.
                // _ = o is var x2 or not (A or B); // 3, 4, 5, 6, 7
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "o is var x2 or not (A or B)").WithArguments("object").WithLocation(3, 5),
                // (3,14): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                // _ = o is var x2 or not (A or B); // 3, 4, 5, 6, 7
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "x2").WithLocation(3, 14),
                // (3,25): warning CS9268: The pattern is redundant.
                // _ = o is var x2 or not (A or B); // 3, 4, 5, 6, 7
                Diagnostic(ErrorCode.WRN_RedundantPattern, "A or B").WithLocation(3, 25),
                // (3,25): warning CS9268: The pattern is redundant.
                // _ = o is var x2 or not (A or B); // 3, 4, 5, 6, 7
                Diagnostic(ErrorCode.WRN_RedundantPattern, "A").WithLocation(3, 25),
                // (3,30): warning CS9268: The pattern is redundant.
                // _ = o is var x2 or not (A or B); // 3, 4, 5, 6, 7
                Diagnostic(ErrorCode.WRN_RedundantPattern, "B").WithLocation(3, 30),
                // (8,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     not A or B => 43, // 8, TODO2 handle switches
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "not A or B").WithLocation(8, 5),
                // (9,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     _ => 44 // 5
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "_").WithLocation(9, 5),
                // (15,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     not (A or B) => 43, // 6
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "not (A or B)").WithLocation(15, 5),
                // (16,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     _ => 44 // 7
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "_").WithLocation(16, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_AOrNotNullPattern()
        {
            var source = """
#nullable enable
object? o = null;
_ = o is A or not null;

_ = o switch
{
    A => 42,
    not null => 43,
    _ => 44
};

class A { }
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotAOrBOrCPattern()
        {
            var source = """
object o = null;
_ = o is not A or (B or C); // 1
_ = o is not (A or (B or C));

_ = o switch
{
    not A => 42,
    B or C => 43, // 2
    _ => 44
};

class A { }
class B { }
class C { }
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,20): warning CS9268: The pattern is redundant.
                // _ = o is not A or (B or C); // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "B").WithLocation(2, 20),
                // (2,25): warning CS9268: The pattern is redundant.
                // _ = o is not A or (B or C); // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "C").WithLocation(2, 25),
                // (8,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     B or C => 43, // 2
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "B or C").WithLocation(8, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotNullOrBPattern()
        {
            var source = """
#nullable enable
object? o = null;
_ = o is not null or string; // 1
_ = o is (not null) or string; // 2
_ = o is not null or (not not string); // 3
_ = o is not (null or string);

_ = o switch
{
    not null => 42,
    string => 43, // 4
    _ => 44
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,22): warning CS9268: The pattern is redundant.
                // _ = o is not null or string; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "string").WithLocation(3, 22),
                // (4,24): warning CS9268: The pattern is redundant.
                // _ = o is (not null) or string; // 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "string").WithLocation(4, 24),
                // (5,31): warning CS9268: The pattern is redundant.
                // _ = o is not null or (not not string); // 3
                Diagnostic(ErrorCode.WRN_RedundantPattern, "string").WithLocation(5, 31),
                // (11,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     string => 43, // 4
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "string").WithLocation(11, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotNullOrRecursivePattern()
        {
            var source = """
#nullable enable
string? s = null;
_ = s is not null or { Length: >0 }; // 1
_ = s is (not null) or { Length: >0 }; // 2
_ = s is not (null or { Length: >0 });
_ = s is null and not { Length: >0 }; // 3

_ = s switch
{
    not null => 42,
    "" => 43, // 4
    _ => 44
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,22): warning CS9268: The pattern is redundant.
                // _ = s is not null or { Length: >0 }; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "{ Length: >0 }").WithLocation(3, 22),
                // (3,24): warning CS9268: The pattern is redundant.
                // _ = s is not null or { Length: >0 }; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "Length: >0").WithLocation(3, 24),
                // (4,24): warning CS9268: The pattern is redundant.
                // _ = s is (not null) or { Length: >0 }; // 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "{ Length: >0 }").WithLocation(4, 24),
                // (4,26): warning CS9268: The pattern is redundant.
                // _ = s is (not null) or { Length: >0 }; // 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "Length: >0").WithLocation(4, 26),
                // (6,23): warning CS9268: The pattern is redundant.
                // _ = s is null and not { Length: >0 }; // 3
                Diagnostic(ErrorCode.WRN_RedundantPattern, "{ Length: >0 }").WithLocation(6, 23),
                // (6,25): warning CS9268: The pattern is redundant.
                // _ = s is null and not { Length: >0 }; // 3
                Diagnostic(ErrorCode.WRN_RedundantPattern, "Length: >0").WithLocation(6, 25),
                // (11,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     "" => 43, // 4
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, @"""""").WithLocation(11, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_RecursivePattern()
        {
            var source = """
string s = null;
_ = s is { Length: not 42 or 43 }; // 1

_ = s switch
{
    { Length: not 42 or 43 } => 42, // TODO2 handle switches
    _ => 44
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,30): warning CS9268: The pattern is redundant.
                // _ = s is { Length: not 42 or 43 }; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(2, 30));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_RecursivePattern_TwoOrs()
        {
            var source = """
S s = default;
_ = s is { Prop1: not 42 or 43, Prop2: not 44 or 45 }; // 1, 2

_ = s switch
{
    { Prop1: not 42 or 43, Prop2: not 44 or 45 } => 42, // TODO2 handle switches
    _ => 44
};

switch (s)
{
    case { Prop1: not 42 or 43, Prop2: not 44 or 45 }: break; // TODO2 handle switches
    default: break;
};

struct S
{
    public int Prop1 { get; set; }
    public int Prop2 { get; set; }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,29): warning CS9268: The pattern is redundant.
                // _ = s is { Prop1: not 42 or 43, Prop2: not 44 or 45 }; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(2, 29),
                // (2,50): warning CS9268: The pattern is redundant.
                // _ = s is { Prop1: not 42 or 43, Prop2: not 44 or 45 }; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(2, 50));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_RecursivePattern_Deconstruction_TwoOrs()
        {
            var source = """
S s = default;
_ = s is (not 42 or 43, not 44 or 45) { Prop: not 46 or 47 }; // 1, 2, 3

_ = s switch
{
    (not 42 or 43, not 44 or 45) { Prop: not 46 or 47 } => 42, // TODO2 switch
    _ => 44
};

switch (s)
{
    case (not 42 or 43, not 44 or 45) { Prop: not 46 or 47 }: break; // TODO2 switch
    default: break;
};

struct S
{
    public int Prop { get; set; }
    public void Deconstruct(out int i, out int j) => throw null;
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,21): warning CS9268: The pattern is redundant.
                // _ = s is (not 42 or 43, not 44 or 45) { Prop: not 46 or 47 }; // 1, 2, 3
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(2, 21),
                // (2,35): warning CS9268: The pattern is redundant.
                // _ = s is (not 42 or 43, not 44 or 45) { Prop: not 46 or 47 }; // 1, 2, 3
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(2, 35),
                // (2,57): warning CS9268: The pattern is redundant.
                // _ = s is (not 42 or 43, not 44 or 45) { Prop: not 46 or 47 }; // 1, 2, 3
                Diagnostic(ErrorCode.WRN_RedundantPattern, "47").WithLocation(2, 57));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_WithDiscardPattern()
        {
            var source = """
int[] s = null;
_ = s is [not 42 or 43, _]; // 1

_ = s switch
{
    [not 42 or 43, _] => 42, // TODO2 switch
    _ => 44
};

switch (s)
{
    case [not 42 or 43, _]: break; // TODO2 switch
    default: break;
};
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (2,21): warning CS9268: The pattern is redundant.
                // _ = s is [not 42 or 43, _]; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(2, 21));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_SlicePattern()
        {
            var source = """
S s = default;
_ = s is [..(not 42 or 43), not 44 or 45]; // 1, 2

_ = s switch
{
    [..(not 42 or 43), not 44 or 45] => 42, // TODO2 switch
    _ => 44
};

switch (s)
{
    case [..(not 42 or 43), not 44 or 45]: break; // TODO2 switch
    default: break;
};

public struct S
{
    public int Length => throw null;
    public int this[System.Index i] => throw null;
    public int this[System.Range r] => throw null;
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (2,24): warning CS9268: The pattern is redundant.
                // _ = s is [..(not 42 or 43), not 44 or 45]; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(2, 24),
                // (2,39): warning CS9268: The pattern is redundant.
                // _ = s is [..(not 42 or 43), not 44 or 45]; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(2, 39));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_ITuplePattern()
        {
            var source = """
System.Runtime.CompilerServices.ITuple s = null;
_ = s is (not 42 or 43, not 44 or 45); // 1, 2
_ = s is not (42 and not 43, 44 and not 45); // 3, 4
_ = s is not (42 and not 43, _ and var x); // 5, 6
_ = s is not (_ and var x2, 42 and not 43); // 7, 8
_ = s is not (_, _);
_ = s is not (var x3, var x4);

_ = s switch
{
    (not 42 or 43, not 44 or 45) => 42, // TODO2 switch
    _ => 44
};

switch (s)
{
    case (not 42 or 43, not 44 or 45): break; // TODO2 switch
    default: break;
};
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (2,21): warning CS9268: The pattern is redundant.
                // _ = s is (not 42 or 43, not 44 or 45); // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(2, 21),
                // (2,35): warning CS9268: The pattern is redundant.
                // _ = s is (not 42 or 43, not 44 or 45); // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(2, 35),
                // (3,26): warning CS9268: The pattern is redundant.
                // _ = s is not (42 and not 43, 44 and not 45); // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(3, 26),
                // (3,41): warning CS9268: The pattern is redundant.
                // _ = s is not (42 and not 43, 44 and not 45); // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(3, 41),
                // (4,26): warning CS9268: The pattern is redundant.
                // _ = s is not (42 and not 43, _ and var x); // 5, 6
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(4, 26),
                // (4,30): warning CS9268: The pattern is redundant.
                // _ = s is not (42 and not 43, _ and var x); // 5, 6
                Diagnostic(ErrorCode.WRN_RedundantPattern, "_ and var x").WithLocation(4, 30),
                // (5,15): warning CS9268: The pattern is redundant.
                // _ = s is not (_ and var x2, 42 and not 43); // 7, 8
                Diagnostic(ErrorCode.WRN_RedundantPattern, "_ and var x2").WithLocation(5, 15),
                // (5,40): warning CS9268: The pattern is redundant.
                // _ = s is not (_ and var x2, 42 and not 43); // 7, 8
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(5, 40));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotNullOrNumericLiteralPattern()
        {
            var source = """
#nullable enable
int? i = null;
_ = i is not null or 0; // 1
_ = i is (not null) or 0; // 2
_ = i is not (null or 0);

_ = i switch
{
    not null => 42,
    0 => 43, // 3
    _ => 44
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,22): warning CS9268: The pattern is redundant.
                // _ = i is not null or 0; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "0").WithLocation(3, 22),
                // (4,24): warning CS9268: The pattern is redundant.
                // _ = i is (not null) or 0; // 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "0").WithLocation(4, 24),
                // (10,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     0 => 43, // 3
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "0").WithLocation(10, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotNotNullOrNumericLiteralPattern()
        {
            var source = """
#nullable enable
int? i = null;
_ = i is not not null or 0;
_ = i is not (not null) or 0;
_ = i is (not not null) or 0;

_ = i switch
{
    not not null => 42,
    0 => 43,
    _ => 44
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NumericLiberalOrNotNullOrNumericLiteralPattern()
        {
            var source = """
#nullable enable
int? i = null;
_ = i is 0 or not null or 1; // 1

_ = i switch
{
    0 => 41,
    not null => 42,
    1 => 43, // 2
    _ => 44
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,27): warning CS9268: The pattern is redundant.
                // _ = i is 0 or not null or 1; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "1").WithLocation(3, 27),
                // (9,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     1 => 43, // 2
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "1").WithLocation(9, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotNullOrNotNumericLiteralPattern()
        {
            var source = """
#nullable enable
int? i = null;
_ = i is not null or not 0; // 1, 2
_ = i is null and 0; // 3, 4

_ = i switch
{
    not null => 42,
    not 0 => 43,
    _ => 44 // 5
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,5): warning CS8794: An expression of type 'int?' always matches the provided pattern.
                // _ = i is not null or not 0; // 1, 2
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "i is not null or not 0").WithArguments("int?").WithLocation(3, 5),
                // (3,10): warning CS9268: The pattern is redundant.
                // _ = i is not null or not 0; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "not null or not 0").WithLocation(3, 10),
                // (4,5): error CS8518: An expression of type 'int?' can never match the provided pattern.
                // _ = i is null and 0; // 3, 4
                Diagnostic(ErrorCode.ERR_IsPatternImpossible, "i is null and 0").WithArguments("int?").WithLocation(4, 5),
                // (4,10): warning CS9268: The pattern is redundant.
                // _ = i is null and 0; // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "null and 0").WithLocation(4, 10),
                // (10,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     _ => 44 // 5
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "_").WithLocation(10, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotNullOrBooleanLiteralPattern()
        {
            var source = """
#nullable enable
bool? b = null;
_ = b is not null or false; // 1
_ = b is (not null) or false; // 2
_ = b is not null or true; // 3
_ = b is (not null) or true; // 4
_ = b is not (null or false);
_ = b is not (null or true);

_ = b switch
{
    not null => 42,
    false => 43, // 5
    _ => 44
};
_ = b switch
{
    not null => 42,
    true => 43, // 6
    _ => 44
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,22): warning CS9268: The pattern is redundant.
                // _ = b is not null or false; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "false").WithLocation(3, 22),
                // (4,24): warning CS9268: The pattern is redundant.
                // _ = b is (not null) or false; // 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "false").WithLocation(4, 24),
                // (5,22): warning CS9268: The pattern is redundant.
                // _ = b is not null or true; // 3
                Diagnostic(ErrorCode.WRN_RedundantPattern, "true").WithLocation(5, 22),
                // (6,24): warning CS9268: The pattern is redundant.
                // _ = b is (not null) or true; // 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "true").WithLocation(6, 24),
                // (13,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     false => 43, // 5
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "false").WithLocation(13, 5),
                // (19,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     true => 43, // 6
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "true").WithLocation(19, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotNullOrBooleanLiteralPattern_WrongType()
        {
            var source = """
#nullable enable
int? i = null;
_ = i is not null or false; // 1
_ = i is (not null) or false; // 2

_ = i switch
{
    not null => 42,
    false => 43, // 3
    _ => 44
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,22): error CS0029: Cannot implicitly convert type 'bool' to 'int?'
                // _ = i is not null or false; // 1
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "false").WithArguments("bool", "int?").WithLocation(3, 22),
                // (4,24): error CS0029: Cannot implicitly convert type 'bool' to 'int?'
                // _ = i is (not null) or false; // 2
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "false").WithArguments("bool", "int?").WithLocation(4, 24),
                // (9,5): error CS0029: Cannot implicitly convert type 'bool' to 'int?'
                //     false => 43, // 3
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "false").WithArguments("bool", "int?").WithLocation(9, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotNullOrEmptyListPattern()
        {
            var source = """
#nullable enable
int[]? i = null;
_ = i is not null or []; // 1
_ = i is (not null) or []; // 2
_ = i is not (null or []);

_ = i switch
{
    not null => 42,
    [] => 43, // 3
    _ => 44
};
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (3,22): warning CS9268: The pattern is redundant.
                // _ = i is not null or []; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "[]").WithLocation(3, 22),
                // (4,24): warning CS9268: The pattern is redundant.
                // _ = i is (not null) or []; // 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "[]").WithLocation(4, 24),
                // (10,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     [] => 43, // 3
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "[]").WithLocation(10, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotNullOrDiscardPattern()
        {
            var source = """
#nullable enable
int[]? i = null;
_ = i is not null or _; // 1
_ = i is (not null) or _; // 2
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (3,5): warning CS8794: An expression of type 'int[]' always matches the provided pattern.
                // _ = i is not null or _; // 1
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "i is not null or _").WithArguments("int[]").WithLocation(3, 5),
                // (4,5): warning CS8794: An expression of type 'int[]' always matches the provided pattern.
                // _ = i is (not null) or _; // 2
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "i is (not null) or _").WithArguments("int[]").WithLocation(4, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotNullOrEmptyStringPattern()
        {
            var source = """
#nullable enable
string? s = null;
_ = s is not null or ""; // 1
_ = s is (not null) or ""; // 2
_ = s is not (null or "");

_ = s switch
{
    not null => 42,
    "" => 43, // 3
    _ => 44
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,22): warning CS9268: The pattern is redundant.
                // _ = s is not null or ""; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, @"""""").WithLocation(3, 22),
                // (4,24): warning CS9268: The pattern is redundant.
                // _ = s is (not null) or ""; // 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, @"""""").WithLocation(4, 24),
                // (10,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     "" => 43, // 3
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, @"""""").WithLocation(10, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotStringLiteralOrOtherStringLiteralPattern()
        {
            var source = """
object o = null;
_ = o is not "A" or "B"; // 1
_ = o is (not "A") or "B"; // 2
_ = o is not ("A" or "B");

_ = o switch
{
    not "A" => 42,
    "B" => 43, // 3
    _ => 44
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,21): warning CS9268: The pattern is redundant.
                // _ = o is not "A" or "B"; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, @"""B""").WithLocation(2, 21),
                // (3,23): warning CS9268: The pattern is redundant.
                // _ = o is (not "A") or "B"; // 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, @"""B""").WithLocation(3, 23),
                // (9,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     "B" => 43, // 3
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, @"""B""").WithLocation(9, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotStringLiteralOrOtherStringLiteralPatterns()
        {
            var source = """
object o = null;
_ = o is not "A" or "B" or "C"; // 1, 2
_ = o is (not "A") or "B" or "C"; // 3, 4
_ = o is not ("A" or "B" or "C");

_ = o switch
{
    not "A" => 42,
    "B" => 43, // 5
    "C" => 44, // 6
    _ => 45
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,21): warning CS9268: The pattern is redundant.
                // _ = o is not "A" or "B" or "C"; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, @"""B""").WithLocation(2, 21),
                // (2,28): warning CS9268: The pattern is redundant.
                // _ = o is not "A" or "B" or "C"; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, @"""C""").WithLocation(2, 28),
                // (3,23): warning CS9268: The pattern is redundant.
                // _ = o is (not "A") or "B" or "C"; // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, @"""B""").WithLocation(3, 23),
                // (3,30): warning CS9268: The pattern is redundant.
                // _ = o is (not "A") or "B" or "C"; // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, @"""C""").WithLocation(3, 30),
                // (9,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     "B" => 43, // 5
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, @"""B""").WithLocation(9, 5),
                // (10,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     "C" => 44, // 6
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, @"""C""").WithLocation(10, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotStringLiteralOrNullPattern()
        {
            var source = """
object o = null;
_ = o is not "A" or null; // 1
_ = o is (not "A") or null; // 2
_ = o is not ("A" or null);

_ = o switch
{
    not "A" => 42,
    null => 43, // 3
    _ => 44
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,21): warning CS9268: The pattern is redundant.
                // _ = o is not "A" or null; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "null").WithLocation(2, 21),
                // (3,23): warning CS9268: The pattern is redundant.
                // _ = o is (not "A") or null; // 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "null").WithLocation(3, 23),
                // (9,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     null => 43, // 3
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "null").WithLocation(9, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotNumericLiteralOrOtherNumericLiteralPattern()
        {
            var source = """
object o = null;
_ = o is not 42 or 43; // 1
_ = o is (not 42) or 43; // 2
_ = o is not (42 or 43);

_ = o switch
{
    not 42 => 42,
    43 => 43, // 3
    _ => 44
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,20): warning CS9268: The pattern is redundant.
                // _ = o is not 42 or 43; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(2, 20),
                // (3,22): warning CS9268: The pattern is redundant.
                // _ = o is (not 42) or 43; // 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(3, 22),
                // (9,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     43 => 43, // 3
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "43").WithLocation(9, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotNumericLiteralOrTypePattern()
        {
            var source = """
#nullable enable
object? o = null;
_ = o is not 42L or string; // 1

_ = o switch
{
    not 42L => 42,
    string => 43, // 2
    _ => 44
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,21): warning CS9268: The pattern is redundant.
                // _ = o is not 42L or string; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "string").WithLocation(3, 21),
                // (8,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     string => 43, // 2
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "string").WithLocation(8, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotTypeAndRecursiveOrBPattern()
        {
            var source = """
object o = null;
_ = o is not string { Length: > 0 } or B; // 1
_ = o is not (string { Length: > 0 } or B);

_ = o switch
{
    not string { Length: > 0 } => 42,
    B => 43, // 2
    _ => 44
};

class B { }
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,40): warning CS9268: The pattern is redundant.
                // _ = o is not string { Length: > 0 } or B; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "B").WithLocation(2, 40),
                // (8,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     B => 43, // 2
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "B").WithLocation(8, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotRecursiveOrStringLiteralPattern()
        {
            var source = """
string o = null;
_ = o is not { Length: > 0 } or "hi";

_ = o switch
{
    not { Length: > 0 } => 42,
    "hi" => 43,
    _ => 44
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotRecursiveOrNullPattern()
        {
            var source = """
string o = null;
_ = o is not { Length: > 0 } or null; // 1
_ = o is not ({ Length: > 0 } or null);

_ = o switch
{
    not { Length: > 0 } => 42,
    null => 43, // 2
    _ => 44
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,33): warning CS9268: The pattern is redundant.
                // _ = o is not { Length: > 0 } or null; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "null").WithLocation(2, 33),
                // (8,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     null => 43, // 2
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "null").WithLocation(8, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotNullOrVariousNumericLiteralPattern()
        {
            var source = """
object o = null;
_ = o is not null or not not "1" or "2"; // 1, 2
_ = o is not (null or not not "1" or "2");

_ = o switch
{
    not null => 42,
    not not "1" or "2" => 43, // 3
    _ => 44
};

_ = o switch
{
    not null => 42,
    not not "1" => 43, // 4
    "2"  => 44, // 5
    _ => 45
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,30): warning CS9268: The pattern is redundant.
                // _ = o is not null or not not "1" or "2"; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, @"""1""").WithLocation(2, 30),
                // (2,37): warning CS9268: The pattern is redundant.
                // _ = o is not null or not not "1" or "2"; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, @"""2""").WithLocation(2, 37),
                // (8,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     not not "1" or "2" => 43, // 3
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, @"not not ""1"" or ""2""").WithLocation(8, 5),
                // (15,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     not not "1" => 43, // 4
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, @"not not ""1""").WithLocation(15, 5),
                // (16,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     "2"  => 44, // 5
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, @"""2""").WithLocation(16, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotNullOrEnumPattern()
        {
            var source = """
E? e = null;
_ = e is not null or E.Zero; // 1
_ = e is not (null or E.Zero);

_ = e switch
{
    not null => 42,
    E.Zero => 43, // 2
    _ => 44
};
enum E { Zero = 0 }
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,22): warning CS9268: The pattern is redundant.
                // _ = e is not null or E.Zero; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "E.Zero").WithLocation(2, 22),
                // (8,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     E.Zero => 43, // 2
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "E.Zero").WithLocation(8, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotEnumOrEnumPattern()
        {
            var source = """
E e = E.Zero;
_ = e is not E.Zero or E.One; // 1
_ = e is not (E.Zero or E.One);

_ = e switch
{
    not E.Zero => 42,
    E.One => 43, // 2
    _ => 44
};
enum E { Zero = 0, One = 1 }
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,24): warning CS9268: The pattern is redundant.
                // _ = e is not E.Zero or E.One; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "E.One").WithLocation(2, 24),
                // (8,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     E.One => 43, // 2
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "E.One").WithLocation(8, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotRecursiveOrRecursivePattern()
        {
            var source = """
string s = null;

_ = s is not { Length: 0 } or { Length: 10 }; // 1
_ = s is not ({ Length: 0 } or { Length: 10 });

_ = s switch
{
    not { Length: 0 } => 42,
    { Length: 10 } => 43, // 2
    _ => 44
};

object o = null;

_ = s is not string { Length: 0 } or string { Length: 10 }; // 3
_ = s is not (string { Length: 0 } or string { Length: 10 });

_ = o switch
{
    not string { Length: 0 } => 42,
    string { Length: 10 } => 43, // 4
    _ => 44
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,31): warning CS9268: The pattern is redundant.
                // _ = s is not { Length: 0 } or { Length: 10 }; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "{ Length: 10 }").WithLocation(3, 31),
                // (9,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     { Length: 10 } => 43, // 2
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "{ Length: 10 }").WithLocation(9, 5),
                // (15,38): warning CS9268: The pattern is redundant.
                // _ = s is not string { Length: 0 } or string { Length: 10 }; // 3
                Diagnostic(ErrorCode.WRN_RedundantPattern, "string { Length: 10 }").WithLocation(15, 38),
                // (21,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     string { Length: 10 } => 43, // 4
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "string { Length: 10 }").WithLocation(21, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotRedundant()
        {
            // A collection of legitimate usages of `not ... or ...` pattern
            var source = """
string s = null;
_ = s is not object or { Length: 0 };

_ = s switch
{
    not object => 42,
    { Length: 0 } => 43,
    _ => 44
};

object o = null;

_ = o is not Base or Derived { };

_ = o switch
{
    not Base => 42,
    Derived => 43,
    _ => 44
};

_ = o is not string or string { Length: 0 };
_ = o is not (not string or string { Length: 0 });

_ = o switch
{
    not string => 42,
    string { Length: 0 } => 43,
    _ => 44
};

_ = o is not C { Prop1: true } or C { Prop2: 10 };

_ = o switch
{
    not C { Prop1: true } => 42,
    C { Prop2: 10 } => 43,
    _ => 44
};

C c = null;

_ = c is not { Prop1: true } or { Prop2: 10 };

_ = c switch
{
    not { Prop1: true } => 42,
    { Prop2: 10 } => 43,
    _ => 44
};

_ = o is not bool or true;

_ = o switch
{
    not bool => 42,
    true => 43,
    _ => 44
};

class Base { }
class Derived : Base { }

class C
{
    public bool Prop1 { get; set; }
    public int Prop2 { get; set; }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_TODO2_16()
        {
            var source = """
object o = null;
_ = o is not string or string { };
_ = o is not (not string or string { });
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,5): warning CS8794: An expression of type 'object' always matches the provided pattern.
                // _ = o is not string or string { };
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "o is not string or string { }").WithArguments("object").WithLocation(2, 5),
                // (3,5): error CS8518: An expression of type 'object' can never match the provided pattern.
                // _ = o is not (not string or string { });
                Diagnostic(ErrorCode.ERR_IsPatternImpossible, "o is not (not string or string { })").WithArguments("object").WithLocation(3, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotNullOrNotAOrBPattern()
        {
            var source = """
object o = null;
_ = o is not null or not A or B; // 1, 2
_ = o is {} or not A or B; // 3, 4

_ = o switch
{
    not null => 41,
    not A => 42,
    B => 43, // 5
};

_ = o switch
{
    {} => 41,
    not A => 42,
    B => 43, // 6
};

class A { }
class B { }
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,5): warning CS8794: An expression of type 'object' always matches the provided pattern.
                // _ = o is not null or not A or B; // 1, 2
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "o is not null or not A or B").WithArguments("object").WithLocation(2, 5),
                // (2,31): warning CS9268: The pattern is redundant.
                // _ = o is not null or not A or B; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "B").WithLocation(2, 31),
                // (3,5): warning CS8794: An expression of type 'object' always matches the provided pattern.
                // _ = o is {} or not A or B; // 3, 4
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "o is {} or not A or B").WithArguments("object").WithLocation(3, 5),
                // (3,25): warning CS9268: The pattern is redundant.
                // _ = o is {} or not A or B; // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "B").WithLocation(3, 25),
                // (9,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     B => 43, // 5
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "B").WithLocation(9, 5),
                // (16,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     B => 43, // 6
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "B").WithLocation(16, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_BaseOrNotAOrBPattern()
        {
            var source = """
object o = null;
_ = o is Base { Prop: 10 } or not A or B; // 1

_ = o switch
{
    Base { Prop: 10 } => 41,
    not A => 42,
    B => 43, // 2
    _ => 44
};

class Base { public int Prop { get; set; } }
class A : Base { }
class B : Base { }
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,40): warning CS9268: The pattern is redundant.
                // _ = o is Base { Prop: 10 } or not A or B; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "B").WithLocation(2, 40),
                // (8,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     B => 43, // 2
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "B").WithLocation(8, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_BaseTypeOrType()
        {
            var source = """
object o = null;
_ = o is object or string; // 1

_ = o switch
{
    object => 41,
    string => 42, // 2
    _ => 43
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,20): warning CS9268: The pattern is redundant.
                // _ = o is object or string; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "string").WithLocation(2, 20),
                // (7,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     string => 42, // 2
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "string").WithLocation(7, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_TypeOrBaseType()
        {
            var source = """
object o = null;
_ = o is string or object;

_ = o switch
{
    string => 41,
    object => 42,
    _ => 43
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotOr()
        {
            var source = """
object o = null;
_ = o is (not (null or 42)) or string; // 1
_ = o is (not null and not 42) or string; // 2
_ = o is (not (not null or 42)) or string; // 3
_ = o is (null and not 42) or string; // 4

_ = o switch
{
    not (null or 42) => 41,
    string => 42, // 5
    _ => 43
};
_ = o switch
{
    not null and not 42 => 41,
    string => 42, // 6
    _ => 43
};
_ = o switch
{
    not (not null or 42) => 41,
    string => 42,
    _ => 43
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,32): warning CS9268: The pattern is redundant.
                // _ = o is (not (null or 42)) or string; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "string").WithLocation(2, 32),
                // (3,35): warning CS9268: The pattern is redundant.
                // _ = o is (not null and not 42) or string; // 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "string").WithLocation(3, 35),
                // (4,28): warning CS9268: The pattern is redundant.
                // _ = o is (not (not null or 42)) or string; // 3
                Diagnostic(ErrorCode.WRN_RedundantPattern, "42").WithLocation(4, 28),
                // (5,24): warning CS9268: The pattern is redundant.
                // _ = o is (null and not 42) or string; // 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "42").WithLocation(5, 24),
                // (10,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     string => 42, // 5
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "string").WithLocation(10, 5),
                // (16,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     string => 42, // 6
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "string").WithLocation(16, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_NotAnd()
        {
            var source = """
object o = null;
_ = o is (not (not null and 42)) or string; // 1
_ = o is null or not 42 or string; // 2
_ = o is not ((not null and 42) or string);
_ = o is null or not 42 or string; // 3

_ = o is (not (int and int)) or string; // 4, 5
_ = o is not ((int and int) or string); // 6

_ = o is (not int) or string; // 7
_ = o is not (int or string);

_ = o switch
{
    not (not null and 42) => 41,
    string => 42, // 8
    _ => 43
};

_ = o switch
{
    null => 40,
    not 42 => 41,
    string => 42, // 9
    _ => 43
};

_ = o switch
{
    not (int and int) => 41,
    string => 42, // 10
    _ => 43
};

_ = o switch
{
    not int => 41,
    string => 42, // 11
    _ => 43
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,37): warning CS9268: The pattern is redundant.
                // _ = o is (not (not null and 42)) or string; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "string").WithLocation(2, 37),
                // (3,28): warning CS9268: The pattern is redundant.
                // _ = o is null or not 42 or string; // 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "string").WithLocation(3, 28),
                // (5,28): warning CS9268: The pattern is redundant.
                // _ = o is null or not 42 or string; // 3
                Diagnostic(ErrorCode.WRN_RedundantPattern, "string").WithLocation(5, 28),
                // (7,24): warning CS9268: The pattern is redundant.
                // _ = o is (not (int and int)) or string; // 4, 5
                Diagnostic(ErrorCode.WRN_RedundantPattern, "int").WithLocation(7, 24),
                // (7,33): warning CS9268: The pattern is redundant.
                // _ = o is (not (int and int)) or string; // 4, 5
                Diagnostic(ErrorCode.WRN_RedundantPattern, "string").WithLocation(7, 33),
                // (8,24): warning CS9268: The pattern is redundant.
                // _ = o is not ((int and int) or string); // 6
                Diagnostic(ErrorCode.WRN_RedundantPattern, "int").WithLocation(8, 24),
                // (10,23): warning CS9268: The pattern is redundant.
                // _ = o is (not int) or string; // 7
                Diagnostic(ErrorCode.WRN_RedundantPattern, "string").WithLocation(10, 23),
                // (16,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     string => 42, // 8
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "string").WithLocation(16, 5),
                // (24,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     string => 42, // 9
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "string").WithLocation(24, 5),
                // (31,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     string => 42, // 10
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "string").WithLocation(31, 5),
                // (38,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     string => 42, // 11
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "string").WithLocation(38, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_InSwitch()
        {
            var source = """
object o = null;
_ = o switch
{
    string { Length: 0 } => 41,
    not int or string => 42, // 1
    _ => 43
};

_ = o switch
{
    string { Length: 0 } => 41,
    not string or int => 42, // 2
    _ => 43
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,16): warning CS9268: The pattern is redundant.
                //     not int or string => 42, // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "string").WithLocation(5, 16),
                // (12,19): warning CS9268: The pattern is redundant.
                //     not string or int => 42, // 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "int").WithLocation(12, 19));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_DisallowDesignatorsUnderNotAndOr()
        {
            var source = """
class C
{
    void M(object o, C c)
    {
        if (o is not (1 and int)) { } // 1
        if (o is (not 1) or (not int)) { } // 2
        if (o is not (1 and int x1)) { }
        if (o is not (1 and not int x2)) { } // 3, 4
        if (o is not (1 and not not int x3)) { } // 5
        if (o is (string or int) and var x4) { }
        if (o is (C or Derived) and var x5) { } // 6
        if (o is (Derived or C) and var x6) { }

        if (c is not (Derived and (var y1, var y2))) { } else { y1.ToString(); }
        if (c is not (Derived and null)) { } else { y1.ToString(); } // 7
        if (c is not Derived or not (var z1, var z2)) { } else { z1.ToString(); } // 8, 9, 10

        _ = c switch
        {
            not Derived => 41,
            not (var w1, var w2) => 42, // 11, 12, 13
            _ => 43
        };

        if (c is not (Derived and (int s1, int s2))) { } else { s1.ToString(); }
        if (c is (not Derived or not (int t1, int t2))) { } else { t1.ToString(); } // 14, 15, 16, 17

        _ = c switch
        {
            not Derived => 41,
            not (int u1, int u2) => 42, // 18, 19, 20
            _ => 43
        };

        if (o is (1 or 2) or int v1) { } // 21
    }

    public void Deconstruct(out int i, out int j) => throw null;
}
class Derived : C { }
""";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (5,29): warning CS9268: The pattern is redundant.
                //         if (o is not (1 and int)) { } // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "int").WithLocation(5, 29),
                // (6,34): warning CS9268: The pattern is redundant.
                //         if (o is (not 1) or (not int)) { } // 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "int").WithLocation(6, 34),
                // (8,13): warning CS8794: An expression of type 'object' always matches the provided pattern.
                //         if (o is not (1 and not int x2)) { } // 3, 4
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "o is not (1 and not int x2)").WithArguments("object").WithLocation(8, 13),
                // (8,37): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (o is not (1 and not int x2)) { } // 3, 4
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "x2").WithLocation(8, 37),
                // (9,41): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (o is not (1 and not not int x3)) { } // 5
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "x3").WithLocation(9, 41),
                // (11,24): warning CS9268: The pattern is redundant.
                //         if (o is (C or Derived) and var x5) { } // 6
                Diagnostic(ErrorCode.WRN_RedundantPattern, "Derived").WithLocation(11, 24),
                // (15,13): warning CS8794: An expression of type 'C' always matches the provided pattern.
                //         if (c is not (Derived and null)) { } else { y1.ToString(); } // 7
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "c is not (Derived and null)").WithArguments("C").WithLocation(15, 13),
                // (16,42): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (c is not Derived or not (var z1, var z2)) { } else { z1.ToString(); } // 8, 9, 10
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "z1").WithLocation(16, 42),
                // (16,50): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (c is not Derived or not (var z1, var z2)) { } else { z1.ToString(); } // 8, 9, 10
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "z2").WithLocation(16, 50),
                // (16,66): error CS0165: Use of unassigned local variable 'z1'
                //         if (c is not Derived or not (var z1, var z2)) { } else { z1.ToString(); } // 8, 9, 10
                Diagnostic(ErrorCode.ERR_UseDefViolation, "z1").WithArguments("z1").WithLocation(16, 66),
                // (21,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             not (var w1, var w2) => 42, // 11, 12, 13
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "not (var w1, var w2)").WithLocation(21, 13),
                // (21,22): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //             not (var w1, var w2) => 42, // 11, 12, 13
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "w1").WithLocation(21, 22),
                // (21,30): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //             not (var w1, var w2) => 42, // 11, 12, 13
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "w2").WithLocation(21, 30),
                // (26,43): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (c is (not Derived or not (int t1, int t2))) { } else { t1.ToString(); } // 14, 15, 16, 17
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "t1").WithLocation(26, 43),
                // (26,51): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (c is (not Derived or not (int t1, int t2))) { } else { t1.ToString(); } // 14, 15, 16, 17
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "t2").WithLocation(26, 51),
                // (26,68): error CS0165: Use of unassigned local variable 't1'
                //         if (c is (not Derived or not (int t1, int t2))) { } else { t1.ToString(); } // 14, 15, 16, 17
                Diagnostic(ErrorCode.ERR_UseDefViolation, "t1").WithArguments("t1").WithLocation(26, 68),
                // (31,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //             not (int u1, int u2) => 42, // 18, 19, 20
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "not (int u1, int u2)").WithLocation(31, 13),
                // (31,22): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //             not (int u1, int u2) => 42, // 18, 19, 20
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "u1").WithLocation(31, 22),
                // (31,30): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //             not (int u1, int u2) => 42, // 18, 19, 20
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "u2").WithLocation(31, 30),
                // (35,34): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (o is (1 or 2) or int v1) { } // 21
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "v1").WithLocation(35, 34));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_LeftBranches()
        {
            var source = """
class C
{
    void M(object o)
    {
        if (o is (1 or 2) or int v1) { } // 1
        if (o is ((1 or 2) or 3) or int v2) { } // 2
        if (o is (1 or (2 or 3)) or int v3) { } // 3
    }
}
""";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (5,34): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (o is (1 or 2) or int v1) { } // 1
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "v1").WithLocation(5, 34),
                // (6,41): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (o is ((1 or 2) or 3) or int v2) { } // 2
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "v2").WithLocation(6, 41),
                // (7,41): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (o is (1 or (2 or 3)) or int v3) { } // 3
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "v3").WithLocation(7, 41));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_DifferentOrSequences()
        {
            var source = """
class C
{
    void M(S s)
    {
        if (s is { Prop1: 42 or 43 } or { Prop2: 44 or 45 }) { }
        if (s is { Prop1: not 42 or 43 } or { Prop2: not 44 or 45 }) { } // 1, 2
        if (s is { Prop1: (not 42 or 43) or 44 } or { Prop2: (not 45 or 45) or 46 }) { } // 3, 4, 5, 6
        if (s is { Prop1: (42 or (not 43 or 44)) or 45 } or { Prop2: (46 or (not 44 or 45)) or 46 }) { } // 7, 8, 9, 10

        if (s is ({ Prop1: not 42 or 43 } or { Prop2: not 44 or 45 }) or { Prop3: not 46 or 47 }) { } // 11, 12, 13
        if (s is ({ Prop0: not 42 or 43 } or ({ Prop1: not 42 or 43 } or { Prop2: not 44 or 45 })) or { Prop3: not 46 or 47 }) { } // 14, 15, 16, 17
    }
}

public struct S
{
    public int Prop0 { get; set; }
    public int Prop1 { get; set; }
    public int Prop2 { get; set; }
    public int Prop3 { get; set; }
}
""";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,37): warning CS9268: The pattern is redundant.
                //         if (s is { Prop1: not 42 or 43 } or { Prop2: not 44 or 45 }) { } // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(6, 37),
                // (6,64): warning CS9268: The pattern is redundant.
                //         if (s is { Prop1: not 42 or 43 } or { Prop2: not 44 or 45 }) { } // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(6, 64),
                // (7,13): warning CS8794: An expression of type 'S' always matches the provided pattern.
                //         if (s is { Prop1: (not 42 or 43) or 44 } or { Prop2: (not 45 or 45) or 46 }) { } // 3, 4, 5, 6
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "s is { Prop1: (not 42 or 43) or 44 } or { Prop2: (not 45 or 45) or 46 }").WithArguments("S").WithLocation(7, 13),
                // (7,38): warning CS9268: The pattern is redundant.
                //         if (s is { Prop1: (not 42 or 43) or 44 } or { Prop2: (not 45 or 45) or 46 }) { } // 3, 4, 5, 6
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(7, 38),
                // (7,45): warning CS9268: The pattern is redundant.
                //         if (s is { Prop1: (not 42 or 43) or 44 } or { Prop2: (not 45 or 45) or 46 }) { } // 3, 4, 5, 6
                Diagnostic(ErrorCode.WRN_RedundantPattern, "44").WithLocation(7, 45),
                // (7,80): warning CS9268: The pattern is redundant.
                //         if (s is { Prop1: (not 42 or 43) or 44 } or { Prop2: (not 45 or 45) or 46 }) { } // 3, 4, 5, 6
                Diagnostic(ErrorCode.WRN_RedundantPattern, "46").WithLocation(7, 80),
                // (8,45): warning CS9268: The pattern is redundant.
                //         if (s is { Prop1: (42 or (not 43 or 44)) or 45 } or { Prop2: (46 or (not 44 or 45)) or 46 }) { } // 7, 8, 9, 10
                Diagnostic(ErrorCode.WRN_RedundantPattern, "44").WithLocation(8, 45),
                // (8,53): warning CS9268: The pattern is redundant.
                //         if (s is { Prop1: (42 or (not 43 or 44)) or 45 } or { Prop2: (46 or (not 44 or 45)) or 46 }) { } // 7, 8, 9, 10
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(8, 53),
                // (8,88): warning CS9268: The pattern is redundant.
                //         if (s is { Prop1: (42 or (not 43 or 44)) or 45 } or { Prop2: (46 or (not 44 or 45)) or 46 }) { } // 7, 8, 9, 10
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(8, 88),
                // (8,96): warning CS9268: The pattern is redundant.
                //         if (s is { Prop1: (42 or (not 43 or 44)) or 45 } or { Prop2: (46 or (not 44 or 45)) or 46 }) { } // 7, 8, 9, 10
                Diagnostic(ErrorCode.WRN_RedundantPattern, "46").WithLocation(8, 96),
                // (10,38): warning CS9268: The pattern is redundant.
                //         if (s is ({ Prop1: not 42 or 43 } or { Prop2: not 44 or 45 }) or { Prop3: not 46 or 47 }) { } // 11, 12, 13
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(10, 38),
                // (10,65): warning CS9268: The pattern is redundant.
                //         if (s is ({ Prop1: not 42 or 43 } or { Prop2: not 44 or 45 }) or { Prop3: not 46 or 47 }) { } // 11, 12, 13
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(10, 65),
                // (10,93): warning CS9268: The pattern is redundant.
                //         if (s is ({ Prop1: not 42 or 43 } or { Prop2: not 44 or 45 }) or { Prop3: not 46 or 47 }) { } // 11, 12, 13
                Diagnostic(ErrorCode.WRN_RedundantPattern, "47").WithLocation(10, 93),
                // (11,38): warning CS9268: The pattern is redundant.
                //         if (s is ({ Prop0: not 42 or 43 } or ({ Prop1: not 42 or 43 } or { Prop2: not 44 or 45 })) or { Prop3: not 46 or 47 }) { } // 14, 15, 16, 17
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(11, 38),
                // (11,66): warning CS9268: The pattern is redundant.
                //         if (s is ({ Prop0: not 42 or 43 } or ({ Prop1: not 42 or 43 } or { Prop2: not 44 or 45 })) or { Prop3: not 46 or 47 }) { } // 14, 15, 16, 17
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(11, 66),
                // (11,93): warning CS9268: The pattern is redundant.
                //         if (s is ({ Prop0: not 42 or 43 } or ({ Prop1: not 42 or 43 } or { Prop2: not 44 or 45 })) or { Prop3: not 46 or 47 }) { } // 14, 15, 16, 17
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(11, 93),
                // (11,122): warning CS9268: The pattern is redundant.
                //         if (s is ({ Prop0: not 42 or 43 } or ({ Prop1: not 42 or 43 } or { Prop2: not 44 or 45 })) or { Prop3: not 46 or 47 }) { } // 14, 15, 16, 17
                Diagnostic(ErrorCode.WRN_RedundantPattern, "47").WithLocation(11, 122));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_DifferentOrSequences_TODO2()
        {
            var source = """
class C
{
    void M(Container c)
    {
        if (c is { PropA: { Prop0: not 42 or 43 } or { Prop1: not 44 or 45 } } // 1, 2
              or { PropB: { Prop2: not 46 or 47 } or { Prop3: not 48 or 49 } }) { } // 3, 4
    }
}
public struct Container
{
    public S PropA { get; set; }
    public S PropB { get; set; }
}
public struct S
{
    public int Prop0 { get; set; }
    public int Prop1 { get; set; }
    public int Prop2 { get; set; }
    public int Prop3 { get; set; }
}
""";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (5,46): warning CS9268: The pattern is redundant.
                //         if (c is { PropA: { Prop0: not 42 or 43 } or { Prop1: not 44 or 45 } } // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(5, 46),
                // (5,73): warning CS9268: The pattern is redundant.
                //         if (c is { PropA: { Prop0: not 42 or 43 } or { Prop1: not 44 or 45 } } // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(5, 73),
                // (6,46): warning CS9268: The pattern is redundant.
                //               or { PropB: { Prop2: not 46 or 47 } or { Prop3: not 48 or 49 } }) { } // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "47").WithLocation(6, 46),
                // (6,73): warning CS9268: The pattern is redundant.
                //               or { PropB: { Prop2: not 46 or 47 } or { Prop3: not 48 or 49 } }) { } // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "49").WithLocation(6, 73));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_TODO2()
        {
            var source = """
object o = null;
_ = o is A or B;
_ = o is not (not A and B);
_ = o is not (A or B);
_ = o is not (A and not B); // 1

class A { }
class B { }
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,25): error CS8121: An expression of type 'A' cannot be handled by a pattern of type 'B'.
                // _ = o is not (A and not B); // 1
                Diagnostic(ErrorCode.ERR_PatternWrongType, "B").WithArguments("A", "B").WithLocation(5, 25));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_TODO2_2()
        {
            var source = """
object o = null;
_ = o is A or B x1; // 1
_ = o is not A or B x2; // 2, 3

_ = o switch
{
    not A => 1,
    B y2 => 2, // 4
    _ => 3
};

_ = o is not (not A and B x3);
_ = o is not (A or B x4); // 5
_ = o is not (A and not B x5); // 6, 7

class A { }
class B { }
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,25): error CS8121: An expression of type 'A' cannot be handled by a pattern of type 'B'.
                // _ = o is not (A and not B); // 1
                Diagnostic(ErrorCode.ERR_PatternWrongType, "B").WithArguments("A", "B").WithLocation(5, 25));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_RecursivePatterns_TypeAndDeconstruction()
        {
            var source = """
object o = null;
_ = o is not S (42 and not 43, 44 and not 45); // 1, 2
_ = o is S (not 42 or 43, not 44 or 45); // 3, 4

public class S
{
    public void Deconstruct(out int x, out int y) => throw null;
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,28): warning CS9268: The pattern is redundant.
                // _ = o is not S (42 and not 43, 44 and not 45); // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(2, 28),
                // (2,43): warning CS9268: The pattern is redundant.
                // _ = o is not S (42 and not 43, 44 and not 45); // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(2, 43),
                // (3,23): warning CS9268: The pattern is redundant.
                // _ = o is S (not 42 or 43, not 44 or 45); // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(3, 23),
                // (3,37): warning CS9268: The pattern is redundant.
                // _ = o is S (not 42 or 43, not 44 or 45); // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(3, 37));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_RecursivePatterns_TypeAndProperties()
        {
            var source = """
object o = null;
_ = o is not S { Prop1: 42 and not 43, Prop2: 44 and not 45 }; // 1, 2
_ = o is S { Prop1: not 42 or 43, Prop2: not 44 or 45 }; // 3, 4

public class S
{
    public int Prop1 { get; set; }
    public int Prop2 { get; set; }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,36): warning CS9268: The pattern is redundant.
                // _ = o is not S { Prop1: 42 and not 43, Prop2: 44 and not 45 }; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(2, 36),
                // (2,58): warning CS9268: The pattern is redundant.
                // _ = o is not S { Prop1: 42 and not 43, Prop2: 44 and not 45 }; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(2, 58),
                // (3,31): warning CS9268: The pattern is redundant.
                // _ = o is S { Prop1: not 42 or 43, Prop2: not 44 or 45 }; // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(3, 31),
                // (3,52): warning CS9268: The pattern is redundant.
                // _ = o is S { Prop1: not 42 or 43, Prop2: not 44 or 45 }; // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(3, 52));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_RecursivePatterns_Properties()
        {
            var source = """
S s = null;
_ = s is not { Prop1: 42 and not 43, Prop2: 44 and not 45 }; // 1, 2
_ = s is { Prop1: not 42 or 43, Prop2: not 44 or 45 }; // 3, 4

public class S
{
    public int Prop1 { get; set; }
    public int Prop2 { get; set; }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,34): warning CS9268: The pattern is redundant.
                // _ = s is not { Prop1: 42 and not 43, Prop2: 44 and not 45 }; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(2, 34),
                // (2,56): warning CS9268: The pattern is redundant.
                // _ = s is not { Prop1: 42 and not 43, Prop2: 44 and not 45 }; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(2, 56),
                // (3,29): warning CS9268: The pattern is redundant.
                // _ = s is { Prop1: not 42 or 43, Prop2: not 44 or 45 }; // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(3, 29),
                // (3,50): warning CS9268: The pattern is redundant.
                // _ = s is { Prop1: not 42 or 43, Prop2: not 44 or 45 }; // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(3, 50));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_RecursivePatterns_EmptyVsNull()
        {
            var source = """
S s = null;
_ = s is not { } or null; // 1
_ = s is not ({ } and not null); // 2
_ = s is { } or not null; // 3

public class S
{
    public int Prop1 { get; set; }
    public int Prop2 { get; set; }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,21): warning CS9268: The pattern is redundant.
                // _ = s is not { } or null; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "null").WithLocation(2, 21),
                // (3,27): warning CS9268: The pattern is redundant.
                // _ = s is not ({ } and not null); // 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "null").WithLocation(3, 27),
                // (4,21): warning CS9268: The pattern is redundant.
                // _ = s is { } or not null; // 3
                Diagnostic(ErrorCode.WRN_RedundantPattern, "null").WithLocation(4, 21));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_RecursivePatterns_AlwaysTrue()
        {
            var source = """
object o = null;
_ = o is not (string and var x0);
_ = o is not S { Prop1: _, Prop2: var x1 };
_ = o is not S (42 and not 43, int x2); // 1
_ = o is not S { Prop1: 42 and not 43, Prop2: _ and var x3 }; // 2, 3

S s = default;
_ = s is not (_, int);
_ = s is not (int, int);
_ = s is not { Prop1: _ }; // 4

_ = s is not (_, _); // 5
_ = s is not (var x4, var x5); // 6

public struct S
{
    public int Prop1 { get; set; }
    public int Prop2 { get; set; }
    public void Deconstruct(out object x, out object y) => throw null;
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,28): warning CS9268: The pattern is redundant.
                // _ = o is not S (42 and not 43, int x2); // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(4, 28),
                // (5,36): warning CS9268: The pattern is redundant.
                // _ = o is not S { Prop1: 42 and not 43, Prop2: _ and var x3 }; // 2, 3
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(5, 36),
                // (5,40): warning CS9268: The pattern is redundant.
                // _ = o is not S { Prop1: 42 and not 43, Prop2: _ and var x3 }; // 2, 3
                Diagnostic(ErrorCode.WRN_RedundantPattern, "Prop2: _ and var x3").WithLocation(5, 40),
                // (10,5): error CS8518: An expression of type 'S' can never match the provided pattern.
                // _ = s is not { Prop1: _ }; // 4
                Diagnostic(ErrorCode.ERR_IsPatternImpossible, "s is not { Prop1: _ }").WithArguments("S").WithLocation(10, 5),
                // (12,5): error CS8518: An expression of type 'S' can never match the provided pattern.
                // _ = s is not (_, _); // 5
                Diagnostic(ErrorCode.ERR_IsPatternImpossible, "s is not (_, _)").WithArguments("S").WithLocation(12, 5),
                // (13,5): error CS8518: An expression of type 'S' can never match the provided pattern.
                // _ = s is not (var x4, var x5); // 6
                Diagnostic(ErrorCode.ERR_IsPatternImpossible, "s is not (var x4, var x5)").WithArguments("S").WithLocation(13, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_RecursivePatterns_TwoPositionalPatternsWithAnd()
        {
            var source = """
object o = null;
_ = o is not (S and (42 and not 43, 44 and not 45)); // 1, 2
_ = o is not (S and (42 and not 43, _) and (_, 44 and not 45)); // 3, 4

public class S
{
    public void Deconstruct(out int x, out int y) => throw null;
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,33): warning CS9268: The pattern is redundant.
                // _ = o is not (S and (42 and not 43, 44 and not 45)); // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(2, 33),
                // (2,48): warning CS9268: The pattern is redundant.
                // _ = o is not (S and (42 and not 43, 44 and not 45)); // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(2, 48),
                // (3,33): warning CS9268: The pattern is redundant.
                // _ = o is not (S and (42 and not 43, _) and (_, 44 and not 45)); // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(3, 33),
                // (3,59): warning CS9268: The pattern is redundant.
                // _ = o is not (S and (42 and not 43, _) and (_, 44 and not 45)); // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(3, 59));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_RecursivePatterns_TwoPropertyPatternsWithAnd()
        {
            var source = """
object o = null;
_ = o is not (S and { Prop1: 42 and not 43, Prop2: 44 and not 45 }); // 1, 2
_ = o is not (S and { Prop1: 42 and not 43 } and { Prop2: 44 and not 45 }); // 3, 4
_ = o is not (S and { } and { Prop2: 44 and not 45 }); // 5
_ = o is not (S and { Prop1: 42 and not 43 } and { }); // 6

public class S
{
    public int Prop1 { get; set; }
    public int Prop2 { get; set; }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,41): warning CS9268: The pattern is redundant.
                // _ = o is not (S and { Prop1: 42 and not 43, Prop2: 44 and not 45 }); // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(2, 41),
                // (2,63): warning CS9268: The pattern is redundant.
                // _ = o is not (S and { Prop1: 42 and not 43, Prop2: 44 and not 45 }); // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(2, 63),
                // (3,41): warning CS9268: The pattern is redundant.
                // _ = o is not (S and { Prop1: 42 and not 43 } and { Prop2: 44 and not 45 }); // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(3, 41),
                // (3,70): warning CS9268: The pattern is redundant.
                // _ = o is not (S and { Prop1: 42 and not 43 } and { Prop2: 44 and not 45 }); // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(3, 70),
                // (4,49): warning CS9268: The pattern is redundant.
                // _ = o is not (S and { } and { Prop2: 44 and not 45 }); // 5
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(4, 49),
                // (5,41): warning CS9268: The pattern is redundant.
                // _ = o is not (S and { Prop1: 42 and not 43 } and { }); // 6
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(5, 41));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_RecursivePatterns_DeconstructOnDerived()
        {
            // TODO2 resume here
            // TODO2 why aren't we reporting the `C` patterns as redundant?
            var source = """
object o = null;
_ = o is C and (int, int); // 1, 2
_ = o is C and (int x, int y);

C c = null;
_ = c is C and (int, int); // 3, 4
_ = c is C and (int x2, int y2);

_ = c is (int, int); // 5, 6
_ = c is (int x3, int y3);

public class C
{
    public void Deconstruct(out int x, out int y) => throw null;
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,17): warning CS9268: The pattern is redundant.
                // _ = o is C and (int, int); // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "int").WithLocation(2, 17),
                // (2,22): warning CS9268: The pattern is redundant.
                // _ = o is C and (int, int); // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "int").WithLocation(2, 22),
                // (6,17): warning CS9268: The pattern is redundant.
                // _ = c is C and (int, int); // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "int").WithLocation(6, 17),
                // (6,22): warning CS9268: The pattern is redundant.
                // _ = c is C and (int, int); // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "int").WithLocation(6, 22),
                // (9,11): warning CS9268: The pattern is redundant.
                // _ = c is (int, int); // 5, 6
                Diagnostic(ErrorCode.WRN_RedundantPattern, "int").WithLocation(9, 11),
                // (9,16): warning CS9268: The pattern is redundant.
                // _ = c is (int, int); // 5, 6
                Diagnostic(ErrorCode.WRN_RedundantPattern, "int").WithLocation(9, 16));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_ListPatterns()
        {
            var source = """
int[] o = null;
_ = o is not [42 and not 43, 44 and not 45]; // 1, 2
_ = o is [not 42 or 43, not 44 or 45]; // 3, 4
_ = o is not [_, _];
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (2,26): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, 44 and not 45]; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(2, 26),
                // (2,41): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, 44 and not 45]; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(2, 41),
                // (3,21): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, not 44 or 45]; // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(3, 21),
                // (3,35): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, not 44 or 45]; // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(3, 35));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_ListPatterns_TwoWithAnds()
        {
            var source = """
int[] o = null;
_ = o is not [42 and not 43, 44 and not 45]; // 1, 2
_ = o is not ([42 and not 43, _] and [_, 44 and not 45]); // 3, 4
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (2,26): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, 44 and not 45]; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(2, 26),
                // (2,41): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, 44 and not 45]; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(2, 41),
                // (3,27): warning CS9268: The pattern is redundant.
                // _ = o is not ([42 and not 43, _] and [_, 44 and not 45]); // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(3, 27),
                // (3,53): warning CS9268: The pattern is redundant.
                // _ = o is not ([42 and not 43, _] and [_, 44 and not 45]); // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(3, 53));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_SlicePatterns_Empty()
        {
            var source = """
S o = default;
_ = o is not [42 and not 43, 44 and not 45, ..]; // 1, 2
_ = o is [not 42 or 43, not 44 or 45, ..]; // 3, 4

_ = o is not [42 and not 43, .., 44 and not 45]; // 5, 6
_ = o is [not 42 or 43, .., not 44 or 45]; // 7, 8

_ = o is not [42 and not 43, .., 44 and not 45, ..]; // 9

public struct S
{
    public int Length => throw null;
    public int this[System.Index i] => throw null;
    public int this[System.Range r] => throw null;
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (2,26): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, 44 and not 45, ..]; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(2, 26),
                // (2,41): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, 44 and not 45, ..]; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(2, 41),
                // (3,21): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, not 44 or 45, ..]; // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(3, 21),
                // (3,35): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, not 44 or 45, ..]; // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(3, 35),
                // (5,26): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, .., 44 and not 45]; // 5, 6
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(5, 26),
                // (5,45): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, .., 44 and not 45]; // 5, 6
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(5, 45),
                // (6,21): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, .., not 44 or 45]; // 7, 8
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(6, 21),
                // (6,39): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, .., not 44 or 45]; // 7, 8
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(6, 39),
                // (8,49): error CS8980: Slice patterns may only be used once and directly inside a list pattern.
                // _ = o is not [42 and not 43, .., 44 and not 45, ..]; // 9
                Diagnostic(ErrorCode.ERR_MisplacedSlicePattern, "..").WithLocation(8, 49));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_SlicePatterns_AlwaysTrue()
        {
            var source = """
S o = default;
_ = o is not [42 and not 43, .._]; // 1
_ = o is [not 42 or 43, .._]; // 2
_ = o is not [42 and not 43, ..(_ or _)]; // 3
_ = o is not [42 and not 43, ..(_ and _)]; // 4

_ = o is not [42 and not 43, ..var x]; // 5
_ = o is [not 42 or 43, ..var y]; // 6

_ = o is not [..var x2, 42 and not 43]; // 7
_ = o is [..var y2, not 42 or 43]; // 8

_ = o is not [42 and not 43, ..var z or var t]; // 9, 10, 11

public struct S
{
    public int Length => throw null;
    public int this[System.Index i] => throw null;
    public int this[System.Range r] => throw null;
}
""";
            // TODO2 there is some noise on 3 and 4
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (2,26): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, .._]; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(2, 26),
                // (3,21): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, .._]; // 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(3, 21),
                // (4,26): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, ..(_ or _)]; // 3
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(4, 26),
                // (4,30): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, ..(_ or _)]; // 3
                Diagnostic(ErrorCode.WRN_RedundantPattern, "..(_ or _)").WithLocation(4, 30),
                // (4,38): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, ..(_ or _)]; // 3
                Diagnostic(ErrorCode.WRN_RedundantPattern, "_").WithLocation(4, 38),
                // (5,26): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, ..(_ and _)]; // 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(5, 26),
                // (5,30): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, ..(_ and _)]; // 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "..(_ and _)").WithLocation(5, 30),
                // (5,39): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, ..(_ and _)]; // 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "_").WithLocation(5, 39),
                // (7,26): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, ..var x]; // 5
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(7, 26),
                // (8,21): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, ..var y]; // 6
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(8, 21),
                // (10,36): warning CS9268: The pattern is redundant.
                // _ = o is not [..var x2, 42 and not 43]; // 7
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(10, 36),
                // (11,31): warning CS9268: The pattern is redundant.
                // _ = o is [..var y2, not 42 or 43]; // 8
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(11, 31),
                // (13,26): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, ..var z or var t]; // 9, 10, 11
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(13, 26),
                // (13,30): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, ..var z or var t]; // 9, 10, 11
                Diagnostic(ErrorCode.WRN_RedundantPattern, "..var z or var t").WithLocation(13, 30),
                // (13,36): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                // _ = o is not [42 and not 43, ..var z or var t]; // 9, 10, 11
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "z").WithLocation(13, 36),
                // (13,45): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                // _ = o is not [42 and not 43, ..var z or var t]; // 9, 10, 11
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "t").WithLocation(13, 45));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_ListPatterns_Nested()
        {
            var source = """
S s = default;
_ = s is not [..[42]];
_ = s is not [..[42 and not 43]]; // 1
_ = s is [..[not 42 or 43]]; // 2
_ = s is not [..[42 and not 43]]; // 3
_ = s is not [..[42 and not 43]]; // 4

_ = s is not [..[42 and not 43, ..var x]]; // 5
_ = s is [..[not 42 or 43]]; // 6

_ = s is not [..[42 and not 43]]; // 7
_ = s is [.. [not 42 or 43]]; // 8

public struct S
{
    public int Length => throw null;
    public int this[System.Index i] => throw null;
    public S this[System.Range r] => throw null;
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (3,29): warning CS9268: The pattern is redundant.
                // _ = s is not [..[42 and not 43]]; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(3, 29),
                // (4,24): warning CS9268: The pattern is redundant.
                // _ = s is [..[not 42 or 43]]; // 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(4, 24),
                // (5,29): warning CS9268: The pattern is redundant.
                // _ = s is not [..[42 and not 43]]; // 3
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(5, 29),
                // (6,29): warning CS9268: The pattern is redundant.
                // _ = s is not [..[42 and not 43]]; // 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(6, 29),
                // (8,29): warning CS9268: The pattern is redundant.
                // _ = s is not [..[42 and not 43, ..var x]]; // 5
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(8, 29),
                // (9,24): warning CS9268: The pattern is redundant.
                // _ = s is [..[not 42 or 43]]; // 6
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(9, 24),
                // (11,29): warning CS9268: The pattern is redundant.
                // _ = s is not [..[42 and not 43]]; // 7
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(11, 29),
                // (12,25): warning CS9268: The pattern is redundant.
                // _ = s is [.. [not 42 or 43]]; // 8
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(12, 25));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_ListPatterns_AlwaysTrue()
        {
            var source = """
S o = default;
_ = o is not [42 and not 43, _]; // 1
_ = o is [not 42 or 43, _]; // 2

_ = o is not [42 and not 43, var x]; // 3
_ = o is [not 42 or 43, var y]; // 4

_ = o is not [var x2, 42 and not 43]; // 5
_ = o is [var y2, not 42 or 43]; // 6

_ = o is not [42 and not 43, _ and var x3]; // 7, 8
_ = o is [not 42 or 43, _ and var y3]; // 9, 10

_ = o is not [42 and not 43, var x4 and _]; // 11, 12, 13
_ = o is [not 42 or 43, var y4 and _]; // 14, 15, 16

_ = o is [not 42 or 43, _ and int y5]; // 17, 18
_ = o is [not 42 or 43, var y6]; // 19
_ = o is [not 42 or 43, int y7]; // 20

_ = o is not [42 and not 43, int and var y8]; // 21, 22

public struct S
{
    public int Length => throw null;
    public int this[System.Index i] => throw null;
    public int this[System.Range r] => throw null;
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (2,26): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, _]; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(2, 26),
                // (3,21): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, _]; // 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(3, 21),
                // (5,26): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, var x]; // 3
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(5, 26),
                // (6,21): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, var y]; // 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(6, 21),
                // (8,34): warning CS9268: The pattern is redundant.
                // _ = o is not [var x2, 42 and not 43]; // 5
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(8, 34),
                // (9,29): warning CS9268: The pattern is redundant.
                // _ = o is [var y2, not 42 or 43]; // 6
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(9, 29),
                // (11,26): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, _ and var x3]; // 7, 8
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(11, 26),
                // (11,30): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, _ and var x3]; // 7, 8
                Diagnostic(ErrorCode.WRN_RedundantPattern, "_ and var x3").WithLocation(11, 30),
                // (12,21): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, _ and var y3]; // 9, 10
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(12, 21),
                // (12,25): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, _ and var y3]; // 9, 10
                Diagnostic(ErrorCode.WRN_RedundantPattern, "_ and var y3").WithLocation(12, 25),
                // (14,26): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, var x4 and _]; // 11, 12, 13
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(14, 26),
                // (14,30): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, var x4 and _]; // 11, 12, 13
                Diagnostic(ErrorCode.WRN_RedundantPattern, "var x4 and _").WithLocation(14, 30),
                // (14,41): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, var x4 and _]; // 11, 12, 13
                Diagnostic(ErrorCode.WRN_RedundantPattern, "_").WithLocation(14, 41),
                // (15,21): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, var y4 and _]; // 14, 15, 16
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(15, 21),
                // (15,25): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, var y4 and _]; // 14, 15, 16
                Diagnostic(ErrorCode.WRN_RedundantPattern, "var y4 and _").WithLocation(15, 25),
                // (15,36): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, var y4 and _]; // 14, 15, 16
                Diagnostic(ErrorCode.WRN_RedundantPattern, "_").WithLocation(15, 36),
                // (17,21): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, _ and int y5]; // 17, 18
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(17, 21),
                // (17,25): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, _ and int y5]; // 17, 18
                Diagnostic(ErrorCode.WRN_RedundantPattern, "_ and int y5").WithLocation(17, 25),
                // (18,21): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, var y6]; // 19
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(18, 21),
                // (19,21): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, int y7]; // 20
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(19, 21),
                // (21,26): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, int and var y8]; // 21, 22
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(21, 26),
                // (21,30): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, int and var y8]; // 21, 22
                Diagnostic(ErrorCode.WRN_RedundantPattern, "int and var y8").WithLocation(21, 30));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_SlicePatterns_WithRedundancy()
        {
            var source = """
S o = default;
_ = o is not [42 and not 43, _, ..44 and not 45]; // 1, 2
_ = o is [not 42 or 43, _, ..not 44 or 45]; // 3, 4

_ = o is not [42 and not 43, var x, ..44 and not 45]; // 5, 6
_ = o is [not 42 or 43, var y, ..not 44 or 45]; // 7, 8

public struct S
{
    public int Length => throw null;
    public int this[System.Index i] => throw null;
    public int this[System.Range r] => throw null;
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (2,26): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, _, ..44 and not 45]; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(2, 26),
                // (2,46): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, _, ..44 and not 45]; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(2, 46),
                // (3,21): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, _, ..not 44 or 45]; // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(3, 21),
                // (3,40): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, _, ..not 44 or 45]; // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(3, 40),
                // (5,26): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, var x, ..44 and not 45]; // 5, 6
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(5, 26),
                // (5,50): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, var x, ..44 and not 45]; // 5, 6
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(5, 50),
                // (6,21): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, var y, ..not 44 or 45]; // 7, 8
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(6, 21),
                // (6,44): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, var y, ..not 44 or 45]; // 7, 8
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(6, 44));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_TODO2_14()
        {
            var source = """
S o = default;
_ = o is not (42 and not 43 and var x); // 1, 2
_ = o is [not 42 or 43, _, ..not 44 or 45]; // 3, 4

_ = o is not [42 and not 43, var x, ..44 and not 45]; // 5, 6, TODO2
_ = o is [not 42 or 43, var y, ..not 44 or 45]; // 7, 8

public struct S
{
    public int Length => throw null;
    public int this[System.Index i] => throw null;
    public int this[System.Range r] => throw null;
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (2,26): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, _, ..44 and not 45]; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(2, 26),
                // (2,46): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, _, ..44 and not 45]; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(2, 46),
                // (3,21): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, _, ..not 44 or 45]; // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(3, 21),
                // (3,40): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, _, ..not 44 or 45]; // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(3, 40),
                // (5,14): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, var x, ..44 and not 45]; // 5, 6
                Diagnostic(ErrorCode.WRN_RedundantPattern, "[42 and not 43, var x, ..44 and not 45]").WithLocation(5, 14),
                // (5,26): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, var x, ..44 and not 45]; // 5, 6
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(5, 26),
                // (5,50): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, var x, ..44 and not 45]; // 5, 6
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(5, 50),
                // (6,21): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, var y, ..not 44 or 45]; // 7, 8
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(6, 21),
                // (6,44): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, var y, ..not 44 or 45]; // 7, 8
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(6, 44));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_TODO2_15()
        {
            var source = """
public class C
{
    public void M(S o)
    {
        _ = o switch
        {
            not { Length: 2 } => 1,
            [not 42, _] => 2,
            [_, not _] => 3, // TODO2
        };
    }
}

public struct S
{
    public int Length => throw null;
    public int this[System.Index i] => throw null;
    public int this[System.Range r] => throw null;
}
""";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (2,26): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, _, ..44 and not 45]; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(2, 26),
                // (2,46): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, _, ..44 and not 45]; // 1, 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(2, 46),
                // (3,21): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, _, ..not 44 or 45]; // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(3, 21),
                // (3,40): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, _, ..not 44 or 45]; // 3, 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(3, 40),
                // (5,14): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, var x, ..44 and not 45]; // 5, 6
                Diagnostic(ErrorCode.WRN_RedundantPattern, "[42 and not 43, var x, ..44 and not 45]").WithLocation(5, 14), // TODO2
                // (5,26): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, var x, ..44 and not 45]; // 5, 6
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(5, 26),
                // (5,50): warning CS9268: The pattern is redundant.
                // _ = o is not [42 and not 43, var x, ..44 and not 45]; // 5, 6
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(5, 50),
                // (6,21): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, var y, ..not 44 or 45]; // 7, 8
                Diagnostic(ErrorCode.WRN_RedundantPattern, "43").WithLocation(6, 21),
                // (6,44): warning CS9268: The pattern is redundant.
                // _ = o is [not 42 or 43, var y, ..not 44 or 45]; // 7, 8
                Diagnostic(ErrorCode.WRN_RedundantPattern, "45").WithLocation(6, 44));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_TODO2_17()
        {
            var source = """
object o = null;
_ = o is 1 or 2 or 1; // 1
_ = o is 1 or 2 or not 1; // 2

_ = o is not (1 or 2 or 1); // 3
_ = o is not (1 or 2 or not 1); // 4

_ = o switch
{
    1 => 0,
    2 => 0,
    1 => 0, // 5
    _ => 0,
};

_ = o switch
{
    1 => 0,
    2 => 0,
    not 1 => 0,
    _ => 0, // 6
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,20): warning CS9268: The pattern is redundant.
                // _ = o is 1 or 2 or 1; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "1").WithLocation(2, 20),
                // (3,5): warning CS8794: An expression of type 'object' always matches the provided pattern.
                // _ = o is 1 or 2 or not 1; // 2
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "o is 1 or 2 or not 1").WithArguments("object").WithLocation(3, 5),
                // (3,10): warning CS9268: The pattern is redundant.
                // _ = o is 1 or 2 or not 1; // 2
                Diagnostic(ErrorCode.WRN_RedundantPattern, "1 or 2 or not 1").WithLocation(3, 10),
                // (5,25): warning CS9268: The pattern is redundant.
                // _ = o is not (1 or 2 or 1); // 3
                Diagnostic(ErrorCode.WRN_RedundantPattern, "1").WithLocation(5, 25),
                // (6,5): error CS8518: An expression of type 'object' can never match the provided pattern.
                // _ = o is not (1 or 2 or not 1); // 4
                Diagnostic(ErrorCode.ERR_IsPatternImpossible, "o is not (1 or 2 or not 1)").WithArguments("object").WithLocation(6, 5),
                // (6,10): warning CS9268: The pattern is redundant.
                // _ = o is not (1 or 2 or not 1); // 4
                Diagnostic(ErrorCode.WRN_RedundantPattern, "not (1 or 2 or not 1)").WithLocation(6, 10),
                // (12,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     1 => 0, // 5
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "1").WithLocation(12, 5),
                // (21,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     _ => 0, // 6
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "_").WithLocation(21, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75506")]
        public void RedundantPattern_TODO2_18()
        {
            var source = """
object o = null;
_ = o is { } or null; // 1

_ = o switch
{
    { } => 0,
    null => 0, // 
    _ => 0,
};
""";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,20): warning CS9268: The pattern is redundant.
                // _ = o is 1 or 2 or 1; // 1
                Diagnostic(ErrorCode.WRN_RedundantPattern, "1").WithLocation(2, 20),
                // (3,5): warning CS8794: An expression of type 'object' always matches the provided pattern.
                // _ = o is 1 or 2 or not 1; // 2
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "o is 1 or 2 or not 1").WithArguments("object").WithLocation(3, 5),
                // (9,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     1 => 0, // 3
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "1").WithLocation(9, 5),
                // (18,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     _ => 0, // 4
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "_").WithLocation(18, 5));
        }

        // TODO2 the null portion?
    }
}
