// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Basic.Reference.Assemblies;
using Utils = Microsoft.CodeAnalysis.CSharp.UnitTests.CompilationUtils;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class ExtensionMethodTests : CSharpTestBase
    {
        [ClrOnlyFact]
        public void IsExtensionMethod()
        {
            var source =
@"static class C
{
    internal static void M1(object o) { }
    internal static void M2(this object o) { }
    internal static void M3<T, U>(this T t, U u) { }
}";
            Action<ModuleSymbol> validator = module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

                // Ordinary method.
                var method = type.GetMember<MethodSymbol>("M1");
                Assert.False(method.IsExtensionMethod);
                var parameter = method.Parameters[0];
                Assert.Equal(SpecialType.System_Object, parameter.Type.SpecialType);

                // Extension method.
                method = type.GetMember<MethodSymbol>("M2");
                Assert.True(method.IsExtensionMethod);
                parameter = method.Parameters[0];
                Assert.Equal(SpecialType.System_Object, parameter.Type.SpecialType);

                // Extension method with type parameters.
                method = type.GetMember<MethodSymbol>("M3");
                Assert.True(method.IsExtensionMethod);
                parameter = method.Parameters[0];
                Assert.Equal(TypeKind.TypeParameter, parameter.Type.TypeKind);
            };
            CompileAndVerify(source, validator: validator, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal));
        }

        /// <summary>
        /// IsExtensionMethod should be false for
        /// invalid extension methods.
        /// </summary>
        [Fact]
        public void InvalidExtensionMethods()
        {
            var ilSource =
@".assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core {}
.assembly '<<GeneratedFileName>>'
{
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
}
.class public C
{
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .method public static void M1()
    {
        .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        ret
    }
    .method public void M2(object o)
    {
        .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        ret
    }
}
.class public S
{
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .method public static void M1()
    {
        .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        ret
    }
    .method public static void M2([out] object& o)
    {
        .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        ret
    }
    .method public static void M3(object[] o)
    {
        .param [1]
        .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 )
        .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
        ret
    }
}
";
            var source = @"class A
{
    internal static C F = null;
    internal static S G = null;
}";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, appendDefaultHeader: false);

            var refType = compilation.Assembly.GlobalNamespace.GetMember<NamedTypeSymbol>("A");
            var type = (NamedTypeSymbol)refType.GetMember<FieldSymbol>("F").Type;

            // Static method no args.
            var method = type.GetMember<MethodSymbol>("M1");
            Assert.Equal(0, method.Parameters.Length);
            Assert.True(method.IsStatic);
            Assert.False(method.IsExtensionMethod);

            // Instance method.
            method = type.GetMember<MethodSymbol>("M2");
            Assert.Equal(1, method.Parameters.Length);
            Assert.False(method.IsStatic);
            Assert.False(method.IsExtensionMethod);

            type = (NamedTypeSymbol)refType.GetMember<FieldSymbol>("G").Type;

            // Static method no args.
            method = type.GetMember<MethodSymbol>("M1");
            Assert.Equal(0, method.Parameters.Length);
            Assert.True(method.IsStatic);
            Assert.False(method.IsExtensionMethod);

            // Static method out param.
            method = type.GetMember<MethodSymbol>("M2");
            Assert.Equal(1, method.Parameters.Length);
            Assert.True(method.IsStatic);
            Assert.False(method.IsExtensionMethod);

            // Static method params array.
            method = type.GetMember<MethodSymbol>("M3");
            Assert.Equal(1, method.Parameters.Length);
            Assert.True(method.IsStatic);
            Assert.False(method.IsExtensionMethod);
        }

        [ClrOnlyFact]
        public void OverloadResolution()
        {
            var source =
@"class C
{
    void N()
    {
        this.M(3);
        (new C()).M(0.5);
    }
}
static class S
{
    public static void M(this object o, int i) { }
    public static void M(this C c, int i) { }
    public static void M(this C c, double x) { }
}";
            CompileAndVerify(source);
        }

        [Fact]
        public void SameNameAsMember()
        {
            var source =
@"class C
{
    public object F = null;
    public object P { get; set; }
    public class T { }
    static void A(System.Action a) { }
    static void B(C c)
    {
        c.F(c.F);
        c.P(c.P);
        c.T();
        A(c.F);
        A(c.P);
        A(((object)c).F);
        A(((object)c).P);
    }
}
static class S
{
    public static void F(this object o) { }
    public static void F(this object x, object y) { }
    public static void P(this object o) { }
    public static void P(this object x, object y) { }
    public static void T(this object o) { }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (12,11): error CS1503: Argument 1: cannot convert from 'object' to 'System.Action'
                Diagnostic(ErrorCode.ERR_BadArgType, "c.F").WithArguments("1", "object", "System.Action").WithLocation(12, 11),
                // (13,11): error CS1503: Argument 1: cannot convert from 'object' to 'System.Action'
                Diagnostic(ErrorCode.ERR_BadArgType, "c.P").WithArguments("1", "object", "System.Action").WithLocation(13, 11));
        }

        [WorkItem(529063, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529063")]
        [Fact]
        public void GetSymbolInfoTest()
        {
            var source =
@"static class S
{
    static void Goo(this string s) { }
    static void Main() { 
        string s = null;
        s.Goo();
    }
}";
            var compilation = CreateCompilation(source);
            var syntaxTree = compilation.SyntaxTrees.Single();
            var gooSymbol = (IMethodSymbol)compilation.GetSemanticModel(syntaxTree).GetSymbolInfo(
                syntaxTree.GetCompilationUnitRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single()).Symbol;
            Assert.True(gooSymbol.IsExtensionMethod);
            Assert.Equal(MethodKind.ReducedExtension, gooSymbol.MethodKind);
            var gooOriginal = gooSymbol.ReducedFrom;
            Assert.True(gooOriginal.IsExtensionMethod);
            Assert.Equal(MethodKind.Ordinary, gooOriginal.MethodKind);
        }

        [Fact]
        public void InaccessibleExtensionMethodSameNameAsMember()
        {
            var source =
@"class C
{
    public object F = null;
    public object P { get; set; }
    static void A(System.Action a) { }
    static void B(C c)
    {
        c.F();
        c.P();
        A(c.F);
        A(c.P);
    }
}
static class S
{
    private static void F(this object o) { }
    private static void P(this object o) { }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (8,11): error CS1955: Non-invocable member 'C.F' cannot be used like a method.
                //         c.F();
                Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "F").WithArguments("C.F"),
                // (9,11): error CS1955: Non-invocable member 'C.P' cannot be used like a method.
                //         c.P();
                Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "P").WithArguments("C.P"),
                // (10,11): error CS1503: Argument 1: cannot convert from 'object' to 'System.Action'
                Diagnostic(ErrorCode.ERR_BadArgType, "c.F").WithArguments("1", "object", "System.Action").WithLocation(10, 11),
                // (11,11): error CS1503: Argument 1: cannot convert from 'object' to 'System.Action'
                Diagnostic(ErrorCode.ERR_BadArgType, "c.P").WithArguments("1", "object", "System.Action").WithLocation(11, 11));
        }

        [ClrOnlyFact]
        public void ExtensionMethodInTheSameClass()
        {
            var source =
@"using System;
static class Program
{
    static void Main()
    {
        ""ABC"".Goo();
        Action a = ""123"".Goo;
        a();
        a = new Action(a);
        a();
        a = new Action(""xyz"".Goo);
        a();
    }
    static void Goo(this string x)
    {
        Console.WriteLine(x);
    }
}";
            CompileAndVerify(source, expectedOutput:
@"ABC
123
123
xyz");
        }

        [WorkItem(541143, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541143")]
        [ClrOnlyFact]
        public void NumericConversionsAreNotAllowed()
        {
            var source =
@"
using System;

static class Program
{
    static void Main()
    {
        0.Goo();
    }

    static void Goo(this long x)
    {
        Console.WriteLine(""long"");
    }

    static void Goo(this object x)
    {
        Console.WriteLine(""object"");
    }
}
";
            CompileAndVerify(source, expectedOutput: "object");
        }

        [WorkItem(541144, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541144")]
        [ClrOnlyFact]
        public void EnumerationConversionsAreNotAllowed()
        {
            var source =
@"
using System;

static class Program
{
    static void Main()
    {
        0.Goo();
    }

    static void Goo(this DayOfWeek x)
    {
        Console.WriteLine(""DayOfWeek"");
    }

    static void Goo(this object x)
    {
        Console.WriteLine(""object"");
    }
}
";
            CompileAndVerify(source, expectedOutput: "object");
        }

        [WorkItem(541145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541145")]
        [ClrOnlyFact]
        public void CannotCreateDelegateToExtensionMethodOnValueType()
        {
            var source =
@"
using System;

static class Program
{
    static void Main()
    {
        Bar(x => x.Goo);
    }

    static void Bar(Func<int, Action> x) { Console.WriteLine(1); }
    static void Bar(Func<object, Action> x) { Console.WriteLine(2); }

    static void Goo<T>(this T x) { }
}
";
            CompileAndVerify(source, expectedOutput: "2");
        }

        [WorkItem(528426, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528426")]
        [ClrOnlyFact]
        public void TypedReferenceCannotBeUsedAsTypeArgument()
        {
            var source =
@"
using System;

static class Program
{
    static void Main()
    {
        Bar(y => new TypedReference().Goo(y));
    }

    static void Bar(Action<string> a) { Console.WriteLine(1); }
    static void Bar(Action<int> a) { Console.WriteLine(2); }

    static void Goo<T>(this T x, string y) { }
    static void Goo(this TypedReference x, int y) { }
}
";
            CompileAndVerify(source, expectedOutput: "2");
        }

        [WorkItem(541146, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541146")]
        [WorkItem(868538, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/868538")]
        [Fact]
        public void VariablesUsedInExtensionMethodGroupMustBeDefinitelyAssigned()
        {
            var source =
@"
using System;

static class Program
{
    static void Main()
    {
        string s;
        bool x = s.Goo is Action;

        int i;
        bool y = i.Goo is Action;
    }

    static void Goo(this string x) { }
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (9,18): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //         bool x = s.Goo is Action;
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "s.Goo is Action").WithLocation(9, 18),
                // (12,20): error CS1061: 'int' does not contain a definition for 'Goo' and no accessible extension method 'Goo' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                //         bool y = i.Goo is Action;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Goo").WithArguments("int", "Goo").WithLocation(12, 20),
                // (9,18): error CS0165: Use of unassigned local variable 's'
                //         bool x = s.Goo is Action;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s").WithArguments("s").WithLocation(9, 18),
                // (12,18): error CS0165: Use of unassigned local variable 'i'
                //         bool y = i.Goo is Action;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "i").WithArguments("i").WithLocation(12, 18)
                );
        }

        [WorkItem(541187, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541187")]
        [Fact]
        public void ExtensionMethodsCannotBeDeclaredInNamespaces()
        {
            var source =
@"
namespace N
{
    static void Goo(this int x) { }
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (4,17): error CS0116: A namespace does not directly contain members such as fields or methods
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "Goo"),
                // (4,17): error CS1106: Extension methods must be defined in a non-generic static class
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "Goo"));
        }

        [WorkItem(541189, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541189")]
        [ClrOnlyFact]
        public void ExtensionMethodsDeclaredInEnclosingNamespaceArePreferredOverImported2()
        {
            var source =
@"
using System;
using N;

static class Program
{
    static void Main()
    {
        """".Goo(1);
    }

    public static void Goo(this string x, object y) { Console.WriteLine(1); }
}

namespace N
{
    static class C
    {
        public static void Goo(this string x, int y) { Console.WriteLine(2); }
    }
}";
            CompileAndVerify(source, expectedOutput: "1");
        }

        [WorkItem(541189, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541189")]
        [ClrOnlyFact]
        public void ExtensionMethodsDeclaredInEnclosingNamespaceArePreferredOverImported()
        {
            var source =
@"
using System;
using N;

static class Program
{
    static void Main()
    {
        """".Goo();
    }

    public static void Goo(this string x) { Console.WriteLine(1); }
}

namespace N
{
    static class C
    {
        public static void Goo(this string x) { Console.WriteLine(2); }
    }
}
";
            CompileAndVerify(source, expectedOutput: "1");
        }

        [ClrOnlyFact]
        public void CandidateSearchByArgType()
        {
            var source =
@"static class A
{
    public static void E(this object o, double d) { }
}
namespace N1
{
    static class B
    {
        public static void E(this object o, bool b) { }
    }
}
namespace N1.N2
{
    static class C
    {
        public static void E(this object o, int i) { }
        static void Main()
        {
            1.E(2.0);
        }
    }
}";
            CompileAndVerify(source);
        }

        [ClrOnlyFact]
        public void CandidateSearchConversion()
        {
            var source =
@"interface I<T> { }
namespace N
{
    class C
    {
        static void M()
        {
            object o = 1.F1();
            o = 2.F2();
            o = 3.F3(1, 2, 3);
        }
    }
    static class S1
    {
        internal static void F1<T>(this I<T> t) { }
        internal static void F2(this double d) { }
        internal static void F3(this long l, params object[] args) { }
    }
}
static class S2
{
    internal static object F1<T>(this T t) { return null; }
    internal static object F2(this int i) { return null; }
    internal static object F3(this int i, params object[] args) { return null; }
}";
            CompileAndVerify(source);
        }

        /// <summary>
        /// Continue search for extension method candidates in certain
        /// cases where nearer candidates are not applicable.
        /// </summary>
        [Fact]
        public void CandidateSearch()
        {
            var source =
@"namespace N1
{
    namespace N2
    {
        partial class C
        {
            // Invalid calls with no extension methods in scope.
            void M()
            {
                this.M1(1, 2, 3); // MethodResolutionKind.NoCorrespondingParameter
                this.M2(1); // MethodResolutionKind.RequiredParameterMissing
                this.M3(1, 2.0); // MethodResolutionKind.BadArguments
                this.M4(null, 2); // MethodResolutionKind.TypeInferenceFailed
                this.M5<string, string>(null, 2); // Bad arity
                this.M6(null, null); // Ambiguous
            }
            void M1(int x, int y) { }
            void M2(int x, int y) { }
            void M3(int x, int y) { }
            void M3(int x, long y) { }
            void M4<T>(T x, int y) { }
            void M4<T>(T x, long y) { }
            void M5<T>(T x, int y) { }
            void M5<T>(T x, long y) { }
            void M6(object x, string y) { }
            void M6(string x, object y) { }
        }
    }
}
namespace N1
{
    using N4;
    namespace N2
    {
        using N3;
        partial class C
        {
            // Same calls as above but with N3.S and N4.S extension methods in scope.
            void N()
            {
                this.M1(1, 2, 3); // MethodResolutionKind.NoCorrespondingParameter
                this.M2(1); // MethodResolutionKind.RequiredParameterMissing
                this.M3(1, 2.0); // MethodResolutionKind.BadArguments
                this.M4(null, 2); // MethodResolutionKind.TypeInferenceFailed
                this.M5<string, string>(null, 2); // Bad arity
                this.M6(null, null); // Ambiguous
            }
        }
    }
}
namespace N3
{
    static class S
    {
        // Same signatures as instance methods above.
        public static void M1(this N1.N2.C c, int x, int y) { }
        public static void M2(this N1.N2.C c, int x, int y) { }
        public static void M3(this N1.N2.C c, int x, int y) { }
        public static void M3(this N1.N2.C c, int x, long y) { }
        public static void M4<T>(this N1.N2.C c, T x, int y) { }
        public static void M4<T>(this N1.N2.C c, T x, long y) { }
        public static void M5<T>(this N1.N2.C c, T x, int y) { }
        public static void M5<T>(this N1.N2.C c, T x, long y) { }
        public static void M6(this N1.N2.C c, object x, string y) { }
        public static void M6(this N1.N2.C c, string x, object y) { }
    }
}
namespace N4
{
    static class S
    {
        // Different signatures but also resulting in errors.
        public static void M1(this N1.N2.C c, string x, int y, int z) { }
        public static void M2(this N1.N2.C c, string x) { }
        public static void M3(this N1.N2.C c, string x) { }
        public static void M4(this N1.N2.C c, int x, int y) { }
        public static void M5<T, U>(this N1.N2.C c, T x, T y) { }
        public static void M6(this N1.N2.C c, int x, int y) { }
    }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (10,17): error CS1501: No overload for method 'M1' takes 3 arguments
                //                 this.M1(1, 2, 3); // MethodResolutionKind.NoCorrespondingParameter
                Diagnostic(ErrorCode.ERR_BadArgCount, "M1").WithArguments("M1", "3").WithLocation(10, 22),
                // (11,17): error CS7036: There is no argument given that corresponds to the required parameter 'y' of 'N1.N2.C.M2(int, int)'
                //                 this.M2(1); // MethodResolutionKind.RequiredParameterMissing
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M2").WithArguments("y", "N1.N2.C.M2(int, int)").WithLocation(11, 22),
                // (12,28): error CS1503: Argument 2: cannot convert from 'double' to 'int'
                //                 this.M3(1, 2.0); // MethodResolutionKind.BadArguments
                Diagnostic(ErrorCode.ERR_BadArgType, "2.0").WithArguments("2", "double", "int").WithLocation(12, 28),
                // (13,17): error CS0411: The type arguments for method 'N1.N2.C.M4<T>(T, int)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //                 this.M4(null, 2); // MethodResolutionKind.TypeInferenceFailed
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M4").WithArguments("N1.N2.C.M4<T>(T, int)").WithLocation(13, 22),
                // (14,22): error CS0305: Using the generic method 'N1.N2.C.M5<T>(T, int)' requires 1 type arguments
                //                 this.M5<string, string>(null, 2); // Bad arity
                Diagnostic(ErrorCode.ERR_BadArity, "M5<string, string>").WithArguments("N1.N2.C.M5<T>(T, int)", "method", "1").WithLocation(14, 22),
                // (15,17): error CS0121: The call is ambiguous between the following methods or properties: 'N1.N2.C.M6(object, string)' and 'N1.N2.C.M6(string, object)'
                //                 this.M6(null, null); // Ambiguous
                Diagnostic(ErrorCode.ERR_AmbigCall, "M6").WithArguments("N1.N2.C.M6(object, string)", "N1.N2.C.M6(string, object)").WithLocation(15, 22),
                // (41,17): error CS1501: No overload for method 'M1' takes 3 arguments
                //                 this.M1(1, 2, 3); // MethodResolutionKind.NoCorrespondingParameter
                Diagnostic(ErrorCode.ERR_BadArgCount, "M1").WithArguments("M1", "3").WithLocation(41, 22),
                // (42,17): error CS7036: There is no argument given that corresponds to the required parameter 'y' of 'N1.N2.C.M2(int, int)'
                //                 this.M2(1); // MethodResolutionKind.RequiredParameterMissing
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M2").WithArguments("y", "N1.N2.C.M2(int, int)").WithLocation(42, 22),
                // (43,28): error CS1503: Argument 2: cannot convert from 'double' to 'int'
                //                 this.M3(1, 2.0); // MethodResolutionKind.BadArguments
                Diagnostic(ErrorCode.ERR_BadArgType, "2.0").WithArguments("2", "double", "int").WithLocation(43, 28),
                // (44,17): error CS0411: The type arguments for method 'N1.N2.C.M4<T>(T, int)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //                 this.M4(null, 2); // MethodResolutionKind.TypeInferenceFailed
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M4").WithArguments("N1.N2.C.M4<T>(T, int)").WithLocation(44, 22),
                // (45,47): error CS1503: Argument 3: cannot convert from 'int' to 'string'
                //                 this.M5<string, string>(null, 2); // Bad arity
                Diagnostic(ErrorCode.ERR_BadArgType, "2").WithArguments("3", "int", "string").WithLocation(45, 47),
                // (46,17): error CS0121: The call is ambiguous between the following methods or properties: 'N1.N2.C.M6(object, string)' and 'N1.N2.C.M6(string, object)'
                //                 this.M6(null, null); // Ambiguous
                Diagnostic(ErrorCode.ERR_AmbigCall, "M6").WithArguments("N1.N2.C.M6(object, string)", "N1.N2.C.M6(string, object)").WithLocation(46, 22));
        }

        /// <summary>
        /// End search for extension method candidates
        /// if current method group is ambiguous.
        /// </summary>
        [Fact]
        public void EndSearchIfAmbiguous()
        {
            var source =
@"namespace N1
{
    internal static class S
    {
        public static void E(this object o, int x, object y) { }
        public static void E(this object o, double x, int y) { }
        public static void E(this object o, A x, B y) { }
    }
    class A { }
    class B { }
    namespace N2
    {
        internal static class S
        {
            public static void E(this object o, double x, A y) { }
            public static void E(this object o, double x, B y) { }
        }
        class C
        {
            static void M(object o)
            {
                o.E(1, null); // ambiguous
                o.E(1.0, 2.0); // N2.S.E(object, double, A)
                o.E(1.0, 2); // N1.S.E(object, double, int)
                o.E(null, null); // N1.S.E(object, A, B)
            }
        }
    }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (22,17): error CS0121: The call is ambiguous between the following methods or properties: 'N1.N2.S.E(object, double, N1.A)' and 'N1.N2.S.E(object, double, N1.B)'
                Diagnostic(ErrorCode.ERR_AmbigCall, "E").WithArguments("N1.N2.S.E(object, double, N1.A)", "N1.N2.S.E(object, double, N1.B)").WithLocation(22, 19),
                // (23,26): error CS1503: Argument 3: cannot convert from 'double' to 'N1.A'
                Diagnostic(ErrorCode.ERR_BadArgType, "2.0").WithArguments("3", "double", "N1.A").WithLocation(23, 26));
        }

        [Fact(Skip = "528425")]
        public void ParenthesizedMethodGroup()
        {
            var source =
@"static class S
{
    public static void E(this object a, object b) { }
    public static void E(this object o, int i) { }
    private static void E(this object o, object x, object y) { }
}
class C
{
    void E(int i, int j) { }
    void M()
    {
        ((this.E))(null);
        ((this.E))(null, null);
    }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (13, 9): error CS0122: 'S.E(object, object, object)' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "((this.E))(null, null)").WithArguments("S.E(object, object, object)").WithLocation(13, 9));
        }

        [ClrOnlyFact]
        public void DelegateMembers()
        {
            var source =
@"using System;
class C
{
    public Action<int> F = A;
    public Action<int> P { get { return A; } }
    static void A(int i)
    {
        Console.WriteLine(i);
    }
    static void Main()
    {
        C c = new C();
        c.F(1);
        c.P(2);
    }
}";
            CompileAndVerify(source, expectedOutput:
@"1
2");
        }

        [Fact]
        public void DelegatesAndExtensionMethods()
        {
            var source =
@"using System;
class C
{
    public Action<int> F = A;
    public Action<int> P { get { return A; } }
    static void A(int i) { }
    void M()
    {
        this.F(1, 2);
        this.P(1.0);
    }
}
class D
{
    void F(int i) { }
    void P(int i) { }
    void M()
    {
        this.F(1, 2);
        this.P(2.0);
    }
}
static class S
{
    public static void F(this object o, int x, int y) { }
    public static void P(this object o, double d) { }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (9, 9): error CS1593: Delegate 'System.Action<int>' does not take 2 arguments
                Diagnostic(ErrorCode.ERR_BadDelArgCount, "F").WithArguments("System.Action<int>", "2").WithLocation(9, 14),
                // (10,16): error CS1503: Argument 1: cannot convert from 'double' to 'int'
                Diagnostic(ErrorCode.ERR_BadArgType, "1.0").WithArguments("1", "double", "int").WithLocation(10, 16));
        }

        [ClrOnlyFact]
        public void DelegatesFromOverloads()
        {
            var source =
@"namespace N
{
    class C
    {
        void M(System.Action<object> a)
        {
            M(this.F);
            a = this.F;
            C c = new C();
            M(c.G);
            a = c.G;
        }
    }
    static class A
    {
        internal static void G(this C c) { }
    }
}
static class B
{
    internal static void F(this object o) { }
    internal static void F(this object x, object y) { }
    internal static void G(this object x, object y) { }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("N.C.M",
@"{
  // Code size       71 (0x47)
  .maxstack  3
  .locals init (N.C V_0) //c
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldftn      ""void B.F(object, object)""
  IL_0008:  newobj     ""System.Action<object>..ctor(object, System.IntPtr)""
  IL_000d:  call       ""void N.C.M(System.Action<object>)""
  IL_0012:  ldarg.0
  IL_0013:  ldftn      ""void B.F(object, object)""
  IL_0019:  newobj     ""System.Action<object>..ctor(object, System.IntPtr)""
  IL_001e:  starg.s    V_1
  IL_0020:  newobj     ""N.C..ctor()""
  IL_0025:  stloc.0
  IL_0026:  ldarg.0
  IL_0027:  ldloc.0
  IL_0028:  ldftn      ""void B.G(object, object)""
  IL_002e:  newobj     ""System.Action<object>..ctor(object, System.IntPtr)""
  IL_0033:  call       ""void N.C.M(System.Action<object>)""
  IL_0038:  ldloc.0
  IL_0039:  ldftn      ""void B.G(object, object)""
  IL_003f:  newobj     ""System.Action<object>..ctor(object, System.IntPtr)""
  IL_0044:  starg.s    V_1
  IL_0046:  ret
}");
        }

        [ClrOnlyFact]
        public void DelegatesAsArguments()
        {
            var source =
@"using System;
namespace N
{
    class C
    {
        static void M(object o)
        {
            o.M1(o.F1); // S1.M1(S1.F1)
            o.M1(o.F2); // S1.M1(S2.F2)
            o.M2(o.F3); // S2.M2(S1.F3)
            o.M2(o.F4); // S2.M2(S2.F4)
        }
    }
    static class S1
    {
        internal static void M1(this object o, Action<object> f) { }
        internal static void M2(this object o, Action f) { }
        internal static void F1(this object x, object y) { }
        internal static void F2(this object x, int y) { }
        internal static void F3(this object x, object y) { }
        internal static void F4(this object x, int y) { }
    }
}
static class S2
{
    internal static void M1(this object o, Action f) { }
    internal static void M2(this object o, Action<object> f) { }
    internal static void F1(this object x, int y) { }
    internal static void F2(this object x, object y) { }
    internal static void F3(this object x, int y) { }
    internal static void F4(this object x, object y) { }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("N.C.M",
@"
{
  // Code size       73 (0x49)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldftn      ""void N.S1.F1(object, object)""
  IL_0008:  newobj     ""System.Action<object>..ctor(object, System.IntPtr)""
  IL_000d:  call       ""void N.S1.M1(object, System.Action<object>)""
  IL_0012:  ldarg.0
  IL_0013:  ldarg.0
  IL_0014:  ldftn      ""void S2.F2(object, object)""
  IL_001a:  newobj     ""System.Action<object>..ctor(object, System.IntPtr)""
  IL_001f:  call       ""void N.S1.M1(object, System.Action<object>)""
  IL_0024:  ldarg.0
  IL_0025:  ldarg.0
  IL_0026:  ldftn      ""void N.S1.F3(object, object)""
  IL_002c:  newobj     ""System.Action<object>..ctor(object, System.IntPtr)""
  IL_0031:  call       ""void S2.M2(object, System.Action<object>)""
  IL_0036:  ldarg.0
  IL_0037:  ldarg.0
  IL_0038:  ldftn      ""void S2.F4(object, object)""
  IL_003e:  newobj     ""System.Action<object>..ctor(object, System.IntPtr)""
  IL_0043:  call       ""void S2.M2(object, System.Action<object>)""
  IL_0048:  ret
}");
        }

        [Fact]
        public void DelegatesFromInvalidOverloads()
        {
            var source =
@"namespace N
{
    class C
    {
        static void M1(System.Func<object, object> f) { }
        static void M2(System.Action<object> f) { }
        static void M()
        {
            C c = new C();
            M1(c.F1); // wrong return type
            M2(c.F1);
            M1(c.F2);
            M2(c.F2); // wrong return type
            M1(c.F3); // ambiguous
        }
    }
    static class S1
    {
        internal static void F1(this C c) { }
        internal static object F2(this C c) { return null; }
    }
}
static class S2
{
    internal static void F1(this object x, object y) { }
    internal static object F2(this N.C x, object y) { return null; }
    internal static object F3(this N.C x, object y) { return null; }
}
static class S3
{
    internal static object F3(this N.C x, object y) { return null; }
}";
            CreateCompilationWithMscorlib40(source, references: new[] { Net40.References.SystemCore },
                    parseOptions: TestOptions.WithoutImprovedOverloadCandidates).VerifyDiagnostics(
                // (10,16): error CS0407: 'void S2.F1(object, object)' has the wrong return type
                //             M1(c.F1); // wrong return type
                Diagnostic(ErrorCode.ERR_BadRetType, "c.F1").WithArguments("S2.F1(object, object)", "void").WithLocation(10, 16),
                // (13,16): error CS0407: 'object S2.F2(C, object)' has the wrong return type
                //             M2(c.F2); // wrong return type
                Diagnostic(ErrorCode.ERR_BadRetType, "c.F2").WithArguments("S2.F2(N.C, object)", "object").WithLocation(13, 16),
                // (14,16): error CS0121: The call is ambiguous between the following methods or properties: 'S2.F3(C, object)' and 'S3.F3(C, object)'
                //             M1(c.F3); // ambiguous
                Diagnostic(ErrorCode.ERR_AmbigCall, "c.F3").WithArguments("S2.F3(N.C, object)", "S3.F3(N.C, object)").WithLocation(14, 16));
            // NOTE: we have a degradation in the quality of diagnostics for a delegate conversion in this particular failure case.
            // See https://github.com/dotnet/roslyn/issues/24787
            // It is caused by a combination of two shortcomings  in the computation of diagnostics. First, in `BindExtensionMethod`
            // when we fail to find an applicable extension method, we only report a diagnostic for the first extension method group
            // that failed, even if some other extension method group contains a much better candidate. In the case of this test the first
            // extension method group contains a method with the wrong number of parameters, while the second one has an extension method
            // that fails only because of its return type mismatch.  Second, in
            // `OverloadResolutionResult<TMember>.ReportDiagnostics<T>`, we do not report a diagnostic for the failure
            // `MemberResolutionKind.NoCorrespondingParameter`, leaving it to the caller to notice that we failed to produce a
            // diagnostic (the caller has to grub through the diagnostic bag to see that there is no error there) and then the caller
            // has to produce a generic error message, which we see below. It does not appear that all callers have that test, though,
            // suggesting there may be a latent bug of missing diagnostics.
            CreateCompilationWithMscorlib40(source, references: new[] { Net40.References.SystemCore }).VerifyDiagnostics(
                // (10,16): error CS1503: Argument 1: cannot convert from 'method group' to 'Func<object, object>'
                //             M1(c.F1); // wrong return type
                Diagnostic(ErrorCode.ERR_BadArgType, "c.F1").WithArguments("1", "method group", "System.Func<object, object>").WithLocation(10, 16),
                // (13,16): error CS1503: Argument 1: cannot convert from 'method group' to 'Action<object>'
                //             M2(c.F2); // wrong return type
                Diagnostic(ErrorCode.ERR_BadArgType, "c.F2").WithArguments("1", "method group", "System.Action<object>").WithLocation(13, 16),
                // (14,16): error CS0121: The call is ambiguous between the following methods or properties: 'S2.F3(C, object)' and 'S3.F3(C, object)'
                //             M1(c.F3); // ambiguous
                Diagnostic(ErrorCode.ERR_AmbigCall, "c.F3").WithArguments("S2.F3(N.C, object)", "S3.F3(N.C, object)").WithLocation(14, 16));
        }

        [Fact]
        public void DelegatesAsInvalidArguments()
        {
            var source =
@"class A { }
class B { }
delegate A DA(DA a);
delegate B DB(DB b);
class C
{
    static void M()
    {
        M1(F);
        M2(F);
        M1(G(F));
        M2(G(F));
    }
    static void M1(DA f) { }
    static void M2(DB f) { }
    static A F(DA a) { return null; }
    static B F(DB b) { return null; }
    static DA G(DA f) { return f; }
    static DB G(DB f) { return f; }
}
namespace N
{
    class C
    {
        static void M(object o)
        {
            o.M1(o.F);
            o.M2(o.F);
            o.M1(G(o.F));
            o.M2(G(o.F));
        }
        static DA G(DA f) { return f; }
        static DB G(DB f) { return f; }
    }
    static class S1
    {
        internal static void M1(this object o, DA f) { }
        internal static void M2(this object o, DB f) { }
        internal static A F(this object o, DA a) { return null; }
    }
}
static class S2
{
    internal static B F(this object o, DB b) { return null; }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (11,12): error CS0121: The call is ambiguous between the following methods or properties: 'C.G(DA)' and 'C.G(DB)'
                Diagnostic(ErrorCode.ERR_AmbigCall, "G").WithArguments("C.G(DA)", "C.G(DB)").WithLocation(11, 12),
                // (12,12): error CS0121: The call is ambiguous between the following methods or properties: 'C.G(DA)' and 'C.G(DB)'
                Diagnostic(ErrorCode.ERR_AmbigCall, "G").WithArguments("C.G(DA)", "C.G(DB)").WithLocation(12, 12),
                // (29,18): error CS0121: The call is ambiguous between the following methods or properties: 'N.C.G(DA)' and 'N.C.G(DB)'
                Diagnostic(ErrorCode.ERR_AmbigCall, "G").WithArguments("N.C.G(DA)", "N.C.G(DB)").WithLocation(29, 18),
                // (30,18): error CS0121: The call is ambiguous between the following methods or properties: 'N.C.G(DA)' and 'N.C.G(DB)'
                Diagnostic(ErrorCode.ERR_AmbigCall, "G").WithArguments("N.C.G(DA)", "N.C.G(DB)").WithLocation(30, 18));
        }

        /// <summary>
        /// Extension methods should be resolved correctly even
        /// in cases where a method group is not allowed.
        /// </summary>
        [Fact]
        public void InvalidUseOfExtensionMethodGroup()
        {
            var source =
@"class C
{
    static void M(object o)
    {
        o.E += o.E;
        if (o.E != null)
        {
            M(o.E);
            o.E.ToString();
            o = !o.E;
        }
        o.F += o.F;
        if (o.F != null)
        {
            M(o.F);
            o.F.ToString();
            o = !o.F;
        }
        o.E.F();
    }
}
static class S
{
    internal static object E(this object o) { return null; }
    private static object F(this object o) { return null; }
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            compilation.VerifyDiagnostics(
                // (5,9): error CS1656: Cannot assign to 'E' because it is a 'method group'
                //         o.E += o.E;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "o.E").WithArguments("E", "method group").WithLocation(5, 9),
                // (6,13): error CS0019: Operator '!=' cannot be applied to operands of type 'method group' and '<null>'
                //         if (o.E != null)
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "o.E != null").WithArguments("!=", "method group", "<null>").WithLocation(6, 13),
                // (8,15): error CS1503: Argument 1: cannot convert from 'method group' to 'object'
                //             M(o.E);
                Diagnostic(ErrorCode.ERR_BadArgType, "o.E").WithArguments("1", "method group", "object").WithLocation(8, 15),
                // (9,15): error CS0119: 'S.E(object)' is a method, which is not valid in the given context
                //             o.E.ToString();
                Diagnostic(ErrorCode.ERR_BadSKunknown, "E").WithArguments("S.E(object)", "method").WithLocation(9, 15),
                // (10,17): error CS0023: Operator '!' cannot be applied to operand of type 'method group'
                //             o = !o.E;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "!o.E").WithArguments("!", "method group").WithLocation(10, 17),
                // (12,11): error CS1061: 'object' does not contain a definition for 'F' and no accessible extension method 'F' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //         o.F += o.F;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F").WithArguments("object", "F").WithLocation(12, 11),
                // (12,18): error CS1061: 'object' does not contain a definition for 'F' and no accessible extension method 'F' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //         o.F += o.F;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F").WithArguments("object", "F").WithLocation(12, 18),
                // (13,15): error CS1061: 'object' does not contain a definition for 'F' and no accessible extension method 'F' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //         if (o.F != null)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F").WithArguments("object", "F").WithLocation(13, 15),
                // (15,17): error CS1061: 'object' does not contain a definition for 'F' and no accessible extension method 'F' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //             M(o.F);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F").WithArguments("object", "F").WithLocation(15, 17),
                // (16,15): error CS1061: 'object' does not contain a definition for 'F' and no accessible extension method 'F' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //             o.F.ToString();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F").WithArguments("object", "F").WithLocation(16, 15),
                // (17,20): error CS1061: 'object' does not contain a definition for 'F' and no accessible extension method 'F' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //             o = !o.F;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F").WithArguments("object", "F").WithLocation(17, 20),
                // (19,11): error CS0119: 'S.E(object)' is a method, which is not valid in the given context
                //         o.E.F();
                Diagnostic(ErrorCode.ERR_BadSKunknown, "E").WithArguments("S.E(object)", "method").WithLocation(19, 11)
            );
        }

        [Fact]
        public void Inaccessible()
        {
            var source =
@"using System;
class C
{
    static void M(object o)
    {
        o.F();
        M(o.F);
        Action a = o.F;
        o = o.F;
    }
    static void M(Action a) { }
}
static class S
{
    static void F(this object o) { }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,11): error CS1061: 'object' does not contain a definition for 'F' and no extension method 'F' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //         o.F();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F").WithArguments("object", "F").WithLocation(6, 11),
                // (7,13): error CS1061: 'object' does not contain a definition for 'F' and no extension method 'F' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //         M(o.F);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F").WithArguments("object", "F").WithLocation(7, 13),
                // (8,22): error CS1061: 'object' does not contain a definition for 'F' and no extension method 'F' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //         Action a = o.F;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F").WithArguments("object", "F").WithLocation(8, 22),
                // (9,15): error CS1061: 'object' does not contain a definition for 'F' and no extension method 'F' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //         o = o.F;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F").WithArguments("object", "F").WithLocation(9, 15)
            );
        }

        [Fact(Skip = "528425")]
        public void InaccessibleAndAccessible()
        {
            var source =
@"using System;
namespace N
{
    class C
    {
        static void M(object o)
        {
            o.F(null);
            o.F();
            M1(o.F);
            M2(o.F);
            Action<object> a = o.F;
            Action b = o.F;
            o.G(); // no error
        }
        static void M1(Action<object> a) { }
        static void M2(Action a) { }
    }
    static class S1
    {
        static void F(this object o) { }
        internal static void F(this object x, object y) { }
        static void G(this object o) { }
    }
}
static class S2
{
    internal static void G(this object o) { }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (9,9): error CS0122: 'S.F(object)' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "F").WithArguments("S.F(object)").WithLocation(9, 9),
                // (11,9): error CS1503: Argument 1: cannot convert from 'method group' to 'System.Action'
                Diagnostic(ErrorCode.ERR_BadArgType, "o.F").WithArguments("1", "method group", "System.Action").WithLocation(11, 9),
                // (13,20): error CS0122: 'S.F(object)' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "F").WithArguments("S.F(object)").WithLocation(13, 20));
        }

        /// <summary>
        /// Inaccessible instance member and
        /// extension method of same name.
        /// </summary>
        [Fact]
        public void InaccessibleInstanceMember()
        {
            var source =
@"using System;
class A
{
    void F() { }
    Action G;
    A H;
}
namespace N1
{
    class B
    {
        // No extension methods.
        static void M(A a)
        {
            a.F();
            a.G();
            a.H();
            M(a.F);
            M(a.G);
            M(a.H);
        }
        static void M(Action a) { }
    }
}
namespace N2
{
    class C
    {
        // Valid extension methods.
        static void M(A a)
        {
            a.F();
            a.G();
            a.H();
            M(a.F);
            M(a.G);
            M(a.H);
        }
        static void M(Action a) { }
    }
    static class S
    {
        internal static void F(this A a) { }
        internal static void G(this A a) { }
        internal static void H(this A a) { }
    }
}
namespace N3
{
    class C
    {
        // Inaccessible extension methods.
        static void M(A a)
        {
            a.F();
            a.G();
            a.H();
            M(a.F);
            M(a.G);
            M(a.H);
        }
        static void M(Action a) { }
    }
    static class S
    {
        static void F(this A a) { }
        static void G(this A a) { }
        static void H(this A a) { }
    }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (15,15): error CS0122: 'A.F()' is inaccessible due to its protection level
                //             a.F();
                Diagnostic(ErrorCode.ERR_BadAccess, "F").WithArguments("A.F()"),
                // (16,15): error CS0122: 'A.G' is inaccessible due to its protection level
                //             a.G();
                Diagnostic(ErrorCode.ERR_BadAccess, "G").WithArguments("A.G"),
                // (17,15): error CS1955: Non-invocable member 'A.H' cannot be used like a method.
                //             a.H();
                Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "H").WithArguments("A.H"),
                // (18,17): error CS0122: 'A.F()' is inaccessible due to its protection level
                //             M(a.F);
                Diagnostic(ErrorCode.ERR_BadAccess, "F").WithArguments("A.F()"),
                // (19,17): error CS0122: 'A.G' is inaccessible due to its protection level
                //             M(a.G);
                Diagnostic(ErrorCode.ERR_BadAccess, "G").WithArguments("A.G"),
                // (20,17): error CS0122: 'A.H' is inaccessible due to its protection level
                //             M(a.H);
                Diagnostic(ErrorCode.ERR_BadAccess, "H").WithArguments("A.H"),
                // (55,15): error CS0122: 'A.F()' is inaccessible due to its protection level
                //             a.F();
                Diagnostic(ErrorCode.ERR_BadAccess, "F").WithArguments("A.F()"),
                // (56,15): error CS0122: 'A.G' is inaccessible due to its protection level
                //             a.G();
                Diagnostic(ErrorCode.ERR_BadAccess, "G").WithArguments("A.G"),
                // (57,15): error CS1955: Non-invocable member 'A.H' cannot be used like a method.
                //             a.H();
                Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "H").WithArguments("A.H"),
                // (58,17): error CS0122: 'A.F()' is inaccessible due to its protection level
                //             M(a.F);
                Diagnostic(ErrorCode.ERR_BadAccess, "F").WithArguments("A.F()"),
                // (59,17): error CS0122: 'A.G' is inaccessible due to its protection level
                //             M(a.G);
                Diagnostic(ErrorCode.ERR_BadAccess, "G").WithArguments("A.G"),
                // (60,17): error CS0122: 'A.H' is inaccessible due to its protection level
                //             M(a.H);
                Diagnostic(ErrorCode.ERR_BadAccess, "H").WithArguments("A.H"),
                // (5,12): warning CS0169: The field 'A.G' is never used
                //     Action G;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "G").WithArguments("A.G"),
                // (6,7): warning CS0169: The field 'A.H' is never used
                //     A H;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "H").WithArguments("A.H"));
        }

        /// <summary>
        /// Method arguments should be evaluated,
        /// even if too many.
        /// </summary>
        [Fact]
        public void InaccessibleTooManyArgs()
        {
            var source =
@"static class S
{
    static void E(this object o) { }
}
class A
{
    static void F() { }
    void G() { }
}
class B
{
    static void M()
    {
        A a = null;
        M(a.E(), A.F(), a.G());
    }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (15,13): error CS1061: 'A' does not contain a definition for 'E' and no extension method 'E' accepting a first argument of type 'A' could be found (are you missing a using directive or an assembly reference?)
                //         M(a.E(), A.F(), a.G());
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "E").WithArguments("A", "E").WithLocation(15, 13),
                // (15,20): error CS0122: 'A.F()' is inaccessible due to its protection level
                //         M(a.E(), A.F(), a.G());
                Diagnostic(ErrorCode.ERR_BadAccess, "F").WithArguments("A.F()").WithLocation(15, 20),
                // (15,27): error CS0122: 'A.G()' is inaccessible due to its protection level
                //         M(a.E(), A.F(), a.G());
                Diagnostic(ErrorCode.ERR_BadAccess, "G").WithArguments("A.G()").WithLocation(15, 27)
            );
        }

        [WorkItem(541330, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541330")]
        [WorkItem(541335, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541335")]
        [Fact]
        public void ReturnDelegateAsObject()
        {
            var source =
@"class C
{
    static object M(object o)
    {
        return o.E;
    }
}
static class S
{
    internal static void E(this object o) { }
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            compilation.VerifyDiagnostics(
                // (5,18): error CS0428: Cannot convert method group 'E' to non-delegate type 'object'. Did you intend to invoke the method?
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "E").WithArguments("E", "object").WithLocation(5, 18));

            compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (5,16): warning CS8974: Converting method group 'E' to non-delegate type 'object'. Did you intend to invoke the method?
                //         return o.E;
                Diagnostic(ErrorCode.WRN_MethGrpToNonDel, "o.E").WithArguments("E", "object").WithLocation(5, 16));
        }

        [Fact]
        public void AllExtensionMethodsInaccessible()
        {
            var source =
@"namespace N
{
    class A
    {
        void F() { }
    }
    class C
    {
        void M(A a)
        {
            a.F(); // instance and extension methods
            a.G(); // only extension methods
        }
    }
    static class S1
    {
        static void F(this object o) { }
        static void G(this object o) { }
    }
}
static class S2
{
    static void F(this object o) { }
    static void G(this object o) { }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (11,15): error CS0122: 'A.F()' is inaccessible due to its protection level
                //             a.F(); // instance and extension methods
                Diagnostic(ErrorCode.ERR_BadAccess, "F").WithArguments("N.A.F()").WithLocation(11, 15),
                // (12,15): error CS1061: 'A' does not contain a definition for 'G' and no extension method 'G' accepting a first argument of type 'A' could be found (are you missing a using directive or an assembly reference?)
                //             a.G(); // only extension methods
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "G").WithArguments("N.A", "G").WithLocation(12, 15)
            );
        }

        [WorkItem(868538, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/868538")]
        [Fact]
        public void IsAndAs()
        {
            var source =
@"delegate void D();
static class S
{
    internal static void E(this object o) { }
}
class C
{
    static void M(C c)
    {
        if (F is D)
        {
            (F as D)();
        }
        if (c.F is D)
        {
            (c.F as D)();
        }
        if (G is D)
        {
            (G as D)();
        }
        if (c.E is D)
        {
            (c.E as D)();
        }
    }
    void F() { }
    static void G() { }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (10,13): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //         if (F is D)
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "F is D").WithLocation(10, 13),
                // (12,14): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //             (F as D)();
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "F as D").WithLocation(12, 14),
                // (14,13): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //         if (c.F is D)
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "c.F is D").WithLocation(14, 13),
                // (16,14): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //             (c.F as D)();
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "c.F as D").WithLocation(16, 14),
                // (18,13): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //         if (G is D)
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "G is D").WithLocation(18, 13),
                // (20,14): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //             (G as D)();
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "G as D").WithLocation(20, 14),
                // (22,13): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //         if (c.E is D)
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "c.E is D").WithLocation(22, 13),
                // (24,14): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //             (c.E as D)();
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "c.E as D").WithLocation(24, 14));
        }

        [Fact]
        public void Casts()
        {
            var source =
@"delegate void D();
static class S
{
    internal static void E(this object o) { }
}
class C
{
    static void M(C c)
    {
        ((D)c.F)();
        ((D)G)();
        ((D)c.E)();
    }
    void F() { }
    static void G() { }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void NoReceiver()
        {
            var source =
@"class C
{
    void M()
    {
        E();
    }
}
static class S
{
    public static void E(this C c) { }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_NameNotInContext, "E").WithArguments("E").WithLocation(5, 9));
        }

        [Fact]
        public void BaseReceiver()
        {
            var source =
@"class C
{
}
class D : C
{
    void M()
    {
        base.E();
    }
}
static class S
{
    public static void E(this C c) { }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_NoSuchMember, "E").WithArguments("C", "E").WithLocation(8, 14));
        }

        [ClrOnlyFact]
        public void DefinedInSameClass()
        {
            var source =
@"static class C
{
    static void M(this string s, int i)
    {
        s.M(i + 1);
    }
}";
            CompileAndVerify(source);
        }

        /// <summary>
        /// Should not favor method from one class over another in same
        /// namespace, even if one method is defined in caller's class.
        /// </summary>
        [Fact]
        public void AmbiguousMethodDifferentClassesSameNamespace()
        {
            var source =
@"static class A
{
    public static void E(this string s, int i) { }
}
static class B
{
    public static void E(this string s, int i) { }
    static void M(string s)
    {
        s.E(1);
    }
}
static class C
{
    static void M(string s)
    {
        s.E(2);
    }
}
namespace N.S
{
    static class A
    {
        public static void E(this string s, int i) { }
    }
}
namespace N.S
{
    static class B
    {
        public static void E(this string s, int i) { }
        static void M(string s)
        {
            s.E(3);
        }
    }
    static class C
    {
        static void M(string s)
        {
            s.E(4);
        }
    }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_AmbigCall, "E").WithArgumentsAnyOrder("A.E(string, int)", "B.E(string, int)").WithLocation(10, 11),
                Diagnostic(ErrorCode.ERR_AmbigCall, "E").WithArgumentsAnyOrder("B.E(string, int)", "A.E(string, int)").WithLocation(17, 11),
                Diagnostic(ErrorCode.ERR_AmbigCall, "E").WithArguments("N.S.A.E(string, int)", "N.S.B.E(string, int)").WithLocation(34, 15),
                Diagnostic(ErrorCode.ERR_AmbigCall, "E").WithArguments("N.S.A.E(string, int)", "N.S.B.E(string, int)").WithLocation(41, 15));
        }

        /// <summary>
        /// Extension method delegates in different scopes make
        /// consumer (an overloaded method invocation) ambiguous.
        /// </summary>
        [Fact]
        public void AmbiguousConsumerWithExtensionMethodDelegateArg()
        {
            var source =
@"namespace N
{
    class C
    {
        static void M1(object o)
        {
            M2(o.F);
        }
        static void M2(System.Action a) { }
        static void M2(System.Action<int> a) { }
    }
    static class E
    {
        public static void F(this object o) { }
    }
}
static class E
{
    public static void F(this object o, int i) { }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (7,13): error CS0121: The call is ambiguous between the following methods or properties: 'N.C.M2(System.Action)' and 'N.C.M2(System.Action<int>)'
                Diagnostic(ErrorCode.ERR_AmbigCall, "M2").WithArgumentsAnyOrder("N.C.M2(System.Action)", "N.C.M2(System.Action<int>)").WithLocation(7, 13));
        }

        /// <summary>
        /// Prefer methods on classes on inner namespaces.
        /// </summary>
        [ClrOnlyFact]
        public void InnerNamespacesBeforeOuter()
        {
            var source =
@"using System;
static class A
{
    public static void E(this string s)
    {
        Console.WriteLine(""A.E: {0}"", s);
    }
    public static void E(this string s, int i)
    {
        Console.WriteLine(""A.E: {0}, {1}"", s, i);
    }
    public static void E(this string s, bool b)
    {
        Console.WriteLine(""C.E: {0}, {1}"", s, b);
    }
}
namespace N1
{
    static class B
    {
        public static void E(this string s)
        {
            Console.WriteLine(""B.E: {0}"", s);
        }
        public static void E(this string s, bool b)
        {
            Console.WriteLine(""B.E: {0}, {1}"", s, b);
        }
    }
}
namespace N1.N2
{
    static class C
    {
        public static void E(this string s)
        {
            Console.WriteLine(""C.E: {0}"", s);
        }
    }
    namespace N3
    {
        static class D
        {
            public static void E(this string s)
            {
                Console.WriteLine(""D.E: {0}"", s);
            }
        }
    }
    static class E
    {
        static void Main()
        {
            ""str"".E();
            ""int"".E(1);
            ""bool"".E(true);
        }
    }
}";
            CompileAndVerify(source, expectedOutput:
@"C.E: str
A.E: int, 1
B.E: bool, True");
        }

        [Fact]
        public void ExtensionMethodsWithAccessorNames()
        {
            var source =
@"class C
{
    public object P { get; set; }
    void M()
    {
        this.set_P(this.get_P());
        set_P(get_P());
    }
}
class D
{
    static void M(C c)
    {
        c.set_P(c.get_P());
    }
}
static class S
{
    internal static object get_P(this C c) { return null; }
    static void set_P(this C c, object o) { }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,14): error CS0571: 'C.P.set': cannot explicitly call operator or accessor
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "set_P").WithArguments("C.P.set").WithLocation(6, 14),
                // (7,9): error CS0571: 'C.P.set': cannot explicitly call operator or accessor
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "set_P").WithArguments("C.P.set").WithLocation(7, 9),
                // (7,15): error CS0571: 'C.P.get': cannot explicitly call operator or accessor
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "get_P").WithArguments("C.P.get").WithLocation(7, 15),
                // (14,11): error CS0571: 'C.P.set': cannot explicitly call operator or accessor
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "set_P").WithArguments("C.P.set").WithLocation(14, 11));
        }

        [Fact]
        public void DelegateExtensionMethodsWithAccessorNames()
        {
            var source =
@"using System;
class C
{
    object P { get; set; }
    object Q { get; set; }
    void M()
    {
        F(this.get_P);
        F(this.get_Q);
    }
    void F(Func<object> f) { }
}
static class S
{
    internal static object get_P(this C c) { return null; }
    static object get_Q(this C c) { return null; }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (9,16): error CS0571: 'C.Q.get': cannot explicitly call operator or accessor
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "get_Q").WithArguments("C.Q.get").WithLocation(9, 16));
        }

        [Fact]
        public void Delegates()
        {
            var source =
@"static class S
{
    public static void E(this object o) { }
    public static void F(this System.Action a) { }
    public static void G(this System.Action<object> a) { }
}
class C
{
    static void M()
    {
        S.F(4.E);
        S.F(new object().E);
        S.F(""str"".E);
        ""str"".E.F();
        (""str"".E).F();
        System.Action a = ""str"".E;
        a.F();
        S.G(S.E);
        S.E.G();
        System.Action<object> b = S.E;
        b.G();
    }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (14,15): error CS0119: 'S.E(object)' is a 'method', which is not valid in the given context
                Diagnostic(ErrorCode.ERR_BadSKunknown, "E").WithArguments("S.E(object)", "method").WithLocation(14, 15),
                // (15,16): error CS0119: 'S.E(object)' is a 'method', which is not valid in the given context
                Diagnostic(ErrorCode.ERR_BadSKunknown, "E").WithArguments("S.E(object)", "method").WithLocation(15, 16),
                // (19,11): error CS0119: 'S.E(object)' is a 'method', which is not valid in the given context
                Diagnostic(ErrorCode.ERR_BadSKunknown, "E").WithArguments("S.E(object)", "method").WithLocation(19, 11));
        }

        [ClrOnlyFact]
        public void GenericDelegate()
        {
            var source =
@"delegate void D<T>(T t);
class C
{
    static void Main()
    {
        F<int>(new C().M)(2);
    }
    static D<T> F<T>(D<T> d)
    {
        return d;
    }
    public int P { get { return 3; } }
}
static class S
{
    public static void M(this C c, int i)
    {
        System.Console.Write(c.P * i);
    }
}";
            CompileAndVerify(source, expectedOutput: "6");
        }

        [Fact]
        public void InvalidTypeArguments()
        {
            var source =
@"class C
{
    void M()
    {
        this.E<int>(1);
        this.E<S>();
        this.E<A>(null);
        this.E<int, int>(1);
    }
    void E<T>()
    {
    }
}
static class S
{
    public static void E<T>(this C c, T t)
    {
    }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,14): error CS0718: 'S': static types cannot be used as type arguments
                //         this.E<S>();
                Diagnostic(ErrorCode.ERR_GenericArgIsStaticClass, "E<S>").WithArguments("S").WithLocation(6, 14),
                // (7,16): error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)
                //         this.E<A>(null);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("A").WithLocation(7, 16),
                // (8,14): error CS0305: Using the generic method 'C.E<T>()' requires 1 type arguments
                //         this.E<int, int>(1);
                Diagnostic(ErrorCode.ERR_BadArity, "E<int, int>").WithArguments("C.E<T>()", "method", "1").WithLocation(8, 14)
                );
        }

        [Fact]
        public void ThisArgumentConversions()
        {
            var source =
@"class A { }
class B { }
struct S { }
class C
{
    static void M()
    {
        A a = new A();
        a.A();
        a.B();
        a.O();
        a.T();
        B b = new B();
        b.A();
        b.B();
        b.O();
        S s = new S();
        s.A();
        s.S();
        s.O();
        s.T();
        (1.0).D();
        (2.0).O();
        1.A();
        2.O();
        3.T();
    }
}
static class Extensions
{
    internal static void A(this A a) { }
    internal static void B(this B b) { }
    internal static void S(this S s) { }
    internal static void T<U>(this U u) { }
    internal static void D(this double d) { }
    internal static void O(this object o) { }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (10,9): error CS1929: 'A' does not contain a definition for 'B' and the best extension method overload 'Extensions.B(B)' requires a receiver of type 'B'
                //         a.B();
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "a").WithArguments("A", "B", "Extensions.B(B)", "B"),
                // (14,9): error CS1929: 'B' does not contain a definition for 'A' and the best extension method overload 'Extensions.A(A)' requires a receiver of type 'A'
                //         b.A();
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "b").WithArguments("B", "A", "Extensions.A(A)", "A"),
                // (18,9): error CS1929: 'S' does not contain a definition for 'A' and the best extension method overload 'Extensions.A(A)' requires a receiver of type 'A'
                //         s.A();
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "s").WithArguments("S", "A", "Extensions.A(A)", "A"),
                // (24,9): error CS1929: 'int' does not contain a definition for 'A' and the best extension method overload 'Extensions.A(A)' requires a receiver of type 'A'
                //         1.A();
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "1").WithArguments("int", "A", "Extensions.A(A)", "A")
                );
        }

        [Fact]
        public void ThisArgumentImplicitConversions()
        {
            var source =
@"class C
{
    static void M()
    {
        1.E1();
        2.E2();
        3.E3();
        4.E4();
    }
}
static class S
{
    internal static void E1<T>(this T t) { }
    internal static void E2(this double d) { }
    internal static void E3(this long l, params object[] args) { }
    internal static void E4(this object o) { }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,9): error CS1929: 'int' does not contain a definition for 'E2' and the best extension method overload 'S.E2(double)' requires a receiver of type 'double'
                //         2.E2();
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "2").WithArguments("int", "E2", "S.E2(double)", "double").WithLocation(6, 9),
                // (7,9): error CS1929: 'int' does not contain a definition for 'E3' and the best extension method overload 'S.E3(long, params object[])' requires a receiver of type 'long'
                //         3.E3();
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "3").WithArguments("int", "E3", "S.E3(long, params object[])", "long").WithLocation(7, 9));
        }

        [ClrOnlyFact]
        public void ParamsArray()
        {
            var source =
@"delegate void D(params int[] args);
class C
{
    void M()
    {
        this.E1(0);
        this.E1(1, null);
        1.E2();
        1.E2(2, 3);
        D d = this.E2;
    }
}
static class S
{
    internal static void E1(this C c, int i, params object[] args) { }
    internal static void E2(this object o, params int[] args) { }
}";
            CompileAndVerify(source);
        }

        [ClrOnlyFact]
        public void Using()
        {
            var source =
@"using System;
using N1.N2;
namespace N1
{
    internal static class S
    {
        public static void E(this object o) { Console.WriteLine(""N1.S.E""); }
    }
    namespace N2
    {
        internal static class S
        {
            public static void E(this object o) { Console.WriteLine(""N1.N2.S.E""); }
        }
    }
}
namespace N3
{
    internal static class S
    {
        public static void E(this object o) { Console.WriteLine(""N3.S.E""); }
    }
}
namespace N4
{
    using N3;
    class A
    {
        public static void M(object o) { o.E(); }
    }
}
namespace N4
{
    using N1;
    class B
    {
        public static void M(object o) { o.E(); }
    }
}
class C
{
    public static void M(object o) { o.E(); }
}
class D
{
    static void Main()
    {
        object o = null;
        N4.A.M(o);
        N4.B.M(o);
        C.M(o);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput:
@"N3.S.E
N1.S.E
N1.N2.S.E");
        }

        [Fact]
        public void AmbiguousUsing()
        {
            var source =
@"using N1;
using N2;
namespace N1
{
    internal static class S
    {
        public static void E(this object o) { }
    }
}
namespace N2
{
    internal static class S
    {
        public static void E(this object o) { }
    }
}
namespace N3
{
    internal static class S
    {
        public static void F(this object o) { }
        public static void G(this object o) { }
    }
}
namespace N4
{
    using N3;
    internal static class S
    {
        public static void F(this object o) { }
    }
    class C
    {
        static void M()
        {
            object o = null;
            o.E(); // ambiguous N1.S.E, N2.S.E
            o.F(); // choose N4.S.F over N3.S.F
            o.G(); // N3.S.G
        }
    }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (37,13): error CS0121: The call is ambiguous between the following methods or properties: 'N1.S.E(object)' and 'N2.S.E(object)'
                Diagnostic(ErrorCode.ERR_AmbigCall, "E").WithArguments("N1.S.E(object)", "N2.S.E(object)").WithLocation(37, 15));
        }

        [Fact]
        public void VerifyDiagnosticForMissingSystemCoreReference()
        {
            var source =
@"
internal static class C
{
    internal static void M1(this object o) { }
    private static void Main(string[] args) { }
}
";
            var compilation = CreateEmptyCompilation(source, new[] { Net40.References.mscorlib });
            compilation.VerifyDiagnostics(
                // (4,29): error CS1110: Cannot define a new extension because the compiler required type 'System.Runtime.CompilerServices.ExtensionAttribute' cannot be found. Are you missing a reference to System.Core.dll?
                Diagnostic(ErrorCode.ERR_ExtensionAttrNotFound, "this").WithArguments("System.Runtime.CompilerServices.ExtensionAttribute").WithLocation(4, 29));
        }

        [ClrOnlyFact]
        public void SystemLinqEnumerable()
        {
            var source =
@"using System;
using System.Linq;
class C
{
    static void Main()
    {
        string result = F(""banana"", ""orange"", ""lime"", ""apple"", ""kiwi"");
        Console.Write(result);
    }
    static string F(params string[] args)
    {
        return args.Skip(1).Where(Filter).Aggregate(Combine);
    }
    static string G(params string[] args)
    {
        return Enumerable.Aggregate(Enumerable.Where(Enumerable.Skip(args, 1), Filter), Combine);
    }
    static bool Filter(string s)
    {
        return s.Length > 4;
    }
    static string Combine(string s1, string s2)
    {
        return s1 + "", "" + s2;
    }
}";
            var code =
@"{
  // Code size       42 (0x2a)
  .maxstack  3
  IL_0000:  ldarg.0   
  IL_0001:  ldc.i4.1  
  IL_0002:  call       ""System.Collections.Generic.IEnumerable<string> System.Linq.Enumerable.Skip<string>(System.Collections.Generic.IEnumerable<string>, int)""
  IL_0007:  ldnull    
  IL_0008:  ldftn      ""bool C.Filter(string)""
  IL_000e:  newobj     ""System.Func<string, bool>..ctor(object, System.IntPtr)""
  IL_0013:  call       ""System.Collections.Generic.IEnumerable<string> System.Linq.Enumerable.Where<string>(System.Collections.Generic.IEnumerable<string>, System.Func<string, bool>)""
  IL_0018:  ldnull    
  IL_0019:  ldftn      ""string C.Combine(string, string)""
  IL_001f:  newobj     ""System.Func<string, string, string>..ctor(object, System.IntPtr)""
  IL_0024:  call       ""string System.Linq.Enumerable.Aggregate<string>(System.Collections.Generic.IEnumerable<string>, System.Func<string, string, string>)""
  IL_0029:  ret       
}";
            var compilation = CompileAndVerify(source, parseOptions: TestOptions.Regular10, expectedOutput: "orange, apple");
            compilation.VerifyIL("C.F", code);
            compilation.VerifyIL("C.G", code);
        }

        /// <summary>
        /// A value type should be boxed when used as a reference type receiver to an
        /// extension method. Note: Dev10 reports an error in such cases ("No overload for
        /// 'C.F(object)' matches delegate 'System.Action'") even though these cases are valid.
        /// </summary>
        [ClrOnlyFact]
        public void BoxingConversionOfDelegateReceiver01()
        {
            var source =
@"using System;
struct S { }
static class C
{
    static void Main()
    {
        M(1.F);
        M((new S()).F);
        M(1.G);
        M((new S()).G);
    }
    static void F(this object o)
    {
        Console.WriteLine(""F: {0}"", o.GetType());
    }
    static void G(this ValueType v)
    {
        Console.WriteLine(""G: {0}"", v.GetType());
    }
    static void M(Action a)
    {
        a();
    }
}";
            // ILVerify: Unrecognized arguments for delegate .ctor.
            var compilation = CompileAndVerify(source, expectedOutput:
@"F: System.Int32
F: S
G: System.Int32
G: S", verify: Verification.FailsILVerify);
            compilation.VerifyIL("C.Main",
@"{
  // Code size      105 (0x69)
  .maxstack  2
  .locals init (S V_0)
  IL_0000:  ldc.i4.1
  IL_0001:  box        ""int""
  IL_0006:  ldftn      ""void C.F(object)""
  IL_000c:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0011:  call       ""void C.M(System.Action)""
  IL_0016:  ldloca.s   V_0
  IL_0018:  initobj    ""S""
  IL_001e:  ldloc.0
  IL_001f:  box        ""S""
  IL_0024:  ldftn      ""void C.F(object)""
  IL_002a:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_002f:  call       ""void C.M(System.Action)""
  IL_0034:  ldc.i4.1
  IL_0035:  box        ""int""
  IL_003a:  ldftn      ""void C.G(System.ValueType)""
  IL_0040:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0045:  call       ""void C.M(System.Action)""
  IL_004a:  ldloca.s   V_0
  IL_004c:  initobj    ""S""
  IL_0052:  ldloc.0
  IL_0053:  box        ""S""
  IL_0058:  ldftn      ""void C.G(System.ValueType)""
  IL_005e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0063:  call       ""void C.M(System.Action)""
  IL_0068:  ret
}");
        }

        /// <summary>
        /// Similar to the test above, but using instances of type
        /// parameters for the delegate receiver.
        /// </summary>
        [ClrOnlyFact]
        public void BoxingConversionOfDelegateReceiver02()
        {
            var source =
@"using System;
interface I { }
class A { }
class B : A, I { }
class C
{
    static void Main()
    {
        M(new object(), new object(), 1, new B(), new B());
    }
    static void M<T1, T2, T3, T4, T5>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
        where T2 : class
        where T3 : struct
        where T4 : I
        where T5 : A
    {
        F(t1.M);
        F(t2.M);
        F(t3.M);
        F(t4.M);
        F(t5.M);
    }
    static void F(Action a)
    {
        a();
    }
}
static class E
{
    internal static void M(this object o)
    {
        Console.WriteLine(""{0}"", o.GetType());
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput:
@"System.Object
System.Object
System.Int32
B
B");
            compilation.VerifyIL("C.M<T1, T2, T3, T4, T5>",
@"{
  // Code size      112 (0x70)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  box        ""T1""
  IL_0006:  ldftn      ""void E.M(object)""
  IL_000c:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0011:  call       ""void C.F(System.Action)""
  IL_0016:  ldarg.1
  IL_0017:  box        ""T2""
  IL_001c:  ldftn      ""void E.M(object)""
  IL_0022:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0027:  call       ""void C.F(System.Action)""
  IL_002c:  ldarg.2
  IL_002d:  box        ""T3""
  IL_0032:  ldftn      ""void E.M(object)""
  IL_0038:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_003d:  call       ""void C.F(System.Action)""
  IL_0042:  ldarg.3
  IL_0043:  box        ""T4""
  IL_0048:  ldftn      ""void E.M(object)""
  IL_004e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0053:  call       ""void C.F(System.Action)""
  IL_0058:  ldarg.s    V_4
  IL_005a:  box        ""T5""
  IL_005f:  ldftn      ""void E.M(object)""
  IL_0065:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_006a:  call       ""void C.F(System.Action)""
  IL_006f:  ret
}");
        }

        [Fact]
        public void UsingInScript()
        {
            string test =
@"using System.Linq;
(new string[0]).Take(1)";

            var tree = SyntaxFactory.ParseSyntaxTree(test, options: TestOptions.Script.WithLanguageVersion(LanguageVersion.CSharp6));

            var compilation = CSharpCompilation.Create(
                assemblyName: GetUniqueName(),
                options: TestOptions.DebugExe.WithScriptClassName("Script"),
                syntaxTrees: new[] { tree },
                references: new[] { MscorlibRef, LinqAssemblyRef });

            var expr = ((ExpressionStatementSyntax)((GlobalStatementSyntax)tree.GetCompilationUnitRoot().Members[0]).Statement).Expression;
            var model = compilation.GetSemanticModel(tree);
            var info = model.GetSymbolInfo(expr);
            Assert.NotNull(info.Symbol);
            var symbol = info.Symbol;
            Utils.CheckSymbol(symbol, "IEnumerable<string> IEnumerable<string>.Take<string>(int count)");
        }

        [ClrOnlyFact]
        public void AssemblyMightContainExtensionMethods()
        {
            var source =
@"static class C
{
    internal static int F;
    internal static System.Linq.Expressions.Expression G;
    internal static void M(this object o) { }
}";
            Func<bool, Action<ModuleSymbol>> validator = isFromSource => module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

                // mscorlib.dll
                var mscorlib = type.GetMember<FieldSymbol>("F").Type.ContainingAssembly;
                Assert.Equal(RuntimeCorLibName.Name, mscorlib.Name);
                // We assume every PE assembly may contain extension methods.
                Assert.True(mscorlib.MightContainExtensions);

                // TODO: Original references are not included in symbol validator.
                if (isFromSource)
                {
                    // System.Core.dll
                    var systemCore = type.GetMember<FieldSymbol>("G").Type.ContainingAssembly;
                    Assert.True(systemCore.MightContainExtensions);
                }

                // Local assembly.
                var assembly = type.ContainingAssembly;
                Assert.True(assembly.MightContainExtensions);
            };

            CompileAndVerify(
                source: source,
                sourceSymbolValidator: validator(true),
                symbolValidator: validator(false),
                options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal));
        }

        /// <summary>
        /// AssemblySymbol.MightContainExtensionMethods should be reset after
        /// emit, after all types within the assembly have been inspected, if there
        /// are no types with extension methods.
        /// </summary>
        [ClrOnlyFact]
        public void AssemblyMightContainExtensionMethodsReset()
        {
            var source =
@"static class C
{
    internal static void M(object o) { }
}";
            AssemblySymbol sourceAssembly = null;
            Func<bool, Action<ModuleSymbol>> validator = isFromSource => module =>
            {
                var assembly = module.ContainingAssembly;
                var mightContainExtensionMethods = assembly.MightContainExtensions;
                // Every PE assembly is assumed to be capable of having an extension method.
                // The source assembly doesn't know (so reports "true") until all methods have been inspected.
                Assert.True(mightContainExtensionMethods);
                if (isFromSource)
                {
                    Assert.Null(sourceAssembly);
                    sourceAssembly = assembly;
                }
            };
            CompileAndVerify(source, symbolValidator: validator(false), sourceSymbolValidator: validator(true));
            Assert.NotNull(sourceAssembly);
            Assert.False(sourceAssembly.MightContainExtensions);
        }

        [ClrOnlyFact]
        public void ReducedExtensionMethodSymbols()
        {
            var source =
@"using System.Collections.Generic;
static class S
{
    internal static void M1(this object o) { }
    internal static void M2<T>(this IEnumerable<T> t) { }
    internal static void M3<T, U>(this U u, IEnumerable<T> t) { }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal));
            Action<ModuleSymbol> validator = module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("S");
                var intType = compilation.GetSpecialType(SpecialType.System_Int32);
                var stringType = compilation.GetSpecialType(SpecialType.System_String);
                var arrayType = ArrayTypeSymbol.CreateCSharpArray(compilation.Assembly, TypeWithAnnotations.Create(stringType), 1);

                // Non-generic method.
                var method = type.GetMember<MethodSymbol>("M1");
                CheckExtensionMethod(method,
                    ImmutableArray.Create<TypeWithAnnotations>(),
                    "void object.M1()",
                    "void S.M1(object o)",
                    "void object.M1()",
                    "void S.M1(object o)");

                // Generic method, one type argument.
                method = type.GetMember<MethodSymbol>("M2");
                CheckExtensionMethod(method,
                    ImmutableArray.Create(TypeWithAnnotations.Create(intType)),
                    "void IEnumerable<int>.M2<int>()",
                    "void S.M2<T>(IEnumerable<T> t)",
                    "void IEnumerable<T>.M2<T>()",
                    "void S.M2<T>(IEnumerable<T> t)");

                // Generic method, multiple type arguments.
                method = type.GetMember<MethodSymbol>("M3");
                CheckExtensionMethod(method,
                    ImmutableArray.Create(TypeWithAnnotations.Create(intType), TypeWithAnnotations.Create(arrayType)),
                    "void string[].M3<int, string[]>(IEnumerable<int> t)",
                    "void S.M3<T, U>(U u, IEnumerable<T> t)",
                    "void U.M3<T, U>(IEnumerable<T> t)",
                    "void S.M3<T, U>(U u, IEnumerable<T> t)");
            };

            CompileAndVerify(compilation, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        private void CheckExtensionMethod(
            MethodSymbol method,
            ImmutableArray<TypeWithAnnotations> typeArgs,
            string reducedMethodDescription,
            string reducedFromDescription,
            string constructedFromDescription,
            string reducedAndConstructedFromDescription)
        {
            // Create instance form from constructed method.
            var extensionMethod = ReducedExtensionMethodSymbol.Create(method.ConstructIfGeneric(typeArgs));
            Utils.CheckReducedExtensionMethod(extensionMethod, reducedMethodDescription, reducedFromDescription, constructedFromDescription, reducedAndConstructedFromDescription);

            // Construct method from unconstructed instance form.
            extensionMethod = ReducedExtensionMethodSymbol.Create(method).ConstructIfGeneric(typeArgs);
            Utils.CheckReducedExtensionMethod(extensionMethod, reducedMethodDescription, reducedFromDescription, constructedFromDescription, reducedAndConstructedFromDescription);
        }

        /// <summary>
        /// Roslyn bug 7782: NullRef in PeWriter.DebuggerShouldHideMethod
        /// </summary>
        [Fact]
        public void ExtensionMethod_ValidateExtensionAttribute()
        {
            var comp = CreateCompilation(@"
using System;
internal static class C
{
    internal static void M1(this object o) { }
    private static void Main(string[] args) { }
}
", options: TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All));

            CompileAndVerify(comp, symbolValidator: module =>
            {
                var method = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<PEMethodSymbol>("M1");
                Assert.True(method.IsExtensionMethod);
                Assert.Equal(SpecialType.System_Object, method.Parameters.Single().Type.SpecialType);

                var attr = ((PEModuleSymbol)module).GetCustomAttributesForToken(method.Handle).Single();
                Assert.Equal("System.Runtime.CompilerServices.ExtensionAttribute", attr.AttributeClass.ToTestDisplayString());
            });
        }

        [WorkItem(541327, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541327")]
        [Fact]
        public void RegressBug7992()
        {
            var text =
@"using System.Runtime.InteropServices;
namespace ConsoleApplication1
{
    [StructLayout(Pack = A.B)]
    struct Program { }
}
";
            CreateCompilation(text).GetDiagnostics();
        }

        /// <summary>
        /// Box value type receiver if passed as reference type.
        /// </summary>
        [ClrOnlyFact]
        public void BoxValueTypeReceiverIfNecessary()
        {
            var source =
@"struct S { }
static class C
{
    static void Main()
    {
        ""str"".F();
        ""str"".G();
        (2.0).F();
        (2.0).G();
        (new S()).F();
        (new S()).G();
    }
    static void F(this object o)
    {
        System.Console.WriteLine(""{0}"", o.ToString());
    }
    static void G<T>(this T t)
    {
        System.Console.WriteLine(""{0}"", t.ToString());
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput:
@"str
str
2
2
S
S");
            compilation.VerifyIL("C.Main",
@"{
  // Code size       87 (0x57)
  .maxstack  1
  .locals init (S V_0)
  IL_0000:  ldstr      ""str""
  IL_0005:  call       ""void C.F(object)""
  IL_000a:  ldstr      ""str""
  IL_000f:  call       ""void C.G<string>(string)""
  IL_0014:  ldc.r8     2
  IL_001d:  box        ""double""
  IL_0022:  call       ""void C.F(object)""
  IL_0027:  ldc.r8     2
  IL_0030:  call       ""void C.G<double>(double)""
  IL_0035:  ldloca.s   V_0
  IL_0037:  initobj    ""S""
  IL_003d:  ldloc.0
  IL_003e:  box        ""S""
  IL_0043:  call       ""void C.F(object)""
  IL_0048:  ldloca.s   V_0
  IL_004a:  initobj    ""S""
  IL_0050:  ldloc.0
  IL_0051:  call       ""void C.G<S>(S)""
  IL_0056:  ret
}");
        }

        [WorkItem(541652, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541652")]
        [ClrOnlyFact]
        public void ReduceExtensionMethodWithNullReceiverType()
        {
            var source =
@"static class Extensions
{
    public static int NonGeneric(this object o) { return o.GetHashCode(); }
    public static int Generic<T>(this T o) { return o.GetHashCode(); }
}
";
            CompileAndVerify(source, validator: module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("Extensions");
                var nonGenericExtension = type.GetMember<MethodSymbol>("NonGeneric");
                var genericExtension = type.GetMember<MethodSymbol>("Generic");

                Assert.True(nonGenericExtension.IsExtensionMethod);
                Assert.Throws<ArgumentNullException>(() => nonGenericExtension.ReduceExtensionMethod(receiverType: null, compilation: null!));

                Assert.True(genericExtension.IsExtensionMethod);
                Assert.Throws<ArgumentNullException>(() => genericExtension.ReduceExtensionMethod(receiverType: null, compilation: null!));
            });
        }

        [WorkItem(528730, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528730")]
        [Fact]
        public void ThisParameterCalledOnNonSourceMethodSymbol()
        {
            var code =
@"using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        int[] numbers = new int[] { 4, 5 };
        int i1 = numbers.GetHashCode();
        int i2 = numbers.Cast<T1>();
    }
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(code);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var node = tree.GetCompilationUnitRoot().FindToken(code.IndexOf("GetHashCode", StringComparison.Ordinal)).Parent;
            var symbolInfo = model.GetSymbolInfo((SimpleNameSyntax)node);
            var methodSymbol = symbolInfo.Symbol.GetSymbol<MethodSymbol>();
            Assert.False(methodSymbol.IsFromCompilation(compilation));

            var parameter = methodSymbol.ThisParameter;
            Assert.Equal(-1, parameter.Ordinal);
            Assert.Equal(parameter.ContainingSymbol, methodSymbol);

            // Get the GenericNameSyntax node Cast<T1> for binding
            node = tree.GetCompilationUnitRoot().FindToken(code.IndexOf("Cast<T1>", StringComparison.Ordinal)).Parent;
            symbolInfo = model.GetSymbolInfo((GenericNameSyntax)node);
            methodSymbol = (MethodSymbol)symbolInfo.Symbol.GetSymbol<MethodSymbol>();
            Assert.False(methodSymbol.IsFromCompilation(compilation));

            // 9341 is resolved as Won't Fix since ThisParameter property is internal.
            Assert.Throws<InvalidOperationException>(() => methodSymbol.ThisParameter);
        }

        private CompilationVerifier CompileAndVerify(string source, string expectedOutput = null, Action<ModuleSymbol> validator = null,
            CSharpCompilationOptions options = null)
        {
            return CompileAndVerify(
                source: source,
                expectedOutput: expectedOutput,
                sourceSymbolValidator: validator,
                symbolValidator: validator,
                options: options);
        }

        [WorkItem(528853, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528853")]
        [Fact]
        public void NoOverloadTakesNArguments()
        {
            var source =
@"static class S
{
    static void M(this object x, object y)
    {
        x.M(x, y);
        x.M();
        M(x);
    }
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (5,9): error CS1501: No overload for method 'M' takes 2 arguments
                //         x.M(x, y);
                Diagnostic(ErrorCode.ERR_BadArgCount, "M").WithArguments("M", "2").WithLocation(5, 11),
                // (6,9): error CS7036: There is no argument given that corresponds to the required parameter 'y' of 'S.M(object, object)'
                //         x.M();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M").WithArguments("y", "S.M(object, object)").WithLocation(6, 11),
                // (7,9): error CS7036: There is no argument given that corresponds to the required parameter 'y' of 'S.M(object, object)'
                //         M(x);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M").WithArguments("y", "S.M(object, object)").WithLocation(7, 9));
        }

        [WorkItem(543711, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543711")]
        [Fact]
        public void ReduceReducedExtensionsMethod()
        {
            var source =
@"static class C
{
    static void M(this object x)
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            compilation.VerifyDiagnostics();

            var extensionMethod = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>("M");
            Assert.True(extensionMethod.IsExtensionMethod);

            var reduced = extensionMethod.ReduceExtensionMethod();
            Assert.True(reduced.IsExtensionMethod);

            Assert.Null(reduced.ReduceExtensionMethod());

            var int32Type = compilation.GetSpecialType(SpecialType.System_Int32);

            var reducedWithReceiver = extensionMethod.ReduceExtensionMethod(int32Type, null!);
            Assert.True(reduced.IsExtensionMethod);
            Assert.Equal(reduced, reducedWithReceiver);

            Assert.Null(reducedWithReceiver.ReduceExtensionMethod(int32Type, null!));
        }

        [WorkItem(37780, "https://github.com/dotnet/roslyn/issues/37780")]
        [Fact]
        public void ReducedExtensionMethodVsUnmanagedConstraint()
        {
            var source1 =
@"public static class C
{
    public static void M<T>(this T self) where T : unmanaged
    {
    }
}";
            var compilation1 = CreateCompilation(source1);
            compilation1.VerifyDiagnostics();

            var source2 =
@"public class D
{
    static void M(MyStruct<int> s)
    {
        s.M();
    }
}
public struct MyStruct<T>
{
    public T field;
}
";

            var compilation2 = CreateCompilation(source2, references: new[] { new CSharpCompilationReference(compilation1) }, parseOptions: TestOptions.Regular8);
            compilation2.VerifyDiagnostics();

            var extensionMethod = compilation2.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>("M");
            Assert.True(extensionMethod.IsExtensionMethod);

            var myStruct = (NamedTypeSymbol)compilation2.GlobalNamespace.GetMember<NamedTypeSymbol>("MyStruct");
            var int32Type = compilation2.GetSpecialType(SpecialType.System_Int32);
            var msi = myStruct.Construct(int32Type);

            object reducedWithReceiver = extensionMethod.ReduceExtensionMethod(msi, compilation2);
            Assert.NotNull(reducedWithReceiver);

            reducedWithReceiver = extensionMethod.ReduceExtensionMethod(msi, null!);
            Assert.NotNull(reducedWithReceiver);

            reducedWithReceiver = extensionMethod.GetPublicSymbol().ReduceExtensionMethod(msi.GetPublicSymbol());
            Assert.NotNull(reducedWithReceiver);

            compilation2 = CreateCompilation(source2, references: new[] { new CSharpCompilationReference(compilation1) }, parseOptions: TestOptions.Regular7);
            compilation2.VerifyDiagnostics(
                // (5,9): error CS8107: Feature 'unmanaged constructed types' is not available in C# 7.0. Please use language version 8.0 or greater.
                //         s.M();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "s.M").WithArguments("unmanaged constructed types", "8.0").WithLocation(5, 9)
                );

            extensionMethod = compilation2.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>("M");
            Assert.True(extensionMethod.IsExtensionMethod);

            myStruct = (NamedTypeSymbol)compilation2.GlobalNamespace.GetMember<NamedTypeSymbol>("MyStruct");
            int32Type = compilation2.GetSpecialType(SpecialType.System_Int32);
            msi = myStruct.Construct(int32Type);

            reducedWithReceiver = extensionMethod.ReduceExtensionMethod(msi, compilation2);
            Assert.Null(reducedWithReceiver);

            reducedWithReceiver = extensionMethod.ReduceExtensionMethod(msi, null!);
            Assert.NotNull(reducedWithReceiver);

            reducedWithReceiver = extensionMethod.GetPublicSymbol().ReduceExtensionMethod(msi.GetPublicSymbol());
            Assert.NotNull(reducedWithReceiver);
        }

        /// <summary>
        /// Dev11 reports error for inaccessible extension method in addition to an
        /// error for the instance method that was used for binding. The inaccessible
        /// error may be helpful for the user or for "quick fix" in particular.
        /// </summary>
        [WorkItem(529866, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529866")]
        [Fact]
        public void InstanceMethodAndInaccessibleExtensionMethod_Diagnostics()
        {
            var source =
@"class C
{
    static void Main()
    {
        C c = new C();
        c.Test(1d);
    }
    void Test(float f)
    {
    }
}
static class Extensions
{
    static void Test(this C c, double d)
    {
    }
    static void Test<T>(this T t)
    {
    }
}";
            // Dev11 also reports:
            // (17,17): error CS0122: 'Extensions.Test<T>(T)' is inaccessible due to its protection level
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (6,16): error CS1503: Argument 1: cannot convert from 'double' to 'float'
                Diagnostic(ErrorCode.ERR_BadArgType, "1d").WithArguments("1", "double", "float").WithLocation(6, 16));
        }

        [WorkItem(545322, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545322")] // Bug relates to defunct LookupOptions.IgnoreAccessibility.
        [Fact]
        public void InstanceMethodAndInaccessibleExtensionMethod_Symbols()
        {
            var source =
@"class C
{
    static void Main()
    {
        C c = new C();
        c.Test(1d);
    }
    void Test(float f)
    {
    }
}
static class Extensions
{
    static void Test(this C c, double d)
    {
    }
    static void Test<T>(this T t)
    {
    }
}";
            var compilation = (Compilation)CreateCompilationWithMscorlib40AndSystemCore(source);
            var globalNamespace = compilation.GlobalNamespace;
            var type = globalNamespace.GetMember<INamedTypeSymbol>("C");
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var memberAccess = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single();

            var lookupResult = model.LookupSymbols(
                memberAccess.SpanStart,
                container: null,
                name: "Test",
                includeReducedExtensionMethods: true);
            Utils.CheckISymbols(lookupResult,
                "void C.Test(float f)");

            lookupResult = model.LookupSymbols(
                memberAccess.SpanStart,
                container: type,
                name: "Test",
                includeReducedExtensionMethods: true);
            Utils.CheckISymbols(lookupResult,
                "void C.Test(float f)"); // Extension methods not found.

            var memberGroup = model.GetMemberGroup(memberAccess);
            Utils.CheckISymbols(memberGroup,
                "void C.Test(float f)");

            compilation.VerifyDiagnostics(
                // (6,16): error CS1503: Argument 1: cannot convert from 'double' to 'float'
                //         c.Test(1d);
                Diagnostic(ErrorCode.ERR_BadArgType, "1d").WithArguments("1", "double", "float"));
        }

        [WorkItem(541890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541890")]
        [Fact]
        public void InstanceMethodAndInaccessibleExtensionMethod_CandidateSymbols()
        {
            var source =
@"class C
{
    static void Main()
    {
        C c = new C();
        c.Test(1d);
    }
    void Test(float f)
    {
    }
}
static class Extensions
{
    static void Test(this C c, double d)
    {
    }
    static void Test<T>(this T t)
    {
    }
}";
            var compilation = (Compilation)CreateCompilationWithMscorlib40AndSystemCore(source);
            var globalNamespace = compilation.GlobalNamespace;
            var type = globalNamespace.GetMember<INamedTypeSymbol>("C");
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var memberAccess = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single();

            var call = (ExpressionSyntax)memberAccess.Parent;
            Assert.Equal(SyntaxKind.InvocationExpression, call.Kind());

            var info = model.GetSymbolInfo(call);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, info.CandidateReason);
            // Definitely want the extension method here for quick fix.
            Utils.CheckISymbols(info.CandidateSymbols,
                "void C.Test(float f)");
        }

        [WorkItem(529596, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529596")]
        [Fact(Skip = "529596")]
        public void DelegateFromValueTypeExtensionMethod()
        {
            var source = @"
public delegate void VoidDelegate();

static class C
{
    public static void Goo(this int x)
    {
        VoidDelegate v;
        v = x.Goo; // CS1113
        v = new VoidDelegate(x.Goo); // Roslyn reports CS0123
        v += x.Goo; // Roslyn reports CS0019
    }
}
";
            // TODO: Dev10 reports CS1113 for all of these.  Roslyn reports various other diagnostics
            // because we detect the condition for CS1113 and then indicate that no conversion exists,
            // resulting in various cascaded errors.
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (9,13): error CS1113: Extension method 'C.Goo(int)' defined on value type 'int' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "x.Goo").WithArguments("C.Goo(int)", "int").WithLocation(9, 13),
                // (10,13): error CS1113: Extension method 'C.Goo(int)' defined on value type 'int' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "new VoidDelegate(x.Goo)").WithArguments("C.Goo(int)", "int").WithLocation(10, 13),
                // (11,14): error CS1113: Extension method 'C.Goo(int)' defined on value type 'int' cannot be used to create delegates
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "x.Goo").WithArguments("'C.Goo(int)", "int").WithLocation(11, 14));
        }

        [Fact]
        public void DelegateFromGenericExtensionMethod()
        {
            var source = @"
public delegate void VoidDelegate();

static class DevDivBugs142219
{
    public static void Goo<T>(this T x)
    {
        VoidDelegate f = x.Goo; // CS1113
    }
    public static void Bar<T>(this T x) where T : class
    {
        VoidDelegate f = x.Bar; // ok
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (8,26): error CS1113: Extension method 'DevDivBugs142219.Goo<T>(T)' defined on value type 'T' cannot be used to create delegates
                //         VoidDelegate f = x.Goo; // CS1113
                Diagnostic(ErrorCode.ERR_ValueTypeExtDelegate, "x.Goo").WithArguments("DevDivBugs142219.Goo<T>(T)", "T"));
        }

        [ClrOnlyFact]
        [WorkItem(545734, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545734")]
        public void ExtensionMethodWithRefParameterFromMetadata()
        {
            var lib = @"
public static class Extensions
{
        public static bool TryGetWithoutAttributeSuffix(
            this string name,
            out string result)
        {
            result = ""42"";
            return false;
        }
}";
            var consumer = @"
static class Program
{
    static void Main()
    {
        var symbolName = ""test"";
        string nameWithoutAttributeSuffix;
        symbolName.TryGetWithoutAttributeSuffix(out nameWithoutAttributeSuffix);
    }
}";
            var libCompilation = CreateCompilationWithMscorlib40AndSystemCore(lib, assemblyName: Guid.NewGuid().ToString());
            var libReference = new CSharpCompilationReference(libCompilation);

            CompileAndVerify(consumer, references: new[] { libReference });
        }

        [Fact, WorkItem(545800, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545800")]
        public void SameExtensionMethodSymbol()
        {
            var src1 = @"
using System.Collections.Generic;

public class MyClass
{
    public void InstanceMethod<T>(T t)  {    }
}

public static class Extensions
{
    public static void ExtensionMethod<T>(this MyClass p, T t)  {    }
}

class Test
{
    static void Main()
    {
        var obj = new MyClass();
        obj.InstanceMethod('q');
        obj.ExtensionMethod('c');
    }
}
";

            var comp = CreateCompilation(src1);
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var nodes = tree.GetCompilationUnitRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();
            Assert.Equal(2, nodes.Count);

            var firstInvocation = nodes[0];
            var firstInvocationExpression = firstInvocation.Expression;
            var firstInvocationSymbol = model.GetSymbolInfo(firstInvocation).Symbol;
            var firstInvocationExpressionSymbol = model.GetSymbolInfo(firstInvocationExpression).Symbol;

            var secondInvocation = nodes[1];
            var secondInvocationExpression = secondInvocation.Expression;
            var secondInvocationSymbol = model.GetSymbolInfo(secondInvocation).Symbol;
            var secondInvocationExpressionSymbol = model.GetSymbolInfo(secondInvocationExpression).Symbol;

            Assert.Equal("obj.InstanceMethod", firstInvocationExpression.ToString());
            Assert.Equal(SyntaxKind.SimpleMemberAccessExpression, firstInvocationExpression.Kind());
            Assert.Equal(SymbolKind.Method, firstInvocationSymbol.Kind);
            Assert.Equal("InstanceMethod", firstInvocationSymbol.Name);
            Assert.Equal(firstInvocationSymbol, firstInvocationExpressionSymbol);

            Assert.Equal("obj.ExtensionMethod", secondInvocationExpression.ToString());
            Assert.Equal(SyntaxKind.SimpleMemberAccessExpression, secondInvocationExpression.Kind());
            Assert.Equal(SymbolKind.Method, secondInvocationSymbol.Kind);
            Assert.Equal("ExtensionMethod", secondInvocationSymbol.Name);
            Assert.Equal(secondInvocationSymbol, secondInvocationExpressionSymbol);
        }

        /// <summary>
        /// Dev11 allows referencing extension methods defined on
        /// non-static classes, generic classes, structs, and delegates.
        /// </summary>
        [WorkItem(546093, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546093")]
        [Fact]
        public void NonStaticClasses()
        {
            var source1 =
@".assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core {}
.assembly '<<GeneratedFileName>>'
{
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
}
.class public A
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  // public method
  .method public static void MA(object o)
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    ret
  }
  // protected method
  .method family static void MP(object o)
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    ret
  }
}
.class public B<T>
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  // generic type method
  .method public static void MB(object o)
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    ret
  }
}
.class public sealed S extends [mscorlib]System.ValueType
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
  // struct method
  .method public static void MS(object o)
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    ret
  }
}
.class public abstract interface I
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
  // interface method
  .method public static void MI(object o)
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    ret
  }
}
.class public sealed D extends [mscorlib]System.MulticastDelegate
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
  .method public instance void Invoke() { ret }
  // delegate method
  .method public static void MD(object o)
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    ret
  }
}";
            var reference1 = CompileIL(source1, prependDefaultHeader: false);
            var source2 =
@"class C : A
{
    static void M(object o)
    {
        o.MA(); // A.MA()
        o.MP(); // A.MP() (protected)
        o.MB(); // B<T>.MB()
        o.MS(); // S.MS()
        o.MI(); // I.MI()
        o.MD(); // D.MD()
    }
}";
            var compilation = CreateCompilation(source2, new[] { reference1 });
            compilation.VerifyDiagnostics(
                // (9,11): error CS1061: 'object' does not contain a definition for 'MI' and no extension method 'MI' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "MI").WithArguments("object", "MI").WithLocation(9, 11));
        }

        [WorkItem(546093, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546093")]
        [ClrOnlyFact]
        public void VBExtensionMethod()
        {
            var source1 =
@"Imports System.Runtime.CompilerServices
Public Module M
    <Extension()>
    Public Sub F(o As Object)
    End Sub
End Module";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1, references: new[] { MscorlibRef, SystemCoreRef, MsvbRef });
            var source2 =
@"class C
{
    static void M(object o)
    {
        o.F();
    }
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics();
        }

        [WorkItem(602893, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602893")]
        [ClrOnlyFact]
        public void Bug602893()
        {
            var source1 =
@"namespace NA
{
    internal static class A
    {
        public static void F(this object o) { }
    }
}";
            var compilation1 = CreateCompilationWithMscorlib40AndSystemCore(source1, assemblyName: "A");
            compilation1.VerifyDiagnostics();
            var compilationVerifier = CompileAndVerify(compilation1);
            var reference1 = MetadataReference.CreateFromImage(compilationVerifier.EmittedAssemblyData);
            var source2 =
@"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""C"")]
namespace NB
{
    internal static class B
    {
        public static void F(this object o) { }
    }
}";
            var compilation2 = CreateCompilationWithMscorlib40AndSystemCore(source2, assemblyName: "B");
            compilation2.VerifyDiagnostics();
            compilationVerifier = CompileAndVerify(compilation2);
            var reference2 = MetadataReference.CreateFromImage(compilationVerifier.EmittedAssemblyData);
            var source3 =
@"using NB;
namespace NA.NC
{
    class C
    {
        static void Main()
        {
            new object().F();
        }
    }
}";
            var compilation3 = CreateCompilation(source3, assemblyName: "C", references: new[] { reference1, reference2 });
            compilation3.VerifyDiagnostics();
        }

        /// <summary>
        /// As test above but with all classes defined in the same compilation.
        /// </summary>
        [WorkItem(602893, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602893")]
        [Fact]
        public void Bug602893_2()
        {
            var source =
@"using NB;
namespace NA
{
    internal static class A
    {
        static void F(this object o) { }
    }
}
namespace NB
{
    internal static class B
    {
        public static void F(this object o) { }
    }
}
namespace NA
{
    class C
    {
        static void Main()
        {
            new object().F();
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            compilation.VerifyDiagnostics();
        }

        /// <summary>
        /// Ambiguous methods should hide methods in outer scopes.
        /// </summary>
        [Fact]
        public void AmbiguousMethodsHideOuterScope()
        {
            var source =
@"using NB;
namespace NA
{
    internal static class A
    {
        public static void F(this object o) { }
    }
    internal static class B
    {
        public static void F(this object o) { }
    }
    class C
    {
        static void Main()
        {
            new object().F();
        }
    }
}
namespace NB
{
    internal static class B
    {
        public static void F(this object o) { }
    }
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (16,13): error CS0121: The call is ambiguous between the following methods or properties: 'NA.A.F(object)' and 'NA.B.F(object)'
                Diagnostic(ErrorCode.ERR_AmbigCall, "F").WithArguments("NA.A.F(object)", "NA.B.F(object)").WithLocation(16, 26),
                // (1,1): info CS8019: Unnecessary using directive.
                // using NB;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using NB;"));
        }

        [Fact, WorkItem(822125, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/822125")]
        public void ConsumeFSharpExtensionMethods()
        {
            var source =
@"using FSharpTestLibrary;

namespace CSharpApp
{
    class Program
    {
        static void Main()
        {
            var question = 42.GetQuestion();
        }
    }
}";
            var compilation = CreateCompilation(source, references: new[] { FSharpTestLibraryRef });
            compilation.VerifyDiagnostics();
        }

        [ClrOnlyFact]
        public void InternalExtensionAttribute()
        {
            var source =
@"
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    class ExtensionAttribute : Attribute
    {
    }
}

internal static class Test
{
    public static void M(this int p)
    {
    }
}
";
            var compilation = CreateEmptyCompilation(source, new[] { MscorlibRef_v20 }, TestOptions.ReleaseDll);
            CompileAndVerify(compilation);
        }

        [ClrOnlyFact]
        [WorkItem(1010648, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1010648")]
        public void ExtensionMethodFromUsingStatic()
        {
            const string source = @"
using System;
using static N.S;

class Program
{
    static void Main()
    {
        1.Goo();
    }
}

namespace N
{
    static class S
    {
        public static void Goo(this int x)
        {
            Console.Write(x);
        }
    }
}";
            CompileAndVerify(source, expectedOutput: "1");
        }

        [Fact, WorkItem(1085744, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1085744")]
        public void ExtensionMethodsAreNotImportedAsSimpleNames()
        {
            const string source = @"
using System;
using static N.S;

class Program
{
    static void Main()
    {
        1.Goo();
        Goo(1);
    }
}

namespace N
{
    static class S
    {
        public static void Goo(this int x)
        {
            Console.Write(x);
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (10,9): error CS0103: The name 'Goo' does not exist in the current context
                //         Goo(1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Goo").WithArguments("Goo").WithLocation(10, 9));
        }

        [ClrOnlyFact]
        [WorkItem(1010648, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1010648")]
        public void ExtensionMethodImportedTwiceNoErrors()
        {
            const string source = @"
using System;
using N;
using static N.S;

class Program
{
    static void Main()
    {
        1.Goo();
    }
}

namespace N
{
    static class S
    {
        public static void Goo(this int x)
        {
            Console.WriteLine(x);
        }
    }
}";
            CompileAndVerify(source, expectedOutput: "1");
        }

        [Fact, WorkItem(1010648, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1010648")]
        public void ExtensionMethodIsNotDisambiguatedByUsingStaticAtTheSameLevel()
        {
            const string source = @"
using N;
using static N.S;

class Program
{
    static void Main()
    {
        1.Goo();
    }
}

namespace N
{
    static class S
    {
        public static void Goo(this int x)
        {
        }
    }

    static class R
    {
        public static void Goo(this int x)
        {
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (9,11): error CS0121: The call is ambiguous between the following methods or properties: 'S.Goo(int)' and 'R.Goo(int)'
                //         1.Goo();
                Diagnostic(ErrorCode.ERR_AmbigCall, "Goo").WithArguments("N.S.Goo(int)", "N.R.Goo(int)").WithLocation(9, 11));
        }

        [ClrOnlyFact]
        [WorkItem(1010648, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1010648")]
        public void ExtensionMethodIsDisambiguatedByUsingStaticAtDeeperLevel()
        {
            const string source = @"
using System;
using N;

namespace K
{
    using static S;

    class Program
    {
        static void Main()
        {
            1.Goo();
        }
    }
}

namespace N
{
    static class S
    {
        public static void Goo(this int x)
        {
            Console.WriteLine(""S"");
        }
    }

    static class R
    {
        public static void Goo(this int x)
        {
            Console.WriteLine(""R"");
        }
    }
}";
            CompileAndVerify(source, expectedOutput: "S");
        }

        [Fact, WorkItem(1010648, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1010648")]
        public void ExtensionMethodAmbiguousAcrossMultipleUsingStatic()
        {
            const string source = @"
using System;

namespace K
{
    using static N.S;
    using static N.R;

    class Program
    {
        static void Main()
        {
            1.Goo();
        }
    }
}

namespace N
{
    static class S
    {
        public static void Goo(this int x)
        {
            Console.WriteLine(""S"");
        }
    }

    static class R
    {
        public static void Goo(this int x)
        {
            Console.WriteLine(""R"");
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (13,15): error CS0121: The call is ambiguous between the following methods or properties: 'S.Goo(int)' and 'R.Goo(int)'
                //             1.Goo();
                Diagnostic(ErrorCode.ERR_AmbigCall, "Goo").WithArguments("N.S.Goo(int)", "N.R.Goo(int)").WithLocation(13, 15));
        }

        [Fact, WorkItem(1010648, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1010648")]
        public void ExtensionMethodsInTheContainingClassDoNotHaveHigherPrecedence()
        {
            const string source = @"
namespace N
{
    using static Program;

    static class Program
    {
        static void Main()
        {
            1.Goo();
        }

        public static void Goo(this int x)
        {
        }
    }

    static class R
    {
        public static void Goo(this int x)
        {
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (10,15): error CS0121: The call is ambiguous between the following methods or properties: 'Program.Goo(int)' and 'R.Goo(int)'
                //             1.Goo();
                Diagnostic(ErrorCode.ERR_AmbigCall, "Goo").WithArguments("N.Program.Goo(int)", "N.R.Goo(int)").WithLocation(10, 15),
                // (4,5): hidden CS8019: Unnecessary using directive.
                //     using Program;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static Program;").WithLocation(4, 5));
        }

        [Fact, WorkItem(1010648, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1010648")]
        public void UsingAliasDoesNotImportExtensionMethods()
        {
            const string source = @"
namespace K
{
    using X = N.S;
    class Program
    {
        static void Main()
        {
            1.Goo();
        }
    }
}

namespace N
{
    static class S
    {
        public static void Goo(this int x)
        {
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (9,15): error CS1061: 'int' does not contain a definition for 'Goo' and no extension method 'Goo' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                //             1.Goo();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Goo").WithArguments("int", "Goo").WithLocation(9, 15),
                // (4,5): hidden CS8019: Unnecessary using directive.
                //     using X = N.S;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using X = N.S;").WithLocation(4, 5));
        }

        [WorkItem(1094849, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1094849"), WorkItem(2288, "https://github.com/dotnet/roslyn/issues/2288")]
        [Fact]
        public void LookupSymbolsWithPartialInference()
        {
            var source =
@"
using System.Collections.Generic;

namespace ConsoleApplication22
{
    static class Program
    {
        static void Main(string[] args)
        {
        }

        internal static void GetEnumerableDisposable1<T, TEnumerator>(this IEnumerable<T> enumerable)
            where TEnumerator : struct , IEnumerator<T>
        {
        }

        internal static void GetEnumerableDisposable2<T, TEnumerator>(this IEnumerable<T> enumerable)
            where TEnumerator : struct
        {
        }

        private static void Overlaps<T, TEnumerator>(IEnumerable<T> other) where TEnumerator : struct, IEnumerator<T>
        {
            other.GetEnumerableDisposable1<T, TEnumerator>();
        }
    }
}";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();
            var syntaxTree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(syntaxTree);

            var member = (MemberAccessExpressionSyntax)syntaxTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single().Expression;
            Assert.Equal("other.GetEnumerableDisposable1<T, TEnumerator>", member.ToString());

            var type = model.GetTypeInfo(member.Expression).Type;
            Assert.Equal("System.Collections.Generic.IEnumerable<T>", type.ToTestDisplayString());

            var symbols = model.LookupSymbols(member.Expression.EndPosition, type, includeReducedExtensionMethods: true).Select(s => s.Name).ToArray();
            Assert.Contains("GetEnumerableDisposable2", symbols);
            Assert.Contains("GetEnumerableDisposable1", symbols);
        }

        [Fact]
        public void ScriptExtensionMethods()
        {
            var source =
@"static object F(this object o) { return null; }
class C
{
    void M() { this.F(); }
}
var o = new object();
o.F();";
            var compilation = CreateCompilationWithMscorlib461(source, parseOptions: TestOptions.Script);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void InteractiveExtensionMethods()
        {
            var parseOptions = TestOptions.Script;
            var references = new[] { MscorlibRef, SystemCoreRef };
            var source0 =
@"static object F(this object o) { return 0; }
var o = new object();
o.F();";
            var source1 =
@"static object G(this object o) { return 1; }
var o = new object();
o.G().F();";

            var s0 = CSharpCompilation.CreateScriptCompilation(
                "s0.dll",
                syntaxTree: SyntaxFactory.ParseSyntaxTree(source0, options: parseOptions),
                references: references);
            s0.VerifyDiagnostics();

            var s1 = CSharpCompilation.CreateScriptCompilation(
                "s1.dll",
                syntaxTree: SyntaxFactory.ParseSyntaxTree(source1, options: parseOptions),
                previousScriptCompilation: s0,
                references: references);
            s1.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(11166, "https://github.com/dotnet/roslyn/issues/11166")]
        public void SemanticModelLookup_01()
        {
            var source =
@"
public static class TestClass 
{
    public static void Test() 
    {
        var Instance = new BaseClass<int>();
        Instance.SetMember(32);
    }
}

public static class Extensions
{
    public static BC SetMember<BC, TMember>(this BC This, TMember NewValue) where BC : BaseClass<TMember> {
        This.Member = NewValue;
        return This;
    }
}

public class BaseClass<TMember>
{
    public TMember Member { get; set; }
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var instance = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => n.Identifier.ValueText == "Instance").First();
            Assert.Equal("Instance.SetMember", instance.Parent.ToString());
            var baseClass = model.GetTypeInfo(instance).Type;
            Assert.Equal("BaseClass<System.Int32>", baseClass.ToTestDisplayString());

            var setMember = model.LookupSymbols(instance.Position, baseClass, "SetMember", includeReducedExtensionMethods: true).Single();
            Assert.Equal("BaseClass<System.Int32> BaseClass<System.Int32>.SetMember<BaseClass<System.Int32>, TMember>(TMember NewValue)", setMember.ToTestDisplayString());
            Assert.Contains(setMember, model.LookupSymbols(instance.Position, baseClass, includeReducedExtensionMethods: true));
        }

        [Fact]
        [WorkItem(11166, "https://github.com/dotnet/roslyn/issues/11166")]
        public void SemanticModelLookup_02()
        {
            var source =
@"
public static class TestClass 
{
    public static void Test() 
    {
        var Instance = new BaseClass<int>();
        Instance.SetMember(32);
    }
}

public static class Extensions
{
    public static BC SetMember<BC, TMember>(this BC This, TMember NewValue) where BC : BaseClass<long> {
        return This;
    }
}

public class BaseClass<TMember>
{
    public TMember Member { get; set; }
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (7,18): error CS0311: The type 'BaseClass<int>' cannot be used as type parameter 'BC' in the generic type or method 'Extensions.SetMember<BC, TMember>(BC, TMember)'. There is no implicit reference conversion from 'BaseClass<int>' to 'BaseClass<long>'.
                //         Instance.SetMember(32);
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "SetMember").WithArguments("Extensions.SetMember<BC, TMember>(BC, TMember)", "BaseClass<long>", "BC", "BaseClass<int>").WithLocation(7, 18)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var instance = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => n.Identifier.ValueText == "Instance").First();
            Assert.Equal("Instance.SetMember", instance.Parent.ToString());
            var baseClass = model.GetTypeInfo(instance).Type;
            Assert.Equal("BaseClass<System.Int32>", baseClass.ToTestDisplayString());

            Assert.Empty(model.LookupSymbols(instance.Position, baseClass, "SetMember", includeReducedExtensionMethods: true));
            Assert.Empty(model.LookupSymbols(instance.Position, baseClass, includeReducedExtensionMethods: true).Where(s => s.Name == "SetMembers"));
        }

        [Fact]
        [WorkItem(11166, "https://github.com/dotnet/roslyn/issues/11166")]
        public void SemanticModelLookup_03()
        {
            var source =
@"
public static class TestClass 
{
    public static void Test() 
    {
        var Instance = new BaseClass<int>();
        Instance.SetMember(32);
    }
}

public static class Extensions
{
    public static BC SetMember<BC, TMember>(this BC This, TMember NewValue) where BC : BaseClass<TMember>, I1<TMember> {
        This.Member = NewValue;
        return This;
    }
}

public interface I1<T>{}

public class BaseClass<TMember> : I1<TMember>
{
    public TMember Member { get; set; }
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var instance = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => n.Identifier.ValueText == "Instance").First();
            Assert.Equal("Instance.SetMember", instance.Parent.ToString());
            var baseClass = model.GetTypeInfo(instance).Type;
            Assert.Equal("BaseClass<System.Int32>", baseClass.ToTestDisplayString());

            var setMember = model.LookupSymbols(instance.Position, baseClass, "SetMember", includeReducedExtensionMethods: true).Single();
            Assert.Equal("BaseClass<System.Int32> BaseClass<System.Int32>.SetMember<BaseClass<System.Int32>, TMember>(TMember NewValue)", setMember.ToTestDisplayString());
            Assert.Contains(setMember, model.LookupSymbols(instance.Position, baseClass, includeReducedExtensionMethods: true));
        }

        [Fact]
        [WorkItem(11166, "https://github.com/dotnet/roslyn/issues/11166")]
        public void SemanticModelLookup_04()
        {
            var source =
@"
public static class TestClass 
{
    public static void Test() 
    {
        var Instance = new BaseClass<int>();
        Instance.SetMember(32);
    }
}

public static class Extensions
{
    public static BC SetMember<BC, TMember>(this BC This, TMember NewValue) where BC : BaseClass<TMember>, I1<long> {
        This.Member = NewValue;
        return This;
    }
}

public interface I1<T>{}

public class BaseClass<TMember> : I1<TMember>
{
    public TMember Member { get; set; }
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (7,18): error CS0311: The type 'BaseClass<int>' cannot be used as type parameter 'BC' in the generic type or method 'Extensions.SetMember<BC, TMember>(BC, TMember)'. There is no implicit reference conversion from 'BaseClass<int>' to 'I1<long>'.
                //         Instance.SetMember(32);
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "SetMember").WithArguments("Extensions.SetMember<BC, TMember>(BC, TMember)", "I1<long>", "BC", "BaseClass<int>").WithLocation(7, 18)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var instance = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => n.Identifier.ValueText == "Instance").First();
            Assert.Equal("Instance.SetMember", instance.Parent.ToString());
            var baseClass = model.GetTypeInfo(instance).Type;
            Assert.Equal("BaseClass<System.Int32>", baseClass.ToTestDisplayString());

            Assert.Empty(model.LookupSymbols(instance.Position, baseClass, "SetMember", includeReducedExtensionMethods: true));
            Assert.Empty(model.LookupSymbols(instance.Position, baseClass, includeReducedExtensionMethods: true).Where(s => s.Name == "SetMembers"));
        }

        [Fact]
        public void InExtensionMethods()
        {
            var source = @"
public static class C
{
    public static void M1(this in int p) { }
    public static void M2(in this int p) { }
}";

            void validator(ModuleSymbol module)
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

                var method = type.GetMember<MethodSymbol>("M1");
                Assert.True(method.IsExtensionMethod);
                var parameter = method.Parameters[0];
                Assert.Equal(SpecialType.System_Int32, parameter.Type.SpecialType);
                Assert.Equal(RefKind.In, parameter.RefKind);

                method = type.GetMember<MethodSymbol>("M2");
                Assert.True(method.IsExtensionMethod);
                parameter = method.Parameters[0];
                Assert.Equal(SpecialType.System_Int32, parameter.Type.SpecialType);
                Assert.Equal(RefKind.In, parameter.RefKind);
            }

            CompileAndVerify(source, validator: validator, options: TestOptions.ReleaseDll);
        }

        [Fact]
        public void RefExtensionMethods()
        {
            var source = @"
public static class C
{
    public static void M1(this ref int p) { }
    public static void M2(ref this int p) { }
}";

            void validator(ModuleSymbol module)
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

                var method = type.GetMember<MethodSymbol>("M1");
                Assert.True(method.IsExtensionMethod);
                var parameter = method.Parameters[0];
                Assert.Equal(SpecialType.System_Int32, parameter.Type.SpecialType);
                Assert.Equal(RefKind.Ref, parameter.RefKind);

                method = type.GetMember<MethodSymbol>("M2");
                Assert.True(method.IsExtensionMethod);
                parameter = method.Parameters[0];
                Assert.Equal(SpecialType.System_Int32, parameter.Type.SpecialType);
                Assert.Equal(RefKind.Ref, parameter.RefKind);
            }

            CompileAndVerify(source, validator: validator, options: TestOptions.ReleaseDll);
        }

        [Fact]
        [WorkItem(65020, "https://github.com/dotnet/roslyn/issues/65020")]
        public void ReduceExtensionsMethodOnReceiverTypeSystemVoid()
        {
            var source =
@"static class C
{
    static void M(this object x)
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            compilation.VerifyDiagnostics();

            var extensionMethod = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>("M");
            Assert.True(extensionMethod.IsExtensionMethod);

            var systemVoidType = compilation.GetSpecialType(SpecialType.System_Void);
            Assert.Equal(SpecialType.System_Void, systemVoidType.SpecialType);

            var reduced = extensionMethod.ReduceExtensionMethod(systemVoidType, null!);
            Assert.Null(reduced);

            reduced = extensionMethod.ReduceExtensionMethod(systemVoidType, compilation);
            Assert.Null(reduced);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68110")]
        public void DefaultSyntaxValueReentrancy_01()
        {
            var source =
                """
                #nullable enable

                [A(3, X = 6)]
                public struct A
                {
                    public int X;

                    public A(int x, A a = new A().M(1)) { }
                }

                public static class AExt
                {
                    public static void M(this A s, ref int i) {}
                }
                """;
            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);

            var a = compilation.GlobalNamespace.GetTypeMember("A").InstanceConstructors.Where(c => !c.IsDefaultValueTypeConstructor()).Single();

            Assert.Null(a.Parameters[1].ExplicitDefaultValue);
            Assert.True(a.Parameters[1].HasExplicitDefaultValue);

            compilation.VerifyDiagnostics(
                // (3,2): error CS0616: 'A' is not an attribute class
                // [A(3, X = 6)]
                Diagnostic(ErrorCode.ERR_NotAnAttributeClass, "A").WithArguments("A").WithLocation(3, 2),
                // (3,2): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A(3, X = 6)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "A(3, X = 6)").WithLocation(3, 2),
                // (8,37): error CS1620: Argument 2 must be passed with the 'ref' keyword
                //     public A(int x, A a = new A().M(1)) { }
                Diagnostic(ErrorCode.ERR_BadArgRef, "1").WithArguments("2", "ref").WithLocation(8, 37));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74404")]
        public void Repro_74404()
        {
            var source = """
                #nullable enable
                class C<T>;
                static class CExt
                {
                    public static void M<T>(this C<T> c)
                    {
                        c.M = 42;
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,9): error CS1656: Cannot assign to 'M' because it is a 'method group'
                //         c.M = 42;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "c.M").WithArguments("M", "method group").WithLocation(7, 9));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73746")]
        public void TestInModifier()
        {
            var source = """
                struct S;
                enum E;
                class C;
                interface I;
                delegate void D();

                static class Extensions
                {
                    public static void M1(this in S s) { }
                    public static void M2(this in E e) { }
                    public static void M3(this in C c) { }
                    public static void M4(this in I i) { }
                    public static void M5(this in D d) { }
                    public static void M6(this in S[] s) { }
                    public static void M7<T>(this in T t) where T : struct { }
                    public static unsafe void M8(this in int* ptr) { }
                }
                """;

            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
            comp.VerifyEmitDiagnostics(
                // (11,24): error CS8338: The first 'in' or 'ref readonly' parameter of the extension method 'M3' must be a concrete (non-generic) value type.
                //     public static void M3(this in C c) { }
                Diagnostic(ErrorCode.ERR_InExtensionMustBeValueType, "M3").WithArguments("M3").WithLocation(11, 24),
                // (12,24): error CS8338: The first 'in' or 'ref readonly' parameter of the extension method 'M4' must be a concrete (non-generic) value type.
                //     public static void M4(this in I i) { }
                Diagnostic(ErrorCode.ERR_InExtensionMustBeValueType, "M4").WithArguments("M4").WithLocation(12, 24),
                // (13,24): error CS8338: The first 'in' or 'ref readonly' parameter of the extension method 'M5' must be a concrete (non-generic) value type.
                //     public static void M5(this in D d) { }
                Diagnostic(ErrorCode.ERR_InExtensionMustBeValueType, "M5").WithArguments("M5").WithLocation(13, 24),
                // (14,24): error CS8338: The first 'in' or 'ref readonly' parameter of the extension method 'M6' must be a concrete (non-generic) value type.
                //     public static void M6(this in S[] s) { }
                Diagnostic(ErrorCode.ERR_InExtensionMustBeValueType, "M6").WithArguments("M6").WithLocation(14, 24),
                // (15,24): error CS8338: The first 'in' or 'ref readonly' parameter of the extension method 'M7' must be a concrete (non-generic) value type.
                //     public static void M7<T>(this in T t) where T : struct { }
                Diagnostic(ErrorCode.ERR_InExtensionMustBeValueType, "M7").WithArguments("M7").WithLocation(15, 24),
                // (16,42): error CS1103: The receiver parameter of an extension cannot be of type 'int*'
                //     public static unsafe void M8(this in int* ptr) { }
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "int*").WithArguments("int*").WithLocation(16, 42));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73746")]
        public void TestRefReadonlyModifier()
        {
            var source = """
                struct S;
                enum E;
                class C;
                interface I;
                delegate void D();
                
                static class Extensions
                {
                    public static void M1(this ref readonly S s) { }
                    public static void M2(this ref readonly E e) { }
                    public static void M3(this ref readonly C c) { }
                    public static void M4(this ref readonly I i) { }
                    public static void M5(this ref readonly D d) { }
                    public static void M6(this ref readonly S[] s) { }
                    public static void M7<T>(this ref readonly T t) where T : struct { }
                    public static unsafe void M8(this ref readonly int* ptr) { }
                }
                """;

            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
            comp.VerifyEmitDiagnostics(
                // (11,24): error CS8338: The first 'in' or 'ref readonly' parameter of the extension method 'M3' must be a concrete (non-generic) value type.
                //     public static void M3(this ref readonly C c) { }
                Diagnostic(ErrorCode.ERR_InExtensionMustBeValueType, "M3").WithArguments("M3").WithLocation(11, 24),
                // (12,24): error CS8338: The first 'in' or 'ref readonly' parameter of the extension method 'M4' must be a concrete (non-generic) value type.
                //     public static void M4(this ref readonly I i) { }
                Diagnostic(ErrorCode.ERR_InExtensionMustBeValueType, "M4").WithArguments("M4").WithLocation(12, 24),
                // (13,24): error CS8338: The first 'in' or 'ref readonly' parameter of the extension method 'M5' must be a concrete (non-generic) value type.
                //     public static void M5(this ref readonly D d) { }
                Diagnostic(ErrorCode.ERR_InExtensionMustBeValueType, "M5").WithArguments("M5").WithLocation(13, 24),
                // (14,24): error CS8338: The first 'in' or 'ref readonly' parameter of the extension method 'M6' must be a concrete (non-generic) value type.
                //     public static void M6(this ref readonly S[] s) { }
                Diagnostic(ErrorCode.ERR_InExtensionMustBeValueType, "M6").WithArguments("M6").WithLocation(14, 24),
                // (15,24): error CS8338: The first 'in' or 'ref readonly' parameter of the extension method 'M7' must be a concrete (non-generic) value type.
                //     public static void M7<T>(this ref readonly T t) where T : struct { }
                Diagnostic(ErrorCode.ERR_InExtensionMustBeValueType, "M7").WithArguments("M7").WithLocation(15, 24),
                // (16,52): error CS1103: The receiver parameter of an extension cannot be of type 'int*'
                //     public static unsafe void M8(this ref readonly int* ptr) { }
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "int*").WithArguments("int*").WithLocation(16, 52));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73746")]
        public void TestInModifierInExtensionBlock()
        {
            var source = """
                struct S;
                enum E;
                class C;
                interface I;
                delegate void D();

                static unsafe class Extensions
                {
                    extension(in S s) { }
                    extension(in E e) { }
                    extension(in C c) { }
                    extension(in I i) { }
                    extension(in D d) { }
                    extension(in S[] s) { }
                    extension<T>(in T t) where T : struct { }
                    extension(in int* ptr) { }
                }
                """;

            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
            comp.VerifyEmitDiagnostics(
                // (11,18): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension(in C c) { }
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "C").WithLocation(11, 18),
                // (12,18): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension(in I i) { }
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "I").WithLocation(12, 18),
                // (13,18): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension(in D d) { }
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "D").WithLocation(13, 18),
                // (14,18): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension(in S[] s) { }
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "S[]").WithLocation(14, 18),
                // (15,21): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension<T>(in T t) where T : struct { }
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(15, 21),
                // (16,18): error CS1103: The receiver parameter of an extension cannot be of type 'int*'
                //     extension(in int* ptr) { }
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "int*").WithArguments("int*").WithLocation(16, 18));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73746")]
        public void TestRefReadonlyModifierInExtensionBlock()
        {
            var source = """
                struct S;
                enum E;
                class C;
                interface I;
                delegate void D();
                
                static unsafe class Extensions
                {
                    extension(ref readonly S s) { }
                    extension(ref readonly E e) { }
                    extension(ref readonly C c) { }
                    extension(ref readonly I i) { }
                    extension(ref readonly D d) { }
                    extension(ref readonly S[] s) { }
                    extension<T>(ref readonly T t) where T : struct { }
                    extension(ref readonly int* ptr) { }
                }
                """;

            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
            comp.VerifyEmitDiagnostics(
                // (11,28): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension(ref readonly C c) { }
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "C").WithLocation(11, 28),
                // (12,28): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension(ref readonly I i) { }
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "I").WithLocation(12, 28),
                // (13,28): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension(ref readonly D d) { }
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "D").WithLocation(13, 28),
                // (14,28): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension(ref readonly S[] s) { }
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "S[]").WithLocation(14, 28),
                // (15,31): error CS9301: The 'in' or 'ref readonly' receiver parameter of extension must be a concrete (non-generic) value type.
                //     extension<T>(ref readonly T t) where T : struct { }
                Diagnostic(ErrorCode.ERR_InExtensionParameterMustBeValueType, "T").WithLocation(15, 31),
                // (16,28): error CS1103: The receiver parameter of an extension cannot be of type 'int*'
                //     extension(ref readonly int* ptr) { }
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "int*").WithArguments("int*").WithLocation(16, 28));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73746")]
        public void TestPointerExtensionParameterType()
        {
            var source = """
                unsafe
                {
                    int* ptr = null;
                    ptr.M();
                }
                """;

            // Equivalent to:
            // public static class Extensions {
            //     public static void M(this int* ptr) { }
            // }
            var ilSource = """
                .class public auto ansi abstract sealed beforefieldinit Extensions
                    extends [mscorlib]System.Object
                {
                    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
                        01 00 00 00
                    )
                    // Methods
                    .method public hidebysig static 
                        void M (
                            int32* ptr
                        ) cil managed 
                    {
                        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
                            01 00 00 00
                        )
                        // Method begins at RVA 0x2050
                        // Code size 1 (0x1)
                        .maxstack 8

                        IL_0000: ret
                    } // end of method Extensions::M

                } // end of class Extensions
                """;

            var comp = CreateCompilationWithIL(source, ilSource, options: TestOptions.UnsafeDebugExe);
            comp.VerifyDiagnostics(
                // (4,9): error CS1061: 'int*' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'int*' could be found (are you missing a using directive or an assembly reference?)
                //     ptr.M();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("int*", "M").WithLocation(4, 9));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73746")]
        public void TestDynamicExtensionParameterType()
        {
            var source = """
                object obj = null;
                obj.M();
                """;

            // Equivalent to:
            // public static class Extensions {
            //     public static void M(this dynamic obj) { }
            // }
            var ilSource = """
                .class public auto ansi abstract sealed beforefieldinit Extensions
                    extends [mscorlib]System.Object
                {
                    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
                        01 00 00 00
                    )
                    // Methods
                    .method public hidebysig static 
                        void M (
                            object obj
                        ) cil managed 
                    {
                        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
                            01 00 00 00
                        )
                        .param [1]
                            .custom instance void [mscorlib]System.Runtime.CompilerServices.DynamicAttribute::.ctor() = (
                                01 00 00 00
                            )
                        // Method begins at RVA 0x2050
                        // Code size 1 (0x1)
                        .maxstack 8

                        IL_0000: ret
                    } // end of method Extensions::M

                } // end of class Extensions
                """;

            var comp = CreateCompilationWithIL(source, ilSource);
            comp.VerifyDiagnostics(
                // (2,5): error CS1061: 'object' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                // obj.M();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("object", "M").WithLocation(2, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73746")]
        public void TestFunctionPointerExtensionParameterType()
        {
            var source = """
                unsafe
                {
                    delegate*<void> ptr = null;
                    ptr.M();
                }
                """;

            // Equivalent to:
            // public static class Extensions {
            //     public static unsafe void M(this delegate*<void> ptr) { }
            // }
            var ilSource = """
                .class public auto ansi abstract sealed beforefieldinit Extensions
                    extends [mscorlib]System.Object
                {
                    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
                        01 00 00 00
                    )
                    // Methods
                    .method public hidebysig static 
                        void M (
                            method void *() ptr
                        ) cil managed 
                    {
                        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
                            01 00 00 00
                        )
                        // Method begins at RVA 0x2050
                        // Code size 1 (0x1)
                        .maxstack 8

                        IL_0000: ret
                    } // end of method Extensions::M

                } // end of class Extensions
                """;

            var comp = CreateCompilationWithIL(source, ilSource, options: TestOptions.UnsafeDebugExe);
            comp.VerifyDiagnostics(
                // (4,9): error CS1061: 'delegate*<void>' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'delegate*<void>' could be found (are you missing a using directive or an assembly reference?)
                //     ptr.M();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("delegate*<void>", "M").WithLocation(4, 9));
        }
    }
}
