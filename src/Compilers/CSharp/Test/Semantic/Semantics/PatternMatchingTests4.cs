// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
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
            compilation.VerifyDiagnostics(
                // (9,18): error CS0119: '_' is a type, which is not valid in the given context
                //             case _ x: break;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "_").WithArguments("_", "type").WithLocation(9, 18),
                // (9,20): error CS1003: Syntax error, ':' expected
                //             case _ x: break;
                Diagnostic(ErrorCode.ERR_SyntaxError, "x").WithArguments(":", "").WithLocation(9, 20),
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
                Diagnostic(ErrorCode.ERR_SyntaxError, "x").WithArguments(",", "").WithLocation(9, 34),
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
                Diagnostic(ErrorCode.ERR_SyntaxError, "x").WithArguments(",", "").WithLocation(10, 25),
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
            (true, true) => 4
            };
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,25): warning CS8509: The switch expression does not handle all possible inputs (it is not exhaustive).
                //         return (b1, b2) switch {
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithLocation(6, 25)
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
            (true, true) => 4
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
            (null, true) => 6
            };
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (13,13): error CS8510: The pattern has already been handled by a previous arm of the switch expression.
                //             (null, true) => 6
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "(null, true)").WithLocation(13, 13)
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
                // (22,18): warning CS8509: The switch expression does not handle all possible inputs (it is not exhaustive).
                //         return b switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithLocation(22, 18)
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
    }
}
