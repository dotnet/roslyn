// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Utils = Microsoft.CodeAnalysis.CSharp.UnitTests.CompilationUtils;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class GenericConstraintTests : CSharpTestBase
    {
        [ClrOnlyFact]
        public void LoadAndPersist()
        {
            var source =
@"class A<T> where T : struct { }
class B<T> where T : class { }
interface IA<T> { }
interface IB<T> where T : IA<T> { }
class C<T> where T : IB<T>, IA<T>, new() { }
class D<T> where T : A<int>, new() { }";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("A");
                CheckConstraints(type.TypeParameters[0], TypeParameterConstraintKind.ValueType, true, false, "ValueType", "ValueType");

                type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("B");
                CheckConstraints(type.TypeParameters[0], TypeParameterConstraintKind.ReferenceType, false, true, "object", "object");

                type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("IA");
                CheckConstraints(type.TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object");

                type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("IB");
                CheckConstraints(type.TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object", "IA<T>");

                type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                CheckConstraints(type.TypeParameters[0], TypeParameterConstraintKind.Constructor, false, false, "object", "object", "IB<T>", "IA<T>");

                type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("D");
                CheckConstraints(type.TypeParameters[0], TypeParameterConstraintKind.Constructor, false, true, "A<int>", "A<int>", "A<int>");
            };

            CompileAndVerify(
                source: source,
                sourceSymbolValidator: validator,
                symbolValidator: validator);
        }

        [ClrOnlyFact]
        public void OverriddenMethods()
        {
            var source =
@"class A<T>
{
    internal virtual void M<U>() where U : T { }
}
class B0<T> : A<T>
{
    internal override void M<U>() { }
}
class B1 : A<int>
{
    internal override void M<U>() { }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("A");
                var method = type.GetMember<MethodSymbol>("M");
                CheckConstraints(method.TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object", "T");

                type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("B0");
                method = type.GetMember<MethodSymbol>("M");
                CheckConstraints(method.TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object", "T");

                type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("B1");
                method = type.GetMember<MethodSymbol>("M");
                CheckConstraints(method.TypeParameters[0], TypeParameterConstraintKind.None, true, false, "ValueType", "int", "int");
            };

            CompileAndVerify(
                source: source,
                sourceSymbolValidator: validator,
                symbolValidator: validator);
        }

        [ClrOnlyFact]
        public void ExplicitInterfaceMethods()
        {
            var source =
@"interface I<T, U>
{
    void M<V>() where V : T, U;
}
class C : I<C, object>
{
    void I<C, object>.M<V>() { }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var method = type.GetMethod("I<C,System.Object>.M");
                CheckConstraints(method.TypeParameters[0], TypeParameterConstraintKind.None, false, true, "C", "C", "C");
            };

            CompileAndVerify(
                source: source,
                sourceSymbolValidator: validator,
                symbolValidator: validator);
        }

        /// <summary>
        /// SourceMemberMethodSymbol binds parameters and type parameters
        /// of partial methods early - in the constructor. Ensure constraints for
        /// overridden methods are handled in these cases.
        /// </summary>
        [ClrOnlyFact]
        public void PartialClassOverriddenMethods()
        {
            var source =
@"interface I<T> { }
abstract partial class A<T>
{
    internal abstract void M1<U>(T t) where U : T;
}
abstract partial class A<T>
{
    internal abstract void M2<U>(U u) where U : I<T>;
}
partial class B<T> : A<T>
{
    internal override void M1<U>(T t) { }
}
partial class B<T> : A<T>
{
    internal override void M2<U>(U u) { }
}";

            Action<ModuleSymbol> validator = module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("A");
                var method = type.GetMember<MethodSymbol>("M1");
                CheckConstraints(method.TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object", "T");
                Utils.CheckSymbol(method, "void A<T>.M1<U>(T t)");

                method = type.GetMember<MethodSymbol>("M2");
                CheckConstraints(method.TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object", "I<T>");
                Utils.CheckSymbol(method, "void A<T>.M2<U>(U u)");

                type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("B");
                method = type.GetMember<MethodSymbol>("M1");
                CheckConstraints(method.TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object", "T");
                Utils.CheckSymbol(method, "void B<T>.M1<U>(T t)");
                Utils.CheckSymbol(method.OverriddenMethod, "void A<T>.M1<U>(T t)");

                method = type.GetMember<MethodSymbol>("M2");
                CheckConstraints(method.TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object", "I<T>");
                Utils.CheckSymbol(method, "void B<T>.M2<U>(U u)");
                Utils.CheckSymbol(method.OverriddenMethod, "void A<T>.M2<U>(U u)");
            };

            CompileAndVerify(
                source: source,
                sourceSymbolValidator: validator,
                symbolValidator: validator);
        }

        [ClrOnlyFact]
        public void ConstraintWithTypeParameter()
        {
            var source =
@"interface I<T> { }
struct S<T> where T : I<T>
{
    void M<U, V>()
        where U : V
        where V : I<U>
    {
    }
}
delegate void D<T>() where T : I<T>;";
            CompileAndVerify(source);
        }

        [ClrOnlyFact]
        public void ConstraintWithContainingType()
        {
            var source =
@"interface IA<T> { }
class C<T> where T : IA<C<T>> { }
interface IB<T> where T : IB<T> { }";
            CompileAndVerify(source);
        }

        [ClrOnlyFact]
        public void ConstraintWithSameType()
        {
            var source =
@"interface I<T> where T : I<T> { }
class C<T, U> where T : C<T, U> { }";
            CompileAndVerify(source);
        }

        [ClrOnlyFact]
        public void BaseWithSameType()
        {
            var source =
@"interface IA<T> { }
interface IB<T> : IA<IB<T>> where T : IA<T> { }
class A<T> { }
class B<T> : A<B<T>> where T : A<T> { }";
            CompileAndVerify(source);
        }

        [Fact]
        public void ConstraintWithNestedInterfaceTypeArgument()
        {
            var source =
@"interface A1<T> where T : A2 { }
interface A2 { }
class B1 : A1<B1.B2> // valid
{
    internal interface B2 : A2 { }
}
class C1 : A1<C1.C2> // invalid
{
    internal interface C2 { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,7): error CS0311: The type 'C1.C2' cannot be used as type parameter 'T' in the generic type or method 'A1<T>'. There is no implicit reference conversion from 'C1.C2' to 'A2'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "C1").WithArguments("A1<T>", "A2", "T", "C1.C2").WithLocation(7, 7));
        }

        [Fact]
        public void ConstraintWithNestedClassTypeArgument()
        {
            var source =
@"abstract class A1<T> where T : A1<T>.A2
{
    internal class A2 { }
}
class B1 : A1<B1.B2> // valid
{
    internal class B2 : A2 { }
}
class C1 : A1<C1.C2> // invalid
{
    internal class C2 { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,7): error CS0311: The type 'C1.C2' cannot be used as type parameter 'T' in the generic type or method 'A1<T>'. There is no implicit reference conversion from 'C1.C2' to 'A1<C1.C2>.A2'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "C1").WithArguments("A1<T>", "A1<C1.C2>.A2", "T", "C1.C2").WithLocation(9, 7));
        }

        [WorkItem(542616, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542616")]
        [Fact]
        public void NewConstraintWithPrivateConstructorErr()
        {
            var metadatasrc =
@"public class PrivateCtorClass
{
    private PrivateCtorClass() { }
}";
            var source =
@"public class Test
{
    public static int Main()
    {
        var g1 = new Gen<PrivateCtorClass>(); // CS0310
        return 0;
    }
}

public class Gen<T> where T : new() { public T t;}
";
            var comp1 = CreateCompilation(metadatasrc);
            var comp2 = CreateCompilation(source, new MetadataReference[] { comp1.EmitToImageReference() });

            comp2.VerifyDiagnostics(
                // (5,26): error CS0310: 'PrivateCtorClass' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'Gen<T>'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "PrivateCtorClass").WithArguments("Gen<T>", "T", "PrivateCtorClass").WithLocation(5, 26));
        }

        [WorkItem(542617, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542617")]
        [Fact]
        public void InterfaceConstraintWithClassTypeArgumentErr()
        {
            var source =
@"public interface InterfaceConstraint { }
public class ViolateInterfaceConstraint { }
 
public class Gen<T> where T : InterfaceConstraint
{
    public void Meth(Gen<ViolateInterfaceConstraint>.Nested[] Param) {} // CS0311

    public class Nested { }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,63): error CS0311: The type 'ViolateInterfaceConstraint' cannot be used as type parameter 'T' in the generic type or method 'Gen<T>'. 
                //                       There is no implicit reference conversion from 'ViolateInterfaceConstraint' to 'InterfaceConstraint'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "Param").
                    WithArguments("Gen<T>", "InterfaceConstraint", "T", "ViolateInterfaceConstraint").WithLocation(6, 63));
        }

        [WorkItem(542617, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542617")]
        [Fact]
        public void NestedViolationsInvolvingArraysAndPointers()
        {
            var source =
@"class A<T> where T : struct
{
    internal class B1 { }
    private class B2 { }
}
unsafe interface I
{
    void M1(A<A<int>*>.B1 o);
    void M2(A<string>.B1** o);
    void M3(A<A<int>.B2>.B1* o);
    void M4(A<A<int>[]>.B1 o);
    void M5(A<string>.B1[][] o);
    void M6(A<A<int>.B2>.B1[] o);
    void M7(A<A<int>.B1[]>.B1 o);
}";
            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,15): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('A<int>')
                Diagnostic(ErrorCode.ERR_ManagedAddr, "A<int>*").WithArguments("A<int>").WithLocation(8, 15),
                // (9,13): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('A<string>.B1')
                Diagnostic(ErrorCode.ERR_ManagedAddr, "A<string>.B1*").WithArguments("A<string>.B1").WithLocation(9, 13),
                // (10,22): error CS0122: 'A<int>.B2' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "B2").WithArguments("A<int>.B2").WithLocation(10, 22),
                // (10,13): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('A<A<int>.B2>.B1')
                Diagnostic(ErrorCode.ERR_ManagedAddr, "A<A<int>.B2>.B1*").WithArguments("A<A<int>.B2>.B1").WithLocation(10, 13),
                // (13,22): error CS0122: 'A<int>.B2' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "B2").WithArguments("A<int>.B2").WithLocation(13, 22),
                // (8,27): error CS0306: The type 'A<int>*' may not be used as a type argument
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "o").WithArguments("A<int>*").WithLocation(8, 27),
                // (9,28): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "o").WithArguments("A<T>", "T", "string").WithLocation(9, 28),
                // (11,28): error CS0453: The type 'A<int>[]' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "o").WithArguments("A<T>", "T", "A<int>[]").WithLocation(11, 28),
                // (12,30): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "o").WithArguments("A<T>", "T", "string").WithLocation(12, 30),
                // (14,31): error CS0453: The type 'A<int>.B1[]' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "o").WithArguments("A<T>", "T", "A<int>.B1[]").WithLocation(14, 31));
        }

        [WorkItem(542618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542618")]
        [Fact]
        public void AllowReferenceTypeVolatileField()
        {
            var source =
@"public interface I {}
public class C : I{}

class G<T> where T : C
{
    public volatile T Fld = default(T);
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }

        /// <summary>
        /// Implicit implementations must specify constraints.
        /// </summary>
        [ClrOnlyFact]
        public void ImplicitImplementation()
        {
            var source =
@"interface I<T>
{
    void M1<U>() where U : T;
    void M2<U>() where U : struct, T;
    void M3<U>() where U : I<T>;
}
class A<T> : I<T>
{
    public void M1<U>() where U : T { }
    public void M2<U>() where U : struct, T { }
    public void M3<U>() where U : I<T> { }
}
class B : I<object>
{
    public void M1<T>() { }
    public void M2<T>() where T : struct { }
    public void M3<T>() where T : I<object> { }
}";
            // TODO: Verify constraints for implementations are emitted correctly.
            CompileAndVerify(source);
        }

        /// <summary>
        /// Explicit implementations do not specify constraints.
        /// </summary>
        [ClrOnlyFact]
        public void ExplicitImplementation()
        {
            var source =
@"interface I<T>
{
    void M1<U>() where U : T;
    void M2<U>() where U : class, T;
    void M3<U>() where U : I<U>;
    void M4<U, V>() where U : V;
}
class A<T> : I<T>
{
    void I<T>.M1<U>() { }
    void I<T>.M2<U>() { }
    void I<T>.M3<U>() { }
    void I<T>.M4<U, V>() { }
}
class B : I<string>
{
    void I<string>.M1<U>() { }
    void I<string>.M2<U>() { }
    void I<string>.M3<U>() { }
    void I<string>.M4<U, V>() { }
}
class C : I<object>
{
    void I<object>.M1<T>() { }
    void I<object>.M2<T>() { }
    void I<object>.M3<T>() { }
    void I<object>.M4<T, U>() { }
}";
            // TODO: Verify constraints for implementations are emitted correctly.
            CompileAndVerify(source);
        }

        /// <summary>
        /// Dev10 reports constraint violations at every reference to an
        /// interface type, including in explicit member declarations.
        /// </summary>
        [Fact]
        public void ExplicitImplementationInterfaceConstraintViolations()
        {
            var source =
@"interface I<T, U>
    where T : class
    where U : struct
{
    void M<V>();
    object P { get; set; }
}
class C : I<int, object>
{
    void I<int, object>.M<V>() { }
    object I<int, object>.P { get; set; }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,7): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'I<T, U>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "C").WithArguments("I<T, U>", "T", "int").WithLocation(8, 7),
                // (8,7): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'U' in the generic type or method 'I<T, U>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "C").WithArguments("I<T, U>", "U", "object").WithLocation(8, 7),
                // (10,10): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'I<T, U>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "I<int, object>").WithArguments("I<T, U>", "T", "int").WithLocation(10, 10),
                // (10,10): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'U' in the generic type or method 'I<T, U>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "I<int, object>").WithArguments("I<T, U>", "U", "object").WithLocation(10, 10),
                // (11,12): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'I<T, U>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "I<int, object>").WithArguments("I<T, U>", "T", "int").WithLocation(11, 12),
                // (11,12): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'U' in the generic type or method 'I<T, U>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "I<int, object>").WithArguments("I<T, U>", "U", "object").WithLocation(11, 12));
        }

        /// <summary>
        /// Similar to ExplicitImplementationInterfaceConstraintViolations but
        /// where the constraint violation involves a reference to the containing type.
        /// </summary>
        [WorkItem(542948, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542948")]
        [Fact]
        public void ExplicitImplementationInterfaceConstraintViolationsOnContainer()
        {
            var source =
@"delegate void D();
class A
{
    internal interface I<T> where T : new()
    {
        void M();
        object P { get; }
        object this[object index] { get; }
        event D E;
    }
}
abstract class B : A.I<B>
{
    void A.I<B>.M() { }
    object A.I<B>.P { get { return null; } }
    object A.I<B>.this[object index] { get { return null; } }
    event D A.I<B>.E { add { } remove { } }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (12,16): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'A.I<T>'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "B").WithArguments("A.I<T>", "T", "B").WithLocation(12, 16),
                // (14,10): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'A.I<T>'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "A.I<B>").WithArguments("A.I<T>", "T", "B").WithLocation(14, 10),
                // (15,12): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'A.I<T>'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "A.I<B>").WithArguments("A.I<T>", "T", "B").WithLocation(15, 12),
                // (16,12): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'A.I<T>'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "A.I<B>").WithArguments("A.I<T>", "T", "B").WithLocation(16, 12),
                // (17,13): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'A.I<T>'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "A.I<B>").WithArguments("A.I<T>", "T", "B").WithLocation(17, 13));
        }

        /// <summary>
        /// Ensure generic methods are handled in an explicit
        /// implementation where the interface method does not exist.
        /// </summary>
        [Fact]
        public void ExplicitImplementationNoSuchMethod()
        {
            var source =
@"interface I<T> where T : class
{
    void M1<U>();
}
class C : I<string>
{
    void I<string>.M2<U>() { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,20): error CS0539: 'C.M2<U>()' in explicit interface declaration is not a member of interface
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "M2").WithArguments("C.M2<U>()"),
                // (5,11): error CS0535: 'C' does not implement interface member 'I<string>.M1<U>()'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I<string>").WithArguments("C", "I<string>.M1<U>()"));
        }

        /// <summary>
        /// Constraints on base types, interfaces, and method parameters
        /// and return types are all checked outside of BindType to avoid
        /// cycles. Verify that constraints are checked in those cases.
        /// </summary>
        [Fact]
        public void BasesInterfacesParametersAndReturnTypes()
        {
            var source =
@"interface I<T> where T : class { }
class A<T> where T : class
{
    internal interface I { }
    internal class C { }
}
class B
{
    internal interface I<U> where U : struct { }
    internal class C<U> where U : struct { }
}
// Simple type: A<T>, etc.
abstract class C1<T> : A<T>, I<T>
{
    internal abstract A<U> F<U>(I<U> a);
}
// Outer type: A<T>.C, etc.
abstract class C2<T> : A<T>.C, A<T>.I
{
    internal abstract A<U>.I F<U>(A<U>.C a);
}
// Inner type: B.C<T>, etc.
abstract class C3<T> : B.C<T>, B.I<T>
{
    internal abstract B.C<U> F<U>(B.I<U> a);
}
// Array: T[].
abstract class C4<T> : A<B.C<T>[]>, I<A<T>[]>
{
    internal abstract I<A<U>[]> F<U>(A<B.C<U>[]> a);
}
// Generic type parameter: A<I<T>>, etc.
abstract class C5<T> : A<I<T>>, I<A<T>>
{
    internal abstract I<A<U>> F<U>(A<I<U>> a);
}
// Multiple interfaces, multiple method parameters.
abstract class C6<T, U> : A<object>, I<object>, B.I<U>
{
    internal abstract void F<X, Y>(I<object> a, A<Y> b);
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (13,16): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "C1").WithArguments("A<T>", "T", "T").WithLocation(13, 16),
                // (13,16): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'I<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "C1").WithArguments("I<T>", "T", "T").WithLocation(13, 16),
                // (15,28): error CS0452: The type 'U' must be a reference type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "F").WithArguments("A<T>", "T", "U").WithLocation(15, 28),
                // (15,38): error CS0452: The type 'U' must be a reference type in order to use it as parameter 'T' in the generic type or method 'I<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "a").WithArguments("I<T>", "T", "U").WithLocation(15, 38),
                // (18,16): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "C2").WithArguments("A<T>", "T", "T").WithLocation(18, 16),
                // (18,16): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "C2").WithArguments("A<T>", "T", "T").WithLocation(18, 16),
                // (20,30): error CS0452: The type 'U' must be a reference type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "F").WithArguments("A<T>", "T", "U").WithLocation(20, 30),
                // (20,42): error CS0452: The type 'U' must be a reference type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "a").WithArguments("A<T>", "T", "U").WithLocation(20, 42),
                // (23,16): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'U' in the generic type or method 'B.C<U>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "C3").WithArguments("B.C<U>", "U", "T").WithLocation(23, 16),
                // (23,16): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'U' in the generic type or method 'B.I<U>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "C3").WithArguments("B.I<U>", "U", "T").WithLocation(23, 16),
                // (25,30): error CS0453: The type 'U' must be a non-nullable value type in order to use it as parameter 'U' in the generic type or method 'B.C<U>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "F").WithArguments("B.C<U>", "U", "U").WithLocation(25, 30),
                // (25,42): error CS0453: The type 'U' must be a non-nullable value type in order to use it as parameter 'U' in the generic type or method 'B.I<U>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "a").WithArguments("B.I<U>", "U", "U").WithLocation(25, 42),
                // (28,16): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'U' in the generic type or method 'B.C<U>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "C4").WithArguments("B.C<U>", "U", "T").WithLocation(28, 16),
                // (28,16): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "C4").WithArguments("A<T>", "T", "T").WithLocation(28, 16),
                // (30,33): error CS0452: The type 'U' must be a reference type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "F").WithArguments("A<T>", "T", "U").WithLocation(30, 33),
                // (30,50): error CS0453: The type 'U' must be a non-nullable value type in order to use it as parameter 'U' in the generic type or method 'B.C<U>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "a").WithArguments("B.C<U>", "U", "U").WithLocation(30, 50),
                // (33,16): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'I<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "C5").WithArguments("I<T>", "T", "T").WithLocation(33, 16),
                // (33,16): error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "C5").WithArguments("A<T>", "T", "T").WithLocation(33, 16),
                // (35,31): error CS0452: The type 'U' must be a reference type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "F").WithArguments("A<T>", "T", "U").WithLocation(35, 31),
                // (35,44): error CS0452: The type 'U' must be a reference type in order to use it as parameter 'T' in the generic type or method 'I<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "a").WithArguments("I<T>", "T", "U").WithLocation(35, 44),
                // (38,16): error CS0453: The type 'U' must be a non-nullable value type in order to use it as parameter 'U' in the generic type or method 'B.I<U>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "C6").WithArguments("B.I<U>", "U", "U").WithLocation(38, 16),
                // (40,54): error CS0452: The type 'Y' must be a reference type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "b").WithArguments("A<T>", "T", "Y").WithLocation(40, 54));
        }

        /// <summary>
        /// Partial method signatures are bound eagerly, not lazily.
        /// (See SourceMemberMethodSymbol..ctor.) Ensure constraints
        /// on parameters and return types are checked in those cases.
        /// </summary>
        [Fact]
        public void PartialMethodWithArgumentConstraint()
        {
            var source =
@"class A<T> where T : struct
{
    internal class B { }
}
partial class B<T> where T : struct
{
    static partial void M1<U>(A<U> a) where U : struct;
    static partial void M2<U>(A<U> a, A<A<int>>.B b) where U : class;
    static partial void M3(A<T> a);
    static partial void M4<U, V>() where U : A<V>;
    static partial A<U> M5<U>();
}
partial class B<T> where T : struct
{
    static partial void M1<U>(A<U> a) where U : struct { }
    static partial void M2<U>(A<U> a, A<A<int>>.B b) where U : class { }
    static partial void M3(A<T> a) { }
    static partial void M4<U, V>() where U : A<V> { }
    static partial A<U> M5<U>() { return null; }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (11,25): error CS0766: Partial methods must have a void return type
                Diagnostic(ErrorCode.ERR_PartialMethodMustReturnVoid, "M5").WithLocation(11, 25),
                // (19,25): error CS0766: Partial methods must have a void return type
                Diagnostic(ErrorCode.ERR_PartialMethodMustReturnVoid, "M5").WithLocation(19, 25),
                // (10,28): error CS0453: The type 'V' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "U").WithArguments("A<T>", "T", "V").WithLocation(10, 28),
                // (18,28): error CS0453: The type 'V' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "U").WithArguments("A<T>", "T", "V").WithLocation(18, 28),
                // (8,36): error CS0453: The type 'U' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "a").WithArguments("A<T>", "T", "U").WithLocation(8, 36),
                // (8,51): error CS0453: The type 'A<int>' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "b").WithArguments("A<T>", "T", "A<int>").WithLocation(8, 51),
                // (19,25): error CS0453: The type 'U' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "M5").WithArguments("A<T>", "T", "U").WithLocation(11, 25));
        }

        [ClrOnlyFact]
        public void StructAndUnconstrainedTypeParameterConstraints()
        {
            var source =
@"class C<T, U>
    where U : struct, T
{
}";
            CompileAndVerify(source);
        }

        [ClrOnlyFact]
        public void WhereTypeParameter()
        {
            var source =
@"interface I<T> { }
class C<where> where where : I<where> { }";
            CompileAndVerify(source);
        }

        [ClrOnlyFact]
        public void NewConstraintWithValueType()
        {
            var source =
@"struct S { }
class C<T> where T : new()
{
    static void M(object o)
    {
        M(new C<int>());
        M(new S());
    }
}";
            CompileAndVerify(source);
        }

        [Fact]
        public void NewConstraintNotInherited()
        {
            var source =
@"class C<T, U>
    where T : U
    where U : new()
{
    static void M(object o)
    {
        M(new U());
        M(new T());
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,11): error CS0304: Cannot create an instance of the variable type 'T' because it does not have the new() constraint
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "new T()").WithArguments("T").WithLocation(8, 11));
        }

        [ClrOnlyFact]
        public void RedundantConstraints()
        {
            var source =
@"class A { }
class B<T> where T : A
{
    class C<U> where U : A, T
    {
        class D<V> where V : A, U { }
    }
}";
            CompileAndVerify(source);
        }

        /// <summary>
        /// Constraint errors in aliases are reported at the alias declaration,
        /// and errors are reported regardless of whether the alias is used.
        /// This is a breaking change from Dev10 which reports constraint errors
        /// in aliases at the point the alias is used, not at the alias declaration,
        /// and does not report constraint errors on unused aliases.
        /// </summary>
        [Fact]
        public void AliasConstraintErrors01()
        {
            var text =
@"using A = C<int>; // unused
using B = C<bool>;
class C<T> where T : class
{
    static void M()
    {
        B.M();
        new B();
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (1,7): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'C<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "A").WithArguments("C<T>", "T", "int").WithLocation(1, 7),
                // (2,7): error CS0452: The type 'bool' must be a reference type in order to use it as parameter 'T' in the generic type or method 'C<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "B").WithArguments("C<T>", "T", "bool").WithLocation(2, 7),
                // (1,1): info CS8019: Unnecessary using directive.
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using A = C<int>;"));
        }

        /// <summary>
        /// More constraint errors in aliases.
        /// </summary>
        [Fact]
        public void AliasConstraintErrors02()
        {
            var text =
@"using A = C<I<int?>>;
using B1 = C<I<int>>.D1<object>;
using B2 = C<int>.D2<string>;
interface I<T> where T : struct { }
class C<T> where T : class
{
    internal delegate void D1<U>() where U : T;
    internal delegate void D2<U>() where U : new();
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (1,7): error CS0453: The type 'int?' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'I<T>'
                // using A = C<I<int?>>;
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "A").WithArguments("I<T>", "T", "int?"),
                // (2,7): error CS0311: The type 'object' cannot be used as type parameter 'U' in the generic type or method 'C<I<int>>.D1<U>'. There is no implicit reference conversion from 'object' to 'I<int>'.
                // using B1 = C<I<int>>.D1<object>;
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "B1").WithArguments("C<I<int>>.D1<U>", "I<int>", "U", "object"),
                // (3,7): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'C<T>'
                // using B2 = C<int>.D2<string>;
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "B2").WithArguments("C<T>", "T", "int"),
                // (3,7): error CS0310: 'string' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'U' in the generic type or method 'C<int>.D2<U>'
                // using B2 = C<int>.D2<string>;
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "B2").WithArguments("C<int>.D2<U>", "U", "string"),
                // (1,1): info CS8019: Unnecessary using directive.
                // using A = C<I<int?>>;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using A = C<I<int?>>;"),
                // (2,1): info CS8019: Unnecessary using directive.
                // using B1 = C<I<int>>.D1<object>;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using B1 = C<I<int>>.D1<object>;"),
                // (3,1): info CS8019: Unnecessary using directive.
                // using B2 = C<int>.D2<string>;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using B2 = C<int>.D2<string>;"));
        }

        /// <summary>
        /// Constraints in method signatures are not checked
        /// at the time types in the signature are bound.
        /// Ensure the constraints are checked.
        /// </summary>
        [Fact]
        public void MethodSignatureConstraints()
        {
            var text =
@"class A : System.Attribute
{
    public A(object o) { }
}
class B<T> where T : class { }
class C
{
    [A(new B<int>())]
    [return: A(new B<short>())]
    static B<byte> F(
        [A(new B<float>())]
        B<double> o)
    {
        return null;
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (11,18): error CS0452: The type 'float' must be a reference type in order to use it as parameter 'T' in the generic type or method 'B<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "float").WithArguments("B<T>", "T", "float").WithLocation(11, 18),
                // (11,12): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "new B<float>()").WithLocation(11, 12),
                // (10,20): error CS0452: The type 'byte' must be a reference type in order to use it as parameter 'T' in the generic type or method 'B<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "F").WithArguments("B<T>", "T", "byte").WithLocation(10, 20),
                // (12,19): error CS0452: The type 'double' must be a reference type in order to use it as parameter 'T' in the generic type or method 'B<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "o").WithArguments("B<T>", "T", "double").WithLocation(12, 19),
                // (8,14): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'B<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("B<T>", "T", "int").WithLocation(8, 14),
                // (8,8): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "new B<int>()").WithLocation(8, 8),
                // (9,22): error CS0452: The type 'short' must be a reference type in order to use it as parameter 'T' in the generic type or method 'B<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "short").WithArguments("B<T>", "T", "short").WithLocation(9, 22),
                // (9,16): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "new B<short>()").WithLocation(9, 16));
        }

        [Fact]
        public void DefaultArguments()
        {
            var text =
@"class A<T> where T : struct
{
    const int F = 1;
    static void M(int arg = A<object>.F) { }
}
class B
{
    static int F<T>(int arg = F<string>(0)) where T : struct
    {
        return 0;
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
                // (4,31): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "object").WithArguments("A<T>", "T", "object").WithLocation(4, 31),
                // (8,31): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'B.F<T>(int)'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "F<string>").WithArguments("B.F<T>(int)", "T", "string").WithLocation(8, 31));
        }

        [Fact]
        public void AttributeReferencingAttributedType()
        {
            var text =
@"class A : System.Attribute
{
    public A(object o) { }
}
[A(typeof(C<object>))]
class C<T> where T : C<T> { }";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,13): error CS0311: The type 'object' cannot be used as type parameter 'T' in the generic type or method 'C<T>'. There is no implicit reference conversion from 'object' to 'C<object>'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "object").WithArguments("C<T>", "C<object>", "T", "object").WithLocation(5, 13));
        }

        /// <summary>
        /// Ensure constraint diagnostics are generated. Specifically,
        /// ensure ForceComplete resolves constraints completely.
        /// </summary>
        [Fact]
        public void ForceComplete()
        {
            var source =
@"class C<T> where T : A
{
    static void M<U>() where U : B { }
}
interface IA<T> where T : T { }
interface IB
{
    void M<U>() where U : U;
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (1,22): error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A").WithArguments("A").WithLocation(1, 22),
                // (3,34): error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "B").WithArguments("B").WithLocation(3, 34),
                // (5,14): error CS0454: Circular constraint dependency involving 'T' and 'T'
                Diagnostic(ErrorCode.ERR_CircularConstraint, "T").WithArguments("T", "T").WithLocation(5, 14),
                // (8, 12): error CS0454: Circular constraint dependency involving 'U' and 'U'
                Diagnostic(ErrorCode.ERR_CircularConstraint, "U").WithArguments("U", "U").WithLocation(8, 12));
        }

        [Fact]
        public void ParameterAndReturnTypeViolationsNonMethods01()
        {
            var source =
@"interface I<T> where T : class { }
delegate void D<T>();
class C
{
    C(I<int> i) { }
    I<byte> P { get; set; }
    I<double> this[I<float> index] { get { return null; } }
    event D<I<short>> E;
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,23): error CS0452: The type 'short' must be a reference type in order to use it as parameter 'T' in the generic type or method 'I<T>'
                //     event D<I<short>> E;
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "E").WithArguments("I<T>", "T", "short").WithLocation(8, 23),
                // (5,14): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'I<T>'
                //     C(I<int> i) { }
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "i").WithArguments("I<T>", "T", "int").WithLocation(5, 14),
                // (6,13): error CS0452: The type 'byte' must be a reference type in order to use it as parameter 'T' in the generic type or method 'I<T>'
                //     I<byte> P { get; set; }
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "P").WithArguments("I<T>", "T", "byte").WithLocation(6, 13),
                // (7,29): error CS0452: The type 'float' must be a reference type in order to use it as parameter 'T' in the generic type or method 'I<T>'
                //     I<double> this[I<float> index] { get { return null; } }
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "index").WithArguments("I<T>", "T", "float").WithLocation(7, 29),
                // (7,15): error CS0452: The type 'double' must be a reference type in order to use it as parameter 'T' in the generic type or method 'I<T>'
                //     I<double> this[I<float> index] { get { return null; } }
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "this").WithArguments("I<T>", "T", "double").WithLocation(7, 15),
                // (8,23): warning CS0067: The event 'C.E' is never used
                //     event D<I<short>> E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(8, 23));
        }

        [Fact]
        public void ParameterAndReturnTypeViolationsNonMethods02()
        {
            var source =
@"interface I<T> where T : class { }
class C
{
    static void M()
    {
        (delegate(I<long> o) { })(null);
        ((I<short> o) => { })();
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,21): error CS0452: The type 'long' must be a reference type in order to use it as parameter 'T' in the generic type or method 'I<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "long").WithArguments("I<T>", "T", "long").WithLocation(6, 21),
                // (6,9): error CS0149: Method name expected
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "(delegate(I<long> o) { })").WithLocation(6, 9),
                // (7,13): error CS0452: The type 'short' must be a reference type in order to use it as parameter 'T' in the generic type or method 'I<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "short").WithArguments("I<T>", "T", "short").WithLocation(7, 13),
                // (7,9): error CS0149: Method name expected
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "((I<short> o) => { })").WithLocation(7, 9));
        }

        [Fact]
        public void FixedFieldArgument()
        {
            var source =
@"unsafe class C<T> where T : new()
{
    private C() { }
    fixed int F[C<C<T>>.G];
    const int G = 1;
}";
            CreateCompilation(source, options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true)).VerifyDiagnostics(
                // (4,15): error CS1642: Fixed size buffer fields may only be members of structs
                Diagnostic(ErrorCode.ERR_FixedNotInStruct, "F").WithLocation(4, 15),
                // (4,19): error CS0310: 'C<T>' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'C<T>'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "C<T>").WithArguments("C<T>", "T", "C<T>").WithLocation(4, 19));
        }

        [ClrOnlyFact]
        public void PartialClass()
        {
            var source =
@"interface IA<T> { }
interface IB { }
partial class C<T> where T : struct, IA<T>, IB { }
partial class C<T> where T : struct, IB, IA<T> { }
partial class C<T> { }";
            CompileAndVerify(source);
        }

        [ClrOnlyFact]
        public void SubstitutedLambdaConstraints()
        {
            var source =
@"using System;
interface I<T> { }
class A : I<A> { }
class C<T> where T : class, I<T>
{
    static internal Action<T> M()
    {
        return (T t) => { };
    }
}
struct S
{
    static internal Action<T> M<T>() where T : class, I<T>, new()
    {
        return (T t) => { };
    }
    static void M()
    {
        Action<A> a;
        a = C<A>.M();
        a = S.M<A>();
    }
}";
            CompileAndVerify(source);
        }

        [WorkItem(528571, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528571")]
        [Fact]
        public void ConstraintsWithinStruct()
        {
            var source =
@"interface I<T> { }
struct S
{
    static void M<T>() where T : class, I<T>, new() { }
    static void N()
    {
        S.M<A>();
    }
    class A : I<A> { }
}";
            CompileAndVerify(source);
        }

        [Fact]
        public void ExtensionMethodsWithConstraints()
        {
            var text =
@"interface I { }
struct S { }
static class C
{
    static void M(I i, S s)
    {
        i.E();
        s.E();
        i.F();
        s.F();
    }
    static void E(this object o) { }
    static void E<T>(this T t) where T : new() { }
    static void F(this object o) { }
    static void F<T>(this T t) where T : struct { }
}";
            CreateCompilationWithMscorlib40(text, references: new[] { SystemCoreRef }, parseOptions: TestOptions.WithoutImprovedOverloadCandidates).VerifyDiagnostics(
                // (7,9): error CS0310: 'I' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'C.E<T>(T)'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "i.E").WithArguments("C.E<T>(T)", "T", "I").WithLocation(7, 9),
                // (9,9): error CS0453: The type 'I' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'C.F<T>(T)'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "i.F").WithArguments("C.F<T>(T)", "T", "I").WithLocation(9, 9));
            CreateCompilationWithMscorlib40(text, references: new[] { SystemCoreRef }).VerifyDiagnostics();
        }

        [ClrOnlyFact]
        public void DefaultT()
        {
            var source =
@"struct S { }
class C
{
    static T F1<T>()
    {
        return default(T);
    }
    static T F2<T>() where T : class
    {
        return default(T);
    }
    static T F3<T>() where T : struct
    {
        return default(T);
    }
    static void M(object o)
    {
        if (o == null)
        {
            o = ""null"";
        }
        System.Console.WriteLine(""{0}"", o);
    }
    static void Main()
    {
        M(F1<C>());
        M(F1<S>());
        M(F2<C>());
        M(F3<S>());
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput:
@"null
S
null
S");
            var expectedIL =
@"{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (T V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""T""
  IL_0008:  ldloc.0
  IL_0009:  ret
}";
            compilation.VerifyIL("C.F1<T>()", expectedIL);
            compilation.VerifyIL("C.F2<T>()", expectedIL);
            compilation.VerifyIL("C.F3<T>()", expectedIL);
        }

        [WorkItem(542376, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542376")]
        [ClrOnlyFact]
        public void NullT()
        {
            var source =
@"class C
{
    static object F0()
    {
        return null;
    }
    static T F1<T>() where T : class
    {
        return null;
    }
    static T F2<T>() where T : C
    {
        return null;
    }
    static T F3<T>() where T : class
    {
        return (T)null;
    }
    static T F4<T>() where T : C
    {
        return (T)null;
    }
    static T F5<T>() where T : class
    {
        return null as T;
    }
    static T F6<T>() where T : C
    {
        return null as T;
    }
    static void M<T>(T t) where T : class
    {
        bool b;
        b = (t == null);
        b = (t != null);
        b = (null is T);
    }
}";
            var compilation = CompileAndVerify(source);
            var expectedIL =
@"{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (T V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""T""
  IL_0008:  ldloc.0
  IL_0009:  ret
}";
            compilation.VerifyIL("C.F1<T>()", expectedIL);
            compilation.VerifyIL("C.F2<T>()", expectedIL);
            compilation.VerifyIL("C.F3<T>()", expectedIL);
            compilation.VerifyIL("C.F4<T>()", expectedIL);
            compilation.VerifyIL("C.F5<T>()", expectedIL);
            compilation.VerifyIL("C.F6<T>()", expectedIL);
        }

        [ClrOnlyFact]
        public void TryCast()
        {
            var source =
@"class A { }
class B1<T>
    where T : A
{
    static T F1<U>(U u) { return u as T; }
    static T F2<U>(U u) where U : A { return u as T; }
    static T F3<U>(U u) where U : class { return u as T; }
    static T F4<U>(U u) where U : struct { return u as T; }
}
class B2<T>
    where T : class
{
    static T F1<U>(U u) { return u as T; }
    static T F2<U>(U u) where U : A { return u as T; }
    static T F3<U>(U u) where U : class { return u as T; }
    static T F4<U>(U u) where U : struct { return u as T; }
}";
            var compilation = CompileAndVerify(source);
            var expectedIL =
@"{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""U""
  IL_0006:  isinst     ""T""
  IL_000b:  unbox.any  ""T""
  IL_0010:  ret
}";
            compilation.VerifyIL("B1<T>.F1<U>(U)", expectedIL);
            compilation.VerifyIL("B1<T>.F2<U>(U)", expectedIL);
            compilation.VerifyIL("B1<T>.F3<U>(U)", expectedIL);
            compilation.VerifyIL("B1<T>.F4<U>(U)", expectedIL);
            compilation.VerifyIL("B2<T>.F1<U>(U)", expectedIL);
            compilation.VerifyIL("B2<T>.F2<U>(U)", expectedIL);
            compilation.VerifyIL("B2<T>.F3<U>(U)", expectedIL);
            compilation.VerifyIL("B2<T>.F4<U>(U)", expectedIL);
        }

        [ClrOnlyFact]
        public void NewT()
        {
            var source =
@"struct S { }
class C
{
    static T F1<T>() where T : new()
    {
        return new T();
    }
    static T F2<T>() where T : class, new()
    {
        return new T();
    }
    static T F3<T>() where T : struct
    {
        return new T();
    }
    static void M(object o)
    {
        System.Console.WriteLine(""{0}"", o);
    }
    static void Main()
    {
        M(F1<C>());
        M(F1<S>());
        M(F2<C>());
        M(F3<S>());
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput:
@"C
S
C
S");
            compilation.VerifyIL("C.F1<T>()",
@"
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  call       ""T System.Activator.CreateInstance<T>()""
  IL_0005:  ret
}");
            compilation.VerifyIL("C.F2<T>()",
@"{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  call       ""T System.Activator.CreateInstance<T>()""
  IL_0005:  ret
}");
            compilation.VerifyIL("C.F3<T>()",
@"
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  call       ""T System.Activator.CreateInstance<T>()""
  IL_0005:  ret
}");
        }

        [WorkItem(542312, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542312")]
        [ClrOnlyFact]
        public void NewTStatement()
        {
            var source =
@"struct S { }
class A { }
class B
{
    internal B() { }
}
class C
{
    static void M<T, U, V>()
        where T : struct
        where U : new()
        where V : class, new()
    {
        new S();
        new A();
        new B();
        new T();
        new U();
        new V();
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.M<T, U, V>()",
@"
{
  // Code size       31 (0x1f)
  .maxstack  1
  IL_0000:  newobj     ""A..ctor()""
  IL_0005:  pop
  IL_0006:  newobj     ""B..ctor()""
  IL_000b:  pop
  IL_000c:  call       ""T System.Activator.CreateInstance<T>()""
  IL_0011:  pop
  IL_0012:  call       ""U System.Activator.CreateInstance<U>()""
  IL_0017:  pop
  IL_0018:  call       ""V System.Activator.CreateInstance<V>()""
  IL_001d:  pop
  IL_001e:  ret
}");
        }

        /// <summary>
        /// Should bind type parameter constructor arguments
        /// even though no arguments are expected.
        /// </summary>
        [Fact]
        public void NewTWithBadArguments()
        {
            var source =
@"struct S<T, U> where T : new()
{
   void M()
   {
       object o = new T(F());
       o = new U(G());
   }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,19): error CS0417: 'T': cannot provide arguments when creating an instance of a variable type
                Diagnostic(ErrorCode.ERR_NewTyvarWithArgs, "new T(F())").WithArguments("T").WithLocation(5, 19),
                // (5,25): error CS0103: The name 'F' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "F").WithArguments("F").WithLocation(5, 25),
                // (6,12): error CS0304: Cannot create an instance of the variable type 'U' because it does not have the new() constraint
                Diagnostic(ErrorCode.ERR_NoNewTyvar, "new U(G())").WithArguments("U").WithLocation(6, 12),
                // (6,18): error CS0103: The name 'G' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "G").WithArguments("G").WithLocation(6, 18));
        }

        /// <summary>
        /// Invoke methods and properties on constrained generic types.
        /// </summary>
        [ClrOnlyFact]
        public void Members()
        {
            var source =
@"using System;
interface I
{
    object P { get; set; }
    void M();
}
abstract class A
{
    public abstract object P { get; set; }
    public abstract void M();
}
class B : A, I
{
    public override object P
    {
        get { Console.WriteLine(""B.get_P""); return null; }
        set { Console.WriteLine(""B.set_P""); }
    }
    public override void M()
    {
        Console.WriteLine(""B.M"");
    }
}
struct S : I
{
    public object P
    {
        get { Console.WriteLine(""S.get_P""); return null; }
        set { Console.WriteLine(""S.set_P""); }
    }
    public void M()
    {
        Console.WriteLine(""S.M"");
    }
}
class C<T1, T2>
    where T1 : I
    where T2 : A
{
    internal static void M<U1, U2>(T1 t1, T2 t2, U1 u1, U2 u2)
        where U1 : I
        where U2 : A
    {
        t1.P = t1.P;
        t1.M();
        t2.P = t2.P;
        t2.M();
        u1.P = u1.P;
        u1.M();
        u2.P = u2.P;
        u2.M();
    }
}
class C
{
    static void Main()
    {
        B b = new B();
        S s = new S();
        C<I, A>.M(s, b, s, b);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput:
@"S.get_P
S.set_P
S.M
B.get_P
B.set_P
B.M
S.get_P
S.set_P
S.M
B.get_P
B.set_P
B.M");
            compilation.VerifyIL("C<T1, T2>.M<U1, U2>(T1, T2, U1, U2)",
@"
{
  // Code size      145 (0x91)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarga.s   V_0
  IL_0004:  constrained. ""T1""
  IL_000a:  callvirt   ""object I.P.get""
  IL_000f:  constrained. ""T1""
  IL_0015:  callvirt   ""void I.P.set""
  IL_001a:  ldarga.s   V_0
  IL_001c:  constrained. ""T1""
  IL_0022:  callvirt   ""void I.M()""
  IL_0027:  ldarg.1
  IL_0028:  box        ""T2""
  IL_002d:  ldarg.1
  IL_002e:  box        ""T2""
  IL_0033:  callvirt   ""object A.P.get""
  IL_0038:  callvirt   ""void A.P.set""
  IL_003d:  ldarg.1
  IL_003e:  box        ""T2""
  IL_0043:  callvirt   ""void A.M()""
  IL_0048:  ldarga.s   V_2
  IL_004a:  ldarga.s   V_2
  IL_004c:  constrained. ""U1""
  IL_0052:  callvirt   ""object I.P.get""
  IL_0057:  constrained. ""U1""
  IL_005d:  callvirt   ""void I.P.set""
  IL_0062:  ldarga.s   V_2
  IL_0064:  constrained. ""U1""
  IL_006a:  callvirt   ""void I.M()""
  IL_006f:  ldarg.3
  IL_0070:  box        ""U2""
  IL_0075:  ldarg.3
  IL_0076:  box        ""U2""
  IL_007b:  callvirt   ""object A.P.get""
  IL_0080:  callvirt   ""void A.P.set""
  IL_0085:  ldarg.3
  IL_0086:  box        ""U2""
  IL_008b:  callvirt   ""void A.M()""
  IL_0090:  ret
}");
        }

        [ClrOnlyFact]
        public void Indexers()
        {
            var source =
@"using System;
interface I
{
    object this[object index] { set; }
}
class A
{
    internal object this[object index]
    {
        get
        {
            Console.WriteLine(""A[{0}]"", index);
            return null;
        }
    }
}
struct S : I
{
    public object this[object index]
    {
        set
        {
            Console.WriteLine(""S[{0}]"", index);
        }
    }
}
class C
{
    static void M<T, U>(T t, U u)
        where T : I
        where U : A
    {
        t[0] = u[1];
    }
    static void Main()
    {
        M(new S(), new A());
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput:
@"A[1]
S[0]");
            compilation.VerifyIL("C.M<T, U>(T, U)",
@"
{
  // Code size       37 (0x25)
  .maxstack  4
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldc.i4.0
  IL_0003:  box        ""int""
  IL_0008:  ldarg.1
  IL_0009:  box        ""U""
  IL_000e:  ldc.i4.1
  IL_000f:  box        ""int""
  IL_0014:  callvirt   ""object A.this[object].get""
  IL_0019:  constrained. ""T""
  IL_001f:  callvirt   ""void I.this[object].set""
  IL_0024:  ret
}");
        }

        /// <summary>
        /// Access fields on constrained generic types.
        /// </summary>
        [ClrOnlyFact]
        [WorkItem(542277, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542277")]
        public void Fields()
        {
            var source =
@"class A
{
    internal object F;
}
class B<T>
    where T : A
{
    internal static void Swap<U>(T t, U u)
        where U : T
    {
        var v1 = t.F;
        var v2 = u.F;
        t.F = v2;
        u.F = v1;
    }
}
class C
{
    static void Main()
    {
        var a1 = new A();
        var a2 = new A();
        a1.F = 1;
        a2.F = 2;
        B<A>.Swap(a1, a2);
        System.Console.WriteLine(""{0}, {1}"", a1.F, a2.F);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "2, 1");
            compilation.VerifyIL("B<T>.Swap<U>(T, U)",
@"{
  // Code size       49 (0x31)
  .maxstack  2
  .locals init (object V_0, //v1
  object V_1) //v2
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  ldfld      ""object A.F""
  IL_000b:  stloc.0
  IL_000c:  ldarg.1
  IL_000d:  box        ""U""
  IL_0012:  ldfld      ""object A.F""
  IL_0017:  stloc.1
  IL_0018:  ldarg.0
  IL_0019:  box        ""T""
  IL_001e:  ldloc.1
  IL_001f:  stfld      ""object A.F""
  IL_0024:  ldarg.1
  IL_0025:  box        ""U""
  IL_002a:  ldloc.0
  IL_002b:  stfld      ""object A.F""
  IL_0030:  ret
}");
        }

        /// <summary>
        /// Access events on constrained generic types.
        /// </summary>
        [ClrOnlyFact]
        public void Events()
        {
            var source =
@"delegate void D();
class A
{
    internal event D E;
    internal static void Swap<T, U>(T t, U u)
        where T : A
        where U : T
    {
        var v1 = t.E;
        var v2 = u.E;
        t.E = v2;
        u.E = v1;
    }
    static void F1() { }
    static void F2() { }
    static void Main()
    {
        var a1 = new A();
        var a2 = new A();
        a1.E += F1;
        a1.E += F2;
        a2.E += F1;
        Swap<A, A>(a1, a2);
        System.Console.WriteLine(""{0}, {1}"", a1.E.GetInvocationList().Length, a2.E.GetInvocationList().Length);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "1, 2");
            compilation.VerifyIL("A.Swap<T, U>(T, U)",
@"{
  // Code size       49 (0x31)
  .maxstack  2
  .locals init (D V_0, //v1
  D V_1) //v2
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  ldfld      ""D A.E""
  IL_000b:  stloc.0
  IL_000c:  ldarg.1
  IL_000d:  box        ""U""
  IL_0012:  ldfld      ""D A.E""
  IL_0017:  stloc.1
  IL_0018:  ldarg.0
  IL_0019:  box        ""T""
  IL_001e:  ldloc.1
  IL_001f:  stfld      ""D A.E""
  IL_0024:  ldarg.1
  IL_0025:  box        ""U""
  IL_002a:  ldloc.0
  IL_002b:  stfld      ""D A.E""
  IL_0030:  ret
}");
        }

        [Fact]
        public void ConflictingConstraints01()
        {
            var source =
@"class A { }
class B { }
class C<S, T, U, V, W>
    where T : new()
    where U : struct
    where V : class
    where W : A
{
    struct S1<X> where X : S, new() { }
    struct S2<X> where X : T, new() { }
    struct S3<X> where X : U, new() { }
    struct S4<X> where X : V, new() { }
    struct S5<X> where X : W, new() { }
    class C1<X> where X : struct, S { }
    class C2<X> where X : struct, T { }
    class C3<X> where X : struct, U { }
    class C4<X> where X : struct, V { }
    class C5<X> where X : struct, W { }
    delegate void D1<X>() where X : class, S;
    delegate void D2<X>() where X : class, T;
    delegate void D3<X>() where X : class, U;
    delegate void D4<X>() where X : class, V;
    delegate void D5<X>() where X : class, W;
    void M1<X>() where X : A, S { }
    void M2<X>() where X : A, T { }
    void M3<X>() where X : A, U { }
    void M4<X>() where X : A, V { }
    void M5<X>() where X : A, W { }
    interface I1<X> where X : B, S { }
    interface I2<X> where X : B, T { }
    interface I3<X> where X : B, U { }
    interface I4<X> where X : B, V { }
    interface I5<X> where X : B, W { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (11,15): error CS0456: Type parameter 'U' has the 'struct' constraint so 'U' cannot be used as a constraint for 'X'
                Diagnostic(ErrorCode.ERR_ConWithValCon, "X").WithArguments("X", "U").WithLocation(11, 15),
                // (16,14): error CS0456: Type parameter 'U' has the 'struct' constraint so 'U' cannot be used as a constraint for 'X'
                Diagnostic(ErrorCode.ERR_ConWithValCon, "X").WithArguments("X", "U").WithLocation(16, 14),
                // (18,14): error CS0455: Type parameter 'X' inherits conflicting constraints 'A' and 'System.ValueType'
                Diagnostic(ErrorCode.ERR_BaseConstraintConflict, "X").WithArguments("X", "A", "System.ValueType").WithLocation(18, 14),
                // (21,22): error CS0456: Type parameter 'U' has the 'struct' constraint so 'U' cannot be used as a constraint for 'X'
                Diagnostic(ErrorCode.ERR_ConWithValCon, "X").WithArguments("X", "U").WithLocation(21, 22),
                // (26,13): error CS0456: Type parameter 'U' has the 'struct' constraint so 'U' cannot be used as a constraint for 'X'
                Diagnostic(ErrorCode.ERR_ConWithValCon, "X").WithArguments("X", "U").WithLocation(26, 13),
                // (31,18): error CS0456: Type parameter 'U' has the 'struct' constraint so 'U' cannot be used as a constraint for 'X'
                Diagnostic(ErrorCode.ERR_ConWithValCon, "X").WithArguments("X", "U").WithLocation(31, 18),
                // (33,18): error CS0455: Type parameter 'X' inherits conflicting constraints 'A' and 'B'
                Diagnostic(ErrorCode.ERR_BaseConstraintConflict, "X").WithArguments("X", "A", "B").WithLocation(33, 18));
        }

        /// <summary>
        /// No error for conflicting constraint on virtual method
        /// in derived class unless the method is overridden.
        /// </summary>
        [Fact]
        public void ConflictingConstraints02()
        {
            var source =
@"class A { }
class B { }
class C<T, U>
{
    internal virtual void M<X>() where X : T, U { }
}
class D1 : C<A, B> { }
class D2 : C<A, B>
{
    internal override void M<X>() { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (10,30): error CS0455: Type parameter 'X' inherits conflicting constraints 'B' and 'A'
                Diagnostic(ErrorCode.ERR_BaseConstraintConflict, "X").WithArguments("X", "B", "A").WithLocation(10, 30));
        }

        [ClrOnlyFact]
        public void MovedConstraints()
        {
            var source =
@"interface I { }
class C { }
interface IA<T, U> where U : T, I { }
class A<X> : IA<I, X> where X : I { }
class A<X, Y> : IA<X, Y> where X : I where Y : X { }
interface IB<T, U> where U : C, T { }
class B<X> : IB<C, X> where X : C { }
class B<X, Y> : IB<X, Y> where X : C where Y : X { }";
            CompileAndVerify(source);
        }

        /// <summary>
        /// The constraint type can be dropped from the overridden
        /// method if the type is object. Spec. 13.4.3.
        /// </summary>
        [Fact]
        public void OverriddenMethodWithObjectConstraint()
        {
            var source =
@"interface I<T>
{
    void M1<U>() where U : T;
    void M2<U>() where U : class, T;
}
abstract class A<T>
{
    public abstract void M1<U>() where U : T;
    public void M2<U>() where U : T { }
}
class B1 : I<object>
{
    public void M1<T>() { }
    public void M2<T>() { }
}
class B2 : A<object>, I<object>
{
    public override void M1<T>() { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (14,17): error CS0425: The constraints for type parameter 'T' of method 'B1.M2<T>()' must match the constraints for type parameter 'U' of interface method 'I<object>.M2<U>()'. Consider using an explicit interface implementation instead.
                //     public void M2<T>() { }
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M2").WithArguments("T", "B1.M2<T>()", "U", "I<object>.M2<U>()").WithLocation(14, 17),
                // (16,23): error CS0425: The constraints for type parameter 'U' of method 'A<object>.M2<U>()' must match the constraints for type parameter 'U' of interface method 'I<object>.M2<U>()'. Consider using an explicit interface implementation instead.
                // class B2 : A<object>, I<object>
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "I<object>").WithArguments("U", "A<object>.M2<U>()", "U", "I<object>.M2<U>()").WithLocation(16, 23));
        }

        [Fact]
        public void ImplicitImplementations()
        {
            var source =
@"interface I { }
class A { }
class B : A, I { }
interface I1<T, U>
{
    void M<V>() where V : T, U;
}
class C1<T, U> : I1<T, U> where T : U
{
    public void M<V>() where V : T { } // error
}
class C2<T> : I1<T, T>
{
    public void M<U>() where U : T { }
}
class C3 : I1<B, I>
{
    public void M<T>() where T : B, I { }
}
class C4 : I1<B, I>
{
    public void M<T>() where T : B { } // error
}
class C5 : I1<A, A>
{
    public void M<T>() where T : A { }
}
class C6 : I1<A, B>
{
    public void M<T>() where T : B { } // error
}
interface I2<T>
{
    void M<U>() where U : class, T, I;
}
class C7<T> : I2<T> where T : I
{
    public void M<U>() where U : class, T { } // error
}
class C8 : I2<A>
{
    public void M<T>() where T : A, I { } // error
}
class C9 : I2<B>
{
    public void M<T>() where T : B { } // error
}
interface I3<T>
{
    void M<U>() where U : struct, T, I;
}
class C10 : I3<I>
{
    public void M<U>() where U : struct, I { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (10,17): error CS0425: The constraints for type parameter 'V' of method 'C1<T, U>.M<V>()' must match the constraints for type parameter 'V' of interface method 'I1<T, U>.M<V>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M").WithArguments("V", "C1<T, U>.M<V>()", "V", "I1<T, U>.M<V>()").WithLocation(10, 17),
                // (22,17): error CS0425: The constraints for type parameter 'T' of method 'C4.M<T>()' must match the constraints for type parameter 'V' of interface method 'I1<B, I>.M<V>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M").WithArguments("T", "C4.M<T>()", "V", "I1<B, I>.M<V>()").WithLocation(22, 17),
                // (30,17): error CS0425: The constraints for type parameter 'T' of method 'C6.M<T>()' must match the constraints for type parameter 'V' of interface method 'I1<A, B>.M<V>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M").WithArguments("T", "C6.M<T>()", "V", "I1<A, B>.M<V>()").WithLocation(30, 17),
                // (38,17): error CS0425: The constraints for type parameter 'U' of method 'C7<T>.M<U>()' must match the constraints for type parameter 'U' of interface method 'I2<T>.M<U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M").WithArguments("U", "C7<T>.M<U>()", "U", "I2<T>.M<U>()").WithLocation(38, 17),
                // (42,17): error CS0425: The constraints for type parameter 'T' of method 'C8.M<T>()' must match the constraints for type parameter 'U' of interface method 'I2<A>.M<U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M").WithArguments("T", "C8.M<T>()", "U", "I2<A>.M<U>()").WithLocation(42, 17),
                // (46,17): error CS0425: The constraints for type parameter 'T' of method 'C9.M<T>()' must match the constraints for type parameter 'U' of interface method 'I2<B>.M<U>()'. Consider using an explicit interface implementation instead.
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M").WithArguments("T", "C9.M<T>()", "U", "I2<B>.M<U>()").WithLocation(46, 17));
        }

        /// <summary>
        /// Report ERR_ImplBadConstraints on the base class that
        /// implements the interface methods with incorrect constraints,
        /// even when the base class does not implement the interface.
        /// </summary>
        [Fact]
        public void CS0425ERR_ImplBadConstraints_BaseFromSource()
        {
            var source =
@"interface I
{
    void M<T>() where T : class;
}
class A1
{
    public void M<T>() where T : class { }
}
class B1 : A1, I
{
}
class A2
{
    public void M<T>() where T : struct { }
}
class B2 : A2, I
{
}
class A3
{
    public void M<T>() { }
}
class B3 : A3, I
{
}
class A4 : I
{
    public void M<T>() { }
}
class B4 : A4, I
{
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (16,16): error CS0425: The constraints for type parameter 'T' of method 'A2.M<T>()' must match the constraints for type parameter 'T' of interface method 'I.M<T>()'. Consider using an explicit interface implementation instead.
                // class B2 : A2, I
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "I").WithArguments("T", "A2.M<T>()", "T", "I.M<T>()").WithLocation(16, 16),
                // (23,16): error CS0425: The constraints for type parameter 'T' of method 'A3.M<T>()' must match the constraints for type parameter 'T' of interface method 'I.M<T>()'. Consider using an explicit interface implementation instead.
                // class B3 : A3, I
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "I").WithArguments("T", "A3.M<T>()", "T", "I.M<T>()").WithLocation(23, 16),
                // (28,17): error CS0425: The constraints for type parameter 'T' of method 'A4.M<T>()' must match the constraints for type parameter 'T' of interface method 'I.M<T>()'. Consider using an explicit interface implementation instead.
                //     public void M<T>() { }
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M").WithArguments("T", "A4.M<T>()", "T", "I.M<T>()").WithLocation(28, 17),
                // (30,16): error CS0425: The constraints for type parameter 'T' of method 'A4.M<T>()' must match the constraints for type parameter 'T' of interface method 'I.M<T>()'. Consider using an explicit interface implementation instead.
                // class B4 : A4, I
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "I").WithArguments("T", "A4.M<T>()", "T", "I.M<T>()").WithLocation(30, 16));
        }

        /// <summary>
        /// Same as CS0425ERR_ImplBadConstraints_BaseFromSource
        /// but with base class defined in metadata.
        /// </summary>
        [Fact]
        public void CS0425ERR_ImplBadConstraints_BaseFromMetadata()
        {
            var ilSource =
@".class interface public abstract I
{
  .method public hidebysig newslot abstract virtual instance void M<class T>() { }
}
.class public A1
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public hidebysig instance void M<class T>() { ret }
}
.class public A2
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public hidebysig instance void M<valuetype T>() { ret }
}
.class public A3
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public hidebysig instance void M<T>() { ret }
}
.class public A4 implements I
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public hidebysig instance void M<T>() { ret }
}
.class public A5
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public hidebysig instance void M<T, U>() { ret }
}";
            var csharpSource =
@"class B1 : A1, I { }
class B2 : A2, I { }
class B3 : A3, I { }
class B4 : A4, I { }
class B5 : A5, I { }";
            CreateCompilationWithILAndMscorlib40(csharpSource, ilSource).VerifyDiagnostics(
                // (2,16): error CS0425: The constraints for type parameter 'T' of method 'A2.M<T>()' must match the constraints for type parameter 'T' of interface method 'I.M<T>()'. Consider using an explicit interface implementation instead.
                // class B2 : A2, I { }
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "I").WithArguments("T", "A2.M<T>()", "T", "I.M<T>()").WithLocation(2, 16),
                // (3,16): error CS0425: The constraints for type parameter 'T' of method 'A3.M<T>()' must match the constraints for type parameter 'T' of interface method 'I.M<T>()'. Consider using an explicit interface implementation instead.
                // class B3 : A3, I { }
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "I").WithArguments("T", "A3.M<T>()", "T", "I.M<T>()").WithLocation(3, 16),
                // (4,16): error CS0425: The constraints for type parameter 'T' of method 'A4.M<T>()' must match the constraints for type parameter 'T' of interface method 'I.M<T>()'. Consider using an explicit interface implementation instead.
                // class B4 : A4, I { }
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "I").WithArguments("T", "A4.M<T>()", "T", "I.M<T>()").WithLocation(4, 16),
                // (5,16): error CS0535: 'B5' does not implement interface member 'I.M<T>()'
                // class B5 : A5, I { }
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I").WithArguments("B5", "I.M<T>()").WithLocation(5, 16));
        }

        /// <summary>
        /// Should not report constraint differences across partial declarations
        /// if the only differences are duplicated constraints.
        /// </summary>
        [Fact]
        public void DuplicateConstraintDifferencesOnPartialDeclarations()
        {
            var source =
@"interface IA { }
// Differ only by duplicates.
partial class A<T> where T : IA, IA { }
partial class A<T> where T : IA { }
partial class A<T> where T : IA, IA { }
// Differ by duplicates and others.
partial class B<T, U> where T : IA, IA { }
partial class B<T, U> where T : U, IA { }
class C<T>
{
    // Differ only by duplicates.
    interface IB<U> { }
    partial class B<U, V> where U : T, T where V : U, IB<T> { }
    partial class B<U, V> where U : T where V : U, U, IB<T>, IB<T> { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,34): error CS0405: Duplicate constraint 'IA' for type parameter 'T'
                Diagnostic(ErrorCode.ERR_DuplicateBound, "IA").WithArguments("IA", "T").WithLocation(3, 34),
                // (5,34): error CS0405: Duplicate constraint 'IA' for type parameter 'T'
                Diagnostic(ErrorCode.ERR_DuplicateBound, "IA").WithArguments("IA", "T").WithLocation(5, 34),
                // (7,15): error CS0265: Partial declarations of 'B<T, U>' have inconsistent constraints for type parameter 'T'
                Diagnostic(ErrorCode.ERR_PartialWrongConstraints, "B").WithArguments("B<T, U>", "T").WithLocation(7, 15),
                // (7,37): error CS0405: Duplicate constraint 'IA' for type parameter 'T'
                Diagnostic(ErrorCode.ERR_DuplicateBound, "IA").WithArguments("IA", "T").WithLocation(7, 37),
                // (13,40): error CS0405: Duplicate constraint 'T' for type parameter 'U'
                Diagnostic(ErrorCode.ERR_DuplicateBound, "T").WithArguments("T", "U").WithLocation(13, 40),
                // (14,52): error CS0405: Duplicate constraint 'U' for type parameter 'V'
                Diagnostic(ErrorCode.ERR_DuplicateBound, "U").WithArguments("U", "V").WithLocation(14, 52),
                // (14,62): error CS0405: Duplicate constraint 'C<T>.IB<T>' for type parameter 'V'
                Diagnostic(ErrorCode.ERR_DuplicateBound, "IB<T>").WithArguments("C<T>.IB<T>", "V").WithLocation(14, 62));
        }

        [Fact]
        public void DuplicateConstraintDifferencesOnPartialMethod()
        {
            var source =
@"interface I<T> { }
partial class C<T>
{
    partial void F<U>() where U : T, T, I<T>;
    partial void F<U>() where U : T, I<T>, I<T> { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,38): error CS0405: Duplicate constraint 'T' for type parameter 'U'
                //     partial void F<U>() where U : T, T, I<T>;
                Diagnostic(ErrorCode.ERR_DuplicateBound, "T").WithArguments("T", "U").WithLocation(4, 38),
                // (5,44): error CS0405: Duplicate constraint 'I<T>' for type parameter 'U'
                //     partial void F<U>() where U : T, I<T>, I<T> { }
                Diagnostic(ErrorCode.ERR_DuplicateBound, "I<T>").WithArguments("I<T>", "U").WithLocation(5, 44));
        }

        [Fact]
        public void ConstraintErrorMultiplePartialDeclarations_01()
        {
            var source =
@"interface I { }
class A { }
sealed class B { }
static class S { }
partial class C<T> where T : S { }
partial class C<T> where T : S { }
partial class D<T> where T : A, I { }
partial class D<T> where T : I, A { }
partial class D<T> where T : I, A { }
partial class E<T> where T : B { }
partial class E<T> where T : I, B { }
";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,30): error CS0717: 'S': static classes cannot be used as constraints
                // partial class C<T> where T : S { }
                Diagnostic(ErrorCode.ERR_ConstraintIsStaticClass, "S").WithArguments("S").WithLocation(5, 30),
                // (6,30): error CS0717: 'S': static classes cannot be used as constraints
                // partial class C<T> where T : S { }
                Diagnostic(ErrorCode.ERR_ConstraintIsStaticClass, "S").WithArguments("S").WithLocation(6, 30),
                // (7,15): error CS0265: Partial declarations of 'D<T>' have inconsistent constraints for type parameter 'T'
                // partial class D<T> where T : A, I { }
                Diagnostic(ErrorCode.ERR_PartialWrongConstraints, "D").WithArguments("D<T>", "T").WithLocation(7, 15),
                // (8,33): error CS0406: The class type constraint 'A' must come before any other constraints
                // partial class D<T> where T : I, A { }
                Diagnostic(ErrorCode.ERR_ClassBoundNotFirst, "A").WithArguments("A").WithLocation(8, 33),
                // (9,33): error CS0406: The class type constraint 'A' must come before any other constraints
                // partial class D<T> where T : I, A { }
                Diagnostic(ErrorCode.ERR_ClassBoundNotFirst, "A").WithArguments("A").WithLocation(9, 33),
                // (10,15): error CS0265: Partial declarations of 'E<T>' have inconsistent constraints for type parameter 'T'
                // partial class E<T> where T : B { }
                Diagnostic(ErrorCode.ERR_PartialWrongConstraints, "E").WithArguments("E<T>", "T").WithLocation(10, 15),
                // (10,30): error CS0701: 'B' is not a valid constraint. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                // partial class E<T> where T : B { }
                Diagnostic(ErrorCode.ERR_BadBoundType, "B").WithArguments("B").WithLocation(10, 30),
                // (11,33): error CS0701: 'B' is not a valid constraint. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                // partial class E<T> where T : I, B { }
                Diagnostic(ErrorCode.ERR_BadBoundType, "B").WithArguments("B").WithLocation(11, 33));
        }

        [Fact]
        public void ConstraintErrorMultiplePartialDeclarations_02()
        {
            var source =
@"sealed class A { }
partial class B<T, U> where T : A where U : T { }
partial class B<T, U> where T : A { }
partial class C<T, U> where U : T { }
partial class C<T, U> where T : A where U : T { }
";
            CreateCompilation(source).VerifyDiagnostics(
                // (2,15): error CS0265: Partial declarations of 'B<T, U>' have inconsistent constraints for type parameter 'U'
                // partial class B<T, U> where T : A where U : T { }
                Diagnostic(ErrorCode.ERR_PartialWrongConstraints, "B").WithArguments("B<T, U>", "U").WithLocation(2, 15),
                // (2,33): error CS0701: 'A' is not a valid constraint. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                // partial class B<T, U> where T : A where U : T { }
                Diagnostic(ErrorCode.ERR_BadBoundType, "A").WithArguments("A").WithLocation(2, 33),
                // (3,33): error CS0701: 'A' is not a valid constraint. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                // partial class B<T, U> where T : A { }
                Diagnostic(ErrorCode.ERR_BadBoundType, "A").WithArguments("A").WithLocation(3, 33),
                // (5,33): error CS0701: 'A' is not a valid constraint. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                // partial class C<T, U> where T : A where U : T { }
                Diagnostic(ErrorCode.ERR_BadBoundType, "A").WithArguments("A").WithLocation(5, 33));
        }

        [Fact]
        public void EffectiveBaseClass01()
        {
            var source =
@"class A
{
    internal object F;
}
class B : A
{
    internal object G;
}
class C : B
{
    internal object H;
}
class D<T> where T : B
{
    static void M<X, Y, Z>(X x, Y y, Z z)
        where X : A, T
        where Y : C, T
        where Z : C, X
    {
        object o;
        o = x.F;
        o = x.G;
        o = x.H;
        o = y.F;
        o = y.G;
        o = y.H;
        o = z.F;
        o = z.G;
        o = z.H;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (23,15): error CS1061: 'X' does not contain a definition for 'H' and no extension method 'H' accepting a first argument of type 'X' could be found (are you missing a using directive or an assembly reference?)
                //         o = x.H;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "H").WithArguments("X", "H"),
                // (3,21): warning CS0649: Field 'A.F' is never assigned to, and will always have its default value null
                //     internal object F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("A.F", "null"),
                // (7,21): warning CS0649: Field 'B.G' is never assigned to, and will always have its default value null
                //     internal object G;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "G").WithArguments("B.G", "null"),
                // (11,21): warning CS0649: Field 'C.H' is never assigned to, and will always have its default value null
                //     internal object H;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "H").WithArguments("C.H", "null")
                );
        }

        [Fact]
        public void EffectiveBaseClass02()
        {
            var source =
@"struct S { }
class A<T>
{
    public virtual void F<U>(U u) where U : T { }
    public void M(T t) { }
}
class B1 : A<int>
{
    public override void F<U>(U u)
    {
        int i = u;
        M(u);
    }
}
class B2 : A<S>
{
    public override void F<U>(U u)
    {
        S s = u;
        M(u);
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (11,17): error CS0029: Cannot implicitly convert type 'U' to 'int'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "u").WithArguments("U", "int").WithLocation(11, 17),
                // (12,11): error CS1503: Argument 1: cannot convert from 'U' to 'int'
                Diagnostic(ErrorCode.ERR_BadArgType, "u").WithArguments("1", "U", "int").WithLocation(12, 11),
                // (19,15): error CS0029: Cannot implicitly convert type 'U' to 'S'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "u").WithArguments("U", "S").WithLocation(19, 15),
                // (20,11): error CS1503: Argument 1: cannot convert from 'U' to 'S'
                Diagnostic(ErrorCode.ERR_BadArgType, "u").WithArguments("1", "U", "S").WithLocation(20, 11));
        }

        /// <summary>
        /// Should not be able to access members on constraint
        /// type if type is a struct (since effective base class
        /// should be nearest reference type in type hierarchy).
        /// </summary>
        [Fact]
        public void EffectiveBaseClass03()
        {
            var source =
@"struct S
{
    internal object F;
}
class C
{
    internal object F;
}
abstract class A<T1, T2>
{
    internal abstract void M<U1, U2>(U1 u1, U2 u2) where U1 : T1 where U2 : T2;
}
class B : A<S, C>
{
    internal override void M<U1, U2>(U1 u1, U2 u2)
    {
        u1.F = u2.F;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (17,12): error CS1061: 'U1' does not contain a definition for 'F' and no extension method 'F' accepting a first argument of type 'U1' could be found (are you missing a using directive or an assembly reference?)
                //         u1.F = u2.F;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "F").WithArguments("U1", "F"),
                // (3,21): warning CS0649: Field 'S.F' is never assigned to, and will always have its default value null
                //     internal object F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("S.F", "null"),
                // (7,21): warning CS0649: Field 'C.F' is never assigned to, and will always have its default value null
                //     internal object F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("C.F", "null")
                );
        }

        /// <summary>
        /// Should not be able to access protected
        /// members on constraint type.
        /// </summary>
        [Fact]
        public void EffectiveBaseClass04()
        {
            var source =
@"class A
{
    protected void M() { }
}
class B
{
}
class C<T, U>
    where T : A
    where U : B
{
    static void M(T t, U u)
    {
        t.M();
        u.M();
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (14,11): error CS0122: 'A.M()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "M").WithArguments("A.M()").WithLocation(14, 11),
                // (15,11): error CS1061: 'U' does not contain a definition for 'M' and no extension method 'M' accepting a first argument of type 'U' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("U", "M").WithLocation(15, 11));
        }

        [Fact]
        public void EffectiveInterfaceSet01()
        {
            var source =
@"using System.Collections.Generic;
abstract class A<T>
{
    internal abstract void M<U>(T t, U u) where U : T;
    internal static void M_Array(object[] o) { }
    internal static void M_IList(IList<object> o) { }
}
class B : A<object[]>
{
    internal override void M<U>(object[] t, U u)
    {
        M_Array(t);
        M_Array(u);
        M_IList(t);
        M_IList(u);
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (13,17): error CS1503: Argument 1: Argument 1: cannot convert from 'U' to 'object[]'
                Diagnostic(ErrorCode.ERR_BadArgType, "u").WithArguments("1", "U", "object[]").WithLocation(13, 17),
                // (15,17): error CS1503: Argument 1: Argument 1: cannot convert from 'U' to 'System.Collections.Generic.IList<object>'
                Diagnostic(ErrorCode.ERR_BadArgType, "u").WithArguments("1", "U", "System.Collections.Generic.IList<object>").WithLocation(15, 17));
        }

        /// <summary>
        /// Explicit interface implementations on class constraints
        /// should not be included in member lookup.
        /// </summary>
        [Fact]
        public void EffectiveInterfaceSet02()
        {
            var source =
@"interface I
{
    void M();
}
class A : I
{
    void I.M() { }
}
class B : I
{
    public void M() { }
}
class C
{
    static void M<T1, T2, T3, T4, T5>(A a, B b, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
        where T1 : A
        where T2 : A, I
        where T3 : B
        where T4 : T1
        where T5 : T1, I
    {
        a.M();
        b.M();
        t1.M();
        t2.M();
        t3.M();
        t4.M();
        t5.M();
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (22,11): error CS1061: 'A' does not contain a definition for 'M' and no extension method 'M' accepting a first argument of type 'A' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("A", "M").WithLocation(22, 11),
                // (24,12): error CS1061: 'T1' does not contain a definition for 'M' and no extension method 'M' accepting a first argument of type 'T1' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("T1", "M").WithLocation(24, 12),
                // (27,12): error CS1061: 'T4' does not contain a definition for 'M' and no extension method 'M' accepting a first argument of type 'U1' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("T4", "M").WithLocation(27, 12));
        }

        /// <summary>
        /// Class constraint members should hide
        /// interface constraint members.
        /// </summary>
        [ClrOnlyFact]
        public void EffectiveInterfaceSet03()
        {
            var source =
@"interface I
{
    void M();
}
class A
{
    internal void M() { System.Console.WriteLine(""A.M""); }
}
class B : A, I
{
    void  I.M() { System.Console.WriteLine(""B.M""); }
}
class C
{
    static void M<T1, T2, T3, T4, T5, T6>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
        where T1 : I
        where T2 : A
        where T3 : A, I
        where T4 : A, T1
        where T5 : T2, I
        where T6 : T1, T2
    {
        t1.M();
        t2.M();
        t3.M();
        t4.M();
        t5.M();
        t6.M();
    }
    static void Main()
    {
        var b = new B();
        M(b, b, b, b, b, b);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput:
@"B.M
A.M
A.M
A.M
A.M
A.M");
        }

        /// <summary>
        /// Method type inference should consider all
        /// interfaces reachable from the type parameter.
        /// </summary>
        [Fact]
        public void EffectiveInterfaceSet04()
        {
            var source =
@"interface IA<T> { }
interface IB<T> : IA<T> { }
class A : IA<object> { }
class C
{
    static void M<T>(IA<T> t)
    {
    }
    static void M<T, U>(T t, U u)
        where T : A
        where U : IB<object>
    {
        M(t);
        M(u);
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [ClrOnlyFact]
        public void ThrowT()
        {
            var source =
@"class C<T> where T : System.Exception
{
    static void ThrowT<U>() where U : T, new()
    {
        throw new U();
    }
    static void ThrowT(T t)
    {
        throw t;
    }
    static void RethrowT()
    {
        try
        {
        }
        catch (T)
        {
            throw;
        }
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C<T>.ThrowT(T)",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  throw
}");
        }

        [ClrOnlyFact]
        public void CatchT()
        {
            var source =
@"class C<T> where T : System.Exception
{
    static void M<U>() where U : T
    {
        try { }
        catch (T) { }
        try { }
        catch (U) { }
    }
}";
            CompileAndVerify(source);
        }

        [ClrOnlyFact]
        public void CatchTLifted()
        {
            var source =
@"
class C<T> where T : System.Exception
{
    public static void M<U>() where U : T
    {
        System.Action a = () =>
        {
            try { throw new System.ArgumentException(); }
            catch (T e) { System.Action aa = () => System.Console.WriteLine(e.ToString()); aa(); }

            try { throw new System.ArgumentException(); }
            catch (U e) { System.Action aa = () => System.Console.WriteLine(e.ToString()); aa(); }
        };

        a();
    }
}

class Test
{
    static void Main()
    {
        C<System.Exception>.M<System.ArgumentException>();
    }
}";
            CompileAndVerify(source);
        }

        [Fact]
        public void OverriddenConstraintTypes()
        {
            var source =
@"class A<T>
{
    internal virtual void M<U>() where U : T { }
}
// U as class type.
class B1 : A<object>
{
    internal override void M<U>() { }
}
// U as value type.
class B2 : A<int>
{
    internal override void M<U>() { }
}
// U as nullable type.
class B3 : A<int?>
{
    internal override void M<U>() { }
}
// U as enum type.
enum E { }
class B4 : A<E>
{
    internal override void M<U>() { }
}
// U as delegate type.
delegate void D();
class B5 : A<D>
{
    internal override void M<U>() { }
}
// U as array type.
class B6 : A<object[]>
{
    internal override void M<U>() { }
}
// U as interface type.
interface I { }
class B7 : A<I>
{
    internal override void M<U>() { }
}
// U as dynamic type.
class B8 : A<dynamic>
{
    internal override void M<U>() { }
}
// U as error type.
class B9 : A<Unknown>
{
    internal override void M<U>() { }
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (49,14): error CS0246: The type or namespace name 'Unknown' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Unknown").WithArguments("Unknown").WithLocation(49, 14));
        }

        [Fact]
        public void ErrorTypesInConstraints()
        {
            var source =
@"class A { }
// U depends on T where T has an error type.
class B<T, U>
    where T : X
    where U : I<T>
{
}
// T depends on a valid type and an error type.
class C<T>
    where T : A, Y
{
    // U depends on an error type and a valid type.
    void M<U>() where U : Z, A { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (10,18): error CS0246: The type or namespace name 'Y' could not be found (are you missing a using directive or an assembly reference?)
                //     where T : A, Y
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Y").WithArguments("Y").WithLocation(10, 18),
                // (4,15): error CS0246: The type or namespace name 'X' could not be found (are you missing a using directive or an assembly reference?)
                //     where T : X
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "X").WithArguments("X").WithLocation(4, 15),
                // (5,15): error CS0246: The type or namespace name 'I<>' could not be found (are you missing a using directive or an assembly reference?)
                //     where U : I<T>
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "I<T>").WithArguments("I<>").WithLocation(5, 15),
                // (13,27): error CS0246: The type or namespace name 'Z' could not be found (are you missing a using directive or an assembly reference?)
                //     void M<U>() where U : Z, A { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Z").WithArguments("Z").WithLocation(13, 27),
                // (13,30): error CS0406: The class type constraint 'A' must come before any other constraints
                //     void M<U>() where U : Z, A { }
                Diagnostic(ErrorCode.ERR_ClassBoundNotFirst, "A").WithArguments("A").WithLocation(13, 30));
        }

        [ClrOnlyFact]
        public void LookupObjectMembers()
        {
            var source =
@"interface IA { }
interface IB { }
class C<T> where T : IA, IB
{
    static string F<U>(T t, U u) where U : T
    {
        return t.ToString() + u.GetHashCode();
    }
    static string G<U, V>(U u, V v) where V : struct
    {
        return u.ToString() + v.GetHashCode();
    }
}";
            CompileAndVerify(source);
        }

        /// <summary>
        /// Handle constraints from metadata that
        /// would be invalid from source.
        /// </summary>
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void InvalidConstraintsFromMetadata()
        {
            var ilSource =
@".class public sealed Sealed { }
.class public abstract sealed Static { }
.class enum public Enum extends [mscorlib]System.Enum
{
  .field public int32 value__;
}
.class value public Struct extends [mscorlib]System.ValueType { }
.class public C1<([mscorlib]System.Object)T>
{
  .field public static object F;
}
.class public C2<([mscorlib]System.Enum)T>
{
  .field public static object F;
}
.class public C3<([mscorlib]System.ValueType)T>
{
  .field public static object F;
}
.class public C4<([mscorlib]System.Array)T>
{
  .field public static object F;
}
.class public C5<([mscorlib]System.Int32)T>
{
  .field public static object F;
}
.class public C6<(Sealed)T>
{
  .field public static object F;
}
.class public C7<(Static)T>
{
  .field public static object F;
}
.class public C8<(Enum)T>
{
  .field public static object F;
}
.class public C9<(Struct)T>
{
  .field public static object F;
}";
            var csharpSource =
@"class C
{
    static void M()
    {
        object o;
        // C1<T> where T : object
        o = C1<object>.F;
        o = C1<string>.F;
        // C2<T> where T : enum
        o = C2<Enum>.F;
        o = C2<string>.F;
        // C3<T> where T : System.ValueType
        o = C3<int>.F;
        o = C3<string>.F;
        // C4<T> where T : System.Array
        o = C4<object[]>.F;
        o = C4<string>.F;
        // C5<T> where T : int
        o = C5<int>.F;
        o = C5<string>.F;
        // C6<T> where T : sealed-type
        o = C6<Sealed>.F;
        o = C6<string>.F;
        // C7<T> where T : static-type
        o = C7<Static>.F;
        o = C7<string>.F;
        // C8<T> where T : enum-type
        o = C8<Enum>.F;
        o = C8<string>.F;
        // C9<T> where T : struct-type
        o = C9<Struct>.F;
        o = C9<string>.F;
    }
}";
            CreateCompilationWithILAndMscorlib40(csharpSource, ilSource).VerifyDiagnostics(
                // (11,16): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'C2<T>'. There is no implicit reference conversion from 'string' to 'System.Enum'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("C2<T>", "System.Enum", "T", "string").WithLocation(11, 16),
                // (14,16): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'C3<T>'. There is no implicit reference conversion from 'string' to 'System.ValueType'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("C3<T>", "System.ValueType", "T", "string").WithLocation(14, 16),
                // (17,16): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'C4<T>'. There is no implicit reference conversion from 'string' to 'System.Array'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("C4<T>", "System.Array", "T", "string").WithLocation(17, 16),
                // (20,16): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'C5<T>'. There is no implicit reference conversion from 'string' to 'int'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("C5<T>", "int", "T", "string").WithLocation(20, 16),
                // (23,16): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'C6<T>'. There is no implicit reference conversion from 'string' to 'Sealed'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("C6<T>", "Sealed", "T", "string").WithLocation(23, 16),
                // (25,16): error CS0718: 'Static': static types cannot be used as type arguments
                Diagnostic(ErrorCode.ERR_GenericArgIsStaticClass, "Static").WithArguments("Static").WithLocation(25, 16),
                // (26,16): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'C7<T>'. There is no implicit reference conversion from 'string' to 'Static'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("C7<T>", "Static", "T", "string").WithLocation(26, 16),
                // (29,16): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'C8<T>'. There is no implicit reference conversion from 'string' to 'Enum'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("C8<T>", "Enum", "T", "string").WithLocation(29, 16),
                // (32,16): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'C9<T>'. There is no implicit reference conversion from 'string' to 'Struct'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "string").WithArguments("C9<T>", "Struct", "T", "string").WithLocation(32, 16));
        }

        /// <summary>
        /// Use-site errors should be reported when a type or
        /// method from PE with a circular constraint is used.
        /// </summary>
        [Fact]
        public void UseSiteErrorCircularConstraints()
        {
            var ilSource =
@".class public A<(!T)T> { }
.class public B
{
    .method public static void M<(!!U)T, (!!T)U>() { ret }
}";
            var csharpSource =
@"class C
{
    static void M(A<object> a) { }
    static void M()
    {
        B.M<string, string>();
    }
}";
            CreateCompilationWithILAndMscorlib40(csharpSource, ilSource).VerifyDiagnostics(
                // (3,29): error CS0454: Circular constraint dependency involving 'T' and 'T'
                //     static void M(A<object> a) { }
                Diagnostic(ErrorCode.ERR_CircularConstraint, "a").WithArguments("T", "T").WithLocation(3, 29),
                // (6,11): error CS0454: Circular constraint dependency involving 'T' and 'U'
                //         B.M<string, string>();
                Diagnostic(ErrorCode.ERR_CircularConstraint, "M<string, string>").WithArguments("T", "U").WithLocation(6, 11));
        }

        /// <summary>
        /// Use-site errors should not be reported for a type or
        /// method from PE with a missing constraint type in
        /// addition to any conversion error satisfying constraints.
        /// </summary>
        [Fact]
        public void UseSiteErrorMissingConstraintType()
        {
            var ilSource =
@".assembly extern other {}
.class public A<([other]C)T> { }
.class public B
{
    .method public static void M<([other]C)U>() { ret }
}";
            var csharpSource =
@"class D
{
    static void M(A<object> a) { }
    static void M()
    {
        B.M<string>();
    }
}";
            // Note: for method overload resolution, methods with use-site errors
            // are ignored so there is no constraint error for B.M<string>().
            CreateCompilationWithILAndMscorlib40(csharpSource, ilSource).VerifyDiagnostics(
                // (3,29): error CS0311: The type 'object' cannot be used as type parameter 'T' in the generic type or method 'A<T>'. There is no implicit reference conversion from 'object' to 'C'.
                //     static void M(A<object> a) { }
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "a").WithArguments("A<T>", "C", "T", "object").WithLocation(3, 29),
                // (3,29): error CS0012: The type 'C' is defined in an assembly that is not referenced. You must add a reference to assembly 'other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     static void M(A<object> a) { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "a").WithArguments("C", "other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(3, 29),
                // (6,11): error CS0311: The type 'string' cannot be used as type parameter 'U' in the generic type or method 'B.M<U>()'. There is no implicit reference conversion from 'string' to 'C'.
                //         B.M<string>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M<string>").WithArguments("B.M<U>()", "C", "U", "string").WithLocation(6, 11),
                // (6,11): error CS0012: The type 'C' is defined in an assembly that is not referenced. You must add a reference to assembly 'other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         B.M<string>();
                Diagnostic(ErrorCode.ERR_NoTypeDef, "M<string>").WithArguments("C", "other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(6, 11));
        }

        [Fact]
        public void UseSiteErrorMissingConstraintTypeOverriddenMethod()
        {
            var source1 =
@"public interface IA { }
public class A { }";
            var compilation1 = CreateCompilation(source1, assemblyName: "d521fe98-c881-45cf-0788-249e00d004ea");
            compilation1.VerifyDiagnostics();
            var source2 =
@"public interface IB : IA { }
public class B : A { }
public interface IB<T> { }
public class B<T> { }
public interface IB1
{
    void M1<T, U>() where T : IA where U : B;
}
public interface IB2
{
    void M2<T, U>() where T : IB where U : A;
}
public interface IB3
{
    void M3<T, U>() where T : B<IA> where U : T, IB<A[]>;
}
public interface IB4<T, U>
{
    void M4<V, W>() where V : T where W : U;
}
public interface IB5 : IB4<A, IA>
{
}
public abstract class B1
{
    public abstract void M1<T, U>() where T : IA where U : B;
}
public abstract class B2
{
    public abstract void M2<T, U>() where T : IB where U : A;
}
public abstract class B3
{
    public abstract void M3<T, U>() where T : B<IA> where U : T, IB<A[]>;
}
public abstract class B4<T, U>
{
    public abstract void M4<V, W>() where V : T where W : U;
}
public abstract class B5 : B4<A, IA>
{
}";
            var compilation2 = CreateCompilation(source2, references: new MetadataReference[] { MetadataReference.CreateFromImage(compilation1.EmitToArray()) });
            compilation2.VerifyDiagnostics();
            var source3 =
@"class C1A : IB1
{
    void IB1.M1<T1A, U1A>() { }
}
class C2A : IB2
{
    void IB2.M2<T2A, U2A>() { }
}
class C3A : IB3
{
    void IB3.M3<T3A, U3A>() { }
}
class C1B : B1
{
    public override void M1<T1B, U1B>() { }
}
class C2B : B2
{
    public override void M2<T2B, U2B>() { }
}
class C3B : B3
{
    public override void M3<T3B, U3B>() { }
}
class C5B : B5
{
    public override void M4<T4B, U4B>() { }
}";
            var compilation3 = CreateCompilation(source3, references: new MetadataReference[] { MetadataReference.CreateFromImage(compilation2.EmitToArray()) });
            compilation3.VerifyDiagnostics(
                // (3,17): error CS0012: The type 'IA' is defined in an assembly that is not referenced. You must add a reference to assembly 'd521fe98-c881-45cf-0788-249e00d004ea, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     void IB1.M1<T1A, U1A>() { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "T1A").WithArguments("IA", "d521fe98-c881-45cf-0788-249e00d004ea, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(3, 17),
                // (7,22): error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'd521fe98-c881-45cf-0788-249e00d004ea, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     void IB2.M2<T2A, U2A>() { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "U2A").WithArguments("A", "d521fe98-c881-45cf-0788-249e00d004ea, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 22),
                // (11,17): error CS0012: The type 'IA' is defined in an assembly that is not referenced. You must add a reference to assembly 'd521fe98-c881-45cf-0788-249e00d004ea, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     void IB3.M3<T3A, U3A>() { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "T3A").WithArguments("IA", "d521fe98-c881-45cf-0788-249e00d004ea, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(11, 17),
                // (11,22): error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'd521fe98-c881-45cf-0788-249e00d004ea, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     void IB3.M3<T3A, U3A>() { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "U3A").WithArguments("A", "d521fe98-c881-45cf-0788-249e00d004ea, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(11, 22),
                // (15,29): error CS0012: The type 'IA' is defined in an assembly that is not referenced. You must add a reference to assembly 'd521fe98-c881-45cf-0788-249e00d004ea, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override void M1<T1B, U1B>() { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "T1B").WithArguments("IA", "d521fe98-c881-45cf-0788-249e00d004ea, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(15, 29),
                // (19,34): error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'd521fe98-c881-45cf-0788-249e00d004ea, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override void M2<T2B, U2B>() { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "U2B").WithArguments("A", "d521fe98-c881-45cf-0788-249e00d004ea, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(19, 34),
                // (23,29): error CS0012: The type 'IA' is defined in an assembly that is not referenced. You must add a reference to assembly 'd521fe98-c881-45cf-0788-249e00d004ea, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override void M3<T3B, U3B>() { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "T3B").WithArguments("IA", "d521fe98-c881-45cf-0788-249e00d004ea, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(23, 29),
                // (23,34): error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'd521fe98-c881-45cf-0788-249e00d004ea, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override void M3<T3B, U3B>() { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "U3B").WithArguments("A", "d521fe98-c881-45cf-0788-249e00d004ea, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(23, 34),
                // (25,13): error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'd521fe98-c881-45cf-0788-249e00d004ea, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                // class C5B : B5
                Diagnostic(ErrorCode.ERR_NoTypeDef, "B5").WithArguments("A", "d521fe98-c881-45cf-0788-249e00d004ea, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(25, 13),
                // (27,29): error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'd521fe98-c881-45cf-0788-249e00d004ea, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override void M4<T4B, U4B>() { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "T4B").WithArguments("A", "d521fe98-c881-45cf-0788-249e00d004ea, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(27, 29),
                // (27,34): error CS0012: The type 'IA' is defined in an assembly that is not referenced. You must add a reference to assembly 'd521fe98-c881-45cf-0788-249e00d004ea, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     public override void M4<T4B, U4B>() { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "U4B").WithArguments("IA", "d521fe98-c881-45cf-0788-249e00d004ea, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(27, 34));
        }

        /// <summary>
        /// If a type parameter from metadata has multiple errors
        /// including a missing constraint type (a use-site error on
        /// the constraint type), the missing constraint type should
        /// be reported as the use-site error for the type parameter.
        /// </summary>
        [Fact]
        public void UseSiteErrorMissingConstraintTypeAndCircularConstraint()
        {
            var ilSource =
@".assembly extern other {}
.class public A1<([other]B1, !T)T> { }
.class public A2<([other]B2, [other]I)T> { }
.class public A3
{
  .method static public hidebysig void M<([other]B3, !!T)T>() { ret }
}";
            var csharpSource =
@"class C
{
    static void M(A1<object> a) { }
    static void M(A2<object> a) { }
    static void M()
    {
        A3.M<object>();
    }
}";
            CreateCompilationWithILAndMscorlib40(csharpSource, ilSource).VerifyDiagnostics(
                // (4,30): error CS0311: The type 'object' cannot be used as type parameter 'T' in the generic type or method 'A2<T>'. There is no implicit reference conversion from 'object' to 'B2'.
                //     static void M(A2<object> a) { }
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "a").WithArguments("A2<T>", "B2", "T", "object").WithLocation(4, 30),
                // (4,30): error CS0311: The type 'object' cannot be used as type parameter 'T' in the generic type or method 'A2<T>'. There is no implicit reference conversion from 'object' to 'I'.
                //     static void M(A2<object> a) { }
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "a").WithArguments("A2<T>", "I", "T", "object").WithLocation(4, 30),
                // (4,30): error CS0012: The type 'B2' is defined in an assembly that is not referenced. You must add a reference to assembly 'other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     static void M(A2<object> a) { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "a").WithArguments("B2", "other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(4, 30),
                // (4,30): error CS0012: The type 'I' is defined in an assembly that is not referenced. You must add a reference to assembly 'other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     static void M(A2<object> a) { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "a").WithArguments("I", "other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(4, 30),
                // (3,30): error CS0311: The type 'object' cannot be used as type parameter 'T' in the generic type or method 'A1<T>'. There is no implicit reference conversion from 'object' to 'B1'.
                //     static void M(A1<object> a) { }
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "a").WithArguments("A1<T>", "B1", "T", "object").WithLocation(3, 30),
                // (3,30): error CS0454: Circular constraint dependency involving 'T' and 'T'
                //     static void M(A1<object> a) { }
                Diagnostic(ErrorCode.ERR_CircularConstraint, "a").WithArguments("T", "T").WithLocation(3, 30),
                // (3,30): error CS0012: The type 'B1' is defined in an assembly that is not referenced. You must add a reference to assembly 'other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     static void M(A1<object> a) { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "a").WithArguments("B1", "other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(3, 30),
                // (7,12): error CS0311: The type 'object' cannot be used as type parameter 'T' in the generic type or method 'A3.M<T>()'. There is no implicit reference conversion from 'object' to 'B3'.
                //         A3.M<object>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M<object>").WithArguments("A3.M<T>()", "B3", "T", "object").WithLocation(7, 12),
                // (7,12): error CS0454: Circular constraint dependency involving 'T' and 'T'
                //         A3.M<object>();
                Diagnostic(ErrorCode.ERR_CircularConstraint, "M<object>").WithArguments("T", "T").WithLocation(7, 12),
                // (7,12): error CS0012: The type 'B3' is defined in an assembly that is not referenced. You must add a reference to assembly 'other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         A3.M<object>();
                Diagnostic(ErrorCode.ERR_NoTypeDef, "M<object>").WithArguments("B3", "other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 12));
        }

        // Same as UseSiteErrorMissingConstraintTypeAndCircularConstraint but
        // with use-site errors from retargeting symbols rather than PE symbols.
        [Fact]
        public void RetargetingUseSiteErrorMissingConstraintTypeAndCircularConstraint()
        {
            var source1 =
@"public class B1 { }
public class B2 { }
public class B3 { }
public interface I { }";
            var compilation1 = CreateCompilation(source1, assemblyName: "d521fe98-c881-45cf-8870-249e00ae400d");
            compilation1.VerifyDiagnostics();
            var source2 =
@"public class A1<T> where T : B1, T { }
public class A2<T> where T : B2, I { }
public class A3
{
    public static void M<T>() where T : B3, T { }
}";
            var compilation2 = CreateCompilation(source2, assemblyName: "d03a3229-eb22-4682-88df-77efaa348e3b", references: new MetadataReference[] { new CSharpCompilationReference(compilation1) });
            compilation2.VerifyDiagnostics(
                // (1,17): error CS0454: Circular constraint dependency involving 'T' and 'T'
                Diagnostic(ErrorCode.ERR_CircularConstraint, "T").WithArguments("T", "T").WithLocation(1, 17),
                // (5,26): error CS0454: Circular constraint dependency involving 'T' and 'T'
                Diagnostic(ErrorCode.ERR_CircularConstraint, "T").WithArguments("T", "T").WithLocation(5, 26));
            var source3 =
@"class C
{
    static void M(A1<object> a) { }
    static void M(A2<object> a) { }
    static void M()
    {
        A3.M<object>();
    }
}";
            var compilation3 = CreateCompilation(source3, references: new MetadataReference[] { new CSharpCompilationReference(compilation2) });
            compilation3.VerifyDiagnostics(
                // (4,30): error CS0311: The type 'object' cannot be used as type parameter 'T' in the generic type or method 'A2<T>'. There is no implicit reference conversion from 'object' to 'B2'.
                //     static void M(A2<object> a) { }
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "a").WithArguments("A2<T>", "B2", "T", "object").WithLocation(4, 30),
                // (4,30): error CS0311: The type 'object' cannot be used as type parameter 'T' in the generic type or method 'A2<T>'. There is no implicit reference conversion from 'object' to 'I'.
                //     static void M(A2<object> a) { }
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "a").WithArguments("A2<T>", "I", "T", "object").WithLocation(4, 30),
                // (4,30): error CS0012: The type 'B2' is defined in an assembly that is not referenced. You must add a reference to assembly 'd521fe98-c881-45cf-8870-249e00ae400d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     static void M(A2<object> a) { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "a").WithArguments("B2", "d521fe98-c881-45cf-8870-249e00ae400d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(4, 30),
                // (4,30): error CS0012: The type 'I' is defined in an assembly that is not referenced. You must add a reference to assembly 'd521fe98-c881-45cf-8870-249e00ae400d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     static void M(A2<object> a) { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "a").WithArguments("I", "d521fe98-c881-45cf-8870-249e00ae400d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(4, 30),
                // (3,30): error CS0311: The type 'object' cannot be used as type parameter 'T' in the generic type or method 'A1<T>'. There is no implicit reference conversion from 'object' to 'B1'.
                //     static void M(A1<object> a) { }
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "a").WithArguments("A1<T>", "B1", "T", "object").WithLocation(3, 30),
                // (3,30): error CS0012: The type 'B1' is defined in an assembly that is not referenced. You must add a reference to assembly 'd521fe98-c881-45cf-8870-249e00ae400d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     static void M(A1<object> a) { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "a").WithArguments("B1", "d521fe98-c881-45cf-8870-249e00ae400d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(3, 30),
                // (7,12): error CS0311: The type 'object' cannot be used as type parameter 'T' in the generic type or method 'A3.M<T>()'. There is no implicit reference conversion from 'object' to 'B3'.
                //         A3.M<object>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M<object>").WithArguments("A3.M<T>()", "B3", "T", "object").WithLocation(7, 12),
                // (7,12): error CS0012: The type 'B3' is defined in an assembly that is not referenced. You must add a reference to assembly 'd521fe98-c881-45cf-8870-249e00ae400d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         A3.M<object>();
                Diagnostic(ErrorCode.ERR_NoTypeDef, "M<object>").WithArguments("B3", "d521fe98-c881-45cf-8870-249e00ae400d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 12));
        }

        [WorkItem(542753, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542753")]
        [Fact]
        public void MissingTypeInVariantInterfaceConstraint()
        {
            var ilSource =
@".assembly extern other {}
.class interface public abstract I<T> { }
.class interface public abstract IIn<-T> { }
.class interface public abstract IOut<+T> { }
.class public A0<(class [other]B)T> { }
.class public A<(class I<class [other]B>)T> { }
.class public AIn<(class IIn<class [other]B>)T> { }
.class public AOut<(class IOut<class [other]B>)T> { }";
            var csharpSource =
@"class C
{
    static void M(A0<object> o) { }
    static void M(A<I<object>> o) { }
    static void M(AIn<IIn<object>> o) { }
    static void M(AOut<IOut<object>> o) { }
}";
            CreateCompilationWithILAndMscorlib40(csharpSource, ilSource).VerifyDiagnostics(
                // (3,30): error CS0012: The type 'B' is defined in an assembly that is not referenced. You must add a reference to assembly 'other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     static void M(A0<object> o) { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "o").WithArguments("B", "other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(3, 30),
                // (3,30): error CS0311: The type 'object' cannot be used as type parameter 'T' in the generic type or method 'A0<T>'. There is no implicit reference conversion from 'object' to 'B'.
                //     static void M(A0<object> o) { }
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "o").WithArguments("A0<T>", "B", "T", "object").WithLocation(3, 30),
                // (4,32): error CS0012: The type 'B' is defined in an assembly that is not referenced. You must add a reference to assembly 'other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     static void M(A<I<object>> o) { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "o").WithArguments("B", "other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(4, 32),
                // (4,32): error CS0311: The type 'I<object>' cannot be used as type parameter 'T' in the generic type or method 'A<T>'. There is no implicit reference conversion from 'I<object>' to 'I<B>'.
                //     static void M(A<I<object>> o) { }
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "o").WithArguments("A<T>", "I<B>", "T", "I<object>").WithLocation(4, 32),
                // (5,36): error CS0012: The type 'B' is defined in an assembly that is not referenced. You must add a reference to assembly 'other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     static void M(AIn<IIn<object>> o) { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "o").WithArguments("B", "other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(5, 36),
                // (5,36): error CS0311: The type 'I<object>' cannot be used as type parameter 'T' in the generic type or method 'AIn<T>'. There is no implicit reference conversion from 'IIn<object>' to 'IIn<B>'.
                //     static void M(AIn<IIn<object>> o) { }
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "o").WithArguments("AIn<T>", "IIn<B>", "T", "IIn<object>").WithLocation(5, 36),
                // (6,38): error CS0012: The type 'B' is defined in an assembly that is not referenced. You must add a reference to assembly 'other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //     static void M(AOut<IOut<object>> o) { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "o").WithArguments("B", "other, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(6, 38),
                // (6,38): error CS0311: The type 'I<object>' cannot be used as type parameter 'T' in the generic type or method 'AOut<T>'. There is no implicit reference conversion from 'IOut<object>' to 'IOut<B>'.
                //     static void M(AOut<IOut<object>> o) { }
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "o").WithArguments("AOut<T>", "IOut<B>", "T", "IOut<object>").WithLocation(6, 38));
        }

        /// <summary>
        /// Similar to above but with unrecognized type
        /// rather than missing type, and all in source.
        /// </summary>
        [Fact]
        public void UnrecognizedTypeInVariantInterfaceConstraint()
        {
            var source =
@"interface I<T> { }
interface IIn<in T> { }
interface IOut<out T> { }
class A0<T> where T : B { }
class A<T> where T : I<B> { }
class AIn<T> where T : IIn<B> { }
class AOut<T> where T : IOut<B> { }
class C
{
    static void M(A0<object> o) { }
    static void M(A<I<object>> o) { }
    static void M(AIn<IIn<object>> o) { }
    static void M(AOut<IOut<object>> o) { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,23): error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "B").WithArguments("B").WithLocation(4, 23),
                // (5,24): error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "B").WithArguments("B").WithLocation(5, 24),
                // (6,28): error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "B").WithArguments("B").WithLocation(6, 28),
                // (7,30): error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "B").WithArguments("B").WithLocation(7, 30),
                // (10,30): error CS0311: The type 'object' cannot be used as type parameter 'T' in the generic type or method 'A0<T>'. There is no implicit reference conversion from 'object' to 'B'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "o").WithArguments("A0<T>", "B", "T", "object").WithLocation(10, 30),
                // (11,32): error CS0311: The type 'I<object>' cannot be used as type parameter 'T' in the generic type or method 'A<T>'. There is no implicit reference conversion from 'I<object>' to 'I<B>'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "o").WithArguments("A<T>", "I<B>", "T", "I<object>").WithLocation(11, 32),
                // (12,36): error CS0311: The type 'I<object>' cannot be used as type parameter 'T' in the generic type or method 'AIn<T>'. There is no implicit reference conversion from 'IIn<object>' to 'IIn<B>'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "o").WithArguments("AIn<T>", "IIn<B>", "T", "IIn<object>").WithLocation(12, 36),
                // (13,38): error CS0311: The type 'I<object>' cannot be used as type parameter 'T' in the generic type or method 'AOut<T>'. There is no implicit reference conversion from 'IOut<object>' to 'IOut<B>'.
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "o").WithArguments("AOut<T>", "IOut<B>", "T", "IOut<object>").WithLocation(13, 38));
        }

        [WorkItem(542174, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542174")]
        [ClrOnlyFact]
        public void ConstraintsOnOverriddenMethod()
        {
            var source =
@"interface I<T> where T : class { }
abstract class A
{
    public abstract I<T> F<T>() where T : class;
}
class B: A
{
    public override I<U> F<U>() { return null; }
}";
            var comp = CreateCompilation(source);
            CompileAndVerify(comp);
            var method = comp.GetMember<MethodSymbol>("B.F");
            Assert.Equal("I<U> B.F<U>() where U : class", method.ToDisplayString(SymbolDisplayFormat.TestFormatWithConstraints));
        }

        [WorkItem(542264, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542264")]
        [Fact]
        public void PartialMethodsDifferentTypeParameterNames()
        {
            var source =
@"interface I<T> { }
partial class C
{
    partial void M<T, U>(T t, U u)
        where T : U
        where U : I<T>;
    partial void M<X, Y>(X x, Y y)
        where X : Y
        where Y : I<X>
    {
        x.ToString();
        y.ToString();
        t.ToString();
        u.ToString();
    }
    partial void M<T1, T2>(T1 t1, T2 t2)
        where T1 : T2
        where T2 : I<T1>;
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (13,9): error CS0103: The name 't' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "t").WithArguments("t").WithLocation(13, 9),
                // (14,9): error CS0103: The name 'u' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "u").WithArguments("u").WithLocation(14, 9),
                // (16,18): error CS0756: A partial method may not have multiple defining declarations
                Diagnostic(ErrorCode.ERR_PartialMethodOnlyOneLatent, "M").WithLocation(16, 18));
        }

        [WorkItem(542331, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542331")]
        [Fact]
        public void InterfaceImplementationMismatchNewMethod()
        {
            var source =
@"interface I
{
    void M<T>() where T : class;
}
class A
{
    public void M<T>() where T : class { }
}
class B : A
{
    public new void M<T>() { }
}
class C1 : B, I
{
}
class C2 : A, I
{
    public new void M<T>() { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (13,15): error CS0425: The constraints for type parameter 'T' of method 'B.M<T>()' must match the constraints for type parameter 'T' of interface method 'I.M<T>()'. Consider using an explicit interface implementation instead.
                // class C1 : B, I
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "I").WithArguments("T", "B.M<T>()", "T", "I.M<T>()").WithLocation(13, 15),
                // (18,21): error CS0425: The constraints for type parameter 'T' of method 'C2.M<T>()' must match the constraints for type parameter 'T' of interface method 'I.M<T>()'. Consider using an explicit interface implementation instead.
                //     public new void M<T>() { }
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "M").WithArguments("T", "C2.M<T>()", "T", "I.M<T>()").WithLocation(18, 21));
        }

        /// <summary>
        /// Same as above but with implementing class from metadata.
        /// </summary>
        [Fact]
        public void InterfaceImplementationMismatchNewMethodMetadata()
        {
            var ilSource =
@".class interface public abstract I
{
  .method public hidebysig newslot abstract virtual instance void M<class T>() { }
}
.class public A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public hidebysig instance void M<class T>() { ret }
}
.class public B extends A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public hidebysig instance void M<T>() { ret }
}
.class public C1 extends B implements I
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
}";
            var csharpSource =
@"class C2 : B, I { }
class D
{
    static void M1(I i) { }
    static void M2()
    {
        M1(new C1());
        M1(new C2());
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(csharpSource, ilSource);
            compilation.VerifyDiagnostics(
                // (1,15): error CS0425: The constraints for type parameter 'T' of method 'B.M<T>()' must match the constraints for type parameter 'T' of interface method 'I.M<T>()'. Consider using an explicit interface implementation instead.
                // class C2 : B, I { }
                Diagnostic(ErrorCode.ERR_ImplBadConstraints, "I").WithArguments("T", "B.M<T>()", "T", "I.M<T>()").WithLocation(1, 15));

            // Arguably, B.M<T> should not be considered an implementation of
            // I.M<T> since the CLR does not consider it so. For now, however,
            // FindImplementationForInterfaceMember returns B.M<T>.
            var globalNamespace = compilation.GlobalNamespace;
            var im = globalNamespace.GetMember<NamedTypeSymbol>("I").GetMember<MethodSymbol>("M");
            var bx = globalNamespace.GetMember<NamedTypeSymbol>("B").GetMember<MethodSymbol>("M");
            var c1 = globalNamespace.GetMember<NamedTypeSymbol>("C1");
            var impl = c1.FindImplementationForInterfaceMember(im);
            Assert.Equal(bx, impl);
        }

        [WorkItem(528855, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528855")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void ModReqsInConstraintsAreNotSupported()
        {
            var ilSource =
@".class public A
{
}
.class interface public abstract I
{
    .method public abstract virtual instance void M<(class A modreq(int32))T>() { }
}
.class interface public abstract IT<(class A modreq(int32))T>
{
}";
            var csharpSource =
@"class C1 : I
{
    void I.M<T>() { }
}
class C2 : IT<A>
{
}
class C<T> : IT<T>
{
    void M<U>() where U : T { }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(csharpSource, ilSource);
            compilation.VerifyDiagnostics(
                // (3,14): error CS0648: '' is a type not supported by the language
                //     void I.M<T>() { }
                Diagnostic(ErrorCode.ERR_BogusType, "T").WithArguments("").WithLocation(3, 14),
                // (5,7): error CS0311: The type 'A' cannot be used as type parameter 'T' in the generic type or method 'IT<T>'. There is no implicit reference conversion from 'A' to '?'.
                // class C2 : IT<A>
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "C2").WithArguments("IT<T>", "?", "T", "A").WithLocation(5, 7),
                // (5,7): error CS0648: '' is a type not supported by the language
                // class C2 : IT<A>
                Diagnostic(ErrorCode.ERR_BogusType, "C2").WithArguments("").WithLocation(5, 7),
                // (8,7): error CS0314: The type 'T' cannot be used as type parameter 'T' in the generic type or method 'IT<T>'. There is no boxing conversion or type parameter conversion from 'T' to '?'.
                // class C<T> : IT<T>
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar, "C").WithArguments("IT<T>", "?", "T", "T").WithLocation(8, 7),
                // (8,7): error CS0648: '' is a type not supported by the language
                // class C<T> : IT<T>
                Diagnostic(ErrorCode.ERR_BogusType, "C").WithArguments("").WithLocation(8, 7));

            var m = ((NamedTypeSymbol)compilation.GetMember("C1")).GetMember("I.M");
            var constraintType = ((SourceOrdinaryMethodSymbol)m).TypeParameters[0].ConstraintTypesNoUseSiteDiagnostics[0].Type;
            Assert.IsType<UnsupportedMetadataTypeSymbol>(constraintType);
            Assert.False(((INamedTypeSymbol)constraintType).IsSerializable);
        }

        /// <summary>
        /// Constraints with modopts are treated as unsupported types.
        /// (The native compiler imports constraints with modopts but
        /// generates invalid types when implementing or overriding
        /// generic methods with such constraints.)
        /// </summary>
        [WorkItem(528856, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528856")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void ModOptsInConstraintsAreIgnored()
        {
            var ilSource =
@".class public A
{
    .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
    .method public virtual instance void M<(class A modopt(A) modopt(int32))T>() { ret }
}
.class interface public abstract I<(class A modopt(A))T>
{
    .method public abstract virtual instance void M<(!T modopt(int32))U>() { }
}";
            var csharpSource =
@"class B : A
{
    public override void M<T>() { }
}
class C : I<A>
{
    void I<A>.M<T>() { }
}
class P
{
    static void Main()
    {
        new A().M<A>();
        new B().M<A>();
        ((I<A>)new C()).M<A>();
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(csharpSource, ilSource);
            compilation.VerifyDiagnostics(
                // (5,7): error CS0311: The type 'A' cannot be used as type parameter 'T' in the generic type or method 'I<T>'. There is no implicit reference conversion from 'A' to '?'.
                // class C : I<A>
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "C").WithArguments("I<T>", "?", "T", "A").WithLocation(5, 7),
                // (5,7): error CS0648: '' is a type not supported by the language
                // class C : I<A>
                Diagnostic(ErrorCode.ERR_BogusType, "C").WithArguments("").WithLocation(5, 7),
                // (7,10): error CS0311: The type 'A' cannot be used as type parameter 'T' in the generic type or method 'I<T>'. There is no implicit reference conversion from 'A' to '?'.
                //     void I<A>.M<T>() { }
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "I<A>").WithArguments("I<T>", "?", "T", "A").WithLocation(7, 10),
                // (7,10): error CS0648: '' is a type not supported by the language
                //     void I<A>.M<T>() { }
                Diagnostic(ErrorCode.ERR_BogusType, "I<A>").WithArguments("").WithLocation(7, 10),
                // (7,17): error CS0648: '' is a type not supported by the language
                //     void I<A>.M<T>() { }
                Diagnostic(ErrorCode.ERR_BogusType, "T").WithArguments("").WithLocation(7, 17),
                // (13,17): error CS0311: The type 'A' cannot be used as type parameter 'T' in the generic type or method 'A.M<T>()'. There is no implicit reference conversion from 'A' to '?'.
                //         new A().M<A>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M<A>").WithArguments("A.M<T>()", "?", "T", "A").WithLocation(13, 17),
                // (13,17): error CS0648: '' is a type not supported by the language
                //         new A().M<A>();
                Diagnostic(ErrorCode.ERR_BogusType, "M<A>").WithArguments("").WithLocation(13, 17),
                // (15,13): error CS0311: The type 'A' cannot be used as type parameter 'T' in the generic type or method 'I<T>'. There is no implicit reference conversion from 'A' to '?'.
                //         ((I<A>)new C()).M<A>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "A").WithArguments("I<T>", "?", "T", "A").WithLocation(15, 13),
                // (15,13): error CS0648: '' is a type not supported by the language
                //         ((I<A>)new C()).M<A>();
                Diagnostic(ErrorCode.ERR_BogusType, "A").WithArguments("").WithLocation(15, 13),
                // (15,25): error CS0311: The type 'A' cannot be used as type parameter 'U' in the generic type or method 'I<A>.M<U>()'. There is no implicit reference conversion from 'A' to '?'.
                //         ((I<A>)new C()).M<A>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M<A>").WithArguments("I<A>.M<U>()", "?", "U", "A").WithLocation(15, 25),
                // (15,25): error CS0648: '' is a type not supported by the language
                //         ((I<A>)new C()).M<A>();
                Diagnostic(ErrorCode.ERR_BogusType, "M<A>").WithArguments("").WithLocation(15, 25));
        }

        /// <summary>
        /// Constraints on the nested type must match
        /// constraints from the containing types.
        /// </summary>
        [WorkItem(528859, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528859")]
        [Fact]
        public void InconsistentConstraintsAreNotSupported()
        {
            var ilSource =
@".class public A
{
  .method public specialname rtspecialname instance void .ctor() { ret }
}
.class interface abstract public I { }
.class interface abstract public IT<T>
{
  .class interface abstract nested public IU<U> { }
  .class interface abstract nested public ITU<T, U> { }
  .class interface abstract nested public ITU2<(!U)T, U> { }
  .class interface abstract nested public IT<T>
  {
    .class interface abstract nested public IAI<(A, I)T> { }
  }
  .class interface abstract nested public IAI<(A, I)T>
  {
    .class interface abstract nested public IT<T> { }
    .class interface abstract nested public IAI<(A, I)T> { }
  }
  .class interface abstract nested public IF<class .ctor T> { }
  .class interface abstract nested public IIn<-T> { }
}
.class interface abstract public ITU<T, (!T)U>
{
  .class interface abstract nested public ITU<T, (!T)U> { }
  .class interface abstract nested public ITU2<(!U)T, U> { }
}
.class abstract interface public IAI<(A, I)T>
{
  .class interface abstract nested public IT<T>
  {
    .class interface abstract nested public IAI<(A, I)T> { }
  }
  .class nested public CIA<(I, A)T>
  {
    .method public specialname rtspecialname instance void .ctor() { ret }
  }
}
.class public CF<class .ctor T>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .class nested public CT<T>
  {
    .method public specialname rtspecialname instance void .ctor() { ret }
  }
  .class interface abstract nested public IF<.ctor class T> { }
}
.class interface abstract public IIn<-T>
{
  .class interface abstract nested public IT<T> { }
  .class interface abstract nested public IInU<-U> { }
  .class interface abstract nested public IOut<+T> { }
}";
            var csharpSource =
@"class C : A, I { }
class P
{
    static void M()
    {
        object o;
        o = typeof(IT<object>);
        o = typeof(IT<object>.IU);
        o = typeof(IT<object>.ITU<int>);
        o = typeof(IT<object>.ITU2<object>); // CS0648
        o = typeof(IT<object>.IT.IAI); // CS0648
        o = typeof(IT<object>.IAI); // CS0648
        o = typeof(IT<object>.IAI.IT); // CS0648
        o = typeof(IT<object>.IAI.IAI); // CS0648
        o = typeof(IT<object>.IF); // CS0648
        o = typeof(IT<object>.IIn); // CS0648 (not reported by Dev11)
        o = typeof(ITU<object, object>.ITU);
        o = typeof(ITU<object, object>.ITU2); // CS0648
        o = typeof(IAI<C>);
        o = typeof(IAI<C>.IT); // CS0648
        o = typeof(IAI<C>.IT.IAI); // CS0648
        o = typeof(IAI<C>.CIA);
        o = typeof(CF<C>);
        o = typeof(CF<C>.CT); // CS0648
        o = typeof(CF<C>.IF);
        o = typeof(IIn<object>);
        o = typeof(IIn<object>.IT); // CS0648 (not reported by Dev11)
        o = typeof(IIn<object>.IInU);
        o = typeof(IIn<object>.IOut); // CS0648 (not reported by Dev11)
    }
}";
            var compilation1 = CreateCompilationWithILAndMscorlib40(csharpSource, ilSource);
            compilation1.VerifyDiagnostics(
                // (10,31): error CS0648: 'IT<T>.ITU2<U>' is a type not supported by the language
                //         o = typeof(IT<object>.ITU2<object>); // CS0648
                Diagnostic(ErrorCode.ERR_BogusType, "ITU2<object>").WithArguments("IT<T>.ITU2<U>"),
                // (11,34): error CS0648: 'IT<T>.IT.IAI' is a type not supported by the language
                //         o = typeof(IT<object>.IT.IAI); // CS0648
                Diagnostic(ErrorCode.ERR_BogusType, "IAI").WithArguments("IT<T>.IT.IAI"),
                // (12,31): error CS0648: 'IT<T>.IAI' is a type not supported by the language
                //         o = typeof(IT<object>.IAI); // CS0648
                Diagnostic(ErrorCode.ERR_BogusType, "IAI").WithArguments("IT<T>.IAI"),
                // (13,31): error CS0648: 'IT<T>.IAI' is a type not supported by the language
                //         o = typeof(IT<object>.IAI.IT); // CS0648
                Diagnostic(ErrorCode.ERR_BogusType, "IAI").WithArguments("IT<T>.IAI"),
                // (14,31): error CS0648: 'IT<T>.IAI' is a type not supported by the language
                //         o = typeof(IT<object>.IAI.IAI); // CS0648
                Diagnostic(ErrorCode.ERR_BogusType, "IAI").WithArguments("IT<T>.IAI"),
                // (14,35): error CS0648: 'IT<T>.IAI.IAI' is a type not supported by the language
                //         o = typeof(IT<object>.IAI.IAI); // CS0648
                Diagnostic(ErrorCode.ERR_BogusType, "IAI").WithArguments("IT<T>.IAI.IAI"),
                // (15,31): error CS0648: 'IT<T>.IF' is a type not supported by the language
                //         o = typeof(IT<object>.IF); // CS0648
                Diagnostic(ErrorCode.ERR_BogusType, "IF").WithArguments("IT<T>.IF"),
                // (16,31): error CS0648: 'IT<T>.IIn' is a type not supported by the language
                //         o = typeof(IT<object>.IIn); // CS0648 (not reported by Dev11)
                Diagnostic(ErrorCode.ERR_BogusType, "IIn").WithArguments("IT<T>.IIn"),
                // (18,40): error CS0648: 'ITU<T, U>.ITU2' is a type not supported by the language
                //         o = typeof(ITU<object, object>.ITU2); // CS0648
                Diagnostic(ErrorCode.ERR_BogusType, "ITU2").WithArguments("ITU<T, U>.ITU2"),
                // (20,27): error CS0648: 'IAI<T>.IT' is a type not supported by the language
                //         o = typeof(IAI<C>.IT); // CS0648
                Diagnostic(ErrorCode.ERR_BogusType, "IT").WithArguments("IAI<T>.IT"),
                // (21,27): error CS0648: 'IAI<T>.IT' is a type not supported by the language
                //         o = typeof(IAI<C>.IT.IAI); // CS0648
                Diagnostic(ErrorCode.ERR_BogusType, "IT").WithArguments("IAI<T>.IT"),
                // (24,26): error CS0648: 'CF<T>.CT' is a type not supported by the language
                //         o = typeof(CF<C>.CT); // CS0648
                Diagnostic(ErrorCode.ERR_BogusType, "CT").WithArguments("CF<T>.CT"),
                // (27,32): error CS0648: 'IIn<T>.IT' is a type not supported by the language
                //         o = typeof(IIn<object>.IT); // CS0648 (not reported by Dev11)
                Diagnostic(ErrorCode.ERR_BogusType, "IT").WithArguments("IIn<T>.IT"),
                // (29,32): error CS0648: 'IIn<T>.IOut' is a type not supported by the language
                //         o = typeof(IIn<object>.IOut); // CS0648 (not reported by Dev11)
                Diagnostic(ErrorCode.ERR_BogusType, "IOut").WithArguments("IIn<T>.IOut"));
        }

        [WorkItem(528861, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528861")]
        [Fact]
        public void ConstraintsAreCheckedAlongHierarchy()
        {
            var ilSource =
@".class interface public abstract I
{
}
.class public abstract A
{
    .method public specialname rtspecialname instance void .ctor() { ret }
}
.class interface public abstract IA1_1<valuetype T> { }
.class interface public abstract IA1_2<(I)T> { }
.class interface public abstract IA1_3<T, (!T)U> { }
.class interface public abstract IB1<T, U, V>
    implements class IA1_1<!T>, class IA1_2<!U>, class IA1_3<!U, !V>
{
}
.class interface public abstract IA2_1<valuetype T> { }
.class interface public abstract IA2_2<(I)T> { }
.class interface public abstract IA2_3<T, (!T)U> { }
.class interface public abstract IB2<T, U, V>
    implements class IA2_1<!T>, class IA2_2<!U>, class IA2_3<!U, !V>
{
}
.class interface public abstract IA3_1<valuetype T> { }
.class interface public abstract IA3_2<(I)T> { }
.class interface public abstract IA3_3<T, (!T)U> { }
.class interface public abstract IB3_1<T>
    implements class IA3_1<!T>
{
}
.class public abstract B3<T, U, V>
    implements class IA3_1<!T>, class IA3_2<!U>, class IA3_3<!U, !V>
{
    .method public specialname rtspecialname instance void .ctor() { ret }
}
.class public abstract A4<valuetype T, (I)U, (!U)V>
{
    .method public specialname rtspecialname instance void .ctor() { ret }
}
.class public abstract B4<T, U, V>
    extends class A4<!T, !U, !V>
{
    .method public specialname rtspecialname instance void .ctor() { ret }
}";
            var csharpSource =
@"interface IC1 : IB1<I, A, object> { }
interface IC2<T, U, V> : IB2<T, U, V> { }
interface IC3_1 : IB3_1<object> { }
class C2 : IC2<I, A, object> { }
class C3 : B3<I, A, object>, IC3_1 { }
class C4 : B4<I, A, object> { }
";
            var compilation = CreateCompilationWithILAndMscorlib40(csharpSource, ilSource);
            compilation.VerifyDiagnostics(
                // (1,11): error CS0453: The type 'I' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'IA1_1<T>'
                // interface IC1 : IB1<I, A, object> { }
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "IC1").WithArguments("IA1_1<T>", "T", "I").WithLocation(1, 11),
                // (1,11): error CS0311: The type 'A' cannot be used as type parameter 'T' in the generic type or method 'IA1_2<T>'. There is no implicit reference conversion from 'A' to 'I'.
                // interface IC1 : IB1<I, A, object> { }
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "IC1").WithArguments("IA1_2<T>", "I", "T", "A").WithLocation(1, 11),
                // (1,11): error CS0311: The type 'object' cannot be used as type parameter 'U' in the generic type or method 'IA1_3<T, U>'. There is no implicit reference conversion from 'object' to 'A'.
                // interface IC1 : IB1<I, A, object> { }
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "IC1").WithArguments("IA1_3<T, U>", "A", "U", "object").WithLocation(1, 11),
                // (2,11): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'IA2_1<T>'
                // interface IC2<T, U, V> : IB2<T, U, V> { }
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "IC2").WithArguments("IA2_1<T>", "T", "T").WithLocation(2, 11),
                // (2,11): error CS0314: The type 'U' cannot be used as type parameter 'T' in the generic type or method 'IA2_2<T>'. There is no boxing conversion or type parameter conversion from 'U' to 'I'.
                // interface IC2<T, U, V> : IB2<T, U, V> { }
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar, "IC2").WithArguments("IA2_2<T>", "I", "T", "U").WithLocation(2, 11),
                // (2,11): error CS0314: The type 'V' cannot be used as type parameter 'U' in the generic type or method 'IA2_3<T, U>'. There is no boxing conversion or type parameter conversion from 'V' to 'U'.
                // interface IC2<T, U, V> : IB2<T, U, V> { }
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar, "IC2").WithArguments("IA2_3<T, U>", "U", "U", "V").WithLocation(2, 11),
                // (3,11): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'IA3_1<T>'
                // interface IC3_1 : IB3_1<object> { }
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "IC3_1").WithArguments("IA3_1<T>", "T", "object").WithLocation(3, 11),
                // (4,7): error CS0453: The type 'I' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'IA2_1<T>'
                // class C2 : IC2<I, A, object> { }
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "C2").WithArguments("IA2_1<T>", "T", "I").WithLocation(4, 7),
                // (4,7): error CS0311: The type 'A' cannot be used as type parameter 'T' in the generic type or method 'IA2_2<T>'. There is no implicit reference conversion from 'A' to 'I'.
                // class C2 : IC2<I, A, object> { }
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "C2").WithArguments("IA2_2<T>", "I", "T", "A").WithLocation(4, 7),
                // (4,7): error CS0311: The type 'object' cannot be used as type parameter 'U' in the generic type or method 'IA2_3<T, U>'. There is no implicit reference conversion from 'object' to 'A'.
                // class C2 : IC2<I, A, object> { }
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "C2").WithArguments("IA2_3<T, U>", "A", "U", "object").WithLocation(4, 7),
                // (5,7): error CS0453: The type 'object' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'IA3_1<T>'
                // class C3 : B3<I, A, object>, IC3_1 { }
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "C3").WithArguments("IA3_1<T>", "T", "object").WithLocation(5, 7));
        }

        [WorkItem(542755, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542755")]
        [ClrOnlyFact]
        public void SpellingOfGenericClassNameIsPreserved()
        {
            var ilSource =
@"
.class interface public abstract I2<T> { }

.class interface public abstract I
{
  .method public hidebysig newslot abstract virtual instance void M<(class I2<string>) T>() { }
}
";
            var csharpSource =
@"
class C : I
{
   void I.M<T>() { }
}
";
            Action<CSharpCompilation> compilationVerifier =
                delegate (CSharpCompilation compilation)
                {
                    NamedTypeSymbol i2 = compilation.GetTypeByMetadataName("I2");
                    Assert.False(i2.IsErrorType());
                    Assert.Equal(1, i2.Arity);
                    Assert.Equal("I2", i2.Name);
                    Assert.False(i2.MangleName);
                    Assert.Equal("I2<T>", i2.ToTestDisplayString());
                    Assert.Equal("I2", i2.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat));
                };

            CompileWithCustomILSource(csharpSource, ilSource, compilationVerifier: compilationVerifier);
        }

        [ClrOnlyFact]
        public void SpellingOfGenericClassNameIsPreserved2()
        {
            var ilSource =
@"
.class interface public abstract I2`2<T> { }

.class interface public abstract I
{
  .method public hidebysig newslot abstract virtual instance void M<(class I2`2<string>) T>() { }
}
";
            var csharpSource =
@"
class C : I
{
   void I.M<T>() { }
}
";
            Action<CSharpCompilation> compilationVerifier =
                delegate (CSharpCompilation compilation)
                {
                    NamedTypeSymbol i2 = compilation.GetTypeByMetadataName("I2`2");
                    Assert.False(i2.IsErrorType());
                    Assert.Equal(1, i2.Arity);
                    Assert.Equal("I2`2", i2.Name);
                    Assert.False(i2.MangleName);
                    Assert.Equal("I2`2<T>", i2.ToTestDisplayString());
                    Assert.Equal("I2`2", i2.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat));
                };

            CompileWithCustomILSource(csharpSource, ilSource, compilationVerifier: compilationVerifier);
        }

        [ClrOnlyFact]
        public void SpellingOfGenericClassNameIsPreserved3()
        {
            var ilSource =
@"
.class interface public abstract I2`1<T> { }

.class interface public abstract I
{
  .method public hidebysig newslot abstract virtual instance void M<(class I2`1<string>) T>() { }
}
";
            var csharpSource =
@"
class C : I
{
   void I.M<T>() { }
}
";
            Action<CSharpCompilation> compilationVerifier =
                delegate (CSharpCompilation compilation)
                {
                    NamedTypeSymbol i2 = compilation.GetTypeByMetadataName("I2`1");
                    Assert.False(i2.IsErrorType());
                    Assert.Equal(1, i2.Arity);
                    Assert.Equal("I2", i2.Name);
                    Assert.True(i2.MangleName);
                    Assert.Equal("I2<T>", i2.ToTestDisplayString());
                    Assert.Equal("I2`1", i2.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat));
                };

            CompileWithCustomILSource(csharpSource, ilSource, compilationVerifier: compilationVerifier);
        }

        [ClrOnlyFact]
        public void SpellingOfGenericClassNameIsPreserved4()
        {
            var ilSource =
@"
.class interface public abstract I2`01<T> { }

.class interface public abstract I
{
  .method public hidebysig newslot abstract virtual instance void M<(class I2`01<string>) T>() { }
}
";
            var csharpSource =
@"
class C : I
{
   void I.M<T>() { }
}
";
            Action<CSharpCompilation> compilationVerifier =
                delegate (CSharpCompilation compilation)
                {
                    NamedTypeSymbol i2 = compilation.GetTypeByMetadataName("I2`01");
                    Assert.False(i2.IsErrorType());
                    Assert.Equal(1, i2.Arity);
                    Assert.Equal("I2`01", i2.Name);
                    Assert.False(i2.MangleName);
                    Assert.Equal("I2`01<T>", i2.ToTestDisplayString());
                    Assert.Equal("I2`01", i2.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat));
                };

            CompileWithCustomILSource(csharpSource, ilSource, compilationVerifier: compilationVerifier);
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        public void SpellingOfGenericClassNameIsPreserved5()
        {
            var ilSource =
@"
.class interface public abstract I2`1 { }

.class interface public abstract I
{
  .method public hidebysig newslot abstract virtual instance void M<(class I2`1) T>() { }

    .class interface nested public abstract I2`1 { }
    .class interface nested public abstract I { }
    .class interface nested public abstract I3`1<T> { }
    .class interface nested public abstract I4`2<T> { }
}

.class interface public System.IEquatable`1 { }

.class interface public System.Linq.IQueryable`1 { }

.class interface public System.Linq.IQueryable<T> { }

.class interface public abstract I3`1<T> { }

.class interface public abstract I4`2<T> { }
";
            var csharpSource =
@"
class C : I
{
   void I.M<T>() { }
}
";
            Action<CSharpCompilation> compilationVerifier =
                delegate (CSharpCompilation compilation)
                {
                    NamedTypeSymbol i2 = compilation.GetTypeByMetadataName("I2`1");
                    Assert.False(i2.IsErrorType());
                    Assert.Equal(0, i2.Arity);
                    Assert.Equal("I2`1", i2.Name);
                    Assert.False(i2.MangleName);
                    Assert.Equal("I2`1", i2.ToTestDisplayString());
                    Assert.Equal("I2`1", i2.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat));

                    NamedTypeSymbol iEquatable = compilation.GetWellKnownType(WellKnownType.System_IEquatable_T);
                    Assert.False(iEquatable.IsErrorType());
                    Assert.Equal(1, iEquatable.Arity);
                    Assert.Null(compilation.GetTypeByMetadataName("System.IEquatable`1"));

                    NamedTypeSymbol iQueryable_T = compilation.GetWellKnownType(WellKnownType.System_Linq_IQueryable_T);
                    Assert.True(iQueryable_T.IsErrorType());
                    Assert.Equal(1, iQueryable_T.Arity);

                    NamedTypeSymbol iQueryable = compilation.GetWellKnownType(WellKnownType.System_Linq_IQueryable);
                    Assert.True(iQueryable.IsErrorType());
                    Assert.Equal(0, iQueryable.Arity);

                    MetadataTypeName mdName;
                    NamedTypeSymbol t;
                    AssemblySymbol asm = i2.ContainingAssembly;

                    mdName = MetadataTypeName.FromFullName("I3`1", false, -1);
                    t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    Assert.False(t.IsErrorType());
                    Assert.Equal("I3", t.Name);
                    Assert.True(t.MangleName);
                    Assert.Equal(1, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I3`1", false, 0);
                    t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    Assert.True(t.IsErrorType());
                    Assert.Equal("I3`1", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(0, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I3`1", false, 1);
                    t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    Assert.False(t.IsErrorType());
                    Assert.Equal("I3", t.Name);
                    Assert.True(t.MangleName);
                    Assert.Equal(1, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I3`1", false, 2);
                    t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    Assert.True(t.IsErrorType());
                    Assert.Equal("I3`1", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(2, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I3`1", true, -1);
                    t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    Assert.False(t.IsErrorType());
                    Assert.Equal("I3", t.Name);
                    Assert.True(t.MangleName);
                    Assert.Equal(1, t.Arity);

                    //mdName = MetadataTypeName.FromFullName("I3`1", true, 0);
                    //t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    //Assert.True(t.IsErrorType());
                    //Assert.Equal("I3`1", t.Name);
                    //Assert.False(t.MangleName);
                    //Assert.Equal(0, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I3`1", true, 1);
                    t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    Assert.False(t.IsErrorType());
                    Assert.Equal("I3", t.Name);
                    Assert.True(t.MangleName);
                    Assert.Equal(1, t.Arity);

                    //mdName = MetadataTypeName.FromFullName("I3`1", true, 2);
                    //t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    //Assert.True(t.IsErrorType());
                    //Assert.Equal("I3`1", t.Name);
                    //Assert.False(t.MangleName);
                    //Assert.Equal(2, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I", false, -1);
                    t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    Assert.False(t.IsErrorType());
                    Assert.Equal("I", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(0, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I", false, 0);
                    t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    Assert.False(t.IsErrorType());
                    Assert.Equal("I", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(0, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I", false, 1);
                    t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    Assert.True(t.IsErrorType());
                    Assert.Equal("I", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(1, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I", true, -1);
                    t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    Assert.False(t.IsErrorType());
                    Assert.Equal("I", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(0, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I", true, 0);
                    t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    Assert.False(t.IsErrorType());
                    Assert.Equal("I", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(0, t.Arity);

                    //mdName = MetadataTypeName.FromFullName("I", true, 1);
                    //t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    //Assert.True(t.IsErrorType());
                    //Assert.Equal("I", t.Name);
                    //Assert.False(t.MangleName);
                    //Assert.Equal(1, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I2`1", false, -1);
                    t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    Assert.False(t.IsErrorType());
                    Assert.Equal("I2`1", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(0, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I2`1", false, 0);
                    t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    Assert.False(t.IsErrorType());
                    Assert.Equal("I2`1", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(0, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I2`1", false, 1);
                    t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    Assert.True(t.IsErrorType());
                    Assert.Equal("I2", t.Name);
                    Assert.True(t.MangleName);
                    Assert.Equal(1, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I2`1", false, 2);
                    t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    Assert.True(t.IsErrorType());
                    Assert.Equal("I2`1", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(2, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I2`1", true, -1);
                    t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    Assert.True(t.IsErrorType());
                    Assert.Equal("I2", t.Name);
                    Assert.True(t.MangleName);
                    Assert.Equal(1, t.Arity);

                    //mdName = MetadataTypeName.FromFullName("I2`1", true, 0);
                    //t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    //Assert.True(t.IsErrorType());
                    //Assert.Equal("I2`1", t.Name);
                    //Assert.False(t.MangleName);
                    //Assert.Equal(0, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I2`1", true, 1);
                    t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    Assert.True(t.IsErrorType());
                    Assert.Equal("I2", t.Name);
                    Assert.True(t.MangleName);
                    Assert.Equal(1, t.Arity);

                    //mdName = MetadataTypeName.FromFullName("I2`1", true, 2);
                    //t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    //Assert.True(t.IsErrorType());
                    //Assert.Equal("I2`1", t.Name);
                    //Assert.False(t.MangleName);
                    //Assert.Equal(2, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I4`2", false, -1);
                    t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    Assert.False(t.IsErrorType());
                    Assert.Equal("I4`2", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(1, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I4`2", false, 0);
                    t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    Assert.True(t.IsErrorType());
                    Assert.Equal("I4`2", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(0, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I4`2", false, 1);
                    t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    Assert.False(t.IsErrorType());
                    Assert.Equal("I4`2", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(1, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I4`2", false, 2);
                    t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    Assert.True(t.IsErrorType());
                    Assert.Equal("I4", t.Name);
                    Assert.True(t.MangleName);
                    Assert.Equal(2, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I4`2", true, -1);
                    t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    Assert.True(t.IsErrorType());
                    Assert.Equal("I4", t.Name);
                    Assert.True(t.MangleName);
                    Assert.Equal(2, t.Arity);

                    //mdName = MetadataTypeName.FromFullName("I4`2", true, 0);
                    //t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    //Assert.True(t.IsErrorType());
                    //Assert.Equal("I4`2", t.Name);
                    //Assert.False(t.MangleName);
                    //Assert.Equal(0, t.Arity);

                    //mdName = MetadataTypeName.FromFullName("I4`2", true, 1);
                    //t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    //Assert.True(t.IsErrorType());
                    //Assert.Equal("I4`2", t.Name);
                    //Assert.False(t.MangleName);
                    //Assert.Equal(1, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I4`2", true, 2);
                    t = asm.LookupTopLevelMetadataType(ref mdName, true);
                    Assert.True(t.IsErrorType());
                    Assert.Equal("I4", t.Name);
                    Assert.True(t.MangleName);
                    Assert.Equal(2, t.Arity);

                    NamedTypeSymbol containingType = compilation.GetTypeByMetadataName("I");

                    mdName = MetadataTypeName.FromFullName("I3`1", false, -1);
                    t = containingType.LookupMetadataType(ref mdName);
                    Assert.False(t.IsErrorType());
                    Assert.Equal("I3", t.Name);
                    Assert.True(t.MangleName);
                    Assert.Equal(1, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I3`1", false, 0);
                    t = containingType.LookupMetadataType(ref mdName);
                    Assert.True(t.IsErrorType());
                    Assert.Equal("I3`1", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(0, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I3`1", false, 1);
                    t = containingType.LookupMetadataType(ref mdName);
                    Assert.False(t.IsErrorType());
                    Assert.Equal("I3", t.Name);
                    Assert.True(t.MangleName);
                    Assert.Equal(1, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I3`1", false, 2);
                    t = containingType.LookupMetadataType(ref mdName);
                    Assert.True(t.IsErrorType());
                    Assert.Equal("I3`1", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(2, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I3`1", true, -1);
                    t = containingType.LookupMetadataType(ref mdName);
                    Assert.False(t.IsErrorType());
                    Assert.Equal("I3", t.Name);
                    Assert.True(t.MangleName);
                    Assert.Equal(1, t.Arity);

                    //mdName = MetadataTypeName.FromFullName("I3`1", true, 0);
                    //t = containingType.LookupMetadataType(ref mdName);
                    //Assert.True(t.IsErrorType());
                    //Assert.Equal("I3`1", t.Name);
                    //Assert.False(t.MangleName);
                    //Assert.Equal(0, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I3`1", true, 1);
                    t = containingType.LookupMetadataType(ref mdName);
                    Assert.False(t.IsErrorType());
                    Assert.Equal("I3", t.Name);
                    Assert.True(t.MangleName);
                    Assert.Equal(1, t.Arity);

                    //mdName = MetadataTypeName.FromFullName("I3`1", true, 2);
                    //t = containingType.LookupMetadataType(ref mdName);
                    //Assert.True(t.IsErrorType());
                    //Assert.Equal("I3`1", t.Name);
                    //Assert.False(t.MangleName);
                    //Assert.Equal(2, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I", false, -1);
                    t = containingType.LookupMetadataType(ref mdName);
                    Assert.False(t.IsErrorType());
                    Assert.Equal("I", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(0, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I", false, 0);
                    t = containingType.LookupMetadataType(ref mdName);
                    Assert.False(t.IsErrorType());
                    Assert.Equal("I", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(0, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I", false, 1);
                    t = containingType.LookupMetadataType(ref mdName);
                    Assert.True(t.IsErrorType());
                    Assert.Equal("I", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(1, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I", true, -1);
                    t = containingType.LookupMetadataType(ref mdName);
                    Assert.False(t.IsErrorType());
                    Assert.Equal("I", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(0, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I", true, 0);
                    t = containingType.LookupMetadataType(ref mdName);
                    Assert.False(t.IsErrorType());
                    Assert.Equal("I", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(0, t.Arity);

                    //mdName = MetadataTypeName.FromFullName("I", true, 1);
                    //t = containingType.LookupMetadataType(ref mdName);
                    //Assert.True(t.IsErrorType());
                    //Assert.Equal("I", t.Name);
                    //Assert.False(t.MangleName);
                    //Assert.Equal(1, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I2`1", false, -1);
                    t = containingType.LookupMetadataType(ref mdName);
                    Assert.False(t.IsErrorType());
                    Assert.Equal("I2`1", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(0, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I2`1", false, 0);
                    t = containingType.LookupMetadataType(ref mdName);
                    Assert.False(t.IsErrorType());
                    Assert.Equal("I2`1", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(0, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I2`1", false, 1);
                    t = containingType.LookupMetadataType(ref mdName);
                    Assert.True(t.IsErrorType());
                    Assert.Equal("I2", t.Name);
                    Assert.True(t.MangleName);
                    Assert.Equal(1, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I2`1", false, 2);
                    t = containingType.LookupMetadataType(ref mdName);
                    Assert.True(t.IsErrorType());
                    Assert.Equal("I2`1", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(2, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I2`1", true, -1);
                    t = containingType.LookupMetadataType(ref mdName);
                    Assert.True(t.IsErrorType());
                    Assert.Equal("I2", t.Name);
                    Assert.True(t.MangleName);
                    Assert.Equal(1, t.Arity);

                    //mdName = MetadataTypeName.FromFullName("I2`1", true, 0);
                    //t = containingType.LookupMetadataType(ref mdName);
                    //Assert.True(t.IsErrorType());
                    //Assert.Equal("I2`1", t.Name);
                    //Assert.False(t.MangleName);
                    //Assert.Equal(0, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I2`1", true, 1);
                    t = containingType.LookupMetadataType(ref mdName);
                    Assert.True(t.IsErrorType());
                    Assert.Equal("I2", t.Name);
                    Assert.True(t.MangleName);
                    Assert.Equal(1, t.Arity);

                    //mdName = MetadataTypeName.FromFullName("I2`1", true, 2);
                    //t = containingType.LookupMetadataType(ref mdName);
                    //Assert.True(t.IsErrorType());
                    //Assert.Equal("I2`1", t.Name);
                    //Assert.False(t.MangleName);
                    //Assert.Equal(2, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I4`2", false, -1);
                    t = containingType.LookupMetadataType(ref mdName);
                    Assert.False(t.IsErrorType());
                    Assert.Equal("I4`2", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(1, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I4`2", false, 0);
                    t = containingType.LookupMetadataType(ref mdName);
                    Assert.True(t.IsErrorType());
                    Assert.Equal("I4`2", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(0, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I4`2", false, 1);
                    t = containingType.LookupMetadataType(ref mdName);
                    Assert.False(t.IsErrorType());
                    Assert.Equal("I4`2", t.Name);
                    Assert.False(t.MangleName);
                    Assert.Equal(1, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I4`2", false, 2);
                    t = containingType.LookupMetadataType(ref mdName);
                    Assert.True(t.IsErrorType());
                    Assert.Equal("I4", t.Name);
                    Assert.True(t.MangleName);
                    Assert.Equal(2, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I4`2", true, -1);
                    t = containingType.LookupMetadataType(ref mdName);
                    Assert.True(t.IsErrorType());
                    Assert.Equal("I4", t.Name);
                    Assert.True(t.MangleName);
                    Assert.Equal(2, t.Arity);

                    //mdName = MetadataTypeName.FromFullName("I4`2", true, 0);
                    //t = containingType.LookupMetadataType(ref mdName);
                    //Assert.True(t.IsErrorType());
                    //Assert.Equal("I4`2", t.Name);
                    //Assert.False(t.MangleName);
                    //Assert.Equal(0, t.Arity);

                    //mdName = MetadataTypeName.FromFullName("I4`2", true, 1);
                    //t = containingType.LookupMetadataType(ref mdName);
                    //Assert.True(t.IsErrorType());
                    //Assert.Equal("I4`2", t.Name);
                    //Assert.False(t.MangleName);
                    //Assert.Equal(1, t.Arity);

                    mdName = MetadataTypeName.FromFullName("I4`2", true, 2);
                    t = containingType.LookupMetadataType(ref mdName);
                    Assert.True(t.IsErrorType());
                    Assert.Equal("I4", t.Name);
                    Assert.True(t.MangleName);
                    Assert.Equal(2, t.Arity);
                };

            CompileWithCustomILSource(csharpSource, ilSource, compilationVerifier: compilationVerifier, targetFramework: TargetFramework.Mscorlib40);
        }

        [WorkItem(542358, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542358")]
        [Fact]
        public void InterfaceConstraintsAbsorbed()
        {
            var source =
@"interface I<T>
{
    void M<U>() where U : struct, T;
}
class C : I<System.ValueType>
{
    public void M<U>() where U : struct { }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [WorkItem(542359, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542359")]
        [Fact]
        public void ExplicitImplementationMethodConstraintViolations()
        {
            var source =
@"interface IA<T, U>
{
    void M<V>() where V : T, U;
}
interface IB<T>
{
    void M<U>() where U : struct, T;
}
interface IC<T>
{
    void M<U>() where U : class, T;
}
interface ID<T>
{
    void M<U>() where U : T, new();
}
class C : IA<int, double>, IB<string>, IC<int>, ID<string>
{
    void IA<int, double>.M<V>() { }
    void IB<string>.M<U>() { }
    void IC<int>.M<U>() { }
    void ID<string>.M<U>() { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (19,28): error CS0455: Type parameter 'V' inherits conflicting constraints 'double' and 'int'
                Diagnostic(ErrorCode.ERR_BaseConstraintConflict, "V").WithArguments("V", "double", "int").WithLocation(19, 28),
                // (20,23): error CS0455: Type parameter 'U' inherits conflicting constraints 'string' and 'System.ValueType'
                Diagnostic(ErrorCode.ERR_BaseConstraintConflict, "U").WithArguments("U", "string", "System.ValueType").WithLocation(20, 23),
                // (20,23): error CS0455: Type parameter 'U' inherits conflicting constraints 'System.ValueType' and 'struct'
                Diagnostic(ErrorCode.ERR_BaseConstraintConflict, "U").WithArguments("U", "System.ValueType", "struct").WithLocation(20, 23),
                // (21,20): error CS0455: Type parameter 'U' inherits conflicting constraints 'int' and 'class'
                Diagnostic(ErrorCode.ERR_BaseConstraintConflict, "U").WithArguments("U", "int", "class").WithLocation(21, 20));
        }

        [WorkItem(542362, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542362")]
        [Fact]
        public void CycleInvolvingAlias()
        {
            var source =
@"using C = B<A>;
class A : C { }
class B<T> where T : C { }";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [WorkItem(542363, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542363")]
        [Fact]
        public void InvokeExplicitImplementationMethod()
        {
            var source =
@"interface IA
{
    object M(object o);
    object P { get; set; }
}
interface IB
{
    void M<T>(T t) where T : IA;
}
class C : IB
{
    void IB.M<T>(T t)
    {
        t.P = t.M(t.P);
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [WorkItem(542364, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542364")]
        [Fact]
        public void CheckConstraintsOverriddenMethodDefaultParameter()
        {
            var source =
@"abstract class B<T>
{
    public abstract void F<S>(S x) where S : T;
}
class C : B<int>
{
    public override void F<S>(S x = 0) { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,33): error CS1750: A value of type 'int' cannot be used as a default parameter because there are no standard conversions to type 'S'
                Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "x").WithArguments("int", "S").WithLocation(7, 33));
        }

        [WorkItem(542366, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542366")]
        [Fact]
        public void NestedConstraintsWithinConstraints()
        {
            var source =
@"class A<T> where T : class
{
    internal interface I { }
}
class B1
{
    internal class B2<T, U> where U : struct { }
}
class C1<T> where T : class, A<int>.I { }
class C2<T> where T : A<B1.B2<object, A<T>>> { }
class C3<T, U> where U : T, A<A<A<int>[]>[]>.I { }
interface I
{
    void M1<T>() where T : class, A<int>.I;
    void M2<T>() where T : A<B1.B2<object, A<T>>>;
    void M3<T, U>() where U : T, A<A<A<int>[]>[]>.I;
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (9,10): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "T").WithArguments("A<T>", "T", "int").WithLocation(9, 10),
                // (10,10): error CS0453: The type 'A<T>' must be a non-nullable value type in order to use it as parameter 'U' in the generic type or method 'B1.B2<T, U>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "T").WithArguments("B1.B2<T, U>", "U", "A<T>").WithLocation(10, 10),
                // (11,13): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "U").WithArguments("A<T>", "T", "int").WithLocation(11, 13),
                // (14,13): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "T").WithArguments("A<T>", "T", "int").WithLocation(14, 13),
                // (15,13): error CS0453: The type 'A<T>' must be a non-nullable value type in order to use it as parameter 'U' in the generic type or method 'B1.B2<T, U>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "T").WithArguments("B1.B2<T, U>", "U", "A<T>").WithLocation(15, 13),
                // (16,16): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'A<T>'
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "U").WithArguments("A<T>", "T", "int").WithLocation(16, 16));
        }

        [WorkItem(542367, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542367")]
        [Fact]
        public void ConstraintChecksBeforeOverloadResolution()
        {
            var source =
@"class C<T> where T : class { }
class Program
{
    static void Main()
    {
        Goo<int>(null);
    }
    static void Goo<T>(object x) { }
    static void Goo<T>(C<T> x) where T : class { }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [WorkItem(542380, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542380")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void StructProperties()
        {
            var source =
@"interface I
{
    int P { get; set; }
    int this[int index] { get; set; }
}
struct S : I
{
    private int i;
    public int P { get; set; }
    public int this[int index] { get { return i; } set { i = value; } }
}
class A : I
{
    private int i;
    public int P { get; set; }
    public int this[int index] { get { return i; } set { i = value; } }
}
class B
{
    static void M1<T>(T t) where T : I
    {
        t.P++;
        t[0]++;
        t.P += 2;
        t[0] += 2;
        System.Console.WriteLine(""{0}, {1}"", t.P, t[0]);
    }
    static void M2<T>(T t) where T : class, I
    {
        t.P++;
        t[0]++;
        t.P += 2;
        t[0] += 2;
        System.Console.WriteLine(""{0}, {1}"", t.P, t[0]);
    }
    static void M3<T>(T t) where T : struct, I
    {
        t.P++;
        t[0]++;
        t.P += 2;
        t[0] += 2;
        System.Console.WriteLine(""{0}, {1}"", t.P, t[0]);
    }
    static void Main()
    {
        var a = new A();
        var s = new S();
        M1(a);
        M1(s);
        M2(a);
        M3(s);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput:
@"3, 3
3, 3
6, 6
3, 3");
            compilation.VerifyIL("B.M1<T>(T)",
@"
{
  // Code size      166 (0xa6)
  .maxstack  4
  .locals init (int V_0,
  T& V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  dup
  IL_0003:  constrained. ""T""
  IL_0009:  callvirt   ""int I.P.get""
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  ldc.i4.1
  IL_0011:  add
  IL_0012:  constrained. ""T""
  IL_0018:  callvirt   ""void I.P.set""
  IL_001d:  ldarga.s   V_0
  IL_001f:  dup
  IL_0020:  ldc.i4.0
  IL_0021:  constrained. ""T""
  IL_0027:  callvirt   ""int I.this[int].get""
  IL_002c:  stloc.0
  IL_002d:  ldc.i4.0
  IL_002e:  ldloc.0
  IL_002f:  ldc.i4.1
  IL_0030:  add
  IL_0031:  constrained. ""T""
  IL_0037:  callvirt   ""void I.this[int].set""
  IL_003c:  ldarga.s   V_0
  IL_003e:  dup
  IL_003f:  constrained. ""T""
  IL_0045:  callvirt   ""int I.P.get""
  IL_004a:  ldc.i4.2
  IL_004b:  add
  IL_004c:  constrained. ""T""
  IL_0052:  callvirt   ""void I.P.set""
  IL_0057:  ldarga.s   V_0
  IL_0059:  stloc.1
  IL_005a:  ldloc.1
  IL_005b:  ldc.i4.0
  IL_005c:  ldloc.1
  IL_005d:  ldc.i4.0
  IL_005e:  constrained. ""T""
  IL_0064:  callvirt   ""int I.this[int].get""
  IL_0069:  ldc.i4.2
  IL_006a:  add
  IL_006b:  constrained. ""T""
  IL_0071:  callvirt   ""void I.this[int].set""
  IL_0076:  ldstr      ""{0}, {1}""
  IL_007b:  ldarga.s   V_0
  IL_007d:  constrained. ""T""
  IL_0083:  callvirt   ""int I.P.get""
  IL_0088:  box        ""int""
  IL_008d:  ldarga.s   V_0
  IL_008f:  ldc.i4.0
  IL_0090:  constrained. ""T""
  IL_0096:  callvirt   ""int I.this[int].get""
  IL_009b:  box        ""int""
  IL_00a0:  call       ""void System.Console.WriteLine(string, object, object)""
  IL_00a5:  ret
}
");
            compilation.VerifyIL("B.M2<T>(T)",
@"
{
  // Code size      164 (0xa4)
  .maxstack  4
  .locals init (int V_0,
                T& V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  dup
  IL_0003:  constrained. ""T""
  IL_0009:  callvirt   ""int I.P.get""
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  ldc.i4.1
  IL_0011:  add
  IL_0012:  constrained. ""T""
  IL_0018:  callvirt   ""void I.P.set""
  IL_001d:  ldarga.s   V_0
  IL_001f:  dup
  IL_0020:  ldc.i4.0
  IL_0021:  constrained. ""T""
  IL_0027:  callvirt   ""int I.this[int].get""
  IL_002c:  stloc.0
  IL_002d:  ldc.i4.0
  IL_002e:  ldloc.0
  IL_002f:  ldc.i4.1
  IL_0030:  add
  IL_0031:  constrained. ""T""
  IL_0037:  callvirt   ""void I.this[int].set""
  IL_003c:  ldarga.s   V_0
  IL_003e:  stloc.1
  IL_003f:  ldloc.1
  IL_0040:  ldloc.1
  IL_0041:  constrained. ""T""
  IL_0047:  callvirt   ""int I.P.get""
  IL_004c:  ldc.i4.2
  IL_004d:  add
  IL_004e:  constrained. ""T""
  IL_0054:  callvirt   ""void I.P.set""
  IL_0059:  ldarga.s   V_0
  IL_005b:  stloc.1
  IL_005c:  ldloc.1
  IL_005d:  ldc.i4.0
  IL_005e:  ldloc.1
  IL_005f:  ldc.i4.0
  IL_0060:  constrained. ""T""
  IL_0066:  callvirt   ""int I.this[int].get""
  IL_006b:  ldc.i4.2
  IL_006c:  add
  IL_006d:  constrained. ""T""
  IL_0073:  callvirt   ""void I.this[int].set""
  IL_0078:  ldstr      ""{0}, {1}""
  IL_007d:  ldarg.0
  IL_007e:  box        ""T""
  IL_0083:  callvirt   ""int I.P.get""
  IL_0088:  box        ""int""
  IL_008d:  ldarg.0
  IL_008e:  box        ""T""
  IL_0093:  ldc.i4.0
  IL_0094:  callvirt   ""int I.this[int].get""
  IL_0099:  box        ""int""
  IL_009e:  call       ""void System.Console.WriteLine(string, object, object)""
  IL_00a3:  ret
}
");
            compilation.VerifyIL("B.M3<T>(T)",
@"
{
  // Code size      166 (0xa6)
  .maxstack  4
  .locals init (int V_0,
  T& V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  dup
  IL_0003:  constrained. ""T""
  IL_0009:  callvirt   ""int I.P.get""
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  ldc.i4.1
  IL_0011:  add
  IL_0012:  constrained. ""T""
  IL_0018:  callvirt   ""void I.P.set""
  IL_001d:  ldarga.s   V_0
  IL_001f:  dup
  IL_0020:  ldc.i4.0
  IL_0021:  constrained. ""T""
  IL_0027:  callvirt   ""int I.this[int].get""
  IL_002c:  stloc.0
  IL_002d:  ldc.i4.0
  IL_002e:  ldloc.0
  IL_002f:  ldc.i4.1
  IL_0030:  add
  IL_0031:  constrained. ""T""
  IL_0037:  callvirt   ""void I.this[int].set""
  IL_003c:  ldarga.s   V_0
  IL_003e:  dup
  IL_003f:  constrained. ""T""
  IL_0045:  callvirt   ""int I.P.get""
  IL_004a:  ldc.i4.2
  IL_004b:  add
  IL_004c:  constrained. ""T""
  IL_0052:  callvirt   ""void I.P.set""
  IL_0057:  ldarga.s   V_0
  IL_0059:  stloc.1
  IL_005a:  ldloc.1
  IL_005b:  ldc.i4.0
  IL_005c:  ldloc.1
  IL_005d:  ldc.i4.0
  IL_005e:  constrained. ""T""
  IL_0064:  callvirt   ""int I.this[int].get""
  IL_0069:  ldc.i4.2
  IL_006a:  add
  IL_006b:  constrained. ""T""
  IL_0071:  callvirt   ""void I.this[int].set""
  IL_0076:  ldstr      ""{0}, {1}""
  IL_007b:  ldarga.s   V_0
  IL_007d:  constrained. ""T""
  IL_0083:  callvirt   ""int I.P.get""
  IL_0088:  box        ""int""
  IL_008d:  ldarga.s   V_0
  IL_008f:  ldc.i4.0
  IL_0090:  constrained. ""T""
  IL_0096:  callvirt   ""int I.this[int].get""
  IL_009b:  box        ""int""
  IL_00a0:  call       ""void System.Console.WriteLine(string, object, object)""
  IL_00a5:  ret
}
");
        }

        [WorkItem(542527, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542527")]
        [ClrOnlyFact]
        public void SelfReferentialInheritedConstraints01()
        {
            var source =
@"using System;
abstract class A<T>
{
    public virtual void M1<U>() where U : IComparable<T>, T
    {
    }
    public virtual void M2<U>() where U : IComparable<U>
    {
    }
    public virtual void M3<U>() where U : IComparable<U>, T
    {
    }
}
class C : A<string>
{
    public override void M1<U>()
    {
        Console.WriteLine(typeof(U));
    }
    public override void M2<U>()
    {
        Console.WriteLine(typeof(U));
    }
    public override void M3<U>()
    {
        Console.WriteLine(typeof(U));
    }
    static void Main()
    {
        var c = new C();
        c.M1<string>();
        c.M2<string>();
        c.M3<string>();
    }
}";
            CompileAndVerify(source, expectedOutput:
@"System.String
System.String
System.String");
        }

        [ClrOnlyFact]
        public void SelfReferentialInheritedConstraints02()
        {
            var source =
@"interface IA<T>
{
}
class A<T>
{
    internal class B<U> { }
}
abstract class C
{
    internal abstract void M1<T, U>()
        where T : IA<A<T>.B<object>>
        where U : A<U[]>.B<T>;
}
interface IB
{
    void M2<T, U>()
        where T : IA<A<T>.B<object>>
        where U : A<U[]>.B<T>;
}
class D : C
{
    internal override void M1<X, Y>() { }
}
abstract class E : D, IB
{
    internal abstract override void M1<T1, T2>();
    void IB.M2<X, Y>() { }
}";
            Action<ModuleSymbol> validator = module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("E");

                var method = type.GetMember<MethodSymbol>("M1");
                var typeParameter = method.TypeParameters[0];
                Assert.Equal("IA<A<T1>.B<object>>", typeParameter.ConstraintTypes()[0].ToDisplayString());
                CheckTypeParameterContainingSymbols(method, typeParameter.EffectiveBaseClassNoUseSiteDiagnostics, 0);
                CheckTypeParameterContainingSymbols(method, typeParameter.EffectiveInterfacesNoUseSiteDiagnostics[0], 1);
                CheckTypeParameterContainingSymbols(method, typeParameter.ConstraintTypes()[0], 1);
                typeParameter = method.TypeParameters[1];
                Assert.Equal("A<T2[]>.B<T1>", typeParameter.ConstraintTypes()[0].ToDisplayString());
                CheckTypeParameterContainingSymbols(method, typeParameter.EffectiveBaseClassNoUseSiteDiagnostics, 2);
                CheckTypeParameterContainingSymbols(method, typeParameter.ConstraintTypes()[0], 2);

                method = type.GetMethod("IB.M2");
                typeParameter = method.TypeParameters[0];
                Assert.Equal("IA<A<X>.B<object>>", typeParameter.ConstraintTypes()[0].ToDisplayString());
                CheckTypeParameterContainingSymbols(method, typeParameter.EffectiveBaseClassNoUseSiteDiagnostics, 0);
                CheckTypeParameterContainingSymbols(method, typeParameter.EffectiveInterfacesNoUseSiteDiagnostics[0], 1);
                CheckTypeParameterContainingSymbols(method, typeParameter.ConstraintTypes()[0], 1);
                typeParameter = method.TypeParameters[1];
                Assert.Equal("A<Y[]>.B<X>", typeParameter.ConstraintTypes()[0].ToDisplayString());
                CheckTypeParameterContainingSymbols(method, typeParameter.EffectiveBaseClassNoUseSiteDiagnostics, 2);
                CheckTypeParameterContainingSymbols(method, typeParameter.ConstraintTypes()[0], 2);
            };
            CompileAndVerify(
                source: source,
                sourceSymbolValidator: validator,
                symbolValidator: validator);
        }

        [WorkItem(542601, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542601")]
        [ClrOnlyFact]
        public void SelfReferentialInheritedConstraints03()
        {
            var source =
@"using System;
abstract class A
{
    public abstract void Goo<T>() where T : IComparable<T>;
}
class B<S> : A
{
    public override void Goo<T>()
    {
        Console.WriteLine(typeof(S));
        Console.WriteLine(typeof(T));
    }
}
class P
{
    static void Main()
    {
        new B<string>().Goo<int>();
    }
}
";
            CompileAndVerify(source, expectedOutput:
@"System.String
System.Int32");
        }

        [WorkItem(542601, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542601")]
        [ClrOnlyFact]
        public void SelfReferentialInheritedConstraints04()
        {
            var source =
@"interface I<T> { }
class A<T>
{
    internal static void M<U>() where U : I<T>, I<U> { }
    internal class B<U> where U : I<T>, I<U> { }
}
class C : I<object>, I<C>
{
    static void M()
    {
        A<object>.M<C>();
        new A<object>.B<C>();
    }
}";
            CompileAndVerify(source);
        }

        /// <summary>
        /// Verify any type parameter symbols within the type
        /// have the given containing method symbol.
        /// </summary>
        private void CheckTypeParameterContainingSymbols(MethodSymbol containingMethod, TypeSymbol type, int nReferencesExpected)
        {
            int nReferences = 0;
            type.VisitType((t, unused1, unused2) =>
                {
                    if (t.TypeKind == TypeKind.TypeParameter)
                    {
                        nReferences++;
                        Assert.Same(t.ContainingSymbol, containingMethod);
                    }
                    return false;
                },
                (object)null);
            Assert.Equal(nReferencesExpected, nReferences);
        }

        [WorkItem(542532, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542532")]
        [ClrOnlyFact]
        public void SelfReferentialConstraintsWithLambda()
        {
            var source =
@"using System;
class A<T> { }
abstract class B
{
    internal abstract void M<T, U>() where U : A<T>;
}
class C : B
{
    internal override void M<T, U>()
    {
        Action a = () => Console.WriteLine(""M1<T, U>"");
        a();
    }
}
class Program
{
    static void M<T>(T x) where T : IComparable<T>
    {
        Action a = () => Console.WriteLine(x.CompareTo(x));
        a();
    }
    static void Main()
    {
        M(string.Empty);
        (new C()).M<int, A<int>>();
    }
}";
            CompileAndVerify(source, expectedOutput:
@"0
M1<T, U>");
        }

        [WorkItem(542277, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542277")]
        [ClrOnlyFact]
        public void AccessToMembersOfInheritedConstraints()
        {
            var source =
@"
using System;

abstract class A<T>
{
    public abstract void Goo<S>() where S : T, new();
}

class B : A<S>
{
    public override void Goo<S>()
    {
        var s = new S();
        s.X = 5;
        Console.WriteLine(s.X);
    }

    static void Main()
    {
        new B().Goo<S>();
    }
}

class S
{
    public int X;
}
";
            CompileAndVerify(source, expectedOutput: "5");
        }

        [WorkItem(542564, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542564")]
        [Fact]
        public void Arrays()
        {
            var source =
@"class A<T>
{
    static int[] F0(object[] x)
    {
        return (int[])x;
    }
    static U[] F1<U>(T[] x) where U : class, T
    {
        return (U[])x;
    }
    static U[] F2<U>(T[] x) where U : struct, T
    {
        return (U[])x;
    }
    static U[] F3<U>(T[] x) where U : T
    {
        return (U[])x;
    }
}
class B<T>
{
    static object[] F0(int[] x)
    {
        return (object[])x;
    }
    static T[] F1<U>(U[] x) where U : class, T
    {
        return (T[])x;
    }
    static T[] F2<U>(U[] x) where U : struct, T
    {
        return (T[])x;
    }
    static T[] F3<U>(U[] x) where U : T
    {
        return (T[])x;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,16): error CS0030: Cannot convert type 'object[]' to 'int[]'
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int[])x").WithArguments("object[]", "int[]").WithLocation(5, 16),
                // (13,16): error CS0030: Cannot convert type 'T[]' to 'U[]'
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(U[])x").WithArguments("T[]", "U[]").WithLocation(13, 16),
                // (17,16): error CS0030: Cannot convert type 'T[]' to 'U[]'
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(U[])x").WithArguments("T[]", "U[]").WithLocation(17, 16),
                // (24,16): error CS0030: Cannot convert type 'int[]' to 'object[]'
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(object[])x").WithArguments("int[]", "object[]").WithLocation(24, 16),
                // (32,16): error CS0030: Cannot convert type 'U[]' to 'T[]'
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(T[])x").WithArguments("U[]", "T[]").WithLocation(32, 16),
                // (36,16): error CS0030: Cannot convert type 'U[]' to 'T[]'
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(T[])x").WithArguments("U[]", "T[]").WithLocation(36, 16));
        }

        [ClrOnlyFact]
        public void ImplicitReferenceTypeParameterConversion()
        {
            var source =
@"class C<T, U> where U : class, T
{
    static U F1(T x)
    {
        return (U)x;
    }
    static T F2(U x)
    {
        return x;
    }
}";
            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C<T, U>.F1(T)",
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  unbox.any  ""U""
  IL_000b:  ret
}");
            compilation.VerifyIL("C<T, U>.F2(U)",
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""U""
  IL_0006:  unbox.any  ""T""
  IL_000b:  ret
}");
        }

        [WorkItem(542620, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542620")]
        [ClrOnlyFact]
        public void DuplicateConstraintTypes()
        {
            var source =
@"interface I<T> { }
interface I1<T, U>
{
    void M<V>() where V : T, U;
}
interface I2<T, U>
{
    void M<V>() where V : T, I<U>, I<object>;
}
interface I3<T, U>
{
    void M<V>() where V : I<T>, I<U>;
}
interface I4<T> : I1<T, T> { }
interface I5<T> : I2<I<object>, T> { }
interface I6<U> : I3<I<U>, I<U>> { }";
            Action<ModuleSymbol> validator = module =>
            {
                var method = module.GlobalNamespace.GetMember<NamedTypeSymbol>("I1").GetMember<MethodSymbol>("M");
                CheckConstraints(method.TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object", "T", "U");

                method = module.GlobalNamespace.GetMember<NamedTypeSymbol>("I2").GetMember<MethodSymbol>("M");
                CheckConstraints(method.TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object", "T", "I<U>", "I<object>");

                method = module.GlobalNamespace.GetMember<NamedTypeSymbol>("I3").GetMember<MethodSymbol>("M");
                CheckConstraints(method.TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object", "I<T>", "I<U>");

                method = module.GlobalNamespace.GetMember<NamedTypeSymbol>("I4").Interfaces()[0].GetMember<MethodSymbol>("M");
                CheckConstraints(method.TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object", "T");

                method = module.GlobalNamespace.GetMember<NamedTypeSymbol>("I5").Interfaces()[0].GetMember<MethodSymbol>("M");
                CheckConstraints(method.TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object", "I<object>", "I<T>");

                method = module.GlobalNamespace.GetMember<NamedTypeSymbol>("I6").Interfaces()[0].GetMember<MethodSymbol>("M");
                CheckConstraints(method.TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object", "I<I<U>>");
            };
            CompileAndVerify(
                source: source,
                sourceSymbolValidator: validator,
                symbolValidator: validator);
        }

        /// <summary>
        /// Type argument violating duplicate constraint types
        /// should result in a single error, not multiple.
        /// </summary>
        [WorkItem(542620, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542620")]
        [Fact]
        public void DuplicateConstraintTypeViolations()
        {
            var source =
@"interface I<T> { }
class A<T, U> where U : T, I<U> { }
class B<T>
{
    internal static void M<U>() where U : T, I<object>, I<string> { }
}
class C
{
    static void M<T1, T2>() where T2 : I<T1>, I<T2>
    {
        new A<I<object>, object>();
        B<I<object>>.M<string>();
        M<object, object>();
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (11,26): error CS0311: The type 'object' cannot be used as type parameter 'U' in the generic type or method 'A<T, U>'. There is no implicit reference conversion from 'object' to 'I<object>'.
                //         new A<I<object>, object>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "object").WithArguments("A<T, U>", "I<object>", "U", "object").WithLocation(11, 26),
                // (12,22): error CS0311: The type 'string' cannot be used as type parameter 'U' in the generic type or method 'B<I<object>>.M<U>()'. There is no implicit reference conversion from 'string' to 'I<object>'.
                //         B<I<object>>.M<string>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M<string>").WithArguments("B<I<object>>.M<U>()", "I<object>", "U", "string").WithLocation(12, 22),
                // (12,22): error CS0311: The type 'string' cannot be used as type parameter 'U' in the generic type or method 'B<I<object>>.M<U>()'. There is no implicit reference conversion from 'string' to 'I<string>'.
                //         B<I<object>>.M<string>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M<string>").WithArguments("B<I<object>>.M<U>()", "I<string>", "U", "string").WithLocation(12, 22),
                // (13,9): error CS0311: The type 'object' cannot be used as type parameter 'T2' in the generic type or method 'C.M<T1, T2>()'. There is no implicit reference conversion from 'object' to 'I<object>'.
                //         M<object, object>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M<object, object>").WithArguments("C.M<T1, T2>()", "I<object>", "T2", "object").WithLocation(13, 9));
        }

        [ClrOnlyFact]
        public void ContravariantInterfacesInConstraints()
        {
            var source =
@"
using System;
using System.Collections.Generic;

interface X<in T> { }
interface _<in T> : X<T> { }

class C
{
    static void Goo<T>() where T : class, X<_<X<T>>> {
        Goo<X<_<X<X<_<X<X<X<_<X<_<X<T>>>>>>>>>>>>>();
    }

    static void Main()
    {
    }
}
";
            CompileAndVerify(source, expectedOutput: "");
        }

        /// <summary>
        /// Redundant System.Object constraints should be removed
        /// and '.ctor' and System.ValueType constraints should be
        /// removed if 'valuetype' is specified. By contrast, redundant
        /// 'class' constraints should not be removed if explicit class
        /// constraint is specified.
        /// </summary>
        [WorkItem(543335, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543335")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void ObjectAndValueTypeMetadataConstraints()
        {
            var ilSource =
@".class public A { }
.class public O1<T> { }
.class public O2<(object)T> { }
.class public V1<valuetype T> { }
.class public V2<valuetype .ctor T> { }
.class public V3<valuetype ([mscorlib]System.ValueType) T> { }
.class public V4<valuetype .ctor ([mscorlib]System.ValueType) T> { }
.class public V5<([mscorlib]System.ValueType) T> { }
.class public R1<(A) T> { }
.class public R2<class (A) T> { }";
            var csharpSource = "";
            var compilation = CreateCompilationWithILAndMscorlib40(csharpSource, ilSource);
            var @namespace = compilation.GlobalNamespace;
            CheckConstraints(@namespace.GetMember<NamedTypeSymbol>("O1").TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object");
            CheckConstraints(@namespace.GetMember<NamedTypeSymbol>("O2").TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object");
            CheckConstraints(@namespace.GetMember<NamedTypeSymbol>("V1").TypeParameters[0], TypeParameterConstraintKind.ValueType, true, false, "ValueType", "ValueType");
            CheckConstraints(@namespace.GetMember<NamedTypeSymbol>("V2").TypeParameters[0], TypeParameterConstraintKind.ValueType, true, false, "ValueType", "ValueType");
            CheckConstraints(@namespace.GetMember<NamedTypeSymbol>("V3").TypeParameters[0], TypeParameterConstraintKind.ValueType, true, false, "ValueType", "ValueType");
            CheckConstraints(@namespace.GetMember<NamedTypeSymbol>("V4").TypeParameters[0], TypeParameterConstraintKind.ValueType, true, false, "ValueType", "ValueType");
            CheckConstraints(@namespace.GetMember<NamedTypeSymbol>("V5").TypeParameters[0], TypeParameterConstraintKind.None, false, false, "ValueType", "ValueType", "ValueType");
            CheckConstraints(@namespace.GetMember<NamedTypeSymbol>("R1").TypeParameters[0], TypeParameterConstraintKind.None, false, true, "A", "A", "A");
            CheckConstraints(@namespace.GetMember<NamedTypeSymbol>("R2").TypeParameters[0], TypeParameterConstraintKind.ReferenceType, false, true, "A", "A", "A");
        }

        [WorkItem(543335, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543335")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void ObjectAndValueTypeMethodMetadataConstraints()
        {
            var ilSource =
@".class public abstract A<T>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance void M1<(!T)U>() { }
  .method public abstract virtual instance void M2<valuetype (!T)U>() { }
}
.class public B0 extends class A<object>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public virtual instance void M1<U>() { ret }
  .method public virtual instance void M2<valuetype U>() { ret }
}
.class public B1 extends class A<object>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public virtual instance void M1<(object)U>() { ret }
  .method public virtual instance void M2<valuetype (object)U>() { ret }
}
.class public B2 extends class A<class [mscorlib]System.ValueType>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public virtual instance void M1<(class [mscorlib]System.ValueType)U>() { ret }
  .method public virtual instance void M2<valuetype (class [mscorlib]System.ValueType)U>() { ret }
}";
            var csharpSource = "";
            var compilation = CreateCompilationWithILAndMscorlib40(csharpSource, ilSource);
            var @namespace = compilation.GlobalNamespace;
            var type = @namespace.GetMember<NamedTypeSymbol>("B0");
            CheckConstraints(type.GetMember<MethodSymbol>("M1").TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object");
            CheckConstraints(type.GetMember<MethodSymbol>("M2").TypeParameters[0], TypeParameterConstraintKind.ValueType, true, false, "ValueType", "ValueType");
            type = @namespace.GetMember<NamedTypeSymbol>("B1");
            CheckConstraints(type.GetMember<MethodSymbol>("M1").TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object");
            CheckConstraints(type.GetMember<MethodSymbol>("M2").TypeParameters[0], TypeParameterConstraintKind.ValueType, true, false, "ValueType", "ValueType");
            type = @namespace.GetMember<NamedTypeSymbol>("B2");
            CheckConstraints(type.GetMember<MethodSymbol>("M1").TypeParameters[0], TypeParameterConstraintKind.None, false, false, "ValueType", "ValueType", "ValueType");
            CheckConstraints(type.GetMember<MethodSymbol>("M2").TypeParameters[0], TypeParameterConstraintKind.ValueType, true, false, "ValueType", "ValueType");
        }

        /// <summary>
        /// Overriding methods with implicit and explicit
        /// System.Object and System.ValueType constraints.
        /// </summary>
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void OverridingObjectAndValueTypeMethodMetadataConstraints()
        {
            var ilSource =
@".class interface public abstract IA
{
  .method public abstract virtual instance void M1<U>() { }
  .method public abstract virtual instance void M2<(object)U>() { }
}
.class interface public abstract IB
{
  .method public abstract virtual instance void M1<valuetype U>() { }
  .method public abstract virtual instance void M2<valuetype (object)U>() { }
}
.class interface public abstract IC
{
  .method public abstract virtual instance void M1<valuetype U>() { }
  .method public abstract virtual instance void M2<valuetype (class [mscorlib]System.ValueType)U>() { }
}
.class public abstract A<T>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance void M1<(!T)U>() { }
  .method public abstract virtual instance void M2<(!T)U>() { }
}
.class public abstract A0 extends class A<object>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance void M1<U>() { }
  .method public abstract virtual instance void M2<(object)U>() { }
}
.class public abstract B<T>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance void M1<valuetype (!T)U>() { }
  .method public abstract virtual instance void M2<valuetype (!T)U>() { }
}
.class public abstract B0 extends class B<object>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance void M1<valuetype U>() { }
  .method public abstract virtual instance void M2<valuetype (object)U>() { }
}
.class public abstract C0 extends class B<class [mscorlib]System.ValueType>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance void M1<valuetype U>() { }
  .method public abstract virtual instance void M2<valuetype (class [mscorlib]System.ValueType)U>() { }
}";
            var csharpSource =
@"class AImplicit : IA
{
    public void M1<U>() { }
    public void M2<U>() { }
}
class AExplicit : IA
{
    void IA.M1<U>() { }
    void IA.M2<U>() { }
}
class BImplicit : IB
{
    public void M1<U>() where U : struct { } // Dev10 error
    public void M2<U>() where U : struct { } // Dev10 error
}
class BExplicit : IB
{
    void IB.M1<U>() { }
    void IB.M2<U>() { }
}
class CImplicit : IC
{
    public void M1<U>() where U : struct { } // Dev10 error
    public void M2<U>() where U : struct { }
}
class CExplicit : IC
{
    void IC.M1<U>() { }
    void IC.M2<U>() { }
}
class A1 : A0
{
    public override void M1<U>() { }
    public override void M2<U>() { }
}
class B1 : B0
{
    public override void M1<U>() { }
    public override void M2<U>() { }
}
class C1 : C0
{
    public override void M1<U>() { }
    public override void M2<U>() { }
}";
            CreateCompilationWithILAndMscorlib40(csharpSource, ilSource).VerifyDiagnostics();
        }

        /// <summary>
        /// Object constraints should be dropped from TypeParameterSymbol.ConstraintTypes
        /// on import and type substitution.
        /// </summary>
        [WorkItem(543831, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543831")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void ObjectConstraintTypes()
        {
            var ilSource =
@".class interface public abstract I<T>
{
  .method public abstract virtual instance void M<(!T)U>() { }
}
.class interface public abstract I0 implements class I<object>
{
}
.class public abstract A<T>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance void M<(!T)U>() { }
}
.class public abstract A1 extends class A<object>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance void M<U>() { }
}
.class public abstract A2 extends class A<object>
{
  .method public specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance void M<(object)U>() { }
}";
            var csharpSource =
@"interface I1 : I<object>
{
}
class B0 : A<object>
{
    public override void M<U>() { }
}
class B1 : A1
{
    public override void M<U>() { }
}
class B2 : A2
{
    public override void M<U>() { }
}
class C0 : I0
{
    public void M<U>() { }
}
class C1 : I<object>
{
    public void M<U>() { }
}
class C2 : I<object>
{
    void I<object>.M<U>() { }
}
class D<T>
{
    public void M<U>() where U : T { }
}
class D0 : D<object>
{
}";
            var compilation = CreateCompilationWithILAndMscorlib40(csharpSource, ilSource).VerifyDiagnostics();
            var @namespace = compilation.GlobalNamespace;
            var type = @namespace.GetMember<NamedTypeSymbol>("I0");
            CheckConstraints(type.Interfaces()[0].GetMember<MethodSymbol>("M").TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object");
            type = @namespace.GetMember<NamedTypeSymbol>("A1");
            CheckConstraints(type.GetMember<MethodSymbol>("M").TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object");
            type = @namespace.GetMember<NamedTypeSymbol>("A2");
            CheckConstraints(type.GetMember<MethodSymbol>("M").TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object");
            type = @namespace.GetMember<NamedTypeSymbol>("I1");
            CheckConstraints(type.Interfaces()[0].GetMember<MethodSymbol>("M").TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object");
            type = @namespace.GetMember<NamedTypeSymbol>("B0");
            CheckConstraints(type.GetMember<MethodSymbol>("M").TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object");
            type = @namespace.GetMember<NamedTypeSymbol>("B1");
            CheckConstraints(type.GetMember<MethodSymbol>("M").TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object");
            type = @namespace.GetMember<NamedTypeSymbol>("B2");
            CheckConstraints(type.GetMember<MethodSymbol>("M").TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object");
            type = @namespace.GetMember<NamedTypeSymbol>("C0");
            CheckConstraints(type.GetMember<MethodSymbol>("M").TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object");
            type = @namespace.GetMember<NamedTypeSymbol>("C1");
            CheckConstraints(type.GetMember<MethodSymbol>("M").TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object");
            type = @namespace.GetMember<NamedTypeSymbol>("C2");
            CheckConstraints(type.GetMethod("I<System.Object>.M").TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object");
            type = @namespace.GetMember<NamedTypeSymbol>("D0");
            CheckConstraints(type.BaseType().GetMember<MethodSymbol>("M").TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object");
        }

        /// <summary>
        /// Object constraint should not be emitted
        /// for compatibility with Dev10.
        /// </summary>
        [WorkItem(543710, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543710")]
        [ClrOnlyFact]
        public void EmittedObjectConstraint()
        {
            var source =
@"class C { }
interface I<T, U> where U : T { }
interface I0<T> : I<object, T> { }
interface I1<T> : I<C, T> where T : C { }
abstract class A<T>
{
    public abstract void M<U>() where U : T;
}
class A0 : A<object>
{
    public override void M<U>() { }
}
class A1 : A<C>
{
    public override void M<U>() { }
}";
            Action<ModuleSymbol> validator = module =>
            {
                var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("I0");
                CheckConstraints(type.TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object");
                type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("I1");
                CheckConstraints(type.TypeParameters[0], TypeParameterConstraintKind.None, false, true, "C", "C", "C");
                var method = module.GlobalNamespace.GetMember<NamedTypeSymbol>("A0").GetMember<MethodSymbol>("M");
                CheckConstraints(method.TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object");
                method = module.GlobalNamespace.GetMember<NamedTypeSymbol>("A1").GetMember<MethodSymbol>("M");
                CheckConstraints(method.TypeParameters[0], TypeParameterConstraintKind.None, false, true, "C", "C", "C");
            };
            CompileAndVerify(
                source: source,
                sourceSymbolValidator: validator,
                symbolValidator: validator);
        }

        [Fact]
        public void InheritedObjectConstraint()
        {
            var source =
@"interface IA { }
interface IB : IA { }
class A : IA { }
class B : A, IB { }
abstract class C<T>
{
    public abstract void M1<U>() where U : T;
    public abstract void M2<U>() where U : struct, T;
}
class C0 : C<object>
{
    public override void M1<U>() { }
    public override void M2<U>() { }
}
class C1 : C<System.ValueType>
{
    public override void M1<U>() { }
    public override void M2<U>() { }
}
abstract class D<T>
{
    public abstract void M1<U>() where U : IA, T;
    public abstract void M2<U>() where U : IB, T;
    public abstract void M3<U>() where U : A, T;
    public abstract void M4<U>() where U : B, T;
}
class D0 : D<object>
{
    public override void M1<U>() { }
    public override void M2<U>() { }
    public override void M3<U>() { }
    public override void M4<U>() { }
}
class D1 : D<IA>
{
    public override void M1<U>() { }
    public override void M2<U>() { }
    public override void M3<U>() { }
    public override void M4<U>() { }
}
class D2 : D<A>
{
    public override void M1<U>() { }
    public override void M2<U>() { }
    public override void M3<U>() { }
    public override void M4<U>() { }
}";
            var compilation = CreateCompilation(source);
            var @namespace = compilation.GlobalNamespace;
            var type = @namespace.GetMember<NamedTypeSymbol>("C0");
            CheckConstraints(type.GetMember<MethodSymbol>("M1").TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object");
            CheckConstraints(type.GetMember<MethodSymbol>("M2").TypeParameters[0], TypeParameterConstraintKind.ValueType, true, false, "ValueType", "ValueType");
            type = @namespace.GetMember<NamedTypeSymbol>("C1");
            CheckConstraints(type.GetMember<MethodSymbol>("M1").TypeParameters[0], TypeParameterConstraintKind.None, false, false, "ValueType", "ValueType", "ValueType");
            CheckConstraints(type.GetMember<MethodSymbol>("M2").TypeParameters[0], TypeParameterConstraintKind.ValueType, true, false, "ValueType", "ValueType", "ValueType");
            type = @namespace.GetMember<NamedTypeSymbol>("D0");
            CheckConstraints(type.GetMember<MethodSymbol>("M1").TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object", "IA");
            CheckConstraints(type.GetMember<MethodSymbol>("M2").TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object", "IB");
            CheckConstraints(type.GetMember<MethodSymbol>("M3").TypeParameters[0], TypeParameterConstraintKind.None, false, true, "A", "A", "A");
            CheckConstraints(type.GetMember<MethodSymbol>("M4").TypeParameters[0], TypeParameterConstraintKind.None, false, true, "B", "B", "B");
            type = @namespace.GetMember<NamedTypeSymbol>("D1");
            CheckConstraints(type.GetMember<MethodSymbol>("M1").TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object", "IA");
            CheckConstraints(type.GetMember<MethodSymbol>("M2").TypeParameters[0], TypeParameterConstraintKind.None, false, false, "object", "object", "IB", "IA");
            CheckConstraints(type.GetMember<MethodSymbol>("M3").TypeParameters[0], TypeParameterConstraintKind.None, false, true, "A", "A", "A", "IA");
            CheckConstraints(type.GetMember<MethodSymbol>("M4").TypeParameters[0], TypeParameterConstraintKind.None, false, true, "B", "B", "B", "IA");
            type = @namespace.GetMember<NamedTypeSymbol>("D2");
            CheckConstraints(type.GetMember<MethodSymbol>("M1").TypeParameters[0], TypeParameterConstraintKind.None, false, true, "A", "A", "IA", "A");
            CheckConstraints(type.GetMember<MethodSymbol>("M2").TypeParameters[0], TypeParameterConstraintKind.None, false, true, "A", "A", "IB", "A");
            CheckConstraints(type.GetMember<MethodSymbol>("M3").TypeParameters[0], TypeParameterConstraintKind.None, false, true, "A", "A", "A");
            CheckConstraints(type.GetMember<MethodSymbol>("M4").TypeParameters[0], TypeParameterConstraintKind.None, false, true, "B", "B", "B", "A");
        }

        [WorkItem(545410, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545410")]
        [Fact]
        public void InheritedValueConstraintForNullable1()
        {
            var source = @"
class A
{
    public virtual T? Goo<T>() where T : struct 
    { 
        return null; 
    }
}

class B : A
{
    public override T? Goo<T>()
    {
        return null;
    }
} 
";
            CreateCompilation(source, options: TestOptions.ReleaseDll).VerifyDiagnostics();
        }

        [WorkItem(545410, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545410")]
        [Fact]
        public void InheritedValueConstraintForNullable2()
        {
            var source = @"
class A
{
    public virtual T? Goo<T>()
    { 
        return null; 
    }
}

class B : A
{
    public override T? Goo<T>()
    {
        return null;
    }
} 
";
            CreateCompilation(source, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (4,21): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' context.
                //     public virtual T? Goo<T>()
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(4, 21),
                // (4,20): error CS8627: A nullable type parameter must be known to be a value type or non-nullable reference type. Consider adding a 'class', 'struct', or type constraint.
                //     public virtual T? Goo<T>()
                Diagnostic(ErrorCode.ERR_NullableUnconstrainedTypeParameter, "T?").WithLocation(4, 20),
                // (12,22): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' context.
                //     public override T? Goo<T>()
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(12, 22),
                // (12,24): error CS0508: 'B.Goo<T>()': return type must be 'T' to match overridden member 'A.Goo<T>()'
                //     public override T? Goo<T>()
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "Goo").WithArguments("B.Goo<T>()", "A.Goo<T>()", "T").WithLocation(12, 24),
                // (12,24): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                //     public override T? Goo<T>()
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "Goo").WithArguments("System.Nullable<T>", "T", "T").WithLocation(12, 24),
                // (6,16): error CS0403: Cannot convert null to type parameter 'T' because it could be a non-nullable value type. Consider using 'default(T)' instead.
                //         return null; 
                Diagnostic(ErrorCode.ERR_TypeVarCantBeNull, "null").WithArguments("T").WithLocation(6, 16));
        }

        [WorkItem(543710, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543710")]
        [ClrOnlyFact]
        public void InheritedObjectConstraint2()
        {
            var csCompilation = CreateCSharpCompilation("InheritedObjectConstraint2CS",
@"using System;
public abstract class Base1<T>
{
    public virtual void Goo<G>(G d) where G : struct, T { Console.WriteLine(""Base1""); }
}
public class Base2 : Base1<Object>
{
    public override void Goo<G>(G d) { Console.WriteLine(""Base2""); }
}",
                compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var csVerifier = CompileAndVerify(csCompilation);
            csVerifier.VerifyDiagnostics();

            var vbCompilation = CreateVisualBasicCompilation("InheritedObjectConstraint2VB",
@"Imports System
Class Derived : Inherits Base2
    Public Overrides Sub Goo(Of G As Structure)(ByVal d As G)
        Console.WriteLine(""Derived"")
    End Sub
End Class

Module Program
    Sub Main
        Dim x As Base1(Of Object) = New Derived
        x.Goo(1)
    End Sub
End Module",
                compilationOptions: new Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                referencedCompilations: new[] { csCompilation });
            vbCompilation.VerifyDiagnostics();
        }

        private static void CheckConstraints(
            TypeParameterSymbol typeParameter,
            TypeParameterConstraintKind constraints,
            bool isValueType,
            bool isReferenceType,
            string effectiveBaseClassDescription,
            string deducedBaseTypeDescription,
            params string[] constraintTypeDescriptions)
        {
            Assert.Equal(constraints, Utils.GetTypeParameterConstraints(typeParameter));
            Assert.Equal(typeParameter.IsValueType, isValueType);
            Assert.Equal(typeParameter.IsReferenceType, isReferenceType);
            Assert.Null(typeParameter.BaseType());
            Assert.Equal(typeParameter.Interfaces().Length, 0);
            Utils.CheckSymbol(typeParameter.EffectiveBaseClassNoUseSiteDiagnostics, effectiveBaseClassDescription);
            Utils.CheckSymbol(typeParameter.DeducedBaseTypeNoUseSiteDiagnostics, deducedBaseTypeDescription);
            Utils.CheckSymbols(typeParameter.ConstraintTypes(), constraintTypeDescriptions);
        }

        [WorkItem(545327, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545327")]
        [Fact]
        public void MissingObjectType()
        {
            var source =
@"class A { }
class B<T> where T : A { }";
            CreateEmptyCompilation(source).VerifyDiagnostics(
                // (1,7): error CS0518: Predefined type 'System.Object' is not defined or imported
                // class A { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.Object").WithLocation(1, 7),
                // (2,7): error CS0518: Predefined type 'System.Object' is not defined or imported
                // class B<T> where T : A { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "B").WithArguments("System.Object").WithLocation(2, 7),
                // (2,7): error CS1729: 'object' does not contain a constructor that takes 0 arguments
                // class B<T> where T : A { }
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "B").WithArguments("object", "0").WithLocation(2, 7),
                // (1,7): error CS1729: 'object' does not contain a constructor that takes 0 arguments
                // class A { }
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "A").WithArguments("object", "0").WithLocation(1, 7));
        }

        [Fact]
        public void MissingValueType()
        {
            var source =
@"struct S { }
abstract class A<T>
{
    internal abstract void M<U>() where U : struct, T;
}
class B : A<S>
{
    internal override void M<U>() { }
}";
            CreateEmptyCompilation(source).VerifyDiagnostics(
                // (2,16): error CS0518: Predefined type 'System.Object' is not defined or imported
                // abstract class A<T>
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.Object").WithLocation(2, 16),
                // (1,8): error CS0518: Predefined type 'System.ValueType' is not defined or imported
                // struct S { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "S").WithArguments("System.ValueType").WithLocation(1, 8),
                // (4,23): error CS0518: Predefined type 'System.Void' is not defined or imported
                //     internal abstract void M<U>() where U : struct, T;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "void").WithArguments("System.Void").WithLocation(4, 23),
                // (8,23): error CS0518: Predefined type 'System.Void' is not defined or imported
                //     internal override void M<U>() { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "void").WithArguments("System.Void").WithLocation(8, 23),
                // (2,16): error CS1729: 'object' does not contain a constructor that takes 0 arguments
                // abstract class A<T>
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "A").WithArguments("object", "0").WithLocation(2, 16),
                // (6,7): error CS0518: Predefined type 'System.Void' is not defined or imported
                // class B : A<S>
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "B").WithArguments("System.Void").WithLocation(6, 7));
        }

        [WorkItem(11243, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void ConstraintGenericForPoint()
        {
            var source = @"
class A
{
    public interface I { }
}
class F<T> : A where T : F<object*>.I
{
}

class G<T> : A where T : G<void*>.I
{
}
class c
{
    static void Main() { }
}
";
            // NOTE: As in Dev10, we don't report that object* and void* are invalid type arguments, since validation
            // is performed on A.I, not on F<object*>.I or G<void*>.I.
            // BREAKING: Dev10 (incorrectly) fails to report that "object*" is an illegal type since the pointed-at
            // type is managed.
            CreateCompilation(source).VerifyDiagnostics(
                // (6,28): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('object')
                // class F<T> : A where T : F<object*>.I
                Diagnostic(ErrorCode.ERR_ManagedAddr, "object*").WithArguments("object"));
        }

        [WorkItem(545460, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545460")]
        [Fact]
        public void TypeConstrainedToLambda()
        {
            var source =
@"abstract class A<T>
{
    public abstract void M<U>(U u) where U : () => T;
}
class B : A<int>
{
    public override void M<U>(U u) { }
}";
            var compilation = CreateCompilation(source);
            Assert.NotEmpty(compilation.GetDiagnostics());
        }

        [WorkItem(545460, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545460")]
        [Fact]
        public void TypeConstrainedToErrorType()
        {
            var source =
@"abstract class A<T>
{
    public abstract void M<U>(U u) where U : X, T;
}
class B : A<int>
{
    public override void M<U>(U u) { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,46): error CS0246: The type or namespace name 'X' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "X").WithArguments("X").WithLocation(3, 46));
        }

        [WorkItem(545588, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545588")]
        [Fact]
        public void SatisfyOwnConstraints01()
        {
            var source =
@"struct S { }
enum E { }
class A<T>
{
    internal virtual void M<U>() where U : T { }
}
class B1 : A<int>
{
    internal override void M<U>() { base.M<U>(); }
}
class B2 : A<S>
{
    internal override void M<U>() { base.M<U>(); }
}
class B3 : A<E>
{
    internal override void M<U>() { base.M<U>(); }
}
class B4 : A<int?>
{
    internal override void M<U>() { base.M<U>(); }
}
class B5 : A<object[]>
{
    internal override void M<U>() { base.M<U>(); }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [WorkItem(545588, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545588")]
        [Fact]
        public void SatisfyOwnConstraints02()
        {
            var source =
@"class A<T1, T2>
{
    internal virtual void M0<U>() where U : T1, T2 { }
    internal virtual void M1<U>() where U : T1 { }
    internal virtual void M2<U>() where U : T2 { }
}
class B : A<int, object>
{
    internal override void M0<U>()
    {
        base.M0<U>();
        base.M1<U>();
        base.M2<U>();
    }
    internal override void M1<U>()
    {
        base.M0<U>();
        base.M1<U>();
        base.M2<U>();
    }
    internal override void M2<U>()
    {
        base.M0<U>();
        base.M1<U>();
        base.M2<U>();
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (23,14): error CS0314: The type 'U' cannot be used as type parameter 'U' in the generic type or method 'A<int, object>.M0<U>()'. There is no boxing conversion or type parameter conversion from 'U' to 'int'.
                //         base.M0<U>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar, "M0<U>").WithArguments("A<int, object>.M0<U>()", "int", "U", "U").WithLocation(23, 14),
                // (24,14): error CS0314: The type 'U' cannot be used as type parameter 'U' in the generic type or method 'A<int, object>.M1<U>()'. There is no boxing conversion or type parameter conversion from 'U' to 'int'.
                //         base.M1<U>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar, "M1<U>").WithArguments("A<int, object>.M1<U>()", "int", "U", "U").WithLocation(24, 14));
        }

        [WorkItem(545588, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545588")]
        [Fact]
        public void SatisfyOwnConstraints03()
        {
            var source =
@"class A<T>
{
    internal virtual void M1<U, V>() where U : T where V : U { }
    internal virtual void M2<U>() where U : T { }
}
class B : A<object[]>
{
    internal override void M1<U, V>()
    {
        base.M1<U, U>();
        base.M1<U, V>();
        base.M1<V, U>();
        base.M1<V, V>();
        base.M2<U>();
        base.M2<V>();
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (12,14): error CS0311: The type 'U' cannot be used as type parameter 'V' in the generic type or method 'A<object[]>.M1<U, V>()'. There is no implicit reference conversion from 'U' to 'V'.
                //         base.M1<V, U>();
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M1<V, U>").WithArguments("A<object[]>.M1<U, V>()", "V", "V", "U").WithLocation(12, 14));
        }

        [Fact]
        public void ExtensionMethodOnArrayInterface()
        {
            var source =
@"using System.Collections;
using System.Collections.Generic;
abstract class A<T>
{
    internal abstract void M<U>(U o) where U : T;
    internal static void M1(IEnumerable o) { }
    internal static void M2(IEnumerable<object> o) { }
    internal T F() { return default(T); }
}
class B : A<object[]>
{
    internal override void M<U>(U o)
    {
        M1(o);
        M2(o);
        M1(F());
        M2(F());
        o.E1();
        o.E2();
        F().E1();
        F().E2();
    }
}
static class M
{
    internal static void E1(this IEnumerable o) { }
    internal static void E2(this IEnumerable<object> o) { }
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (15,12): error CS1503: Argument 1: cannot convert from 'U' to 'System.Collections.Generic.IEnumerable<object>'
                //         M2(o);
                Diagnostic(ErrorCode.ERR_BadArgType, "o").WithArguments("1", "U", "System.Collections.Generic.IEnumerable<object>"),
                // (19,9): error CS1929: 'U' does not contain a definition for 'E2' and the best extension method overload 'M.E2(System.Collections.Generic.IEnumerable<object>)' requires a receiver of type 'System.Collections.Generic.IEnumerable<object>'
                //         o.E2();
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "o").WithArguments("U", "E2", "M.E2(System.Collections.Generic.IEnumerable<object>)", "System.Collections.Generic.IEnumerable<object>")
                );
        }

        /// <summary>
        /// Constraint failures on derived type when referencing members of
        /// base type. Dev11 does not report errors on such constraint failures
        /// for base type, interfaces, or method signatures.
        /// </summary>
        [Fact]
        public void MembersOfBaseTypeConstraintViolationOnDerived()
        {
            var source =
@"class A
{
    internal class B { }
    internal interface I { }
    internal static object F = null;
}
class C<T> : A where T : struct { }
class C1 { }
class C2 { }
class C3 { }
class C4 { }
class C5 { }
class C6 { }
class D : C<C1>.B, C<C2>.I { } // Dev11: no errors
class E
{
    static C<C3>.B M1(C<C4>.B o) // Dev11: no errors
    {
        return null;
    }
    static void M2()
    {
        object o;
        o = new C<C5>.B();
        o = C<C6>.F;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (24,19): error CS0453: The type 'C5' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'C<T>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "C5").WithArguments("C<T>", "T", "C5").WithLocation(24, 19),
                // (25,15): error CS0453: The type 'C6' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'C<T>'
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "C6").WithArguments("C<T>", "T", "C6").WithLocation(25, 15));
        }

        /// <summary>
        /// Cycle with field types with new() constraint.
        /// </summary>
        [WorkItem(546394, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546394")]
        [Fact]
        public void HasPublicParameterlessConstructorCycle01()
        {
            var source =
@"class A
{
    C<B> F;
}
class B
{
    C<A> F;
}
class C<T> where T : new() { }";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,10): warning CS0169: The field 'A.F' is never used
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("A.F").WithLocation(3, 10),
                // (7,10): warning CS0169: The field 'B.F' is never used
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("B.F").WithLocation(7, 10));
        }

        /// <summary>
        /// Cycle with event types with new() constraint.
        /// </summary>
        [WorkItem(546394, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546394")]
        [Fact]
        public void HasPublicParameterlessConstructorCycle02()
        {
            var source =
@"class A
{
    event D<B> E;
}
class B
{
    private B() { }
    event D<A> E;
}
class C
{
    private C() { }
    event D<C> E { add { } remove { } }
}
delegate D<T> D<T>() where T : new();";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,13): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'D<T>'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "E").WithArguments("D<T>", "T", "B").WithLocation(3, 16),
                // (13,16): error CS0310: 'C' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'D<T>'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "E").WithArguments("D<T>", "T", "C").WithLocation(13, 16),
                // (8,16): warning CS0067: The event 'B.E' is never used
                //     event D<A> E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("B.E"),
                // (3,16): warning CS0067: The event 'A.E' is never used
                //     event D<B> E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("A.E"));
        }

        /// <summary>
        /// Cycle with field-like event type with new() constraint
        /// where field type is determined by an initializer.
        /// </summary>
        [WorkItem(546394, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546394")]
        [Fact]
        public void HasPublicParameterlessConstructorCycle03()
        {
            var source =
@"class A
{
    event D<object> E2 = new D<B>(() => { });
}
class B
{
    private B() { }
    event D<object> E1 = new D<A>(() => { });
}
delegate void D<out T>() where T : new();";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,32): error CS0310: 'B' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'D<T>'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "B").WithArguments("D<T>", "T", "B").WithLocation(3, 32));
        }

        /// <summary>
        /// Cycle with event type with new() constraint where
        /// the event is an explicit implementation.
        /// </summary>
        [WorkItem(546394, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546394")]
        [Fact]
        public void HasPublicParameterlessConstructorCycle04()
        {
            var source =
@"delegate D<T> D<T>();
interface I<T> where T : new()
{
    event D<T> E;
}
class C : I<C>
{
    private C() { }
    event D<C> I<C>.E { add { } remove { } }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,7): error CS0310: 'C' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'I<T>'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "C").WithArguments("I<T>", "T", "C").WithLocation(6, 7),
                // (9,16): error CS0310: 'C' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'I<T>'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "I<C>").WithArguments("I<T>", "T", "C").WithLocation(9, 16));
        }

        /// <summary>
        /// Cycle with property types with new() constraint.
        /// </summary>
        [WorkItem(546394, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546394")]
        [Fact]
        public void HasPublicParameterlessConstructorCycle05()
        {
            var source =
@"class A
{
    C<B> P { get; set; }
}
class B
{
    C<A> P { get; set; }
}
class C<T> where T : new() { }";
            CreateCompilation(source).VerifyDiagnostics();
        }

        /// <summary>
        /// Cycle with property types with new() constraint where the types
        /// are parameter types and properties are explicit implementations.
        /// </summary>
        [WorkItem(546394, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546394")]
        [Fact]
        public void HasPublicParameterlessConstructorCycle06()
        {
            var source =
@"interface IA<T> where T : new()
{
    IA<T> P { get; }
}
interface IB<T> where T : new()
{
    object this[IB<T> i] { get; }
}
class A1 : IA<A1>
{
    private A1() { }
    IA<A1> IA<A1>.P { get { return null; } }
}
class A2 : IA<A2>
{
    private A2() { }
    public IA<A2> P { get { return null; } }
}
class B1 : IB<B1>
{
    private B1() { }
    object IB<B1>.this[IB<B1> i] { get { return null; } }
}
class B2 : IB<B2>
{
    private B2() { }
    public object this[IB<B2> i] { get { return null; } }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (24,7): error CS0310: 'B2' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'IB<T>'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "B2").WithArguments("IB<T>", "T", "B2").WithLocation(24, 7),
                // (14,7): error CS0310: 'A2' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'IA<T>'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "A2").WithArguments("IA<T>", "T", "A2").WithLocation(14, 7),
                // (9,7): error CS0310: 'A1' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'IA<T>'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "A1").WithArguments("IA<T>", "T", "A1").WithLocation(9, 7),
                // (19,7): error CS0310: 'B1' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'IB<T>'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "B1").WithArguments("IB<T>", "T", "B1").WithLocation(19, 7),
                // (22,12): error CS0310: 'B1' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'IB<T>'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "IB<B1>").WithArguments("IB<T>", "T", "B1").WithLocation(22, 12),
                // (22,31): error CS0310: 'B1' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'IB<T>'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "i").WithArguments("IB<T>", "T", "B1").WithLocation(22, 31),
                // (12,12): error CS0310: 'A1' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'IA<T>'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "IA<A1>").WithArguments("IA<T>", "T", "A1").WithLocation(12, 12),
                // (12,19): error CS0310: 'A1' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'IA<T>'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "P").WithArguments("IA<T>", "T", "A1").WithLocation(12, 19),
                // (17,19): error CS0310: 'A2' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'IA<T>'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "P").WithArguments("IA<T>", "T", "A2").WithLocation(17, 19),
                // (27,31): error CS0310: 'B2' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'IB<T>'
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "i").WithArguments("IB<T>", "T", "B2").WithLocation(27, 31));
        }

        [WorkItem(546780, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546780")]
        [Fact]
        public void Bug16806()
        {
            var source =
@"class A<T>
{
    class B<U> : A<object>
    {
        class C : B<> { }
        object F = typeof(B<>);
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,19): error CS7003: Unexpected use of an unbound generic name
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "B<>").WithLocation(5, 19));
        }

        [WorkItem(546972, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546972")]
        [Fact]
        public void Bug17407()
        {
            var source =
@"
public class Test
{
    public static void Main()
    {
    }
}

// TEST 1
public class GClass<T, U>
    where T : U
    where U : class
{ }
public class Test1
{
    public static void RunTest()
    {
        new GClass<int?, object>();
    }
}

// TEST 2
public class GNew<T, U>
    where T : U
    where U : new()
{ }
public class Test2
{
    public static void RunTest()
    {
        new GNew<int?, object>();
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [WorkItem(531227, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531227")]
        [Fact]
        public void ConstraintOverrideBaseTypeCycle()
        {
            var text = @"
public class Base<T> where T : new()
{
    public virtual int P { get; set; }
}

public class Derived : Base<Derived>
{
    public override int P { get; set; }
}
";

            var comp = CreateCompilation(text);
            var derivedType = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
            derivedType.GetMembers();
        }

        [WorkItem(531227, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531227")]
        [Fact]
        public void ConstraintExplicitImplementationInterfaceCycle()
        {
            var text = @"
public interface Interface<T> where T : new()
{
    int P { get; set; }
}

public class Implementation : Interface<Implementation>
{
    int Interface<Implementation>.P { get; set; }
}
";

            var comp = CreateCompilation(text);
            var implementingType = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Implementation");
            implementingType.GetMembers();
        }

        [WorkItem(546973, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546973")]
        [Fact]
        public void AllowBadConstraintsInMetadata()
        {
            var ilSource =
@".assembly extern mscorlib
{
  .ver 0:0:0:0
}
.assembly '<<GeneratedFileName>>'
{
  .ver 0:0:0:0
}
.class public auto ansi beforefieldinit GStruct`2<(!U) T,valuetype U>
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method GStruct`2::.ctor

} // end of class GStruct`2
";
            var source =
@"
public interface I { }
public struct S : I { }
 
public class Test5
{
    public static void RunTest()
    {
        GStruct<S, S> obj14 = new GStruct<S, S>();
        GStruct<int, int> obj15 = new GStruct<int, int>();
    }
}";
            CreateCompilationWithILAndMscorlib40(source, ilSource, appendDefaultHeader: false).VerifyDiagnostics();
        }

        [WorkItem(531630, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531630")]
        [Fact]
        public void CheckConstraintOnArrayTypeArgument()
        {
            var source = @"
public struct S
{
    E?[] eNullableArr;
    public void DoSomething<T>(T t) { }
    public void Test() {   DoSomething(this.eNullableArr);  }
}
 
public class E { }
";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,6): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' context.
                //     E?[] eNullableArr;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(4, 6),
                // (4,10): warning CS0649: Field 'S.eNullableArr' is never assigned to, and will always have its default value null
                //     E?[] eNullableArr;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "eNullableArr").WithArguments("S.eNullableArr", "null").WithLocation(4, 10));
        }

        [WorkItem(575455, "DevDiv")]
        [Fact]
        public void UseSiteErrorReportingCycleInBaseReference()
        {
            var source1 =
@"public class A { }
public interface IA { }";
            var compilation1 = CreateCompilation(source1, assemblyName: "e521fe98-c881-45cf-8870-249e00ae400d");
            compilation1.VerifyDiagnostics();
            var source2 =
@"public class B : A { }
public class C<T> : B where T : B { }
public interface IB : IA { }
public interface IC<T> : IB where T : IB { }";
            var compilation2 = CreateCompilation(source2, references: new MetadataReference[] { new CSharpCompilationReference(compilation1) });
            compilation2.VerifyDiagnostics();
            var source3 =
@"class D : C<D>, IC<D> { }";
            var compilation3 = CreateCompilation(source3, references: new MetadataReference[] { new CSharpCompilationReference(compilation2) });
            compilation3.VerifyDiagnostics(
                // (1,11): error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'e521fe98-c881-45cf-8870-249e00ae400d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                // class D : C<D>, IC<D> { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "C<D>").WithArguments("A", "e521fe98-c881-45cf-8870-249e00ae400d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 11),
                // (1,7): error CS0012: The type 'IA' is defined in an assembly that is not referenced. You must add a reference to assembly 'e521fe98-c881-45cf-8870-249e00ae400d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                // class D : C<D>, IC<D> { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "D").WithArguments("IA", "e521fe98-c881-45cf-8870-249e00ae400d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (1,7): error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'e521fe98-c881-45cf-8870-249e00ae400d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                // class D : C<D>, IC<D> { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "D").WithArguments("A", "e521fe98-c881-45cf-8870-249e00ae400d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"),
                // (1,7): error CS0012: The type 'IA' is defined in an assembly that is not referenced. You must add a reference to assembly 'e521fe98-c881-45cf-8870-249e00ae400d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                // class D : C<D>, IC<D> { }
                Diagnostic(ErrorCode.ERR_NoTypeDef, "D").WithArguments("IA", "e521fe98-c881-45cf-8870-249e00ae400d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 7));
        }

        [WorkItem(577251, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/577251")]
        [Fact]
        public void Bug577251()
        {
            var source =
@"interface IA<T> { }
interface IB
{
    void F<T>() where T : IA<C<int>.E*[]>;
}
class C<T>
{
    public enum E { }
    public void F<U>() where U : IA<E*[]> { }
}
class D : C<int>, IB { }";
            CreateCompilation(source).VerifyDiagnostics();
            source =
@"interface IA<T> { }
interface IB
{
    void F<T, U>() where T : IA<C<U>.E*[]>;
}
class C<T>
{
    public enum E { }
    public void F<U, V>() where U : IA<C<V>.E*[]> { }
}
class D<T> : C<T>, IB { }";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [WorkItem(578350, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578350")]
        [Fact]
        public void Bug578350()
        {
            var source =
@"class C<T> { }
interface I
{
    void F<T>() where T : C<object>;
}
abstract class A<T>
{
    public virtual void F<U>() where U : C<T> { }
}
class B0 : A<object>, I
{
}
// Dev11: CS0425: 'The constraints for type parameter 'U' of method 'A<dynamic>.F<U>()' must match ...'
class B1 : A<dynamic>, I
{
}
// Dev11: No error
class B2 : A<dynamic>, I
{
    public override void F<T>() { }
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
        }

        [WorkItem(654522, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/654522")]
        [ClrOnlyFact]
        public void Bug654522()
        {
            var compilation = CreateCompilationWithMscorlib40AndSystemCore("public interface I<W> where W : struct {}").VerifyDiagnostics();

            Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    var metadata = ((PEModuleSymbol)module).Module;

                    var typeI = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("I").Single();
                    Assert.Equal(1, typeI.TypeParameters.Length);

                    var tp = (PETypeParameterSymbol)typeI.TypeParameters[0];

                    string name;
                    GenericParameterAttributes flags;
                    metadata.GetGenericParamPropsOrThrow(tp.Handle, out name, out flags);
                    Assert.Equal(GenericParameterAttributes.DefaultConstructorConstraint, flags & GenericParameterAttributes.DefaultConstructorConstraint);

                    var metadataReader = metadata.MetadataReader;
                    var constraints = metadataReader.GetGenericParameter(tp.Handle).GetConstraints();
                    Assert.Equal(1, constraints.Count);

                    var tokenDecoder = new MetadataDecoder((PEModuleSymbol)module, typeI);
                    var constraintTypeHandle = metadataReader.GetGenericParameterConstraint(constraints[0]).Type;
                    TypeSymbol typeSymbol = tokenDecoder.GetTypeOfToken(constraintTypeHandle);
                    Assert.Equal(SpecialType.System_ValueType, typeSymbol.SpecialType);
                };

            CompileAndVerify(compilation, symbolValidator: metadataValidator);
        }

        [Fact]
        public void Bug578762()
        {
            var source =
@"interface IA<T> { }
interface IB<T> : IA<T> { }
interface IC<T> : IB<T> { }
static class M
{
    internal static void E1<T>(this IA<T> o) { }
    internal static void E2<T>(this IB<T> o) { }
    internal static void E3<T>(this IC<T> o) { }
}
class C
{
    static void F<T>(T o) where T : IA<string>, IC<string>, IB<string>
    {
        o.E1();
        o.E2();
        o.E3();
    }
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
            source =
@"class A<T> { }
class B<T> : A<T> { }
class C<T> : B<T> { }
static class M
{
    internal static void E1<T>(this A<T> o) { }
    internal static void E2<T>(this B<T> o) { }
    internal static void E3<T>(this C<T> o) { }
}
abstract class D<T, U, V>
{
    internal abstract void F<X>(X o) where X : T, U, V;
}
class E : D<A<string>, C<string>, B<string>>
{
    internal override void F<X>(X o)
    {
        o.E1();
        o.E2();
        o.E3();
    }
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics();
            source =
@"interface IA<T> { }
class A<T> : IA<T> { }
class B<T> : A<T>, IA<object> { }
static class M
{
    internal static void E0(this IA<object> o) { }
    internal static void E1<T>(this IA<T> o) { }
}
abstract class C<T, U>
{
    internal abstract void F<X>(X o) where X : T, U;
}
class D1 : C<A<string>, B<string>>
{
    internal override void F<X>(X o)
    {
        o.E0();
        o.E1();
    }
}
class D2 : C<B<object>, A<object>>
{
    internal override void F<X>(X o)
    {
        o.E0();
        o.E1();
    }
}";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (18,9): error CS1061: 'X' does not contain a definition for 'E1' and no extension method 'E1' accepting a first argument of type 'X' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "E1").WithArguments("X", "E1").WithLocation(18, 11));
        }

        [Fact]
        public void AccessProtectedMemberOnInstance_1()
        {
            var source =
@"delegate void D();
class A
{
    protected object F = 1;
    protected object G() { return null; }
    protected object P { get; set; }
    protected object this[int index] { get { return null; } set { } }
    protected event D E;
}
class B<T> : A where T : A
{
    static void M(T t)
    {
        object o;
        o = t.F;
        o = t.G();
        o = t.P;
        t.P = o;
        o = t[1];
        t[2] = o;
        t.E += null;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (15,15): error CS1540: Cannot access protected member 'A.F' via a qualifier of type 'A'; the qualifier must be of type 'B<T>' (or derived from it)
                //         o = t.F;
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "F").WithArguments("A.F", "A", "B<T>"),
                // (16,15): error CS1540: Cannot access protected member 'A.G()' via a qualifier of type 'A'; the qualifier must be of type 'B<T>' (or derived from it)
                //         o = t.G();
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "G").WithArguments("A.G()", "A", "B<T>"),
                // (17,15): error CS1540: Cannot access protected member 'A.P' via a qualifier of type 'A'; the qualifier must be of type 'B<T>' (or derived from it)
                //         o = t.P;
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "P").WithArguments("A.P", "A", "B<T>"),
                // (18,11): error CS1540: Cannot access protected member 'A.P' via a qualifier of type 'A'; the qualifier must be of type 'B<T>' (or derived from it)
                //         t.P = o;
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "P").WithArguments("A.P", "A", "B<T>"),
                // (19,13): error CS1540: Cannot access protected member 'A.this[int]' via a qualifier of type 'A'; the qualifier must be of type 'B<T>' (or derived from it)
                //         o = t[1];
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "t[1]").WithArguments("A.this[int]", "A", "B<T>"),
                // (20,9): error CS1540: Cannot access protected member 'A.this[int]' via a qualifier of type 'A'; the qualifier must be of type 'B<T>' (or derived from it)
                //         t[2] = o;
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "t[2]").WithArguments("A.this[int]", "A", "B<T>"),
                // (21,11): error CS1540: Cannot access protected member 'A.E' via a qualifier of type 'A'; the qualifier must be of type 'B<T>' (or derived from it)
                //         t.E += null;
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "E").WithArguments("A.E", "A", "B<T>"),
                // (8,23): warning CS0067: The event 'A.E' is never used
                //     protected event D E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("A.E"));
        }

        [WorkItem(746999, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/746999")]
        [Fact]
        public void AccessProtectedMemberOnInstance_2()
        {
            var source =
@"delegate void D();
class A
{
    protected object F = 1;
    protected object G() { return null; }
    protected object P { get; set; }
    protected object this[int index] { get { return null; } set { } }
    protected event D E;
}
class B<T> : A where T : B<T>
{
    static void M(T t)
    {
        object o;
        o = t.F;
        o = t.G();
        o = t.P;
        t.P = o;
        o = t[1];
        t[2] = o;
        t.E += null;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (8,23): warning CS0067: The event 'A.E' is never used
                //     protected event D E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("A.E"));
        }

        [WorkItem(746999, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/746999")]
        [Fact]
        public void AccessProtectedMemberOnInstance_3()
        {
            var source =
@"class A
{
    protected object F = 1;
    protected object P { get { return null; } }
}
class B : A
{
    class C1<T, U, V>
        where T : A
        where U : B
        where V : T, U
    {
        static void M(T t, U u, V v)
        {
            object o;
            o = t.F;
            o = t.P;
            o = u.F;
            o = u.P;
            o = v.F;
            o = v.P;
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (16,19): error CS1540: Cannot access protected member 'A.F' via a qualifier of type 'A'; the qualifier must be of type 'B.C1<T, U, V>' (or derived from it)
                //             o = t.F;
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "F").WithArguments("A.F", "A", "B.C1<T, U, V>"),
                // (17,19): error CS1540: Cannot access protected member 'A.P' via a qualifier of type 'A'; the qualifier must be of type 'B.C1<T, U, V>' (or derived from it)
                //             o = t.P;
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "P").WithArguments("A.P", "A", "B.C1<T, U, V>"));
        }

        [WorkItem(767334, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/767334")]
        [ClrOnlyFact]
        public void ConstraintOnSynthesizedExplicitImplementationMethod()
        {
            var source1 =
@"public interface I0
{
    void M<T>() where T : A;
}
public interface I1<T>
{
    void M<U>() where U : T;
}
public class A { }
public class B0
{
    public void M<T>() { }
}
public class B<T>
{
    public void M<U>() where U : T { }
    public interface I2<U>
    {
        void M<V>() where V : T, U;
    }
}";
            var compilation1 = CreateCompilation(source1);
            compilation1.VerifyDiagnostics();
            var source2 =
@"class C0 : B0, I1<object> { }
class C1 : B<A>, I0 { }
class C2 : B<A>, I1<A> { }
class C3 : B0, B<object>.I2<object> { }
class C4 : B<A>, B<object>.I2<A> { }
class C<T> : B<T>, B<object>.I2<T> { }
class P
{
    static void Main()
    {
        System.Console.WriteLine(new C0());
        System.Console.WriteLine(new C1());
        System.Console.WriteLine(new C2());
        System.Console.WriteLine(new C3());
        System.Console.WriteLine(new C4());
        System.Console.WriteLine(new C<A>());
    }
}";
            var compilation2 = CreateCompilation(
                source2,
                references: new MetadataReference[] { MetadataReference.CreateFromImage(compilation1.EmitToArray()) },
                options: TestOptions.ReleaseExe);
            compilation2.VerifyDiagnostics();
            CompileAndVerify(compilation2, expectedOutput:
@"C0
C1
C2
C3
C4
C`1[A]");
        }

        [WorkItem(837422, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/837422")]
        [ClrOnlyFact]
        public void RedundantValueTypeConstraint()
        {
            var source =
@"using System;
interface I<T>
{
    void M<U>() where U : struct, T;
}
abstract class A<T>
{
    internal abstract void M<U>() where U : struct, T;
}
class B : A<ValueType>, I<ValueType>
{
    internal override void M<T>() { }
    void I<ValueType>.M<T>() { }
}";
            CompileAndVerify(source);
        }

        [WorkItem(4097, "https://github.com/dotnet/roslyn/issues/4097")]
        [Fact]
        public void ObsoleteTypeInConstraints()
        {
            var source =
@"
[System.Obsolete]
class Class1<T> where T : Class2
{
}

[System.Obsolete]
class Class2
{
}

class Class3<T> where T : Class2
{
    [System.Obsolete]
    void M1<S>() where S : Class2
    {}   

    void M2<S>() where S : Class2
    {}   
}

partial class Class4
{
    [System.Obsolete]
    partial void M3<S>() where S : Class2;
}

partial class Class4
{
    partial void M4<S>() where S : Class2;
}
";
            CompileAndVerify(source, options: TestOptions.DebugDll).VerifyDiagnostics(
    // (12,27): warning CS0612: 'Class2' is obsolete
    // class Class3<T> where T : Class2
    Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "Class2").WithArguments("Class2").WithLocation(12, 27),
    // (18,28): warning CS0612: 'Class2' is obsolete
    //     void M2<S>() where S : Class2
    Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "Class2").WithArguments("Class2").WithLocation(18, 28),
    // (30,36): warning CS0612: 'Class2' is obsolete
    //     partial void M4<S>() where S : Class2;
    Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "Class2").WithArguments("Class2").WithLocation(30, 36)
                );
        }

        [Fact]
        [WorkItem(278264, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=278264")]
        public void IntPointerConstraintIntroducedBySubstitution()
        {
            string source = @"
class R1<T1>
{
    public virtual void f<T2>() where T2 : T1 { }
}
class R2 : R1<int*>
{
    public override void f<T2>() { }
}
class Program
{
    static void Main(string[] args)
    {
        R2 r = new R2();
        r.f<int>();
    }
}";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,7): error CS0306: The type 'int*' may not be used as a type argument
                // class R2 : R1<int *>
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "R2").WithArguments("int*").WithLocation(6, 7)
                );
        }
    }
}
