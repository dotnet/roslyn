// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.RefLocalsReturns)]
    public class RefLocalsAndReturnsTests : CompilingTestBase
    {
        [Fact]
        public void ReassignExpressionTree()
        {
            var comp = CreateCompilation(@"
using System;
using System.Linq.Expressions;
class C
{
    void M()
    {
        int x = 0;
        ref int rx = ref x;
        Expression<Func<int>> e = () => (rx = ref x);
    }
}");
            comp.VerifyDiagnostics(
                // (10,42): error CS8175: Cannot use ref local 'rx' inside an anonymous method, lambda expression, or query expression
                //         Expression<Func<int>> e = () => (rx = ref x);
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseLocal, "rx").WithArguments("rx").WithLocation(10, 42));
        }

        [Fact]
        public void RefEscapeInFor()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(ref int r1)
    {
        int x = 0;
        ref int rx = ref x;
        for (int i = 0; i < (r1 = ref rx); i++)
        {
        }
        for (int i = 0; i < 5; (r1 = ref rx)++)
        {
        }
    }
}");
            comp.VerifyDiagnostics(
                // (8,30): error CS8374: Cannot ref-assign 'rx' to 'r1' because 'rx' has a narrower escape scope than 'r1'.
                //         for (int i = 0; i < (r1 = ref rx); i++)
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "r1 = ref rx").WithArguments("r1", "rx").WithLocation(8, 30),
                // (11,33): error CS8374: Cannot ref-assign 'rx' to 'r1' because 'rx' has a narrower escape scope than 'r1'.
                //         for (int i = 0; i < 5; (r1 = ref rx)++)
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "r1 = ref rx").WithArguments("r1", "rx").WithLocation(11, 33));
        }

        [Fact]
        public void RefForMultipleDeclarations()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        int x = 0;
        for (ref int rx = ref x, ry = ref x;;) 
        { 
            rx += ry;
        }
    }
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RefForNoInitializer()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        int i = 0;
        for (ref int rx; i < 5; i++) { }
    }
}");
            comp.VerifyDiagnostics(
                // (7,22): error CS8174: A declaration of a by-reference variable must have an initializer
                //         for (ref int rx; i < 5; i++) { }
                Diagnostic(ErrorCode.ERR_ByReferenceVariableMustBeInitialized, "rx").WithLocation(7, 22),
                // (7,22): warning CS0168: The variable 'rx' is declared but never used
                //         for (ref int rx; i < 5; i++) { }
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "rx").WithArguments("rx").WithLocation(7, 22));
        }

        [Fact]
        public void RefReassignVolatileField()
        {
            var comp = CreateCompilation(@"
class C
{
    volatile int _f;
    void M()
    {
        ref int rx = ref _f;
        rx = ref _f;
    }
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RefReassignDynamic()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(ref dynamic rd)
    {
        ref var rd2 = ref rd.Length; // Legal
        rd = ref rd.Length; // Error, escape scope is local
    }
}");
            comp.VerifyDiagnostics(
                // (7,9): error CS8374: Cannot ref-assign 'rd.Length' to 'rd' because 'rd.Length' has a narrower escape scope than 'rd'.
                //         rd = ref rd.Length; // Error, escape scope is local
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "rd = ref rd.Length").WithArguments("rd", "rd.Length").WithLocation(7, 9));
        }

        [Fact]
        public void RefReassignIsUse()
        {
            var comp = CreateCompilation(@"
class C
{
    private int _f = 0;

    void M()
    {
        int x = 0, y = 0;
        ref int rx = ref x;
        rx = ref y;
        rx = ref _f;
    }
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NoRefReassignThisInStruct()
        {
            var comp = CreateCompilation(@"
struct S
{
    void M(ref S s)
    {
        s = ref this;
        this = ref s;
    }
}");
            comp.VerifyDiagnostics(
                // (6,9): error CS8374: Cannot ref-assign 'this' to 's' because 'this' has a narrower escape scope than 's'.
                //         s = ref this;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "s = ref this").WithArguments("s", "this").WithLocation(6, 9),
                // (7,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         this = ref s;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "this").WithLocation(7, 9));
        }

        [Fact]
        public void RefReassignLifetimeIsLHS()
        {
            var comp = CreateCompilation(@"
class C
{
    ref int M()
    {
        int x = 0;
        ref int rx = ref x;
        return ref (rx = ref (new int[1])[0]);
    }
}");
            comp.VerifyDiagnostics(
                // (8,21): error CS8157: Cannot return 'rx' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref (rx = ref (new int[1])[0]);
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "rx = ref (new int[1])[0]").WithArguments("rx").WithLocation(8, 21));
        }

        [Fact]
        public void InReassignmentWithConversion()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(string s)
    {
        ref string rs = ref s;
        M2(in (rs = ref s));
    }
    void M2(in object o) {}
}");
            comp.VerifyDiagnostics(
                // (7,16): error CS1503: Argument 1: cannot convert from 'in string' to 'in object'
                //         M2(in (rs = ref s));
                Diagnostic(ErrorCode.ERR_BadArgType, "rs = ref s").WithArguments("1", "in string", "in object").WithLocation(7, 16));
        }

        [Fact]
        public void RefEscapeInForeach()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    ref int M(Span<int> s)
    {
        foreach (ref int x in s)
        {
            if (x == 0)
            {
                return ref x; // OK
            }
        }

        Span<int> s2 = stackalloc int[10];
        foreach (ref int x in s2)
        {
            if (x == 0)
            {
                return ref x; // error
            }
        }

        return ref s[0];
    }
}");
            comp.VerifyDiagnostics(
                // (20,28): error CS8157: Cannot return 'x' by reference because it was initialized to a value that cannot be returned by reference
                //                 return ref x; // error
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "x").WithArguments("x").WithLocation(20, 28));
        }

        [Fact]
        public void RefFor72()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(int x)
    {
        for (ref int rx = ref x; x < 0; x++) { }
    }
}", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_2));
            comp.VerifyDiagnostics(
                // (6,14): error CS8320: Feature 'ref for-loop variables' is not available in C# 7.2. Please use language version 7.3 or greater.
                //         for (ref int rx = ref x; x < 0; x++) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "ref int").WithArguments("ref for-loop variables", "7.3").WithLocation(6, 14));
        }

        [Fact]
        public void RefForEach72()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    void M(Span<int> span)
    {
        foreach (ref int x in span) { }
    }
}", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_2));
            comp.VerifyDiagnostics(
                // (7,18): error CS8320: Feature 'ref foreach iteration variables' is not available in C# 7.2. Please use language version 7.3 or greater.
                //         foreach (ref int x in span) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "ref int").WithArguments("ref foreach iteration variables", "7.3").WithLocation(7, 18));
        }

        [Fact]
        public void RefReturnRefExpression()
        {
            var comp = CreateCompilation(@"
class C
{
    readonly int _ro = 42;
    int _rw = 42;

    ref int M1(ref int rrw) => ref (rrw = ref _rw);

    ref readonly int M2(in int rro) => ref (rro = ref _ro);

    ref readonly int M3(in int rro) => ref (rro = ref _rw);

    ref int M4(in int rro) => ref (rro = ref _rw);

    ref int M5(ref int rrw) => ref (rrw = ref _ro);
}");
            comp.VerifyDiagnostics(
                // (13,36): error CS8333: Cannot return variable 'rro' by writable reference because it is a readonly variable
                //     ref int M4(in int rro) => ref (rro = ref _rw);
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "rro = ref _rw").WithArguments("variable", "rro").WithLocation(13, 36),
                // (15,47): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     ref int M5(ref int rrw) => ref (rrw = ref _ro);
                Diagnostic(ErrorCode.ERR_AssgReadonly, "_ro").WithLocation(15, 47));
        }

        [Fact, WorkItem(42259, "https://github.com/dotnet/roslyn/issues/42259")]
        public void RefReturnLocalFunction()
        {
            var source = @"
#pragma warning disable CS8321
class C {
    static void M(){
        ref int M1(in int i) => ref i;
        ref int M2(in int i) { return ref i; }
        ref readonly int M3(in int i) => ref i;
        ref readonly int M4(in int i) { return ref i; }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,37): error CS8333: Cannot return variable 'i' by writable reference because it is a readonly variable
                //         ref int M1(in int i) => ref i;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "i").WithArguments("variable", "i").WithLocation(5, 37),
                // (6,43): error CS8333: Cannot return variable 'i' by writable reference because it is a readonly variable
                //         ref int M2(in int i) { return ref i; }
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyNotField, "i").WithArguments("variable", "i").WithLocation(6, 43)
            );
        }

        [Fact, WorkItem(42259, "https://github.com/dotnet/roslyn/issues/42259")]
        public void RefReadonlyReturnLocalFunction()
        {
            var source = @"
#pragma warning disable CS8321
class C {
    ref int M(){
        throw new System.Exception();
        ref readonly int M1(in int i) => ref i;
        ref readonly int M2(in int i) { return ref i; }
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void ReadonlyFieldRefReassign()
        {
            var comp = CreateCompilation(@"
class C
{
    readonly int _ro = 42;
    int _rw = 42;

    void M()
    {
        ref readonly var rro = ref _ro;
        ref var rrw = ref _rw;

        rrw = ref (rro = ref _ro);
        rrw = ref (rro = ref rrw);

        rrw = ref (true
                    ? ref (rro = ref _rw)
                    : ref (rrw = ref _rw));
    }
}");
            comp.VerifyDiagnostics(
                // (12,20): error CS1510: A ref or out value must be an assignable variable
                //         rrw = ref (rro = ref _ro);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "rro = ref _ro").WithLocation(12, 20),
                // (13,20): error CS1510: A ref or out value must be an assignable variable
                //         rrw = ref (rro = ref rrw);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "rro = ref rrw").WithLocation(13, 20),
                // (16,28): error CS1510: A ref or out value must be an assignable variable
                //                     ? ref (rro = ref _rw)
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "rro = ref _rw").WithLocation(16, 28),
                // (15,20): error CS1510: A ref or out value must be an assignable variable
                //         rrw = ref (true
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, @"true
                    ? ref (rro = ref _rw)
                    : ref (rrw = ref _rw)").WithLocation(15, 20));
        }

        [Fact]
        public void RefForeachErrorRecovery()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        foreach (ref var x in )
        {
        }
    }
}");
            comp.VerifyDiagnostics(
                // (6,31): error CS1525: Invalid expression term ')'
                //         foreach (ref var x in )
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(6, 31));
        }

        [Fact]
        public void RefForToReadonly()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(in int x)
    {
        for (ref int i = ref x; i < 0; i++) {}
    }
}");
            comp.VerifyDiagnostics(
                // (6,30): error CS8329: Cannot use variable 'x' as a ref or out value because it is a readonly variable
                //         for (ref int i = ref x; i < 0; i++) {}
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "x").WithArguments("variable", "x").WithLocation(6, 30));
        }

        [Fact]
        public void RefForOutOfScope()
        {
            var comp = CreateCompilation(@"
class C
{
    ref int M()
    {
        int x = 0;
        for (ref int i = ref x; i < 0; i++)
        {
            if (i == 0)
            {
                return ref i;
            }
        }
        return ref (new int[1])[0];
    }
}");
            comp.VerifyDiagnostics(
                // (11,28): error CS8157: Cannot return 'i' by reference because it was initialized to a value that cannot be returned by reference
                //                 return ref i;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "i").WithArguments("i").WithLocation(11, 28));
        }

        [Fact]
        public void RefForeachReadonly()
        {
            var comp = CreateCompilation(@"
using System;
class C
{
    void M()
    {
        foreach (ref var v in new int[0])
        {
        }
        foreach (ref readonly var v in new int[0])
        {
        }
        foreach (ref var v in new RefEnumerable())
        {
            Console.WriteLine(v);
        }
    }
}

class RefEnumerable
{
    private readonly int[] _arr = new int[5];
    public StructEnum GetEnumerator() => new StructEnum(_arr);

    public struct StructEnum
    {
        private readonly int[] _arr;
        private int _current;
        public StructEnum(int[] arr)
        {
            _arr = arr;
            _current = -1;
        }
        public ref readonly int Current => ref _arr[_current];
        public bool MoveNext() => ++_current != _arr.Length;
    }
}");
            comp.VerifyDiagnostics(
                // (7,31): error CS1510: A ref or out value must be an assignable variable
                //         foreach (ref var v in new int[0])
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "new int[0]").WithLocation(7, 31),
                // (10,40): error CS1510: A ref or out value must be an assignable variable
                //         foreach (ref readonly var v in new int[0])
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "new int[0]").WithLocation(10, 40),
                // (13,31): error CS8331: Cannot assign to method 'Current.get' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         foreach (ref var v in new RefEnumerable())
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "new RefEnumerable()").WithArguments("method", "Current.get").WithLocation(13, 31));
        }

        [Fact]
        public void RefReassignIdentityConversion()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        string s = ""s"";
        object o = s;
        ref string rs = ref s;
        ref object ro = ref o;

        rs = ref (string)o;
        ro = ref s;
        ro = s;
    }
}");
            comp.VerifyDiagnostics(
                // (11,18): error CS1510: A ref or out value must be an assignable variable
                //         rs = ref (string)o;
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "(string)o").WithLocation(11, 18),
                // (12,18): error CS8173: The expression must be of type 'object' because it is being assigned by reference
                //         ro = ref s;
                Diagnostic(ErrorCode.ERR_RefAssignmentMustHaveIdentityConversion, "s").WithArguments("object").WithLocation(12, 18));
        }

        [Fact]
        public void RefReassign71()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    static int _f = 0;
    void M(ref int x, out int o)
    {
        x = ref _f;

        ref int z = ref x;
        z = ref _f;
        o = 0;
        o = ref _f;
    }
}", CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_2));
            CreateCompilation(tree).VerifyDiagnostics(
                // (7,13): error CS8320: Feature 'ref reassignment' is not available in C# 7.2. Please use language version 7.3 or greater.
                //         x = ref _f;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "ref").WithArguments("ref reassignment", "7.3").WithLocation(7, 13),
                // (10,13): error CS8320: Feature 'ref reassignment' is not available in C# 7.2. Please use language version 7.3 or greater.
                //         z = ref _f;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "ref").WithArguments("ref reassignment", "7.3").WithLocation(10, 13),
                // (12,13): error CS8320: Feature 'ref reassignment' is not available in C# 7.2. Please use language version 7.3 or greater.
                //         o = ref _f;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_2, "ref").WithArguments("ref reassignment", "7.3").WithLocation(12, 13));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefReassignSpanLifetime(LanguageVersion languageVersion)
        {
            string source = @"using System;

class C
{
    void M(ref Span<int> s)
    {
        Span<int> s2 = new Span<int>(new int[10]);
        s = ref s2; // Illegal, narrower escape scope

        s2 = stackalloc int[10]; // Illegal, narrower lifetime

        Span<int> s3 = stackalloc int[10];
        s = ref s3; // Illegal, narrower escape scope
    }
}";
            var comp = CreateCompilationWithMscorlibAndSpan(new[] { source, UnscopedRefAttributeDefinition }, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics(
                // (8,9): error CS8374: Cannot ref-assign 's2' to 's' because 's2' has a narrower escape scope than 's'.
                //         s = ref s2; // Illegal, narrower escape scope
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "s = ref s2").WithArguments("s", "s2").WithLocation(8, 9),
                // (10,14): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //         s2 = stackalloc int[10]; // Illegal, narrower lifetime
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[10]").WithArguments("System.Span<int>").WithLocation(10, 14),
                // (13,9): error CS8374: Cannot ref-assign 's3' to 's' because 's3' has a narrower escape scope than 's'.
                //         s = ref s3; // Illegal, narrower escape scope
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "s = ref s3").WithArguments("s", "s3").WithLocation(13, 9));
        }

        [Fact]
        public void RefReassignReferenceLocalToParam()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(ref string s)
    {
        var s2 = string.Empty;
        s = ref s2;
    }
}");
            comp.VerifyDiagnostics(
                // (7,9): error CS8356: Cannot ref-assign 's2' to 's' because 's2' has a narrower escape scope than 's'.
                //         s = ref s2;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "s = ref s2").WithArguments("s", "s2").WithLocation(7, 9));
        }

        [Fact]
        public void RefAssignReferencePropertyToParam()
        {
            var comp = CreateCompilation(@"
class C
{
    string s2 => string.Empty;

    void M(ref string s)
    {
        s = ref s2;
    }
}");
            comp.VerifyDiagnostics(
                // (8,17): error CS1510: A ref or out value must be an assignable variable
                //         s = ref s2;
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "s2").WithLocation(8, 17));
        }

        [Fact]
        public void RefAssignReferenceFileToParam()
        {
            var comp = CreateCompilation(@"
class C
{
    string _s2 = string.Empty;

    void M(ref string s)
    {
        s = ref _s2;
    }
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RefAssignStaticField()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(ref string s)
    {
        s = ref string.Empty;
    }
}");
            comp.VerifyDiagnostics(
                // (6,17): error CS0198: A static readonly field cannot be assigned to (except in a static constructor or a variable initializer)
                //         s = ref string.Empty;
                Diagnostic(ErrorCode.ERR_AssgReadonlyStatic, "string.Empty").WithLocation(6, 17));
        }

        [Fact]
        public void RefReassignForEach()
        {
            var comp = CreateCompilation(@"
using System;
class E
{
    public class Enumerator
    {
        public ref int Current => throw new NotImplementedException();
        public bool MoveNext() => throw new NotImplementedException();
    }

    public Enumerator GetEnumerator() => new Enumerator();
}

class C
{
    void M()
    {
        foreach (ref int x in new E())
        {
            int y = 0;
            x = ref y;
        }
    }
}");
            comp.VerifyDiagnostics(
                // (21,13): error CS1656: Cannot assign to 'x' because it is a 'foreach iteration variable'
                //             x = ref y;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "x").WithArguments("x", "foreach iteration variable").WithLocation(21, 13));
        }

        [Fact]
        public void RefReassignNarrowLifetime()
        {
            var comp = CreateCompilation(@"
class C
{
    int _x = 0;
    void M()
    {
        int y = 0;
        ref int rx = ref _x;
        rx = ref y;
    }
}");
            comp.VerifyDiagnostics(
                // (9,9): error CS8356: Cannot ref-assign 'y' to 'rx' because 'y' has a narrower escape scope than 'rx'.
                //         rx = ref y;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "rx = ref y").WithArguments("rx", "y").WithLocation(9, 9));
        }

        [Fact]
        public void RefReassignRangeVar()
        {
            var comp = CreateCompilation(@"
using System;
using System.Linq;
class C
{
    void M()
    {
        _ = from c in ""test"" select (Action)(() =>
        {
            int x = 0;
            ref int rx = ref x;
            rx = ref c;
            c = ref x;
        });
    }
}");
            comp.VerifyDiagnostics(
                // (12,22): error CS1939: Cannot pass the range variable 'c' as an out or ref parameter
                //             rx = ref c;
                Diagnostic(ErrorCode.ERR_QueryOutRefRangeVariable, "c").WithArguments("c").WithLocation(12, 22),
                // (13,13): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //             c = ref x;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "c").WithLocation(13, 13));
        }

        [Fact]
        public void RefReassignOutDefiniteAssignment()
        {
            var comp = CreateCompilation(@"
class C
{
    static int y = 0;
    void M(out int x)
    {
        x = ref y;
        x = 0;
    }
}");
            comp.VerifyDiagnostics(
                // (7,9): error CS0177: The out parameter 'x' must be assigned to before control leaves the current method
                //         x = ref y;
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "x").WithArguments("x").WithLocation(7, 9));
        }

        [Fact]
        public void RefReassignOutDefiniteAssignment2()
        {
            var source = @"
class C
{
    void M(out int x)
    {
        x = 0;
        int y;
        x = ref y;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (8,9): error CS8356: Cannot ref-assign 'y' to 'x' because 'y' has a narrower escape scope than 'x'.
                //         x = ref y;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "x = ref y").WithArguments("x", "y").WithLocation(8, 9),
                // (8,17): error CS0165: Use of unassigned local variable 'y'
                //         x = ref y;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(8, 17));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,17): error CS0165: Use of unassigned local variable 'y'
                //         x = ref y;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(8, 17));
        }

        [Fact]
        public void RefReassignParamEscape()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(ref int x)
    {
        int y = 0;
        x = ref y;

        ref int z = ref y;
        x = ref z;
    }
}");
            comp.VerifyDiagnostics(
                // (7,9): error CS8356: Cannot ref-assign 'y' to 'x' because 'y' has a narrower escape scope than 'x'.
                //         x = ref y;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "x = ref y").WithArguments("x", "y").WithLocation(7, 9),
                // (10,9): error CS8356: Cannot ref-assign 'z' to 'x' because 'z' has a narrower escape scope than 'x'.
                //         x = ref z;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "x = ref z").WithArguments("x", "z").WithLocation(10, 9));
        }

        [Fact]
        public void RefReassignParamEscape_UnsafeContext()
        {
            var comp = CreateCompilation(@"
class C
{
    unsafe void M(ref int x)
    {
        int y = 0;
        x = ref y;

        ref int z = ref y;
        x = ref z;
    }
}", options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (7,9): warning CS9085: This ref-assigns 'y' to 'x' but 'y' has a narrower escape scope than 'x'.
                //         x = ref y;
                Diagnostic(ErrorCode.WRN_RefAssignNarrower, "x = ref y").WithArguments("x", "y").WithLocation(7, 9),
                // (10,9): warning CS9085: This ref-assigns 'z' to 'x' but 'z' has a narrower escape scope than 'x'.
                //         x = ref z;
                Diagnostic(ErrorCode.WRN_RefAssignNarrower, "x = ref z").WithArguments("x", "z").WithLocation(10, 9)
                );
        }

        [Fact]
        public void RefReassignThisStruct()
        {
            var comp = CreateCompilation(@"
struct S
{
    int _f;
    public S(int x) { _f = x; }

    void M()
    {
        var s = new S(0);
        this = ref s;

        ref var s2 = ref s;
        this = ref s2;

        ref readonly var s3 = ref s;
        this = ref s3;

        this = ref (new S[1])[0];

        ref var s4 = ref (new S[1])[0];
        this = ref s4;

        ref readonly var s5 = ref (new S[1])[0];
        this = ref s5;
    }
}");
            comp.VerifyDiagnostics(
                // (10,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         this = ref s;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "this").WithLocation(10, 9),
                // (13,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         this = ref s2;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "this").WithLocation(13, 9),
                // (16,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         this = ref s3;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "this").WithLocation(16, 9),
                // (18,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         this = ref (new S[1])[0];
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "this").WithLocation(18, 9),
                // (21,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         this = ref s4;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "this").WithLocation(21, 9),
                // (24,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         this = ref s5;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "this").WithLocation(24, 9));
        }

        [Fact]
        public void RefReassignThisClass()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        this = ref (new int[1])[0];
    }
}");
            comp.VerifyDiagnostics(
                // (6,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         this = ref (new int[1])[0];
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "this").WithLocation(6, 9));
        }

        [Fact]
        public void RefReassignField()
        {
            var comp = CreateCompilation(@"
class C
{
    int _f = 0;
    void M()
    {
        _f = ref (new int[1])[0];
    }
}");
            comp.VerifyDiagnostics(
                // (7,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         _f = ref (new int[1])[0];
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "_f").WithLocation(7, 9));
        }

        [Fact]
        public void RefReassignOperatorResult()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        int x = 0;
        (2 + 3) = ref x;
    }
}");
            comp.VerifyDiagnostics(
                // (7,10): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         (2 + 3) = ref x;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "2 + 3").WithLocation(7, 10));
        }

        [Fact]
        public void RefReassignToValue()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        int x = 0;
        int y = 0;
        x = ref y;
    }
}");
            comp.VerifyDiagnostics(
                // (8,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         x = ref y;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "x").WithLocation(8, 9));
        }

        [Fact]
        public void RefReassignWithReadonly()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        int x = 0;
        ref int rx = ref x;
        ref readonly int rrx = ref x;
        rx = ref rrx;
    }
}");
            comp.VerifyDiagnostics(
                // (9,18): error CS1510: A ref or out value must be an assignable variable
                //         rx = ref rrx;
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "rrx").WithLocation(9, 18));
        }

        [Fact]
        public void RefReassignToRefReturn()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        int x = 0;
        ref int rx = ref M2();
        M2() = ref x;
    }
    ref int M2() => ref (new int[1])[0];
}");
            comp.VerifyDiagnostics(
                // (8,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         M2() = ref x;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "M2()").WithLocation(8, 9));
        }

        [Fact]
        public void RefReassignToProperty()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        int x = 0;
        ref int rx = ref P;
        P = ref x;
    }
    ref int P
    {
        get => ref (new int[1])[0];
    }
}");
            comp.VerifyDiagnostics(
                // (8,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         P = ref x;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "P").WithLocation(8, 9));
        }

        [Fact, WorkItem(44153, "https://github.com/dotnet/roslyn/issues/44153")]
        public void RefErrorProperty()
        {
            CreateCompilation(@"
public class C {
    public ref ERROR Prop => throw null!;
}
").VerifyEmitDiagnostics(
                // (3,16): error CS0246: The type or namespace name 'ERROR' could not be found (are you missing a using directive or an assembly reference?)
                //     public ref ERROR Prop => throw null!;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "ERROR").WithArguments("ERROR").WithLocation(3, 16));
        }

        [Fact]
        public void RefReadonlyOnlyIn72()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        int x = 0;
        ref readonly int y = ref x;
    }
}", options: TestOptions.Regular7_1);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics(
                // (7,13): error CS8302: Feature 'readonly references' is not available in C# 7.1. Please use language version 7.2 or greater.
                //         ref readonly int y = ref x;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "readonly").WithArguments("readonly references", "7.2").WithLocation(7, 13));
        }

        [Fact]
        public void CovariantConversionRefReadonly()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        string s = string.Empty;
        ref readonly object x = ref s;
    }
}");
            comp.VerifyDiagnostics(
                // (7,37): error CS8173: The expression must be of type 'object' because it is being assigned by reference
                //         ref readonly object x = ref s;
                Diagnostic(ErrorCode.ERR_RefAssignmentMustHaveIdentityConversion, "s").WithArguments("object").WithLocation(7, 37));
        }

        [Fact]
        public void ImplicitNumericRefReadonlyConversion()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        int x = 0;
        ref readonly long y = ref x;
    }
}");
            comp.VerifyDiagnostics(
                // (7,35): error CS8173: The expression must be of type 'long' because it is being assigned by reference
                //         ref readonly long y = ref x;
                Diagnostic(ErrorCode.ERR_RefAssignmentMustHaveIdentityConversion, "x").WithArguments("long").WithLocation(7, 35));
        }

        [Fact]
        public void RefReadonlyLocalToLiteral()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        ref readonly int x = ref 42;
    }
}");
            comp.VerifyDiagnostics(
                // (6,34): error CS8156: An expression cannot be used in this context because it may not be returned by reference
                //         ref readonly int x = ref 42;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "42").WithLocation(6, 34));
        }

        [Fact]
        public void RefReadonlyNoCaptureInLambda()
        {
            var comp = CreateCompilation(@"
using System;
class C
{
    void M()
    {
        ref readonly int x = ref (new int[1])[0];
        Action a = () =>
        {
            int i = x;
        };
    }
}");
            comp.VerifyDiagnostics(
                // (10,21): error CS8175: Cannot use ref local 'x' inside an anonymous method, lambda expression, or query expression
                //             int i = x;
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseLocal, "x").WithArguments("x").WithLocation(10, 21));
        }

        [Fact]
        public void RefReadonlyInLambda()
        {
            var comp = CreateCompilation(@"
using System;
class C
{
    void M()
    {
        Action a = () =>
        {
            ref readonly int x = ref (new int[1])[0];
            int i = x;
        };
    }
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RefReadonlyNoCaptureInLocalFunction()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        ref readonly int x = ref (new int[1])[0];
        void Local()
        {
            int i = x;
        }
        Local();
    }
}");
            comp.VerifyDiagnostics(
                // (9,21): error CS8175: Cannot use ref local 'x' inside an anonymous method, lambda expression, or query expression
                //             int i = x;
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseLocal, "x").WithArguments("x").WithLocation(9, 21));
        }

        [Fact]
        public void RefReadonlyInLocalFunction()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        void Local()
        {
            ref readonly int x = ref (new int[1])[0];
            int i = x;
        }
        Local();
    }
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RefReadonlyInAsync()
        {
            var comp = CreateCompilationWithMscorlib46(@"
using System.Threading.Tasks;
class C
{
    async Task M()
    {
        ref readonly int x = ref (new int[1])[0];
        int i = x;
        await Task.FromResult(false);
    }
}");
            comp.VerifyDiagnostics(
                // (7,26): error CS8177: Async methods cannot have by-reference locals
                //         ref readonly int x = ref (new int[1])[0];
                Diagnostic(ErrorCode.ERR_BadAsyncLocalType, "x").WithLocation(7, 26));
        }

        [Fact]
        public void RefReadonlyInIterator()
        {
            var comp = CreateCompilation(@"
using System.Collections.Generic;
class C
{
    IEnumerable<int> M()
    {
        ref readonly int x = ref (new int[1])[0];
        int i = x;
        yield return i;
    }
}");
            comp.VerifyDiagnostics(
                // (7,26): error CS8176: Iterators cannot have by-reference locals
                //         ref readonly int x = ref (new int[1])[0];
                Diagnostic(ErrorCode.ERR_BadIteratorLocalType, "x").WithLocation(7, 26));
        }

        [Fact]
        public void RefReadonlyInSwitchCaseInIterator_01()
        {
            var comp = CreateCompilation(@"
using System.Collections.Generic;
class C
{
    IEnumerable<int> M()
    {
        switch (this)
        {
            default:
                ref readonly int x = ref (new int[1])[0]; // 1
                yield return 1;
                yield return x;

                local();
                void local()
                {
                    ref readonly int z = ref (new int[1])[0];
                }
                break;
        }
    }
}");
            comp.VerifyDiagnostics(
                // (10,34): error CS8176: Iterators cannot have by-reference locals
                //                 ref readonly int x = ref (new int[1])[0]; // 1
                Diagnostic(ErrorCode.ERR_BadIteratorLocalType, "x").WithLocation(10, 34));
        }

        [Fact]
        public void RefReadonlyInSwitchCaseInIterator_02()
        {
            var comp = CreateCompilation(@"
using System.Collections.Generic;
class C
{
    IEnumerable<int> M()
    {
        switch (this)
        {
            default:
                ref readonly int x; // 1, 2
                yield return 1;
                yield return x; // 3
                break;
        }
    }
}");
            comp.VerifyDiagnostics(
                // (10,34): error CS8176: Iterators cannot have by-reference locals
                //                 ref readonly int x; // 1, 2
                Diagnostic(ErrorCode.ERR_BadIteratorLocalType, "x").WithLocation(10, 34),
                // (10,34): error CS8174: A declaration of a by-reference variable must have an initializer
                //                 ref readonly int x; // 1, 2
                Diagnostic(ErrorCode.ERR_ByReferenceVariableMustBeInitialized, "x").WithLocation(10, 34),
                // (12,30): error CS0165: Use of unassigned local variable 'x'
                //                 yield return x; // 3
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(12, 30));
        }

        [Fact]
        public void RefReadonlyInSwitchCaseInIterator_03()
        {
            var comp = CreateCompilation(@"
using System.Collections.Generic;
class C
{
    IEnumerable<int> M()
    {
        switch (this)
        {
            default:
                foreach (ref readonly int x in (new int[1]))
                yield return 1;
                break;
        }
    }
}");
            comp.VerifyDiagnostics(
                // (10,43): error CS8176: Iterators cannot have by-reference locals
                //                 foreach (ref readonly int x in (new int[1]))
                Diagnostic(ErrorCode.ERR_BadIteratorLocalType, "x").WithLocation(10, 43));
        }

        [Fact]
        public void RefReadonlyInEmbeddedStatementInIterator()
        {
            var comp = CreateCompilation(@"
using System.Collections.Generic;
class C
{
    IEnumerable<int> M()
    {
        if (true)
            ref int x = ref (new int[1])[0]; // 1, 2
        
        yield return 1;
    }
}");
            comp.VerifyDiagnostics(
                // (8,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             ref int x = ref (new int[1])[0]; // 1, 2
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "ref int x = ref (new int[1])[0];").WithLocation(8, 13),
                // (8,21): error CS8176: Iterators cannot have by-reference locals
                //             ref int x = ref (new int[1])[0]; // 1, 2
                Diagnostic(ErrorCode.ERR_BadIteratorLocalType, "x").WithLocation(8, 21));
        }

        [Fact]
        public void RefReadonlyInEmbeddedStatementInAsync()
        {
            var comp = CreateCompilation(@"
using System.Threading.Tasks;
class C
{
    async Task M()
    {
        if (true)
            ref int x = ref (new int[1])[0]; // 1, 2
        
        await Task.Yield();
    }
}");
            comp.VerifyDiagnostics(
                // (8,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             ref int x = ref (new int[1])[0]; // 1, 2
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "ref int x = ref (new int[1])[0];").WithLocation(8, 13),
                // (8,21): error CS8177: Async methods cannot have by-reference locals
                //             ref int x = ref (new int[1])[0]; // 1, 2
                Diagnostic(ErrorCode.ERR_BadAsyncLocalType, "x").WithLocation(8, 21));
        }

        [Fact]
        public void RefReadonlyLocalNotWritable()
        {
            var comp = CreateCompilation(@"
struct S
{
    public int X;
    public S(int x) => X = x;
    
    public void AddOne() => this.X++;
}

class C
{
    void M()
    {
        S s = new S(0);
        ref readonly S rs = ref s;
        s.X++;
        rs.X++;
        s.AddOne();
        rs.AddOne();
        s.X = 0;
        rs.X = 0;
    }
}");
            comp.VerifyDiagnostics(
                // (17,9): error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer
                //         rs.X++;
                Diagnostic(ErrorCode.ERR_IncrementLvalueExpected, "rs.X").WithLocation(17, 9),
                // (21,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         rs.X = 0;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "rs.X").WithLocation(21, 9));
        }

        [Fact]
        public void StripReadonlyInReturn()
        {
            var comp = CreateCompilation(@"
class C
{
    ref int M(ref int p)
    {
        ref readonly int rp = ref p;
        return ref rp;
    }
}");
            comp.VerifyDiagnostics(
                // (7,20): error CS8156: An expression cannot be used in this context because it may not be returned by reference
                //         return ref rp;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "rp").WithLocation(7, 20));
        }

        [Fact]
        public void MixingRefParams()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        void L(ref int x, in int y)
        {
            L(ref x, y);
            L(ref y, x);
            L(ref x, ref x);

            ref readonly int xr = ref x;
            L(ref x, xr);
            L(ref x, ref xr);
            L(ref xr, y);
        }
    }
}");
            comp.VerifyDiagnostics(
                // (9,19): error CS8329: Cannot use variable 'y' as a ref or out value because it is a readonly variable
                //             L(ref y, x);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "y").WithArguments("variable", "y").WithLocation(9, 19),
                // (10,26): warning CS9191: The 'ref' modifier for argument 2 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
                //             L(ref x, ref x);
                Diagnostic(ErrorCode.WRN_BadArgRef, "x").WithArguments("2").WithLocation(10, 26),
                // (14,26): error CS1510: A ref or out value must be an assignable variable
                //             L(ref x, ref xr);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "xr").WithLocation(14, 26),
                // (15,19): error CS1510: A ref or out value must be an assignable variable
                //             L(ref xr, y);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "xr").WithLocation(15, 19));
        }

        [Fact]
        public void AssignRefReadonlyToRefParam()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        void L(ref int p) { }

        L(ref 42);
        int x = 0;
        ref readonly int xr = ref x;
        L(xr);
        L(ref xr);

        ref readonly int L2() => ref (new int[1])[0];

        L(L2());
        L(ref L2());
    }
}");
            comp.VerifyDiagnostics(
                // (8,15): error CS1510: A ref or out value must be an assignable variable
                //         L(ref 42);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "42").WithLocation(8, 15),
                // (11,11): error CS1620: Argument 1 must be passed with the 'ref' keyword
                //         L(xr);
                Diagnostic(ErrorCode.ERR_BadArgRef, "xr").WithArguments("1", "ref").WithLocation(11, 11),
                // (12,15): error CS1510: A ref or out value must be an assignable variable
                //         L(ref xr);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "xr").WithLocation(12, 15),
                // (16,11): error CS1620: Argument 1 must be passed with the 'ref' keyword
                //         L(L2());
                Diagnostic(ErrorCode.ERR_BadArgRef, "L2()").WithArguments("1", "ref").WithLocation(16, 11),
                // (17,15): error CS8329: Cannot use method 'L2' as a ref or out value because it is a readonly variable
                //         L(ref L2());
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "L2()").WithArguments("method", "L2").WithLocation(17, 15));
        }

        [Fact]
        public void AssignRefReadonlyLocalToRefLocal()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        ref readonly int L() => ref (new int[1])[0];

        ref int w = ref L();
        ref readonly int x = ref L();
        ref int y = x;
        ref int z = ref x;
    }
}");
            comp.VerifyDiagnostics(
                // (8,25): error CS8329: Cannot use method 'L' as a ref or out value because it is a readonly variable
                //         ref int w = ref L();
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "L()").WithArguments("method", "L").WithLocation(8, 25),
                // (10,17): error CS8172: Cannot initialize a by-reference variable with a value
                //         ref int y = x;
                Diagnostic(ErrorCode.ERR_InitializeByReferenceVariableWithValue, "y = x").WithLocation(10, 17),
                // (10,21): error CS1510: A ref or out value must be an assignable variable
                //         ref int y = x;
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "x").WithLocation(10, 21),
                // (11,25): error CS1510: A ref or out value must be an assignable variable
                //         ref int z = ref x;
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "x").WithLocation(11, 25)
            );
        }

        [Fact]
        public void RefLocalMissingInitializer()
        {
            var text = @"
class Test
{
    void A()
    {
        ref int x;
    }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
    // (6,17): error CS8174: A declaration of a by-reference variable must have an initializer
    //         ref int x;
    Diagnostic(ErrorCode.ERR_ByReferenceVariableMustBeInitialized, "x").WithLocation(6, 17),
    // (6,17): warning CS0168: The variable 'x' is declared but never used
    //         ref int x;
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(6, 17)
                );
        }

        [Fact]
        public void RefLocalHasValueInitializer()
        {
            var text = @"
class Test
{
    void A()
    {
        int a = 123;
        ref int x = a;
        var y = x;
    }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
    // (7,17): error CS8172: Cannot initialize a by-reference variable with a value
    //         ref int x = a;
    Diagnostic(ErrorCode.ERR_InitializeByReferenceVariableWithValue, "x = a").WithLocation(7, 17)
                );
        }

        [Fact]
        public void ValLocalHasRefInitializer()
        {
            var text = @"
class Test
{
    void A()
    {
        int a = 123;
        ref int x = ref a;
        var y = ref x;
    }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
    // (8,13): error CS8171: Cannot initialize a by-value variable with a reference
    //         var y = ref x;
    Diagnostic(ErrorCode.ERR_InitializeByValueVariableWithReference, "y = ref x").WithLocation(8, 13)
                );
        }

        [Fact]
        public void RefReturnNotLValue()
        {
            var text = @"
class Test
{
    ref int A()
    {
        return ref 2 + 2;
    }

    ref int B()
    {
        return ref 2;
    }

    ref object C()
    {
        return ref null;
    }

    void VoidMethod(){}

    ref object D()
    {
        return ref VoidMethod();
    }

    int P1 {get{return 1;} set{}}

    ref int E()
    {
        return ref P1;
    }

}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
    // (6,20): error CS8156: An expression cannot be used in this context because it may not be returned by reference
    //         return ref 2 + 2;
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "2 + 2").WithLocation(6, 20),
    // (11,20): error CS8156: An expression cannot be used in this context because it may not be returned by reference
    //         return ref 2;
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "2").WithLocation(11, 20),
    // (16,20): error CS8156: An expression cannot be used in this context because it may not be returned by reference
    //         return ref null;
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "null").WithLocation(16, 20),
    // (23,20): error CS8156: An expression cannot be used in this context because it may not be returned by reference
    //         return ref VoidMethod();
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "VoidMethod()").WithLocation(23, 20),
    // (30,20): error CS8156: An expression cannot be used in this context because it may not be returned by reference
    //         return ref P1;
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "P1").WithLocation(30, 20)
            );
        }

        [Fact]
        public void RefReturnNotLValue1()
        {
            var text = @"
class Test
{
    delegate ref int D1();
    delegate ref object D2();

    void Test1()
    {
        D1 d1 = () => ref 2 + 2;
        D1 d2 = () => ref 2;
        D2 d3 = () => ref null;
        D2 d4 = () => ref VoidMethod();
        D1 d5 = () => ref P1;
    }

    void VoidMethod(){}
    int P1 {get{return 1;} set{}}
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
    // (9,27): error CS8156: An expression cannot be used in this context because it may not be returned by reference
    //         D1 d1 = () => ref 2 + 2;
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "2 + 2").WithLocation(9, 27),
    // (10,27): error CS8156: An expression cannot be used in this context because it may not be returned by reference
    //         D1 d2 = () => ref 2;
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "2").WithLocation(10, 27),
    // (11,27): error CS8156: An expression cannot be used in this context because it may not be returned by reference
    //         D2 d3 = () => ref null;
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "null").WithLocation(11, 27),
    // (12,27): error CS8156: An expression cannot be used in this context because it may not be returned by reference
    //         D2 d4 = () => ref VoidMethod();
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "VoidMethod()").WithLocation(12, 27),
    // (13,27): error CS8156: An expression cannot be used in this context because it may not be returned by reference
    //         D1 d5 = () => ref P1;
    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "P1").WithLocation(13, 27));
        }

        [Fact]
        public void RefByValLocalParam()
        {
            var text = @"
public class Test
{
    public struct S1
    {
        public char x;
    }

    ref char Test1(char arg1, S1 arg2)
    {
        if (1.ToString() == null)
        {
            char l = default(char);
            // valid
            ref char r = ref l;

            // invalid
            return ref l;
        }

        if (2.ToString() == null)
        {
            S1 l = default(S1);
            // valid
            ref char r = ref l.x;

            // invalid
            return ref l.x;
        }

        if (3.ToString() == null)
        {
            // valid
            ref char r = ref arg1;

            // invalid
            return ref arg1;
        }

        if (4.ToString() == null)
        {
            // valid
            ref char r = ref arg2.x;

            // invalid
            return ref arg2.x;
        }

        throw null;
    }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
    // (18,24): error CS8168: Cannot return local 'l' by reference because it is not a ref local
    //             return ref l;
    Diagnostic(ErrorCode.ERR_RefReturnLocal, "l").WithArguments("l").WithLocation(18, 24),
    // (28,24): error CS8169: Cannot return a member of local 'l' by reference because it is not a ref local
    //             return ref l.x;
    Diagnostic(ErrorCode.ERR_RefReturnLocal2, "l").WithArguments("l").WithLocation(28, 24),
    // (37,24): error CS8166: Cannot return a parameter by reference 'arg1' because it is not a ref parameter
    //             return ref arg1;
    Diagnostic(ErrorCode.ERR_RefReturnParameter, "arg1").WithArguments("arg1").WithLocation(37, 24),
    // (46,24): error CS8167: Cannot return a member of parameter 'arg2' by reference because it is not a ref or out parameter
    //             return ref arg2.x;
    Diagnostic(ErrorCode.ERR_RefReturnParameter2, "arg2").WithArguments("arg2").WithLocation(46, 24)
            );
        }

        [Fact]
        public void RefByValLocalParam_UnsafeContext()
        {
            var text = @"
public unsafe class Test
{
    public struct S1
    {
        public char x;
    }

    ref char Test1()
    {
        char l = default(char);
        ref char r = ref l;
        return ref l; // 1
    }

    ref char Test2()
    {
        S1 l = default(S1);
        ref char r = ref l.x;
        return ref l.x; // 2
    }

    ref char Test2(char arg1)
    {
        ref char r = ref arg1;
        return ref arg1; // 3
    }

    ref char Test2(S1 arg2)
    {
        ref char r = ref arg2.x;
        return ref arg2.x; // 4
    }
}";
            var comp = CreateCompilation(text, options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (13,20): warning CS9088: This returns local 'l' by reference but it is not a ref local
                //         return ref l; // 1
                Diagnostic(ErrorCode.WRN_RefReturnLocal, "l").WithArguments("l").WithLocation(13, 20),
                // (20,20): warning CS9089: This returns a member of local 'l' by reference but it is not a ref local
                //         return ref l.x; // 2
                Diagnostic(ErrorCode.WRN_RefReturnLocal2, "l").WithArguments("l").WithLocation(20, 20),
                // (26,20): warning CS9084: This returns a parameter by reference 'arg1' but it is not a ref parameter
                //         return ref arg1; // 3
                Diagnostic(ErrorCode.WRN_RefReturnParameter, "arg1").WithArguments("arg1").WithLocation(26, 20),
                // (32,20): warning CS9086: This returns by reference a member of parameter 'arg2' that is not a ref or out parameter
                //         return ref arg2.x; // 4
                Diagnostic(ErrorCode.WRN_RefReturnParameter2, "arg2").WithArguments("arg2").WithLocation(32, 20)
                );
        }

        [Fact]
        public void RefReadonlyLocal()
        {
            var text = @"
public class Test
{
    public struct S1
    {
        public int x;
    }

    ref char Test1()
    {
        foreach(var ro in ""qqq"")
        {
            ref char r = ref ro;
        }

        foreach(var ro in ""qqq"")
        {
            return ref ro;
        }

        foreach(var ro in new S1[1])
        {
            ref char r = ref ro.x;
        }

        foreach(var ro in new S1[1])
        {
            return ref ro.x;
        }


        throw null;
    }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (13,30): error CS1657: Cannot use 'ro' as a ref or out value because it is a 'foreach iteration variable'
                //             ref char r = ref ro;
                Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "ro").WithArguments("ro", "foreach iteration variable").WithLocation(13, 30),
                // (18,24): error CS1657: Cannot use 'ro' as a ref or out value because it is a 'foreach iteration variable'
                //             return ref ro;
                Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "ro").WithArguments("ro", "foreach iteration variable").WithLocation(18, 24),
                // (23,30): error CS1655: Cannot use fields of 'ro' as a ref or out value because it is a 'foreach iteration variable'
                //             ref char r = ref ro.x;
                Diagnostic(ErrorCode.ERR_RefReadonlyLocal2Cause, "ro.x").WithArguments("ro", "foreach iteration variable").WithLocation(23, 30),
                // (28,24): error CS1655: Cannot use fields of 'ro' as a ref or out value because it is a 'foreach iteration variable'
                //             return ref ro.x;
                Diagnostic(ErrorCode.ERR_RefReadonlyLocal2Cause, "ro.x").WithArguments("ro", "foreach iteration variable").WithLocation(28, 24)
            );
        }

        [Fact]
        public void RefRangeVar()
        {
            var text = @"
using System.Linq;

public class Test
{
    public struct S1
    {
        public char x;
    }

    delegate ref char D1();

    static void Test1()
    {
        var x = from ch in ""qqq""
            select(D1)(() => ref ch);

        var y = from s in new S1[10]
            select(D1)(() => ref s.x);
    }

}";
            var comp = CreateCompilation(text, targetFramework: TargetFramework.Empty, references: new[] { MscorlibRef, SystemCoreRef });
            comp.VerifyDiagnostics(
                // (16,34): error CS8159: Cannot return the range variable 'ch' by reference
                //             select(D1)(() => ref ch);
                Diagnostic(ErrorCode.ERR_RefReturnRangeVariable, "ch").WithArguments("ch").WithLocation(16, 34),
                // (19,34): error CS8159: Cannot return the range variable 's' by reference
                //             select(D1)(() => ref s.x);
                Diagnostic(ErrorCode.ERR_RefReturnRangeVariable, "s.x").WithArguments("s").WithLocation(19, 34)
            );
        }

        [Fact]
        public void RefMethodGroup()
        {
            var text = @"

public class Test
{
    public struct S1
    {
        public char x;
    }

    delegate ref char D1();

    static ref char Test1()
    {
        ref char r = ref M;
        ref char r1 = ref MR;

        if (1.ToString() != null)
        {
            return ref M;
        }
        else
        {
            return ref MR;
        }
    }

    static char M()
    {
        return default(char);
    }

    static ref char MR()
    {
        return ref (new char[1])[0];
    }

}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
    // (14,26): error CS1657: Cannot use 'M' as a ref or out value because it is a 'method group'
    //         ref char r = ref M;
    Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "M").WithArguments("M", "method group").WithLocation(14, 26),
    // (15,27): error CS1657: Cannot use 'MR' as a ref or out value because it is a 'method group'
    //         ref char r1 = ref MR;
    Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "MR").WithArguments("MR", "method group").WithLocation(15, 27),
    // (19,24): error CS1657: Cannot use 'M' as a ref or out value because it is a 'method group'
    //             return ref M;
    Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "M").WithArguments("M", "method group").WithLocation(19, 24),
    // (23,24): error CS1657: Cannot use 'MR' as a ref or out value because it is a 'method group'
    //             return ref MR;
    Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "MR").WithArguments("MR", "method group").WithLocation(23, 24)
            );
        }

        [Fact]
        public void RefReadonlyField()
        {
            var text = @"
public class Test
{
    public struct S1
    {
        public char x;
    }

    public static readonly char s1;
    public static readonly S1 s2;

    public readonly char i1;
    public readonly S1 i2;

    public Test()
    {
        if (1.ToString() != null)
        {
            // not an error
            ref char temp = ref i1;
            temp.ToString();
        }
        else
        {
            // not an error
            ref char temp = ref i2.x;
            temp.ToString();
        }

        if (1.ToString() != null)
        {
            // error
            ref char temp = ref s1;
            temp.ToString();
        }
        else
        {
            // error
            ref char temp = ref s2.x;
            temp.ToString();
        }

    }

    static Test()
    {
        if (1.ToString() != null)
        {
            // not an error
            ref char temp = ref s1;
            temp.ToString();
        }
        else
        {
            // not an error
            ref char temp = ref s2.x;
            temp.ToString();
        }
    }

    ref char Test1()
    {
        if (1.ToString() != null)
        {
            ref char temp = ref i1;
            temp.ToString();

            return ref i1;
        }
        else
        {
            ref char temp = ref i2.x;
            temp.ToString();

            return ref i2.x;
        }
    }

    ref char Test2()
    {
        if (1.ToString() != null)
        {
            ref char temp = ref s1;
            temp.ToString();

            return ref s1;
        }
        else
        {
            ref char temp = ref s2.x;
            temp.ToString();

            return ref s2.x;
        }
    }

}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (33,33): error CS0199: A static readonly field cannot be used as a ref or out value (except in a static constructor)
                //             ref char temp = ref s1;
                Diagnostic(ErrorCode.ERR_RefReadonlyStatic, "s1").WithLocation(33, 33),
                // (39,33): error CS1651: Fields of static readonly field 'Test.s2' cannot be used as a ref or out value (except in a static constructor)
                //             ref char temp = ref s2.x;
                Diagnostic(ErrorCode.ERR_RefReadonlyStatic2, "s2.x").WithArguments("Test.s2").WithLocation(39, 33),
                // (65,33): error CS0192: A readonly field cannot be used as a ref or out value (except in a constructor)
                //             ref char temp = ref i1;
                Diagnostic(ErrorCode.ERR_RefReadonly, "i1").WithLocation(65, 33),
                // (68,24): error CS8160: A readonly field cannot be returned by writable reference
                //             return ref i1;
                Diagnostic(ErrorCode.ERR_RefReturnReadonly, "i1").WithLocation(68, 24),
                // (72,33): error CS1649: Members of readonly field 'Test.i2' cannot be used as a ref or out value (except in a constructor)
                //             ref char temp = ref i2.x;
                Diagnostic(ErrorCode.ERR_RefReadonly2, "i2.x").WithArguments("Test.i2").WithLocation(72, 33),
                // (75,24): error CS8162: Members of readonly field 'Test.i2' cannot be returned by writable reference
                //             return ref i2.x;
                Diagnostic(ErrorCode.ERR_RefReturnReadonly2, "i2.x").WithArguments("Test.i2").WithLocation(75, 24),
                // (83,33): error CS0199: A static readonly field cannot be used as a ref or out value (except in a static constructor)
                //             ref char temp = ref s1;
                Diagnostic(ErrorCode.ERR_RefReadonlyStatic, "s1").WithLocation(83, 33),
                // (86,24): error CS8161: A static readonly field cannot be returned by writable reference
                //             return ref s1;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyStatic, "s1").WithLocation(86, 24),
                // (90,33): error CS1651: Fields of static readonly field 'Test.s2' cannot be used as a ref or out value (except in a static constructor)
                //             ref char temp = ref s2.x;
                Diagnostic(ErrorCode.ERR_RefReadonlyStatic2, "s2.x").WithArguments("Test.s2").WithLocation(90, 33),
                // (93,24): error CS8163: Fields of static readonly field 'Test.s2' cannot be returned by writable reference
                //             return ref s2.x;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyStatic2, "s2.x").WithArguments("Test.s2").WithLocation(93, 24)
            );
        }

        [Fact]
        public void RefReadonlyCall()
        {
            var text = @"
public class Test
{
    public struct S1
    {
        public char x;

        public ref S1 GooS()
        {
            return ref this;
        }        

        public ref char Goo()
        {
            return ref x;
        }

        public ref char Goo1()
        {
            return ref this.x;
        }
    }

    static ref T Goo<T>(ref T arg)
    {
        return ref arg;
    }

    static ref char Test1()
    {
        char M1 = default(char);
        S1   M2 = default(S1);

        if (1.ToString() != null)
        {
            return ref Goo(ref M1);
        }
        
        if (2.ToString() != null)
        {
            return ref Goo(ref M2.x);
        }

        if (3.ToString() != null)
        {
            return ref Goo(ref M2).x;
        }
        else
        {
            return ref M2.Goo();
        }
    }
  
    public class C
    {
        public ref C M()
        {
            return ref this;
        }
    }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (10,24): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //             return ref this;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "this").WithLocation(10, 24),
                // (15,24): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //             return ref x;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "x").WithLocation(15, 24),
                // (20,24): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //             return ref this.x;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "this.x").WithLocation(20, 24),
                // (36,32): error CS8168: Cannot return local 'M1' by reference because it is not a ref local
                //             return ref Goo(ref M1);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "M1").WithArguments("M1").WithLocation(36, 32),
                // (36,24): error CS8347: Cannot use a result of 'Test.Goo<char>(ref char)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Goo(ref M1);
                Diagnostic(ErrorCode.ERR_EscapeCall, "Goo(ref M1)").WithArguments("Test.Goo<char>(ref char)", "arg").WithLocation(36, 24),
                // (41,32): error CS8169: Cannot return a member of local 'M2' by reference because it is not a ref local
                //             return ref Goo(ref M2.x);
                Diagnostic(ErrorCode.ERR_RefReturnLocal2, "M2").WithArguments("M2").WithLocation(41, 32),
                // (41,24): error CS8347: Cannot use a result of 'Test.Goo<char>(ref char)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Goo(ref M2.x);
                Diagnostic(ErrorCode.ERR_EscapeCall, "Goo(ref M2.x)").WithArguments("Test.Goo<char>(ref char)", "arg").WithLocation(41, 24),
                // (46,32): error CS8168: Cannot return local 'M2' by reference because it is not a ref local
                //             return ref Goo(ref M2).x;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "M2").WithArguments("M2").WithLocation(46, 32),
                // (46,24): error CS8348: Cannot use a member of result of 'Test.Goo<Test.S1>(ref Test.S1)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //             return ref Goo(ref M2).x;
                Diagnostic(ErrorCode.ERR_EscapeCall2, "Goo(ref M2)").WithArguments("Test.Goo<Test.S1>(ref Test.S1)", "arg").WithLocation(46, 24),
                // (58,24): error CS8354: Cannot return 'this' by reference.
                //             return ref this;
                Diagnostic(ErrorCode.ERR_RefReturnThis, "this").WithLocation(58, 24)
                );
        }

        [Fact]
        public void RefReturnUnreturnableLocalParam()
        {
            var text = @"
public class Test
{
    public struct S1
    {
        public char x;
    }

    ref char Test1(char arg1, S1 arg2)
    {
        if (1.ToString() == null)
        {
            char l = default(char);
            // valid
            ref char r = ref l;

            // invalid
            return ref r;
        }

        if (2.ToString() == null)
        {
            S1 l = default(S1);
            // valid
            ref char r = ref l.x;

            // invalid
            return ref r;
        }

        if (21.ToString() == null)
        {
            S1 l = default(S1);
            // valid
            ref var r = ref l;

            // invalid
            return ref r.x;
        }

        if (3.ToString() == null)
        {
            // valid
            ref char r = ref arg1;

            // invalid
            return ref r;
        }

        if (4.ToString() == null)
        {
            // valid
            ref char r = ref arg2.x;

            // invalid
            return ref r;
        }

        if (41.ToString() == null)
        {
            // valid
            ref S1 r = ref arg2;

            // invalid
            return ref r.x;
        }

        throw null;
    }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
    // (18,24): error CS8157: Cannot return 'r' by reference because it was initialized to a value that cannot be returned by reference
    //             return ref r;
    Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r").WithArguments("r").WithLocation(18, 24),
    // (28,24): error CS8157: Cannot return 'r' by reference because it was initialized to a value that cannot be returned by reference
    //             return ref r;
    Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r").WithArguments("r").WithLocation(28, 24),
    // (38,24): error CS8158: Cannot return by reference a member of 'r' because it was initialized to a value that cannot be returned by reference
    //             return ref r.x;
    Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal2, "r").WithArguments("r").WithLocation(38, 24),
    // (47,24): error CS8157: Cannot return 'r' by reference because it was initialized to a value that cannot be returned by reference
    //             return ref r;
    Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r").WithArguments("r").WithLocation(47, 24),
    // (56,24): error CS8157: Cannot return 'r' by reference because it was initialized to a value that cannot be returned by reference
    //             return ref r;
    Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r").WithArguments("r").WithLocation(56, 24),
    // (65,24): error CS8158: Cannot return by reference a member of 'r' because it was initialized to a value that cannot be returned by reference
    //             return ref r.x;
    Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal2, "r").WithArguments("r").WithLocation(65, 24)
            );
        }

        [Fact]
        public void RefReturnUnreturnableLocalParam_UnsafeContext()
        {
            var text = @"
public unsafe class Test
{
    public struct S1
    {
        public char x;
    }

    ref char Test1()
    {
        char l = default(char);
        ref char r = ref l;
        return ref r; // 1
    }

    ref char Test2()
    {
        S1 l = default(S1);
        ref char r = ref l.x;
        return ref r; // 2
    }

    ref char Test3()
    {
        S1 l = default(S1);
        ref var r = ref l;
        return ref r.x; // 3
    }

    ref char Test4(char arg1)
    {
        ref char r = ref arg1;
        return ref r; // 4
    }

    ref char Test5(S1 arg2)
    {
        ref char r = ref arg2.x;
        return ref r; // 5
    }

    ref char Test6(S1 arg2)
    {
        ref S1 r = ref arg2;
        return ref r.x; // 6
    }
}";
            var comp = CreateCompilation(text, options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (13,20): warning CS9079: Local 'r' is returned by reference but was initialized to a value that cannot be returned by reference
                //         return ref r; // 1
                Diagnostic(ErrorCode.WRN_RefReturnNonreturnableLocal, "r").WithArguments("r").WithLocation(13, 20),
                // (20,20): warning CS9079: Local 'r' is returned by reference but was initialized to a value that cannot be returned by reference
                //         return ref r; // 2
                Diagnostic(ErrorCode.WRN_RefReturnNonreturnableLocal, "r").WithArguments("r").WithLocation(20, 20),
                // (27,20): warning CS9080: A member of 'r' is returned by reference but was initialized to a value that cannot be returned by reference
                //         return ref r.x; // 3
                Diagnostic(ErrorCode.WRN_RefReturnNonreturnableLocal2, "r").WithArguments("r").WithLocation(27, 20),
                // (33,20): warning CS9079: Local 'r' is returned by reference but was initialized to a value that cannot be returned by reference
                //         return ref r; // 4
                Diagnostic(ErrorCode.WRN_RefReturnNonreturnableLocal, "r").WithArguments("r").WithLocation(33, 20),
                // (39,20): warning CS9079: Local 'r' is returned by reference but was initialized to a value that cannot be returned by reference
                //         return ref r; // 5
                Diagnostic(ErrorCode.WRN_RefReturnNonreturnableLocal, "r").WithArguments("r").WithLocation(39, 20),
                // (45,20): warning CS9080: A member of 'r' is returned by reference but was initialized to a value that cannot be returned by reference
                //         return ref r.x; // 6
                Diagnostic(ErrorCode.WRN_RefReturnNonreturnableLocal2, "r").WithArguments("r").WithLocation(45, 20)
                );

            var verifier = CompileAndVerify(comp, verify: Verification.Skipped);
            verifier.VerifyIL("Test.Test1", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (char V_0, //l
                char& V_1, //r
                char& V_2)
  IL_0000:  nop
  IL_0001:  ldc.i4.0
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  stloc.1
  IL_0006:  ldloc.1
  IL_0007:  stloc.2
  IL_0008:  br.s       IL_000a
  IL_000a:  ldloc.2
  IL_000b:  ret
}
");
            verifier.VerifyIL("Test.Test6", @"
{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (Test.S1& V_0, //r
                char& V_1)
  IL_0000:  nop
  IL_0001:  ldarga.s   V_1
  IL_0003:  stloc.0
  IL_0004:  ldloc.0
  IL_0005:  ldflda     ""char Test.S1.x""
  IL_000a:  stloc.1
  IL_000b:  br.s       IL_000d
  IL_000d:  ldloc.1
  IL_000e:  ret
}
");
        }

        [Fact]
        public void RefAssignUnreturnableLocalParam_UnsafeContext()
        {
            var text = @"
public unsafe class Test
{
    public struct S1
    {
        public char x;
    }

    void Test1()
    {
        char c = default;
        ref char outer = ref c;
        {
            char l = default;
            ref char r = ref l;
            outer = ref r; // 1
        }
    }
}";
            var comp = CreateCompilation(text, options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (16,13): warning CS9082: The right-hand-side expression 'r' has a narrower escape scope than the left-hand-side expression 'outer' in ref-assignment.
                //             outer = ref r; // 1
                Diagnostic(ErrorCode.WRN_RefAssignNarrower, "outer = ref r").WithArguments("outer", "r").WithLocation(16, 13)
                );

            var verifier = CompileAndVerify(comp, verify: Verification.Skipped);
            verifier.VerifyIL("Test.Test1", @"
{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (char V_0, //c
                char& V_1, //outer
                char V_2, //l
                char& V_3) //r
  IL_0000:  nop
  IL_0001:  ldc.i4.0
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  stloc.1
  IL_0006:  nop
  IL_0007:  ldc.i4.0
  IL_0008:  stloc.2
  IL_0009:  ldloca.s   V_2
  IL_000b:  stloc.3
  IL_000c:  ldloc.3
  IL_000d:  stloc.1
  IL_000e:  nop
  IL_000f:  ret
}
");
        }

        [Fact]
        public void RefReturnSelfReferringRef()
        {
            var text = @"
public class Test
{
    public struct S1
    {
        public char x;
    }

    ref char Goo(ref char a, ref char b)
    {
        return ref a;
    }

    ref char Test1(char arg1, S1 arg2)
    {
        if (1.ToString() == null)
        {
            ref char r = ref r;
            return ref r;   //1
        }

        if (2.ToString() == null)
        {
            ref S1 r = ref r;
            return ref r.x;  //2
        }

        if (3.ToString() == null)
        {
            ref char a = ref (new char[1])[0];
            ref char invalid = ref Goo(ref a, ref a);

            // valid
            return ref r;
        }

        if (4.ToString() == null)
        {
            ref char a = ref (new char[1])[0];
            ref char valid = ref Goo(ref a, ref arg1);

            // valid
            return ref valid; //4
        }

        if (5.ToString() == null)
        {
            ref char a = ref (new char[1])[0];
            ref char r = ref Goo(ref a, ref r);

            // invalid
            return ref r;  //5
        }

        throw null;
    }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (19,24): error CS8157: Cannot return 'r' by reference because it was initialized to a value that cannot be returned by reference
                //             return ref r;   //1
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r").WithArguments("r").WithLocation(19, 24),
                // (25,24): error CS8158: Cannot return by reference a member of 'r' because it was initialized to a value that cannot be returned by reference
                //             return ref r.x;  //2
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal2, "r").WithArguments("r").WithLocation(25, 24),
                // (34,24): error CS0103: The name 'r' does not exist in the current context
                //             return ref r;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "r").WithArguments("r").WithLocation(34, 24),
                // (43,24): error CS8157: Cannot return 'valid' by reference because it was initialized to a value that cannot be returned by reference
                //             return ref valid; //4
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "valid").WithArguments("valid").WithLocation(43, 24),
                // (52,24): error CS8157: Cannot return 'r' by reference because it was initialized to a value that cannot be returned by reference
                //             return ref r;  //5
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r").WithArguments("r").WithLocation(52, 24),
                // (18,30): error CS0165: Use of unassigned local variable 'r'
                //             ref char r = ref r;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "r").WithArguments("r").WithLocation(18, 30),
                // (24,28): error CS0165: Use of unassigned local variable 'r'
                //             ref S1 r = ref r;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "r").WithArguments("r").WithLocation(24, 28),
                // (49,45): error CS0165: Use of unassigned local variable 'r'
                //             ref char r = ref Foo(ref a, ref r);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "r").WithArguments("r").WithLocation(49, 45)
            );
        }

        [Fact]
        public void RefReturnNested()
        {
            var text = @"
public class Test
{
    public static void Main()
    {
        ref char Goo(ref char a, ref char b)
        {
            // valid
            return ref a;
        }
        
        char Goo1(ref char a, ref char b)
        {
            return ref b;
        }

        ref char Goo2(ref char c, ref char b)
        {
            return c;
        }
    }
}";
            var options = TestOptions.Regular;
            var comp = CreateCompilationWithMscorlib45(text, parseOptions: options);
            comp.VerifyDiagnostics(
                // (14,13): error CS8149: By-reference returns may only be used in methods that return by reference
                //             return ref b;
                Diagnostic(ErrorCode.ERR_MustNotHaveRefReturn, "return").WithLocation(14, 13),
                // (19,13): error CS8150: By-value returns may only be used in methods that return by value
                //             return c;
                Diagnostic(ErrorCode.ERR_MustHaveRefReturn, "return").WithLocation(19, 13),
                // (6,18): warning CS8321: The local function 'Goo' is declared but never used
                //         ref char Goo(ref char a, ref char b)
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Goo").WithArguments("Goo").WithLocation(6, 18),
                // (12,14): warning CS8321: The local function 'Goo1' is declared but never used
                //         char Goo1(ref char a, ref char b)
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Goo1").WithArguments("Goo1").WithLocation(12, 14),
                // (17,18): warning CS8321: The local function 'Goo2' is declared but never used
                //         ref char Goo2(ref char c, ref char b)
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Goo2").WithArguments("Goo2").WithLocation(17, 18));
        }

        [Fact]
        public void RefReturnNestedArrow()
        {
            var text = @"
public class Test
{
    public static void Main()
    {
        // valid
        ref char Goo(ref char a, ref char b) => ref a;
        
        char Goo1(ref char a, ref char b) => ref b;

        ref char Goo2(ref char c, ref char b) => c;

        var arr = new int[1];
        ref var r = ref arr[0];

        ref char Moo1(ref char a, ref char b) => ref r;
        char Moo3(ref char a, ref char b) => r;
    }
}";
            var options = TestOptions.Regular;
            var comp = CreateCompilationWithMscorlib45(text, parseOptions: options);
            comp.VerifyDiagnostics(
                // (9,50): error CS8149: By-reference returns may only be used in methods that return by reference
                //         char Goo1(ref char a, ref char b) => ref b;
                Diagnostic(ErrorCode.ERR_MustNotHaveRefReturn, "b").WithLocation(9, 50),
                // (11,50): error CS8150: By-value returns may only be used in methods that return by value
                //         ref char Goo2(ref char c, ref char b) => c;
                Diagnostic(ErrorCode.ERR_MustHaveRefReturn, "c").WithLocation(11, 50),
                // (16,54): error CS8175: Cannot use ref local 'r' inside an anonymous method, lambda expression, or query expression
                //         ref char Moo1(ref char a, ref char b) => ref r;
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseLocal, "r").WithArguments("r").WithLocation(16, 54),
                // (17,46): error CS8175: Cannot use ref local 'r' inside an anonymous method, lambda expression, or query expression
                //         char Moo3(ref char a, ref char b) => r;
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseLocal, "r").WithArguments("r").WithLocation(17, 46),
                // (7,18): warning CS8321: The local function 'Goo' is declared but never used
                //         ref char Goo(ref char a, ref char b) => ref a;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Goo").WithArguments("Goo").WithLocation(7, 18),
                // (9,14): warning CS8321: The local function 'Goo1' is declared but never used
                //         char Goo1(ref char a, ref char b) => ref b;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Goo1").WithArguments("Goo1").WithLocation(9, 14),
                // (11,18): warning CS8321: The local function 'Goo2' is declared but never used
                //         ref char Goo2(ref char c, ref char b) => c;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Goo2").WithArguments("Goo2").WithLocation(11, 18),
                // (16,18): warning CS8321: The local function 'Moo1' is declared but never used
                //         ref char Moo1(ref char a, ref char b) => ref r;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Moo1").WithArguments("Moo1").WithLocation(16, 18),
                // (17,14): warning CS8321: The local function 'Moo3' is declared but never used
                //         char Moo3(ref char a, ref char b) => r;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Moo3").WithArguments("Moo3").WithLocation(17, 14)
                );
        }

        [Fact, WorkItem(13062, "https://github.com/dotnet/roslyn/issues/13062")]
        public void NoRefInIndex()
        {
            var text = @"
class C
{
    void F(object[] a, object[,] a2, int i)
    {
        int j;
        j = a[ref i];    // error 1
        j = a[out i];    // error 2
        j = this[ref i]; // error 3
        j = a2[i, out i]; // error 4
        j = a2[i, ref i]; // error 5
        j = a2[ref i, out i]; // error 6
    }
    public int this[int i] => 1;
}
";
            CreateCompilationWithMscorlib45(text).VerifyDiagnostics(
                // (7,13): error CS0266: Cannot implicitly convert type 'object' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         j = a[ref i];    // error 1
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "a[ref i]").WithArguments("object", "int").WithLocation(7, 13),
                // (7,19): error CS1615: Argument 1 may not be passed with the 'ref' keyword
                //         j = a[ref i];    // error 1
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "i").WithArguments("1", "ref").WithLocation(7, 19),
                // (8,13): error CS0266: Cannot implicitly convert type 'object' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         j = a[out i];    // error 2
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "a[out i]").WithArguments("object", "int").WithLocation(8, 13),
                // (8,19): error CS1615: Argument 1 may not be passed with the 'out' keyword
                //         j = a[out i];    // error 2
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "i").WithArguments("1", "out").WithLocation(8, 19),
                // (9,22): error CS1615: Argument 1 may not be passed with the 'ref' keyword
                //         j = this[ref i]; // error 3
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "i").WithArguments("1", "ref").WithLocation(9, 22),
                // (10,13): error CS0266: Cannot implicitly convert type 'object' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         j = a2[i, out i]; // error 4
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "a2[i, out i]").WithArguments("object", "int").WithLocation(10, 13),
                // (10,23): error CS1615: Argument 2 may not be passed with the 'out' keyword
                //         j = a2[i, out i]; // error 4
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "i").WithArguments("2", "out").WithLocation(10, 23),
                // (11,13): error CS0266: Cannot implicitly convert type 'object' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         j = a2[i, ref i]; // error 5
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "a2[i, ref i]").WithArguments("object", "int").WithLocation(11, 13),
                // (11,23): error CS1615: Argument 2 may not be passed with the 'ref' keyword
                //         j = a2[i, ref i]; // error 5
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "i").WithArguments("2", "ref").WithLocation(11, 23),
                // (12,13): error CS0266: Cannot implicitly convert type 'object' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         j = a2[ref i, out i]; // error 6
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "a2[ref i, out i]").WithArguments("object", "int").WithLocation(12, 13),
                // (12,20): error CS1615: Argument 1 may not be passed with the 'ref' keyword
                //         j = a2[ref i, out i]; // error 6
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "i").WithArguments("1", "ref").WithLocation(12, 20)
                );
        }

        [Fact, WorkItem(14174, "https://github.com/dotnet/roslyn/issues/14174")]
        public void RefDynamicBinding()
        {
            var text = @"
class C
{
    static object[] arr = new object[] { ""f"" };
    static void Main(string[] args)
    {
        System.Console.Write(arr[0].ToString());

        RefParam(ref arr[0]);
        System.Console.Write(arr[0].ToString());

        ref dynamic x = ref arr[0];
        x = ""o"";
        System.Console.Write(arr[0].ToString());

        RefReturn() = ""g"";
        System.Console.Write(arr[0].ToString());
    }

    static void RefParam(ref dynamic p)
    {
        p = ""r"";
    }

    static ref dynamic RefReturn()
    {
        return ref arr[0];
    }
}
";
            CompileAndVerify(text,
                expectedOutput: "frog",
                references: new[] { CSharpRef }).VerifyDiagnostics();
        }

        [Fact]
        public void RefQueryClause()
        {
            // a "ref" may not precede the expression of a query clause...
            // simply because the grammar doesn't permit it. Here we check
            // that the situation is diagnosed, either syntactically or semantically.
            // The precise diagnostics are not important for the purposes of this test.
            var text = @"
class C
{
    static void Main(string[] args)
    {
        var a = new[] { 1, 2, 3, 4 };
        bool b = true;
        int i = 0;
        { var za = from x in a select ref x; } // error 1
        { var zc = from x in a from y in ref a select x; } // error2
        { var zd = from x in a from int y in ref a select x; } // error 3
        { var ze = from x in a from y in ref a where true select x; } // error 4
        { var zf = from x in a from int y in ref a where true select x; } // error 5
        { var zg = from x in a let y = ref a select x; } // error 6
        { var zh = from x in a where ref b select x; } // error 7
        { var zi = from x in a join y in ref a on x equals y select x; } // error 8 (not lambda case)
        { var zj = from x in a join y in a on ref i equals y select x; } // error 9
        { var zk = from x in a join y in a on x equals ref i select x; } // error 10
        { var zl = from x in a orderby ref i select x; } // error 11
        { var zm = from x in a orderby x, ref i select x; } // error 12
        { var zn = from x in a group ref i by x; } // error 13
        { var zo = from x in a group x by ref i; } // error 14
    }
    public static T M<T>(T x, out T z) => z = x;

    public C Select(RefFunc<C, C> c1) => this;
    public C SelectMany(RefFunc<C, C> c1, RefFunc<C, C, C> c2) => this;
    public C Cast<T>() => this;
}
public delegate ref TR RefFunc<T1, TR>(T1 t1);
public delegate ref TR RefFunc<T1, T2, TR>(T1 t1, T2 t2);
";
            CreateCompilationWithMscorlib40AndSystemCore(text)
                .GetDiagnostics()
                // It turns out each of them is diagnosed with ErrorCode.ERR_InvalidExprTerm in the midst
                // of a flurry of other syntax errors.
                .Where(d => d.Code == (int)ErrorCode.ERR_InvalidExprTerm)
                .Verify(
                // (9,39): error CS1525: Invalid expression term 'ref'
                //         { var za = from x in a select ref x; } // error 1
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref x").WithArguments("ref").WithLocation(9, 39),
                // (10,42): error CS1525: Invalid expression term 'ref'
                //         { var zc = from x in a from y in ref a select x; } // error2
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref a").WithArguments("ref").WithLocation(10, 42),
                // (11,46): error CS1525: Invalid expression term 'ref'
                //         { var zd = from x in a from int y in ref a select x; } // error 3
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref a").WithArguments("ref").WithLocation(11, 46),
                // (12,42): error CS1525: Invalid expression term 'ref'
                //         { var ze = from x in a from y in ref a where true select x; } // error 4
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref a").WithArguments("ref").WithLocation(12, 42),
                // (13,46): error CS1525: Invalid expression term 'ref'
                //         { var zf = from x in a from int y in ref a where true select x; } // error 5
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref a").WithArguments("ref").WithLocation(13, 46),
                // (14,40): error CS1525: Invalid expression term 'ref'
                //         { var zg = from x in a let y = ref a select x; } // error 6
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref a").WithArguments("ref").WithLocation(14, 40),
                // (15,38): error CS1525: Invalid expression term 'ref'
                //         { var zh = from x in a where ref b select x; } // error 7
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref b").WithArguments("ref").WithLocation(15, 38),
                // (16,42): error CS1525: Invalid expression term 'ref'
                //         { var zi = from x in a join y in ref a on x equals y select x; } // error 8 (not lambda case)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref a").WithArguments("ref").WithLocation(16, 42),
                // (17,47): error CS1525: Invalid expression term 'ref'
                //         { var zj = from x in a join y in a on ref i equals y select x; } // error 9
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref i").WithArguments("ref").WithLocation(17, 47),
                // (18,56): error CS1525: Invalid expression term 'ref'
                //         { var zk = from x in a join y in a on x equals ref i select x; } // error 10
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref i").WithArguments("ref").WithLocation(18, 56),
                // (19,40): error CS1525: Invalid expression term 'ref'
                //         { var zl = from x in a orderby ref i select x; } // error 11
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref i").WithArguments("ref").WithLocation(19, 40),
                // (20,43): error CS1525: Invalid expression term 'ref'
                //         { var zm = from x in a orderby x, ref i select x; } // error 12
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref i").WithArguments("ref").WithLocation(20, 43),
                // (21,38): error CS1525: Invalid expression term 'ref'
                //         { var zn = from x in a group ref i by x; } // error 13
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref i").WithArguments("ref").WithLocation(21, 38),
                // (22,43): error CS1525: Invalid expression term 'ref'
                //         { var zo = from x in a group x by ref i; } // error 14
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref i").WithArguments("ref").WithLocation(22, 43)
                );
        }

        [Fact, WorkItem(13073, "https://github.com/dotnet/roslyn/issues/13073")]
        public void CannotUseYieldReturnInAReturnByRefFunction()
        {
            var code = @"
class TestClass
{
    int x = 0;
    ref int TestFunction()
    {
        yield return x;

        ref int localFunction()
        {
            yield return x;
        }

        yield return localFunction();
    }
}";

            CreateCompilation(code).VerifyDiagnostics(
                // (9,17): error CS8154: The body of 'localFunction()' cannot be an iterator block because 'localFunction()' returns by reference
                //         ref int localFunction()
                Diagnostic(ErrorCode.ERR_BadIteratorReturnRef, "localFunction").WithArguments("localFunction()").WithLocation(9, 17),
                // (5,13): error CS8154: The body of 'TestClass.TestFunction()' cannot be an iterator block because 'TestClass.TestFunction()' returns by reference
                //     ref int TestFunction()
                Diagnostic(ErrorCode.ERR_BadIteratorReturnRef, "TestFunction").WithArguments("TestClass.TestFunction()").WithLocation(5, 13));
        }

        [Fact]
        public void CannotUseYieldReturnInAReturnByRefFunction_InIfBlock()
        {
            var code = @"
class TestClass
{
    int x = 0;
    ref int TestFunction()
    {
        if (true)
        {
            yield return x;
        }

        ref int localFunction()
        {
            if (true)
            {
                yield return x;
            }
        }

        yield return localFunction();
    }
}";

            CreateCompilation(code).VerifyDiagnostics(
                // (12,17): error CS8154: The body of 'localFunction()' cannot be an iterator block because 'localFunction()' returns by reference
                //         ref int localFunction()
                Diagnostic(ErrorCode.ERR_BadIteratorReturnRef, "localFunction").WithArguments("localFunction()").WithLocation(12, 17),
                // (5,13): error CS8154: The body of 'TestClass.TestFunction()' cannot be an iterator block because 'TestClass.TestFunction()' returns by reference
                //     ref int TestFunction()
                Diagnostic(ErrorCode.ERR_BadIteratorReturnRef, "TestFunction").WithArguments("TestClass.TestFunction()").WithLocation(5, 13));
        }

        [Fact, WorkItem(13073, "https://github.com/dotnet/roslyn/issues/13073")]
        public void CannotUseRefReturnInExpressionTree_ParenthesizedLambdaExpression()
        {
            var code = @"
using System.Linq.Expressions;
class TestClass
{
    int x = 0;

    delegate ref int RefReturnIntDelegate(int y);

    void TestFunction()
    {
        Expression<RefReturnIntDelegate> lambda = (y) => ref x;
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (11,51): error CS8155: Lambda expressions that return by reference cannot be converted to expression trees
                //         Expression<RefReturnIntDelegate> lambda = (y) => ref x;
                Diagnostic(ErrorCode.ERR_BadRefReturnExpressionTree, "(y) => ref x").WithLocation(11, 51));
        }

        [Fact, WorkItem(13073, "https://github.com/dotnet/roslyn/issues/13073")]
        public void CannotUseRefReturnInExpressionTree_SimpleLambdaExpression()
        {
            var code = @"
using System.Linq.Expressions;
class TestClass
{
    int x = 0;

    delegate ref int RefReturnIntDelegate(int y);

    void TestFunction()
    {
        Expression<RefReturnIntDelegate> lambda = y => ref x;
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (11,51): error CS8155: Lambda expressions that return by reference cannot be converted to expression trees
                //         Expression<RefReturnIntDelegate> lambda = y => ref x;
                Diagnostic(ErrorCode.ERR_BadRefReturnExpressionTree, "y => ref x").WithLocation(11, 51));
        }

        [Fact, WorkItem(13073, "https://github.com/dotnet/roslyn/issues/13073")]
        public void CannotCallExpressionThatReturnsByRefInExpressionTree_01()
        {
            var code = @"
using System;
using System.Linq.Expressions;
namespace TestRefReturns
{
    class TestClass
    {
        int x = 0;

        ref int RefReturnFunction()
        {
            return ref x;
        }

        ref int RefReturnProperty
        {
            get { return ref x; }
        }

        ref int this[int y]
        {
            get { return ref x; }
        }

        int TakeRefFunction(ref int y)
        {
            return y;
        }

        void TestFunction()
        {
            Expression<Func<int>> lambda1 = () => TakeRefFunction(ref RefReturnFunction());
            Expression<Func<int>> lambda2 = () => TakeRefFunction(ref RefReturnProperty);
            Expression<Func<int>> lambda3 = () => TakeRefFunction(ref this[0]);
        }
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code).VerifyEmitDiagnostics(
                // (32,71): error CS8153: An expression tree lambda may not contain a call to a method, property, or indexer that returns by reference
                //             Expression<Func<int>> lambda1 = () => TakeRefFunction(ref RefReturnFunction());
                Diagnostic(ErrorCode.ERR_RefReturningCallInExpressionTree, "RefReturnFunction()").WithLocation(32, 71),
                // (33,71): error CS8153: An expression tree lambda may not contain a call to a method, property, or indexer that returns by reference
                //             Expression<Func<int>> lambda2 = () => TakeRefFunction(ref RefReturnProperty);
                Diagnostic(ErrorCode.ERR_RefReturningCallInExpressionTree, "RefReturnProperty").WithLocation(33, 71),
                // (34,71): error CS8153: An expression tree lambda may not contain a call to a method, property, or indexer that returns by reference
                //             Expression<Func<int>> lambda3 = () => TakeRefFunction(ref this[0]);
                Diagnostic(ErrorCode.ERR_RefReturningCallInExpressionTree, "this[0]").WithLocation(34, 71));
        }

        [WorkItem(19930, "https://github.com/dotnet/roslyn/issues/19930")]
        [Fact, WorkItem(13073, "https://github.com/dotnet/roslyn/issues/13073")]
        public void CannotRefReturnQueryRangeVariable()
        {
            var code = @"
using System.Linq;
class TestClass
{
    delegate ref char RefCharDelegate();
    void TestMethod()
    {
        var x = from c in ""TestValue"" select (RefCharDelegate)(() => ref c);
    }

    delegate ref readonly char RoRefCharDelegate();
    void TestMethod1()
    {
        var x = from c in ""TestValue"" select (RoRefCharDelegate)(() => ref c);
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code).VerifyDiagnostics(
                // (8,74): error CS8159: Cannot return the range variable 'c' by reference
                //         var x = from c in "TestValue" select (RefCharDelegate)(() => ref c);
                Diagnostic(ErrorCode.ERR_RefReturnRangeVariable, "c").WithArguments("c").WithLocation(8, 74),
                // (14,76): error CS8159: Cannot return the range variable 'c' by reference
                //         var x = from c in "TestValue" select (RoRefCharDelegate)(() => ref c);
                Diagnostic(ErrorCode.ERR_RefReturnRangeVariable, "c").WithArguments("c").WithLocation(14, 76)
                );
        }

        [Fact, WorkItem(13073, "https://github.com/dotnet/roslyn/issues/13073")]
        public void CannotAssignRefInNonIdentityConversion()
        {
            var code = @"
using System;
using System.Collections.Generic;

class TestClass
{
    int intVar = 0;
    string stringVar = ""TEST"";

    void TestMethod()
    {
        ref int? nullableConversion = ref intVar;
        ref dynamic dynamicConversion = ref intVar;
        ref IEnumerable<char> enumerableConversion = ref stringVar;
        ref IFormattable interpolatedStringConversion = ref stringVar;
    }
}";

            CreateCompilation(code).VerifyDiagnostics(
                // (12,43): error CS8173: The expression must be of type 'int?' because it is being assigned by reference
                //         ref int? nullableConversion = ref intVar;
                Diagnostic(ErrorCode.ERR_RefAssignmentMustHaveIdentityConversion, "intVar").WithArguments("int?").WithLocation(12, 43),
                // (13,45): error CS8173: The expression must be of type 'dynamic' because it is being assigned by reference
                //         ref dynamic dynamicConversion = ref intVar;
                Diagnostic(ErrorCode.ERR_RefAssignmentMustHaveIdentityConversion, "intVar").WithArguments("dynamic").WithLocation(13, 45),
                // (14,58): error CS8173: The expression must be of type 'IEnumerable<char>' because it is being assigned by reference
                //         ref IEnumerable<char> enumerableConversion = ref stringVar;
                Diagnostic(ErrorCode.ERR_RefAssignmentMustHaveIdentityConversion, "stringVar").WithArguments("System.Collections.Generic.IEnumerable<char>").WithLocation(14, 58),
                // (15,61): error CS8173: The expression must be of type 'IFormattable' because it is being assigned by reference
                //         ref IFormattable interpolatedStringConversion = ref stringVar;
                Diagnostic(ErrorCode.ERR_RefAssignmentMustHaveIdentityConversion, "stringVar").WithArguments("System.IFormattable").WithLocation(15, 61));
        }

        [Fact, WorkItem(13073, "https://github.com/dotnet/roslyn/issues/13073")]
        public void IteratorMethodsCannotHaveRefLocals()
        {
            var code = @"
using System.Collections.Generic;
class TestClass
{
    int x = 0;
    IEnumerable<int> TestMethod()
    {
        ref int y = ref x;
        yield return y;

        IEnumerable<int> localFunction()
        {
            ref int z = ref x;
            yield return z;
        }

        foreach(var item in localFunction())
        {
            yield return item;
        }
    }
}";

            CreateCompilation(code).VerifyDiagnostics(
                // (13,21): error CS8176: Iterators cannot have by-reference locals
                //             ref int z = ref x;
                Diagnostic(ErrorCode.ERR_BadIteratorLocalType, "z").WithLocation(13, 21),
                // (8,17): error CS8176: Iterators cannot have by-reference locals
                //         ref int y = ref x;
                Diagnostic(ErrorCode.ERR_BadIteratorLocalType, "y").WithLocation(8, 17));
        }

        [Fact, WorkItem(13073, "https://github.com/dotnet/roslyn/issues/13073")]
        public void AsyncMethodsCannotHaveRefLocals()
        {
            var code = @"
using System.Threading.Tasks;
class TestClass
{
    int x = 0;
    async Task TestMethod()
    {
        ref int y = ref x;
        await Task.Run(async () =>
        {
            ref int z = ref x;
            await Task.Delay(0);
        });
    }
}";
            CreateCompilationWithMscorlib45(code).VerifyDiagnostics(
                // (8,17): error CS8177: Async methods cannot have by-reference locals
                //         ref int y = ref x;
                Diagnostic(ErrorCode.ERR_BadAsyncLocalType, "y").WithLocation(8, 17),
                // (11,21): error CS8177: Async methods cannot have by-reference locals
                //             ref int z = ref x;
                Diagnostic(ErrorCode.ERR_BadAsyncLocalType, "z").WithLocation(11, 21));
        }

        [Fact, WorkItem(13073, "https://github.com/dotnet/roslyn/issues/13073")]
        public void CannotUseAwaitExpressionInACallToAFunctionThatReturnsByRef()
        {
            var code = @"
using System;
using System.Threading.Tasks;
class TestClass
{
    int x = 0;
    ref int Save(int y)
    {
        x = y;
        return ref x;
    }
    void Write(ref int y)
    {
        Console.WriteLine(y);
    }
    void Write(ref int y, int z)
    {
        Console.WriteLine(z);
    }
    async Task TestMethod()
    {
        // this is OK. `ref` is not spilled.
        Write(ref Save(await Task.FromResult(0)));

        // ERROR. `ref` is spilled because it must survive until after the second `await.
        Write(ref Save(await Task.FromResult(0)), await Task.FromResult(1));
    }
}";
            CreateCompilationWithMscorlib45(code).VerifyEmitDiagnostics(
                // (26,19): error CS8178: A reference returned by a call to 'TestClass.Save(int)' cannot be preserved across 'await' or 'yield' boundary.
                //         Write(ref Save(await Task.FromResult(0)), await Task.FromResult(1));
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait, "Save(await Task.FromResult(0))").WithArguments("TestClass.Save(int)").WithLocation(26, 19)
            );
        }

        [Fact]
        public void BadRefAssignByValueProperty()
        {
            var text = @"
class Program
{
    static int P { get; set; }

    static void M()
    {
        ref int rl = ref P;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (8,26): error CS0206: A non ref-returning property or indexer may not be used as an out or ref value
                //         ref int rl = ref P;
                Diagnostic(ErrorCode.ERR_RefProperty, "P").WithLocation(8, 26));
        }

        [Fact]
        public void BadRefAssignByValueIndexer()
        {
            var text = @"
class Program
{
    int this[int i] { get { return 0; } }

    void M()
    {
        ref int rl = ref this[0];
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (8,26): error CS0206: A non ref-returning property or indexer may not be used as an out or ref value
                //         ref int rl = ref this[0];
                Diagnostic(ErrorCode.ERR_RefProperty, "this[0]").WithLocation(8, 26));
        }

        [Fact]
        public void BadRefAssignNonFieldEvent()
        {
            var text = @"
delegate void D();

class Program
{
    event D d { add { } remove { } }

    void M()
    {
        ref int rl = ref d;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (10,26): error CS0079: The event 'Program.d' can only appear on the left hand side of += or -=
                //         ref int rl = ref d;
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "d").WithArguments("Program.d").WithLocation(10, 26));
        }

        [Fact]
        public void BadRefAssignReadonlyField()
        {
            var text = @"
class Program
{
    readonly int i = 0;

    void M()
    {
        ref int rl = ref i;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (8,26): error CS0192: A readonly field cannot be used as a ref or out value (except in a constructor)
                //         ref int rl = ref i;
                Diagnostic(ErrorCode.ERR_RefReadonly, "i").WithLocation(8, 26));
        }

        [Fact]
        public void BadRefAssignFieldReceiver()
        {
            var text = @"
struct Program
{
    int i;

    Program(int i)
    {
        this.i = i;
    }

    ref int M()
    {
        ref int rl = ref i;
        return ref rl;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (14,20): error CS8157: Cannot return 'rl' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref rl;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "rl").WithArguments("rl").WithLocation(14, 20)
            );
        }

        [Fact]
        public void BadRefAssignByValueCall()
        {
            var text = @"
class Program
{
    static int L()
    {
        return 0;
    }

    static void M()
    {
        ref int rl = ref L();
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (11,26): error CS1510: A ref or out value must be an assignable variable
                //         ref int rl = ref L();
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "L()").WithLocation(11, 26)
            );
        }

        [Fact]
        public void BadRefAssignByValueDelegateInvocation()
        {
            var text = @"
delegate int D();

class Program
{
    static void M(D d)
    {
        ref int rl = ref d();
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (8,26): error CS1510: A ref or out value must be an assignable variable
                //         ref int rl = ref d();
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "d()").WithLocation(8, 26)
            );
        }

        [Fact]
        public void BadRefAssignCallArgument()
        {
            var text = @"
class Program
{
    static ref int M(ref int i)
    {
        int j = 0;
        ref int rl = ref M(ref j);
        return ref rl;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (8,20): error CS8157: Cannot return 'rl' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref rl;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "rl").WithArguments("rl").WithLocation(8, 20)
            );
        }

        [Fact]
        public void BadRefAssignThisReference()
        {
            var text = @"
class Program
{
    void M()
    {
        ref int rl = ref this;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (6,26): error CS1605: Cannot use 'this' as a ref or out value because it is read-only
                //         ref int rl = ref this;
                Diagnostic(ErrorCode.ERR_RefReadonlyLocal, "this").WithArguments("this").WithLocation(6, 26)
            );
        }

        [Fact]
        public void BadRefAssignWrongType()
        {
            var text = @"
class Program
{
    void M(ref long i)
    {
        ref int rl = ref i;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (6,26): error CS8173: The expression must be of type 'int' because it is being assigned by reference
                //         ref int rl = ref i;
                Diagnostic(ErrorCode.ERR_RefAssignmentMustHaveIdentityConversion, "i").WithArguments("int").WithLocation(6, 26)
            );
        }

        [Fact]
        public void BadRefLocalCapturedInAnonymousMethod()
        {
            var text = @"
using System.Linq;

delegate int D();

class Program
{
    static int field = 0;

    static void M()
    {
        ref int rl = ref field;
        var d = new D(delegate { return rl; });
        d = new D(() => rl);
        rl = (from v in new int[10] where v > rl select v).Single();
    }
}
";

            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (13,41): error CS8930: Cannot use ref local 'rl' inside an anonymous method, lambda expression, or query expression
                //         var d = new D(delegate { return rl; });
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseLocal, "rl").WithArguments("rl").WithLocation(13, 41),
                // (14,25): error CS8930: Cannot use ref local 'rl' inside an anonymous method, lambda expression, or query expression
                //         d = new D(() => rl);
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseLocal, "rl").WithArguments("rl").WithLocation(14, 25),
                // (15,47): error CS8930: Cannot use ref local 'rl' inside an anonymous method, lambda expression, or query expression
                //         rl = (from v in new int[10] where v > rl select r1).Single();
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseLocal, "rl").WithArguments("rl").WithLocation(15, 47));
        }

        [Fact]
        public void BadRefLocalInAsyncMethod()
        {
            var text = @"
class Program
{
    static int field = 0;

    static async void Goo()
    {
        ref int i = ref field;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (8,17): error CS8177: Async methods cannot have by-reference locals
                //         ref int i = ref field;
                Diagnostic(ErrorCode.ERR_BadAsyncLocalType, "i").WithLocation(8, 17),
                // (6,23): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     static async void Goo()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Goo").WithLocation(6, 23));
        }

        [Fact]
        public void BadRefLocalInIteratorMethod()
        {
            var text = @"
using System.Collections;

class Program
{
    static int field = 0;

    static IEnumerable ObjEnumerable()
    {
        ref int i = ref field;
        yield return new object();
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (10,17): error CS8931: Iterators cannot have by-reference locals
                //         ref int i = ref field;
                Diagnostic(ErrorCode.ERR_BadIteratorLocalType, "i").WithLocation(10, 17));
        }

        [Fact]
        public void BadRefAssignByValueLocal()
        {
            var text = @"
class Program
{
    static void M(ref int i)
    {
        int l = ref i;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (6,13): error CS8922: Cannot initialize a by-value variable with a reference
                //         int l = ref i;
                Diagnostic(ErrorCode.ERR_InitializeByValueVariableWithReference, "l = ref i").WithLocation(6, 13)
               );
        }

        [Fact]
        public void BadByValueInitRefLocal()
        {
            var text = @"
class Program
{
    static void M(int i)
    {
        ref int rl = i;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (6,17): error CS8921: Cannot initialize a by-reference variable with a value
                //         ref int rl = i;
                Diagnostic(ErrorCode.ERR_InitializeByReferenceVariableWithValue, "rl = i").WithLocation(6, 17));
        }

        [Fact]
        public void BadRefReturnParameter()
        {
            var text = @"
class Program
{
    static ref int M(int i)
    {
        return ref i;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (6,20): error CS8911: Cannot return or assign a reference to parameter 'i' because it is not a ref or out parameter
                //         return ref i;
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "i").WithArguments("i").WithLocation(6, 20));
        }

        [Fact]
        public void BadRefReturnLocal()
        {
            var text = @"
class Program
{
    static ref int M()
    {
        int i = 0;
        return ref i;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (7,20): error CS8913: Cannot return or assign a reference to local 'i' because it is not a ref local
                //         return ref i;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "i").WithArguments("i").WithLocation(7, 20));
        }

        [Fact]
        public void BadRefReturnByValueProperty()
        {
            var text = @"
class Program
{
    static int P { get; set; }

    static ref int M()
    {
        return ref P;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (8,20): error CS8900: The argument to a by reference return or assignment must be an assignable variable or a property or call that returns by reference
                //         return ref P;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "P").WithLocation(8, 20));
        }

        [Fact]
        public void BadRefReturnByValueIndexer()
        {
            var text = @"
class Program
{
    int this[int i] { get { return 0; } }

    ref int M()
    {
        return ref this[0];
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (8,20): error CS8900: The argument to a by reference return or assignment must be an assignable variable or a property or call that returns by reference
                //         return ref this[0];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "this[0]").WithLocation(8, 20));
        }

        [Fact]
        public void BadRefReturnNonFieldEvent()
        {
            var text = @"
delegate void D();

class Program
{
    event D d { add { } remove { } }

    ref int M()
    {
        return ref d;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (10,20): error CS0079: The event 'Program.d' can only appear on the left hand side of += or -=
                //         return ref d;
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "d").WithArguments("Program.d").WithLocation(10, 20));
        }

        [Fact]
        public void BadRefReturnEventReceiver()
        {
            var text = @"
delegate void D();

struct Program
{
    event D d;

    ref D M()
    {
        return ref d;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (10,20): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //         return ref d;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "d").WithLocation(10, 20)
            );
        }

        [Fact]
        public void BadRefReturnReadonlyField()
        {
            var text = @"
class Program
{
    readonly int i = 0;

    ref int M()
    {
        return ref i;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (8,20): error CS8160: A readonly field cannot be returned by writable reference
                //         return ref i;
                Diagnostic(ErrorCode.ERR_RefReturnReadonly, "i").WithLocation(8, 20)
            );
        }

        [Fact]
        public void BadRefReturnFieldReceiver()
        {
            var text = @"
struct Program
{
    int i;

    Program(int i)
    {
        this.i = i;
    }

    ref int M()
    {
        return ref i;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (13,20): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //         return ref i;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "i").WithLocation(13, 20)
            );
        }

        [Fact]
        public void BadRefReturnByValueCall()
        {
            var text = @"
class Program
{
    static int L()
    {
        return 0;
    }

    static ref int M()
    {
        return ref L();
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (11,20): error CS8900: The argument to a by reference return or assignment must be an assignable variable or a property or call that returns by reference
                //         return ref L();
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "L()").WithLocation(11, 20));
        }

        [Fact]
        public void BadRefReturnByValueDelegateInvocation()
        {
            var text = @"
delegate int D();

class Program
{
    static ref int M(D d)
    {
        return ref d();
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (8,20): error CS8900: The argument to a by reference return or assignment must be an assignable variable or a property or call that returns by reference
                //         return ref d();
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "d()").WithLocation(8, 20));
        }

        [Fact]
        public void BadRefReturnDelegateInvocationWithArguments()
        {
            var text = @"
delegate ref int D(ref int i, ref int j, object o);

class Program
{
    static ref int M(D d, int i, int j, object o)
    {
        return ref d(ref i, ref j, o);
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (8,26): error CS8166: Cannot return a parameter by reference 'i' because it is not a ref parameter
                //         return ref d(ref i, ref j, o);
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "i").WithArguments("i").WithLocation(8, 26),
                // (8,20): error CS8347: Cannot use a result of 'D.Invoke(ref int, ref int, object)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //         return ref d(ref i, ref j, o);
                Diagnostic(ErrorCode.ERR_EscapeCall, "d(ref i, ref j, o)").WithArguments("D.Invoke(ref int, ref int, object)", "i").WithLocation(8, 20)
                );
        }

        [Fact]
        public void BadRefReturnCallArgument()
        {
            var text = @"
class Program
{
    static ref int M(ref int i)
    {
        int j = 0;
        return ref M(ref j);
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (7,26): error CS8168: Cannot return local 'j' by reference because it is not a ref local
                //         return ref M(ref j);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "j").WithArguments("j").WithLocation(7, 26),
                // (7,20): error CS8347: Cannot use a result of 'Program.M(ref int)' in this context because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //         return ref M(ref j);
                Diagnostic(ErrorCode.ERR_EscapeCall, "M(ref j)").WithArguments("Program.M(ref int)", "i").WithLocation(7, 20)
            );
        }

        [Fact]
        public void BadRefReturnStructThis()
        {
            var text = @"
struct Program
{
    ref Program M()
    {
        return ref this;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (6,20): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //         return ref this;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "this").WithLocation(6, 20));
        }

        [Fact]
        public void BadRefReturnThisReference()
        {
            var text = @"
class Program
{
    ref Program M()
    {
        return ref this;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (6,20): error CS8354: Cannot return 'this' by reference.
                //         return ref this;
                Diagnostic(ErrorCode.ERR_RefReturnThis, "this").WithLocation(6, 20)
            );
        }

        [Fact]
        public void BadRefReturnWrongType()
        {
            var text = @"
class Program
{
    ref int M(ref long i)
    {
        return ref i;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (6,20): error CS8085: The return expression must be of type 'int' because this method returns by reference.
                //         return ref i;
                Diagnostic(ErrorCode.ERR_RefReturnMustHaveIdentityConversion, "i").WithArguments("int").WithLocation(6, 20));
        }

        [Fact]
        public void BadByRefReturnInByValueReturningMethod()
        {
            var text = @"
class Program
{
    static int M(ref int i)
    {
        return ref i;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (6,9): error CS8083: By-reference returns may only be used in by-reference returning methods.
                //         return ref i;
                Diagnostic(ErrorCode.ERR_MustNotHaveRefReturn, "return").WithLocation(6, 9));
        }

        [Fact]
        public void BadByValueReturnInByRefReturningMethod()
        {
            var text = @"
class Program
{
    static ref int M(ref int i)
    {
        return i;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (6,9): error CS8084: By-value returns may only be used in by-value returning methods.
                //         return;
                Diagnostic(ErrorCode.ERR_MustHaveRefReturn, "return").WithLocation(6, 9));
        }

        [Fact]
        public void BadEmptyReturnInByRefReturningMethod()
        {
            var text = @"
class Program
{
    static ref int M(ref int i)
    {
        return;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (6,9): error CS8150: By-value returns may only be used in methods that return by value
                //         return;
                Diagnostic(ErrorCode.ERR_MustHaveRefReturn, "return").WithLocation(6, 9)
            );
        }

        [ConditionalFact(typeof(NoUsedAssembliesValidation))] // The test hook is blocked by https://github.com/dotnet/roslyn/issues/39971
        [WorkItem(39971, "https://github.com/dotnet/roslyn/issues/39971")]
        public void BadIteratorReturnInRefReturningMethod()
        {
            var text = @"
using System.Collections;
using System.Collections.Generic;

class C
{
    public ref IEnumerator ObjEnumerator()
    {
        yield return new object();
    }

    public ref IEnumerable ObjEnumerable()
    {
        yield return new object();
    }

    public ref IEnumerator<int> GenEnumerator()
    {
        yield return 0;
    }

    public ref IEnumerable<int> GenEnumerable()
    {
        yield return 0;
    }
}
";

            CreateCompilationWithMscorlib46(text).VerifyDiagnostics(
                // (7,28): error CS8089: The body of 'C.ObjEnumerator()' cannot be an iterator block because 'C.ObjEnumerator()' returns by reference
                //     public ref IEnumerator ObjEnumerator()
                Diagnostic(ErrorCode.ERR_BadIteratorReturnRef, "ObjEnumerator").WithArguments("C.ObjEnumerator()").WithLocation(7, 28),
                // (12,28): error CS8089: The body of 'C.ObjEnumerable()' cannot be an iterator block because 'C.ObjEnumerable()' returns by reference
                //     public ref IEnumerable ObjEnumerable()
                Diagnostic(ErrorCode.ERR_BadIteratorReturnRef, "ObjEnumerable").WithArguments("C.ObjEnumerable()").WithLocation(12, 28),
                // (17,33): error CS8089: The body of 'C.GenEnumerator()' cannot be an iterator block because 'C.GenEnumerator()' returns by reference
                //     public ref IEnumerator<int> GenEnumerator()
                Diagnostic(ErrorCode.ERR_BadIteratorReturnRef, "GenEnumerator").WithArguments("C.GenEnumerator()").WithLocation(17, 33),
                // (22,33): error CS8089: The body of 'C.GenEnumerable()' cannot be an iterator block because 'C.GenEnumerable()' returns by reference
                //     public ref IEnumerable<int> GenEnumerable()
                Diagnostic(ErrorCode.ERR_BadIteratorReturnRef, "GenEnumerable").WithArguments("C.GenEnumerable()").WithLocation(22, 33));
        }

        [Fact]
        public void BadRefReturnInExpressionTree()
        {
            var text = @"
using System.Linq.Expressions;

delegate ref int D();
delegate ref int E(int i);

class C
{
    static int field = 0;

    static void M()
    {
        Expression<D> d = () => ref field;
        Expression<E> e = (int i) => ref field;
    }
}
";

            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (13,27): error CS8090: Lambda expressions that return by reference cannot be converted to expression trees
                //         Expression<D> d = () => ref field;
                Diagnostic(ErrorCode.ERR_BadRefReturnExpressionTree, "() => ref field").WithLocation(13, 27),
                // (14,27): error CS8090: Lambda expressions that return by reference cannot be converted to expression trees
                //         Expression<E> e = (int i) => ref field;
                Diagnostic(ErrorCode.ERR_BadRefReturnExpressionTree, "(int i) => ref field").WithLocation(14, 27));
        }

        [Fact]
        public void BadRefReturningCallInExpressionTree()
        {
            var text = @"
using System.Linq.Expressions;

delegate int D(C c);

class C
{
    int field = 0;

    ref int P { get { return ref field; } }
    ref int this[int i] { get { return ref field; } }
    ref int M() { return ref field; }

    static void M1()
    {
        Expression<D> e = c => c.P;
        e = c => c[0];
        e = c => c.M();
    }
}
";

            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyEmitDiagnostics(
                // (16,32): error CS8091: An expression tree lambda may not contain a call to a method, property, or indexer that returns by reference
                //         Expression<D> e = c => c.P;
                Diagnostic(ErrorCode.ERR_RefReturningCallInExpressionTree, "c.P").WithLocation(16, 32),
                // (17,18): error CS8091: An expression tree lambda may not contain a call to a method, property, or indexer that returns by reference
                //         e = c => c[0];
                Diagnostic(ErrorCode.ERR_RefReturningCallInExpressionTree, "c[0]").WithLocation(17, 18),
                // (18,18): error CS8091: An expression tree lambda may not contain a call to a method, property, or indexer that returns by reference
                //         e = c => c.M();
                Diagnostic(ErrorCode.ERR_RefReturningCallInExpressionTree, "c.M()").WithLocation(18, 18));
        }

        [Fact]
        public void BadRefReturningCallWithAwait()
        {
            var text = @"
using System.Threading.Tasks;

struct S
{
    static S s = new S();

    public static ref S Instance { get { return ref s; } }

    public int Echo(int i)
    {
        return i;
    }
}

class C
{
    ref int Assign(ref int loc, int val)
    {
        loc = val;
        return ref loc;
    }

    public async Task<int> Do(int i)
    {
        if (i == 0)
        {
            return 0;
        }

        int temp = 0;
        var a = S.Instance.Echo(await Do(i - 1));
        var b = Assign(ref Assign(ref temp, 0), await Do(i - 1));
        return a + b;
    }
}
";

            CreateCompilationWithMscorlib45(text).VerifyEmitDiagnostics(
                // (32,17): error CS8178: A reference returned by a call to 'S.Instance.get' cannot be preserved across 'await' or 'yield' boundary.
                //         var a = S.Instance.Echo(await Do(i - 1));
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait, "S.Instance").WithArguments("S.Instance.get").WithLocation(32, 17),
                // (33,28): error CS8178: A reference returned by a call to 'C.Assign(ref int, int)' cannot be preserved across 'await' or 'yield' boundary.
                //         var b = Assign(ref Assign(ref temp, 0), await Do(i - 1));
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait, "Assign(ref temp, 0)").WithArguments("C.Assign(ref int, int)").WithLocation(33, 28)
                );
        }

        [Fact]
        public void CannotUseAwaitExpressionToAssignRefReturning()
        {
            var code = @"
using System;
using System.Threading.Tasks;
class TestClass
{
    int x = 0;
    ref int Save(int y)
    {
        x = y;
        return ref x;
    }

    void Write(ref int y)
    {
        Console.WriteLine(y);
    }

    public int this[int arg]
    {
        get { return 1; }
        set { }
    }

    public ref int this[int arg, int arg2] => ref x;

    async Task TestMethod()
    {
        Save(1) = await Task.FromResult(0);

        var inst = new TestClass();

        // valid
        inst[1] = await Task.FromResult(1);

        // invalid
        inst[1, 2] = await Task.FromResult(1);
    }
}";
            CreateCompilationWithMscorlib45(code).VerifyEmitDiagnostics(
                // (28,9): error CS8178: A reference returned by a call to 'TestClass.Save(int)' cannot be preserved across 'await' or 'yield' boundary.
                //         Save(1) = await Task.FromResult(0);
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait, "Save(1)").WithArguments("TestClass.Save(int)").WithLocation(28, 9),
                // (36,9): error CS8178: A reference returned by a call to 'TestClass.this[int, int].get' cannot be preserved across 'await' or 'yield' boundary.
                //         inst[1, 2] = await Task.FromResult(1);
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait, "inst[1, 2]").WithArguments("TestClass.this[int, int].get").WithLocation(36, 9)
            );
        }

        [Fact]
        public void RefReadOnlyInAsyncMethodDisallowed()
        {
            CreateCompilationWithMscorlib45(@"
using System.Threading.Tasks;
class Test
{
    async Task Method(in int p)
    {
        await Task.FromResult(0);
    }
}").VerifyDiagnostics(
                // (5,30): error CS1988: Async methods cannot have ref, in or out parameters
                //     async Task Method(in int p)
                Diagnostic(ErrorCode.ERR_BadAsyncArgType, "p").WithLocation(5, 30)
                );
        }

        [Fact]
        public void RefReadOnlyInIteratorMethodsDisallowed()
        {
            CreateCompilationWithMscorlib45(@"
using System.Collections.Generic;
class Test
{
    IEnumerable<int> Method(in int p)
    {
        yield return 0;
        yield return 1;
        yield return 2;
    }
}").VerifyDiagnostics(
                // (5,36): error CS1623: Iterators cannot have ref, in or out parameters
                //     IEnumerable<int> Method(in int p)
                Diagnostic(ErrorCode.ERR_BadIteratorArgType, "p").WithLocation(5, 36)
                );
        }

        [Fact]
        public void RefReadOnlyInEnumeratorMethodsDisallowed()
        {
            CreateCompilation(@"
using System.Collections.Generic;
class Test
{
    public IEnumerator<int> GetEnumerator(in int p)
    {
        yield return 0;
    }
}").VerifyDiagnostics(
                // (5,50): error CS1623: Iterators cannot have ref, in or out parameters
                //     public IEnumerator<int> GetEnumerator(in int p)
                Diagnostic(ErrorCode.ERR_BadIteratorArgType, "p").WithLocation(5, 50));
        }

        [Fact]
        public void CannotCallRefReadOnlyMethodsUsingDiscardParameter()
        {
            CreateCompilation(@"
class Test
{
	void M(in int p)
    {
    }
    void N()
    {
        M(_);
    }
}").VerifyDiagnostics(
                // (9,11): error CS0103: The name '_' does not exist in the current context
                //         M(_);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "_").WithArguments("_").WithLocation(9, 11));
        }

        [Fact, WorkItem(26418, "https://github.com/dotnet/roslyn/issues/26418")]
        public void OutArgumentsDeclaration_Ref()
        {
            CreateCompilation(@"
class Test
{
	void M(out int p)
    {
        p = 0;
    }
    void N()
    {
        M(out ref int x);
        M(out ref var y);

        M(out ref int _);
        M(out ref var _);
    }
}").VerifyDiagnostics(
                // (10,15): error CS8387: An out variable cannot be declared as a ref local
                //         M(out ref int x);
                Diagnostic(ErrorCode.ERR_OutVariableCannotBeByRef, "ref int").WithLocation(10, 15),
                // (11,15): error CS8387: An out variable cannot be declared as a ref local
                //         M(out ref var y);
                Diagnostic(ErrorCode.ERR_OutVariableCannotBeByRef, "ref var").WithLocation(11, 15),
                // (13,15): error CS8387: An out variable cannot be declared as a ref local
                //         M(out ref int _);
                Diagnostic(ErrorCode.ERR_OutVariableCannotBeByRef, "ref int").WithLocation(13, 15),
                // (14,15): error CS8387: An out variable cannot be declared as a ref local
                //         M(out ref var _);
                Diagnostic(ErrorCode.ERR_OutVariableCannotBeByRef, "ref var").WithLocation(14, 15));
        }

        [Fact, WorkItem(26418, "https://github.com/dotnet/roslyn/issues/26418")]
        public void OutArgumentsDeclaration_RefReadOnly()
        {
            CreateCompilation(@"
class Test
{
	void M(out int p)
    {
        p = 0;
    }
    void N()
    {
        M(out ref readonly int x);
        M(out ref readonly var y);

        M(out ref readonly int _);
        M(out ref readonly var _);
    }
}").VerifyDiagnostics(
                // (10,15): error CS8387: An out variable cannot be declared as a ref local
                //         M(out ref readonly int x);
                Diagnostic(ErrorCode.ERR_OutVariableCannotBeByRef, "ref readonly int").WithLocation(10, 15),
                // (11,15): error CS8387: An out variable cannot be declared as a ref local
                //         M(out ref readonly var y);
                Diagnostic(ErrorCode.ERR_OutVariableCannotBeByRef, "ref readonly var").WithLocation(11, 15),
                // (13,15): error CS8387: An out variable cannot be declared as a ref local
                //         M(out ref readonly int _);
                Diagnostic(ErrorCode.ERR_OutVariableCannotBeByRef, "ref readonly int").WithLocation(13, 15),
                // (14,15): error CS8387: An out variable cannot be declared as a ref local
                //         M(out ref readonly var _);
                Diagnostic(ErrorCode.ERR_OutVariableCannotBeByRef, "ref readonly var").WithLocation(14, 15));
        }

        [Fact, WorkItem(26418, "https://github.com/dotnet/roslyn/issues/26418")]
        public void OutArgumentsDeclaration_Out()
        {
            CreateCompilation(@"
class Test
{
	void M(out int p)
    {
        p = 0;
    }
    void N()
    {
        M(out out int x);
    }
}").GetParseDiagnostics().Verify(
                // (10,15): error CS1525: Invalid expression term 'out'
                //         M(out out int x);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "out").WithArguments("out").WithLocation(10, 15),
                // (10,15): error CS1003: Syntax error, ',' expected
                //         M(out out int x);
                Diagnostic(ErrorCode.ERR_SyntaxError, "out").WithArguments(",").WithLocation(10, 15));
        }

        [Fact, WorkItem(26418, "https://github.com/dotnet/roslyn/issues/26418")]
        public void OutArgumentsDeclaration_In()
        {
            CreateCompilation(@"
class Test
{
	void M(out int p)
    {
        p = 0;
    }
    void N()
    {
        M(out in int x);
    }
}").GetParseDiagnostics().Verify(
                // (10,15): error CS1525: Invalid expression term 'in'
                //         M(out in int x);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "in").WithArguments("in").WithLocation(10, 15),
                // (10,15): error CS1003: Syntax error, ',' expected
                //         M(out in int x);
                Diagnostic(ErrorCode.ERR_SyntaxError, "in").WithArguments(",").WithLocation(10, 15),
                // (10,18): error CS1525: Invalid expression term 'int'
                //         M(out in int x);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(10, 18),
                // (10,22): error CS1003: Syntax error, ',' expected
                //         M(out in int x);
                Diagnostic(ErrorCode.ERR_SyntaxError, "x").WithArguments(",").WithLocation(10, 22));
        }

        [Fact]
        [WorkItem(28117, "https://github.com/dotnet/roslyn/issues/28117")]
        public void AssigningRefToParameter()
        {
            CreateCompilation(@"
public class C
{
    void M(int a, ref int b)
    {
        a = ref b;
    }
}").VerifyDiagnostics(
                // (6,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         a = ref b;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "a").WithLocation(6, 9));
        }

        [Fact]
        [WorkItem(26516, "https://github.com/dotnet/roslyn/issues/26516")]
        public void BindingRefVoidAssignment()
        {
            CreateCompilation(@"
public class C
{
	public void M(ref int x)
    {
    	M(ref void = ref x);
    }
}").VerifyDiagnostics(
                // (6,12): error CS1525: Invalid expression term 'void'
                //     	M(ref void = ref x);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "void").WithArguments("void").WithLocation(6, 12));
        }

        [Fact]
        [WorkItem(28087, "https://github.com/dotnet/roslyn/issues/28087")]
        public void AssigningRef_ArrayElement()
        {
            var compilation = CreateCompilation(@"
public class C
{
    public void M(int[] array, ref int value)
    {
        array[0] = ref value;
    }
}").VerifyDiagnostics(
                // (6,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         array[0] = ref value;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "array[0]").WithLocation(6, 9));

            var tree = compilation.SyntaxTrees.Single();
            var assignment = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().Single();

            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var left = ((ElementAccessExpressionSyntax)assignment.Left).Expression;
            Assert.Equal(SpecialType.System_Int32, ((IArrayTypeSymbol)model.GetTypeInfo(left).Type).ElementType.SpecialType);

            var right = ((RefExpressionSyntax)assignment.Right).Expression;
            Assert.Equal(SpecialType.System_Int32, model.GetTypeInfo(right).Type.SpecialType);
        }

        [Fact]
        [WorkItem(28087, "https://github.com/dotnet/roslyn/issues/28087")]
        public void AssigningRef_PointerIndirectionOperator()
        {
            var compilation = CreateCompilation(@"
public unsafe class C
{
    public void M(int* ptr, ref int value)
    {
        *ptr = ref value;
    }
}", options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         *ptr = ref value;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "*ptr").WithLocation(6, 9));

            var tree = compilation.SyntaxTrees.Single();
            var assignment = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().Single();

            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var left = ((PrefixUnaryExpressionSyntax)assignment.Left).Operand;
            Assert.Equal(SpecialType.System_Int32, ((IPointerTypeSymbol)model.GetTypeInfo(left).Type).PointedAtType.SpecialType);

            var right = ((RefExpressionSyntax)assignment.Right).Expression;
            Assert.Equal(SpecialType.System_Int32, model.GetTypeInfo(right).Type.SpecialType);
        }

        [Fact]
        [WorkItem(28087, "https://github.com/dotnet/roslyn/issues/28087")]
        public void AssigningRef_PointerElementAccess()
        {
            var compilation = CreateCompilation(@"
public unsafe class C
{
    public void M(int* ptr, ref int value)
    {
        ptr[0] = ref value;
    }
}", options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         ptr[0] = ref value;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "ptr[0]").WithLocation(6, 9));

            var tree = compilation.SyntaxTrees.Single();
            var assignment = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().Single();

            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var left = ((ElementAccessExpressionSyntax)assignment.Left).Expression;
            Assert.Equal(SpecialType.System_Int32, ((IPointerTypeSymbol)model.GetTypeInfo(left).Type).PointedAtType.SpecialType);

            var right = ((RefExpressionSyntax)assignment.Right).Expression;
            Assert.Equal(SpecialType.System_Int32, model.GetTypeInfo(right).Type.SpecialType);
        }

        [Fact]
        [WorkItem(28087, "https://github.com/dotnet/roslyn/issues/28087")]
        public void AssigningRef_RefvalueExpression()
        {
            var compilation = CreateCompilation(@"
public unsafe class C
{
    public void M(int x)
    {
        __refvalue(__makeref(x), int) = ref x;
    }
}", options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         __refvalue(__makeref(x), int) = ref x;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "__refvalue(__makeref(x), int)").WithLocation(6, 9));

            var tree = compilation.SyntaxTrees.Single();
            var assignment = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().Single();

            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var left = ((MakeRefExpressionSyntax)((RefValueExpressionSyntax)assignment.Left).Expression).Expression;
            Assert.Equal(SpecialType.System_Int32, model.GetTypeInfo(left).Type.SpecialType);

            var right = ((RefExpressionSyntax)assignment.Right).Expression;
            Assert.Equal(SpecialType.System_Int32, model.GetTypeInfo(right).Type.SpecialType);
        }

        [Fact]
        [WorkItem(28087, "https://github.com/dotnet/roslyn/issues/28087")]
        public void AssigningRef_DynamicIndexerAccess()
        {
            var compilation = CreateCompilation(@"
public unsafe class C
{
    public void M(dynamic d, ref int value)
    {
        d[0] = ref value;
    }
}", options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         d[0] = ref value;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "d[0]").WithLocation(6, 9));

            var tree = compilation.SyntaxTrees.Single();
            var assignment = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().Single();

            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var left = ((ElementAccessExpressionSyntax)assignment.Left).Expression;
            Assert.Equal(SymbolKind.DynamicType, model.GetTypeInfo(left).Type.Kind);

            var right = ((RefExpressionSyntax)assignment.Right).Expression;
            Assert.Equal(SpecialType.System_Int32, model.GetTypeInfo(right).Type.SpecialType);
        }

        [Fact]
        [WorkItem(28087, "https://github.com/dotnet/roslyn/issues/28087")]
        public void AssigningRef_DynamicMemberAccess()
        {
            var compilation = CreateCompilation(@"
public unsafe class C
{
    public void M(dynamic d, ref int value)
    {
        d.member = ref value;
    }
}", options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,9): error CS8373: The left-hand side of a ref assignment must be a ref variable.
                //         d.member = ref value;
                Diagnostic(ErrorCode.ERR_RefLocalOrParamExpected, "d.member").WithLocation(6, 9));

            var tree = compilation.SyntaxTrees.Single();
            var assignment = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().Single();

            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var left = ((MemberAccessExpressionSyntax)assignment.Left).Expression;
            Assert.Equal(SymbolKind.DynamicType, model.GetTypeInfo(left).Type.Kind);

            var right = ((RefExpressionSyntax)assignment.Right).Expression;
            Assert.Equal(SpecialType.System_Int32, model.GetTypeInfo(right).Type.SpecialType);
        }

        [Fact]
        [WorkItem(28238, "https://github.com/dotnet/roslyn/issues/28238")]
        public void AssigningRef_TypeExpression()
        {
            var compilation = CreateCompilation(@"
public unsafe class C
{
    public void M(int value)
    {
        var temp = new
        {
            object = ref value
        };
    }
}", options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,13): error CS1525: Invalid expression term 'object'
                //             object = ref value
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "object").WithArguments("object").WithLocation(8, 13),
                // (8,13): error CS0746: Invalid anonymous type member declarator. Anonymous type members must be declared with a member assignment, simple name or member access.
                //             object = ref value
                Diagnostic(ErrorCode.ERR_InvalidAnonymousTypeMemberDeclarator, "object = ref value").WithLocation(8, 13));

            var tree = compilation.SyntaxTrees.Single();
            var assignment = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().Single();

            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var left = (PredefinedTypeSyntax)assignment.Left;
            Assert.Equal(SpecialType.System_Object, model.GetTypeInfo(left).Type.SpecialType);

            var right = ((RefExpressionSyntax)assignment.Right).Expression;
            Assert.Equal(SpecialType.System_Int32, model.GetTypeInfo(right).Type.SpecialType);
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        [WorkItem(27772, "https://github.com/dotnet/roslyn/issues/27772")]
        public void RefReturnInvocationOfRefLikeTypeRefResult(LanguageVersion langVersion)
        {
            string source = @"
class C
{
    public ref long M(S receiver)
    {
        long x = 0;
        ref long y = ref receiver.M(ref x);
        return ref y;
    }

    public ref long M2(S receiver)
    {
        long x = 0;
        {
            ref long y = ref receiver.M(ref x);
            return ref y;
        }
    }
}
ref struct S
{
    public ref long M(ref long x) => ref x;
}";

            var comp = CreateCompilationWithMscorlibAndSpan(source, parseOptions: TestOptions.RegularDefault.WithLanguageVersion(langVersion));
            comp.VerifyDiagnostics(
                // (8,20): error CS8157: Cannot return 'y' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref y;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "y").WithArguments("y").WithLocation(8, 20),
                // (16,24): error CS8157: Cannot return 'y' by reference because it was initialized to a value that cannot be returned by reference
                //             return ref y;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "y").WithArguments("y").WithLocation(16, 24));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        [WorkItem(27772, "https://github.com/dotnet/roslyn/issues/27772")]
        public void RefReturnInvocationOfRefLikeTypeRefResult_Repro(LanguageVersion langVersion)
        {
            string source = @"
using System;
class C
{
  static void Main(string[] args)
  {
    ref long x = ref M(default); // get a reference to the stack which will be used for the next method
    M2(ref x); // break things
    Console.ReadKey();
  }

  public static ref long M(S receiver)
  {
    Span<long> ls = stackalloc long[0]; // change the length of this stackalloc to move the resulting pointer and break different things
    long x = 0;
    ref var y = ref x;
    {
      ref var z = ref receiver.M(ref y);
      return ref z;
    }
  }

  static void M2(ref long q)
  {
    Span<long> span = stackalloc long[50];
    var element = span[0]; // it was ok
    q = -1; // break things
    element = span[0]; // and not it's broken:
                       // System.AccessViolationException: 'Attempted to read or write protected memory. This is often an indication that other memory is corrupt.'
  }
}

ref struct S
{
  public ref long M(ref long x) => ref x;
}";

            var comp = CreateCompilationWithMscorlibAndSpan(source, parseOptions: TestOptions.RegularDefault.WithLanguageVersion(langVersion));
            comp.VerifyDiagnostics(
                // (19,18): error CS8157: Cannot return 'z' by reference because it was initialized to a value that cannot be returned by reference
                //       return ref z;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "z").WithArguments("z").WithLocation(19, 18));
        }

        [Fact, WorkItem(49617, "https://github.com/dotnet/roslyn/issues/49617")]
        public void CannotCallExpressionThatReturnsByRefInExpressionTree_02()
        {
            var code = @"
class C
{
    static void Main()
    {
        Test2(c => c.P = true);
    }
    
    ref bool P => throw null;

    static void Test2(System.Linq.Expressions.Expression<System.Action<C>> y){}
}
";

            CreateCompilationWithMscorlib40AndSystemCore(code).VerifyEmitDiagnostics(
                // (6,20): error CS0832: An expression tree may not contain an assignment operator
                //         Test2(c => c.P = true);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "c.P = true").WithLocation(6, 20),
                // (6,20): error CS8153: An expression tree lambda may not contain a call to a method, property, or indexer that returns by reference
                //         Test2(c => c.P = true);
                Diagnostic(ErrorCode.ERR_RefReturningCallInExpressionTree, "c.P").WithLocation(6, 20)
                );
        }

        [Fact, WorkItem(49617, "https://github.com/dotnet/roslyn/issues/49617")]
        public void CannotCallExpressionThatReturnsByRefInExpressionTree_03()
        {
            var code = @"
using System;
using System.Linq.Expressions;

namespace RefPropCrash
{
    class Program
    {
        static void Main(string[] args)
        {
            TestExpression(() => new Model { Value = 1 });
        }

        static void TestExpression(Expression<Func<Model>> expression)
        {
        }
    }

    class Model
    {
        int value;
        public ref int Value => ref value;
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code).VerifyEmitDiagnostics(
                // (11,46): error CS8153: An expression tree lambda may not contain a call to a method, property, or indexer that returns by reference
                //             TestExpression(() => new Model { Value = 1 });
                Diagnostic(ErrorCode.ERR_RefReturningCallInExpressionTree, "Value").WithLocation(11, 46)
                );
        }

        [Fact, WorkItem(49617, "https://github.com/dotnet/roslyn/issues/49617")]
        public void CannotCallExpressionThatReturnsByRefInExpressionTree_04()
        {
            var code = @"
using System;
using System.Linq.Expressions;

namespace RefPropCrash
{
    class Program
    {
        static void Main(string[] args)
        {
            TestExpression(() => new Model { 1, 2, 3 });
        }

        static void TestExpression(Expression<Func<Model>> expression)
        {
        }
    }

    class Model : System.Collections.IEnumerable
    {
        public System.Collections.IEnumerator GetEnumerator() => throw null;
        public ref bool Add(int x) => throw null;
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(code).VerifyEmitDiagnostics(
                // (11,46): error CS8153: An expression tree lambda may not contain a call to a method, property, or indexer that returns by reference
                //             TestExpression(() => new Model { 1, 2, 3 });
                Diagnostic(ErrorCode.ERR_RefReturningCallInExpressionTree, "1").WithLocation(11, 46),
                // (11,49): error CS8153: An expression tree lambda may not contain a call to a method, property, or indexer that returns by reference
                //             TestExpression(() => new Model { 1, 2, 3 });
                Diagnostic(ErrorCode.ERR_RefReturningCallInExpressionTree, "2").WithLocation(11, 49),
                // (11,52): error CS8153: An expression tree lambda may not contain a call to a method, property, or indexer that returns by reference
                //             TestExpression(() => new Model { 1, 2, 3 });
                Diagnostic(ErrorCode.ERR_RefReturningCallInExpressionTree, "3").WithLocation(11, 52)
                );
        }

        [Fact]
        public void RefLocalInUsing()
        {
            var code = @"
var r = new R();
using (ref R r2 = ref r) {}
using ref R r1 = ref r;

struct R : System.IDisposable
{
    public void Dispose() {}
}
";

            CreateCompilation(code).VerifyEmitDiagnostics(
                // (3,8): error CS1073: Unexpected token 'ref'
                // using (ref R r2 = ref r) {}
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(3, 8),
                // (4,7): error CS1073: Unexpected token 'ref'
                // using ref R r1 = ref r;
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(4, 7)
                );
        }

        [Fact]
        [WorkItem(64259, "https://github.com/dotnet/roslyn/issues/64259")]
        public void RefLocalInDeconstruct_01()
        {
            var code = @"
class C
{
    static void Main()
    {
        int x = 0, y = 0;
        (ref int a, ref readonly int b) = (x, y);
    }
}
";

            CreateCompilation(code).VerifyEmitDiagnostics(
                // (7,10): error CS9072: A deconstruction variable cannot be declared as a ref local
                //         (ref int a, ref readonly int b) = (x, y);
                Diagnostic(ErrorCode.ERR_DeconstructVariableCannotBeByRef, "ref").WithLocation(7, 10),
                // (7,21): error CS9072: A deconstruction variable cannot be declared as a ref local
                //         (ref int a, ref readonly int b) = (x, y);
                Diagnostic(ErrorCode.ERR_DeconstructVariableCannotBeByRef, "ref").WithLocation(7, 21)
                );
        }

        [Fact]
        [WorkItem(64259, "https://github.com/dotnet/roslyn/issues/64259")]
        public void RefLocalInDeconstruct_02()
        {
            var code = @"
class C
{
    static void Main()
    {
        int x = 0, y = 0, z = 0;
        (ref var a, ref var (b, c)) = (x, (y, z));
        (ref int d, var e) = (x, y);
    }
}
";

            var comp = CreateCompilation(code).VerifyEmitDiagnostics(
                // (7,10): error CS9072: A deconstruction variable cannot be declared as a ref local
                //         (ref var a, ref var (b, c)) = (x, (y, z));
                Diagnostic(ErrorCode.ERR_DeconstructVariableCannotBeByRef, "ref").WithLocation(7, 10),
                // (7,21): error CS1525: Invalid expression term 'ref'
                //         (ref var a, ref var (b, c)) = (x, (y, z));
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref var (b, c)").WithArguments("ref").WithLocation(7, 21),
                // (7,21): error CS1073: Unexpected token 'ref'
                //         (ref var a, ref var (b, c)) = (x, (y, z));
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(7, 21),
                // (8,10): error CS9072: A deconstruction variable cannot be declared as a ref local
                //         (ref int d, var e) = (x, y);
                Diagnostic(ErrorCode.ERR_DeconstructVariableCannotBeByRef, "ref").WithLocation(8, 10)
            );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var decls = tree.GetRoot().DescendantNodes().OfType<DeclarationExpressionSyntax>().ToArray();

            foreach (var decl in decls)
            {
                var type = decl.Type;

                if (type is RefTypeSyntax refType)
                {
                    Assert.Null(model.GetSymbolInfo(type).Symbol);
                    Assert.Null(model.GetTypeInfo(type).Type);

                    type = refType.Type;
                }

                Assert.Equal("System.Int32", model.GetSymbolInfo(type).Symbol.ToTestDisplayString());
                Assert.Equal("System.Int32", model.GetTypeInfo(type).Type.ToTestDisplayString());
            }
        }

        [Fact]
        [WorkItem(64259, "https://github.com/dotnet/roslyn/issues/64259")]
        public void RefLocalInDeconstruct_03()
        {
            var code = @"
int x = 0, y = 0, z = 0;
(ref var d, var e) = (x, y);
(ref int f, ref var _) = (x, y);
";

            var comp = CreateCompilation(code, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);
            comp.VerifyEmitDiagnostics(
                // (3,2): error CS1073: Unexpected token 'ref'
                // (ref var d, var e) = (x, y);
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(3, 2),
                // (4,2): error CS1073: Unexpected token 'ref'
                // (ref int f, ref var _) = (x, y);
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(4, 2),
                // (4,13): error CS9072: A deconstruction variable cannot be declared as a ref local
                // (ref int f, ref var _) = (x, y);
                Diagnostic(ErrorCode.ERR_DeconstructVariableCannotBeByRef, "ref").WithLocation(4, 13)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var decls = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().ToArray();

            Assert.Equal(3, decls.Length);

            foreach (var decl in decls)
            {
                var f = model.GetDeclaredSymbol(decl).GetSymbol<FieldSymbol>();

                Assert.Equal(RefKind.None, f.RefKind);
                Assert.Equal("System.Int32", f.Type.ToTestDisplayString());
            }
        }

        [Fact]
        public void RefLocalInOutVar_01()
        {
            var code = @"
M(out ref var a);
M(out ref int b);
M(out ref var _);

void M(out int x) => throw null;
";

            var comp = CreateCompilation(code, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);
            comp.VerifyEmitDiagnostics(
                // (2,7): error CS1073: Unexpected token 'ref'
                // M(out ref var a);
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(2, 7),
                // (3,7): error CS1073: Unexpected token 'ref'
                // M(out ref int b);
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(3, 7),
                // (4,7): error CS8388: An out variable cannot be declared as a ref local
                // M(out ref var _);
                Diagnostic(ErrorCode.ERR_OutVariableCannotBeByRef, "ref var").WithLocation(4, 7)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var decls = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().ToArray();

            Assert.Equal(2, decls.Length);

            foreach (var decl in decls)
            {
                var f = model.GetDeclaredSymbol(decl).GetSymbol<FieldSymbol>();

                Assert.Equal(RefKind.None, f.RefKind);
                Assert.Equal("System.Int32", f.Type.ToTestDisplayString());
            }
        }
    }
}
