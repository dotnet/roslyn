// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// this place is dedicated to binding related error tests
    /// </summary>
    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    public class SpanStackSafetyTests : CompilingTestBase
    {
        [Fact]
        public void SpanAssignmentExpression()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    void M()
    {
        Span<int> s1 = stackalloc int[1];
        Span<int> s2 = new Span<int>();

        s2 = (s2 = new Span<int>());
        s2 = (s1 = s2);
    }
}");
            comp.VerifyDiagnostics(
                // (11,15): error CS8352: Cannot use local 's1' in this context because it may expose referenced variables outside of their declaration scope
                //         s2 = (s1 = s2);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "s1 = s2").WithArguments("s1").WithLocation(11, 15));
        }

        [Fact]
        public void SpanToSpanSwitch()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    void M(Span<string> s)
    {
        switch (s)
        {
            case Span<string> span:
                break;
        }
    }
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void SpanToObjectPatternSwitch()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;

class C
{
    void M(object o)
    {
        switch (o)
        {
            case Span<int> span:
                break;
            default:
                break;
        }
    }
}");
            comp.VerifyDiagnostics(
                // (10,18): error CS8121: An expression of type 'object' cannot be handled by a pattern of type 'Span<int>'.
                //             case Span<int> span:
                Diagnostic(ErrorCode.ERR_PatternWrongType, "Span<int>").WithArguments("object", "System.Span<int>").WithLocation(10, 18)
                );
        }

        [Fact]
        public void SpanToGenericPatternSwitch()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    void M<T>(T t)
    {
        switch (t)
        {
            case Span<int> span:
                break;
            default:
                break;
        }
    }
}");
            comp.VerifyDiagnostics(
                // (9,18): error CS8121: An expression of type 'T' cannot be handled by a pattern of type 'Span<int>'.
                //             case Span<int> span:
                Diagnostic(ErrorCode.ERR_PatternWrongType, "Span<int>").WithArguments("T", "System.Span<int>").WithLocation(9, 18)
                );
        }

        [Fact]
        public void SpanToGenericPatternSwitch2()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    void M<T>(T t) where T : struct
    {
        switch (t)
        {
            case Span<int> span:
                break;
            default:
                break;
        }
    }
}");
            comp.VerifyDiagnostics(
                // (9,18): error CS8121: An expression of type 'T' cannot be handled by a pattern of type 'Span<int>'.
                //             case Span<int> span:
                Diagnostic(ErrorCode.ERR_PatternWrongType, "Span<int>").WithArguments("T", "System.Span<int>").WithLocation(9, 18)
                );
        }

        [Fact]
        public void ObjectToSpanPatternSwitch()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    void M(Span<string> s)
    {
        switch (s)
        {
            case Span<object> span:
                break;
            case object o:
                break;
        }
    }
}");
            comp.VerifyDiagnostics(
                // (9,18): error CS8121: An expression of type 'Span<string>' cannot be handled by a pattern of type 'Span<object>'.
                //             case Span<object> span:
                Diagnostic(ErrorCode.ERR_PatternWrongType, "Span<object>").WithArguments("System.Span<string>", "System.Span<object>").WithLocation(9, 18),
                // (11,18): error CS8121: An expression of type 'Span<string>' cannot be handled by a pattern of type 'object'.
                //             case object o:
                Diagnostic(ErrorCode.ERR_PatternWrongType, "object").WithArguments("System.Span<string>", "object").WithLocation(11, 18)
                );
        }

        [Fact]
        public void GenericToSpanPatternSwitch()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    void M<T>(Span<string> s)
    {
        switch (s)
        {
            case T t:
                break;
        }
    }
}");
            comp.VerifyDiagnostics(
                // (9,18): error CS8121: An expression of type 'Span<string>' cannot be handled by a pattern of type 'T'.
                //             case T t:
                Diagnostic(ErrorCode.ERR_PatternWrongType, "T").WithArguments("System.Span<string>", "T").WithLocation(9, 18)
                );
        }

        [Fact]
        public void GenericToSpanPatternSwitch2()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    void M<T>(Span<string> s) where T : struct
    {
        switch (s)
        {
            case T t:
                break;
        }
    }
}");
            comp.VerifyDiagnostics(
                // (9,18): error CS8121: An expression of type 'Span<string>' cannot be handled by a pattern of type 'T'.
                //             case T t:
                Diagnostic(ErrorCode.ERR_PatternWrongType, "T").WithArguments("System.Span<string>", "T").WithLocation(9, 18)
                );
        }

        [Fact]
        public void SpanToSpanIsExpr()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    bool M(Span<string> s) => s is Span<string> && s is Span<string> span;
}");
            comp.VerifyDiagnostics(
                // (5,31): warning CS0183: The given expression is always of the provided ('Span<string>') type
                //     bool M(Span<string> s) => s is Span<string> && s is Span<string> span;
                Diagnostic(ErrorCode.WRN_IsAlwaysTrue, "s is Span<string>").WithArguments("System.Span<string>").WithLocation(5, 31));
        }

        [Fact]
        public void ObjectToSpanIsExpr()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    void M(object o)
    {
        if (o is Span<int>)
        { }
        if (o is Span<int> s)
        { }
    }
}");
            comp.VerifyDiagnostics(
                // (7,13): warning CS0184: The given expression is never of the provided ('Span<int>') type
                //         if (o is Span<int>)
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "o is Span<int>").WithArguments("System.Span<int>").WithLocation(7, 13),
                // (9,18): error CS8121: An expression of type 'object' cannot be handled by a pattern of type 'Span<int>'.
                //         if (o is Span<int> s)
                Diagnostic(ErrorCode.ERR_PatternWrongType, "Span<int>").WithArguments("object", "System.Span<int>").WithLocation(9, 18));
        }

        [Fact]
        public void GenericToSpanIsExpr()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    void M<T>(T t)
    {
        if (t is Span<int>)
        { }
        if (t is Span<int> s)
        { }
    }
}");
            comp.VerifyDiagnostics(
                // (7,13): warning CS0184: The given expression is never of the provided ('Span<int>') type
                //         if (t is Span<int>)
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "t is Span<int>").WithArguments("System.Span<int>").WithLocation(7, 13),
                // (9,18): error CS8121: An expression of type 'T' cannot be handled by a pattern of type 'Span<int>'.
                //         if (t is Span<int> s)
                Diagnostic(ErrorCode.ERR_PatternWrongType, "Span<int>").WithArguments("T", "System.Span<int>").WithLocation(9, 18));
        }

        [Fact]
        public void SpanToObjectIsExpr()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    void M(Span<int> s)
    {
        if (s is object) { }
        if (s is object o) { }
    }
}");
            comp.VerifyDiagnostics(
                // (7,13): warning CS0184: The given expression is never of the provided ('object') type
                //         if (s is object) { }
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "s is object").WithArguments("object").WithLocation(7, 13),
                // (8,18): error CS8121: An expression of type 'Span<int>' cannot be handled by a pattern of type 'object'.
                //         if (s is object o) { }
                Diagnostic(ErrorCode.ERR_PatternWrongType, "object").WithArguments("System.Span<int>", "object").WithLocation(8, 18));
        }

        [Fact]
        public void SpanToGenericIsExpr()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    void M<T>(Span<int> s)
    {
        if (s is T) { }
        if (s is T t) { }
    }
}");
            comp.VerifyDiagnostics(
                // (7,13): warning CS0184: The given expression is never of the provided ('T') type
                //         if (s is T) { }
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "s is T").WithArguments("T").WithLocation(7, 13),
                // (8,18): error CS8121: An expression of type 'Span<int>' cannot be handled by a pattern of type 'T'.
                //         if (s is T t) { }
                Diagnostic(ErrorCode.ERR_PatternWrongType, "T").WithArguments("System.Span<int>", "T").WithLocation(8, 18));
        }

        [Fact]
        public void SpanToGenericIsExpr2()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    void M<T>(Span<int> s) where T : struct
    {
        if (s is T) { }
        if (s is T t) { }
    }
}");
            comp.VerifyDiagnostics(
                // (7,13): warning CS0184: The given expression is never of the provided ('T') type
                //         if (s is T) { }
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "s is T").WithArguments("T").WithLocation(7, 13),
                // (8,18): error CS8121: An expression of type 'Span<int>' cannot be handled by a pattern of type 'T'.
                //         if (s is T t) { }
                Diagnostic(ErrorCode.ERR_PatternWrongType, "T").WithArguments("System.Span<int>", "T").WithLocation(8, 18));
        }

        [Fact]
        public void TrivialBoxing()
        {
            var text = @"
using System;

class Program
{
    static void Main()
    {
        object x = new Span<int>();
        object y = new ReadOnlySpan<byte>();
        object z = new SpanLike<int>();
    }
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(text);

            comp.VerifyDiagnostics(
                // (8,20): error CS0029: Cannot implicitly convert type 'System.Span<int>' to 'object'
                //         object x = new Span<int>();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new Span<int>()").WithArguments("System.Span<int>", "object").WithLocation(8, 20),
                // (9,20): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<byte>' to 'object'
                //         object y = new ReadOnlySpan<byte>();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new ReadOnlySpan<byte>()").WithArguments("System.ReadOnlySpan<byte>", "object").WithLocation(9, 20),
                // (10,20): error CS0029: Cannot implicitly convert type 'System.SpanLike<int>' to 'object'
                //         object z = new SpanLike<int>();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new SpanLike<int>()").WithArguments("System.SpanLike<int>", "object")
            );

            comp = CreateCompilationWithMscorlibAndSpanSrc(text);

            comp.VerifyDiagnostics(
                // (8,20): error CS0029: Cannot implicitly convert type 'System.Span<int>' to 'object'
                //         object x = new Span<int>();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new Span<int>()").WithArguments("System.Span<int>", "object").WithLocation(8, 20),
                // (9,20): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<byte>' to 'object'
                //         object y = new ReadOnlySpan<byte>();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new ReadOnlySpan<byte>()").WithArguments("System.ReadOnlySpan<byte>", "object").WithLocation(9, 20),
                // (10,20): error CS0029: Cannot implicitly convert type 'System.SpanLike<int>' to 'object'
                //         object z = new SpanLike<int>();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new SpanLike<int>()").WithArguments("System.SpanLike<int>", "object")
            );
        }

        [Fact]
        public void LambdaCapturing()
        {
            var text = @"
using System;

class Program
{
    // this should be ok
    public delegate Span<T> D1<T>(Span<T> arg);

    static void Main()
    {
        var x = new Span<int>();

        D1<int> d = (t)=>t;
        x = d(x);
        
        // error due to capture
        Func<int> f = () => x[1];
    }
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(text);

            comp.VerifyDiagnostics(
                // (17,29): error CS8175: Cannot use ref local 'x' inside an anonymous method, lambda expression, or query expression
                //         Func<int> f = () => x[1];
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseLocal, "x").WithArguments("x").WithLocation(17, 29)
            );
        }

        [Fact]
        public void GenericArgsAndConstraints()
        {
            var text = @"
using System;

class Program
{
    static void Main()
    {
        var x = new Span<int>();

        Func<Span<int>> d = ()=>x;
    }

    class C1<T> where T: Span<int>
    {
    }
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(text);

            comp.VerifyDiagnostics(
                // (13,26): error CS0701: 'Span<int>' is not a valid constraint. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                //     class C1<T> where T: Span<int>
                Diagnostic(ErrorCode.ERR_BadBoundType, "Span<int>").WithArguments("System.Span<int>").WithLocation(13, 26),
                // (10,14): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         Func<Span<int>> d = ()=>x;
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "Span<int>").WithArguments("System.Span<int>").WithLocation(10, 14),
                // (10,33): error CS8175: Cannot use ref local 'x' inside an anonymous method, lambda expression, or query expression
                //         Func<Span<int>> d = ()=>x;
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseLocal, "x").WithArguments("x").WithLocation(10, 33)
            );
        }

        [Fact]
        public void Arrays()
        {
            var text = @"
using System;

class Program
{
    static void Main()
    {
        var x = new Span<int>[1];

        var y = new SpanLike<int>[1,2];
    }
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(text);

            comp.VerifyDiagnostics(
                // (8,21): error CS0611: Array elements cannot be of type 'Span<int>'
                //         var x = new Span<int>[1];
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "Span<int>").WithArguments("System.Span<int>"),
                // (10,21): error CS0611: Array elements cannot be of type 'SpanLike<int>'
                //         var y = new SpanLike<int>[1,2];
                Diagnostic(ErrorCode.ERR_ArrayElementCantBeRefAny, "SpanLike<int>").WithArguments("System.SpanLike<int>").WithLocation(10, 21)
            );
        }

        [Fact]
        public void ByrefParam()
        {
            var text = @"
using System;

class Program
{
    static void Main()
    {
    }

    // OK
    static void M1(ref Span<string> ss)
    {
    }

    // OK
    static void M2(out SpanLike<string> ss)
    {
        ss = default;
    }

    // OK
    static void M3(in Span<string> ss)
    {
    }

    // OK
    static void M3l(in SpanLike<string> ss)
    {
    }

    // OK
    static ref Span<string> M4(ref Span<string> ss) { return ref ss; }

    // OK
    static ref readonly Span<string> M5(ref Span<string> ss) => ref ss;

    // Not OK
    // TypedReference baseline
    static ref TypedReference M1(ref TypedReference ss) => ref ss;
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(text);

            comp.VerifyDiagnostics(
                // (39,34): error CS1601: Cannot make reference to variable of type 'TypedReference'
                //     static ref TypedReference M1(ref TypedReference ss) => ref ss;
                Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "ref TypedReference ss").WithArguments("System.TypedReference").WithLocation(39, 34),
                // (39,12): error CS1599: The return type of a method, delegate, or function pointer cannot be 'TypedReference'
                //     static ref TypedReference M1(ref TypedReference ss) => ref ss;
                Diagnostic(ErrorCode.ERR_MethodReturnCantBeRefAny, "ref TypedReference").WithArguments("System.TypedReference").WithLocation(39, 12)
            );
        }

        [Fact]
        public void FieldsSpan()
        {
            var text = @"
using System;

public class Program
{
    static void Main()
    {
    }

    public static Span<byte> fs;
    public Span<int> fi; 

    public ref struct S1
    {
        public static Span<byte> fs1;
        public Span<int> fi1; 
    }

    public struct S2
    {
        public static Span<byte> fs2;
        public Span<int> fi2; 
    }
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(text);

            comp.VerifyDiagnostics(
                // (22,16): error CS8345: Field or auto-implemented property cannot be of type 'Span<int>' unless it is an instance member of a ref struct.
                //         public Span<int> fi2; 
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "Span<int>").WithArguments("System.Span<int>").WithLocation(22, 16),
                // (21,23): error CS8345: Field or auto-implemented property cannot be of type 'Span<byte>' unless it is an instance member of a ref struct.
                //         public static Span<byte> fs2;
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "Span<byte>").WithArguments("System.Span<byte>").WithLocation(21, 23),
                // (10,19): error CS8345: Field or auto-implemented property cannot be of type 'Span<byte>' unless it is an instance member of a ref struct.
                //     public static Span<byte> fs;
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "Span<byte>").WithArguments("System.Span<byte>").WithLocation(10, 19),
                // (11,12): error CS8345: Field or auto-implemented property cannot be of type 'Span<int>' unless it is an instance member of a ref struct.
                //     public Span<int> fi; 
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "Span<int>").WithArguments("System.Span<int>").WithLocation(11, 12),
                // (15,23): error CS8345: Field or auto-implemented property cannot be of type 'Span<byte>' unless it is an instance member of a ref struct.
                //         public static Span<byte> fs1;
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "Span<byte>").WithArguments("System.Span<byte>").WithLocation(15, 23)
            );

            comp = CreateCompilationWithMscorlibAndSpanSrc(text);

            comp.VerifyDiagnostics(
                // (22,16): error CS8345: Field or auto-implemented property cannot be of type 'Span<int>' unless it is an instance member of a ref struct.
                //         public Span<int> fi2; 
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "Span<int>").WithArguments("System.Span<int>").WithLocation(22, 16),
                // (21,23): error CS8345: Field or auto-implemented property cannot be of type 'Span<byte>' unless it is an instance member of a ref struct.
                //         public static Span<byte> fs2;
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "Span<byte>").WithArguments("System.Span<byte>").WithLocation(21, 23),
                // (10,19): error CS8345: Field or auto-implemented property cannot be of type 'Span<byte>' unless it is an instance member of a ref struct.
                //     public static Span<byte> fs;
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "Span<byte>").WithArguments("System.Span<byte>").WithLocation(10, 19),
                // (11,12): error CS8345: Field or auto-implemented property cannot be of type 'Span<int>' unless it is an instance member of a ref struct.
                //     public Span<int> fi; 
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "Span<int>").WithArguments("System.Span<int>").WithLocation(11, 12),
                // (15,23): error CS8345: Field or auto-implemented property cannot be of type 'Span<byte>' unless it is an instance member of a ref struct.
                //         public static Span<byte> fs1;
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "Span<byte>").WithArguments("System.Span<byte>").WithLocation(15, 23)
            );
        }

        [Fact]
        public void FieldsSpanLike()
        {
            var text = @"
using System;

public class Program
{
    static void Main()
    {
    }

    public static SpanLike<byte> fs;
    public SpanLike<int> fi; 

    public ref struct S1
    {
        public static SpanLike<byte> fs1;
        public SpanLike<int> fi1; 
    }

    public struct S2
    {
        public static SpanLike<byte> fs2;
        public SpanLike<int> fi2; 
    }
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(text);

            comp.VerifyDiagnostics(
                // (22,16): error CS8345: Field or auto-implemented property cannot be of type 'SpanLike<int>' unless it is an instance member of a ref struct.
                //         public SpanLike<int> fi2; 
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "SpanLike<int>").WithArguments("System.SpanLike<int>").WithLocation(22, 16),
                // (21,23): error CS8345: Field or auto-implemented property cannot be of type 'SpanLike<byte>' unless it is an instance member of a ref struct.
                //         public static SpanLike<byte> fs2;
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "SpanLike<byte>").WithArguments("System.SpanLike<byte>").WithLocation(21, 23),
                // (10,19): error CS8345: Field or auto-implemented property cannot be of type 'SpanLike<byte>' unless it is an instance member of a ref struct.
                //     public static SpanLike<byte> fs;
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "SpanLike<byte>").WithArguments("System.SpanLike<byte>").WithLocation(10, 19),
                // (11,12): error CS8345: Field or auto-implemented property cannot be of type 'SpanLike<int>' unless it is an instance member of a ref struct.
                //     public SpanLike<int> fi; 
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "SpanLike<int>").WithArguments("System.SpanLike<int>").WithLocation(11, 12),
                // (15,23): error CS8345: Field or auto-implemented property cannot be of type 'SpanLike<byte>' unless it is an instance member of a ref struct.
                //         public static SpanLike<byte> fs1;
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "SpanLike<byte>").WithArguments("System.SpanLike<byte>").WithLocation(15, 23)
            );

            comp = CreateCompilationWithMscorlibAndSpanSrc(text);

            comp.VerifyDiagnostics(
                // (22,16): error CS8345: Field or auto-implemented property cannot be of type 'SpanLike<int>' unless it is an instance member of a ref struct.
                //         public SpanLike<int> fi2; 
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "SpanLike<int>").WithArguments("System.SpanLike<int>").WithLocation(22, 16),
                // (21,23): error CS8345: Field or auto-implemented property cannot be of type 'SpanLike<byte>' unless it is an instance member of a ref struct.
                //         public static SpanLike<byte> fs2;
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "SpanLike<byte>").WithArguments("System.SpanLike<byte>").WithLocation(21, 23),
                // (10,19): error CS8345: Field or auto-implemented property cannot be of type 'SpanLike<byte>' unless it is an instance member of a ref struct.
                //     public static SpanLike<byte> fs;
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "SpanLike<byte>").WithArguments("System.SpanLike<byte>").WithLocation(10, 19),
                // (11,12): error CS8345: Field or auto-implemented property cannot be of type 'SpanLike<int>' unless it is an instance member of a ref struct.
                //     public SpanLike<int> fi; 
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "SpanLike<int>").WithArguments("System.SpanLike<int>").WithLocation(11, 12),
                // (15,23): error CS8345: Field or auto-implemented property cannot be of type 'SpanLike<byte>' unless it is an instance member of a ref struct.
                //         public static SpanLike<byte> fs1;
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "SpanLike<byte>").WithArguments("System.SpanLike<byte>").WithLocation(15, 23)
            );
        }

        [WorkItem(20226, "https://github.com/dotnet/roslyn/issues/20226")]
        [Fact]
        public void InterfaceImpl()
        {
            var text = @"
using System;

public class Program
{
    static void Main(string[] args)
    {
        using (new S1())
        {

        }
    }

    public ref struct S1 : IDisposable
    {
        public void Dispose() { }
    }
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(text);

            comp.VerifyDiagnostics(
                // (14,28): error CS8343: 'Program.S1': ref structs cannot implement interfaces
                //     public ref struct S1 : IDisposable
                Diagnostic(ErrorCode.ERR_RefStructInterfaceImpl, "IDisposable").WithArguments("Program.S1", "System.IDisposable").WithLocation(14, 28)
            );
        }

        [Fact]
        public void NoInterfaceImp()
        {
            var text = @"
public class Program
{
    static void Main(string[] args)
    {
        using (new S1())
        {

        }
    }

    public ref struct S1
    {
        public void Dispose() { }
    }
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(text);

            comp.VerifyDiagnostics(
            );
        }

        [WorkItem(20226, "https://github.com/dotnet/roslyn/issues/20226")]
        [Fact]
        public void RefIteratorInAsync()
        {
            var text = @"
using System;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
    }

    static async Task<int> Test()
    {
        var obj = new C1();

        foreach (var i in obj)
        {
            await Task.Yield();
            System.Console.WriteLine(i);
        }

        return 123;
    }
}

class C1
{
    public S1 GetEnumerator()
    {
        return new S1();
    }

    public ref struct S1
    {
        public int Current => throw new NotImplementedException();

        public bool MoveNext()
        {
            throw new NotImplementedException();
        }
    }
}

";

            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(text);

            comp.VerifyDiagnostics(
                // (15,9): error CS8344: foreach statement cannot operate on enumerators of type 'C1.S1' in async or iterator methods because 'C1.S1' is a ref struct.
                //         foreach (var i in obj)
                Diagnostic(ErrorCode.ERR_BadSpecialByRefIterator, "foreach").WithArguments("C1.S1").WithLocation(15, 9)
            );
        }

        [WorkItem(20226, "https://github.com/dotnet/roslyn/issues/20226")]
        [Fact]
        public void RefIteratorInIterator()
        {
            var text = @"
using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        // this is valid
        Action a = () =>
        {
            foreach (var i in new C1())
            {
            }
        };

        a();
    }

    static IEnumerable<int> Test()
    {
        // this is valid
        Action a = () =>
        {
            foreach (var i in new C1())
            {
            }
        };

        a();

        // this is an error
        foreach (var i in new C1())
        {
        }

        yield return 1;
    }
}

class C1
{
    public S1 GetEnumerator()
    {
        return new S1();
    }

    public ref struct S1
    {
        public int Current => throw new NotImplementedException();

        public bool MoveNext()
        {
            throw new NotImplementedException();
        }
    }
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(text);

            comp.VerifyDiagnostics(
                // (33,9): error CS8344: foreach statement cannot operate on enumerators of type 'C1.S1' in async or iterator methods because 'C1.S1' is a ref struct.
                //         foreach (var i in new C1())
                Diagnostic(ErrorCode.ERR_BadSpecialByRefIterator, "foreach").WithArguments("C1.S1").WithLocation(33, 9)
            );
        }

        [Fact]
        public void Properties()
        {
            var text = @"
using System;

public class Program
{
    static void Main()
    {
    }

    // valid
    public static Span<byte> ps => default(Span<byte>);
    public Span<int> pi => default(Span<int>); 

    public Span<int> this[int i] => default(Span<int>); 

    // not valid
    public static Span<byte> aps {get;}
    public Span<int> api {get; set;} 
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(text);

            comp.VerifyDiagnostics(
                // (17,19): error CS8345: Field or auto-implemented property cannot be of type 'Span<byte>' unless it is an instance member of a ref struct.
                //     public static Span<byte> aps {get;}
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "Span<byte>").WithArguments("System.Span<byte>").WithLocation(17, 19),
                // (18,12): error CS8345: Field or auto-implemented property cannot be of type 'Span<int>' unless it is an instance member of a ref struct.
                //     public Span<int> api {get; set;} 
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "Span<int>").WithArguments("System.Span<int>").WithLocation(18, 12)
            );
        }

        [Fact]
        public void Operators()
        {
            var text = @"
using System;

public class Program
{
    static void Main()
    {
    }

    // valid
    public static Span<byte> operator +(Span<byte> x, Program y) => default(Span<byte>);

    // invalid (baseline w/ TypedReference)
    public static TypedReference operator +(Span<int> x, Program y) => default(TypedReference);

}
";

            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(text);

            comp.VerifyDiagnostics(
                // (14,19): error CS1599: The return type of a method, delegate, or function pointer cannot be 'TypedReference'
                //     public static TypedReference operator +(Span<int> x, Program y) => default(TypedReference);
                Diagnostic(ErrorCode.ERR_MethodReturnCantBeRefAny, "TypedReference").WithArguments("System.TypedReference").WithLocation(14, 19)
            );
        }

        [Fact]
        public void AsyncParams()
        {
            var text = @"
using System;
using System.Threading.Tasks;

public class Program
{
    static void Main()
    {
    }

    public static async Task<int> M1(Span<int> arg)
    {
        await Task.Yield();
        return 42;
    }
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(text);

            comp.VerifyDiagnostics(
                // (11,48): error CS4012: Parameters or locals of type 'Span<int>' cannot be declared in async methods or async lambda expressions.
                //     public static async Task<int> M1(Span<int> arg)
                Diagnostic(ErrorCode.ERR_BadSpecialByRefLocal, "arg").WithArguments("System.Span<int>").WithLocation(11, 48)
            );
        }

        [Fact]
        public void AsyncLocals()
        {
            var text = @"
using System;
using System.Threading.Tasks;

public class Program
{
    static void Main()
    {
    }

    public static async Task<int> M1()
    {
        Span<int> local = default(Span<int>);

        await Task.Yield();
        return 42;
    }
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(text);

            comp.VerifyDiagnostics(
                // (13,9): error CS4012: Parameters or locals of type 'Span<int>' cannot be declared in async methods or async lambda expressions.
                //         Span<int> local = default(Span<int>);
                Diagnostic(ErrorCode.ERR_BadSpecialByRefLocal, "Span<int>").WithArguments("System.Span<int>").WithLocation(13, 9)
            );

            comp = CreateCompilationWithMscorlibAndSpan(text, TestOptions.DebugExe);

            comp.VerifyDiagnostics(
                // (13,9): error CS4012: Parameters or locals of type 'Span<int>' cannot be declared in async methods or async lambda expressions.
                //         Span<int> local = default(Span<int>);
                Diagnostic(ErrorCode.ERR_BadSpecialByRefLocal, "Span<int>").WithArguments("System.Span<int>").WithLocation(13, 9)
            );
        }

        [Fact]
        public void AsyncSpilling()
        {
            var text = @"
using System;
using System.Threading.Tasks;

public class Program
{
    static void Main()
    {
    }

    public static async Task<int> M1()
    {
        // this is ok
        TakesSpan(default(Span<int>), 123);

        // this is not ok
        TakesSpan(default(Span<int>), await I1());

        // this is ok
        TakesSpan(await I1(), default(Span<int>));

        return 42;
    }

    public static void TakesSpan(Span<int> s, int i)
    {
    }

    public static void TakesSpan(int i, Span<int> s)
    {
    }

    public static async Task<int> I1()
    {
        await Task.Yield();
        return 42;
    }
    
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(text);

            comp.VerifyEmitDiagnostics(
                // (17,39): error CS4007: 'await' cannot be used in an expression containing the type 'System.Span<int>'
                //         TakesSpan(default(Span<int>), await I1());
                Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "await I1()").WithArguments("System.Span<int>")
            );

            comp = CreateCompilationWithMscorlibAndSpan(text, TestOptions.DebugExe);

            comp.VerifyEmitDiagnostics(
                // (17,39): error CS4007: 'await' cannot be used in an expression containing the type 'System.Span<int>'
                //         TakesSpan(default(Span<int>), await I1());
                Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "await I1()").WithArguments("System.Span<int>")
            );
        }

        [Fact]
        public void AsyncSpillTemp()
        {
            var text = @"
using System;
using System.Threading.Tasks;

public class Program
{
    static void Main()
    {
    }

    public static async Task<int> M1()
    {
        // this is not ok
        TakesSpan(s: default(Span<int>), i: await I1());

        return 42;
    }

    public static void TakesSpan(int i, Span<int> s)
    {
    }

    public static async Task<int> I1()
    {
        await Task.Yield();
        return 42;
    }
    
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(text);

            comp.VerifyEmitDiagnostics(
                // (14,45): error CS4007: 'await' cannot be used in an expression containing the type 'Span<int>'
                //         TakesSpan(s: default(Span<int>), i: await I1());
                Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "await I1()").WithArguments("System.Span<int>").WithLocation(14, 45)
            );

            comp = CreateCompilationWithMscorlibAndSpan(text, TestOptions.DebugExe);

            comp.VerifyEmitDiagnostics(
                // (14,45): error CS4007: 'await' cannot be used in an expression containing the type 'Span<int>'
                //         TakesSpan(s: default(Span<int>), i: await I1());
                Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "await I1()").WithArguments("System.Span<int>").WithLocation(14, 45)
            );
        }

        [Fact]
        public void BaseMethods()
        {
            var text = @"
using System;

public class Program
{
    static void Main()
    {
        // this is ok  (overridden)
        default(Span<int>).GetHashCode();

        // this is ok  (implicit boxing)
        default(Span<int>).GetType();

        // this is not ok  (implicit boxing)
        default(Span<int>).ToString();
    }
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(text);

            comp.VerifyDiagnostics(
                // (12,9): error CS0029: Cannot implicitly convert type 'System.Span<int>' to 'object'
                //         default(Span<int>).GetType();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "default(Span<int>)").WithArguments("System.Span<int>", "object").WithLocation(12, 9),
                // (15,9): error CS0029: Cannot implicitly convert type 'System.Span<int>' to 'System.ValueType'
                //         default(Span<int>).ToString();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "default(Span<int>)").WithArguments("System.Span<int>", "System.ValueType").WithLocation(15, 9)
            );
        }

        [WorkItem(21979, "https://github.com/dotnet/roslyn/issues/21979")]
        [Fact]
        public void MethodConversion()
        {
            var text = @"
using System;

public class Program
{
    static void Main()
    {
        // we no longer allow this.
        // see https://github.com/dotnet/roslyn/issues/21979
        Func<int> d0 = default(TypedReference).GetHashCode;

        // none of the following is ok, since we would need to capture the receiver.
        Func<int> d1 = default(Span<int>).GetHashCode;

        Func<Type> d2 = default(SpanLike<int>).GetType;

        Func<string> d3 = default(Span<int>).ToString;
    }
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(text);

            comp.VerifyEmitDiagnostics(
                // (10,48): error CS0123: No overload for 'GetHashCode' matches delegate 'Func<int>'
                //         Func<int> d0 = default(TypedReference).GetHashCode;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "GetHashCode").WithArguments("GetHashCode", "System.Func<int>").WithLocation(10, 48),
                // (13,43): error CS0123: No overload for 'GetHashCode' matches delegate 'Func<int>'
                //         Func<int> d1 = default(Span<int>).GetHashCode;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "GetHashCode").WithArguments("GetHashCode", "System.Func<int>").WithLocation(13, 43),
                // (15,48): error CS0123: No overload for 'GetType' matches delegate 'Func<Type>'
                //         Func<Type> d2 = default(SpanLike<int>).GetType;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "GetType").WithArguments("GetType", "System.Func<System.Type>").WithLocation(15, 48),
                // (17,46): error CS0123: No overload for 'ToString' matches delegate 'Func<string>'
                //         Func<string> d3 = default(Span<int>).ToString;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "ToString").WithArguments("ToString", "System.Func<string>").WithLocation(17, 46)
            );
        }

        [Fact]
        public void RefSpanDetectBoxing_NoRef()
        {
            string spanSource = @"
namespace System
{
    public struct Span<T> { }
    public struct ReadOnlySpan<T> { }
}";
            var reference = CreateEmptyCompilation(
                spanSource,
                references: new List<MetadataReference>() { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef },
                options: TestOptions.ReleaseDll);

            reference.VerifyDiagnostics();

            var text = @"
class Program
{
    static void Main()
    {
        object x = new System.Span<int>();
        object y = new System.ReadOnlySpan<byte>();
    }
}
";
            var comp = CreateEmptyCompilation(
                text,
                references: new List<MetadataReference>() { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef, reference.EmitToImageReference() },
                options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RefSpanDetectBoxing_Ref()
        {
            string spanSource = @"
namespace System
{
    public ref struct Span<T> { }
    public ref struct ReadOnlySpan<T> { }
}";
            var reference = CreateEmptyCompilation(
                spanSource,
                references: new List<MetadataReference>() { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef },
                options: TestOptions.ReleaseDll);

            reference.VerifyDiagnostics();

            var text = @"
class Program
{
    static void Main()
    {
        object x = new System.Span<int>();
        object y = new System.ReadOnlySpan<byte>();
    }
}
";
            var comp = CreateEmptyCompilation(
                text,
                references: new List<MetadataReference>() { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef, reference.EmitToImageReference() },
                options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (6,20): error CS0029: Cannot implicitly convert type 'System.Span<int>' to 'object'
                //         object x = new System.Span<int>();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new System.Span<int>()").WithArguments("System.Span<int>", "object").WithLocation(6, 20),
                // (7,20): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<byte>' to 'object'
                //         object y = new System.ReadOnlySpan<byte>();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new System.ReadOnlySpan<byte>()").WithArguments("System.ReadOnlySpan<byte>", "object").WithLocation(7, 20));
        }

        [Fact]
        public void CannotUseNonRefSpan()
        {
            string spanSource = @"
namespace System
{
    public struct Span<T> 
    {
        unsafe public Span(void* pointer, int length)
        {
        }
    }
}";
            var reference = CreateEmptyCompilation(
                spanSource,
                references: new List<MetadataReference>() { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef },
                options: TestOptions.UnsafeReleaseDll);

            reference.VerifyDiagnostics();

            var text = @"
using System;
class Program
{
    static void Main()
    {
        Span<int> x = stackalloc int [10];
    }
}
";
            var comp = CreateEmptyCompilation(
                text,
                references: new List<MetadataReference>() { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef, reference.EmitToImageReference() },
                options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (7,23): error CS8346: Conversion of a stackalloc expression of type 'int' to type 'Span<int>' is not possible.
                //         Span<int> x = stackalloc int [10];
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "stackalloc int [10]").WithArguments("int", "System.Span<int>").WithLocation(7, 23));
        }

        [Fact]
        public void CannotUseNonStructSpan()
        {
            string spanSource = @"
namespace System
{
    public class Span<T> 
    {
        unsafe public Span(void* pointer, int length)
        {
        }
    }
}";
            var reference = CreateEmptyCompilation(
                spanSource,
                references: new List<MetadataReference>() { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef },
                options: TestOptions.UnsafeReleaseDll);

            reference.VerifyDiagnostics();

            var text = @"
using System;
class Program
{
    static void Main()
    {
        Span<int> x = stackalloc int [10];
    }
}
";
            var comp = CreateEmptyCompilation(
                text,
                references: new List<MetadataReference>() { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef, reference.EmitToImageReference() },
                options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (7,23): error CS8346: Conversion of a stackalloc expression of type 'int' to type 'Span<int>' is not possible.
                //         Span<int> x = stackalloc int [10];
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "stackalloc int [10]").WithArguments("int", "System.Span<int>").WithLocation(7, 23));
        }

        [Fact]
        [WorkItem(23627, "https://github.com/dotnet/roslyn/issues/23627")]
        public void CreateVariableFromRefStructFieldInNonRefStruct()
        {
            var code = @"
public ref struct Point
{
}
class Program
{
    public Point field1 = new Point();
    public static Point field2 = new Point();

    void Check()
    {
        var temp1 = field1;
        var temp2 = field2;
    }
}";

            CreateCompilation(code).VerifyDiagnostics(
                // (8,19): error CS8345: Field or auto-implemented property cannot be of type 'Point' unless it is an instance member of a ref struct.
                //     public static Point field2 = new Point();
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "Point").WithArguments("Point").WithLocation(8, 19),
                // (7,12): error CS8345: Field or auto-implemented property cannot be of type 'Point' unless it is an instance member of a ref struct.
                //     public Point field1 = new Point();
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "Point").WithArguments("Point").WithLocation(7, 12));
        }

        [Fact]
        [WorkItem(23627, "https://github.com/dotnet/roslyn/issues/23627")]
        public void CreateVariableFromRefStructFieldInRefStruct()
        {
            var code = @"
public ref struct Point
{
}
ref struct Program
{
    public static Point field1;
    public static Point field2 = new Point();

    public Program(Point p)
    {
        field1 = p;
    }

    void Check()
    {
        var temp1 = field1;
        var temp2 = field2;
    }
}";

            CreateCompilation(code).VerifyDiagnostics(
                // (8,19): error CS8345: Field or auto-implemented property cannot be of type 'Point' unless it is an instance member of a ref struct.
                //     public static Point field2 = new Point();
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "Point").WithArguments("Point").WithLocation(8, 19),
                // (7,19): error CS8345: Field or auto-implemented property cannot be of type 'Point' unless it is an instance member of a ref struct.
                //     public static Point field1;
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "Point").WithArguments("Point").WithLocation(7, 19));
        }

        [Fact]
        [WorkItem(24627, "https://github.com/dotnet/roslyn/issues/24627")]
        public void ArgMixingBogusInstanceCall()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
class Program
{
    ref struct S1
    {
        public void Test(int x) => throw null;       
        public int this[int x] => throw null;        
        public S1(S1 x, int y) => throw null;            
    }
    
    static void Main()
    {
        // these are all errors, we should not be doing escape analysis on them.
        S1.Test(1);
        var x = S1[1];       
        var y = new S1(S1, 1);
    }
}");

            comp.VerifyDiagnostics(
                // (14,9): error CS0120: An object reference is required for the non-static field, method, or property 'Program.S1.Test(int)'
                //         S1.Test(1);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "S1.Test").WithArguments("Program.S1.Test(int)").WithLocation(14, 9),
                // (15,17): error CS0119: 'Program.S1' is a type, which is not valid in the given context
                //         var x = S1[1];       
                Diagnostic(ErrorCode.ERR_BadSKunknown, "S1").WithArguments("Program.S1", "type").WithLocation(15, 17),
                // (16,24): error CS0119: 'Program.S1' is a type, which is not valid in the given context
                //         var y = new S1(S1, 1);
                Diagnostic(ErrorCode.ERR_BadSKunknown, "S1").WithArguments("Program.S1", "type").WithLocation(16, 24)
                );
        }

        [Fact]
        [WorkItem(27874, "https://github.com/dotnet/roslyn/issues/27874")]
        public void PassingSpansToLocals_EscapeScope()
        {
            CompileAndVerify(
                CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    static void Main()
    {
        Span<int> x = stackalloc int [10];
        
        Console.WriteLine(M1(ref x).Length);
        Console.WriteLine(M2(ref x).Length);
    }
    
    static ref Span<int> M1(ref Span<int> x)
    {
        ref Span<int> q = ref x;
        return ref q;
    }
    
    static ref Span<int> M2(ref Span<int> x)
    {
        return ref x;
    }
}",
                options: TestOptions.ReleaseExe), verify: Verification.Fails, expectedOutput: @"
10
10");
        }

        [Fact]
        [WorkItem(27357, "https://github.com/dotnet/roslyn/issues/27357")]
        public void PassingSpansToInParameters_Methods()
        {
            CompileAndVerify(CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    static void Main()
    {
        Span<int> s1 = stackalloc int[1];
        M1(s1);
    }
    
    static void M1(Span<int> s1)
    {
        Span<int> s2 = stackalloc int[2];

        M2(s1, s2);
        M2(s2, s1);

        M2(s1, in s2);
        M2(s2, in s1);

        M2(x: s1, y: in s2);
        M2(x: s2, y: in s1);

        M2(y: in s2, x: s1);
        M2(y: in s1, x: s2);
    }

    static void M2(Span<int> x, in Span<int> y)
    {
        Console.WriteLine(x.Length + "" - "" + y.Length);
    }
}", options: TestOptions.ReleaseExe), verify: Verification.Fails, expectedOutput: @"
1 - 2
2 - 1
1 - 2
2 - 1
1 - 2
2 - 1
1 - 2
2 - 1");
        }

        [Fact]
        [WorkItem(27357, "https://github.com/dotnet/roslyn/issues/27357")]
        public void PassingSpansToInParameters_Indexers()
        {
            CompileAndVerify(CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    static void Main()
    {
        Span<int> s1 = stackalloc int[1];
        M1(s1);
    }
    
    static void M1(Span<int> s1)
    {
        var obj = new C();
        Span<int> s2 = stackalloc int[2];

        Console.WriteLine(obj[s1, s2]);
        Console.WriteLine(obj[s2, s1]);

        Console.WriteLine(obj[s1, in s2]);
        Console.WriteLine(obj[s2, in s1]);

        Console.WriteLine(obj[x: s1, y: in s2]);
        Console.WriteLine(obj[x: s2, y: in s1]);

        Console.WriteLine(obj[y: in s2, x: s1]);
        Console.WriteLine(obj[y: in s1, x: s2]);
    }

    string this[Span<int> x, in Span<int> y] => x.Length + "" - "" + y.Length;
}", options: TestOptions.ReleaseExe), verify: Verification.Fails, expectedOutput: @"
1 - 2
2 - 1
1 - 2
2 - 1
1 - 2
2 - 1
1 - 2
2 - 1");
        }

        [Fact]
        [WorkItem(27357, "https://github.com/dotnet/roslyn/issues/27357")]
        public void PassingSpansToParameters_Errors()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    static void Main()
    {
        Span<int> s1 = stackalloc int[1];
        M1(s1);
    }
    
    static void M1(Span<int> s1)
    {
        var obj = new C();
        Span<int> s2 = stackalloc int[2];

        M2(ref s1, out s2);         // one
        M2(ref s2, out s1);         // two

        M2(ref s1, out s2);         // three
        M2(ref s2, out s1);         // four

        M2(y: out s2, x: ref s1);   // five
        M2(y: out s1, x: ref s2);   // six

        M2(ref s1, out s1);         // should be ok
        M2(ref s2, out s2);         // should be ok
    }

    static void M2(ref Span<int> x, out Span<int> y)
    {
        y = default;
    }
}").VerifyDiagnostics(
                // (16,24): error CS8352: Cannot use local 's2' in this context because it may expose referenced variables outside of their declaration scope
                //         M2(ref s1, out s2);         // one
                Diagnostic(ErrorCode.ERR_EscapeLocal, "s2").WithArguments("s2").WithLocation(16, 24),
                // (16,9): error CS8350: This combination of arguments to 'C.M2(ref Span<int>, out Span<int>)' is disallowed because it may expose variables referenced by parameter 'y' outside of their declaration scope
                //         M2(ref s1, out s2);         // one
                Diagnostic(ErrorCode.ERR_CallArgMixing, "M2(ref s1, out s2)").WithArguments("C.M2(ref System.Span<int>, out System.Span<int>)", "y").WithLocation(16, 9),
                // (17,16): error CS8352: Cannot use local 's2' in this context because it may expose referenced variables outside of their declaration scope
                //         M2(ref s2, out s1);         // two
                Diagnostic(ErrorCode.ERR_EscapeLocal, "s2").WithArguments("s2").WithLocation(17, 16),
                // (17,9): error CS8350: This combination of arguments to 'C.M2(ref Span<int>, out Span<int>)' is disallowed because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         M2(ref s2, out s1);         // two
                Diagnostic(ErrorCode.ERR_CallArgMixing, "M2(ref s2, out s1)").WithArguments("C.M2(ref System.Span<int>, out System.Span<int>)", "x").WithLocation(17, 9),
                // (19,24): error CS8352: Cannot use local 's2' in this context because it may expose referenced variables outside of their declaration scope
                //         M2(ref s1, out s2);         // three
                Diagnostic(ErrorCode.ERR_EscapeLocal, "s2").WithArguments("s2").WithLocation(19, 24),
                // (19,9): error CS8350: This combination of arguments to 'C.M2(ref Span<int>, out Span<int>)' is disallowed because it may expose variables referenced by parameter 'y' outside of their declaration scope
                //         M2(ref s1, out s2);         // three
                Diagnostic(ErrorCode.ERR_CallArgMixing, "M2(ref s1, out s2)").WithArguments("C.M2(ref System.Span<int>, out System.Span<int>)", "y").WithLocation(19, 9),
                // (20,16): error CS8352: Cannot use local 's2' in this context because it may expose referenced variables outside of their declaration scope
                //         M2(ref s2, out s1);         // four
                Diagnostic(ErrorCode.ERR_EscapeLocal, "s2").WithArguments("s2").WithLocation(20, 16),
                // (20,9): error CS8350: This combination of arguments to 'C.M2(ref Span<int>, out Span<int>)' is disallowed because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         M2(ref s2, out s1);         // four
                Diagnostic(ErrorCode.ERR_CallArgMixing, "M2(ref s2, out s1)").WithArguments("C.M2(ref System.Span<int>, out System.Span<int>)", "x").WithLocation(20, 9),
                // (22,19): error CS8352: Cannot use local 's2' in this context because it may expose referenced variables outside of their declaration scope
                //         M2(y: out s2, x: ref s1);   // five
                Diagnostic(ErrorCode.ERR_EscapeLocal, "s2").WithArguments("s2").WithLocation(22, 19),
                // (22,9): error CS8350: This combination of arguments to 'C.M2(ref Span<int>, out Span<int>)' is disallowed because it may expose variables referenced by parameter 'y' outside of their declaration scope
                //         M2(y: out s2, x: ref s1);   // five
                Diagnostic(ErrorCode.ERR_CallArgMixing, "M2(y: out s2, x: ref s1)").WithArguments("C.M2(ref System.Span<int>, out System.Span<int>)", "y").WithLocation(22, 9),
                // (23,30): error CS8352: Cannot use local 's2' in this context because it may expose referenced variables outside of their declaration scope
                //         M2(y: out s1, x: ref s2);   // six
                Diagnostic(ErrorCode.ERR_EscapeLocal, "s2").WithArguments("s2").WithLocation(23, 30),
                // (23,9): error CS8350: This combination of arguments to 'C.M2(ref Span<int>, out Span<int>)' is disallowed because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         M2(y: out s1, x: ref s2);   // six
                Diagnostic(ErrorCode.ERR_CallArgMixing, "M2(y: out s1, x: ref s2)").WithArguments("C.M2(ref System.Span<int>, out System.Span<int>)", "x").WithLocation(23, 9));
        }

        [Fact]
        [WorkItem(27357, "https://github.com/dotnet/roslyn/issues/27357")]
        public void PassingSpansToParameters_Errors_Arglist()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    static void Main()
    {
        Span<int> s1 = stackalloc int[1];
        M1(s1);
    }
    
    static void M1(Span<int> s1)
    {
        var obj = new C();
        Span<int> s2 = stackalloc int[2];

        M2(__arglist(ref s1, ref s2));  // one
        M2(__arglist(ref s2, ref s1));  // two
    }

    static void M2(__arglist)
    {
    }
}").VerifyDiagnostics(
                // (16,34): error CS8352: Cannot use local 's2' in this context because it may expose referenced variables outside of their declaration scope
                //         M2(__arglist(ref s1, ref s2));  // one
                Diagnostic(ErrorCode.ERR_EscapeLocal, "s2").WithArguments("s2").WithLocation(16, 34),
                // (16,9): error CS8350: This combination of arguments to 'C.M2(__arglist)' is disallowed because it may expose variables referenced by parameter '__arglist' outside of their declaration scope
                //         M2(__arglist(ref s1, ref s2));  // one
                Diagnostic(ErrorCode.ERR_CallArgMixing, "M2(__arglist(ref s1, ref s2))").WithArguments("C.M2(__arglist)", "__arglist").WithLocation(16, 9),
                // (17,26): error CS8352: Cannot use local 's2' in this context because it may expose referenced variables outside of their declaration scope
                //         M2(__arglist(ref s2, ref s1));  // two
                Diagnostic(ErrorCode.ERR_EscapeLocal, "s2").WithArguments("s2").WithLocation(17, 26),
                // (17,9): error CS8350: This combination of arguments to 'C.M2(__arglist)' is disallowed because it may expose variables referenced by parameter '__arglist' outside of their declaration scope
                //         M2(__arglist(ref s2, ref s1));  // two
                Diagnostic(ErrorCode.ERR_CallArgMixing, "M2(__arglist(ref s2, ref s1))").WithArguments("C.M2(__arglist)", "__arglist").WithLocation(17, 9));
        }

        [Fact]
        [WorkItem(44588, "https://github.com/dotnet/roslyn/issues/44588")]
        public void SwitchDefaultLocalInitialization_01()
        {
            var source = @"
using System;

struct Struct2
{
    public static ReadOnlySpan<T> GetSpan1<T>(int x)
    {
        return x switch
        {
            0 => default,
            _ => default,
        };
    }

    public static ReadOnlySpan<T> GetSpan2<T>(int x)
    {
        ReadOnlySpan<T> span;

        span = x switch
        {
            0 => default,
            _ => default,
        };

        return span;
    }

    public static ReadOnlySpan<T> GetSpan3<T>(int x)
    {
        ReadOnlySpan<T> span = x switch
        {
            0 => default,
            _ => default,
        };

        return span;
    }

    public static ReadOnlySpan<T> GetSpan4<T>(int x)
    {
        ReadOnlySpan<T> span = default;

        return span;
    }
}";
            CreateCompilationWithMscorlibAndSpan(source).VerifyDiagnostics(
                );
        }

        [Fact]
        [WorkItem(44588, "https://github.com/dotnet/roslyn/issues/44588")]
        public void SwitchDefaultLocalInitialization_02()
        {
            var source = @"
using System;

struct Struct2
{
    public static Span<byte> GetSpan1(int x)
    {
        return x switch
        {
            0 => stackalloc byte[10], // 1
            _ => default,
        };
    }

    public static Span<byte> GetSpan2(int x)
    {
        Span<byte> span;

        span = x switch
        {
            0 => stackalloc byte[10], // 2
            _ => default,
        };

        return span;
    }

    public static Span<byte> GetSpan3(int x)
    {
        Span<byte> span = x switch
        {
            0 => stackalloc byte[10],
            _ => default,
        };

        return span; // 3
    }

    public static Span<byte> GetSpan4(int x)
    {
        Span<byte> span = stackalloc byte[10];

        return span; // 4
    }
}";
            CreateCompilationWithMscorlibAndSpan(source).VerifyDiagnostics(
                // (10,18): error CS8353: A result of a stackalloc expression of type 'Span<byte>' cannot be used in this context because it may be exposed outside of the containing method
                //             0 => stackalloc byte[10], // 1
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc byte[10]").WithArguments("System.Span<byte>").WithLocation(10, 18),
                // (21,18): error CS8353: A result of a stackalloc expression of type 'Span<byte>' cannot be used in this context because it may be exposed outside of the containing method
                //             0 => stackalloc byte[10], // 2
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc byte[10]").WithArguments("System.Span<byte>").WithLocation(21, 18),
                // (36,16): error CS8352: Cannot use local 'span' in this context because it may expose referenced variables outside of their declaration scope
                //         return span; // 3
                Diagnostic(ErrorCode.ERR_EscapeLocal, "span").WithArguments("span").WithLocation(36, 16),
                // (43,16): error CS8352: Cannot use local 'span' in this context because it may expose referenced variables outside of their declaration scope
                //         return span; // 4
                Diagnostic(ErrorCode.ERR_EscapeLocal, "span").WithArguments("span").WithLocation(43, 16)
                );
        }

        [Fact]
        [WorkItem(39663, "https://github.com/dotnet/roslyn/issues/39663")]
        public void AssignToDiscard_01()
        {
            var text = @"
using System;

class Program
{
    static void Main()
    {
        _ = Test(stackalloc int[5]);
        System.Console.WriteLine(""Done"");
    }

    static Span<int> Test(Span<int> items) => items;
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(text, options: TestOptions.DebugExe);
            CompileAndVerify(comp, verify: Verification.Fails, expectedOutput: "Done").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(39663, "https://github.com/dotnet/roslyn/issues/39663")]
        public void AssignToDiscard_02()
        {
            var text = @"
using System;

class Program
{
    static int[] _array = new int[] {};

    static void Main()
    {
        _ = Test;
        System.Console.WriteLine(""Done"");
    }

    static Span<int> Test => _array;
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(text, options: TestOptions.DebugExe);
            // ILVerify: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator.
            CompileAndVerify(comp, expectedOutput: "Done", verify: Verification.FailsILVerify).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(39663, "https://github.com/dotnet/roslyn/issues/39663")]
        public void AssignToDiscard_03()
        {
            var text = @"
using System;

class Program
{
    static void Main()
    {
        var l = Test(stackalloc int[5]);
        _ = l;
        System.Console.WriteLine(""Done"");
    }

    static Span<int> Test(Span<int> items) => items;
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(text, options: TestOptions.DebugExe);
            CompileAndVerify(comp, verify: Verification.Fails, expectedOutput: "Done").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(39663, "https://github.com/dotnet/roslyn/issues/39663")]
        public void AssignToDiscard_04()
        {
            var text = @"
using System;

class Program
{
    static void Main()
    {
        var l = Test(stackalloc int[5]);

        {
            int i = 0;
            _ = l;
            i++;
        }

        System.Console.WriteLine(""Done"");
    }

    static Span<int> Test(Span<int> items) => items;
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(text, options: TestOptions.DebugExe);
            CompileAndVerify(comp, verify: Verification.Fails, expectedOutput: "Done").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(39663, "https://github.com/dotnet/roslyn/issues/39663")]
        public void AssignToDiscard_05()
        {
            var text = @"
using System;

class Program
{
    static void Main()
    {
        Test(stackalloc int[5]);
        System.Console.WriteLine(""Done"");
    }

    static Span<int> Test(Span<int> items) => items;
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(text, options: TestOptions.DebugExe);
            CompileAndVerify(comp, verify: Verification.Fails, expectedOutput: "Done").VerifyDiagnostics();
        }
    }
}
