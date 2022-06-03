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

        [Fact]
        [WorkItem(34980, "https://github.com/dotnet/roslyn/issues/34980")]
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
    IL_002f:  call       ""ThrowInvalidOperationException""
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
", sequencePoints: "C.Main", source: source).VerifyIL("ThrowInvalidOperationException", @"
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
    IL_002f:  call       ""ThrowSwitchExpressionExceptionParameterless""
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
", sequencePoints: "C.Main", source: source).VerifyIL("ThrowSwitchExpressionExceptionParameterless", @"
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
    IL_0028:  call       ""ThrowSwitchExpressionException""
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
", sequencePoints: "C.Main", source: source).VerifyIL("ThrowSwitchExpressionException", @"
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
                // (5,18): error CS8521: Pattern-matching is not permitted for pointer types.
                //         if (p is {}) { }
                Diagnostic(ErrorCode.ERR_PointerTypeInPatternMatching, "{}").WithLocation(5, 18),
                // (6,18): error CS0266: Cannot implicitly convert type 'int' to 'int*'. An explicit conversion exists (are you missing a cast?)
                //         if (p is 1) { }
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1").WithArguments("int", "int*").WithLocation(6, 18),
                // (6,18): error CS0150: A constant value is expected
                //         if (p is 1) { }
                Diagnostic(ErrorCode.ERR_ConstantExpected, "1").WithLocation(6, 18),
                // (7,18): error CS8521: Pattern-matching is not permitted for pointer types.
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
        if (o is A? b1) { }           // error 2 (missing :)
        if (o is A? b2 && c) { }      // error 3 (missing :)
        if (o is A[]? b5) { }         // error 4 (missing :)
        if (o is A[]? b6 && c) { }    // error 5 (missing :)
        if (o is A[][]? b7) { }       // error 6 (missing :)
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
                // (8,23): error CS1003: Syntax error, ':' expected
                //         if (o is A? b1) { }           // error 2 (missing :)
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(":").WithLocation(8, 23),
                // (8,23): error CS1525: Invalid expression term ')'
                //         if (o is A? b1) { }           // error 2 (missing :)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(8, 23),
                // (9,28): error CS1003: Syntax error, ':' expected
                //         if (o is A? b2 && c) { }      // error 3 (missing :)
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(":").WithLocation(9, 28),
                // (9,28): error CS1525: Invalid expression term ')'
                //         if (o is A? b2 && c) { }      // error 3 (missing :)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(9, 28),
                // (10,25): error CS1003: Syntax error, ':' expected
                //         if (o is A[]? b5) { }         // error 4 (missing :)
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(":").WithLocation(10, 25),
                // (10,25): error CS1525: Invalid expression term ')'
                //         if (o is A[]? b5) { }         // error 4 (missing :)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(10, 25),
                // (11,30): error CS1003: Syntax error, ':' expected
                //         if (o is A[]? b6 && c) { }    // error 5 (missing :)
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(":").WithLocation(11, 30),
                // (11,30): error CS1525: Invalid expression term ')'
                //         if (o is A[]? b6 && c) { }    // error 5 (missing :)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(11, 30),
                // (12,27): error CS1003: Syntax error, ':' expected
                //         if (o is A[][]? b7) { }       // error 6 (missing :)
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(":").WithLocation(12, 27),
                // (12,27): error CS1525: Invalid expression term ')'
                //         if (o is A[][]? b7) { }       // error 6 (missing :)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(12, 27),
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
                // (26,18): error CS7036: There is no argument given that corresponds to the required formal parameter 'i3' of 'C.Deconstruct(out int, out string, out int?)'
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
#endif
    }
}
