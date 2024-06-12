// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    /// <summary>
    /// Test MethodSymbol.OverriddenOrHiddenMembers and PropertySymbol.OverriddenOrHiddenMembers.
    /// </summary>
    public class OverriddenOrHiddenMembersTests : CSharpTestBase
    {
        [Fact]
        public void TestOverridingGenericMethods()
        {
            // When dealing with one generic method that overrides another, things get a bit
            // complicated. Basically we need to draw a distinction between the method
            // *definition* that is overridden, and the method *reference* that is overridden.
            // We also need to draw a distinction between the *immediately* overridden method
            // and the *original* virtual or abstract method.
            //
            // Consider D.M<V>. What method does it override?  One could make several arguments.
            // First, one could say that *as a definition*, the method D.M<V>(int, V) overrides 
            // the definition B<T>.M<U>(T, U).  That is a bit unsatisfying because the signatures
            // do not match, and it seems implausible that two methods with different signatures
            // can be overrides.
            //
            // Second, one could say that *as a definition*, the method D.M<V>(int, V) overrides
            // the definition B<int>.M<U>(int, U). That is somewhat more satisfying, as now the
            // signatures differ only in terms of generic type substitution of V for U.
            //
            // Third, one could say that *as a reference*, the method D.M<V>(int, V) overrides
            // the method *reference* B<int>.M<V>(int, V). Now we have an exact signature match.
            //
            // Now consider D.M<string>(int, string). What does it override? There are four
            // possibilities: (1) it doesn't override any method because D.M<string> is not a
            // definition, it is a reference, (2) B<T>.M<U>, (3) B<int>.M<U>, (4) B<int>.M<string>.
            //
            // The "OverriddenMethod" property says that D.M<V> overrides B<int>.M<U> and
            // D.M<string> overrides nothing.  The "OriginalOverriddenMethod" property says
            // that D.M<string> overrides B<int>.M<string>.
            //
            // Also, the "OverriddenMethod" property will tell you what abstract, virtual
            // *or overriding* method a given method overrides; that is, it looks up
            // the base class hierarchy until it finds a first match.  By contrast the
            // "OriginalOverriddenMethod" property looks all the way up to find the original
            // virtual or abstract method.

            // Let's see this in action:

            var text = @"
class B<T>
{
  public virtual void M<U>(T t, U u) {}
}
class D : B<D>
{
  public override void M<V>(D d, V v)
  {
  }
}
";

            var tree = Parse(text);
            var comp = CreateCompilation(tree);

            var global = comp.GlobalNamespace;

            // Types
            /* B<T> */
            var BofT = global.GetMember<NamedTypeSymbol>("B");
            /* D    */
            var D = global.GetMember<NamedTypeSymbol>("D");
            /* <D>  */
            var ofD = ImmutableArray.Create<TypeSymbol>(D);
            /* B<D> */
            var BofD = BofT.Construct(ofD);

            // Methods
            /* B<T>.M<U> */
            var BofT_MofU = BofT.GetMember<MethodSymbol>("M");
            /* B<D>.M<U> */
            var BofD_MofU = BofD.GetMember<MethodSymbol>("M");
            /* B<D>.M<D> */
            var BofD_MofD = BofD_MofU.Construct(ofD);
            /* D.M<V>    */
            var D_MofV = D.GetMember<MethodSymbol>("M");
            /* <V>       */
            var ofV = ImmutableArray.Create<TypeSymbol>(D_MofV.TypeParameters[0]);
            /* D.M<D>    */
            var D_MofD = D_MofV.Construct(ofD);
            /* B<D>.M<V> */
            var BofD_MofV = BofD_MofU.Construct(ofV);

            // Nothing is hidden:
            AssertSame(0, BofT_MofU.OverriddenOrHiddenMembers.HiddenMembers.Length);
            AssertSame(0, BofD_MofU.OverriddenOrHiddenMembers.HiddenMembers.Length);
            AssertSame(0, BofD_MofD.OverriddenOrHiddenMembers.HiddenMembers.Length);
            AssertSame(0, D_MofV.OverriddenOrHiddenMembers.HiddenMembers.Length);
            AssertSame(0, D_MofD.OverriddenOrHiddenMembers.HiddenMembers.Length);

            // B<whatever>.M<whatever> overrides nothing.
            AssertSame(0, BofT_MofU.OverriddenOrHiddenMembers.OverriddenMembers.Length);
            AssertSame(0, BofD_MofU.OverriddenOrHiddenMembers.OverriddenMembers.Length);
            AssertSame(0, BofD_MofD.OverriddenOrHiddenMembers.OverriddenMembers.Length);

            // D.M<V> overrides B<D>.M<U>, *not* B<D>.M<V> or B<T>.M<U>...
            AssertSame(1, D_MofV.OverriddenOrHiddenMembers.OverriddenMembers.Length);
            Assert.Equal(D_MofV.OverriddenMethod, BofD_MofU);
            // ... but the "original" overridden method of D.M<V> is B<D>.M<V>
            Assert.Equal(D_MofV.GetConstructedLeastOverriddenMethod(D, requireSameReturnType: false), BofD_MofV);

            // D.M<D> overrides nothing, since it is constructed...
            Assert.Null(D_MofD.OverriddenMethod);
            // ... and the overridden members count is zero if the overridden method is null...
            AssertSame(0, D_MofD.OverriddenOrHiddenMembers.OverriddenMembers.Length);
            // ... but the original overridden method is B<D>.M<D>
            Assert.Equal(D_MofD.GetConstructedLeastOverriddenMethod(D, requireSameReturnType: false), BofD_MofD);
        }

        [Fact]
        public void TestBug6156()
        {
            var text = @"
class Ref1
{
    public virtual void M(ref int x) { x = 1; } // SLOT1
}
class Out1 : Ref1
{
    public virtual void M(out int x) { x = 2; } // SLOT2
}
class Ref2 : Out1
{
    public override void M(ref int x) { x = 3; } 
    // CLR says this overrides SLOT2, even though there is a ref/out mismatch with Out1.
    // C# says this overrides SLOT1
}
class Out2 : Ref2
{
    public override void M(out int x) { x = 4; } 
    // CLR says this overrides SLOT2, even though there is a ref/out mismatch with Ref2.M.
    // C# says this overrides SLOT2
}
class Out3 : Out2
{
    public override void M(out int x) { x = 5; } 
}
";

            var tree = Parse(text);
            var comp = CreateCompilation(tree);

            var global = comp.GlobalNamespace;

            var ref1 = global.GetMember<NamedTypeSymbol>("Ref1");
            var out1 = global.GetMember<NamedTypeSymbol>("Out1");
            var ref2 = global.GetMember<NamedTypeSymbol>("Ref2");
            var out2 = global.GetMember<NamedTypeSymbol>("Out2");
            var out3 = global.GetMember<NamedTypeSymbol>("Out3");

            var ref1M = ref1.GetMember<MethodSymbol>("M");
            var out1M = out1.GetMember<MethodSymbol>("M");
            var ref2M = ref2.GetMember<MethodSymbol>("M");
            var out2M = out2.GetMember<MethodSymbol>("M");
            var out3M = out3.GetMember<MethodSymbol>("M");

            // Clearly nothing is hidden or overridden in Ref1.
            AssertSame(0, ref1M.OverriddenOrHiddenMembers.HiddenMembers.Length);
            AssertSame(0, ref1M.OverriddenOrHiddenMembers.OverriddenMembers.Length);

            // Out1.M does not override anything (because it is not declared with "override") and
            // it does not hide anything (because it does not match signatures with Ref1.M).
            AssertSame(0, out1M.OverriddenOrHiddenMembers.HiddenMembers.Length);
            AssertSame(0, out1M.OverriddenOrHiddenMembers.OverriddenMembers.Length);

            // C# thinks that Ref2.M overrides Ref1.M, though at runtime in fact the CLR
            // will set Ref2.M to the second slot, not the first slot.
            AssertSame(0, ref2M.OverriddenOrHiddenMembers.HiddenMembers.Length);
            AssertSame(1, ref2M.OverriddenOrHiddenMembers.OverriddenMembers.Length);
            Assert.Same(ref1M, ref2M.OverriddenMethod);
            Assert.Same(ref1M, ref2M.OverriddenOrHiddenMembers.OverriddenMembers.Single());

            // C# thinks that Out2.M overrides Out1.M. The runtime agrees and will set
            // Out2.M to the first slot.
            AssertSame(0, out2M.OverriddenOrHiddenMembers.HiddenMembers.Length);
            AssertSame(1, out2M.OverriddenOrHiddenMembers.OverriddenMembers.Length);
            Assert.Same(out1M, out2M.OverriddenMethod);
            Assert.Same(out1M, out2M.OverriddenOrHiddenMembers.OverriddenMembers.Single());

            // Note that the "overridden method" is the *immediately* overridden method,
            // not the original method that was declared.
            AssertSame(0, out3M.OverriddenOrHiddenMembers.HiddenMembers.Length);
            AssertSame(1, out3M.OverriddenOrHiddenMembers.OverriddenMembers.Length);
            Assert.Same(out2M, out3M.OverriddenMethod);
            Assert.Same(out2M, out3M.OverriddenOrHiddenMembers.OverriddenMembers.Single());
        }

        private static void AssertSame(int expected, int actual)
        {
            Assert.True(expected == actual, string.Format("expected {0}, actual {1}", expected, actual));
        }

        [Fact]
        public void TestInterfaceHiding()
        {
            var text = @"
interface BaseInterface1
{
    void Method();
    int Property { get; set; }
}
interface BaseInterface2
{
    void Method();
    int Property { get; set; }
}

interface DerivedInterface1 : BaseInterface1, BaseInterface2
{
    void Method();
    void Method(int x);
    int Property { get; set; }
}

interface DerivedInterface2 : BaseInterface2, BaseInterface1
{
    void Method();
    void Method(int x);
    int Property { get; set; }
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;

            var baseInterface1 = (NamedTypeSymbol)global.GetMembers("BaseInterface1").Single();
            var baseInterface2 = (NamedTypeSymbol)global.GetMembers("BaseInterface2").Single();
            var derivedInterface1 = (NamedTypeSymbol)global.GetMembers("DerivedInterface1").Single();
            var derivedInterface2 = (NamedTypeSymbol)global.GetMembers("DerivedInterface2").Single();

            var baseInterface1Method = (MethodSymbol)baseInterface1.GetMembers("Method").Single();
            var baseInterface1Property = (PropertySymbol)baseInterface1.GetMembers("Property").Single();

            var baseInterface2Method = (MethodSymbol)baseInterface2.GetMembers("Method").Single();
            var baseInterface2Property = (PropertySymbol)baseInterface2.GetMembers("Property").Single();

            var derivedInterface1Method = (MethodSymbol)derivedInterface1.GetMembers("Method").First();
            var derivedInterface1MethodInt = (MethodSymbol)derivedInterface1.GetMembers("Method").Last();
            var derivedInterface1Property = (PropertySymbol)derivedInterface1.GetMembers("Property").Single();

            var derivedInterface2Method = (MethodSymbol)derivedInterface2.GetMembers("Method").First();
            var derivedInterface2MethodInt = (MethodSymbol)derivedInterface2.GetMembers("Method").Last();
            var derivedInterface2Property = (PropertySymbol)derivedInterface2.GetMembers("Property").Single();

            Assert.Same(OverriddenOrHiddenMembersResult.Empty, baseInterface1Method.OverriddenOrHiddenMembers);
            Assert.Same(OverriddenOrHiddenMembersResult.Empty, baseInterface1Property.OverriddenOrHiddenMembers);

            Assert.Same(OverriddenOrHiddenMembersResult.Empty, baseInterface2Method.OverriddenOrHiddenMembers);
            Assert.Same(OverriddenOrHiddenMembersResult.Empty, baseInterface2Property.OverriddenOrHiddenMembers);

            Assert.Same(OverriddenOrHiddenMembersResult.Empty, derivedInterface1MethodInt.OverriddenOrHiddenMembers);

            Assert.Same(OverriddenOrHiddenMembersResult.Empty, derivedInterface2MethodInt.OverriddenOrHiddenMembers);

            var derivedInterface1MethodOverriddenOrHidden = derivedInterface1Method.OverriddenOrHiddenMembers;
            Assert.False(derivedInterface1MethodOverriddenOrHidden.OverriddenMembers.Any());
            AssertEx.SetEqual(derivedInterface1MethodOverriddenOrHidden.HiddenMembers, baseInterface1Method, baseInterface2Method);

            var derivedInterface1PropertyOverriddenOrHidden = derivedInterface1Property.OverriddenOrHiddenMembers;
            Assert.False(derivedInterface1PropertyOverriddenOrHidden.OverriddenMembers.Any());
            AssertEx.SetEqual(derivedInterface1PropertyOverriddenOrHidden.HiddenMembers, baseInterface1Property, baseInterface2Property);

            var derivedInterface2MethodOverriddenOrHidden = derivedInterface2Method.OverriddenOrHiddenMembers;
            Assert.False(derivedInterface2MethodOverriddenOrHidden.OverriddenMembers.Any());
            AssertEx.SetEqual(derivedInterface2MethodOverriddenOrHidden.HiddenMembers, baseInterface1Method, baseInterface2Method);

            var derivedInterface2PropertyOverriddenOrHidden = derivedInterface2Property.OverriddenOrHiddenMembers;
            Assert.False(derivedInterface2PropertyOverriddenOrHidden.OverriddenMembers.Any());
            AssertEx.SetEqual(derivedInterface2PropertyOverriddenOrHidden.HiddenMembers, baseInterface1Property, baseInterface2Property);

            Assert.Null(baseInterface1Method.OverriddenMethod);
            Assert.Null(baseInterface1Property.OverriddenProperty);
            Assert.Null(baseInterface2Method.OverriddenMethod);
            Assert.Null(baseInterface2Property.OverriddenProperty);
            Assert.Null(derivedInterface1Method.OverriddenMethod);
            Assert.Null(derivedInterface1MethodInt.OverriddenMethod);
            Assert.Null(derivedInterface1Property.OverriddenProperty);
            Assert.Null(derivedInterface2Method.OverriddenMethod);
            Assert.Null(derivedInterface2MethodInt.OverriddenMethod);
            Assert.Null(derivedInterface2Property.OverriddenProperty);
        }

        [Fact]
        public void TestGenericInterfaceHiding()
        {
            var text = @"
interface BaseInterface1<T>
{
    void Method(T t);
    void Method(int i);
    int Property { get; set; }
}
interface BaseInterface2<T>
{
    void Method(T t);
    void Method(int i);
    int Property { get; set; }
}

interface DerivedInterface1 : BaseInterface1<int>, BaseInterface2<int>
{
    void Method();
    void Method(int x);
    int Property { get; set; }
}

interface DerivedInterface2 : BaseInterface2<int>, BaseInterface1<int>
{
    void Method();
    void Method(int x);
    int Property { get; set; }
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;

            var baseInterface1 = (NamedTypeSymbol)global.GetMembers("BaseInterface1").Single();
            var baseInterface2 = (NamedTypeSymbol)global.GetMembers("BaseInterface2").Single();
            var derivedInterface1 = (NamedTypeSymbol)global.GetMembers("DerivedInterface1").Single();
            var derivedInterface2 = (NamedTypeSymbol)global.GetMembers("DerivedInterface2").Single();

            var baseInterface1MethodT = (MethodSymbol)baseInterface1.GetMembers("Method").First();
            var baseInterface1MethodInt = (MethodSymbol)baseInterface1.GetMembers("Method").Last();
            var baseInterface1Property = (PropertySymbol)baseInterface1.GetMembers("Property").Single();

            var baseInterface2MethodT = (MethodSymbol)baseInterface2.GetMembers("Method").First();
            var baseInterface2MethodInt = (MethodSymbol)baseInterface2.GetMembers("Method").Last();
            var baseInterface2Property = (PropertySymbol)baseInterface2.GetMembers("Property").Single();

            var derivedInterface1Method = (MethodSymbol)derivedInterface1.GetMembers("Method").First();
            var derivedInterface1MethodInt = (MethodSymbol)derivedInterface1.GetMembers("Method").Last();
            var derivedInterface1Property = (PropertySymbol)derivedInterface1.GetMembers("Property").Single();

            var derivedInterface2Method = (MethodSymbol)derivedInterface2.GetMembers("Method").First();
            var derivedInterface2MethodInt = (MethodSymbol)derivedInterface2.GetMembers("Method").Last();
            var derivedInterface2Property = (PropertySymbol)derivedInterface2.GetMembers("Property").Single();

            Assert.Same(OverriddenOrHiddenMembersResult.Empty, baseInterface1MethodT.OverriddenOrHiddenMembers);
            Assert.Same(OverriddenOrHiddenMembersResult.Empty, baseInterface1MethodInt.OverriddenOrHiddenMembers);
            Assert.Same(OverriddenOrHiddenMembersResult.Empty, baseInterface1Property.OverriddenOrHiddenMembers);

            Assert.Same(OverriddenOrHiddenMembersResult.Empty, baseInterface2MethodT.OverriddenOrHiddenMembers);
            Assert.Same(OverriddenOrHiddenMembersResult.Empty, baseInterface2MethodInt.OverriddenOrHiddenMembers);
            Assert.Same(OverriddenOrHiddenMembersResult.Empty, baseInterface2Property.OverriddenOrHiddenMembers);

            Assert.Same(OverriddenOrHiddenMembersResult.Empty, derivedInterface1Method.OverriddenOrHiddenMembers);

            Assert.Same(OverriddenOrHiddenMembersResult.Empty, derivedInterface2Method.OverriddenOrHiddenMembers);

            var derivedInterface1MethodIntOverriddenOrHidden = derivedInterface1MethodInt.OverriddenOrHiddenMembers;
            Assert.False(derivedInterface1MethodIntOverriddenOrHidden.OverriddenMembers.Any());
            Assert.Equal(4, derivedInterface1MethodIntOverriddenOrHidden.HiddenMembers.Length);

            var derivedInterface1PropertyOverriddenOrHidden = derivedInterface1Property.OverriddenOrHiddenMembers;
            Assert.False(derivedInterface1PropertyOverriddenOrHidden.OverriddenMembers.Any());
            Assert.Equal(2, derivedInterface1PropertyOverriddenOrHidden.HiddenMembers.Length);

            var derivedInterface2MethodIntOverriddenOrHidden = derivedInterface2MethodInt.OverriddenOrHiddenMembers;
            Assert.False(derivedInterface2MethodIntOverriddenOrHidden.OverriddenMembers.Any());
            Assert.Equal(4, derivedInterface2MethodIntOverriddenOrHidden.HiddenMembers.Length);

            var derivedInterface2PropertyOverriddenOrHidden = derivedInterface2Property.OverriddenOrHiddenMembers;
            Assert.False(derivedInterface2PropertyOverriddenOrHidden.OverriddenMembers.Any());
            Assert.Equal(2, derivedInterface2PropertyOverriddenOrHidden.HiddenMembers.Length);

            Assert.Null(baseInterface1MethodT.OverriddenMethod);
            Assert.Null(baseInterface1Property.OverriddenProperty);
            Assert.Null(baseInterface2MethodT.OverriddenMethod);
            Assert.Null(baseInterface2Property.OverriddenProperty);
            Assert.Null(derivedInterface1Method.OverriddenMethod);
            Assert.Null(derivedInterface1MethodInt.OverriddenMethod);
            Assert.Null(derivedInterface1Property.OverriddenProperty);
            Assert.Null(derivedInterface2Method.OverriddenMethod);
            Assert.Null(derivedInterface2MethodInt.OverriddenMethod);
            Assert.Null(derivedInterface2Property.OverriddenProperty);
        }

        [Fact]
        public void TestClassHiding()
        {
            var text = @"
class BaseClass
{
    public void Method();
    public int Property { get; set; }
}

class DerivedClass : BaseClass
{
    public void Method();
    public void Method(int x);
    public int Property { get; set; }
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;

            var baseClass = (NamedTypeSymbol)global.GetMembers("BaseClass").Single();
            var derivedClass = (NamedTypeSymbol)global.GetMembers("DerivedClass").Single();

            var baseClassMethod = (MethodSymbol)baseClass.GetMembers("Method").Single();
            var baseClassProperty = (PropertySymbol)baseClass.GetMembers("Property").Single();

            var derivedClassMethod = (MethodSymbol)derivedClass.GetMembers("Method").First();
            var derivedClassMethodInt = (MethodSymbol)derivedClass.GetMembers("Method").Last();
            var derivedClassProperty = (PropertySymbol)derivedClass.GetMembers("Property").Single();

            Assert.Same(OverriddenOrHiddenMembersResult.Empty, baseClassMethod.OverriddenOrHiddenMembers);
            Assert.Same(OverriddenOrHiddenMembersResult.Empty, baseClassProperty.OverriddenOrHiddenMembers);

            Assert.Same(OverriddenOrHiddenMembersResult.Empty, derivedClassMethodInt.OverriddenOrHiddenMembers);

            var derivedClassMethodOverriddenOrHidden = derivedClassMethod.OverriddenOrHiddenMembers;
            Assert.False(derivedClassMethodOverriddenOrHidden.OverriddenMembers.Any());
            Assert.Same(baseClassMethod, derivedClassMethodOverriddenOrHidden.HiddenMembers.Single());

            var derivedClassPropertyOverriddenOrHidden = derivedClassProperty.OverriddenOrHiddenMembers;
            Assert.False(derivedClassPropertyOverriddenOrHidden.OverriddenMembers.Any());
            Assert.Same(baseClassProperty, derivedClassPropertyOverriddenOrHidden.HiddenMembers.Single());

            Assert.Null(baseClassMethod.OverriddenMethod);
            Assert.Null(baseClassProperty.OverriddenProperty);
            Assert.Null(derivedClassMethod.OverriddenMethod);
            Assert.Null(derivedClassMethodInt.OverriddenMethod);
            Assert.Null(derivedClassProperty.OverriddenProperty);
        }

        [Fact]
        public void TestGenericClassHiding()
        {
            var text = @"
class BaseClass<T>
{
    public void Method(T t) { }
    public void Method(int i) { }
    public int Property { get; set; }
}

class DerivedClass : BaseClass<int>
{
    public void Method() { }
    public void Method(int x) { }
    public int Property { get; set; }
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;

            var baseClass = (NamedTypeSymbol)global.GetMembers("BaseClass").Single();
            var derivedClass = (NamedTypeSymbol)global.GetMembers("DerivedClass").Single();

            var baseClassMethodT = (MethodSymbol)baseClass.GetMembers("Method").First();
            var baseClassMethodInt = (MethodSymbol)baseClass.GetMembers("Method").Last();
            var baseClassProperty = (PropertySymbol)baseClass.GetMembers("Property").Single();

            var derivedClassMethod = (MethodSymbol)derivedClass.GetMembers("Method").First();
            var derivedClassMethodInt = (MethodSymbol)derivedClass.GetMembers("Method").Last();
            var derivedClassProperty = (PropertySymbol)derivedClass.GetMembers("Property").Single();

            Assert.Same(OverriddenOrHiddenMembersResult.Empty, baseClassMethodT.OverriddenOrHiddenMembers);
            Assert.Same(OverriddenOrHiddenMembersResult.Empty, baseClassMethodInt.OverriddenOrHiddenMembers);
            Assert.Same(OverriddenOrHiddenMembersResult.Empty, baseClassProperty.OverriddenOrHiddenMembers);

            Assert.Same(OverriddenOrHiddenMembersResult.Empty, derivedClassMethod.OverriddenOrHiddenMembers);

            var derivedClassMethodIntOverriddenOrHidden = derivedClassMethodInt.OverriddenOrHiddenMembers;
            Assert.False(derivedClassMethodIntOverriddenOrHidden.OverriddenMembers.Any());
            Assert.Equal(2, derivedClassMethodIntOverriddenOrHidden.HiddenMembers.Length);

            var derivedClassPropertyOverriddenOrHidden = derivedClassProperty.OverriddenOrHiddenMembers;
            Assert.False(derivedClassPropertyOverriddenOrHidden.OverriddenMembers.Any());
            Assert.Equal(1, derivedClassPropertyOverriddenOrHidden.HiddenMembers.Length);

            Assert.Null(baseClassMethodT.OverriddenMethod);
            Assert.Null(baseClassMethodInt.OverriddenMethod);
            Assert.Null(baseClassProperty.OverriddenProperty);
            Assert.Null(derivedClassMethod.OverriddenMethod);
            Assert.Null(derivedClassMethodInt.OverriddenMethod);
            Assert.Null(derivedClassProperty.OverriddenProperty);
        }

        [Fact]
        public void TestClassOverriding()
        {
            var text = @"
class BaseClass
{
    public int field;
    public virtual void Method() { }
    public virtual int Property { get; set; }
    public virtual ref int Method1() { return ref field; }
    public virtual ref int Property1 { get { return ref field; } }
    public virtual ref int this[int i] { get { return ref field; } }
}

class DerivedClass : BaseClass
{
    public override void Method() { }
    public override void Method(int x) { } //this is incorrect, but doesn't break the test
    public override int Property { get; set; }
    public override ref int Method1() { return ref field; }
    public override ref int Property1 { get { return ref field; } }
    public override ref int this[int i] { get { return ref field; } }
}
";
            var comp = CreateCompilationWithMscorlib45(text);
            var global = comp.GlobalNamespace;

            var baseClass = (NamedTypeSymbol)global.GetMembers("BaseClass").Single();
            var derivedClass = (NamedTypeSymbol)global.GetMembers("DerivedClass").Single();

            var baseClassMethod = (MethodSymbol)baseClass.GetMembers("Method").Single();
            var baseClassProperty = (PropertySymbol)baseClass.GetMembers("Property").Single();
            var baseClassRefMethod = (MethodSymbol)baseClass.GetMembers("Method1").Single();
            var baseClassRefProperty = (PropertySymbol)baseClass.GetMembers("Property1").Single();
            var baseClassRefIndexer = (PropertySymbol)baseClass.GetMembers("this[]").Single();

            var derivedClassMethod = (MethodSymbol)derivedClass.GetMembers("Method").First();
            var derivedClassMethodInt = (MethodSymbol)derivedClass.GetMembers("Method").Last();
            var derivedClassProperty = (PropertySymbol)derivedClass.GetMembers("Property").Single();
            var derivedClassRefMethod = (MethodSymbol)derivedClass.GetMembers("Method1").Single();
            var derivedClassRefProperty = (PropertySymbol)derivedClass.GetMembers("Property1").Single();
            var derivedClassRefIndexer = (PropertySymbol)derivedClass.GetMembers("this[]").Single();

            Assert.Same(OverriddenOrHiddenMembersResult.Empty, baseClassMethod.OverriddenOrHiddenMembers);
            Assert.Same(OverriddenOrHiddenMembersResult.Empty, baseClassProperty.OverriddenOrHiddenMembers);

            Assert.Same(OverriddenOrHiddenMembersResult.Empty, derivedClassMethodInt.OverriddenOrHiddenMembers);

            var derivedClassMethodOverriddenOrHidden = derivedClassMethod.OverriddenOrHiddenMembers;
            Assert.False(derivedClassMethodOverriddenOrHidden.HiddenMembers.Any());
            Assert.Same(baseClassMethod, derivedClassMethodOverriddenOrHidden.OverriddenMembers.Single());

            var derivedClassPropertyOverriddenOrHidden = derivedClassProperty.OverriddenOrHiddenMembers;
            Assert.False(derivedClassPropertyOverriddenOrHidden.HiddenMembers.Any());
            Assert.Same(baseClassProperty, derivedClassPropertyOverriddenOrHidden.OverriddenMembers.Single());

            var derivedClassRefMethodOverriddenOrHidden = derivedClassRefMethod.OverriddenOrHiddenMembers;
            Assert.False(derivedClassRefMethodOverriddenOrHidden.HiddenMembers.Any());
            Assert.Same(baseClassRefMethod, derivedClassRefMethodOverriddenOrHidden.OverriddenMembers.Single());

            var derivedClassRefPropertyOverriddenOrHidden = derivedClassRefProperty.OverriddenOrHiddenMembers;
            Assert.False(derivedClassRefPropertyOverriddenOrHidden.HiddenMembers.Any());
            Assert.Same(baseClassRefProperty, derivedClassRefPropertyOverriddenOrHidden.OverriddenMembers.Single());

            var derivedClassRefIndexerOverriddenOrHidden = derivedClassRefIndexer.OverriddenOrHiddenMembers;
            Assert.False(derivedClassRefIndexerOverriddenOrHidden.HiddenMembers.Any());
            Assert.Same(baseClassRefIndexer, derivedClassRefIndexerOverriddenOrHidden.OverriddenMembers.Single());

            Assert.Null(baseClassMethod.OverriddenMethod);
            Assert.Null(baseClassProperty.OverriddenProperty);
            Assert.Null(baseClassRefMethod.OverriddenMethod);
            Assert.Null(baseClassRefProperty.OverriddenProperty);
            Assert.Null(baseClassRefIndexer.OverriddenProperty);
            Assert.Null(derivedClassMethodInt.OverriddenMethod);

            Assert.Same(baseClassMethod, derivedClassMethod.OverriddenMethod);
            Assert.Same(baseClassProperty, derivedClassProperty.OverriddenProperty);
            Assert.Same(baseClassRefMethod, derivedClassRefMethod.OverriddenMethod);
            Assert.Same(baseClassRefProperty, derivedClassRefProperty.OverriddenProperty);
            Assert.Same(baseClassRefIndexer, derivedClassRefIndexer.OverriddenProperty);
        }

        [Fact]
        public void TestOverridingMembersOnObject()
        {
            // Tests:
            // Override virtual methods declared on object (ToString, GetHashCode etc.)

            var text = @"
class BaseClass<TInt, TLong>
{
    public override string ToString() { return ""; }
    public override int GetHashCode() { return 0; }
}

abstract class DerivedClass : BaseClass<int, long>
{
    public override int GetHashCode() { return 1; }
    public override bool Equals(object obj) { return true; }
}";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var system = comp.GlobalNamespace.GetNestedNamespace("System");

            var systemObject = (NamedTypeSymbol)system.GetMembers("Object").Single();
            var baseClass = (NamedTypeSymbol)global.GetMembers("BaseClass").Single();
            var derivedClass = (NamedTypeSymbol)global.GetMembers("DerivedClass").Single();

            var objectToString = (MethodSymbol)systemObject.GetMembers("ToString").Single();
            var objectGetHashCode = (MethodSymbol)systemObject.GetMembers("GetHashCode").Single();
            var objectEquals = (MethodSymbol)systemObject.GetMembers("Equals")[0];

            var baseClassToString = (MethodSymbol)baseClass.GetMembers("ToString").Single();
            var baseClassGetHashCode = (MethodSymbol)baseClass.GetMembers("GetHashCode").Single();
            Assert.Null(baseClass.GetMembers("Equals").SingleOrDefault());

            Assert.Null(derivedClass.GetMembers("ToString").SingleOrDefault());
            var derivedClassGetHashCode = (MethodSymbol)derivedClass.GetMembers("GetHashCode").Single();
            var derivedClassEquals = (MethodSymbol)derivedClass.GetMembers("Equals").Single();

            var baseClassToStringOverriddenOrHidden = baseClassToString.OverriddenOrHiddenMembers;
            Assert.False(baseClassToStringOverriddenOrHidden.HiddenMembers.Any());
            Assert.Equal(1, baseClassToStringOverriddenOrHidden.OverriddenMembers.Length);

            var baseClassGetHashCodeOverriddenOrHidden = baseClassGetHashCode.OverriddenOrHiddenMembers;
            Assert.False(baseClassGetHashCodeOverriddenOrHidden.HiddenMembers.Any());
            Assert.Equal(1, baseClassGetHashCodeOverriddenOrHidden.OverriddenMembers.Length);

            var derivedClassEqualsOverriddenOrHidden = derivedClassEquals.OverriddenOrHiddenMembers;
            Assert.False(derivedClassEqualsOverriddenOrHidden.HiddenMembers.Any());
            Assert.Equal(1, derivedClassEqualsOverriddenOrHidden.OverriddenMembers.Length);

            var derivedClassGetHashCodeOverriddenOrHidden = derivedClassGetHashCode.OverriddenOrHiddenMembers;
            Assert.False(derivedClassGetHashCodeOverriddenOrHidden.HiddenMembers.Any());
            Assert.Equal(1, derivedClassGetHashCodeOverriddenOrHidden.OverriddenMembers.Length);

            Assert.Same(objectToString, baseClassToString.OverriddenMethod);
            Assert.Same(objectGetHashCode, baseClassGetHashCode.OverriddenMethod);

            Assert.Same(baseClassGetHashCode, derivedClassGetHashCode.OverriddenMethod.OriginalDefinition);
            Assert.Same(objectEquals, derivedClassEquals.OverriddenMethod);
        }

        [Fact]
        public void TestGenericClassOverriding()
        {
            var text = @"
class BaseClass<TInt, TLong>
{
    public virtual void Method(ref TInt i, out long l) { }
    public virtual void Method(ref int i, ref TLong l) { }
    public virtual void Method(ref int i, ref long l) { }
    public virtual int Property { get; set; }
}

class DerivedClass : BaseClass<int, long>
{
    public override void Method() { } //this is incorrect, but doesn't break the test
    public override void Method(ref int i, ref long l) { }
    public override int Property { get; set; }
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;

            var baseClass = (NamedTypeSymbol)global.GetMembers("BaseClass").Single();
            var derivedClass = (NamedTypeSymbol)global.GetMembers("DerivedClass").Single();

            var baseClassMethod1 = (MethodSymbol)baseClass.GetMembers("Method")[0];
            var baseClassMethod2 = (MethodSymbol)baseClass.GetMembers("Method")[1];
            var baseClassMethod3 = (MethodSymbol)baseClass.GetMembers("Method")[2];
            var baseClassProperty = (PropertySymbol)baseClass.GetMembers("Property").Single();

            var derivedClassMethod = (MethodSymbol)derivedClass.GetMembers("Method")[0];
            var derivedClassMethodParams = (MethodSymbol)derivedClass.GetMembers("Method")[1];
            var derivedClassProperty = (PropertySymbol)derivedClass.GetMembers("Property").Single();

            Assert.Same(OverriddenOrHiddenMembersResult.Empty, baseClassMethod1.OverriddenOrHiddenMembers);
            Assert.Same(OverriddenOrHiddenMembersResult.Empty, baseClassMethod2.OverriddenOrHiddenMembers);
            Assert.Same(OverriddenOrHiddenMembersResult.Empty, baseClassMethod3.OverriddenOrHiddenMembers);
            Assert.Same(OverriddenOrHiddenMembersResult.Empty, baseClassProperty.OverriddenOrHiddenMembers);

            Assert.Same(OverriddenOrHiddenMembersResult.Empty, derivedClassMethod.OverriddenOrHiddenMembers);

            var derivedClassMethodIntOverriddenOrHidden = derivedClassMethodParams.OverriddenOrHiddenMembers;
            Assert.False(derivedClassMethodIntOverriddenOrHidden.HiddenMembers.Any());
            Assert.Equal(2, derivedClassMethodIntOverriddenOrHidden.OverriddenMembers.Length);

            var derivedClassPropertyOverriddenOrHidden = derivedClassProperty.OverriddenOrHiddenMembers;
            Assert.False(derivedClassPropertyOverriddenOrHidden.HiddenMembers.Any());
            Assert.Equal(1, derivedClassPropertyOverriddenOrHidden.OverriddenMembers.Length);

            Assert.Null(baseClassMethod1.OverriddenMethod);
            Assert.Null(baseClassMethod2.OverriddenMethod);
            Assert.Null(baseClassMethod3.OverriddenMethod);
            Assert.Null(baseClassProperty.OverriddenProperty);
            Assert.Null(derivedClassMethod.OverriddenMethod);

            Assert.NotNull(derivedClassMethodParams.OverriddenMethod);
            Assert.NotNull(derivedClassProperty.OverriddenProperty);
        }

        [Fact]
        public void TestClassHiddenOverrides()
        {
            var text1 = @"
public class DefiningClass
{
    public virtual void Method1() { }
    public virtual void Method2() { }
    public virtual int Property1 { get; set; }
    public virtual int Property2 { get; set; }
}
";
            var text2 = @"
public class HidingClass : DefiningClass
{
    public new int Method1;
    public new void Method2() { }
    public new int Property1;
    public new int Property2 { get; set; }
}
";
            var text3 = @"
class OverridingClass : HidingClass
{
    public override void Method1() { } //blocked by non-method
    public override void Method2() { } //blocked by non-virtual method
    public override int Property1 { get; set; } //blocked by non-property
    public override int Property2 { get; set; } //blocked by non-virtual property
}
";

            var comp1 = CreateCompilation(text1);
            var comp1ref = new CSharpCompilationReference(comp1);
            var refs = new System.Collections.Generic.List<MetadataReference>() { comp1ref };

            var comp2 = CreateCompilation(text2, references: refs, assemblyName: "Test2");
            var comp2ref = new CSharpCompilationReference(comp2);

            refs.Add(comp2ref);
            var comp = CreateCompilation(text3, refs, assemblyName: "Test3");

            var global = comp.GlobalNamespace;

            var definingClass = (NamedTypeSymbol)global.GetMembers("DefiningClass").Single();
            var hidingClass = (NamedTypeSymbol)global.GetMembers("HidingClass").Single();
            var overridingClass = (NamedTypeSymbol)global.GetMembers("OverridingClass").Single();

            var definingClassMethod1 = (MethodSymbol)definingClass.GetMembers("Method1").Single();
            var definingClassMethod2 = (MethodSymbol)definingClass.GetMembers("Method2").Single();
            var definingClassProperty1 = (PropertySymbol)definingClass.GetMembers("Property1").Single();
            var definingClassProperty2 = (PropertySymbol)definingClass.GetMembers("Property2").Single();

            var hidingClassMethod1 = (FieldSymbol)hidingClass.GetMembers("Method1").Single();
            var hidingClassMethod2 = (MethodSymbol)hidingClass.GetMembers("Method2").Single();
            var hidingClassProperty1 = (FieldSymbol)hidingClass.GetMembers("Property1").Single();
            var hidingClassProperty2 = (PropertySymbol)hidingClass.GetMembers("Property2").Single();

            var overridingClassMethod1 = (MethodSymbol)overridingClass.GetMembers("Method1").Single();
            var overridingClassMethod2 = (MethodSymbol)overridingClass.GetMembers("Method2").Single();
            var overridingClassProperty1 = (PropertySymbol)overridingClass.GetMembers("Property1").Single();
            var overridingClassProperty2 = (PropertySymbol)overridingClass.GetMembers("Property2").Single();

            var overridingClassMethod1OverriddenOrHidden = overridingClassMethod1.OverriddenOrHiddenMembers;
            Assert.False(overridingClassMethod1OverriddenOrHidden.OverriddenMembers.Any());
            Assert.Same(hidingClassMethod1, overridingClassMethod1OverriddenOrHidden.HiddenMembers.Single());
            Assert.Null(overridingClassMethod1.OverriddenMethod);

            //counts as overriding even though the overridden method isn't virtual - we'll check for that later
            var overridingClassMethod2OverriddenOrHidden = overridingClassMethod2.OverriddenOrHiddenMembers;
            Assert.False(overridingClassMethod2OverriddenOrHidden.HiddenMembers.Any());
            Assert.Same(hidingClassMethod2, overridingClassMethod2OverriddenOrHidden.OverriddenMembers.Single());
            Assert.Null(overridingClassMethod2.OverriddenMethod);

            var overridingClassProperty1OverriddenOrHidden = overridingClassProperty1.OverriddenOrHiddenMembers;
            Assert.False(overridingClassProperty1OverriddenOrHidden.OverriddenMembers.Any());
            Assert.Null(overridingClassProperty1.OverriddenProperty);

            //counts as overriding even though the overridden property isn't virtual - we'll check for that later
            var overridingClassProperty2OverriddenOrHidden = overridingClassProperty2.OverriddenOrHiddenMembers;
            Assert.False(overridingClassProperty2OverriddenOrHidden.HiddenMembers.Any());
            Assert.Same(hidingClassProperty2, overridingClassProperty2OverriddenOrHidden.OverriddenMembers.Single());
            Assert.Null(overridingClassProperty2.OverriddenProperty);
        }

        [Fact]
        public void TestNonImplementedAbstractMembers()
        {
            // Tests: 
            // Test error when not all abstract members have been overridden in derived type

            var text = @"
abstract class Base<T, U>
{
    public abstract T Property { get; set; }
    public virtual void Method(T x, U y) { }
    public abstract ref T RefProperty { get; }
}

class Base2<A, B> : Base<A, B>
{
    A field = default(A);
    public override A Property { set { } }
    public override ref A RefProperty { get { return ref @field; } }
}

abstract class Base3<T, U> : Base2<T, U>
{
    public override abstract T Property { set; }
    public abstract override void Method(T x, U y);
}

class Base4<U, V> : Base3<U, V>
{
    public override U Property { set { } }
}";
            CreateCompilationWithMscorlib45(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Base2").WithArguments("Base2<A, B>", "Base<A, B>.Property.get"),
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Base4").WithArguments("Base4<U, V>", "Base3<U, V>.Method(U, V)"));
        }

        [Fact]
        public void TestNonImplementedAbstractMembers2()
        {
            // Tests: 
            // Test error when not all abstract members from grandparent types have been overridden in derived type
            // Use base class where abstract members are declared in different partial parts of the same type

            var text = @"
abstract partial class Base
{
    public abstract int Property { get; set; }
}

abstract partial class Base
{
    public abstract void Method(int x, long y);
}

abstract partial class Base2
{
    public abstract void Method(int x);
}

abstract partial class Base2 : Base
{
    public override int Property { set { } }
}

abstract class Base3 : Base2
{
    public override abstract int Property { set; }
    public abstract override void Method(int x, long y);
}

class Base4 : Base3
{
    public override int Property { set { } }
}";
            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Base4").WithArguments("Base4", "Base3.Method(int, long)"),
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Base4").WithArguments("Base4", "Base2.Method(int)"),
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Base4").WithArguments("Base4", "Base.Property.get"));
        }

        /// <summary>
        /// Layout:
        /// E : D : C : B : A
        /// All have virtual Method1 and Method2 with the same signatures (modulo custom modifiers)
        /// E, from source, has no custom modifiers
        /// D has 2 custom modifiers
        /// C has 1 custom modifier
        /// B has 2 custom modifiers, but not the same as D
        /// A has 1 custom modifier for Method1, but not the same as C, and 0 custom modifiers for Method2
        /// </summary>
        /// <remarks>
        /// ACASEY: When I initially wrote this test, I had the order of the tie-breakers wrong because I missed
        /// an exit condition in CSemanticChecker::FindSymHiddenByMethPropAgg.  Preferring more-derived types is
        /// a more important tie-breaker than preferring fewer custom modifiers.  Unfortunately, the correct rules
        /// make this test less comprehensive - no tie breaking occurs in practice, since we stop searching after
        /// finding candidates in D.  I updated the test, rather than deleting it, as a record of the correct
        /// behavior in this apparently complicated scenario.
        /// </remarks>
        [Fact]
        public void TestCustomModifierOverride()
        {
            var text = @"
class CustomModifierOverridingE : CustomModifierOverridingD
{
    public override void Method1(int[] x) { }
    public override void Method2(int[] x) { }
}
";
            var ilAssemblyReference = TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll;

            var comp = CreateCompilation(text, new MetadataReference[] { ilAssemblyReference });
            var global = comp.GlobalNamespace;

            Assert.False(comp.GetDiagnostics().Any());

            //IL
            var classA = (NamedTypeSymbol)global.GetTypeMembers("CustomModifierOverridingA").Single();
            var classB = (NamedTypeSymbol)global.GetTypeMembers("CustomModifierOverridingB").Single();
            var classC = (NamedTypeSymbol)global.GetTypeMembers("CustomModifierOverridingC").Single();
            var classD = (NamedTypeSymbol)global.GetTypeMembers("CustomModifierOverridingD").Single();

            var classAMethod1 = (MethodSymbol)classA.GetMembers("Method1").Single();
            var classAMethod2 = (MethodSymbol)classA.GetMembers("Method2").Single();
            var classBMethod1 = (MethodSymbol)classB.GetMembers("Method1").Single();
            var classBMethod2 = (MethodSymbol)classB.GetMembers("Method2").Single();
            var classCMethod1 = (MethodSymbol)classC.GetMembers("Method1").Single();
            var classCMethod2 = (MethodSymbol)classC.GetMembers("Method2").Single();
            var classDMethod1 = (MethodSymbol)classD.GetMembers("Method1").Single();
            var classDMethod2 = (MethodSymbol)classD.GetMembers("Method2").Single();

            Assert.Same(OverriddenOrHiddenMembersResult.Empty, classAMethod1.OverriddenOrHiddenMembers);
            Assert.Same(OverriddenOrHiddenMembersResult.Empty, classAMethod2.OverriddenOrHiddenMembers);
            Assert.Same(OverriddenOrHiddenMembersResult.Empty, classBMethod1.OverriddenOrHiddenMembers);
            Assert.Same(OverriddenOrHiddenMembersResult.Empty, classBMethod2.OverriddenOrHiddenMembers);
            Assert.Same(OverriddenOrHiddenMembersResult.Empty, classCMethod1.OverriddenOrHiddenMembers);
            Assert.Same(OverriddenOrHiddenMembersResult.Empty, classCMethod2.OverriddenOrHiddenMembers);
            Assert.Same(OverriddenOrHiddenMembersResult.Empty, classDMethod1.OverriddenOrHiddenMembers);
            Assert.Same(OverriddenOrHiddenMembersResult.Empty, classDMethod2.OverriddenOrHiddenMembers);

            Assert.Null(classAMethod1.OverriddenMethod);
            Assert.Null(classAMethod2.OverriddenMethod);
            Assert.Null(classBMethod1.OverriddenMethod);
            Assert.Null(classBMethod2.OverriddenMethod);
            Assert.Null(classCMethod1.OverriddenMethod);
            Assert.Null(classCMethod2.OverriddenMethod);
            Assert.Null(classDMethod1.OverriddenMethod);
            Assert.Null(classDMethod2.OverriddenMethod);

            //Source
            var classE = (NamedTypeSymbol)global.GetTypeMembers("CustomModifierOverridingE").Single();

            var classEMethod1 = (MethodSymbol)classE.GetMembers("Method1").Single();
            var classEMethod2 = (MethodSymbol)classE.GetMembers("Method2").Single();

            //no match, so apply tie breakers:
            // 1) prefer more derived (leaves classDMethod1)
            // [2) prefer fewer custom modifiers]
            // [3) prefer first in GetMembers order]
            var classEMethod1OverriddenOrHiddenMembers = classEMethod1.OverriddenOrHiddenMembers;
            Assert.False(classEMethod1OverriddenOrHiddenMembers.HiddenMembers.Any());
            Assert.Same(classDMethod1, classEMethod1OverriddenOrHiddenMembers.OverriddenMembers.Single());

            //no match, so apply tie breakers:
            // 1) prefer more derived (leaves classDMethod2)
            // [2) prefer fewer custom modifiers]
            // [3) prefer first in GetMembers order]
            var classEMethod2OverriddenOrHiddenMembers = classEMethod2.OverriddenOrHiddenMembers;
            Assert.False(classEMethod2OverriddenOrHiddenMembers.HiddenMembers.Any());
            Assert.Same(classDMethod2, classEMethod2OverriddenOrHiddenMembers.OverriddenMembers.Single());
        }

        /// <summary>
        /// Choose candidate with fewer custom modifiers.
        /// </summary>
        [Fact]
        public void TestCustomModifierTieBreak1()
        {
            var il = @"
.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance void  Method(int32[] modopt([mscorlib]System.Int32) x) cil managed
  {
    ret
  }
  .method public hidebysig newslot virtual 
          instance void  Method(int32[] modopt([mscorlib]System.Int32) modopt([mscorlib]System.Int32) x) cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class Base
";

            var csharp = @"
class Derived : Base 
{
    public override void Method(int[] x) { }
}
";

            var comp = CreateCompilationWithILAndMscorlib40(csharp, il);
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;
            var baseType = global.GetMember<NamedTypeSymbol>("Base");
            var derivedType = global.GetMember<NamedTypeSymbol>("Derived");

            var baseMethod1 = baseType.GetMembers("Method").OfType<MethodSymbol>().Single(m => m.CustomModifierCount() == 1);
            var baseMethod2 = baseType.GetMembers("Method").OfType<MethodSymbol>().Single(m => m.CustomModifierCount() == 1);

            var derivedMethod = derivedType.GetMember<MethodSymbol>("Method");

            var overriddenOrHidden = derivedMethod.OverriddenOrHiddenMembers;
            Assert.Equal(baseMethod1, overriddenOrHidden.OverriddenMembers.Single());
            Assert.Equal(0, overriddenOrHidden.HiddenMembers.Length);
        }

        /// <summary>
        /// Choose "first" candidate if custom modifier counts match.
        /// </summary>
        [Fact]
        public void TestCustomModifierTieBreak2()
        {
            var il = @"
.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance void  Method1(int32[] modopt([mscorlib]System.Int32) x) cil managed
  {
    ret
  }
  .method public hidebysig newslot virtual 
          instance void  Method1(int32[] modopt([mscorlib]System.Int64) x) cil managed
  {
    ret
  }

  // Same as Method1, but order of overloads is reversed.
  .method public hidebysig newslot virtual 
          instance void  Method2(int32[] modopt([mscorlib]System.Int64) x) cil managed
  {
    ret
  }
  .method public hidebysig newslot virtual 
          instance void  Method2(int32[] modopt([mscorlib]System.Int32) x) cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class Base
";

            var csharp = @"
class Derived : Base 
{
    public override void Method1(int[] x) { }
    public override void Method2(int[] x) { }
}
";

            var comp = CreateCompilationWithILAndMscorlib40(csharp, il);
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;
            var baseType = global.GetMember<NamedTypeSymbol>("Base");
            var derivedType = global.GetMember<NamedTypeSymbol>("Derived");

            var firstBaseMethod1 = baseType.GetMembers("Method1").OfType<MethodSymbol>().First();
            var firstBaseMethod2 = baseType.GetMembers("Method2").OfType<MethodSymbol>().First();

            var derivedMethod1 = derivedType.GetMember<MethodSymbol>("Method1");
            var derivedMethod2 = derivedType.GetMember<MethodSymbol>("Method2");

            var overriddenOrHidden1 = derivedMethod1.OverriddenOrHiddenMembers;
            Assert.Equal(firstBaseMethod1, overriddenOrHidden1.OverriddenMembers.Single());
            Assert.Equal(0, overriddenOrHidden1.HiddenMembers.Length);

            var overriddenOrHidden2 = derivedMethod2.OverriddenOrHiddenMembers;
            Assert.Equal(firstBaseMethod2, overriddenOrHidden2.OverriddenMembers.Single());
            Assert.Equal(0, overriddenOrHidden2.HiddenMembers.Length);
        }

        [Fact]
        public void HideReadOnlyPropertyWithWriteOnlyProperty()
        {
            var text = @"
public class TestClass1
{
    public int P1 { get { return 0;} }
}
public class TestClass2 : TestClass1
{
    public new int P1 { set { } }
}
class Program
{
    static void Main(string[] args)
    {
        TestClass2 c2 = new TestClass2();
        int i = c2.P1;
        c2.P1 = i;
    }
}
";
            CreateCompilation(text)
                .VerifyDiagnostics(Diagnostic(ErrorCode.ERR_PropertyLacksGet, "c2.P1").WithArguments("TestClass2.P1"));
        }

        [Fact]
        public void HidePublicPropertyWithPrivateProperty()
        {
            var text = @"
public class TestClass1
{
    public int P1 { get; set; }
}
public class TestClass2 : TestClass1
{
    private new int P1 { set { } }
}
class Program
{
    static void Main(string[] args)
    {
        TestClass1 c1 = new TestClass2();
        c1.P1 = 0;
    }
}
";
            var comp = CreateCompilation(text);
            Assert.Empty(comp.GetDiagnostics());
        }

        [Fact]
        public void OverrideAccessorOnly()
        {
            var text = @"
public class Base
{
    public virtual long Property1 { get; set; }
}

public class Derived1 : Base
{
    public override long Property1 { get { return 0; } }
}
";
            var comp = CreateCompilation(text);
            Assert.Empty(comp.GetDiagnostics());
        }

        [Fact]
        public void TestOverrideSealNotVisibleMemberMetadataErr()
        {
            #region "Src"
            var text1 = @"
using Metadata;

public class CSClass : Metadata.VBClass02
{
    // Seal
    public override void Sub01(B p1, A p2)
    {
    }
    // ref
    protected override void Sub01(B p1, B p2)
    {
    }
    // not visible
    internal override void Sub01(params B[] p1)
    {
    }
}
";
            #endregion

            var comp = CreateCompilation(
                text1,
                references: new[] { TestReferences.MetadataTests.InterfaceAndClass.VBClasses02 },
                assemblyName: "OHI_OverrideSealNotVisibleMember001",
                options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "Sub01").WithArguments("CSClass.Sub01(Metadata.B, Metadata.A)", "Metadata.VBClass02.Sub01(Metadata.B, Metadata.A)").WithLocation(7, 26), // CS0239
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Sub01").WithArguments("CSClass.Sub01(Metadata.B, Metadata.B)").WithLocation(11, 29), // CS0115
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Sub01").WithArguments("CSClass.Sub01(params Metadata.B[])").WithLocation(15, 28) // CS0115
            );
        }

        [Fact]
        public void TestAccessUnqualifiedNestedTypeMetadataErr()
        {
            #region "Src"
            var text1 = @"
class CSClass : VBIMeth02Impl, IMeth02, IMeth03
{
    INested n; // CS0246
    public void M(INested nested)  // CS0246
    {
        nested.NestedSub(128);
    }
}
class CSHide : VBIMeth02Impl, IMeth02, IMeth03
{
    internal enum INested { One, Two, Three }
    INested n; // OK
    internal void M(INested nested) // OK
    {
    }
}
";
            #endregion

            var comp = CreateCompilation(
                text1,
                references: new[]
                {
                    TestReferences.MetadataTests.InterfaceAndClass.VBInterfaces01,
                    TestReferences.MetadataTests.InterfaceAndClass.VBClasses01
                },
                assemblyName: "OHI_AccessUnqualifiedNestedType001",
                options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (5,19): error CS0246: The type or namespace name 'INested' could not be found (are you missing a using directive or an assembly reference?)
                //     public void M(INested nested)  // CS0246
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "INested").WithArguments("INested"),
                // (4,5): error CS0246: The type or namespace name 'INested' could not be found (are you missing a using directive or an assembly reference?)
                //     INested n; // CS0246
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "INested").WithArguments("INested"),
                // (4,13): warning CS0169: The field 'CSClass.n' is never used
                //     INested n; // CS0246
                Diagnostic(ErrorCode.WRN_UnreferencedField, "n").WithArguments("CSClass.n"),
                // (13,13): warning CS0169: The field 'CSHide.n' is never used
                //     INested n; // OK
                Diagnostic(ErrorCode.WRN_UnreferencedField, "n").WithArguments("CSHide.n"));
        }

        [WorkItem(543263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543263")]
        [Fact]
        public void TestMixedPropertyAccessorModifiers_EmptyAbstract()
        {
            var text1 = @"
abstract class Derived : AccessorModifierMismatch
{
    // Not overriding anything.
}
";
            var refs = new MetadataReference[] { TestReferences.SymbolsTests.Properties };
            CreateCompilation(text1, references: refs, options: TestOptions.ReleaseDll).VerifyDiagnostics();
        }

        [WorkItem(543263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543263")]
        [Fact]
        public void TestMixedPropertyAccessorModifiers_EmptyConcrete()
        {
            var text1 = @"
class Derived : AccessorModifierMismatch
{
    // Not overriding anything.
}
";
            var refs = new MetadataReference[] { TestReferences.SymbolsTests.Properties };
            CreateCompilation(text1, references: refs, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (2,7): error CS0534: 'Derived' does not implement inherited abstract member 'AccessorModifierMismatch.NoneAbstract.set'
                // class Derived : AccessorModifierMismatch
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "AccessorModifierMismatch.NoneAbstract.set"),
                // (2,7): error CS0534: 'Derived' does not implement inherited abstract member 'AccessorModifierMismatch.AbstractNone.get'
                // class Derived : AccessorModifierMismatch
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "AccessorModifierMismatch.AbstractNone.get"));
        }

        [WorkItem(543263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543263")]
        [Fact]
        public void TestMixedPropertyAccessorModifiers_OverrideGetters()
        {
            var text1 = @"
class Derived : AccessorModifierMismatch // CS0534 (didn't implement AbstractAbstract.set)
{
    public override int NoneNone { get { return 0; } } // CS0506 (not virtual)
    public override int NoneAbstract { get { return 0; } } // CS0506 (not virtual)
    public override int NoneVirtual { get { return 0; } } // CS0506 (not virtual)
    public override int NoneOverride { get { return 0; } } // CS1545 (bogus)
    public override int NoneSealed { get { return 0; } } // CS1545 (bogus)

    public override int AbstractNone { get { return 0; } }
    public override int AbstractAbstract { get { return 0; } }
    public override int AbstractVirtual { get { return 0; } }
    public override int AbstractOverride { get { return 0; } } // CS1545 (bogus)
    public override int AbstractSealed { get { return 0; } } // CS1545 (bogus)

    public override int VirtualNone { get { return 0; } }
    public override int VirtualAbstract { get { return 0; } }
    public override int VirtualVirtual { get { return 0; } }
    public override int VirtualOverride { get { return 0; } } // CS1545 (bogus)
    public override int VirtualSealed { get { return 0; } } // CS1545 (bogus)

    public override int OverrideNone { get { return 0; } } // CS1545 (bogus)
    public override int OverrideAbstract { get { return 0; } } // CS1545 (bogus)
    public override int OverrideVirtual { get { return 0; } } // CS1545 (bogus)
    public override int OverrideOverride { get { return 0; } }
    public override int OverrideSealed { get { return 0; } } // CS1545 (bogus)

    public override int SealedNone { get { return 0; } } // CS1545 (bogus)
    public override int SealedAbstract { get { return 0; } } // CS1545 (bogus)
    public override int SealedVirtual { get { return 0; } } // CS1545 (bogus)
    public override int SealedOverride { get { return 0; } } // CS1545 (bogus)
    public override int SealedSealed { get { return 0; } } // CS0239 (sealed)
}
";
            var refs = new MetadataReference[] { TestReferences.SymbolsTests.Properties };
            CreateCompilation(text1, references: refs, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (4,25): error CS0506: 'Derived.NoneNone': cannot override inherited member 'AccessorModifierMismatch.NoneNone' because it is not marked virtual, abstract, or override
                //     public override int NoneNone { get { return 0; } } // CS0506 (not virtual)
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "NoneNone").WithArguments("Derived.NoneNone", "AccessorModifierMismatch.NoneNone"),
                // (5,40): error CS0506: 'Derived.NoneAbstract.get': cannot override inherited member 'AccessorModifierMismatch.NoneNone.get' because it is not marked virtual, abstract, or override
                //     public override int NoneAbstract { get { return 0; } } // CS0506 (not virtual)
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "get").WithArguments("Derived.NoneAbstract.get", "AccessorModifierMismatch.NoneNone.get"),
                // (6,39): error CS0506: 'Derived.NoneVirtual.get': cannot override inherited member 'AccessorModifierMismatch.NoneNone.get' because it is not marked virtual, abstract, or override
                //     public override int NoneVirtual { get { return 0; } } // CS0506 (not virtual)
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "get").WithArguments("Derived.NoneVirtual.get", "AccessorModifierMismatch.NoneNone.get"),
                // (7,25): error CS0569: 'Derived.NoneOverride': cannot override 'AccessorModifierMismatch.NoneOverride' because it is not supported by the language
                //     public override int NoneOverride { get { return 0; } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "NoneOverride").WithArguments("Derived.NoneOverride", "AccessorModifierMismatch.NoneOverride"),
                // (8,25): error CS0569: 'Derived.NoneSealed': cannot override 'AccessorModifierMismatch.NoneSealed' because it is not supported by the language
                //     public override int NoneSealed { get { return 0; } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "NoneSealed").WithArguments("Derived.NoneSealed", "AccessorModifierMismatch.NoneSealed"),
                // (13,25): error CS0569: 'Derived.AbstractOverride': cannot override 'AccessorModifierMismatch.AbstractOverride' because it is not supported by the language
                //     public override int AbstractOverride { get { return 0; } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "AbstractOverride").WithArguments("Derived.AbstractOverride", "AccessorModifierMismatch.AbstractOverride"),
                // (14,25): error CS0569: 'Derived.AbstractSealed': cannot override 'AccessorModifierMismatch.AbstractSealed' because it is not supported by the language
                //     public override int AbstractSealed { get { return 0; } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "AbstractSealed").WithArguments("Derived.AbstractSealed", "AccessorModifierMismatch.AbstractSealed"),
                // (19,25): error CS0569: 'Derived.VirtualOverride': cannot override 'AccessorModifierMismatch.VirtualOverride' because it is not supported by the language
                //     public override int VirtualOverride { get { return 0; } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "VirtualOverride").WithArguments("Derived.VirtualOverride", "AccessorModifierMismatch.VirtualOverride"),
                // (20,25): error CS0569: 'Derived.VirtualSealed': cannot override 'AccessorModifierMismatch.VirtualSealed' because it is not supported by the language
                //     public override int VirtualSealed { get { return 0; } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "VirtualSealed").WithArguments("Derived.VirtualSealed", "AccessorModifierMismatch.VirtualSealed"),
                // (22,25): error CS0569: 'Derived.OverrideNone': cannot override 'AccessorModifierMismatch.OverrideNone' because it is not supported by the language
                //     public override int OverrideNone { get { return 0; } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "OverrideNone").WithArguments("Derived.OverrideNone", "AccessorModifierMismatch.OverrideNone"),
                // (23,25): error CS0569: 'Derived.OverrideAbstract': cannot override 'AccessorModifierMismatch.OverrideAbstract' because it is not supported by the language
                //     public override int OverrideAbstract { get { return 0; } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "OverrideAbstract").WithArguments("Derived.OverrideAbstract", "AccessorModifierMismatch.OverrideAbstract"),
                // (24,25): error CS0569: 'Derived.OverrideVirtual': cannot override 'AccessorModifierMismatch.OverrideVirtual' because it is not supported by the language
                //     public override int OverrideVirtual { get { return 0; } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "OverrideVirtual").WithArguments("Derived.OverrideVirtual", "AccessorModifierMismatch.OverrideVirtual"),
                // (26,25): error CS0569: 'Derived.OverrideSealed': cannot override 'AccessorModifierMismatch.OverrideSealed' because it is not supported by the language
                //     public override int OverrideSealed { get { return 0; } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "OverrideSealed").WithArguments("Derived.OverrideSealed", "AccessorModifierMismatch.OverrideSealed"),
                // (28,25): error CS0569: 'Derived.SealedNone': cannot override 'AccessorModifierMismatch.SealedNone' because it is not supported by the language
                //     public override int SealedNone { get { return 0; } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "SealedNone").WithArguments("Derived.SealedNone", "AccessorModifierMismatch.SealedNone"),
                // (29,25): error CS0569: 'Derived.SealedAbstract': cannot override 'AccessorModifierMismatch.SealedAbstract' because it is not supported by the language
                //     public override int SealedAbstract { get { return 0; } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "SealedAbstract").WithArguments("Derived.SealedAbstract", "AccessorModifierMismatch.SealedAbstract"),
                // (30,25): error CS0569: 'Derived.SealedVirtual': cannot override 'AccessorModifierMismatch.SealedVirtual' because it is not supported by the language
                //     public override int SealedVirtual { get { return 0; } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "SealedVirtual").WithArguments("Derived.SealedVirtual", "AccessorModifierMismatch.SealedVirtual"),
                // (31,25): error CS0569: 'Derived.SealedOverride': cannot override 'AccessorModifierMismatch.SealedOverride' because it is not supported by the language
                //     public override int SealedOverride { get { return 0; } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "SealedOverride").WithArguments("Derived.SealedOverride", "AccessorModifierMismatch.SealedOverride"),
                // (32,25): error CS0239: 'Derived.SealedSealed': cannot override inherited member 'AccessorModifierMismatch.SealedSealed' because it is sealed
                //     public override int SealedSealed { get { return 0; } } // CS0239 (sealed)
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "SealedSealed").WithArguments("Derived.SealedSealed", "AccessorModifierMismatch.SealedSealed"),
                // (2,7): error CS0534: 'Derived' does not implement inherited abstract member 'AccessorModifierMismatch.NoneAbstract.set'
                // class Derived : AccessorModifierMismatch // CS0534 (didn't implement AbstractAbstract.set)
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "AccessorModifierMismatch.NoneAbstract.set"));
        }

        [WorkItem(543263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543263")]
        [Fact]
        public void TestMixedPropertyAccessorModifiers_OverrideSetters()
        {
            var text1 = @"
class Derived : AccessorModifierMismatch // CS0534 (didn't implement AbstractAbstract.get)
{
    public override int NoneNone { set { } } // CS0506 (not virtual)
    public override int NoneAbstract { set { } }
    public override int NoneVirtual { set { } }
    public override int NoneOverride { set { } } // CS1545 (bogus)
    public override int NoneSealed { set { } } // CS1545 (bogus)

    public override int AbstractNone { set { } } // CS0506 (not virtual)
    public override int AbstractAbstract { set { } }
    public override int AbstractVirtual { set { } }
    public override int AbstractOverride { set { } } // CS1545 (bogus)
    public override int AbstractSealed { set { } } // CS1545 (bogus)

    public override int VirtualNone { set { } }
    public override int VirtualAbstract { set { } }
    public override int VirtualVirtual { set { } }
    public override int VirtualOverride { set { } } // CS1545 (bogus)
    public override int VirtualSealed { set { } } // CS1545 (bogus)

    public override int OverrideNone { set { } } // CS1545 (bogus)
    public override int OverrideAbstract { set { } } // CS1545 (bogus)
    public override int OverrideVirtual { set { } } // CS1545 (bogus)
    public override int OverrideOverride { set { } }
    public override int OverrideSealed { set { } } // CS1545 (bogus)

    public override int SealedNone { set { } } // CS1545 (bogus)
    public override int SealedAbstract { set { } } // CS1545 (bogus)
    public override int SealedVirtual { set { } } // CS1545 (bogus)
    public override int SealedOverride { set { } } // CS1545 (bogus)
    public override int SealedSealed { set { } } // CS0239 (sealed)
}
";
            var refs = new MetadataReference[] { TestReferences.SymbolsTests.Properties };
            CreateCompilation(text1, references: refs, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (4,25): error CS0506: 'Derived.NoneNone': cannot override inherited member 'AccessorModifierMismatch.NoneNone' because it is not marked virtual, abstract, or override
                //     public override int NoneNone { set { } } // CS0506 (not virtual)
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "NoneNone").WithArguments("Derived.NoneNone", "AccessorModifierMismatch.NoneNone"),
                // (7,25): error CS0569: 'Derived.NoneOverride': cannot override 'AccessorModifierMismatch.NoneOverride' because it is not supported by the language
                //     public override int NoneOverride { set { } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "NoneOverride").WithArguments("Derived.NoneOverride", "AccessorModifierMismatch.NoneOverride"),
                // (8,25): error CS0569: 'Derived.NoneSealed': cannot override 'AccessorModifierMismatch.NoneSealed' because it is not supported by the language
                //     public override int NoneSealed { set { } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "NoneSealed").WithArguments("Derived.NoneSealed", "AccessorModifierMismatch.NoneSealed"),
                // (10,40): error CS0506: 'Derived.AbstractNone.set': cannot override inherited member 'AccessorModifierMismatch.NoneNone.set' because it is not marked virtual, abstract, or override
                //     public override int AbstractNone { set { } } // CS0506 (not virtual)
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "set").WithArguments("Derived.AbstractNone.set", "AccessorModifierMismatch.NoneNone.set"),
                // (13,25): error CS0569: 'Derived.AbstractOverride': cannot override 'AccessorModifierMismatch.AbstractOverride' because it is not supported by the language
                //     public override int AbstractOverride { set { } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "AbstractOverride").WithArguments("Derived.AbstractOverride", "AccessorModifierMismatch.AbstractOverride"),
                // (14,25): error CS0569: 'Derived.AbstractSealed': cannot override 'AccessorModifierMismatch.AbstractSealed' because it is not supported by the language
                //     public override int AbstractSealed { set { } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "AbstractSealed").WithArguments("Derived.AbstractSealed", "AccessorModifierMismatch.AbstractSealed"),
                // (16,39): error CS0506: 'Derived.VirtualNone.set': cannot override inherited member 'AccessorModifierMismatch.NoneNone.set' because it is not marked virtual, abstract, or override
                //     public override int VirtualNone { set { } }
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "set").WithArguments("Derived.VirtualNone.set", "AccessorModifierMismatch.NoneNone.set"),
                // (19,25): error CS0569: 'Derived.VirtualOverride': cannot override 'AccessorModifierMismatch.VirtualOverride' because it is not supported by the language
                //     public override int VirtualOverride { set { } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "VirtualOverride").WithArguments("Derived.VirtualOverride", "AccessorModifierMismatch.VirtualOverride"),
                // (20,25): error CS0569: 'Derived.VirtualSealed': cannot override 'AccessorModifierMismatch.VirtualSealed' because it is not supported by the language
                //     public override int VirtualSealed { set { } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "VirtualSealed").WithArguments("Derived.VirtualSealed", "AccessorModifierMismatch.VirtualSealed"),
                // (22,25): error CS0569: 'Derived.OverrideNone': cannot override 'AccessorModifierMismatch.OverrideNone' because it is not supported by the language
                //     public override int OverrideNone { set { } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "OverrideNone").WithArguments("Derived.OverrideNone", "AccessorModifierMismatch.OverrideNone"),
                // (23,25): error CS0569: 'Derived.OverrideAbstract': cannot override 'AccessorModifierMismatch.OverrideAbstract' because it is not supported by the language
                //     public override int OverrideAbstract { set { } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "OverrideAbstract").WithArguments("Derived.OverrideAbstract", "AccessorModifierMismatch.OverrideAbstract"),
                // (24,25): error CS0569: 'Derived.OverrideVirtual': cannot override 'AccessorModifierMismatch.OverrideVirtual' because it is not supported by the language
                //     public override int OverrideVirtual { set { } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "OverrideVirtual").WithArguments("Derived.OverrideVirtual", "AccessorModifierMismatch.OverrideVirtual"),
                // (26,25): error CS0569: 'Derived.OverrideSealed': cannot override 'AccessorModifierMismatch.OverrideSealed' because it is not supported by the language
                //     public override int OverrideSealed { set { } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "OverrideSealed").WithArguments("Derived.OverrideSealed", "AccessorModifierMismatch.OverrideSealed"),
                // (28,25): error CS0569: 'Derived.SealedNone': cannot override 'AccessorModifierMismatch.SealedNone' because it is not supported by the language
                //     public override int SealedNone { set { } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "SealedNone").WithArguments("Derived.SealedNone", "AccessorModifierMismatch.SealedNone"),
                // (29,25): error CS0569: 'Derived.SealedAbstract': cannot override 'AccessorModifierMismatch.SealedAbstract' because it is not supported by the language
                //     public override int SealedAbstract { set { } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "SealedAbstract").WithArguments("Derived.SealedAbstract", "AccessorModifierMismatch.SealedAbstract"),
                // (30,25): error CS0569: 'Derived.SealedVirtual': cannot override 'AccessorModifierMismatch.SealedVirtual' because it is not supported by the language
                //     public override int SealedVirtual { set { } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "SealedVirtual").WithArguments("Derived.SealedVirtual", "AccessorModifierMismatch.SealedVirtual"),
                // (31,25): error CS0569: 'Derived.SealedOverride': cannot override 'AccessorModifierMismatch.SealedOverride' because it is not supported by the language
                //     public override int SealedOverride { set { } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideBogusMethod, "SealedOverride").WithArguments("Derived.SealedOverride", "AccessorModifierMismatch.SealedOverride"),
                // (32,25): error CS0239: 'Derived.SealedSealed': cannot override inherited member 'AccessorModifierMismatch.SealedSealed' because it is sealed
                //     public override int SealedSealed { set { } } // CS0239 (sealed)
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "SealedSealed").WithArguments("Derived.SealedSealed", "AccessorModifierMismatch.SealedSealed"),
                // (2,7): error CS0534: 'Derived' does not implement inherited abstract member 'AccessorModifierMismatch.AbstractNone.get'
                // class Derived : AccessorModifierMismatch // CS0534 (didn't implement AbstractAbstract.get)
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "AccessorModifierMismatch.AbstractNone.get"));
        }

        [WorkItem(543263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543263")]
        [Fact]
        public void TestMixedPropertyAccessorModifiers_AbstractSealed()
        {
            var il = @"
.class public auto ansi beforefieldinit AccessorModifierMismatchBase
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance void  SetSealed(int32 x) cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
} // end of class AccessorModifierMismatchBase

.class public abstract auto ansi beforefieldinit AccessorModifierMismatch
       extends AccessorModifierMismatchBase
{
  .method public hidebysig newslot abstract virtual 
          instance int32  GetAbstract() cil managed
  {
  }

  .method public hidebysig virtual final instance void 
          SetSealed(int32 x) cil managed
  {
    ret
  }

  .property instance int32 AbstractSealed()
  {
    .get instance int32 AccessorModifierMismatch::GetAbstract()
    .set instance void AccessorModifierMismatch::SetSealed(int32)
  }

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void AccessorModifierMismatchBase::.ctor()
    ret
  }
} // end of class AccessorModifierMismatch
";

            var csharp = @"
class Derived : AccessorModifierMismatch
{
    // Override accessor as method since property is bogus.
    public override int GetAbstract() { return 0; }
}
";

            CreateCompilationWithILAndMscorlib40(csharp, il).VerifyDiagnostics();
        }

        [WorkItem(543263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543263")]
        [Fact]
        public void TestMixedEventAccessorModifiers_EmptyAbstract()
        {
            var text1 = @"
abstract class Derived : AccessorModifierMismatch
{
    // Not overriding anything.
}
";
            var refs = new[] { TestReferences.SymbolsTests.Events };
            CreateCompilation(text1, references: refs, options: TestOptions.ReleaseDll).VerifyDiagnostics();
        }

        [WorkItem(543263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543263")]
        [Fact]
        public void TestMixedEventAccessorModifiers_EmptyConcrete()
        {
            var text1 = @"
class Derived : AccessorModifierMismatch
{
    // Not overriding anything.
}
";
            var refs = new MetadataReference[] { TestReferences.SymbolsTests.Events };
            CreateCompilation(text1, references: refs, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (2,7): error CS0534: 'Derived' does not implement inherited abstract member 'AccessorModifierMismatch.NoneAbstract.remove'
                // class Derived : AccessorModifierMismatch
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "AccessorModifierMismatch.NoneAbstract.remove"),
                // (2,7): error CS0534: 'Derived' does not implement inherited abstract member 'AccessorModifierMismatch.AbstractNone.add'
                // class Derived : AccessorModifierMismatch
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "AccessorModifierMismatch.AbstractNone.add"));
        }

        // NOTE: The behavior is quite different from the analogous property tests.
        [WorkItem(543263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543263")]
        [Fact]
        public void TestMixedEventAccessorModifiers_OverrideAccessors()
        {
            var text1 = @"
class Derived : AccessorModifierMismatch
{
    public override event System.Action NoneNone { add { } remove { } } // CS0506 (not virtual)
    public override event System.Action NoneAbstract { add { } remove { } } // CS0506 (add not virtual)
    public override event System.Action NoneVirtual { add { } remove { } } // CS0506 (add not virtual)
    public override event System.Action NoneOverride { add { } remove { } } // CS0506 (add not virtual)
    public override event System.Action NoneSealed { add { } remove { } } // CS1545 (bogus)

    public override event System.Action AbstractNone { add { } remove { } } // CS0506 (remove not virtual)
    public override event System.Action AbstractAbstract { add { } remove { } }
    public override event System.Action AbstractVirtual { add { } remove { } }
    public override event System.Action AbstractOverride { add { } remove { } }
    public override event System.Action AbstractSealed { add { } remove { } } // CS1545 (bogus)

    public override event System.Action VirtualNone { add { } remove { } } // CS0506 (remove not virtual)
    public override event System.Action VirtualAbstract { add { } remove { } }
    public override event System.Action VirtualVirtual { add { } remove { } }
    public override event System.Action VirtualOverride { add { } remove { } }
    public override event System.Action VirtualSealed { add { } remove { } } // CS1545 (bogus)

    public override event System.Action OverrideNone { add { } remove { } } // CS0506 (remove not virtual)
    public override event System.Action OverrideAbstract { add { } remove { } }
    public override event System.Action OverrideVirtual { add { } remove { } }
    public override event System.Action OverrideOverride { add { } remove { } }
    public override event System.Action OverrideSealed { add { } remove { } } // CS1545 (bogus)

    public override event System.Action SealedNone { add { } remove { } } // CS1545 (bogus)
    public override event System.Action SealedAbstract { add { } remove { } } // CS1545 (bogus)
    public override event System.Action SealedVirtual { add { } remove { } } // CS1545 (bogus)
    public override event System.Action SealedOverride { add { } remove { } } // CS1545 (bogus)
    public override event System.Action SealedSealed { add { } remove { } } // CS0239 (sealed)
}
";
            // ACASEY: these are not exactly the errors that Dev10 produces, but they seem sensible.
            var refs = new MetadataReference[] { TestReferences.SymbolsTests.Events };
            CreateCompilation(text1, references: refs, options: TestOptions.ReleaseDll).VerifyDiagnostics(
                // (4,41): error CS0506: 'Derived.NoneNone': cannot override inherited member 'AccessorModifierMismatch.NoneNone' because it is not marked virtual, abstract, or override
                //     public override event System.Action NoneNone { add { } remove { } } // CS0506 (not virtual)
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "NoneNone").WithArguments("Derived.NoneNone", "AccessorModifierMismatch.NoneNone"),
                // (5,56): error CS0506: 'Derived.NoneAbstract.add': cannot override inherited member 'AccessorModifierMismatch.NoneNone.add' because it is not marked virtual, abstract, or override
                //     public override event System.Action NoneAbstract { add { } remove { } } // CS0506 (add not virtual)
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "add").WithArguments("Derived.NoneAbstract.add", "AccessorModifierMismatch.NoneNone.add"),
                // (6,55): error CS0506: 'Derived.NoneVirtual.add': cannot override inherited member 'AccessorModifierMismatch.NoneNone.add' because it is not marked virtual, abstract, or override
                //     public override event System.Action NoneVirtual { add { } remove { } } // CS0506 (add not virtual)
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "add").WithArguments("Derived.NoneVirtual.add", "AccessorModifierMismatch.NoneNone.add"),
                // (7,56): error CS0506: 'Derived.NoneOverride.add': cannot override inherited member 'AccessorModifierMismatch.NoneNone.add' because it is not marked virtual, abstract, or override
                //     public override event System.Action NoneOverride { add { } remove { } } // CS0506 (add not virtual)
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "add").WithArguments("Derived.NoneOverride.add", "AccessorModifierMismatch.NoneNone.add"),
                // (8,41): error CS0239: 'Derived.NoneSealed': cannot override inherited member 'AccessorModifierMismatch.NoneSealed' because it is sealed
                //     public override event System.Action NoneSealed { add { } remove { } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "NoneSealed").WithArguments("Derived.NoneSealed", "AccessorModifierMismatch.NoneSealed"),

                // (10,64): error CS0506: 'Derived.AbstractNone.remove': cannot override inherited member 'AccessorModifierMismatch.NoneNone.remove' because it is not marked virtual, abstract, or override
                //     public override event System.Action AbstractNone { add { } remove { } } // CS0506 (remove not virtual)
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "remove").WithArguments("Derived.AbstractNone.remove", "AccessorModifierMismatch.NoneNone.remove"),
                // (14,41): error CS0239: 'Derived.AbstractSealed': cannot override inherited member 'AccessorModifierMismatch.AbstractSealed' because it is sealed
                //     public override event System.Action AbstractSealed { add { } remove { } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "AbstractSealed").WithArguments("Derived.AbstractSealed", "AccessorModifierMismatch.AbstractSealed"),

                // (16,63): error CS0506: 'Derived.VirtualNone.remove': cannot override inherited member 'AccessorModifierMismatch.NoneNone.remove' because it is not marked virtual, abstract, or override
                //     public override event System.Action VirtualNone { add { } remove { } } // CS0506 (remove not virtual)
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "remove").WithArguments("Derived.VirtualNone.remove", "AccessorModifierMismatch.NoneNone.remove"),
                // (20,41): error CS0239: 'Derived.VirtualSealed': cannot override inherited member 'AccessorModifierMismatch.VirtualSealed' because it is sealed
                //     public override event System.Action VirtualSealed { add { } remove { } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "VirtualSealed").WithArguments("Derived.VirtualSealed", "AccessorModifierMismatch.VirtualSealed"),

                // (22,64): error CS0506: 'Derived.OverrideNone.remove': cannot override inherited member 'AccessorModifierMismatch.NoneNone.remove' because it is not marked virtual, abstract, or override
                //     public override event System.Action OverrideNone { add { } remove { } } // CS0506 (remove not virtual)
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "remove").WithArguments("Derived.OverrideNone.remove", "AccessorModifierMismatch.NoneNone.remove"),
                // (26,41): error CS0239: 'Derived.OverrideSealed': cannot override inherited member 'AccessorModifierMismatch.OverrideSealed' because it is sealed
                //     public override event System.Action OverrideSealed { add { } remove { } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "OverrideSealed").WithArguments("Derived.OverrideSealed", "AccessorModifierMismatch.OverrideSealed"),

                // (28,41): error CS0239: 'Derived.SealedNone': cannot override inherited member 'AccessorModifierMismatch.SealedNone' because it is sealed
                //     public override event System.Action SealedNone { add { } remove { } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "SealedNone").WithArguments("Derived.SealedNone", "AccessorModifierMismatch.SealedNone"),
                // (29,41): error CS0239: 'Derived.SealedAbstract': cannot override inherited member 'AccessorModifierMismatch.SealedAbstract' because it is sealed
                //     public override event System.Action SealedAbstract { add { } remove { } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "SealedAbstract").WithArguments("Derived.SealedAbstract", "AccessorModifierMismatch.SealedAbstract"),
                // (30,41): error CS0239: 'Derived.SealedVirtual': cannot override inherited member 'AccessorModifierMismatch.SealedVirtual' because it is sealed
                //     public override event System.Action SealedVirtual { add { } remove { } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "SealedVirtual").WithArguments("Derived.SealedVirtual", "AccessorModifierMismatch.SealedVirtual"),
                // (31,41): error CS0239: 'Derived.SealedOverride': cannot override inherited member 'AccessorModifierMismatch.SealedOverride' because it is sealed
                //     public override event System.Action SealedOverride { add { } remove { } } // CS1545 (bogus)
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "SealedOverride").WithArguments("Derived.SealedOverride", "AccessorModifierMismatch.SealedOverride"),
                // (32,41): error CS0239: 'Derived.SealedSealed': cannot override inherited member 'AccessorModifierMismatch.SealedSealed' because it is sealed
                //     public override event System.Action SealedSealed { add { } remove { } } // CS0239 (sealed)
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "SealedSealed").WithArguments("Derived.SealedSealed", "AccessorModifierMismatch.SealedSealed"));
        }

        [WorkItem(543263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543263")]
        [Fact]
        public void TestMixedEventAccessorModifiers_AbstractSealed()
        {
            var il = @"
.class public auto ansi beforefieldinit AccessorModifierMismatchBase
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance void  RemoveSealed(class [mscorlib]System.Action a) cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
} // end of class AccessorModifierMismatchBase

.class public abstract auto ansi beforefieldinit AccessorModifierMismatch
       extends AccessorModifierMismatchBase
{
  .method public hidebysig newslot abstract virtual 
          instance void  AddAbstract(class [mscorlib]System.Action a) cil managed
  {
  }

  .method public hidebysig virtual final instance void 
          RemoveSealed(class [mscorlib]System.Action a) cil managed
  {
    ret
  }

  .event [mscorlib]System.Action AbstractSealed
  {
    .addon instance void AccessorModifierMismatch::AddAbstract(class [mscorlib]System.Action)
    .removeon instance void AccessorModifierMismatch::RemoveSealed(class [mscorlib]System.Action)
  }

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void AccessorModifierMismatchBase::.ctor()
    ret
  }
} // end of class AccessorModifierMismatch
";

            var csharp = @"
class Derived : AccessorModifierMismatch
{
    // Failed to implement AddAbstract
}
";
            CreateCompilationWithILAndMscorlib40(csharp, il).VerifyDiagnostics(
                // (2,7): error CS0534: 'Derived' does not implement inherited abstract member 'AccessorModifierMismatch.AbstractSealed.add'
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "AccessorModifierMismatch.AbstractSealed.add"));

            csharp = @"
class Derived : AccessorModifierMismatch
{
    // Cannot override accessor as method since property is bogus.
    public override void AddAbstract(System.Action a) { }
}
";
            CreateCompilationWithILAndMscorlib40(csharp, il).VerifyDiagnostics(
                // (5,26): error CS0115: 'Derived.AddAbstract(System.Action)': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "AddAbstract").WithArguments("Derived.AddAbstract(System.Action)"),
                // (2,7): error CS0534: 'Derived' does not implement inherited abstract member 'AccessorModifierMismatch.AbstractSealed.add'
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "AccessorModifierMismatch.AbstractSealed.add"));

            csharp = @"
class Derived : AccessorModifierMismatch
{
    // Cannot override just one accessor (C# rule).
    public override event System.Action AbstractSealed { add { } }
}
";
            CreateCompilationWithILAndMscorlib40(csharp, il).VerifyDiagnostics(
                // (5,41): error CS0065: 'Derived.AbstractSealed': event property must have both add and remove accessors
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "AbstractSealed").WithArguments("Derived.AbstractSealed"),
                // (5,41): error CS0239: 'Derived.AbstractSealed': cannot override inherited member 'AccessorModifierMismatch.AbstractSealed' because it is sealed
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "AbstractSealed").WithArguments("Derived.AbstractSealed", "AccessorModifierMismatch.AbstractSealed"));

            csharp = @"
class Derived : AccessorModifierMismatch
{
    // Cannot override sealed RemoveSealed.
    public override event System.Action AbstractSealed { add { } remove { } }
}
";
            CreateCompilationWithILAndMscorlib40(csharp, il).VerifyDiagnostics(
                // (5,41): error CS0239: 'Derived.AbstractSealed': cannot override inherited member 'AccessorModifierMismatch.AbstractSealed' because it is sealed
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "AbstractSealed").WithArguments("Derived.AbstractSealed", "AccessorModifierMismatch.AbstractSealed"));
        }

        #region "Regressions"

        [WorkItem(546834, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546834")]
        [Fact]
        public void PropertyHidingAccessor()
        {
            var text = @"
class Derived : Base
{
    public delegate void BogusDelegate();
    public event BogusDelegate set_Instance;

    public int get_Instance
    {
        get { return 0; }
    }

    public int var1
    { get;set; }

    public int var2
    { get;set; }

public void StartEvent()
    {
        if (set_Instance != null)
            set_Instance();
    }
}
public class Base
{
    public int Instance
    {
        get { return 0; }
        set { }
    }
    public int get_var1()
    {
        return 0;
    }
    public int get_var2
    { get;set; }
}
public class MainClass
{
    public static void Main() { }
}
";
            CreateCompilation(text).VerifyDiagnostics();
        }

        [WorkItem(539623, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539623")]
        [Fact]
        public void GenericTypeWithDiffTypeParamNotHideBase()
        {
            var text = @"namespace NS
{
    public class Base
    {
        public class Nested<T> { }
    }

    public class Derived : Base
    {
        public class Nested<T, U> { } // Roslyn warning CS0108
    }
}
namespace A
{
    public class Base<T>
    {
        public class Goo<U> { }

        public class Nested<U>
        {
            public static Goo<U> sFld = new Goo<U>();
        }
    }

    public class Derived<T> : Base<T>
    {
        public class Goo<U, V> { } // Roslyn warning CS0108

        public class Nested<U, V> // Roslyn warning CS0108
        {
            public static Goo<U, V> sFld = new Goo<U, V>();
        }
    }
}
";
            var comp = CreateCompilation(text);
            Assert.Equal(0, comp.GetDiagnostics().Count());
        }

        [Fact]
        public void HideAbstractReadOnlyPropertyWithAbstractWriteOnly()
        {
            var text = @"
abstract public class TestClass1
{
    public abstract int P2 { get; }
}
abstract public class TestClass2 : TestClass1
{
    public abstract new int P2 { set; }
}
public class TestClass3 : TestClass2
{
    int f1;
    public override int P2
    {
        get { return f1; }
        set { f1 = value; }
    }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "P2").WithArguments("TestClass2.P2", "TestClass1.P2"),
                Diagnostic(ErrorCode.ERR_NoGetToOverride, "get").WithArguments("TestClass3.P2.get", "TestClass2.P2"),
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "TestClass3").WithArguments("TestClass3", "TestClass1.P2.get"));
        }

        [WorkItem(540383, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540383")]
        [Fact]
        public void HideBaseImplementationWithPrivateProperty()
        {
            var text = @"
interface I1
{
    int Goo { get; set; }
}
class B1
{
    public int Goo { get { return 1; } set { } }
}
class B2 : B1, I1
{
    private new int Goo { get { return 1; } }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;
            Assert.Equal(
                global.GetMember<NamedTypeSymbol>("B1").GetMember<PropertySymbol>("Goo"),
                global.GetMember<NamedTypeSymbol>("B2").FindImplementationForInterfaceMember(
                    global.GetMember<NamedTypeSymbol>("I1").GetMember<PropertySymbol>("Goo")));
        }

        [WorkItem(540383, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540383")]
        [Fact]
        public void HideBaseImplementationWithStaticProperty()
        {
            var text = @"
interface I1
{
    int Goo { get; set; }
}
class B1
{
    public int Goo { get { return 1; } set { } }
}
class B2 : B1, I1
{
    public static new int Goo { get { return 1; } }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;
            Assert.Equal(
                global.GetMember<NamedTypeSymbol>("B1").GetMember<PropertySymbol>("Goo"),
                global.GetMember<NamedTypeSymbol>("B2").FindImplementationForInterfaceMember(
                    global.GetMember<NamedTypeSymbol>("I1").GetMember<PropertySymbol>("Goo")));
        }

        [WorkItem(540383, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540383")]
        [Fact]
        public void HideBaseImplementationWithWrongTypeProperty()
        {
            var text = @"
interface I1
{
    int Goo { get; set; }
}
class B1
{
    public int Goo { get { return 1; } set { } }
}
class B2 : B1, I1
{
    public new float Goo { get { return 1; } }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;
            Assert.Equal(
                global.GetMember<NamedTypeSymbol>("B1").GetMember<PropertySymbol>("Goo"),
                global.GetMember<NamedTypeSymbol>("B2").FindImplementationForInterfaceMember(
                    global.GetMember<NamedTypeSymbol>("I1").GetMember<PropertySymbol>("Goo")));
        }

        [WorkItem(540383, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540383")]
        [Fact]
        public void HideInvalidBaseImplementationWithPrivateProperty()
        {
            var text = @"
interface I1
{
    int Goo { get; set; }
}
class B1
{
    public float Goo { get { return 1; } set { } }
}
class B2 : B1, I1
{
    private new int Goo { get { return 1; } }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (10,16): error CS0737: 'B2' does not implement interface member 'I1.Goo'. 'B2.Goo' cannot implement an interface member because it is not public.
                // class B2 : B1, I1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, "I1").WithArguments("B2", "I1.Goo", "B2.Goo").WithLocation(10, 16));

            var global = comp.GlobalNamespace;
            Assert.Null(global.GetMember<NamedTypeSymbol>("B2").FindImplementationForInterfaceMember(
                    global.GetMember<NamedTypeSymbol>("I1").GetMember<PropertySymbol>("Goo")));
        }

        [WorkItem(540383, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540383")]
        [Fact]
        public void HideInvalidBaseImplementationWithStaticProperty()
        {
            var text = @"
interface I1
{
    int Goo { get; set; }
}
class B1
{
    public float Goo { get { return 1; } set { } }
}
class B2 : B1, I1
{
    public static new int Goo { get { return 1; } }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (10,16): error CS0736: 'B2' does not implement instance interface member 'I1.Goo'. 'B2.Goo' cannot implement the interface member because it is static.
                // class B2 : B1, I1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic, "I1").WithArguments("B2", "I1.Goo", "B2.Goo").WithLocation(10, 16));

            var global = comp.GlobalNamespace;
            Assert.Null(global.GetMember<NamedTypeSymbol>("B2").FindImplementationForInterfaceMember(
                    global.GetMember<NamedTypeSymbol>("I1").GetMember<PropertySymbol>("Goo")));
        }

        /// <summary>
        /// CS0736ERR_CloseUnimplementedInterfaceMemberStatic should
        /// be reported, even if the close match is defined in metadata.
        /// </summary>
        [Fact]
        public void CS0736ERR_CloseUnimplementedInterfaceMemberStatic_BaseFromMetadata()
        {
            var ilSource =
@".class interface public abstract I
{
  .method public hidebysig newslot abstract virtual instance void M<T>() { }
}
.class public A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public hidebysig static void M<T>() { ret }
}";
            var csharpSource =
@"class B1 : I
{
    public void M<T>() { }
}
class B2 : A, I
{
}
class B3 : I
{
    public static void M<T>() { }
}";
            CreateCompilationWithILAndMscorlib40(csharpSource, ilSource).VerifyDiagnostics(
                // (5,15): error CS0736: 'B2' does not implement instance interface member 'I.M<T>()'. 'A.M<T>()' cannot implement the interface member because it is static.
                // class B2 : A, I
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic, "I").WithArguments("B2", "I.M<T>()", "A.M<T>()").WithLocation(5, 15),
                // (8,12): error CS0736: 'B3' does not implement instance interface member 'I.M<T>()'. 'B3.M<T>()' cannot implement the interface member because it is static.
                // class B3 : I
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic, "I").WithArguments("B3", "I.M<T>()", "B3.M<T>()").WithLocation(8, 12));
        }

        [WorkItem(540383, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540383")]
        [Fact]
        public void HideInvalidBaseImplementationWithWrongTypeProperty()
        {
            var text = @"
interface I1
{
    int Goo { get; set; }
}
class B1
{
    public float Goo { get { return 1; } set { } }
}
class B2 : B1, I1
{
    public new float Goo { get { return 1; } }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (10,16): error CS0738: 'B2' does not implement interface member 'I1.Goo'. 'B2.Goo' cannot implement 'I1.Goo' because it does not have the matching return type of 'int'.
                // class B2 : B1, I1
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, "I1").WithArguments("B2", "I1.Goo", "B2.Goo", "int").WithLocation(10, 16));

            var global = comp.GlobalNamespace;
            Assert.Null(global.GetMember<NamedTypeSymbol>("B2").FindImplementationForInterfaceMember(
                    global.GetMember<NamedTypeSymbol>("I1").GetMember<PropertySymbol>("Goo")));
        }

        [WorkItem(540420, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540420")]
        [Fact]
        public void HidingInEnum()
        {
            var text = @"
enum E
{
    Equals
}
";
            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact, WorkItem(543448, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543448")]
        public void GenericMethodsHidingFieldsAndEvents()
        {
            CreateCompilation(@"
class Base
{
    public int A = 1;
    public int B { get { return 1; } set { } }
    public event System.EventHandler C;
}

class Derived : Base
{
    public void A<T>() { }
    public void B<T>() { }
    public void C<T>() { }
}").
            VerifyDiagnostics(
                Diagnostic(ErrorCode.WRN_NewRequired, "A").WithArguments("Derived.A<T>()", "Base.A"),
                Diagnostic(ErrorCode.WRN_NewRequired, "B").WithArguments("Derived.B<T>()", "Base.B"),
                Diagnostic(ErrorCode.WRN_NewRequired, "C").WithArguments("Derived.C<T>()", "Base.C"),

                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "C").WithArguments("Base.C"));
        }

        [WorkItem(543448, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543448")]
        [Fact]
        public void GenericMethodHidesArityZero()
        {
            var text = @"
class Base
{
    public int A = 1;
    public int B { get { return 1; } set { } }
    public event System.EventHandler C;
}

class Sub : Base
{
    public void A<T>() { }
    public void B<T>() { }
    public void C<T>() { }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (11,17): warning CS0108: 'Sub.A<T>()' hides inherited member 'Base.A'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "A").WithArguments("Sub.A<T>()", "Base.A"),
                // (12,17): warning CS0108: 'Sub.B<T>()' hides inherited member 'Base.B'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "B").WithArguments("Sub.B<T>()", "Base.B"),
                // (13,17): warning CS0108: 'Sub.C<T>()' hides inherited member 'Base.C'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "C").WithArguments("Sub.C<T>()", "Base.C"),
                // (6,38): warning CS0067: The event 'Base.C' is never used
                //     public event System.EventHandler C;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "C").WithArguments("Base.C"));
        }

        [WorkItem(543908, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543908")]
        [Fact]
        public void OverrideMemberOfConstructedProtectedInnerClass()
        {
            var c1 = CreateCompilation(@"
public class Outer1<T>
{
    protected abstract class Inner1
    {
        public abstract void Method();
    }

    protected abstract class Inner2 : Inner1
    {
        public override void Method() { }
    }
}
");
            var c2 = CreateCompilation(@"
internal class Outer2 : Outer1<Outer2>
{
        private class Inner3 : Inner2 {}
}
", new MetadataReference[] { new CSharpCompilationReference(c1) });

            //repro requires two separate compilations
            c2.GetDiagnostics().Verify();
        }

        [WorkItem(543908, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543908")]
        [Fact]
        public void Repro11967()
        {
            var c1 = CreateCompilation(@"

using System.Collections.Generic;

public class ExpressionSyntax {}
public class SimpleNameSyntax : ExpressionSyntax {}
public class InvocationExpressionSyntax: ExpressionSyntax {}

public interface ITypeParameterSymbol {}

public abstract partial class AbstractGenerateMethodService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
{
    protected abstract class MethodInfo
    {
        protected abstract IList<ITypeParameterSymbol> DetermineTypeParameters();
    }

    protected abstract class AbstractInvocationInfo : MethodInfo
    {
        protected sealed override IList<ITypeParameterSymbol> DetermineTypeParameters()
        {
            return null;
        }
    }
}
");
            var c2 = CreateCompilation(@"

internal partial class CSharpGenerateMethodService :
        AbstractGenerateMethodService<CSharpGenerateMethodService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax>
{
        private class InvocationExpressionInfo : AbstractInvocationInfo {}
}
", new MetadataReference[] { new CSharpCompilationReference(c1) });

            //repro requires two separate compilations
            c2.GetDiagnostics().Verify();
        }

        [Fact]
        public void CS0570ERR_BindToBogus_RefParametersWithCustomModifiers()
        {
            var il = @"
.class public auto ansi beforefieldinit ModA extends [mscorlib]System.Object
{
  .method public specialname rtspecialname instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit ModB extends [mscorlib]System.Object
{
  .method public specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit C extends [mscorlib]System.Object
{

  .method public newslot virtual instance void  FN(class C c) cil managed
  {
    ldarg.0
    ldarg.1
    call       instance void C::F(class C)
    ret
  }

  .method public newslot virtual instance void  FA(class C modopt(ModA) c) cil managed
  {
    ldarg.0
    ldarg.1
    call       instance void C::F(class C modopt(ModA))
    ret
  }

  .method public newslot virtual instance void  FB(class C modopt(ModB) c) cil managed
  {
    ldarg.0
    ldarg.1
    call       instance void C::F(class C modopt(ModB))
    ret
  }

  .method public newslot virtual instance void  FAB(class C modopt(ModA) modopt(ModB) c) cil managed
  {
    ldarg.0
    ldarg.1
    call       instance void C::F(class C modopt(ModA) modopt(ModB))
    ret
  }


  .method public newslot virtual instance void G(class C& c) cil managed
  {
    ldstr      ""C::G(C &)""
    call       void [mscorlib]System.Console::WriteLine(string)
    ret
  }

  .method public newslot virtual instance void G(class C modopt(ModA) & c) cil managed
  {
    ldstr      ""C::G(C [A] &)""
    call       void [mscorlib]System.Console::WriteLine(string)
    ret
  }

  .method public newslot virtual instance void G(class C & modopt(ModB) c) cil managed
  {
    ldstr      ""C::G(C & [B])""
    call       void [mscorlib]System.Console::WriteLine(string)
    ret
  }

  .method public newslot virtual instance void G(class C modopt(ModA) & modopt(ModB) c) cil managed
  {
    ldstr      ""C::G(C [A] & [B])""
    call       void [mscorlib]System.Console::WriteLine(string)
    ret
  }

  .method public newslot virtual instance void GN(class C& c) cil managed
  {
    ldarg.0
    ldarg.1
    call       instance void C::G(class C&)
    ret
  }

  .method public newslot virtual instance void GA(class C modopt(ModA) & c) cil managed
  {
    ldarg.0
    ldarg.1
    call       instance void C::G(class C modopt(ModA) &)
    ret
  }

  .method public newslot virtual instance void GB(class C & modopt(ModB) c) cil managed
  {
    ldarg.0
    ldarg.1
    call       instance void C::G(class C & modopt(ModB))
    ret
  }

  .method public newslot virtual instance void GAB(class C modopt(ModA) & modopt(ModB) c) cil managed
  {
    ldarg.0
    ldarg.1
    call       instance void C::G(class C modopt(ModA) & modopt(ModB))
    ret
  }

  .method public specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit D extends C
{
  .method public specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call       instance void C::.ctor()
    ret
  }
}
";

            var csharp = @"
class Test
{
    void Goo()
    {
        C c = new C();

        c.FN(c); // no modopts
        c.FA(c); // modopt A
        c.FB(c); // modopt B
        c.FAB(c); // modopts A, B

        c.GN(ref c); // no modopts
        c.GA(ref c); // modopt A (inside ref)
        c.GB(ref c); // modopt B (outside ref)
        c.GAB(ref c); // modopts A, B (inside and outside ref, respectively)
    }
}";
            CompileAndVerify(CreateCompilationWithILAndMscorlib40(csharp, il));
        }

        [WorkItem(545653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545653")]
        [Fact]
        public void Repro14242_Property()
        {
            var source = @"
abstract class A
{
    public abstract int X { get; set; }
}
 
abstract class B : A
{
    public override int X { set { } }
}
 
abstract class C : B
{
    public new virtual string X {  get; set;  }
}
 
class D : C
{
    public override int X
    {
        get { return 0; }
    }
}
";
            var comp = CreateCompilation(source);
            var global = comp.GlobalNamespace;

            var propA = global.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>("X");
            var propB = global.GetMember<NamedTypeSymbol>("B").GetMember<PropertySymbol>("X");
            var propC = global.GetMember<NamedTypeSymbol>("C").GetMember<PropertySymbol>("X");
            var propD = global.GetMember<NamedTypeSymbol>("D").GetMember<PropertySymbol>("X");

            var ohmA = propA.OverriddenOrHiddenMembers;
            var ohmB = propB.OverriddenOrHiddenMembers;
            var ohmC = propC.OverriddenOrHiddenMembers;
            var ohmD = propD.OverriddenOrHiddenMembers;

            Assert.Equal(0, ohmA.OverriddenMembers.Length);
            Assert.Equal(0, ohmA.HiddenMembers.Length);

            Assert.Equal(propA, ohmB.OverriddenMembers.Single());
            Assert.Equal(0, ohmB.HiddenMembers.Length);

            Assert.Equal(0, ohmC.OverriddenMembers.Length);
            Assert.Equal(propB, ohmC.HiddenMembers.Single());

            Assert.Equal(propC, ohmD.OverriddenMembers.Single());
            Assert.Equal(0, ohmD.HiddenMembers.Length);

            comp.VerifyDiagnostics(
                // (19,25): error CS1715: 'D.X': type must be 'string' to match overridden member 'C.X'
                //     public override int X
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "X").WithArguments("D.X", "C.X", "string"),
                // (17,7): error CS0534: 'D' does not implement inherited abstract member 'A.X.get'
                // class D : C
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "D").WithArguments("D", "A.X.get"));
        }

        [WorkItem(545653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545653")]
        [Fact]
        public void Repro14242_Event()
        {
            var source = @"
abstract class A
{
    public abstract event System.Action E;
}
 
abstract class B : A
{
    public override event System.Action E;
}
 
abstract class C : B
{
    public new virtual event System.Func<int> E;
}
 
class D : C
{
    public override event System.Action E;
}
";
            var comp = CreateCompilation(source);
            var global = comp.GlobalNamespace;

            var eventA = global.GetMember<NamedTypeSymbol>("A").GetMember<EventSymbol>("E");
            var eventB = global.GetMember<NamedTypeSymbol>("B").GetMember<EventSymbol>("E");
            var eventC = global.GetMember<NamedTypeSymbol>("C").GetMember<EventSymbol>("E");
            var eventD = global.GetMember<NamedTypeSymbol>("D").GetMember<EventSymbol>("E");

            var ohmA = eventA.OverriddenOrHiddenMembers;
            var ohmB = eventB.OverriddenOrHiddenMembers;
            var ohmC = eventC.OverriddenOrHiddenMembers;
            var ohmD = eventD.OverriddenOrHiddenMembers;

            Assert.Equal(0, ohmA.OverriddenMembers.Length);
            Assert.Equal(0, ohmA.HiddenMembers.Length);

            Assert.Equal(eventA, ohmB.OverriddenMembers.Single());
            Assert.Equal(0, ohmB.HiddenMembers.Length);

            Assert.Equal(0, ohmC.OverriddenMembers.Length);
            Assert.Equal(eventB, ohmC.HiddenMembers.Single());

            Assert.Equal(eventC, ohmD.OverriddenMembers.Single());
            Assert.Equal(0, ohmD.HiddenMembers.Length);

            comp.VerifyDiagnostics(
                // (19,41): error CS1715: 'D.E': type must be 'Func<int>' to match overridden member 'C.E'
                //     public override event System.Action E;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "E").WithArguments("D.E", "C.E", "System.Func<int>").WithLocation(19, 41),
                // (19,41): warning CS0067: The event 'D.E' is never used
                //     public override event System.Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("D.E").WithLocation(19, 41),
                // (9,41): warning CS0067: The event 'B.E' is never used
                //     public override event System.Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("B.E").WithLocation(9, 41),
                // (14,47): warning CS0067: The event 'C.E' is never used
                //     public new virtual event System.Func<int> E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(14, 47));
        }

        [WorkItem(545653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545653")]
        [Fact]
        public void Repro14242_Method()
        {
            var source = @"
abstract class A
{
    public abstract int M();
}
 
abstract class B : A
{
    public override int M() { return 0; }
}
 
abstract class C : B
{
    public new virtual string M() { return null; }
}
 
class D : C
{
    public override int M() { return 0; }
}
";
            var comp = CreateCompilation(source);
            var global = comp.GlobalNamespace;

            var methodA = global.GetMember<NamedTypeSymbol>("A").GetMember<MethodSymbol>("M");
            var methodB = global.GetMember<NamedTypeSymbol>("B").GetMember<MethodSymbol>("M");
            var methodC = global.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>("M");
            var methodD = global.GetMember<NamedTypeSymbol>("D").GetMember<MethodSymbol>("M");

            var ohmA = methodA.OverriddenOrHiddenMembers;
            var ohmB = methodB.OverriddenOrHiddenMembers;
            var ohmC = methodC.OverriddenOrHiddenMembers;
            var ohmD = methodD.OverriddenOrHiddenMembers;

            Assert.Equal(0, ohmA.OverriddenMembers.Length);
            Assert.Equal(0, ohmA.HiddenMembers.Length);

            Assert.Equal(methodA, ohmB.OverriddenMembers.Single());
            Assert.Equal(0, ohmB.HiddenMembers.Length);

            Assert.Equal(0, ohmC.OverriddenMembers.Length);
            Assert.Equal(methodB, ohmC.HiddenMembers.Single());

            Assert.Equal(methodC, ohmD.OverriddenMembers.Single());
            Assert.Equal(0, ohmD.HiddenMembers.Length);

            comp.VerifyDiagnostics(
                // (19,25): error CS0508: 'D.M()': return type must be 'string' to match overridden member 'C.M()'
                //     public override int M() { return 0; }
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "M").WithArguments("D.M()", "C.M()", "string"));
        }

        [WorkItem(545653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545653")]
        [Fact]
        public void Repro14242_Indexer()
        {
            var source = @"
abstract class A
{
    public abstract int this[int x] { get; set; }
}
 
abstract class B : A
{
    public override int this[int x] { set { } }
}
 
abstract class C : B
{
    public new virtual string this[int x] {  get { return null; } set { } }
}
 
class D : C
{
    public override int this[int x]
    {
        get { return 0; }
    }
}
";
            var comp = CreateCompilation(source);
            var global = comp.GlobalNamespace;

            var indexerA = global.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>(WellKnownMemberNames.Indexer);
            var indexerB = global.GetMember<NamedTypeSymbol>("B").GetMember<PropertySymbol>(WellKnownMemberNames.Indexer);
            var indexerC = global.GetMember<NamedTypeSymbol>("C").GetMember<PropertySymbol>(WellKnownMemberNames.Indexer);
            var indexerD = global.GetMember<NamedTypeSymbol>("D").GetMember<PropertySymbol>(WellKnownMemberNames.Indexer);

            var ohmA = indexerA.OverriddenOrHiddenMembers;
            var ohmB = indexerB.OverriddenOrHiddenMembers;
            var ohmC = indexerC.OverriddenOrHiddenMembers;
            var ohmD = indexerD.OverriddenOrHiddenMembers;

            Assert.Equal(0, ohmA.OverriddenMembers.Length);
            Assert.Equal(0, ohmA.HiddenMembers.Length);

            Assert.Equal(indexerA, ohmB.OverriddenMembers.Single());
            Assert.Equal(0, ohmB.HiddenMembers.Length);

            Assert.Equal(0, ohmC.OverriddenMembers.Length);
            Assert.Equal(indexerB, ohmC.HiddenMembers.Single());

            Assert.Equal(indexerC, ohmD.OverriddenMembers.Single());
            Assert.Equal(0, ohmD.HiddenMembers.Length);

            comp.VerifyDiagnostics(
                // (19,25): error CS1715: 'D.this[int]': type must be 'string' to match overridden member 'C.this[int]'
                //     public override int this[int x]
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "this").WithArguments("D.this[int]", "C.this[int]", "string"),
                // (17,7): error CS0534: 'D' does not implement inherited abstract member 'A.this[int].get'
                // class D : C
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "D").WithArguments("D", "A.this[int].get"));
        }

        [ConditionalFact(typeof(DesktopOnly), typeof(ClrOnly))]
        [WorkItem(545658, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545658")]
        [WorkItem(18411, "https://github.com/dotnet/roslyn/issues/18411")]
        public void MethodConstructedFromOverrideWithCustomModifiers()
        {
            var il = @"
.class public auto ansi beforefieldinit G`1<T>
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class G`1

.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class C

.class public auto ansi beforefieldinit Base`1<T>
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance !T modopt([mscorlib]System.Runtime.CompilerServices.IsConst) 
          vMeth(!T modopt([mscorlib]System.Runtime.CompilerServices.IsConst) t) cil managed
  {
    ldstr      ""Base[{0}].vMeth({0})""
    ldtoken    !T
    call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    call       void [mscorlib]System.Console::WriteLine(string, object)
    ldarg.1
    ret
  }

  .method public hidebysig instance !T modopt([mscorlib]System.Runtime.CompilerServices.IsConst) 
          Meth(!T modopt([mscorlib]System.Runtime.CompilerServices.IsConst) t,
               int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) i) cil managed
  {
    ldstr      ""Base[{0}].Meth({0},{1})""
    ldtoken    !T
    call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    ldtoken    [mscorlib]System.Int32
    call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    call       void [mscorlib]System.Console::WriteLine(string, object, object)
    ldarg.1
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class Base`1

.class public auto ansi beforefieldinit SubT`1<T>
       extends class Base`1<!T>
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void class Base`1<!T>::.ctor()
    ret
  }

} // end of class SubT`1

.class public auto ansi beforefieldinit SubGT`1<T>
       extends class Base`1<class G`1<!T>>
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void class Base`1<class G`1<!T>>::.ctor()
    ret
  }

} // end of class SubGT`1

.class public auto ansi beforefieldinit SubC
       extends class Base`1<class C>
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void class Base`1<class C>::.ctor()
    ret
  }

} // end of class SubC
";

            var csharp = @"
using System;

public interface I<T>
{
	T Meth(T t, int i);
	T vMeth(T t);
}

public class SubSubT<T> : SubT<T> , I<T>
{
	public override T vMeth(T t) 
	{
		Console.WriteLine(""SubSubT[{0}].vMeth({0})"",typeof(T));
		return base.vMeth(t);
	}
}

public class SubSubGT<T> : SubGT<T> , I<G<T>>
{
	public override G<T> vMeth(G<T> t) 
	{
		Console.WriteLine(""SubSubGT[{0}].vMeth({1})"",typeof(T),typeof(G<T>));
		return base.vMeth(t);
	}
}

public class SubSubC : SubC , I<C>
{
	public override C vMeth(C t) 
	{ 
		Console.WriteLine(""SubSubC.vMeth(C)"");
		return base.vMeth(t);
	}
}

class Test
{
	public static int Main()
	{
		new SubSubT<int>().vMeth(1);
		new SubSubGT<int>().vMeth(new G<int>());
		new SubSubC().vMeth(new C());

		Console.WriteLine();
		
		new SubSubT<string>().Meth(""1"",1);
		new SubSubGT<string>().Meth(new G<string>(),1);
		new SubSubC().Meth(new C(),1);

		Console.WriteLine();
		
		((I<string>)new SubSubT<string>()).vMeth(""1"");
		((I<G<string>>)new SubSubGT<string>()).vMeth(new G<string>());
		((I<C>)new SubSubC()).vMeth(new C());

		Console.WriteLine();

		((I<int>)new SubSubT<int>()).Meth(1,1);
		((I<G<int>>)new SubSubGT<int>()).Meth(new G<int>(),1);
		((I<C>)new SubSubC()).Meth(new C(),1);
		
		return 0;
	}
}
";
            var ref1 = CompileIL(il);

            var comp = CreateCompilation(csharp, new[] { ref1 }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"
SubSubT[System.Int32].vMeth(System.Int32)
Base[System.Int32].vMeth(System.Int32)
SubSubGT[System.Int32].vMeth(G`1[System.Int32])
Base[G`1[System.Int32]].vMeth(G`1[System.Int32])
SubSubC.vMeth(C)
Base[C].vMeth(C)

Base[System.String].Meth(System.String,System.Int32)
Base[G`1[System.String]].Meth(G`1[System.String],System.Int32)
Base[C].Meth(C,System.Int32)

SubSubT[System.String].vMeth(System.String)
Base[System.String].vMeth(System.String)
SubSubGT[System.String].vMeth(G`1[System.String])
Base[G`1[System.String]].vMeth(G`1[System.String])
SubSubC.vMeth(C)
Base[C].vMeth(C)

Base[System.Int32].Meth(System.Int32,System.Int32)
Base[G`1[System.Int32]].Meth(G`1[System.Int32],System.Int32)
Base[C].Meth(C,System.Int32)");
        }

        [ClrOnlyFact]
        [WorkItem(546816, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546816")]
        public void Bug16887()
        {
            var text = @"
class Test
{
    ~Test()
    {
    }
}
";
            var compilation = CreateEmptyCompilation(text, new MetadataReference[] { MscorlibRef_v20 });

            var obj = compilation.GetSpecialType(SpecialType.System_Object);
            var finalize = (MethodSymbol)obj.GetMembers("Finalize").Single();

            Assert.False(finalize.IsVirtual);
            Assert.False(finalize.IsOverride);

            CompileAndVerify(compilation);
        }

        [Fact, WorkItem(546836, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546836")]
        public void OverriddenPropertyAccessibility1()
        {
            var source1 = @"
public class A
{
	protected internal virtual int P { get; set; }
}
";
            var source2 = @"
[assembly:System.Runtime.CompilerServices.InternalsVisibleTo(""C"")]
public class B : A
{
	protected override int P { get; set; }
}
";
            var source3 = @"
public class C : B
{
	protected override int P { get; set; }
}
";

            var comp1 = CreateCompilation(source1, assemblyName: "A.dll");
            var ref1 = comp1.EmitToImageReference();

            var comp2 = CreateCompilation(source2, new[] { ref1 }, assemblyName: "B.dll");
            var ref2 = comp2.EmitToImageReference();

            var comp3 = CreateCompilation(source3, new[] { ref1, ref2 }, assemblyName: "C.dll");
            comp3.VerifyDiagnostics();

            var properties = new[]
            {
                comp1.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>("P"),

                comp2.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>("P"),
                comp2.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMember<PropertySymbol>("P"),

                comp3.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>("P"),
                comp3.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMember<PropertySymbol>("P"),
                comp3.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<PropertySymbol>("P"),
            };

            AssertEx.All(properties, p =>
                p.DeclaredAccessibility == (p.ContainingType.Name == "A" ? Accessibility.ProtectedOrInternal : Accessibility.Protected));
        }

        [Fact, WorkItem(546836, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546836")]
        public void OverriddenPropertyAccessibility2()
        {
            var source1 = @"
public class A
{
	protected internal virtual int P { get; set; }
}
";
            var source2 = @"
[assembly:System.Runtime.CompilerServices.InternalsVisibleTo(""C"")]
public class B : A
{
	protected override int P { set { } }
}
";
            var source3 = @"
public class C : B
{
	protected override int P { get { return 0; } }
}
";

            var comp1 = CreateCompilation(source1, assemblyName: "A.dll");
            var ref1 = comp1.EmitToImageReference();

            var comp2 = CreateCompilation(source2, new[] { ref1 }, assemblyName: "B.dll");
            var ref2 = comp2.EmitToImageReference();

            var comp3 = CreateCompilation(source3, new[] { ref1, ref2 }, assemblyName: "C.dll");
            comp3.VerifyDiagnostics();

            var properties = new[]
            {
                comp1.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>("P"),

                comp2.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>("P"),
                comp2.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMember<PropertySymbol>("P"),

                comp3.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>("P"),
                comp3.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMember<PropertySymbol>("P"),
                comp3.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<PropertySymbol>("P"),
            };

            AssertEx.All(properties, p =>
                p.DeclaredAccessibility == (p.ContainingType.Name == "A" ? Accessibility.ProtectedOrInternal : Accessibility.Protected));
        }

        [Fact, WorkItem(546836, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546836")]
        public void OverriddenPropertyAccessibility3()
        {
            var source1 = @"
public class A
{
	public virtual int P { get; protected internal set; }
}
";
            var source2 = @"
[assembly:System.Runtime.CompilerServices.InternalsVisibleTo(""C"")]
public class B : A
{
	public override int P { protected set { } }
}
";
            var source3 = @"
public class C : B
{
	public override int P { get { return 0; } }
}
";

            var comp1 = CreateCompilation(source1, assemblyName: "A.dll");
            var ref1 = comp1.EmitToImageReference();

            var comp2 = CreateCompilation(source2, new[] { ref1 }, assemblyName: "B.dll");
            var ref2 = comp2.EmitToImageReference();

            var comp3 = CreateCompilation(source3, new[] { ref1, ref2 }, assemblyName: "C.dll");
            comp3.VerifyDiagnostics();

            var properties = new[]
            {
                comp1.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>("P"),

                comp2.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>("P"),
                comp2.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMember<PropertySymbol>("P"),

                comp3.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>("P"),
                comp3.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMember<PropertySymbol>("P"),
                comp3.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<PropertySymbol>("P"),
            };

            AssertEx.All(properties, p => p.DeclaredAccessibility == Accessibility.Public);
        }

        [Fact, WorkItem(546836, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546836")]
        public void OverriddenPropertyAccessibility4()
        {
            var source1 = @"
[assembly:System.Runtime.CompilerServices.InternalsVisibleTo(""B"")]
public class A
{
	public virtual int P { get; protected internal set; }
}
";
            var source2 = @"
public class B : A
{
	public override int P { protected internal set { } }
}
";
            var source3 = @"
public class C : B
{
	public override int P { get { return 0; } }
}
";

            var comp1 = CreateCompilation(source1, assemblyName: "A");
            var ref1 = comp1.EmitToImageReference();

            var comp2 = CreateCompilation(source2, new[] { ref1 }, assemblyName: "B");
            var ref2 = comp2.EmitToImageReference();

            var comp3 = CreateCompilation(source3, new[] { ref1, ref2 }, assemblyName: "C");
            comp3.VerifyDiagnostics();

            var properties = new[]
            {
                comp1.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>("P"),

                comp2.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>("P"),
                comp2.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMember<PropertySymbol>("P"),

                comp3.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>("P"),
                comp3.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMember<PropertySymbol>("P"),
                comp3.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<PropertySymbol>("P"),
            };

            AssertEx.All(properties, p => p.DeclaredAccessibility == Accessibility.Public);
        }

        [Fact, WorkItem(546836, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546836")]
        public void OverriddenPropertyAccessibility5()
        {
            var source1 = @"
public class A
{
	public virtual int P { get; protected set; }
}
";
            var source2 = @"
[assembly:System.Runtime.CompilerServices.InternalsVisibleTo(""C"")]
public class B : A
{
	public override int P { protected set { } }
}
";
            var source3 = @"
public class C : B
{
	public override int P { get { return 0; } }
}
";

            var comp1 = CreateCompilation(source1, assemblyName: "A.dll");
            var ref1 = comp1.EmitToImageReference();

            var comp2 = CreateCompilation(source2, new[] { ref1 }, assemblyName: "B.dll");
            var ref2 = comp2.EmitToImageReference();

            var comp3 = CreateCompilation(source3, new[] { ref1, ref2 }, assemblyName: "C.dll");
            comp3.VerifyDiagnostics();

            var properties = new[]
            {
                comp1.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>("P"),

                comp2.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>("P"),
                comp2.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMember<PropertySymbol>("P"),

                comp3.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>("P"),
                comp3.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMember<PropertySymbol>("P"),
                comp3.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<PropertySymbol>("P"),
            };

            AssertEx.All(properties, p => p.DeclaredAccessibility == Accessibility.Public);
        }

        [Fact, WorkItem(546836, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546836")]
        public void OverriddenPropertyAccessibility6()
        {
            var source1 = @"
[assembly:System.Runtime.CompilerServices.InternalsVisibleTo(""B"")]
public class A
{
	public virtual int P { get; protected set; }
}
";
            var source2 = @"
public class B : A
{
	public override int P { protected set { } }
}
";
            var source3 = @"
public class C : B
{
	public override int P { get { return 0; } }
}
";

            var comp1 = CreateCompilation(source1, assemblyName: "A.dll");
            var ref1 = comp1.EmitToImageReference();

            var comp2 = CreateCompilation(source2, new[] { ref1 }, assemblyName: "B.dll");
            var ref2 = comp2.EmitToImageReference();

            var comp3 = CreateCompilation(source3, new[] { ref1, ref2 }, assemblyName: "C.dll");
            comp3.VerifyDiagnostics();

            var properties = new[]
            {
                comp1.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>("P"),

                comp2.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>("P"),
                comp2.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMember<PropertySymbol>("P"),

                comp3.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>("P"),
                comp3.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMember<PropertySymbol>("P"),
                comp3.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<PropertySymbol>("P"),
            };

            AssertEx.All(properties, p => p.DeclaredAccessibility == Accessibility.Public);
        }

        [Fact, WorkItem(546836, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546836")]
        public void OverriddenPropertyAccessibility7()
        {
            var source1 = @"
[assembly:System.Runtime.CompilerServices.InternalsVisibleTo(""B"")]
public class A
{
	protected internal virtual int P { get; set; }
}
";
            var source2 = @"
public class B : A
{
	protected internal override int P { set { } }
}
";
            // If this was C#, we would have to change the accessibility from
            // "protected internal" to "protected", so we'll do this in IL.
            var source3 = @"
.assembly extern mscorlib { }
.assembly extern B { }
.assembly C { }

.class public auto ansi beforefieldinit C
       extends [B]B
{
  .method famorassem hidebysig specialname virtual 
          instance int32  get_P() cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [B]B::.ctor()
    ret
  } // end of method C::.ctor

  .property instance int32 P()
  {
    .get instance int32 C::get_P()
  } // end of property C::P
} // end of class C
";

            var comp1 = CreateCompilation(source1, assemblyName: "A");
            var ref1 = comp1.EmitToImageReference();

            var comp2 = CreateCompilation(source2, new[] { ref1 }, assemblyName: "B");
            var ref2 = comp2.EmitToImageReference();

            var ilRef = CompileIL(source3, prependDefaultHeader: false);

            var comp3 = CreateCompilation("", new[] { ref1, ref2, ilRef }, assemblyName: "Test");
            comp3.VerifyDiagnostics();

            var properties = new[]
            {
                comp1.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>("P"),

                comp2.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>("P"),
                comp2.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMember<PropertySymbol>("P"),

                comp3.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>("P"),
                comp3.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMember<PropertySymbol>("P"),
                comp3.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<PropertySymbol>("P"),
            };

            AssertEx.All(properties, p => p.DeclaredAccessibility == Accessibility.ProtectedOrInternal);
        }

        [Fact, WorkItem(546836, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546836")]
        public void OverriddenEventAccessibility()
        {
            var source1 = @"
public class A
{
	protected internal virtual event System.Action E;

    void UseEvent() { E(); }
}
";
            var source2 = @"
[assembly:System.Runtime.CompilerServices.InternalsVisibleTo(""C"")]
public class B : A
{
	protected override event System.Action E;

    void UseEvent() { E(); }
}
";
            var source3 = @"
public class C : B
{
	protected override event System.Action E;

    void UseEvent() { E(); }
}
";

            var comp1 = CreateCompilation(source1, assemblyName: "A.dll");
            var ref1 = comp1.EmitToImageReference();

            var comp2 = CreateCompilation(source2, new[] { ref1 }, assemblyName: "B.dll");
            var ref2 = comp2.EmitToImageReference();

            var comp3 = CreateCompilation(source3, new[] { ref1, ref2 }, assemblyName: "C.dll");
            comp3.VerifyDiagnostics();

            var events = new[]
            {
                comp1.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<EventSymbol>("E"),

                comp2.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<EventSymbol>("E"),
                comp2.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMember<EventSymbol>("E"),

                comp3.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<EventSymbol>("E"),
                comp3.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMember<EventSymbol>("E"),
                comp3.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<EventSymbol>("E"),
            };

            AssertEx.All(events, e =>
                e.DeclaredAccessibility == (e.ContainingType.Name == "A" ? Accessibility.ProtectedOrInternal : Accessibility.Protected));
        }

        [Fact, WorkItem(546836, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546836")]
        public void HideDestructorOperatorConversion1()
        {
            var source = @"
public class B
{
    public static int operator +(B b)
    {
        return 0;
    }

    public static explicit operator int(B b)
    {
        return 0;
    }

    ~B()
    {
    }
}

public class D1 : B
{
    public class op_UnaryPlus { }
    public class op_Explicit { }
    public class Finalize { }
}

public class D2 : B
{
    public int op_UnaryPlus { get; set; }
    public int op_Explicit { get; set; }
    public int Finalize { get; set; }
}

public class D3 : B
{
    public int op_UnaryPlus = 1; //CS0108
    public int op_Explicit = 1; //CS0108
    public int Finalize = 1;
}

public class D4 : B
{
    public event System.Action op_UnaryPlus; //CS0108
    public event System.Action op_Explicit; //CS0108
    public event System.Action Finalize;
}

public class D5 : B
{
    public event System.Action op_UnaryPlus { add { } remove { } } //CS0108
    public event System.Action op_Explicit { add { } remove { } } //CS0108
    public event System.Action Finalize { add { } remove { } }
}

public class D6 : B
{
    public delegate void op_UnaryPlus();
    public delegate void op_Explicit();
    public delegate void Finalize();
}

public class D7 : B
{
    public void op_UnaryPlus(B b) { }
    public void op_Explicit(B b) { }
    public void Finalize() { }
}

public class D8 : B
{
    public static int op_UnaryPlus(B b) { return 0; }
    public static int op_Explicit(B b) { return 0; }
    public static void Finalize() { }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (36,16): warning CS0108: 'D3.op_Explicit' hides inherited member 'B.explicit operator int(B)'. Use the new keyword if hiding was intended.
                //     public int op_Explicit = 1;//
                Diagnostic(ErrorCode.WRN_NewRequired, "op_Explicit").WithArguments("D3.op_Explicit", "B.explicit operator int(B)"),
                // (35,16): warning CS0108: 'D3.op_UnaryPlus' hides inherited member 'B.operator +(B)'. Use the new keyword if hiding was intended.
                //     public int op_UnaryPlus = 1;//
                Diagnostic(ErrorCode.WRN_NewRequired, "op_UnaryPlus").WithArguments("D3.op_UnaryPlus", "B.operator +(B)"),
                // (43,32): warning CS0108: 'D4.op_Explicit' hides inherited member 'B.explicit operator int(B)'. Use the new keyword if hiding was intended.
                //     public event System.Action op_Explicit;//
                Diagnostic(ErrorCode.WRN_NewRequired, "op_Explicit").WithArguments("D4.op_Explicit", "B.explicit operator int(B)"),
                // (42,32): warning CS0108: 'D4.op_UnaryPlus' hides inherited member 'B.operator +(B)'. Use the new keyword if hiding was intended.
                //     public event System.Action op_UnaryPlus;//
                Diagnostic(ErrorCode.WRN_NewRequired, "op_UnaryPlus").WithArguments("D4.op_UnaryPlus", "B.operator +(B)"),
                // (50,32): warning CS0108: 'D5.op_Explicit' hides inherited member 'B.explicit operator int(B)'. Use the new keyword if hiding was intended.
                //     public event System.Action op_Explicit { add { } remove { } }//
                Diagnostic(ErrorCode.WRN_NewRequired, "op_Explicit").WithArguments("D5.op_Explicit", "B.explicit operator int(B)"),
                // (49,32): warning CS0108: 'D5.op_UnaryPlus' hides inherited member 'B.operator +(B)'. Use the new keyword if hiding was intended.
                //     public event System.Action op_UnaryPlus { add { } remove { } }//
                Diagnostic(ErrorCode.WRN_NewRequired, "op_UnaryPlus").WithArguments("D5.op_UnaryPlus", "B.operator +(B)"),

                // (43,32): warning CS0067: The event 'D4.op_Explicit' is never used
                //     public event System.Action op_Explicit;//
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "op_Explicit").WithArguments("D4.op_Explicit"),
                // (42,32): warning CS0067: The event 'D4.op_UnaryPlus' is never used
                //     public event System.Action op_UnaryPlus;//
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "op_UnaryPlus").WithArguments("D4.op_UnaryPlus"),
                // (44,32): warning CS0067: The event 'D4.Finalize' is never used
                //     public event System.Action Finalize;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Finalize").WithArguments("D4.Finalize"),

                // (72,24): warning CS0465: Introducing a 'Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?
                //     public static void Finalize() { }
                Diagnostic(ErrorCode.WRN_FinalizeMethod, "Finalize"),
                // (65,17): warning CS0465: Introducing a 'Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?
                //     public void Finalize() { }
                Diagnostic(ErrorCode.WRN_FinalizeMethod, "Finalize"));
        }

        [Fact, WorkItem(546836, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546836")]
        public void HideDestructorOperatorConversion2()
        {
            var source = @"
public class B
{
    public static int operator +(B b)
    {
        return 0;
    }

    public static explicit operator int(B b)
    {
        return 0;
    }

    ~B()
    {
    }
}

public class D1 : B
{
    new public class op_UnaryPlus { } //CS0109
    new public class op_Explicit { } //CS0109
    new public class Finalize { } //CS0109
}

public class D2 : B
{
    new public int op_UnaryPlus { get; set; } //CS0109
    new public int op_Explicit { get; set; } //CS0109
    new public int Finalize { get; set; } //CS0109
}

public class D3 : B
{
    new public int op_UnaryPlus = 1;
    new public int op_Explicit = 1;
    new public int Finalize = 1; //CS0109
}

public class D4 : B
{
    new public event System.Action op_UnaryPlus;
    new public event System.Action op_Explicit;
    new public event System.Action Finalize; //CS0109
}

public class D5 : B
{
    new public event System.Action op_UnaryPlus { add { } remove { } }
    new public event System.Action op_Explicit { add { } remove { } }
    new public event System.Action Finalize { add { } remove { } } //CS0109
}

public class D6 : B
{
    new public delegate void op_UnaryPlus(); //CS0109
    new public delegate void op_Explicit(); //CS0109
    new public delegate void Finalize(); //CS0109
}

public class D7 : B
{
    new public void op_UnaryPlus(B b) { } //CS0109
    new public void op_Explicit(B b) { } //CS0109
    new public void Finalize() { } //CS0109
}

public class D8 : B
{
    new public static int op_UnaryPlus(B b) { return 0; } //CS0109
    new public static int op_Explicit(B b) { return 0; } //CS0109
    new public static void Finalize() { } //CS0109
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (23,22): warning CS0109: The member 'D1.Finalize' does not hide an accessible member. The new keyword is not required.
                //     new public class Finalize { } //CS0109
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Finalize").WithArguments("D1.Finalize"),
                // (21,22): warning CS0109: The member 'D1.op_UnaryPlus' does not hide an accessible member. The new keyword is not required.
                //     new public class op_UnaryPlus { } //CS0109
                Diagnostic(ErrorCode.WRN_NewNotRequired, "op_UnaryPlus").WithArguments("D1.op_UnaryPlus"),
                // (22,22): warning CS0109: The member 'D1.op_Explicit' does not hide an accessible member. The new keyword is not required.
                //     new public class op_Explicit { } //CS0109
                Diagnostic(ErrorCode.WRN_NewNotRequired, "op_Explicit").WithArguments("D1.op_Explicit"),
                // (58,30): warning CS0109: The member 'D6.Finalize' does not hide an accessible member. The new keyword is not required.
                //     new public delegate void Finalize(); //CS0109
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Finalize").WithArguments("D6.Finalize"),
                // (56,30): warning CS0109: The member 'D6.op_UnaryPlus' does not hide an accessible member. The new keyword is not required.
                //     new public delegate void op_UnaryPlus(); //CS0109
                Diagnostic(ErrorCode.WRN_NewNotRequired, "op_UnaryPlus").WithArguments("D6.op_UnaryPlus"),
                // (57,30): warning CS0109: The member 'D6.op_Explicit' does not hide an accessible member. The new keyword is not required.
                //     new public delegate void op_Explicit(); //CS0109
                Diagnostic(ErrorCode.WRN_NewNotRequired, "op_Explicit").WithArguments("D6.op_Explicit"),
                // (37,20): warning CS0109: The member 'D3.Finalize' does not hide an accessible member. The new keyword is not required.
                //     new public int Finalize = 1; //CS0109
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Finalize").WithArguments("D3.Finalize"),
                // (51,36): warning CS0109: The member 'D5.Finalize' does not hide an accessible member. The new keyword is not required.
                //     new public event System.Action Finalize { add { } remove { } } //CS0109
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Finalize").WithArguments("D5.Finalize"),
                // (71,27): warning CS0109: The member 'D8.op_Explicit(B)' does not hide an accessible member. The new keyword is not required.
                //     new public static int op_Explicit(B b) { return 0; } //CS0109
                Diagnostic(ErrorCode.WRN_NewNotRequired, "op_Explicit").WithArguments("D8.op_Explicit(B)"),
                // (72,28): warning CS0109: The member 'D8.Finalize()' does not hide an accessible member. The new keyword is not required.
                //     new public static void Finalize() { } //CS0109
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Finalize").WithArguments("D8.Finalize()"),
                // (70,27): warning CS0109: The member 'D8.op_UnaryPlus(B)' does not hide an accessible member. The new keyword is not required.
                //     new public static int op_UnaryPlus(B b) { return 0; } //CS0109
                Diagnostic(ErrorCode.WRN_NewNotRequired, "op_UnaryPlus").WithArguments("D8.op_UnaryPlus(B)"),
                // (44,36): warning CS0109: The member 'D4.Finalize' does not hide an accessible member. The new keyword is not required.
                //     new public event System.Action Finalize; //CS0109
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Finalize").WithArguments("D4.Finalize"),
                // (64,21): warning CS0109: The member 'D7.op_Explicit(B)' does not hide an accessible member. The new keyword is not required.
                //     new public void op_Explicit(B b) { } //CS0109
                Diagnostic(ErrorCode.WRN_NewNotRequired, "op_Explicit").WithArguments("D7.op_Explicit(B)"),
                // (65,21): warning CS0109: The member 'D7.Finalize()' does not hide an accessible member. The new keyword is not required.
                //     new public void Finalize() { } //CS0109
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Finalize").WithArguments("D7.Finalize()"),
                // (63,21): warning CS0109: The member 'D7.op_UnaryPlus(B)' does not hide an accessible member. The new keyword is not required.
                //     new public void op_UnaryPlus(B b) { } //CS0109
                Diagnostic(ErrorCode.WRN_NewNotRequired, "op_UnaryPlus").WithArguments("D7.op_UnaryPlus(B)"),
                // (29,20): warning CS0109: The member 'D2.op_Explicit' does not hide an accessible member. The new keyword is not required.
                //     new public int op_Explicit { get; set; } //CS0109
                Diagnostic(ErrorCode.WRN_NewNotRequired, "op_Explicit").WithArguments("D2.op_Explicit"),
                // (30,20): warning CS0109: The member 'D2.Finalize' does not hide an accessible member. The new keyword is not required.
                //     new public int Finalize { get; set; } //CS0109
                Diagnostic(ErrorCode.WRN_NewNotRequired, "Finalize").WithArguments("D2.Finalize"),
                // (28,20): warning CS0109: The member 'D2.op_UnaryPlus' does not hide an accessible member. The new keyword is not required.
                //     new public int op_UnaryPlus { get; set; } //CS0109
                Diagnostic(ErrorCode.WRN_NewNotRequired, "op_UnaryPlus").WithArguments("D2.op_UnaryPlus"),

                // (44,36): warning CS0067: The event 'D4.Finalize' is never used
                //     new public event System.Action Finalize; //CS0109
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Finalize").WithArguments("D4.Finalize"),
                // (42,36): warning CS0067: The event 'D4.op_UnaryPlus' is never used
                //     new public event System.Action op_UnaryPlus;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "op_UnaryPlus").WithArguments("D4.op_UnaryPlus"),
                // (43,36): warning CS0067: The event 'D4.op_Explicit' is never used
                //     new public event System.Action op_Explicit;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "op_Explicit").WithArguments("D4.op_Explicit"),

                // (72,28): warning CS0465: Introducing a 'Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?
                //     new public static void Finalize() { } //CS0109
                Diagnostic(ErrorCode.WRN_FinalizeMethod, "Finalize"),
                // (65,21): warning CS0465: Introducing a 'Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?
                //     new public void Finalize() { } //CS0109
                Diagnostic(ErrorCode.WRN_FinalizeMethod, "Finalize"));
        }

        [Fact]
        [WorkItem(661370, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/661370")]
        public void HideAndOverride1()
        {
            var source = @"
public class Base
{
    public virtual void M() { }
    public virtual int M { get; set; } // NOTE: illegal, since there's already a method M.
}

public class Derived1 : Base
{
    public override void M() { }
}

public class Derived2 : Base
{
    public override int M { get; set; }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,16): error CS0102: The type 'Base' already contains a definition for 'M'
                //     public int M; // NOTE: illegal, since there's already a method M.
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "M").WithArguments("Base", "M"));

            var global = comp.GlobalNamespace;

            var baseClass = global.GetMember<NamedTypeSymbol>("Base");
            var baseMethod = baseClass.GetMembers("M").OfType<MethodSymbol>().Single();
            var baseProperty = baseClass.GetMembers("M").OfType<PropertySymbol>().Single();

            var derivedClass1 = global.GetMember<NamedTypeSymbol>("Derived1");
            var derivedMethod = derivedClass1.GetMember<MethodSymbol>("M");

            var overriddenOrHidden1 = derivedMethod.OverriddenOrHiddenMembers;
            Assert.Equal(baseMethod, overriddenOrHidden1.OverriddenMembers.Single());
            Assert.Equal(baseProperty, overriddenOrHidden1.HiddenMembers.Single());

            var derivedClass2 = global.GetMember<NamedTypeSymbol>("Derived2");
            var derivedProperty = derivedClass2.GetMember<PropertySymbol>("M");

            var overriddenOrHidden2 = derivedProperty.OverriddenOrHiddenMembers;
            Assert.Equal(baseProperty, overriddenOrHidden2.OverriddenMembers.Single());
            Assert.Equal(baseMethod, overriddenOrHidden2.HiddenMembers.Single());
        }

        [Fact]
        [WorkItem(743241, "DevDiv")]
        public void OverrideNonObjectEquals()
        {
            var source = @"
class Base
{
    public new virtual bool Equals(object obj)
    {
        return base.Equals(obj);
    }
}

class Derived : Base
{
    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }
}";

            // Dev11 spuriously reports WRN_EqualsWithoutGetHashCode.
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(6148, "https://github.com/dotnet/roslyn/issues/6148")]
        public void AbstractGenericBase_01()
        {
            var text = @"
class C
{
    public static void Main()
    {
        var t = new Required();
        t.Test1(null);
        t.Test2(null);
    }
}

public abstract class Validator
{
    public abstract void DoValidate(object objectToValidate);

    public void Test1(object objectToValidate)
    {
         DoValidate(objectToValidate);
    }
}

public abstract class Validator<T> : Validator
{
    public override void DoValidate(object objectToValidate)
    {
        System.Console.WriteLine(""void Validator<T>.DoValidate(object objectToValidate)"");
    }

    protected abstract void DoValidate(T objectToValidate);

    public void Test2(T objectToValidate)
    {
         DoValidate(objectToValidate);
    }
}

public abstract class ValidatorBase<T> : Validator<T>
{
    protected override void DoValidate(T objectToValidate)
    {
        System.Console.WriteLine(""void ValidatorBase<T>.DoValidate(T objectToValidate)"");
    }
}

public class Required : ValidatorBase<object>
{
}
";
            var compilation = CreateCompilation(text, options: TestOptions.ReleaseExe);

            var validatorBaseT = compilation.GetTypeByMetadataName("ValidatorBase`1");
            var doValidateT = validatorBaseT.GetMember<MethodSymbol>("DoValidate");

            Assert.Equal(1, doValidateT.OverriddenOrHiddenMembers.OverriddenMembers.Length);
            Assert.Equal("void Validator<T>.DoValidate(T objectToValidate)", doValidateT.OverriddenMethod.ToTestDisplayString());
            Assert.False(validatorBaseT.AbstractMembers.Any());

            var validatorBaseObject = validatorBaseT.Construct(compilation.ObjectType);
            var doValidateObject = validatorBaseObject.GetMember<MethodSymbol>("DoValidate");

            Assert.Equal(2, doValidateObject.OverriddenOrHiddenMembers.OverriddenMembers.Length);
            Assert.Equal("void Validator<T>.DoValidate(T objectToValidate)", doValidateObject.OverriddenMethod.OriginalDefinition.ToTestDisplayString());
            Assert.False(validatorBaseObject.AbstractMembers.Any());

            CompileAndVerify(compilation, expectedOutput: @"void Validator<T>.DoValidate(object objectToValidate)
void ValidatorBase<T>.DoValidate(T objectToValidate)");
        }

        [Fact]
        [WorkItem(6148, "https://github.com/dotnet/roslyn/issues/6148")]
        public void AbstractGenericBase_02()
        {
            var text = @"
class C
{
    public static void Main()
    {
        var t = new Required();
        t.Test1(null);
        t.Test2(null);
    }
}

public abstract class Validator
{
    public abstract void DoValidate(object objectToValidate);

    public void Test1(object objectToValidate)
    {
         DoValidate(objectToValidate);
    }
}

public abstract class Validator<T> : Validator
{
    public abstract override void DoValidate(object objectToValidate);

    public virtual void DoValidate(T objectToValidate)
    {
        System.Console.WriteLine(""void Validator<T>.DoValidate(T objectToValidate)"");
    }

    public void Test2(T objectToValidate)
    {
         DoValidate(objectToValidate);
    }
}


public abstract class ValidatorBase<T> : Validator<T>
{
    public override void DoValidate(T objectToValidate)
    {
        System.Console.WriteLine(""void ValidatorBase<T>.DoValidate(T objectToValidate)"");
    }
}

public class Required : ValidatorBase<object>
{
}";
            var compilation = CreateCompilation(text, options: TestOptions.ReleaseExe);

            compilation.VerifyDiagnostics(
        // (46,14): error CS0534: 'Required' does not implement inherited abstract member 'Validator<object>.DoValidate(object)'
        // public class Required : ValidatorBase<object>
        Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Required").WithArguments("Required", "Validator<object>.DoValidate(object)").WithLocation(46, 14)
                );
        }

        [Fact]
        [WorkItem(6148, "https://github.com/dotnet/roslyn/issues/6148")]
        public void AbstractGenericBase_03()
        {
            var text = @"
class C
{
    public static void Main()
    {
        var t = new Required();
        t.Test1(null);
        t.Test2(null);
    }
}

public abstract class Validator0<T>
{
    public abstract void DoValidate(T objectToValidate);

    public void Test2(T objectToValidate)
    {
         DoValidate(objectToValidate);
    }
}

public abstract class Validator<T> : Validator0<T>
{
    public virtual void DoValidate(object objectToValidate)
    {
        System.Console.WriteLine(""void Validator<T>.DoValidate(object objectToValidate)"");
    }

    public void Test1(object objectToValidate)
    {
         DoValidate(objectToValidate);
    }
}


public abstract class ValidatorBase<T> : Validator<T>
{
    public override void DoValidate(T objectToValidate)
    {
        System.Console.WriteLine(""void ValidatorBase<T>.DoValidate(T objectToValidate)"");
    }
}

public class Required : ValidatorBase<object>
{
}
";
            var compilation = CreateCompilation(text, options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: @"void Validator<T>.DoValidate(object objectToValidate)
void ValidatorBase<T>.DoValidate(T objectToValidate)");
        }

        [Fact]
        [WorkItem(6148, "https://github.com/dotnet/roslyn/issues/6148")]
        public void AbstractGenericBase_04()
        {
            var text = @"
class C
{
    public static void Main()
    {
        var t = new Required();
        Test1(t);
        Test2(t, null);
    }

    static void Test1(Validator v)
    {
        v.Test1(null);
    }

    static void Test2<T>(Validator<T> v, T o)
    {
        v.Test2(o);
    }
}

public abstract class Validator
{
    public abstract void DoValidate(object objectToValidate);

    public void Test1(object objectToValidate)
    {
         DoValidate(objectToValidate);
    }
}

public abstract class Validator<T> : Validator
{
    public virtual void DoValidate(T objectToValidate)
    {
        System.Console.WriteLine(""void Validator<T>.DoValidate(T objectToValidate)"");
    }

    public void Test2(T objectToValidate)
    {
         DoValidate(objectToValidate);
    }
}

public abstract class ValidatorBase<T> : Validator<T>
{
    public override void DoValidate(object objectToValidate)
    {
        System.Console.WriteLine(""void ValidatorBase<T>.DoValidate(object objectToValidate)"");
    }
}

public class Required : ValidatorBase<object>
{
}
";
            var compilation = CreateCompilation(text, options: TestOptions.ReleaseExe);

            CompileAndVerify(compilation, expectedOutput: @"void ValidatorBase<T>.DoValidate(object objectToValidate)
void Validator<T>.DoValidate(T objectToValidate)");
        }

        [Fact]
        [WorkItem(6148, "https://github.com/dotnet/roslyn/issues/6148")]
        public void AbstractGenericBase_05()
        {
            var text = @"
class C
{
    public static void Main()
    {
        var t = new Required();
        t.Test1(null);
        t.Test2(null);
    }
}

public abstract class Validator0<T>
{
    public abstract void DoValidate(object objectToValidate);

    public void Test1(object objectToValidate)
    {
         DoValidate(objectToValidate);
    }
}

public abstract class Validator<T> : Validator0<int>
{
    public override void DoValidate(object objectToValidate)
    {
        System.Console.WriteLine(""void Validator<T>.DoValidate(object objectToValidate)"");
    }

    protected abstract void DoValidate(T objectToValidate);

    public void Test2(T objectToValidate)
    {
         DoValidate(objectToValidate);
    }
}

public abstract class ValidatorBase<T> : Validator<T>
{
    protected override void DoValidate(T objectToValidate)
    {
        System.Console.WriteLine(""void ValidatorBase<T>.DoValidate(T objectToValidate)"");
    }
}

public class Required : ValidatorBase<object>
{
}
";
            var compilation = CreateCompilation(text, options: TestOptions.ReleaseExe);

            var validatorBaseT = compilation.GetTypeByMetadataName("ValidatorBase`1");
            var doValidateT = validatorBaseT.GetMember<MethodSymbol>("DoValidate");

            Assert.Equal(1, doValidateT.OverriddenOrHiddenMembers.OverriddenMembers.Length);
            Assert.Equal("void Validator<T>.DoValidate(T objectToValidate)", doValidateT.OverriddenMethod.ToTestDisplayString());
            Assert.False(validatorBaseT.AbstractMembers.Any());

            var validatorBaseObject = validatorBaseT.Construct(compilation.ObjectType);
            var doValidateObject = validatorBaseObject.GetMember<MethodSymbol>("DoValidate");

            Assert.Equal(2, doValidateObject.OverriddenOrHiddenMembers.OverriddenMembers.Length);
            Assert.Equal("void Validator<T>.DoValidate(T objectToValidate)", doValidateObject.OverriddenMethod.OriginalDefinition.ToTestDisplayString());
            Assert.False(validatorBaseObject.AbstractMembers.Any());

            CompileAndVerify(compilation, expectedOutput: @"void Validator<T>.DoValidate(object objectToValidate)
void ValidatorBase<T>.DoValidate(T objectToValidate)");
        }

        #endregion

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void HidingMethodWithInParameter()
        {
            var code = @"
class A
{
    public void M(in int x) { }
}
class B : A
{
    public void M(in int x) { }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (8,17): warning CS0108: 'B.M(in int)' hides inherited member 'A.M(in int)'. Use the new keyword if hiding was intended.
                //     public void M(in int x) { }
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("B.M(in int)", "A.M(in int)").WithLocation(8, 17));

            var aMethod = comp.GetMember<MethodSymbol>("A.M");
            var bMethod = comp.GetMember<MethodSymbol>("B.M");

            Assert.Empty(aMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aMethod.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aMethod, bMethod.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void HidingMethodWithRefReadOnlyReturnType_RefReadOnly_RefReadOnly()
        {
            var code = @"
class A
{
    protected int x = 0;
    public ref readonly int M() { return ref x; }
}
class B : A
{
    public ref readonly int M() { return ref x; }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (9,29): warning CS0108: 'B.M()' hides inherited member 'A.M()'. Use the new keyword if hiding was intended.
                //     public ref readonly int M() { return ref x; }
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("B.M()", "A.M()").WithLocation(9, 29));

            var aMethod = comp.GetMember<MethodSymbol>("A.M");
            var bMethod = comp.GetMember<MethodSymbol>("B.M");

            Assert.Empty(aMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aMethod.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aMethod, bMethod.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void HidingMethodWithRefReadOnlyReturnType_Ref_RefReadOnly()
        {
            var code = @"
class A
{
    protected int x = 0;
    public ref int M() { return ref x; }
}
class B : A
{
    public ref readonly int M() { return ref x; }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (9,29): warning CS0108: 'B.M()' hides inherited member 'A.M()'. Use the new keyword if hiding was intended.
                //     public ref readonly int M() { return ref x; }
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("B.M()", "A.M()").WithLocation(9, 29));

            var aMethod = comp.GetMember<MethodSymbol>("A.M");
            var bMethod = comp.GetMember<MethodSymbol>("B.M");

            Assert.Empty(aMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aMethod.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aMethod, bMethod.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void HidingMethodWithRefReadOnlyReturnType_RefReadOnly_Ref()
        {
            var code = @"
class A
{
    protected int x = 0;
    public ref readonly int M() { return ref x; }
}
class B : A
{
    public ref int M() { return ref x; }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (9,20): warning CS0108: 'B.M()' hides inherited member 'A.M()'. Use the new keyword if hiding was intended.
                //     public ref int M() { return ref x; }
                Diagnostic(ErrorCode.WRN_NewRequired, "M").WithArguments("B.M()", "A.M()").WithLocation(9, 20));

            var aMethod = comp.GetMember<MethodSymbol>("A.M");
            var bMethod = comp.GetMember<MethodSymbol>("B.M");

            Assert.Empty(aMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aMethod.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aMethod, bMethod.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void HidingPropertyWithRefReadOnlyReturnType_RefReadOnly_RefReadOnly()
        {
            var code = @"
class A
{
    protected int x = 0;
    public ref readonly int Property { get { return ref x; } }
}
class B : A
{
    public ref readonly int Property { get { return ref x; } }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (9,29): warning CS0108: 'B.Property' hides inherited member 'A.Property'. Use the new keyword if hiding was intended.
                //     public ref readonly int Property { get { return ref x; } }
                Diagnostic(ErrorCode.WRN_NewRequired, "Property").WithArguments("B.Property", "A.Property").WithLocation(9, 29));

            var aProperty = comp.GetMember<PropertySymbol>("A.Property");
            var bProperty = comp.GetMember<PropertySymbol>("B.Property");

            Assert.Empty(aProperty.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aProperty.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bProperty.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aProperty, bProperty.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void HidingPropertyWithRefReadOnlyReturnType_RefReadOnly_Ref()
        {
            var code = @"
class A
{
    protected int x = 0;
    public ref readonly int Property { get { return ref x; } }
}
class B : A
{
    public ref int Property { get { return ref x; } }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (9,20): warning CS0108: 'B.Property' hides inherited member 'A.Property'. Use the new keyword if hiding was intended.
                //     public ref int Property { get { return ref x; } }
                Diagnostic(ErrorCode.WRN_NewRequired, "Property").WithArguments("B.Property", "A.Property").WithLocation(9, 20));

            var aProperty = comp.GetMember<PropertySymbol>("A.Property");
            var bProperty = comp.GetMember<PropertySymbol>("B.Property");

            Assert.Empty(aProperty.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aProperty.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bProperty.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aProperty, bProperty.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void HidingPropertyWithRefReadOnlyReturnType_Ref_RefReadOnly()
        {
            var code = @"
class A
{
    protected int x = 0;
    public ref int Property { get { return ref x; } }
}
class B : A
{
    public ref readonly int Property { get { return ref x; } }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (9,29): warning CS0108: 'B.Property' hides inherited member 'A.Property'. Use the new keyword if hiding was intended.
                //     public ref readonly int Property { get { return ref x; } }
                Diagnostic(ErrorCode.WRN_NewRequired, "Property").WithArguments("B.Property", "A.Property").WithLocation(9, 29));

            var aProperty = comp.GetMember<PropertySymbol>("A.Property");
            var bProperty = comp.GetMember<PropertySymbol>("B.Property");

            Assert.Empty(aProperty.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aProperty.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bProperty.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aProperty, bProperty.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void HidingMethodWithInParameterAndNewKeyword()
        {
            var code = @"
class A
{
    public void M(in int x) { }
}
class B : A
{
    public new void M(in int x) { }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics();

            var aMethod = comp.GetMember<MethodSymbol>("A.M");
            var bMethod = comp.GetMember<MethodSymbol>("B.M");

            Assert.Empty(aMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aMethod.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aMethod, bMethod.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void HidingMethodWithRefReadOnlyReturnTypeAndNewKeyword()
        {
            var code = @"
class A
{
    protected int x = 0;
    public ref readonly int M() { return ref x; }
}
class B : A
{
    public new ref readonly int M() { return ref x; }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics();

            var aMethod = comp.GetMember<MethodSymbol>("A.M");
            var bMethod = comp.GetMember<MethodSymbol>("B.M");

            Assert.Empty(aMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aMethod.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aMethod, bMethod.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void HidingPropertyWithRefReadOnlyReturnTypeAndNewKeyword()
        {
            var code = @"
class A
{
    protected int x = 0;
    public ref readonly int Property { get { return ref x; } }
}
class B : A
{
    public new ref readonly int Property { get { return ref x; } }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics();

            var aProperty = comp.GetMember<PropertySymbol>("A.Property");
            var bProperty = comp.GetMember<PropertySymbol>("B.Property");

            Assert.Empty(aProperty.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aProperty.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bProperty.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Equal(aProperty, bProperty.OverriddenOrHiddenMembers.HiddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void OverridingMethodWithInParameter()
        {
            var code = @"
class A
{
    public virtual void M(in int x) { }
}
class B : A
{
    public override void M(in int x) { }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics();

            var aMethod = comp.GetMember<MethodSymbol>("A.M");
            var bMethod = comp.GetMember<MethodSymbol>("B.M");

            Assert.Empty(aMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aMethod.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Equal(aMethod, bMethod.OverriddenOrHiddenMembers.OverriddenMembers.Single());
            Assert.Empty(bMethod.OverriddenOrHiddenMembers.HiddenMembers);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void OverridingMethodWithRefReadOnlyReturnType()
        {
            var code = @"
class A
{
    protected int x = 0;
    public virtual ref readonly int M() { return ref x; }
}
class B : A
{
    public override ref readonly int M() { return ref x; }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics();

            var aMethod = comp.GetMember<MethodSymbol>("A.M");
            var bMethod = comp.GetMember<MethodSymbol>("B.M");

            Assert.Empty(aMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aMethod.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Equal(aMethod, bMethod.OverriddenOrHiddenMembers.OverriddenMembers.Single());
            Assert.Empty(bMethod.OverriddenOrHiddenMembers.HiddenMembers);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void OverridingPropertyWithRefReadOnlyReturnType()
        {
            var code = @"
class A
{
    protected int x = 0;
    public virtual ref readonly int Property { get { return ref x; } }
}
class B : A
{
    public override ref readonly int Property { get { return ref x; } }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics();

            var aProperty = comp.GetMember<PropertySymbol>("A.Property");
            var bProperty = comp.GetMember<PropertySymbol>("B.Property");

            Assert.Empty(aProperty.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aProperty.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Equal(aProperty, bProperty.OverriddenOrHiddenMembers.OverriddenMembers.Single());
            Assert.Empty(bProperty.OverriddenOrHiddenMembers.HiddenMembers);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void DeclaringMethodWithDifferentParameterRefness()
        {
            var code = @"
class A
{
    public void M(in int x) { }
}
class B : A
{
    public void M(ref int x) { }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics();

            var aMethod = comp.GetMember<MethodSymbol>("A.M");
            var bMethod = comp.GetMember<MethodSymbol>("B.M");

            Assert.NotEqual(aMethod, bMethod);

            Assert.Empty(aMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(aMethod.OverriddenOrHiddenMembers.HiddenMembers);

            Assert.Empty(bMethod.OverriddenOrHiddenMembers.OverriddenMembers);
            Assert.Empty(bMethod.OverriddenOrHiddenMembers.HiddenMembers);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void OverridingRefReadOnlyMembersWillOverwriteTheCorrectSlot()
        {
            var text = @"
class BaseClass
{
    protected int field;
    public virtual ref readonly int Method1(in BaseClass a) { return ref field; }
    public virtual ref readonly int Property1 { get { return ref field; } }
    public virtual ref readonly int this[int a] { get { return ref field; } }
}

class DerivedClass : BaseClass
{
    public override ref readonly int Method1(in BaseClass a) { return ref field; }
    public override ref readonly int Property1 { get { return ref field; } }
    public override ref readonly int this[int a] { get { return ref field; } }
}";

            var comp = CreateCompilation(text).VerifyDiagnostics();

            var baseMethod = comp.GetMember<MethodSymbol>("BaseClass.Method1");
            var baseProperty = comp.GetMember<PropertySymbol>("BaseClass.Property1");
            var baseIndexer = comp.GetMember<PropertySymbol>("BaseClass.this[]");

            var derivedMethod = comp.GetMember<MethodSymbol>("DerivedClass.Method1");
            var derivedProperty = comp.GetMember<PropertySymbol>("DerivedClass.Property1");
            var derivedIndexer = comp.GetMember<PropertySymbol>("DerivedClass.this[]");

            Assert.Empty(baseMethod.OverriddenOrHiddenMembers.HiddenMembers);
            Assert.Empty(baseMethod.OverriddenOrHiddenMembers.OverriddenMembers);

            Assert.Empty(baseProperty.OverriddenOrHiddenMembers.HiddenMembers);
            Assert.Empty(baseProperty.OverriddenOrHiddenMembers.OverriddenMembers);

            Assert.Empty(baseIndexer.OverriddenOrHiddenMembers.HiddenMembers);
            Assert.Empty(baseIndexer.OverriddenOrHiddenMembers.OverriddenMembers);

            Assert.Empty(derivedMethod.OverriddenOrHiddenMembers.HiddenMembers);
            Assert.Equal(baseMethod, derivedMethod.OverriddenOrHiddenMembers.OverriddenMembers.Single());

            Assert.Empty(derivedProperty.OverriddenOrHiddenMembers.HiddenMembers);
            Assert.Equal(baseProperty, derivedProperty.OverriddenOrHiddenMembers.OverriddenMembers.Single());

            Assert.Empty(derivedIndexer.OverriddenOrHiddenMembers.HiddenMembers);
            Assert.Equal(baseIndexer, derivedIndexer.OverriddenOrHiddenMembers.OverriddenMembers.Single());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void MethodOverloadsShouldPreserveReadOnlyRefnessInParameters()
        {
            var text = @"
abstract class BaseClass
{
    public virtual void Method1(ref int x) { }
    public virtual void Method2(in int x) { }
}
class ChildClass : BaseClass
{
    public override void Method1(in int x) { }
    public override void Method2(ref int x) { }
}";

            var comp = CreateCompilation(text).VerifyDiagnostics(
                // (10,26): error CS0115: 'ChildClass.Method2(ref int)': no suitable method found to override
                //     public override void Method2(ref int x) { }
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method2").WithArguments("ChildClass.Method2(ref int)").WithLocation(10, 26),
                // (9,26): error CS0115: 'ChildClass.Method1(in int)': no suitable method found to override
                //     public override void Method1(in int x) { }
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Method1").WithArguments("ChildClass.Method1(in int)").WithLocation(9, 26));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void MethodOverloadsShouldPreserveReadOnlyRefnessInReturnTypes()
        {
            var text = @"
abstract class BaseClass
{
    protected int x = 0 ;
    public virtual ref int Method1() { return ref x; }
    public virtual ref readonly int Method2() { return ref x; }
}
class ChildClass : BaseClass
{
    public override ref readonly int Method1() { return ref x; }
    public override ref int Method2() { return ref x; }
}";

            var comp = CreateCompilation(text).VerifyDiagnostics(
                // (11,29): error CS8148: 'ChildClass.Method2()' must match by reference return of overridden member 'BaseClass.Method2()'
                //     public override ref int Method2() { return ref x; }
                Diagnostic(ErrorCode.ERR_CantChangeRefReturnOnOverride, "Method2").WithArguments("ChildClass.Method2()", "BaseClass.Method2()").WithLocation(11, 29),
                // (10,38): error CS8148: 'ChildClass.Method1()' must match by reference return of overridden member 'BaseClass.Method1()'
                //     public override in int Method1() { return ref x; }
                Diagnostic(ErrorCode.ERR_CantChangeRefReturnOnOverride, "Method1").WithArguments("ChildClass.Method1()", "BaseClass.Method1()").WithLocation(10, 38));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void PropertyOverloadsShouldPreserveReadOnlyRefnessInReturnTypes()
        {
            var code = @"
class A
{
    protected int x = 0;
    public virtual ref int Property1 { get { return ref x; } }
    public virtual ref readonly int Property2 { get { return ref x; } }
}
class B : A
{
    public override ref readonly int Property1 { get { return ref x; } }
    public override ref int Property2 { get { return ref x; } }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (11,29): error CS8148: 'B.Property2' must match by reference return of overridden member 'A.Property2'
                //     public override ref int Property2 { get { return ref x; } }
                Diagnostic(ErrorCode.ERR_CantChangeRefReturnOnOverride, "Property2").WithArguments("B.Property2", "A.Property2").WithLocation(11, 29),
                // (10,38): error CS8148: 'B.Property1' must match by reference return of overridden member 'A.Property1'
                //     public override ref readonly int Property1 { get { return ref x; } }
                Diagnostic(ErrorCode.ERR_CantChangeRefReturnOnOverride, "Property1").WithArguments("B.Property1", "A.Property1").WithLocation(10, 38));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void IndexerOverloadsShouldPreserveReadOnlyRefnessInReturnTypes_Ref_RefReadOnly()
        {
            var code = @"
class A
{
    protected int x = 0;
    public virtual ref int this[int p] { get { return ref x; } }
}
class B : A
{
    public override ref readonly int this[int p] { get { return ref x; } }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (9,38): error CS8148: 'B.this[int]' must match by reference return of overridden member 'A.this[int]'
                //     public override ref readonly int this[int p] { get { return ref x; } }
                Diagnostic(ErrorCode.ERR_CantChangeRefReturnOnOverride, "this").WithArguments("B.this[int]", "A.this[int]").WithLocation(9, 38));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void IndexerOverloadsShouldPreserveReadOnlyRefnessInReturnTypes_RefReadOnly_Ref()
        {
            var code = @"
class A
{
    protected int x = 0;
    public virtual ref readonly int this[int p] { get { return ref x; } }
}
class B : A
{
    public override ref int this[int p] { get { return ref x; } }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (9,29): error CS8148: 'B.this[int]' must match by reference return of overridden member 'A.this[int]'
                //     public override ref int this[int p] { get { return ref x; } }
                Diagnostic(ErrorCode.ERR_CantChangeRefReturnOnOverride, "this").WithArguments("B.this[int]", "A.this[int]").WithLocation(9, 29));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void IndexerOverloadsShouldPreserveReadOnlyRefnessInIndexes_Valid()
        {
            var code = @"
abstract class A
{
    public abstract int this[in int p] { get; }
}
class B : A
{
    public override int this[in int p] { get { return p; } }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics();
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void IndexerOverloadsShouldPreserveReadOnlyRefnessInIndexes_Source()
        {
            var code = @"
abstract class A
{
    public abstract int this[in int p] { get; }
}
class B : A
{
    public override int this[int p] { get { return p; } }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (8,25): error CS0115: 'B.this[int]': no suitable method found to override
                //     public override int this[int p] { get { return p; } }
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("B.this[int]").WithLocation(8, 25),
                // (6,7): error CS0534: 'B' does not implement inherited abstract member 'A.this[in int].get'
                // class B : A
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "B").WithArguments("B", "A.this[in int].get").WithLocation(6, 7));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void IndexerOverloadsShouldPreserveReadOnlyRefnessInIndexes_Destination()
        {
            var code = @"
abstract class A
{
    public abstract int this[int p] { get; }
}
class B : A
{
    public override int this[in int p] { get { return p; } }
}";

            var comp = CreateCompilation(code).VerifyDiagnostics(
                // (8,25): error CS0115: 'B.this[in int]': no suitable method found to override
                //     public override int this[in int p] { get { return p; } }
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "this").WithArguments("B.this[in int]").WithLocation(8, 25),
                // (6,7): error CS0534: 'B' does not implement inherited abstract member 'A.this[int].get'
                // class B : A
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "B").WithArguments("B", "A.this[int].get").WithLocation(6, 7));
        }
    }
}
