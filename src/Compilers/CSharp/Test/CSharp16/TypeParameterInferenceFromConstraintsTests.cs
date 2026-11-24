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

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Tests for "Type Parameter Inference from Constraints"
    /// (https://github.com/dotnet/csharplang/issues/9453).
    /// </summary>
    public class TypeParameterInferenceFromConstraintsTests : CSharpTestBase
    {
        private static IMethodSymbol GetInferredMethod(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var syntax = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Single(i => model.GetSymbolInfo(i).Symbol is IMethodSymbol { Name: "M" });
            return Assert.IsAssignableFrom<IMethodSymbol>(model.GetSymbolInfo(syntax).Symbol);
        }

        [Fact]
        public void Basic_IEnumerable_LanguageVersions()
        {
            var source = """
using System.Collections.Generic;

class C
{
    static void Main()
    {
        List<int> l = [1, 2, 3];
        M(l);
    }

    static void M<TEnumerable, TElement>(TEnumerable t) where TEnumerable : IEnumerable<TElement>
    {
    }
}
""";

            CreateCompilation(source, parseOptions: TestOptions.Regular15).VerifyDiagnostics(
                // (8,9): error CS0411: The type arguments for method 'C.M<TEnumerable, TElement>(TEnumerable)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M(l);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("C.M<TEnumerable, TElement>(TEnumerable)").WithLocation(8, 9));

            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularNext);
            comp.VerifyDiagnostics();

            string expected = "void C.M<System.Collections.Generic.List<System.Int32>, System.Int32>(System.Collections.Generic.List<System.Int32> t)";

            var method = GetInferredMethod(comp);
            Assert.Equal(expected, method.ToTestDisplayString());

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            method = GetInferredMethod(comp);
            Assert.Equal(expected, method.ToTestDisplayString());
        }

        [Fact]
        public void ContravariantConstraint_ChangesInferredType_01()
        {
            var source = """
using System;

static Type M1<T, U>(T t, U u) where T : I<U> => typeof(U);
static Type M2<U>(I<U> t, U u) => typeof(U);
Console.Write(M1(new Holder(), new B()).Name);
Console.Write(M2(new Holder(), new B()).Name);
interface I<in T> { }
class A { }
class B : A { }
class Holder : I<A> { }
""";

            CompileAndVerify(
                CreateCompilation(source, parseOptions: TestOptions.Regular15, options: TestOptions.DebugExe),
                expectedOutput: "BA").VerifyDiagnostics();

            CompileAndVerify(
                CreateCompilation(source, parseOptions: TestOptions.RegularNext, options: TestOptions.DebugExe),
                expectedOutput: "AA").VerifyDiagnostics(); // This behavior change is documented as breaking

            CompileAndVerify(
                CreateCompilation(source, options: TestOptions.DebugExe),
                expectedOutput: "AA").VerifyDiagnostics();
        }

        [Fact]
        public void ContravariantConstraint_ChangesInferredType_02()
        {
            var source = """
using System;

static Type M1<T, U>(T t, U u) where T : I<U> where U : B => typeof(U);
Console.Write(M1(new Holder(), new B()).Name);
interface I<in T> { }
class A { }
class B : A { }
class Holder : I<A> { }
""";

            CompileAndVerify(
                CreateCompilation(source, parseOptions: TestOptions.Regular15, options: TestOptions.DebugExe),
                expectedOutput: "B").VerifyDiagnostics();

            var expected =
                // (4,15): error CS0311: The type 'A' cannot be used as type parameter 'U' in the generic type or method 'M1<T, U>(T, U)'. There is no implicit reference conversion from 'A' to 'B'.
                // Console.Write(M1(new Holder(), new B()).Name);
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M1").WithArguments("M1<T, U>(T, U)", "B", "U", "A").WithLocation(4, 15);

            // This behavior change is documented as breaking.
            // However it feels like the problem is actually in how inference through variant interfaces is designed.
            // It adds type argument as a lower bound, that automatically makes more specific bound to lose. But in case of 'in' type parameter,
            // a more specific bound is probably a better choice.
            CreateCompilation(source, parseOptions: TestOptions.RegularNext, options: TestOptions.DebugExe).
                VerifyDiagnostics(expected);

            CreateCompilation(source, options: TestOptions.DebugExe).
                VerifyDiagnostics(expected);
        }

        [Fact]
        public void ConstraintInference_ChangesInferredType()
        {
            var source = """
using System;

static Type M<T, U, V>(T t, V v, Func<V, U> f) where T : I<U> => typeof(U);
Console.WriteLine(M(new Holder(), 0, x => new B()).Name);
interface I<in T> { }
class A { }
class B : A { }
class Holder : I<A> { }
""";

            CompileAndVerify(
                CreateCompilation(source, parseOptions: TestOptions.Regular15, options: TestOptions.DebugExe),
                expectedOutput: "B").VerifyDiagnostics();

            CompileAndVerify(
                CreateCompilation(source, parseOptions: TestOptions.RegularNext, options: TestOptions.DebugExe),
                expectedOutput: "A").VerifyDiagnostics(); // This behavior change is documented as breaking

            CompileAndVerify(
                CreateCompilation(source, options: TestOptions.DebugExe),
                expectedOutput: "A").VerifyDiagnostics();
        }

        [Fact]
        public void MultipleConstraints()
        {
            var source = """
using System;
using System.Collections.Generic;

class C
{
    static void M<T, X>(T obj) where T : IEnumerable<X>, IComparable<X>
    {
    }

    class MyClass : IComparable<string>, IEnumerable<string>
    {
        public int CompareTo(string other) => 0;
        public IEnumerator<string> GetEnumerator() => null;
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => null;
    }

    static void Main()
    {
        var c = new MyClass();
        M(c);
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var method = GetInferredMethod(comp);
            Assert.Equal(
                "void C.M<C.MyClass, System.String>(C.MyClass obj)",
                method.ToTestDisplayString());
        }

        [Fact]
        public void ConstraintWithBaseClass()
        {
            var source = """
class Base<T> { }
class Derived : Base<int> { }

class C
{
    static void M<TDerived, TElement>(TDerived t) where TDerived : Base<TElement>
    {
    }

    static void Main()
    {
        M(new Derived());
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var method = GetInferredMethod(comp);
            Assert.Equal("void C.M<Derived, System.Int32>(Derived t)", method.ToTestDisplayString());
        }

        [Fact]
        public void TransitiveConstraintDependence()
        {
            // TInner depends on TMiddle (constraint), TMiddle depends on TOuter (constraint).
            // Fixing TOuter -> TMiddle -> TInner via successive constraint lower-bound inferences.
            var source = """
using System.Collections.Generic;

class C
{
    static void M<TOuter, TMiddle, TInner>(TOuter t)
        where TOuter : IEnumerable<TMiddle>
        where TMiddle : IEnumerable<TInner>
    {
    }

    static void Main()
    {
        M(new List<List<int>>());
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var method = GetInferredMethod(comp);
            Assert.Equal(
                "void C.M<System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>, System.Collections.Generic.List<System.Int32>, System.Int32>(System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>> t)",
                method.ToTestDisplayString());
        }

        [Fact]
        public void LambdaParameterTypeFromConstraint()
        {
            // The constraint-inferred type parameter is used as the lambda's delegate parameter type.
            // TElement must be fixed (via the constraint) before the lambda can be bound.
            var source = """
using System;
using System.Collections.Generic;

class C
{
    static void M<TEnumerable, TElement>(TEnumerable t, Func<TElement, bool> predicate)
        where TEnumerable : IEnumerable<TElement>
    {
    }

    static void Main()
    {
        var l = new List<int> { 1, 2, 3 };
        M(l, x => x.ToString("X") == "2");
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var method = GetInferredMethod(comp);
            Assert.Equal(
                "void C.M<System.Collections.Generic.List<System.Int32>, System.Int32>(System.Collections.Generic.List<System.Int32> t, System.Func<System.Int32, System.Boolean> predicate)",
                method.ToTestDisplayString());
        }

        [Fact]
        public void LambdaParameterAndReturnTypeFromConstraint()
        {
            var source = """
using System;
using System.Collections.Generic;

class C
{
    static TElement M<TEnumerable, TElement>(TEnumerable t, Func<TElement, TElement> f)
        where TEnumerable : IEnumerable<TElement>
    {
        TElement last = default;
        foreach (var e in t) last = f(e);
        return last;
    }

    static void Main()
    {
        var r = M(new List<int> { 5 }, x => x + 1);
        System.Console.WriteLine(r);
    }
}
""";

            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "6").VerifyDiagnostics();

            var method = GetInferredMethod(comp);
            Assert.Equal(
                "System.Int32 C.M<System.Collections.Generic.List<System.Int32>, System.Int32>(System.Collections.Generic.List<System.Int32> t, System.Func<System.Int32, System.Int32> f)",
                method.ToTestDisplayString());
        }

        [Fact]
        public void BreakingChange_OverloadResolution()
        {
            // Documented breaking change: with the feature, the generic overload becomes applicable
            // and is preferred over the object overload.
            var source = """
using System;
using System.Collections.Generic;

class C
{
    static void M(object obj) => Console.WriteLine("non-generic");
    static void M<T, U>(T t) where T : IEnumerable<U> => Console.WriteLine("generic");

    static void Main()
    {
        M("test");
    }
}
""";

            CompileAndVerify(
                CreateCompilation(source, parseOptions: TestOptions.Regular15, options: TestOptions.DebugExe),
                expectedOutput: "non-generic").VerifyDiagnostics();

            CompileAndVerify(
                CreateCompilation(source, parseOptions: TestOptions.RegularNext, options: TestOptions.DebugExe),
                expectedOutput: "generic").VerifyDiagnostics();

            CompileAndVerify(
                CreateCompilation(source, options: TestOptions.DebugExe),
                expectedOutput: "generic").VerifyDiagnostics();
        }

        [Fact]
        public void ConstraintDoesNotMentionOtherTypeParameter_StillFails()
        {
            // TEnumerable has a constraint (so the feature's dependence/lower-bound logic runs), but
            // that constraint does not mention TElement, so TElement still cannot be inferred.
            var source = """
using System;

class C
{
    static void M<TEnumerable, TElement>(TEnumerable t) where TEnumerable : IComparable<TEnumerable>
    {
    }

    static void Main()
    {
        M(1);
    }
}
""";

            CreateCompilation(source).VerifyDiagnostics(
                // (11,9): error CS0411: The type arguments for method 'C.M<TEnumerable, TElement>(TEnumerable)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M(1);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("C.M<TEnumerable, TElement>(TEnumerable)").WithLocation(11, 9));
        }

        [Fact]
        public void DirectTypeParameterConstraint()
        {
            // where TEnumerable : IEnumerable<TElement>, and the element bound flows to TElement,
            // while a second type parameter constrains directly to another.
            var source = """
using System.Collections.Generic;

class C
{
    static void M<T, U>(T t) where T : U where U : IEnumerable<int>
    {
    }

    static void Main()
    {
        M(new List<int>());
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var method = GetInferredMethod(comp);
            Assert.Equal(
                "void C.M<System.Collections.Generic.List<System.Int32>, System.Collections.Generic.List<System.Int32>>(System.Collections.Generic.List<System.Int32> t)",
                method.ToTestDisplayString());
        }

        [Fact]
        public void ExtensionMethod_01()
        {
            var source = """
using System.Collections.Generic;

static class Extensions
{
    public static void M<TEnumerable, TElement>(this TEnumerable t) where TEnumerable : IEnumerable<TElement>
    {
    }
}

class C
{
    static void Main()
    {
        List<int> l = [1, 2, 3];
        l.M();
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var method = GetInferredMethod(comp);
            Assert.Equal("M", method.Name);
            Assert.Equal(
                new[] { "System.Collections.Generic.List<System.Int32>", "System.Int32" },
                method.TypeArguments.Select(t => t.ToTestDisplayString()));

            string expected = "void System.Collections.Generic.List<System.Int32>.M<System.Collections.Generic.List<System.Int32>, System.Int32>()";

            Assert.Equal(MethodKind.ReducedExtension, method.MethodKind);
            AssertEx.Equal(expected, method.ToTestDisplayString());
            AssertEx.Equal(expected, method.ReducedFrom.OriginalDefinition.ReduceExtensionMethod(method.ReceiverType).ToTestDisplayString());
        }

        [Fact]
        public void ExtensionMethod_02()
        {
            var source = """
using System.Collections.Generic;

static class Extensions
{
    extension<TEnumerable, TElement>(TEnumerable t) where TEnumerable : IEnumerable<TElement>
    {
        public void M()
        {
        }
    }
}

class C
{
    static void Main()
    {
        List<int> l = [1, 2, 3];
        l.M();
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var method = GetInferredMethod(comp);
            Assert.Equal("M", method.Name);
            AssertEx.Equal("Extensions.extension<System.Collections.Generic.List<int>, int>(System.Collections.Generic.List<int>).M()", method.ToDisplayString());
        }

        [Fact]
        public void ExtensionMethod_03()
        {
            var source = """
using System.Collections.Generic;

static class Extensions
{
    public static void M<TEnumerable, TElement>(this TEnumerable t) where TEnumerable : IEnumerable<TElement>
    {
        System.Console.Write(typeof(TElement).Name);
    }
}

class C
{
    static void Main()
    {
        List<int> l = [1, 2, 3];
        System.Action a = l.M;
        a();
        var b = l.M;
        b();
    }
}
""";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular15);
            comp.VerifyDiagnostics(
                // (16,27): error CS0411: The type arguments for method 'Extensions.M<TEnumerable, TElement>(TEnumerable)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         System.Action a = l.M;
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "l.M").WithArguments("Extensions.M<TEnumerable, TElement>(TEnumerable)").WithLocation(16, 27),
                // (18,17): error CS8917: The delegate type could not be inferred.
                //         var b = l.M;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "l.M").WithLocation(18, 17)
                );

            comp = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularNext);
            CompileAndVerify(comp, expectedOutput: "Int32Int32").VerifyDiagnostics();

            comp = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "Int32Int32").VerifyDiagnostics();
        }

        [Fact]
        public void ExtensionMethod_04()
        {
            var source = """
using System.Collections.Generic;

static class Extensions
{
    extension<TElement, TEnumerable>(TEnumerable t) where TEnumerable : IEnumerable<TElement>
    {
        public void M()
        {
            System.Console.Write(typeof(TElement).Name);
        }
    }
}

class C
{
    static void Main()
    {
        List<int> l = [1, 2, 3];
        System.Action a = l.M;
        a();
        var b = l.M;
        b();
    }
}
""";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular15);
            comp.VerifyDiagnostics(
                // (19,27): error CS0411: The type arguments for method 'Extensions.extension<TElement, TEnumerable>(TEnumerable).M()' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         System.Action a = l.M;
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "l.M").WithArguments("Extensions.extension<TElement, TEnumerable>(TEnumerable).M()").WithLocation(19, 27),
                // (21,17): error CS8917: The delegate type could not be inferred.
                //         var b = l.M;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "l.M").WithLocation(21, 17)
                );

            comp = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularNext);
            CompileAndVerify(comp, expectedOutput: "Int32Int32").VerifyDiagnostics();

            comp = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "Int32Int32").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/86458")]
        public void ExtensionMethod_05()
        {
            var source = """
using System.Collections.Generic;

static class Extensions
{
    public static void M1<TEnumerable, TElement>(this TEnumerable t) where TEnumerable : IEnumerable<TElement>
    {
    }

    extension<TElement, TEnumerable>(TEnumerable t) where TEnumerable : IEnumerable<TElement>
    {
        public void M2()
        {
        }
    }
}

class C
{
    static void Main()
    {
        List<int> l = [1, 2, 3];
        _ = nameof(l.M1);
        _ = nameof(l.M2);
    }
}
""";

            var expected = new[] {
                // (22,20): error CS8093: Extension method groups are not allowed as an argument to 'nameof'.
                //         _ = nameof(l.M1);
                Diagnostic(ErrorCode.ERR_NameofExtensionMethod, "l.M1").WithLocation(22, 20),
                // (23,20): error CS8093: Extension method groups are not allowed as an argument to 'nameof'.
                //         _ = nameof(l.M2);
                Diagnostic(ErrorCode.ERR_NameofExtensionMethod, "l.M2").WithLocation(23, 20)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular15);
            comp.VerifyDiagnostics(expected);

            comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(expected);
        }

        [Fact]
        public void ExtensionProperty()
        {
            var source = """
using System.Collections.Generic;

static class Extensions
{
    extension<TEnumerable, TElement>(TEnumerable t) where TEnumerable : IEnumerable<TElement>
    {
        public int P
        {
            get
            {
                System.Console.Write(typeof(TElement).Name);
                return 0;
            }
        }
    }
}

class C
{
    static void Main()
    {
        List<int> l = [1, 2, 3];
        _ = l.P;
    }
}
""";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular15);
            comp.VerifyDiagnostics(
                // (7,20): error CS9295: The type parameter `TElement` is not referenced by either the extension parameter or a parameter of this member
                //         public int P => 0;
                Diagnostic(ErrorCode.ERR_UnderspecifiedExtension, "P").WithArguments("TElement").WithLocation(7, 20),
                // (23,13): error CS1061: 'List<int>' does not contain a definition for 'P' and no accessible extension method 'P' accepting a first argument of type 'List<int>' could be found (are you missing a using directive or an assembly reference?)
                //         _ = l.P;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "l.P").WithArguments("System.Collections.Generic.List<int>", "P").WithLocation(23, 13)
                );

            comp = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularNext);
            CompileAndVerify(comp, expectedOutput: "Int32").VerifyDiagnostics();

            comp = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "Int32").VerifyDiagnostics();
        }

        [Fact]
        public void ExtensionIndexer_01()
        {
            var source = """
using System.Collections.Generic;

static class Extensions
{
    extension<TEnumerable, TElement>(int t) where TEnumerable : IEnumerable<TElement>
    {
        public int this[TEnumerable i]
        {
            get
            {
                System.Console.Write(typeof(TElement).Name);
                return 0;
            }
        }
    }
}

class C
{
    static void Main()
    {
        List<int> l = [1, 2, 3];
        _ = 1[l];
    }
}
""";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular15);
            comp.VerifyDiagnostics(
                // (7,20): error CS9295: The type parameter `TElement` is not referenced by either the extension parameter or a parameter of this member
                //         public int this[TEnumerable i]
                Diagnostic(ErrorCode.ERR_UnderspecifiedExtension, "this").WithArguments("TElement").WithLocation(7, 20),
                // (23,13): error CS0021: Cannot apply indexing with [] to an expression of type 'int'
                //         _ = 1[l];
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "1[l]").WithArguments("int").WithLocation(23, 13)
                );

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularNext, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "Int32").VerifyDiagnostics();

            comp = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "Int32").VerifyDiagnostics();
        }

        [Fact]
        public void ExtensionIndexer_02()
        {
            var source = """
using System.Collections.Generic;

static class Extensions
{
    extension<TEnumerable, TElement1, TElement2>(int t) where TEnumerable : IEnumerable<TElement1> where TElement1 : TElement2
    {
        public int this[TEnumerable i]
        {
            get
            {
                System.Console.Write(typeof(TElement2).Name);
                return 0;
            }
        }
    }
}

class C
{
    static void Main()
    {
        List<int> l = [1, 2, 3];
        _ = 1[l];
    }
}
""";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular15);
            comp.VerifyDiagnostics(
                // (7,20): error CS9295: The type parameter `TElement1` is not referenced by either the extension parameter or a parameter of this member
                //         public int this[TEnumerable i]
                Diagnostic(ErrorCode.ERR_UnderspecifiedExtension, "this").WithArguments("TElement1").WithLocation(7, 20),
                // (7,20): error CS9295: The type parameter `TElement2` is not referenced by either the extension parameter or a parameter of this member
                //         public int this[TEnumerable i]
                Diagnostic(ErrorCode.ERR_UnderspecifiedExtension, "this").WithArguments("TElement2").WithLocation(7, 20),
                // (23,13): error CS0021: Cannot apply indexing with [] to an expression of type 'int'
                //         _ = 1[l];
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "1[l]").WithArguments("int").WithLocation(23, 13)
                );

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularNext, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "Int32").VerifyDiagnostics();

            comp = CreateCompilation(source, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "Int32").VerifyDiagnostics();
        }

        [Fact]
        public void ExtensionIndexer_03()
        {
            var source = """
using System.Collections.Generic;

static class Extensions
{
    extension<TEnumerable, TElement>(int t) where TEnumerable : IEnumerable<TElement>
    {
        public int this[TElement i]
        {
            get
            {
                return 0;
            }
        }
    }
}
""";

            var expected =
                // (7,20): error CS9295: The type parameter `TEnumerable` is not referenced by either the extension parameter or a parameter of this member
                //         public int this[TElement i]
                Diagnostic(ErrorCode.ERR_UnderspecifiedExtension, "this").WithArguments("TEnumerable").WithLocation(7, 20);

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular15);
            comp.VerifyDiagnostics(expected);

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expected);
        }

        [Fact]
        public void ExtensionIndexer_04()
        {
            var source = """
using System.Collections.Generic;

static class Extensions
{
    extension<TEnumerable, TElement1, TElement2>(int t) where TEnumerable : IEnumerable<TElement1>
    {
        public int this[TEnumerable i]
        {
            get
            {
                return 0;
            }
        }
    }
}
""";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular15);
            comp.VerifyDiagnostics(
                // (7,20): error CS9295: The type parameter `TElement1` is not referenced by either the extension parameter or a parameter of this member
                //         public int this[TEnumerable i]
                Diagnostic(ErrorCode.ERR_UnderspecifiedExtension, "this").WithArguments("TElement1").WithLocation(7, 20),
                // (7,20): error CS9295: The type parameter `TElement2` is not referenced by either the extension parameter or a parameter of this member
                //         public int this[TEnumerable i]
                Diagnostic(ErrorCode.ERR_UnderspecifiedExtension, "this").WithArguments("TElement2").WithLocation(7, 20)
                );

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,20): error CS9295: The type parameter `TElement2` is not referenced by either the extension parameter or a parameter of this member
                //         public int this[TEnumerable i]
                Diagnostic(ErrorCode.ERR_UnderspecifiedExtension, "this").WithArguments("TElement2").WithLocation(7, 20)
                );
        }

        [Fact]
        public void NullableReferenceType_AnnotationFlows_01()
        {
            var source = """
#nullable enable
using System.Collections.Generic;

class C
{
    static void M<TEnumerable, TElement>(TEnumerable t) where TEnumerable : IEnumerable<TElement>
    {
    }

    static void Main()
    {
        M(new List<string?>());
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var method = GetInferredMethod(comp);
            var element = method.TypeArguments[1];
            Assert.Equal(SpecialType.System_String, element.SpecialType);
            Assert.Equal(CodeAnalysis.NullableAnnotation.Annotated, element.NullableAnnotation);
            AssertEx.Equal("System.Collections.Generic.List<System.String?>!", method.TypeArguments[0].ToTestDisplayString(includeNonNullable: true));
        }

        [Fact]
        public void NullableReferenceType_AnnotationFlows_02()
        {
            var source = """
#nullable enable
using System.Collections.Generic;

class C
{
    static TEnumerable M1<TEnumerable, TElement>(TEnumerable t) where TEnumerable : IEnumerable<TElement> => throw null!; 
    static TElement M2<TEnumerable, TElement>(TEnumerable t) where TEnumerable : IEnumerable<TElement> => throw null!; 

    static void Main()
    {
        var m1 = M1(new List<string?>());
        m1.ToString();
        m1[0].ToString();

        M2(new List<string?>()).ToString();
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (13,9): warning CS8602: Dereference of a possibly null reference.
                //         m1[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "m1[0]").WithLocation(13, 9),
                // (15,9): warning CS8602: Dereference of a possibly null reference.
                //         M2(new List<string?>()).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "M2(new List<string?>())").WithLocation(15, 9)
                );
        }

        [Fact]
        public void NullableReferenceType_AnnotationFlowsFromOrdinaryBound()
        {
            var source = """
#nullable enable

class C
{
    static void M<T, U>(T x, T y) where T : U
    {
    }

    static void Main()
    {
        M("", null);
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var method = GetInferredMethod(comp);
            AssertEx.Equal(
                new[] { "System.String?", "System.String?" },
                method.TypeArguments.SelectAsArray(t => t.ToTestDisplayString(includeNonNullable: true)));
        }

        [Fact]
        public void NullableReferenceType_DoesNotCrashNullableAnalysis()
        {
            // Exercises the NullableWalker re-inference path through the constraint lower-bound inference.
            var source = """
#nullable enable
using System;
using System.Collections.Generic;

class C
{
    static void M<TEnumerable, TElement>(TEnumerable t, Func<TElement, TElement> f)
        where TEnumerable : IEnumerable<TElement>
    {
    }

    static void Main()
    {
        M(new List<string>(), x => x);
    }
}
""";

            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void OrdinaryBoundConflictsWithConstraint_ConstraintFails()
        {
            var source = """
using System.Collections.Generic;

class C
{
    static void M<TEnumerable, TElement>(TEnumerable t, TElement e) where TEnumerable : IEnumerable<TElement>
    {
    }

    static void Main()
    {
        M(new List<int>(), "hello");
    }
}
""";

            CreateCompilation(source, parseOptions: TestOptions.Regular15).VerifyDiagnostics(
                // (11,9): error CS0311: The type 'System.Collections.Generic.List<int>' cannot be used as type parameter 'TEnumerable' in the generic type or method 'C.M<TEnumerable, TElement>(TEnumerable, TElement)'. There is no implicit reference conversion from 'System.Collections.Generic.List<int>' to 'System.Collections.Generic.IEnumerable<string>'.
                //         M(new List<int>(), "hello");
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M").WithArguments("C.M<TEnumerable, TElement>(TEnumerable, TElement)", "System.Collections.Generic.IEnumerable<string>", "TEnumerable", "System.Collections.Generic.List<int>").WithLocation(11, 9)
                );

            var expected =
                // (11,9): error CS0411: The type arguments for method 'C.M<TEnumerable, TElement>(TEnumerable, TElement)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M(new List<int>(), "hello");
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("C.M<TEnumerable, TElement>(TEnumerable, TElement)").WithLocation(11, 9);

            // Would constraint violation error like in previous version (see above) be better in this case?
            CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expected);
            CreateCompilation(source).VerifyDiagnostics(expected);
        }

        [Fact]
        public void SelfReferentialConstraint_DoesNotLoop()
        {
            // where T : IComparable<T> is satisfied; the constraint lower-bound inference is a
            // no-op (T is already fixed), so this must terminate and succeed.
            var source = """
using System;

class C
{
    static void M<T>(T t) where T : IComparable<T>
    {
    }

    static void Main()
    {
        M(5);
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var method = GetInferredMethod(comp);
            Assert.Equal("void C.M<System.Int32>(System.Int32 t)", method.ToTestDisplayString());
        }

        [Fact]
        public void ConstraintReferencesContainingTypeParameter()
        {
            // The constraint mentions both the containing type's type parameter (T, already bound
            // via the constructed receiver C<string>) and a method type parameter (V, inferred).
            var source = """
using System.Collections.Generic;

class C<T>
{
    public void M<U, V>(U u) where U : IDictionary<T, V>
    {
    }
}

class D
{
    static void Test(C<string> c)
    {
        c.M(new Dictionary<string, int>());
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var method = GetInferredMethod(comp);
            Assert.Equal(
                "void C<System.String>.M<System.Collections.Generic.Dictionary<System.String, System.Int32>, System.Int32>(System.Collections.Generic.Dictionary<System.String, System.Int32> u)",
                method.ToTestDisplayString());
        }

        [Fact]
        public void OverloadResolution_GenericWithConstraintBeatsObject_WithLambda()
        {
            // With the feature, the generic overload becomes applicable (and is more specific than the object overload).
            var source = """
using System;
using System.Collections.Generic;

class C
{
    static void M(object o, Action<object> a) => Console.WriteLine("object");
    static void M<TEnumerable, TElement>(TEnumerable t, Action<TElement> a)
        where TEnumerable : IEnumerable<TElement> => Console.WriteLine("generic");

    static void Main()
    {
        M(new List<int>(), x => { });
    }
}
""";

            CompileAndVerify(
                CreateCompilation(source, parseOptions: TestOptions.Regular15, options: TestOptions.DebugExe),
                expectedOutput: "object").VerifyDiagnostics();

            CompileAndVerify(
                CreateCompilation(source, parseOptions: TestOptions.RegularNext, options: TestOptions.DebugExe),
                expectedOutput: "generic").VerifyDiagnostics();

            CompileAndVerify(
                CreateCompilation(source, options: TestOptions.DebugExe),
                expectedOutput: "generic").VerifyDiagnostics();
        }

        [Fact]
        public void NonGenericPreferredOverConstraintInferredGeneric()
        {
            // With the feature the generic overload becomes applicable (T=List<int>, E=int), but the
            // more specific non-generic overload is still preferred. This verifies that promoting the
            // generic into the candidate set does not change selection of the better candidate.
            var source = """
using System;
using System.Collections.Generic;

class C
{
    static void M(List<int> l) => Console.WriteLine("specific");
    static void M<T, E>(T t) where T : IEnumerable<E> => Console.WriteLine("generic");

    static void Main()
    {
        M(new List<int>());
    }
}
""";

            CompileAndVerify(CreateCompilation(source, options: TestOptions.DebugExe), expectedOutput: "specific").VerifyDiagnostics();
        }

        [Fact]
        public void GenericArgument_ConstraintElementInferred()
        {
            // The element type itself is a generic type referencing another inferred argument.
            var source = """
using System.Collections.Generic;

class C
{
    static void M<TEnumerable, TElement>(TEnumerable t) where TEnumerable : IEnumerable<KeyValuePair<string, TElement>>
    {
    }

    static void Main()
    {
        M(new List<KeyValuePair<string, int>>());
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var method = GetInferredMethod(comp);
            Assert.Equal("System.Int32", method.TypeArguments[1].ToTestDisplayString());
        }

        [Fact]
        public void InferenceIsOneDirectional_ConstrainedParameterNotInferred()
        {
            // The feature flows bounds FROM a fixed constrained parameter (TEnumerable) INTO the
            // type parameters that appear in its constraint (TElement) -- never the other way around.
            // Here TElement is inferable from the argument, but TEnumerable has no bound of its own,
            // so it can never be fixed and inference fails even though the constraint mentions TElement.
            var source = """
using System.Collections.Generic;

class C
{
    static void M<TEnumerable, TElement>(TElement e) where TEnumerable : IEnumerable<TElement>
    {
    }

    static void Main()
    {
        M(42);
    }
}
""";

            CreateCompilation(source).VerifyDiagnostics(
                // (11,9): error CS0411: The type arguments for method 'C.M<TEnumerable, TElement>(TElement)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M(42);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("C.M<TEnumerable, TElement>(TElement)").WithLocation(11, 9));
        }

        [Fact]
        public void MultipleConstraintBoundsMergeToWeakerType()
        {
            // TElement is not inferable from any argument directly (without the feature this fails);
            // it is inferred only via the constraints. The first constraint contributes 'string'
            // (List<string> : IEnumerable<string>) and the second contributes 'object'. Merging the
            // two lower bounds yields the common base 'object', so TElement is inferred as the weaker
            // 'object' even though the user passed a List<string>.
            var source = """
using System.Collections.Generic;

class C
{
    static void M<TA, TB, TElement>(TA a, TB b)
        where TA : IEnumerable<TElement>
        where TB : IEnumerable<TElement>
    {
    }

    static void Main()
    {
        M(new List<string>(), new List<object>());
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var method = GetInferredMethod(comp);
            AssertEx.Equal("System.Collections.Generic.List<System.String>", method.TypeArguments[0].ToTestDisplayString());
            AssertEx.Equal("System.Collections.Generic.List<System.Object>", method.TypeArguments[1].ToTestDisplayString());
            AssertEx.Equal("System.Object", method.TypeArguments[2].ToTestDisplayString());

            // Without the feature TElement is not inferable at all (it only appears in constraints).
            CreateCompilation(source, parseOptions: TestOptions.Regular15).VerifyDiagnostics(
                // (13,9): error CS0411: The type arguments for method 'C.M<TA, TB, TElement>(TA, TB)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M(new List<string>(), new List<object>());
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("C.M<TA, TB, TElement>(TA, TB)").WithLocation(13, 9));
        }

        [Fact]
        public void ArrayImplementingIEnumerable()
        {
            // Arrays implement IEnumerable<T>; the element type is inferred from the constraint.
            var source = """
using System.Collections.Generic;

class C
{
    static void M<TEnumerable, TElement>(TEnumerable t) where TEnumerable : IEnumerable<TElement>
    {
    }

    static void Main()
    {
        M(new int[0]);
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var method = GetInferredMethod(comp);
            Assert.Equal("void C.M<System.Int32[], System.Int32>(System.Int32[] t)", method.ToTestDisplayString());
        }

        [Fact]
        public void InferredTypeViolatesPrimaryConstraint()
        {
            // Inference (from the constraint) succeeds and fixes TElement to 'int', but the separate
            // primary constraint 'where TElement : class' is then checked and is not satisfied.
            var source = """
using System.Collections.Generic;

class C
{
    static void M<TEnumerable, TElement>(TEnumerable t)
        where TEnumerable : IEnumerable<TElement>
        where TElement : class
    {
    }

    static void Main()
    {
        M(new List<int>());
    }
}
""";

            CreateCompilation(source).VerifyDiagnostics(
                // (13,9): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'TElement' in the generic type or method 'C.M<TEnumerable, TElement>(TEnumerable)'
                //         M(new List<int>());
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "M").WithArguments("C.M<TEnumerable, TElement>(TEnumerable)", "TElement", "int").WithLocation(13, 9)
                );

            // Without the feature TElement is not inferred at all, so the user sees a different
            // (arguably clearer) error: CS0411 rather than the primary-constraint violation.
            CreateCompilation(source, parseOptions: TestOptions.Regular15).VerifyDiagnostics(
                // (13,9): error CS0411: The type arguments for method 'C.M<TEnumerable, TElement>(TEnumerable)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M(new List<int>());
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("C.M<TEnumerable, TElement>(TEnumerable)").WithLocation(13, 9));
        }

        [Fact]
        public void CyclicConstraintDependence_TerminatesAndReportsConstraintErrors()
        {
            // The feature makes T depend on U and U depend on T (each occurs in the other's
            // constraint), forming a dependency cycle. Inference must still terminate; both are
            // fixed from their arguments and the (unsatisfiable) constraints are reported normally.
            var source = """
using System.Collections.Generic;

class C
{
    static void M<T, U>(T t, U u) where T : IEnumerable<U> where U : IEnumerable<T>
    {
    }

    static void Main()
    {
        M(1, 2);
    }
}
""";

            CreateCompilation(source).VerifyDiagnostics(
                // (11,9): error CS0315: The type 'int' cannot be used as type parameter 'T' in the generic type or method 'C.M<T, U>(T, U)'. There is no boxing conversion from 'int' to 'System.Collections.Generic.IEnumerable<int>'.
                //         M(1, 2);
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "M").WithArguments("C.M<T, U>(T, U)", "System.Collections.Generic.IEnumerable<int>", "T", "int").WithLocation(11, 9),
                // (11,9): error CS0315: The type 'int' cannot be used as type parameter 'U' in the generic type or method 'C.M<T, U>(T, U)'. There is no boxing conversion from 'int' to 'System.Collections.Generic.IEnumerable<int>'.
                //         M(1, 2);
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "M").WithArguments("C.M<T, U>(T, U)", "System.Collections.Generic.IEnumerable<int>", "U", "int").WithLocation(11, 9));
        }

        [Fact]
        public void CyclicConstraints_WithOrdinaryBounds_DoNotDependOnTypeParameterOrder()
        {
            var source = """
interface I<T> { }
interface J<T> { }
class A : I<B1> { }
class B1 : J<A> { }
class B2 : B1 { }

class Program
{
    static void M1<T, U>(T t, U u) where T : I<U> where U : J<T> { }
    static void M2<U, T>(U u, T t) where T : I<U> where U : J<T> { }

    static void Main()
    {
        M1(new A(), new B2());
        M2(new B2(), new A());
    }
}
""";

            var expected = new[]
            {
                // (14,9): error CS0311: The type 'A' cannot be used as type parameter 'T' in the generic type or method 'Program.M1<T, U>(T, U)'. There is no implicit reference conversion from 'A' to 'I<B2>'.
                //         M1(new A(), new B2());
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M1").WithArguments("Program.M1<T, U>(T, U)", "I<B2>", "T", "A").WithLocation(14, 9),
                // (15,9): error CS0311: The type 'A' cannot be used as type parameter 'T' in the generic type or method 'Program.M2<U, T>(U, T)'. There is no implicit reference conversion from 'A' to 'I<B2>'.
                //         M2(new B2(), new A());
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M2").WithArguments("Program.M2<U, T>(U, T)", "I<B2>", "T", "A").WithLocation(15, 9)
            };

            CreateCompilation(source, parseOptions: TestOptions.Regular15).VerifyDiagnostics(expected);
            CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expected);
            CreateCompilation(source).VerifyDiagnostics(expected);
        }

        [Fact]
        public void CyclicConstraints_WithConstraintBounds_DoNotDependOnTypeParameterOrder()
        {
            var source = """
interface P<out T, out U> { }
interface I<T> { }
interface J<T> { }
class A : I<B1> { }
class B1 : J<A> { }
class B2 : B1 { }
class Seed : P<A, B2> { }

class Program
{
    static void M1<S, T, U>(S s) where S : P<T, U> where T : I<U> where U : J<T> { }
    static void M2<S, U, T>(S s) where S : P<T, U> where T : I<U> where U : J<T> { }

    static void Main()
    {
        M1(new Seed());
        M2(new Seed());
    }
}
""";

            CreateCompilation(source, parseOptions: TestOptions.Regular15).VerifyDiagnostics(
                // (16,9): error CS0411: The type arguments for method 'Program.M1<S, T, U>(S)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M1(new Seed());
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M1").WithArguments("Program.M1<S, T, U>(S)").WithLocation(16, 9),
                // (17,9): error CS0411: The type arguments for method 'Program.M2<S, U, T>(S)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M2(new Seed());
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M2").WithArguments("Program.M2<S, U, T>(S)").WithLocation(17, 9));

            var expected = new[]
            {
                // (16,9): error CS0311: The type 'A' cannot be used as type parameter 'T' in the generic type or method 'Program.M1<S, T, U>(S)'. There is no implicit reference conversion from 'A' to 'I<B2>'.
                //         M1(new Seed());
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M1").WithArguments("Program.M1<S, T, U>(S)", "I<B2>", "T", "A").WithLocation(16, 9),
                // (17,9): error CS0311: The type 'A' cannot be used as type parameter 'T' in the generic type or method 'Program.M2<S, U, T>(S)'. There is no implicit reference conversion from 'A' to 'I<B2>'.
                //         M2(new Seed());
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M2").WithArguments("Program.M2<S, U, T>(S)", "I<B2>", "T", "A").WithLocation(17, 9)
            };

            CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expected);
            CreateCompilation(source).VerifyDiagnostics(expected);
        }

        [Fact]
        public void MultiLevelEnumeratorPattern_InfersThroughConstraintChain()
        {
            // The allocation-free "generic struct enumerator" LINQ pattern: the enumerator type is
            // itself a type parameter, resolved through a two-level constraint chain. From just the
            // receiver (MyList) the feature infers TEnumerable=MyList, then via its constraint
            // TEnumerator=MyEnumerator and T=int, and TEnumerator's own (self-referential) constraint
            // confirms T. All three are inferred with no explicit type arguments.
            var source = """
interface IEnumerator2<TSelf, T> where TSelf : IEnumerator2<TSelf, T>
{
    T Current { get; }
    bool MoveNext();
}

interface IEnumerable2<TEnumerator, T> where TEnumerator : IEnumerator2<TEnumerator, T>
{
    TEnumerator GetEnumerator();
}

struct MyEnumerator : IEnumerator2<MyEnumerator, int>
{
    public int Current => 0;
    public bool MoveNext() => false;
}

class MyList : IEnumerable2<MyEnumerator, int>
{
    public MyEnumerator GetEnumerator() => default;
}

static class Ext
{
    public static void Where<TEnumerable, TEnumerator, T>(this TEnumerable e)
        where TEnumerable : IEnumerable2<TEnumerator, T>
        where TEnumerator : IEnumerator2<TEnumerator, T>
    { }
}

class C
{
    static void Main()
    {
        new MyList().Where();
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var inv = comp.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
            var m = Assert.IsAssignableFrom<IMethodSymbol>(comp.GetSemanticModel(comp.SyntaxTrees.Single()).GetSymbolInfo(inv).Symbol);
            Assert.Equal("MyList", m.TypeArguments[0].ToTestDisplayString());
            Assert.Equal("MyEnumerator", m.TypeArguments[1].ToTestDisplayString());
            Assert.Equal("System.Int32", m.TypeArguments[2].ToTestDisplayString());

            // Without the feature none of the constraint-only parameters can be inferred.
            CreateCompilation(source, parseOptions: TestOptions.Regular15).VerifyDiagnostics(
                // (35,22): error CS0411: The type arguments for method 'Ext.Where<TEnumerable, TEnumerator, T>(TEnumerable)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         new MyList().Where();
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Where").WithArguments("Ext.Where<TEnumerable, TEnumerator, T>(TEnumerable)").WithLocation(35, 22));
        }

        [Fact]
        public void AsyncLambda_TaskReturnDrivesConstrainedTaskParameter()
        {
            // The async lambda's inferred return type (Task<int>) fixes the constrained parameter
            // TTask = Task<int>; the constraint 'where TTask : Task<TResult>' then infers TResult = int.
            var source = """
using System.Threading.Tasks;

class C
{
    static void M<TTask, TResult>(System.Func<TTask> f) where TTask : Task<TResult>
    {
    }

    static void Main()
    {
        M(async () => 42);
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var m = GetInferredMethod(comp);
            Assert.Equal("System.Threading.Tasks.Task<System.Int32>", m.TypeArguments[0].ToTestDisplayString());
            Assert.Equal("System.Int32", m.TypeArguments[1].ToTestDisplayString());

            // Without the feature TResult only appears in the constraint and cannot be inferred.
            CreateCompilation(source, parseOptions: TestOptions.Regular15).VerifyDiagnostics(
                // (11,9): error CS0411: The type arguments for method 'C.M<TTask, TResult>(Func<TTask>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M(async () => 42);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("C.M<TTask, TResult>(System.Func<TTask>)").WithLocation(11, 9));
        }

        [Fact]
        public void TaskAsClassConstraint_InfersResult()
        {
            // Task<T> (a sealed-ish library class) used as a naked class constraint: passing a
            // Task<int> argument fixes TTask = Task<int>, and the constraint infers TResult = int.
            var source = """
using System.Threading.Tasks;

class C
{
    static void M<TTask, TResult>(TTask t) where TTask : Task<TResult>
    {
    }

    static void Main()
    {
        M(Task.FromResult(42));
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var m = GetInferredMethod(comp);
            Assert.Equal("System.Threading.Tasks.Task<System.Int32>", m.TypeArguments[0].ToTestDisplayString());
            Assert.Equal("System.Int32", m.TypeArguments[1].ToTestDisplayString());
        }

        [Fact]
        public void AsyncLambda_InputTypeFromConstraint()
        {
            // TElement is inferred from the constraint (List<int> : IEnumerable<int>), and that
            // inferred type then flows into the async lambda's parameter 'x' (typed as TElement),
            // letting 'await Task.Delay(x)' bind with x : int.
            var source = """
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    static void M<TEnumerable, TElement>(TEnumerable t, Func<TElement, Task> f)
        where TEnumerable : IEnumerable<TElement>
    {
    }

    static void Main()
    {
        M(new List<int>(), async x => { await Task.Delay(x); });
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var m = GetInferredMethod(comp);
            Assert.Equal("System.Collections.Generic.List<System.Int32>", m.TypeArguments[0].ToTestDisplayString());
            Assert.Equal("System.Int32", m.TypeArguments[1].ToTestDisplayString());
        }

        [Fact]
        public void AsyncLambda_ReturnConflictsWithConstraint_01()
        {
            // The async lambda's Task<int> return fixes TElement as int. Constraint inference does
            // not replace that ordinary bound.
            var source = """
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    static void M<TEnumerable, TElement>(TEnumerable t, Func<Task<TElement>> f)
        where TEnumerable : IEnumerable<TElement>
    {
    }

    static void Main()
    {
        M(new List<object>(), async () => 42);
    }
}
""";

            var comp = CreateCompilation(source);

            // Would constraint violation error like in previous version (see below) be better in this case?
            comp.VerifyDiagnostics(
                // (14,9): error CS0411: The type arguments for method 'C.M<TEnumerable, TElement>(TEnumerable, Func<Task<TElement>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M(new List<object>(), async () => 42);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("C.M<TEnumerable, TElement>(TEnumerable, System.Func<System.Threading.Tasks.Task<TElement>>)").WithLocation(14, 9)
                );

            CreateCompilation(source, parseOptions: TestOptions.Regular15).VerifyDiagnostics(
                // (14,9): error CS0311: The type 'System.Collections.Generic.List<object>' cannot be used as type parameter 'TEnumerable' in the generic type or method 'C.M<TEnumerable, TElement>(TEnumerable, Func<Task<TElement>>)'. There is no implicit reference conversion from 'System.Collections.Generic.List<object>' to 'System.Collections.Generic.IEnumerable<int>'.
                //         M(new List<object>(), async () => 42);
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M").WithArguments("C.M<TEnumerable, TElement>(TEnumerable, System.Func<System.Threading.Tasks.Task<TElement>>)", "System.Collections.Generic.IEnumerable<int>", "TEnumerable", "System.Collections.Generic.List<object>").WithLocation(14, 9));
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/86423")]
        public void AsyncLambda_01()
        {
            var source = """
using System;
using System.Threading.Tasks;

class C
{
    static void M<TElement>(TElement t, Func<Task<TElement>> f)
    {
    }

    static void Main()
    {
        M(new object(), async () => 42);
    }
}
""";

            var expected =
                // (12,9): error CS0411: The type arguments for method 'C.M<TElement>(TElement, Func<Task<TElement>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M(new object(), async () => 42);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("C.M<TElement>(TElement, System.Func<System.Threading.Tasks.Task<TElement>>)").WithLocation(12, 9);

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(expected);

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular15);
            comp.VerifyDiagnostics(expected);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/86423")]
        public void AsyncLambda_02()
        {
            var source = """
using System;
using System.Threading.Tasks;

class C
{
    static void M<TElement>(Func<TElement> t, Func<Task<TElement>> f)
    {
    }

    static void Main()
    {
        M(() => new object(), async () => 42);
    }
}
""";

            var expected =
                // (12,9): error CS0411: The type arguments for method 'C.M<TElement>(Func<TElement>, Func<Task<TElement>>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         M(() => new object(), async () => 42);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("C.M<TElement>(System.Func<TElement>, System.Func<System.Threading.Tasks.Task<TElement>>)").WithLocation(12, 9);

            CreateCompilation(source).VerifyDiagnostics(expected);

            CreateCompilation(source, parseOptions: TestOptions.Regular15).VerifyDiagnostics(expected);
        }

        [Fact]
        public void AsyncLambda_ReturnConflictsWithConstraint_02()
        {
            var source = """
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    static void M<TEnumerable, TElement>(TEnumerable t, Func<Task<TElement>> f)
        where TEnumerable : IEnumerable<TElement>
    {
    }

    static void Main()
    {
        M(new List<string>(), async () => new object());
    }
}
""";

            var comp = CreateCompilation(source);

            comp.VerifyDiagnostics();

            var m = GetInferredMethod(comp);
            Assert.Equal("System.Collections.Generic.List<System.String>", m.TypeArguments[0].ToTestDisplayString());
            Assert.Equal("System.Object", m.TypeArguments[1].ToTestDisplayString());

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular15);
            comp.VerifyDiagnostics();

            m = GetInferredMethod(comp);
            Assert.Equal("System.Collections.Generic.List<System.String>", m.TypeArguments[0].ToTestDisplayString());
            Assert.Equal("System.Object", m.TypeArguments[1].ToTestDisplayString());
        }

        [Fact]
        public void Lambda_ReturnConflictsWithConstraint_01()
        {
            var source = """
using System;
using System.Collections.Generic;

class C
{
    static void M<TEnumerable, TElement>(TEnumerable t, Func<TElement> f)
        where TEnumerable : IEnumerable<TElement>
    {
    }

    static void Main()
    {
        M(new List<object>(), () => 42);
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var m = GetInferredMethod(comp);
            Assert.Equal("System.Collections.Generic.List<System.Object>", m.TypeArguments[0].ToTestDisplayString());
            Assert.Equal("System.Object", m.TypeArguments[1].ToTestDisplayString());

            CreateCompilation(source, parseOptions: TestOptions.Regular15).VerifyDiagnostics(
                // (13,9): error CS0311: The type 'System.Collections.Generic.List<object>' cannot be used as type parameter 'TEnumerable' in the generic type or method 'C.M<TEnumerable, TElement>(TEnumerable, Func<TElement>)'. There is no implicit reference conversion from 'System.Collections.Generic.List<object>' to 'System.Collections.Generic.IEnumerable<int>'.
                //         M(new List<object>(), () => 42);
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M").WithArguments("C.M<TEnumerable, TElement>(TEnumerable, System.Func<TElement>)", "System.Collections.Generic.IEnumerable<int>", "TEnumerable", "System.Collections.Generic.List<object>").WithLocation(13, 9));
        }

        [Fact]
        public void Lambda_Return_01()
        {
            var source = """
using System;

class C
{
    static void M<TElement>(TElement t, Func<TElement> f)
    {
    }

    static void Main()
    {
        M("", () => new object());
    }
}
""";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var m = GetInferredMethod(comp);
            Assert.Equal("System.Object", m.TypeArguments[0].ToTestDisplayString());

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular15);
            comp.VerifyDiagnostics();

            m = GetInferredMethod(comp);
            Assert.Equal("System.Object", m.TypeArguments[0].ToTestDisplayString());
        }

        [Fact]
        public void ConstraintsChecks_20() // Cloned from NullableReferenceTypesTests.ConstraintsChecks_20
        {
            var source =
@"
#nullable enable

class B<TB1> where TB1 : class, IB?
{
    public void M<TM1, TM2>(TM1 x, TM2 y) where TM2 : TM1
    {}

    public void Test2(TB1? a2, TB1 b2)
    {
        M(b2, a2); // 1
    }
}

public interface IB
{}
";

            var comp1 = CreateCompilation(source);
            comp1.VerifyDiagnostics();

            var m = GetInferredMethod(comp1);
            Assert.Equal("TB1?", m.TypeArguments[0].ToTestDisplayString(includeNonNullable: true));
            Assert.Equal("TB1?", m.TypeArguments[1].ToTestDisplayString(includeNonNullable: true));

            comp1 = CreateCompilation(source, parseOptions: TestOptions.Regular15);
            comp1.VerifyDiagnostics(
                // (11,9): warning CS8631: The type 'TB1?' cannot be used as type parameter 'TM2' in the generic type or method 'B<TB1>.M<TM1, TM2>(TM1, TM2)'. Nullability of type argument 'TB1?' doesn't match constraint type 'TB1'.
                //         M(b2, a2); // 1
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterConstraint, "M").WithArguments("B<TB1>.M<TM1, TM2>(TM1, TM2)", "TB1", "TM2", "TB1?").WithLocation(11, 9)
                );

            m = GetInferredMethod(comp1);
            Assert.Equal("TB1!", m.TypeArguments[0].ToTestDisplayString(includeNonNullable: true));
            Assert.Equal("TB1?", m.TypeArguments[1].ToTestDisplayString(includeNonNullable: true));
        }

        [Fact]
        public void ChangeAroundSelfDependencyTracking()
        {
            var source = """
using System;

M(
    t: 0,
    u: new B(),
    f: x => x,
    g: x => (I<A>)null,
    h: y => y);

void M<T,U>(
    T t,
    U u,
    Func<T,T> f,
    Func<T,I<U>> g,
    Func<U,U> h)
{
    Console.WriteLine(typeof(T)); // Prints "int"
    Console.WriteLine(typeof(U)); // Used to print "B", prints "A" after the change
}


interface I<in T> { }
class A { }
class B : A { }
""";

            string expected = """
System.Int32
A
""";

            CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.Regular15).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expected).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/86507")]
        public void DependencyTracking_01()
        {
            var source = """
using System;

M(1, (0, x => new object()), x => x.ToString());

static void M<T, U>(T t, (int, Func<T, U>) fs, Func<T, U> f)
{
    Console.WriteLine(typeof(T));
    Console.WriteLine(typeof(U));
}
""";

            string expected = """
System.Int32
System.Object
""";

            CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.Regular15).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expected).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/86507")]
        public void DependencyTracking_02()
        {
            var source = """
using System;

M(1, [x => new object()], x => x.ToString());

static void M<T, U>(T t, Func<T, U>[] fs, Func<T, U> f)
{
    Console.WriteLine(typeof(T));
    Console.WriteLine(typeof(U));
}
""";

            string expected = """
System.Int32
System.Object
""";

            CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.Regular15).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expected).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/86507")]
        public void DependencyTracking_03()
        {
            var source = """
using System;

M(1, (0, x => new object()), "");

static void M<T, U>(T t, (int, Func<T, U>) fs, U y)
{
    Console.WriteLine(typeof(T));
    Console.WriteLine(typeof(U));
}
""";

            string expected = """
System.Int32
System.Object
""";

            CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.Regular15).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expected).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/86507")]
        public void DependencyTracking_04()
        {
            var source = """
using System;

M(1, [x => new object()], "");

static void M<T, U>(T t, Func<T, U>[] fs, U y)
{
    Console.WriteLine(typeof(T));
    Console.WriteLine(typeof(U));
}
""";

            string expected = """
System.Int32
System.Object
""";

            CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.Regular15).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expected).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/86507")]
        public void DependencyTracking_05()
        {
            var source = """
using System;

M(1, (1, x => x.ToString()));

static void M<T, U>(T t, (int, Func<T, U>) fs)
{
    Console.WriteLine(typeof(T));
    Console.WriteLine(typeof(U));
}
""";

            string expected = """
System.Int32
System.String
""";

            CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.Regular15).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expected).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/86507")]
        public void DependencyTracking_06()
        {
            var source = """
using System;

M(1, [x => x.ToString()]);

static void M<T, U>(T t, Func<T, U>[] fs)
{
    Console.WriteLine(typeof(T));
    Console.WriteLine(typeof(U));
}
""";

            string expected = """
System.Int32
System.String
""";

            CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.Regular15).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expected).VerifyDiagnostics();
        }

        [Fact]
        public void AddMethod_Generic_01()
        {
            string source = """
using System.Collections;
class MyCollection<T> : IEnumerable
{
    IEnumerator IEnumerable.GetEnumerator() => throw null;
    public void Add<U, V>(U t) where U : System.IComparable<V>
    {
        System.Console.WriteLine(typeof(V));
    }
}
class Program
{
    static void Main()
    {
        int x = 1;
#line 100
        MyCollection<object> z = [x];
    }
}
""";

            CreateCompilation(source, parseOptions: TestOptions.Regular15).VerifyDiagnostics(
                // (100,34): error CS9215: Collection expression type 'MyCollection<object>' must have an instance or extension method 'Add' that can be called with a single argument.
                //         MyCollection<object> z = [x];
                Diagnostic(ErrorCode.ERR_CollectionExpressionMissingAdd, "[x]").WithArguments("MyCollection<object>").WithLocation(100, 34)
                );

            string expected = "System.Int32";
            CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expected).VerifyDiagnostics();
        }

        [Fact]
        public void AddMethod_Generic_02()
        {
            string source = """
using System.Collections;
public class MyCollection<T> : IEnumerable
{
    IEnumerator IEnumerable.GetEnumerator() => throw null;
}

public static class Ext
{
    public static void Add<U, V>(this MyCollection<object> c, U t) where U : System.IComparable<V>
    {
        System.Console.WriteLine(typeof(V));
    }
}

class Program
{
    static void Main()
    {
        int x = 1;
#line 100
        MyCollection<object> z = [x];
    }
}
""";

            CreateCompilation(source, parseOptions: TestOptions.Regular15).VerifyDiagnostics(
                // (100,34): error CS9215: Collection expression type 'MyCollection<object>' must have an instance or extension method 'Add' that can be called with a single argument.
                //         MyCollection<object> z = [x];
                Diagnostic(ErrorCode.ERR_CollectionExpressionMissingAdd, "[x]").WithArguments("MyCollection<object>").WithLocation(100, 34)
                );

            string expected = "System.Int32";
            CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expected).VerifyDiagnostics();
        }

        [Fact]
        public void AddMethod_Generic_03()
        {
            string source = """
using System.Collections;
public class MyCollection<T> : IEnumerable
{
    IEnumerator IEnumerable.GetEnumerator() => throw null;
}

public static class Ext
{
    extension(MyCollection<object> c)
    {
        public void Add<U, V>(U t) where U : System.IComparable<V>
        {
            System.Console.WriteLine(typeof(V));
        }
    }
}

class Program
{
    static void Main()
    {
        int x = 1;
#line 100
        MyCollection<object> z = [x];
    }
}
""";

            CreateCompilation(source, parseOptions: TestOptions.Regular15).VerifyDiagnostics(
                // (100,34): error CS9215: Collection expression type 'MyCollection<object>' must have an instance or extension method 'Add' that can be called with a single argument.
                //         MyCollection<object> z = [x];
                Diagnostic(ErrorCode.ERR_CollectionExpressionMissingAdd, "[x]").WithArguments("MyCollection<object>").WithLocation(100, 34)
                );

            string expected = "System.Int32";
            CompileAndVerify(source, expectedOutput: expected, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
            CompileAndVerify(source, expectedOutput: expected).VerifyDiagnostics();
        }
    }
}
