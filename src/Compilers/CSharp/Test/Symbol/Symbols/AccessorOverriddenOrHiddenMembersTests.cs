// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    /// <summary>
    /// Test MethodSymbol.OverriddenOrHiddenMembers and PropertySymbol.OverriddenOrHiddenMembers.
    /// </summary>
    public class AccessorOverriddenOrHiddenMembersTests : CSharpTestBase
    {
        [Fact]
        public void TestBasicOverriding()
        {
            var text = @"
class Base
{
    public virtual int P { get; set; }
}

class Derived : Base
{
    public override int P { get; set; }
}
";

            var compilation = CreateCompilation(text);

            var global = compilation.GlobalNamespace;

            var baseClass = global.GetMember<NamedTypeSymbol>("Base");
            var baseProperty = baseClass.GetMember<PropertySymbol>("P");
            var baseGetter = baseProperty.GetMethod;
            var baseSetter = baseProperty.SetMethod;

            var derivedClass = global.GetMember<NamedTypeSymbol>("Derived");
            var derivedProperty = derivedClass.GetMember<PropertySymbol>("P");
            var derivedGetter = derivedProperty.GetMethod;
            var derivedSetter = derivedProperty.SetMethod;

            Assert.Same(OverriddenOrHiddenMembersResult.Empty, baseProperty.OverriddenOrHiddenMembers);
            Assert.Null(baseProperty.OverriddenProperty);

            Assert.Same(OverriddenOrHiddenMembersResult.Empty, baseGetter.OverriddenOrHiddenMembers);
            Assert.Null(baseGetter.OverriddenMethod);

            Assert.Same(OverriddenOrHiddenMembersResult.Empty, baseSetter.OverriddenOrHiddenMembers);
            Assert.Null(baseSetter.OverriddenMethod);

            OverriddenOrHiddenMembersResult derivedPropertyOverriddenOrHidden = derivedProperty.OverriddenOrHiddenMembers;
            Assert.Equal(0, derivedPropertyOverriddenOrHidden.HiddenMembers.Length);
            Assert.Same(baseProperty, derivedPropertyOverriddenOrHidden.OverriddenMembers.Single());
            Assert.Same(baseProperty, derivedProperty.OverriddenProperty);

            OverriddenOrHiddenMembersResult derivedGetterOverriddenOrHidden = derivedGetter.OverriddenOrHiddenMembers;
            Assert.Equal(0, derivedGetterOverriddenOrHidden.HiddenMembers.Length);
            Assert.Same(baseGetter, derivedGetterOverriddenOrHidden.OverriddenMembers.Single());
            Assert.Same(baseGetter, derivedGetter.OverriddenMethod);

            OverriddenOrHiddenMembersResult derivedSetterOverriddenOrHidden = derivedSetter.OverriddenOrHiddenMembers;
            Assert.Equal(0, derivedSetterOverriddenOrHidden.HiddenMembers.Length);
            Assert.Same(baseSetter, derivedSetterOverriddenOrHidden.OverriddenMembers.Single());
            Assert.Same(baseSetter, derivedSetter.OverriddenMethod);
        }

        [Fact]
        public void TestIndirectOverriding()
        {
            var text = @"
class Base
{
    public virtual int P { get; set; }
}

class Derived1 : Base
{
    public override int P { get { return 1; } }
}

class Derived2 : Derived1
{
    public override int P { set { } }
}
";

            var compilation = CreateCompilation(text);

            var global = compilation.GlobalNamespace;

            var baseClass = global.GetMember<NamedTypeSymbol>("Base");
            var baseProperty = baseClass.GetMember<PropertySymbol>("P");
            var baseGetter = baseProperty.GetMethod;
            var baseSetter = baseProperty.SetMethod;

            var derived1Class = global.GetMember<NamedTypeSymbol>("Derived1");
            var derived1Property = derived1Class.GetMember<PropertySymbol>("P");
            var derived1Getter = derived1Property.GetMethod;
            Assert.Null(derived1Property.SetMethod);

            var derived2Class = global.GetMember<NamedTypeSymbol>("Derived2");
            var derived2Property = derived2Class.GetMember<PropertySymbol>("P");
            Assert.Null(derived2Property.GetMethod);
            var derived2Setter = derived2Property.SetMethod;

            OverriddenOrHiddenMembersResult derived1PropertyOverriddenOrHidden = derived1Property.OverriddenOrHiddenMembers;
            Assert.Equal(0, derived1PropertyOverriddenOrHidden.HiddenMembers.Length);
            Assert.Same(baseProperty, derived1PropertyOverriddenOrHidden.OverriddenMembers.Single());

            OverriddenOrHiddenMembersResult derived1GetterOverriddenOrHidden = derived1Getter.OverriddenOrHiddenMembers;
            Assert.Equal(0, derived1GetterOverriddenOrHidden.HiddenMembers.Length);
            Assert.Same(baseGetter, derived1GetterOverriddenOrHidden.OverriddenMembers.Single());

            OverriddenOrHiddenMembersResult derived2PropertyOverriddenOrHidden = derived2Property.OverriddenOrHiddenMembers;
            Assert.Equal(0, derived2PropertyOverriddenOrHidden.HiddenMembers.Length);
            Assert.Same(derived1Property, derived2PropertyOverriddenOrHidden.OverriddenMembers.Single());

            OverriddenOrHiddenMembersResult derived2SetterOverriddenOrHidden = derived2Setter.OverriddenOrHiddenMembers;
            Assert.Equal(0, derived2SetterOverriddenOrHidden.HiddenMembers.Length);
            Assert.Same(baseSetter, derived2SetterOverriddenOrHidden.OverriddenMembers.Single());
        }

        [Fact]
        public void TestBasicHiding()
        {
            var text = @"
class Base
{
    public virtual int P { get; set; }
}

class Derived : Base
{
    public new virtual int P { get; set; }
}
";

            var compilation = CreateCompilation(text);

            var global = compilation.GlobalNamespace;

            var baseClass = global.GetMember<NamedTypeSymbol>("Base");
            var baseProperty = baseClass.GetMember<PropertySymbol>("P");
            var baseGetter = baseProperty.GetMethod;
            var baseSetter = baseProperty.SetMethod;

            var derivedClass = global.GetMember<NamedTypeSymbol>("Derived");
            var derivedProperty = derivedClass.GetMember<PropertySymbol>("P");
            var derivedGetter = derivedProperty.GetMethod;
            var derivedSetter = derivedProperty.SetMethod;

            OverriddenOrHiddenMembersResult derivedPropertyOverriddenOrHidden = derivedProperty.OverriddenOrHiddenMembers;
            Assert.Equal(0, derivedPropertyOverriddenOrHidden.OverriddenMembers.Length);
            Assert.Same(baseProperty, derivedPropertyOverriddenOrHidden.HiddenMembers.Single());

            OverriddenOrHiddenMembersResult derivedGetterOverriddenOrHidden = derivedGetter.OverriddenOrHiddenMembers;
            Assert.Equal(0, derivedGetterOverriddenOrHidden.OverriddenMembers.Length);
            Assert.Same(baseGetter, derivedGetterOverriddenOrHidden.HiddenMembers.Single());

            OverriddenOrHiddenMembersResult derivedSetterOverriddenOrHidden = derivedSetter.OverriddenOrHiddenMembers;
            Assert.Equal(0, derivedSetterOverriddenOrHidden.OverriddenMembers.Length);
            Assert.Same(baseSetter, derivedSetterOverriddenOrHidden.HiddenMembers.Single());
        }

        [Fact]
        public void TestIndirectHiding()
        {
            var text1 = @"public class Base
{
    public virtual int P { get; set; }
}
";

            var text2 = @"public class Derived1 : Base
{
    public new virtual int P { get { return 1; } }
}
";

            var text3 = @"class Derived2 : Derived1
{
    public override int P { set { } }
}
";

            var comp1 = CreateCompilation(text1);
            var comp1ref = new CSharpCompilationReference(comp1);
            var refs = new System.Collections.Generic.List<MetadataReference>() { comp1ref };

            var comp2 = CreateCompilation(text2, references: refs, assemblyName: "Test2");
            var comp2ref = new CSharpCompilationReference(comp2);
            refs.Add(comp2ref);
            var compilation = CreateCompilation(text3, refs, assemblyName: "Test3");

            var global = compilation.GlobalNamespace;

            var baseClass = global.GetMember<NamedTypeSymbol>("Base");
            var baseProperty = baseClass.GetMember<PropertySymbol>("P");
            var baseGetter = baseProperty.GetMethod;

            var derived1Class = global.GetMember<NamedTypeSymbol>("Derived1");
            var derived1Property = derived1Class.GetMember<PropertySymbol>("P");
            var derived1Getter = derived1Property.GetMethod;
            Assert.Null(derived1Property.SetMethod);

            var derived2Class = global.GetMember<NamedTypeSymbol>("Derived2");
            var derived2Property = derived2Class.GetMember<PropertySymbol>("P");
            Assert.Null(derived2Property.GetMethod);
            var derived2Setter = derived2Property.SetMethod;

            OverriddenOrHiddenMembersResult derived1PropertyOverriddenOrHidden = derived1Property.OverriddenOrHiddenMembers;
            Assert.Equal(0, derived1PropertyOverriddenOrHidden.OverriddenMembers.Length);
            Assert.Same(baseProperty, derived1PropertyOverriddenOrHidden.HiddenMembers.Single());

            OverriddenOrHiddenMembersResult derived1GetterOverriddenOrHidden = derived1Getter.OverriddenOrHiddenMembers;
            Assert.Equal(0, derived1GetterOverriddenOrHidden.OverriddenMembers.Length);
            Assert.Same(baseGetter, derived1GetterOverriddenOrHidden.HiddenMembers.Single());

            OverriddenOrHiddenMembersResult derived2PropertyOverriddenOrHidden = derived2Property.OverriddenOrHiddenMembers;
            Assert.Equal(0, derived2PropertyOverriddenOrHidden.HiddenMembers.Length);
            Assert.Same(derived1Property, derived2PropertyOverriddenOrHidden.OverriddenMembers.Single());

            OverriddenOrHiddenMembersResult derived2SetterOverriddenOrHidden = derived2Setter.OverriddenOrHiddenMembers;
            Assert.Equal(0, derived2SetterOverriddenOrHidden.HiddenMembers.Length);
            Assert.Equal(0, derived2SetterOverriddenOrHidden.OverriddenMembers.Length);
        }

        [WorkItem(540145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540145")]
        [Fact]
        public void Regress6304_01()
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
";

            CreateCompilation(text).VerifyDiagnostics(
                // (8,29): error CS0533: 'TestClass2.P2' hides inherited abstract member 'TestClass1.P2'
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "P2").WithArguments("TestClass2.P2", "TestClass1.P2"));
        }

        [WorkItem(540145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540145")]
        [Fact]
        public void Regress6304_02()
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

            CreateCompilation(text).VerifyDiagnostics(
                // (8,29): error CS0533: 'TestClass2.P2' hides inherited abstract member 'TestClass1.P2'
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "P2").WithArguments("TestClass2.P2", "TestClass1.P2"),
                // (15,9): error CS0545: 'TestClass3.P2.get': cannot override because 'TestClass2.P2' does not have an overridable get accessor
                Diagnostic(ErrorCode.ERR_NoGetToOverride, "get").WithArguments("TestClass3.P2.get", "TestClass2.P2"),
                // (10,14): error CS0534: 'TestClass3' does not implement inherited abstract member 'TestClass1.P2.get'
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "TestClass3").WithArguments("TestClass3", "TestClass1.P2.get"));
        }

        [Fact]
        public void OverrideAccessorWithNonStandardName()
        {
            var il = @"
.class public Base {
  .method public hidebysig newslot virtual instance int32 getter() { ldnull throw }
  .property instance int32 P() { .get instance int32 Base::getter() }
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
}
";

            var csharp = @"
public class Derived1 : Base
{
    public override int P { get { return 1; } }
}

public class Derived2 : Derived1
{
    public override int P { get { return 1; } }
}
";

            var compilation = CreateCompilationWithILAndMscorlib40(csharp, il);
            var global = compilation.GlobalNamespace;

            var ilGetter = global.GetMember<NamedTypeSymbol>("Base").GetMember<PropertySymbol>("P").GetMethod;
            var csharpGetter1 = global.GetMember<NamedTypeSymbol>("Derived1").GetMember<PropertySymbol>("P").GetMethod;
            var csharpGetter2 = global.GetMember<NamedTypeSymbol>("Derived2").GetMember<PropertySymbol>("P").GetMethod;

            Assert.Equal(ilGetter.Name, csharpGetter1.Name);
            Assert.Equal(ilGetter.Name, csharpGetter2.Name);

            Assert.False(compilation.GetDiagnostics().Any());
        }

        [Fact]
        public void ImplicitlyImplementAccessorWithNonStandardName()
        {
            var il = @"
.class interface public abstract I {
  .method public hidebysig newslot abstract virtual instance int32 getter() { }
  .property instance int32 P() { .get instance int32 I::getter() }
}
";

            var csharp = @"
public class C : I
{
    public int P { get { return 1; } }
}
";

            var compilation = CreateCompilationWithILAndMscorlib40(csharp, il);
            var global = compilation.GlobalNamespace;

            var ilGetter = global.GetMember<NamedTypeSymbol>("I").GetMember<PropertySymbol>("P").GetMethod;
            var @class = global.GetMember<SourceNamedTypeSymbol>("C");
            var csharpGetter = @class.GetMember<PropertySymbol>("P").GetMethod;

            Assert.NotEqual(ilGetter.Name, csharpGetter.Name); //name not copied

            var bridge = @class.GetSynthesizedExplicitImplementations(CancellationToken.None).ForwardingMethods.Single();
            Assert.Same(csharpGetter, bridge.ImplementingMethod);
            Assert.Same(ilGetter, bridge.ExplicitInterfaceImplementations.Single());

            Assert.False(compilation.GetDiagnostics().Any());
        }

        [Fact]
        public void ExplicitlyImplementAccessorWithNonStandardName()
        {
            var il = @"
.class interface public abstract I {
  .method public hidebysig newslot abstract virtual instance int32 getter() { }
  .property instance int32 P() { .get instance int32 I::getter() }
}
";

            var csharp = @"
public class C : I
{
    int I.P { get { return 1; } }
}
";

            var compilation = CreateCompilationWithILAndMscorlib40(csharp, il);
            var global = compilation.GlobalNamespace;

            var ilGetter = global.GetMember<NamedTypeSymbol>("I").GetMember<PropertySymbol>("P").GetMethod;
            var @class = global.GetMember<SourceNamedTypeSymbol>("C");
            var csharpGetter = @class.GetProperty("I.P").GetMethod;

            Assert.Equal("I.getter", csharpGetter.Name);
            Assert.Equal(0, @class.GetSynthesizedExplicitImplementations(CancellationToken.None).ForwardingMethods.Length); //not needed
        }

        [Fact]
        public void ImplementAccessorWithNonAccessor()
        {
            var text = @"
interface I
{
    int P { get; set; }
}

class Base
{
    public virtual int P { get; set; } //match is never found because of non-accessor in Derived
}

class Derived : Base, I
{
    public int get_P() //CS0470
    {
        return 1;
    }
}
";

            var compilation = CreateCompilation(text);

            compilation.VerifyDiagnostics(
                // (14,16): error CS0470: Method 'Derived.get_P()' cannot implement interface accessor 'I.P.get' for type 'Derived'. Use an explicit interface implementation.
                Diagnostic(ErrorCode.ERR_MethodImplementingAccessor, "get_P").WithArguments("Derived.get_P()", "I.P.get", "Derived"));

            var global = compilation.GlobalNamespace;

            var @interface = global.GetMember<NamedTypeSymbol>("I");
            var interfaceProperty = @interface.GetMember<PropertySymbol>("P");
            var interfaceGetter = interfaceProperty.GetMethod;
            var interfaceSetter = interfaceProperty.SetMethod;

            var baseClass = global.GetMember<NamedTypeSymbol>("Base");
            var baseProperty = baseClass.GetMember<PropertySymbol>("P");
            var baseGetter = baseProperty.GetMethod;
            var baseSetter = baseProperty.SetMethod;

            var derivedClass = global.GetMember<NamedTypeSymbol>("Derived");
            var derivedMethod = derivedClass.GetMember<MethodSymbol>("get_P");
            Assert.Equal(MethodKind.Ordinary, derivedMethod.MethodKind);

            // The property and its setter are implemented in Base
            Assert.Equal(baseProperty, derivedClass.FindImplementationForInterfaceMember(interfaceProperty));
            Assert.Equal(baseSetter, derivedClass.FindImplementationForInterfaceMember(interfaceSetter));

            // The getter is implemented (erroneously) in Derived
            Assert.Equal(derivedMethod, derivedClass.FindImplementationForInterfaceMember(interfaceGetter));
        }

        [Fact]
        public void ImplementNonAccessorWithAccessor()
        {
            var text = @"
interface I
{
    int get_P();
}

class Base
{
    public int get_P() //match is never found because of accessor in Derived
    {
        return 1;
    }
}

class Derived : Base, I
{
    public int P { get; set; }
}
";

            var compilation = CreateCompilation(text);

            compilation.VerifyDiagnostics(
                // (17,20): error CS0686: Accessor 'Derived.P.get' cannot implement interface member 'I.get_P()' for type 'Derived'. Use an explicit interface implementation.
                Diagnostic(ErrorCode.ERR_AccessorImplementingMethod, "get").WithArguments("Derived.P.get", "I.get_P()", "Derived"));

            var global = compilation.GlobalNamespace;

            var @interface = global.GetMember<NamedTypeSymbol>("I");
            var interfaceMethod = @interface.GetMember<MethodSymbol>("get_P");

            var baseClass = global.GetMember<NamedTypeSymbol>("Base");
            var baseMethod = baseClass.GetMember<MethodSymbol>("get_P");

            var derivedClass = global.GetMember<NamedTypeSymbol>("Derived");
            var derivedProperty = derivedClass.GetMember<PropertySymbol>("P");
            var derivedGetter = derivedProperty.GetMethod;
            Assert.Equal(MethodKind.PropertyGet, derivedGetter.MethodKind);

            // The method is implemented (erroneously) in Derived
            Assert.Equal(derivedGetter, derivedClass.FindImplementationForInterfaceMember(interfaceMethod));
        }

        [Fact]
        public void ImplementingAccessorWithNonAccessorMayReportInInterfaceReference()
        {
            var source = @"
interface I<T>
{
	T P { get; }
}
class Base
{
    public int get_P() { return 1; }
}
class Derived : Base, I<int> // CS0470 must be reported in ""I<int>""
{
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (10,23): error CS0470: Method 'Base.get_P()' cannot implement interface accessor 'I<int>.P.get' for type 'Derived'. Use an explicit interface implementation.
                // class Derived : Base, I<int>
                Diagnostic(ErrorCode.ERR_MethodImplementingAccessor, "I<int>").WithArguments("Base.get_P()", "I<int>.P.get", "Derived").WithLocation(10, 23),
                // (10,23): error CS0535: 'Derived' does not implement interface member 'I<int>.P'
                // class Derived : Base, I<int>
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I<int>").WithArguments("Derived", "I<int>.P").WithLocation(10, 23));
        }

        [Fact]
        public void ImplementingNonAccessorWithAccessorMayReportInInterfaceReference()
        {
            var source = @"
interface I<T>
{
    T get_P();
}
class Base
{
    public int P { get; }
}
class Derived : Base, I<int> // CS0686 must be reported in ""I<int>""
{
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (10,23): error CS0686: Accessor 'Base.P.get' cannot implement interface member 'I<int>.get_P()' for type 'Derived'. Use an explicit interface implementation.
                // class Derived : Base, I<int>
                Diagnostic(ErrorCode.ERR_AccessorImplementingMethod, "I<int>").WithArguments("Base.P.get", "I<int>.get_P()", "Derived").WithLocation(10, 23));
        }

        [Fact]
        public void PropertyHidesBetterImplementation()
        {
            var text = @"
interface I
{
    int P { get; set; }
}

class Base
{
    public virtual int P { get; set; }
}

class Derived : Base, I //CS0535
{
    public new virtual int P { get { return 1; } }
}
";

            var compilation = CreateCompilation(text);

            compilation.VerifyDiagnostics(
                // (12,7): error CS0535: 'Derived' does not implement interface member 'I.P.set'
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I").WithArguments("Derived", "I.P.set"));

            var global = compilation.GlobalNamespace;

            var @interface = global.GetMember<NamedTypeSymbol>("I");
            var interfaceProperty = @interface.GetMember<PropertySymbol>("P");
            var interfaceGetter = interfaceProperty.GetMethod;
            var interfaceSetter = interfaceProperty.SetMethod;

            var baseClass = global.GetMember<NamedTypeSymbol>("Base");
            var baseProperty = baseClass.GetMember<PropertySymbol>("P");
            var baseGetter = baseProperty.GetMethod;
            var baseSetter = baseProperty.SetMethod;

            var derivedClass = global.GetMember<NamedTypeSymbol>("Derived");
            var derivedProperty = derivedClass.GetMember<PropertySymbol>("P");
            var derivedGetter = derivedProperty.GetMethod;
            Assert.Null(derivedProperty.SetMethod);

            // The property and its getter are implemented in Derived
            Assert.Equal(derivedProperty, derivedClass.FindImplementationForInterfaceMember(interfaceProperty));
            Assert.Equal(derivedGetter, derivedClass.FindImplementationForInterfaceMember(interfaceGetter));

            // The setter is not implemented
            Assert.Null(derivedClass.FindImplementationForInterfaceMember(interfaceSetter));
        }

        [Fact]
        public void PartialPropertyOverriding()
        {
            var text = @"
interface I
{
    int P { get; set; }
}

class Base
{
    public virtual int P
    {
        get { return 1; }
        set { }
    }
}

class Derived1 : Base
{
    public override int P
    {
        get { return 1; }
    }
}

class Derived2 : Derived1
{
    public override int P
    {
        set { }
    }
}

class Derived3 : Derived2, I
{
}
";

            var compilation = CreateCompilation(text);

            compilation.VerifyDiagnostics();

            var global = compilation.GlobalNamespace;

            var @interface = global.GetMember<NamedTypeSymbol>("I");
            var interfaceProperty = @interface.GetMember<PropertySymbol>("P");
            var interfaceGetter = interfaceProperty.GetMethod;
            var interfaceSetter = interfaceProperty.SetMethod;

            var derived1Class = global.GetMember<NamedTypeSymbol>("Derived1");
            var derived1Property = derived1Class.GetMember<PropertySymbol>("P");
            var derived1Getter = derived1Property.GetMethod;
            Assert.NotNull(derived1Getter);

            var derived2Class = global.GetMember<NamedTypeSymbol>("Derived2");
            var derived2Property = derived2Class.GetMember<PropertySymbol>("P");
            var derived2Setter = derived2Property.SetMethod;
            Assert.NotNull(derived2Setter);

            var derived3Class = global.GetMember<NamedTypeSymbol>("Derived3");

            // Property and setter are implemented in Derived2
            Assert.Equal(derived2Property, derived3Class.FindImplementationForInterfaceMember(interfaceProperty));
            Assert.Equal(derived2Setter, derived3Class.FindImplementationForInterfaceMember(interfaceSetter));

            // Getter is implemented in Derived1
            Assert.Equal(derived1Getter, derived3Class.FindImplementationForInterfaceMember(interfaceGetter));
        }

        [Fact]
        public void InterfaceAccessorHiding()
        {
            var text = @"
interface I1
{
    int P { get; set; }
}

interface I2
{
    int P { get; set; }
}

interface I3 : I1, I2
{
    int P { get; }
}

interface I4 : I3
{
    int P { set; }
}
";

            var compilation = CreateCompilation(text);

            compilation.VerifyDiagnostics(
                // (14,9): warning CS0108: 'I3.P' hides inherited member 'I1.P'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "P").WithArguments("I3.P", "I1.P"),
                // (19,9): warning CS0108: 'I4.P' hides inherited member 'I3.P'. Use the new keyword if hiding was intended.
                Diagnostic(ErrorCode.WRN_NewRequired, "P").WithArguments("I4.P", "I3.P"));

            var global = compilation.GlobalNamespace;

            var interface1 = global.GetMember<NamedTypeSymbol>("I1");
            var interface1Property = interface1.GetMember<PropertySymbol>("P");
            var interface1Getter = interface1Property.GetMethod;
            var interface1Setter = interface1Property.SetMethod;

            var interface2 = global.GetMember<NamedTypeSymbol>("I2");
            var interface2Property = interface2.GetMember<PropertySymbol>("P");
            var interface2Getter = interface2Property.GetMethod;
            var interface2Setter = interface2Property.SetMethod;

            var interface3 = global.GetMember<NamedTypeSymbol>("I3");
            var interface3Property = interface3.GetMember<PropertySymbol>("P");
            var interface3Getter = interface3Property.GetMethod;

            var interface4 = global.GetMember<NamedTypeSymbol>("I4");
            var interface4Property = interface4.GetMember<PropertySymbol>("P");
            var interface4Setter = interface4Property.SetMethod;

            var interface3PropertyOverriddenOrHidden = interface3Property.OverriddenOrHiddenMembers;
            Assert.Equal(0, interface3PropertyOverriddenOrHidden.OverriddenMembers.Length);
            AssertEx.SetEqual(interface3PropertyOverriddenOrHidden.HiddenMembers, interface1Property, interface2Property);

            var interface3GetterOverriddenOrHidden = interface3Getter.OverriddenOrHiddenMembers;
            Assert.Equal(0, interface3GetterOverriddenOrHidden.OverriddenMembers.Length);
            AssertEx.SetEqual(interface3GetterOverriddenOrHidden.HiddenMembers, interface1Getter, interface2Getter);

            var interface4PropertyOverriddenOrHidden = interface4Property.OverriddenOrHiddenMembers;
            Assert.Equal(0, interface4PropertyOverriddenOrHidden.OverriddenMembers.Length);
            Assert.Equal(interface3Property, interface4PropertyOverriddenOrHidden.HiddenMembers.Single());

            var interface4SetterOverriddenOrHidden = interface4Setter.OverriddenOrHiddenMembers;
            Assert.Equal(0, interface4SetterOverriddenOrHidden.OverriddenMembers.Length);
            Assert.Equal(0, interface4SetterOverriddenOrHidden.HiddenMembers.Length);
        }

        [Fact]
        public void ImplicitlyImplementAccessorWithAccessorFromOtherProperty()
        {
            var il = @"
.class interface public abstract I {
  .method public hidebysig newslot abstract virtual instance int32 get_Q() { }
  .property instance int32 P() { .get instance int32 I::get_Q() }
  .method public hidebysig newslot abstract virtual instance int32 get_P() { }
  .property instance int32 Q() { .get instance int32 I::get_P() }
}
";

            var csharp = @"
public class C : I
{
    public int P { get { return 1; } }
    public int Q { get { return 1; } }
}
";

            var compilation = CreateCompilationWithILAndMscorlib40(csharp, il);
            var global = compilation.GlobalNamespace;

            var @interface = global.GetMember<NamedTypeSymbol>("I");

            var interfaceP = @interface.GetMember<PropertySymbol>("P");
            var interfacePGetter = interfaceP.GetMethod;
            Assert.Equal("get_Q", interfacePGetter.Name); //NB: switched

            var interfaceQ = @interface.GetMember<PropertySymbol>("Q");
            var interfaceQGetter = interfaceQ.GetMethod;
            Assert.Equal("get_P", interfaceQGetter.Name); //NB: switched

            var @class = global.GetMember<NamedTypeSymbol>("C");

            var classP = @class.GetMember<PropertySymbol>("P");
            var classPGetter = classP.GetMethod;
            Assert.Equal("get_P", classPGetter.Name); //NB: not switched

            var classQ = @class.GetMember<PropertySymbol>("Q");
            var classQGetter = classQ.GetMethod;
            Assert.Equal("get_Q", classQGetter.Name); //NB: not switched

            Assert.Equal(classP, @class.FindImplementationForInterfaceMember(interfaceP));
            Assert.Equal(classQ, @class.FindImplementationForInterfaceMember(interfaceQ));

            //Dev10 chooses the accessors from the corresponding properties
            Assert.Equal(classPGetter, @class.FindImplementationForInterfaceMember(interfacePGetter));
            Assert.Equal(classQGetter, @class.FindImplementationForInterfaceMember(interfaceQGetter));

            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void TestImplOverridePropGetSetMismatchMetadataErr()
        {
            #region "Impl"
            var text1 = @"
public class CSIPropImpl : VBIPropImpl, IProp
{
    private string _str;
    private uint _uint = 22;

    #region implicit
    public override string ReadOnlyProp
    {
        get { return _str; }
        set { _str = value; } // setter in ReadOnly
    }

    public override string WriteOnlyProp
    {
        get { return _str; } // getter in WriteOnly
    }
    #endregion

    #region explicit
    string IProp.ReadOnlyProp
    {
        set { _str = value; }
    }
    string IProp.WriteOnlyProp
    {
        get { return _str; }
        set { _str = value; }
    }
    #endregion
}
";
            #endregion

            var asm01 = TestReferences.MetadataTests.InterfaceAndClass.VBInterfaces01;
            var asm02 = TestReferences.MetadataTests.InterfaceAndClass.VBClasses01;
            var refs = new System.Collections.Generic.List<MetadataReference>() { asm01, asm02 };

            var comp = CreateCompilation(text1, references: refs, assemblyName: "OHI_ExpImpPropGetSetMismatch001",
                            options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (21,18): error CS0551: Explicit interface implementation 'CSIPropImpl.IProp.ReadOnlyProp' is missing accessor 'IProp.ReadOnlyProp.get'
                //     string IProp.ReadOnlyProp
                Diagnostic(ErrorCode.ERR_ExplicitPropertyMissingAccessor, "ReadOnlyProp").WithArguments("CSIPropImpl.IProp.ReadOnlyProp", "IProp.ReadOnlyProp.get"),
                // (23,9): error CS0550: 'CSIPropImpl.IProp.ReadOnlyProp.set' adds an accessor not found in interface member 'IProp.ReadOnlyProp'
                //         set { _str = value; }
                Diagnostic(ErrorCode.ERR_ExplicitPropertyAddingAccessor, "set").WithArguments("CSIPropImpl.IProp.ReadOnlyProp.set", "IProp.ReadOnlyProp"),
                // (27,9): error CS0550: 'CSIPropImpl.IProp.WriteOnlyProp.get' adds an accessor not found in interface member 'IProp.WriteOnlyProp'
                //         get { return _str; }
                Diagnostic(ErrorCode.ERR_ExplicitPropertyAddingAccessor, "get").WithArguments("CSIPropImpl.IProp.WriteOnlyProp.get", "IProp.WriteOnlyProp"),
                // (11,9): error CS0546: 'CSIPropImpl.ReadOnlyProp.set': cannot override because 'VBIPropImpl.ReadOnlyProp' does not have an overridable set accessor
                //         set { _str = value; } // setter in ReadOnly
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "set").WithArguments("CSIPropImpl.ReadOnlyProp.set", "VBIPropImpl.ReadOnlyProp"),
                // (16,9): error CS0545: 'CSIPropImpl.WriteOnlyProp.get': cannot override because 'VBIPropImpl.WriteOnlyProp' does not have an overridable get accessor
                //         get { return _str; } // getter in WriteOnly
                Diagnostic(ErrorCode.ERR_NoGetToOverride, "get").WithArguments("CSIPropImpl.WriteOnlyProp.get", "VBIPropImpl.WriteOnlyProp"),
                // (5,18): warning CS0414: The field 'CSIPropImpl._uint' is assigned but its value is never used
                //     private uint _uint = 22;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "_uint").WithArguments("CSIPropImpl._uint")
            );
        }

        [WorkItem(546143, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546143")]
        [Fact]
        public void AccessorWithImportedGenericType()
        {
            var comp0 = CreateCompilation(@"
public class MC<T> { }
public delegate void MD<T>(T t);
");

            var compref = new CSharpCompilationReference(comp0);
            var comp1 = CreateCompilation(@"
using System;
public class G<T>
{
    public MC<T> Prop  {    set { }    }
    public int this[MC<T> p]  {    get { return 0;}    }
    public event MD<T> E  {     add { }  remove { }    }
}
", references: new MetadataReference[] { compref }, assemblyName: "ACCImpGen");

            var mtdata = comp1.EmitToArray(options: new EmitOptions(metadataOnly: true));
            var mtref = MetadataReference.CreateFromImage(mtdata);
            var comp2 = CreateCompilation(@"", references: new MetadataReference[] { mtref }, assemblyName: "META");

            var tsym = comp2.GetReferencedAssemblySymbol(mtref).GlobalNamespace.GetMember<NamedTypeSymbol>("G");
            Assert.NotNull(tsym);

            var mems = tsym.GetMembers().Where(s => s.Kind == SymbolKind.Method);
            // 4 accessors + ctor
            Assert.Equal(5, mems.Count());
            foreach (MethodSymbol m in mems)
            {
                if (m.MethodKind == MethodKind.Constructor)
                    continue;

                Assert.NotNull(m.AssociatedSymbol);
                Assert.NotEqual(MethodKind.Ordinary, m.MethodKind);
            }
        }

        [WorkItem(546143, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546143")]
        [ClrOnlyFact]
        public void OverridingExplicitInterfaceImplementationFromSource()
        {
            var il = @"
.class interface public abstract auto ansi I
{
  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_P() cil managed
  {
  }

  .property instance int32 P()
  {
    .get instance int32 I::get_P()
  }
} // end of class I

.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
       implements I
{
  .method public newslot specialname strict virtual 
          instance int32  get_P() cil managed
  {
    .override I::get_P
    
	ldc.i4.0
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .property instance int32 P()
  {
    .get instance int32 Base::get_P()
  }
} // end of class Base
";

            var csharp = @"
using System;

class Derived : Base
{
    public override int P { get { return 1; } }

    static void Main()
    {
        Derived d = new Derived();
        Base b = d;
        I i = d;
        
        Console.WriteLine(d.P);
        Console.WriteLine(b.P);
        Console.WriteLine(i.P);
    }
}
";

            CompileAndVerify(csharp, new[] { CompileIL(il) }, expectedOutput: @"
1
1
1
");
        }

        [WorkItem(1102883, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1102883")]
        [Fact]
        public void Bug1102883_ReproSteps()
        {
            const string sourceFromReproSteps = @"
class A
{
    void Goo()
    {
        var d = new Disposable();
        d.Dispose();
    }
}
class Disposable : System.IDisposable
{
    void IDisposable.Dispose() { }
    public void Dispose() { }
}
";
            // The using directive 'using System;' is not present in the repro steps,
            // but is required to successfully bind the interface name IDisposable 
            // in the explicit interface implementation IDisposable.Dispose().
            // We are testing both cases: with and without the using directive.
            // FindImplementationForInterfaceMember should return the explicit interface implementation 
            // if it is successfully bound, and should fall back to the public method Dispose() otherwise.
            const string sourceWithUsingDirective = @"
using System;
" + sourceFromReproSteps;

            var testCases = new[] {
                Tuple.Create(sourceFromReproSteps, new { isInterfaceNameBound = false, isDisposeExpected = true }),
                Tuple.Create(sourceWithUsingDirective, new { isInterfaceNameBound = true, isDisposeExpected = false })
              };

            foreach (var testCase in testCases)
            {
                var source = testCase.Item1;
                var expectedResult = testCase.Item2;

                var compilation = (Compilation)CreateCompilation(source);
                var syntaxTree = compilation.SyntaxTrees.Single();
                var nodes = syntaxTree.GetRoot().DescendantNodes();

                var invocationSyntax = nodes.OfType<InvocationExpressionSyntax>().Single();
                Assert.Equal("d.Dispose()", invocationSyntax.ToString());

                var memberAccessSyntax = (MemberAccessExpressionSyntax)invocationSyntax.Expression;
                Assert.Equal("d.Dispose", memberAccessSyntax.ToString());

                var identifierSyntax = (IdentifierNameSyntax)memberAccessSyntax.Expression;
                Assert.Equal("d", identifierSyntax.ToString());
                Assert.Equal("d", identifierSyntax.Identifier.Text);
                Assert.Equal(0, identifierSyntax.Arity);

                var memberNameSyntax = memberAccessSyntax.Name;
                Assert.Equal("Dispose", memberNameSyntax.ToString());
                Assert.Equal("Dispose", memberNameSyntax.Identifier.Text);
                Assert.Equal(0, memberNameSyntax.Arity);

                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var classDisposable = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("Disposable");
                Assert.Equal(TypeKind.Class, classDisposable.TypeKind);
                Assert.Equal("Disposable", classDisposable.Name);

                var localD = (ILocalSymbol)semanticModel.GetSymbolInfo(identifierSyntax).Symbol;
                Assert.Equal("d", localD.Name);
                Assert.Equal("Disposable", localD.Type.Name);
                Assert.Equal(classDisposable, localD.Type);

                var methodDispose = (IMethodSymbol)semanticModel.GetSymbolInfo(memberAccessSyntax).Symbol;
                Assert.Equal("Dispose", methodDispose.Name);
                Assert.Equal(0, methodDispose.Arity);
                Assert.Empty(methodDispose.Parameters);
                Assert.True(methodDispose.ReturnsVoid);
                Assert.Equal(classDisposable, methodDispose.ContainingType);
                Assert.Equal(Accessibility.Public, methodDispose.DeclaredAccessibility);
                Assert.Empty(methodDispose.ExplicitInterfaceImplementations);

                var explicitInterfaceImplementation = nodes.OfType<MethodDeclarationSyntax>().Single(d => d.ExplicitInterfaceSpecifier != null);
                var interfaceName = explicitInterfaceImplementation.ExplicitInterfaceSpecifier.Name;
                var isInterfaceNameBound = semanticModel.GetSymbolInfo(interfaceName).Symbol is INamedTypeSymbol;
                Assert.Equal(expectedResult.isInterfaceNameBound, isInterfaceNameBound);

                var memberAccessed = memberAccessSyntax;

                // BEGIN CODE FROM REPRO STEPS

                var methodSymbol = semanticModel.GetSymbolInfo(memberAccessed).Symbol as IMethodSymbol;
                var type = methodSymbol.ContainingType;
                var disposeMethod = (IMethodSymbol)compilation.GetSpecialType(SpecialType.System_IDisposable).GetMembers("Dispose").Single();
                var isDispose = methodSymbol.Equals(type.FindImplementationForInterfaceMember(disposeMethod));

                // END CODE FROM REPRO STEPS

                Assert.Equal(expectedResult.isDisposeExpected, isDispose);
            }
        }
    }
}
