// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    [CompilerTrait(CompilerFeature.Patterns)]
    public class ITuplePatternTests : PatternMatchingTestBase
    {
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
            // - should not match when input type extends ITuple and has Deconstruct (struct)
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
            compilation.VerifyDiagnostics(
                // (10,32): error CS1501: No overload for method 'Deconstruct' takes 2 arguments
                //         Console.WriteLine(t is (3, 4)); // false
                Diagnostic(ErrorCode.ERR_BadArgCount, "(3, 4)").WithArguments("Deconstruct", "2").WithLocation(10, 32),
                // (10,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'C', with 2 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4)); // false
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4)").WithArguments("C", "2").WithLocation(10, 32),
                // (11,32): error CS1501: No overload for method 'Deconstruct' takes 3 arguments
                //         Console.WriteLine(t is (3, 4, 5)); // TRUE
                Diagnostic(ErrorCode.ERR_BadArgCount, "(3, 4, 5)").WithArguments("Deconstruct", "3").WithLocation(11, 32),
                // (11,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'C', with 3 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4, 5)); // TRUE
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4, 5)").WithArguments("C", "3").WithLocation(11, 32),
                // (12,32): error CS1501: No overload for method 'Deconstruct' takes 3 arguments
                //         Console.WriteLine(t is (3, 0, 5)); // false
                Diagnostic(ErrorCode.ERR_BadArgCount, "(3, 0, 5)").WithArguments("Deconstruct", "3").WithLocation(12, 32),
                // (12,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'C', with 3 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 0, 5)); // false
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 0, 5)").WithArguments("C", "3").WithLocation(12, 32),
                // (13,32): error CS1501: No overload for method 'Deconstruct' takes 4 arguments
                //         Console.WriteLine(t is (3, 4, 5, 6)); // false
                Diagnostic(ErrorCode.ERR_BadArgCount, "(3, 4, 5, 6)").WithArguments("Deconstruct", "4").WithLocation(13, 32),
                // (13,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'C', with 4 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4, 5, 6)); // false
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4, 5, 6)").WithArguments("C", "4").WithLocation(13, 32)
                );
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
            // - should not match when input type extends ITuple and has Deconstruct (class)
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
            compilation.VerifyDiagnostics(
                // (10,32): error CS1501: No overload for method 'Deconstruct' takes 2 arguments
                //         Console.WriteLine(t is (3, 4)); // false
                Diagnostic(ErrorCode.ERR_BadArgCount, "(3, 4)").WithArguments("Deconstruct", "2").WithLocation(10, 32),
                // (10,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'C', with 2 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4)); // false
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4)").WithArguments("C", "2").WithLocation(10, 32),
                // (11,32): error CS1501: No overload for method 'Deconstruct' takes 3 arguments
                //         Console.WriteLine(t is (3, 4, 5)); // TRUE
                Diagnostic(ErrorCode.ERR_BadArgCount, "(3, 4, 5)").WithArguments("Deconstruct", "3").WithLocation(11, 32),
                // (11,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'C', with 3 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4, 5)); // TRUE
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4, 5)").WithArguments("C", "3").WithLocation(11, 32),
                // (12,32): error CS1501: No overload for method 'Deconstruct' takes 3 arguments
                //         Console.WriteLine(t is (3, 0, 5)); // false
                Diagnostic(ErrorCode.ERR_BadArgCount, "(3, 0, 5)").WithArguments("Deconstruct", "3").WithLocation(12, 32),
                // (12,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'C', with 3 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 0, 5)); // false
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 0, 5)").WithArguments("C", "3").WithLocation(12, 32),
                // (13,32): error CS1501: No overload for method 'Deconstruct' takes 4 arguments
                //         Console.WriteLine(t is (3, 4, 5, 6)); // false
                Diagnostic(ErrorCode.ERR_BadArgCount, "(3, 4, 5, 6)").WithArguments("Deconstruct", "4").WithLocation(13, 32),
                // (13,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'C', with 4 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4, 5, 6)); // false
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4, 5, 6)").WithArguments("C", "4").WithLocation(13, 32)
                );
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
            // - should not match when input type extends ITuple and has Deconstruct (type parameter)
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
            compilation.VerifyDiagnostics(
                // (13,32): error CS1501: No overload for method 'Deconstruct' takes 2 arguments
                //         Console.WriteLine(t is (3, 4)); // false
                Diagnostic(ErrorCode.ERR_BadArgCount, "(3, 4)").WithArguments("Deconstruct", "2").WithLocation(13, 32),
                // (13,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'T', with 2 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4)); // false
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4)").WithArguments("T", "2").WithLocation(13, 32),
                // (14,32): error CS1501: No overload for method 'Deconstruct' takes 3 arguments
                //         Console.WriteLine(t is (3, 4, 5)); // TRUE
                Diagnostic(ErrorCode.ERR_BadArgCount, "(3, 4, 5)").WithArguments("Deconstruct", "3").WithLocation(14, 32),
                // (14,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'T', with 3 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4, 5)); // TRUE
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4, 5)").WithArguments("T", "3").WithLocation(14, 32),
                // (15,32): error CS1501: No overload for method 'Deconstruct' takes 3 arguments
                //         Console.WriteLine(t is (3, 0, 5)); // false
                Diagnostic(ErrorCode.ERR_BadArgCount, "(3, 0, 5)").WithArguments("Deconstruct", "3").WithLocation(15, 32),
                // (15,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'T', with 3 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 0, 5)); // false
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 0, 5)").WithArguments("T", "3").WithLocation(15, 32),
                // (16,32): error CS1501: No overload for method 'Deconstruct' takes 4 arguments
                //         Console.WriteLine(t is (3, 4, 5, 6)); // false
                Diagnostic(ErrorCode.ERR_BadArgCount, "(3, 4, 5, 6)").WithArguments("Deconstruct", "4").WithLocation(16, 32),
                // (16,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'T', with 4 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4, 5, 6)); // false
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4, 5, 6)").WithArguments("T", "4").WithLocation(16, 32)
                );
        }

        [Fact]
        public void ITuple_12()
        {
            // - should not match when input type extends ITuple and has Deconstruct (type parameter)
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
    public int Deconstruct() => 0;
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (13,32): error CS1501: No overload for method 'Deconstruct' takes 2 arguments
                //         Console.WriteLine(t is (3, 4)); // false
                Diagnostic(ErrorCode.ERR_BadArgCount, "(3, 4)").WithArguments("Deconstruct", "2").WithLocation(13, 32),
                // (13,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'T', with 2 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4)); // false
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4)").WithArguments("T", "2").WithLocation(13, 32),
                // (14,32): error CS1501: No overload for method 'Deconstruct' takes 3 arguments
                //         Console.WriteLine(t is (3, 4, 5)); // TRUE
                Diagnostic(ErrorCode.ERR_BadArgCount, "(3, 4, 5)").WithArguments("Deconstruct", "3").WithLocation(14, 32),
                // (14,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'T', with 3 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4, 5)); // TRUE
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4, 5)").WithArguments("T", "3").WithLocation(14, 32),
                // (15,32): error CS1501: No overload for method 'Deconstruct' takes 3 arguments
                //         Console.WriteLine(t is (3, 0, 5)); // false
                Diagnostic(ErrorCode.ERR_BadArgCount, "(3, 0, 5)").WithArguments("Deconstruct", "3").WithLocation(15, 32),
                // (15,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'T', with 3 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 0, 5)); // false
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 0, 5)").WithArguments("T", "3").WithLocation(15, 32),
                // (16,32): error CS1501: No overload for method 'Deconstruct' takes 4 arguments
                //         Console.WriteLine(t is (3, 4, 5, 6)); // false
                Diagnostic(ErrorCode.ERR_BadArgCount, "(3, 4, 5, 6)").WithArguments("Deconstruct", "4").WithLocation(16, 32),
                // (16,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'T', with 4 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4, 5, 6)); // false
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4, 5, 6)").WithArguments("T", "4").WithLocation(16, 32)
                );
        }

        [Fact]
        public void ITuple_07()
        {
            // - should not match when input type extends ITuple and has Deconstruct (inherited)
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
            compilation.VerifyDiagnostics(
                // (17,32): error CS1501: No overload for method 'Deconstruct' takes 2 arguments
                //         Console.WriteLine(t is (3, 4)); // false
                Diagnostic(ErrorCode.ERR_BadArgCount, "(3, 4)").WithArguments("Deconstruct", "2").WithLocation(17, 32),
                // (17,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'T', with 2 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4)); // false
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4)").WithArguments("T", "2").WithLocation(17, 32),
                // (18,32): error CS1501: No overload for method 'Deconstruct' takes 3 arguments
                //         Console.WriteLine(t is (3, 4, 5)); // TRUE
                Diagnostic(ErrorCode.ERR_BadArgCount, "(3, 4, 5)").WithArguments("Deconstruct", "3").WithLocation(18, 32),
                // (18,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'T', with 3 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4, 5)); // TRUE
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4, 5)").WithArguments("T", "3").WithLocation(18, 32),
                // (19,32): error CS1501: No overload for method 'Deconstruct' takes 3 arguments
                //         Console.WriteLine(t is (3, 0, 5)); // false
                Diagnostic(ErrorCode.ERR_BadArgCount, "(3, 0, 5)").WithArguments("Deconstruct", "3").WithLocation(19, 32),
                // (19,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'T', with 3 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 0, 5)); // false
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 0, 5)").WithArguments("T", "3").WithLocation(19, 32),
                // (20,32): error CS1501: No overload for method 'Deconstruct' takes 4 arguments
                //         Console.WriteLine(t is (3, 4, 5, 6)); // false
                Diagnostic(ErrorCode.ERR_BadArgCount, "(3, 4, 5, 6)").WithArguments("Deconstruct", "4").WithLocation(20, 32),
                // (20,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'T', with 4 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4, 5, 6)); // false
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4, 5, 6)").WithArguments("T", "4").WithLocation(20, 32)
                );
        }

        [Fact]
        public void ITuple_08()
        {
            // - should not match when input type extends ITuple and has Deconstruct (static)
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
            compilation.VerifyDiagnostics(
                // (17,32): error CS1501: No overload for method 'Deconstruct' takes 2 arguments
                //         Console.WriteLine(t is (3, 4)); // false
                Diagnostic(ErrorCode.ERR_BadArgCount, "(3, 4)").WithArguments("Deconstruct", "2").WithLocation(17, 32),
                // (17,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'T', with 2 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4)); // false
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4)").WithArguments("T", "2").WithLocation(17, 32),
                // (18,32): error CS1501: No overload for method 'Deconstruct' takes 3 arguments
                //         Console.WriteLine(t is (3, 4, 5)); // TRUE
                Diagnostic(ErrorCode.ERR_BadArgCount, "(3, 4, 5)").WithArguments("Deconstruct", "3").WithLocation(18, 32),
                // (18,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'T', with 3 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4, 5)); // TRUE
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4, 5)").WithArguments("T", "3").WithLocation(18, 32),
                // (19,32): error CS1501: No overload for method 'Deconstruct' takes 3 arguments
                //         Console.WriteLine(t is (3, 0, 5)); // false
                Diagnostic(ErrorCode.ERR_BadArgCount, "(3, 0, 5)").WithArguments("Deconstruct", "3").WithLocation(19, 32),
                // (19,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'T', with 3 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 0, 5)); // false
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 0, 5)").WithArguments("T", "3").WithLocation(19, 32),
                // (20,32): error CS1501: No overload for method 'Deconstruct' takes 4 arguments
                //         Console.WriteLine(t is (3, 4, 5, 6)); // false
                Diagnostic(ErrorCode.ERR_BadArgCount, "(3, 4, 5, 6)").WithArguments("Deconstruct", "4").WithLocation(20, 32),
                // (20,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'T', with 4 out parameters and a void return type.
                //         Console.WriteLine(t is (3, 4, 5, 6)); // false
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(3, 4, 5, 6)").WithArguments("T", "4").WithLocation(20, 32)
                );
        }

        [Fact]
        public void ITuple_09()
        {
            // - should match when input type extends ITuple and has only an extension Deconstruct
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
static class Extensions
{
    public static void Deconstruct(this C c, out int X, out int Y) => (X, Y) = (3, 4);
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
            var dpcss = tree.GetRoot().DescendantNodes().OfType<DeconstructionPatternClauseSyntax>().ToArray();
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
    }
}
