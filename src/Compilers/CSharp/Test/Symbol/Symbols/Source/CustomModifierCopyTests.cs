// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Test that when metadata methods with custom modifiers in their signatures are overridden
    /// or explicitly implemented, the custom modifiers are copied to the corresponding source
    /// method.  Secondarily, test that generated bridge methods have appropriate custom modifiers
    /// in implicit implementation cases.
    /// </summary>
    public class CustomModifierCopyTests : CSharpTestBase
    {
        private const string ConstModOptType = "System.Runtime.CompilerServices.IsConst";

        /// <summary>
        /// Test implementing a single interface with custom modifiers.
        /// </summary>
        [Fact]
        public void TestSingleInterfaceImplementation()
        {
            var text = @"
class Class : CppCli.CppInterface1
{
    //copy modifiers (even though dev10 doesn't)
    void CppCli.CppInterface1.Method1(int x) { }

    //synthesize bridge method
    public void Method2(int x) { }
}
";

            var ilAssemblyReference = TestReferences.SymbolsTests.CustomModifiers.CppCli.dll;

            var comp = CreateCompilationWithMscorlib(text, new MetadataReference[] { ilAssemblyReference });
            var global = comp.GlobalNamespace;

            comp.VerifyDiagnostics();

            var @class = global.GetMember<SourceNamedTypeSymbol>("Class");

            // explicit implementation copies custom modifiers
            var classMethod1 = @class.GetMethod("CppCli.CppInterface1.Method1");
            AssertAllParametersHaveConstModOpt(classMethod1);

            // implicit implementation does not copy custom modifiers
            var classMethod2 = @class.GetMethod("Method2");
            AssertNoParameterHasModOpts(classMethod2);

            // bridge method for implicit implementation has custom modifiers
            var method2ExplicitImpl = @class.GetSynthesizedExplicitImplementations(CancellationToken.None).Single();
            Assert.Same(classMethod2, method2ExplicitImpl.ImplementingMethod);
            AssertAllParametersHaveConstModOpt(method2ExplicitImpl);
        }

        /// <summary>
        /// Test implementing multiple (identical) interfaces with custom modifiers.
        /// </summary>
        [Fact]
        public void TestMultipleInterfaceImplementation()
        {
            var text = @"
class Class : CppCli.CppInterface1, CppCli.CppInterface2
{
    //copy modifiers (even though dev10 doesn't)
    void CppCli.CppInterface1.Method1(int x) { }

    //copy modifiers (even though dev10 doesn't)
    void CppCli.CppInterface2.Method1(int x) { }

    //synthesize two bridge methods
    public void Method2(int x) { }
}
";

            var ilAssemblyReference = TestReferences.SymbolsTests.CustomModifiers.CppCli.dll;

            var comp = CreateCompilationWithMscorlib(text, new MetadataReference[] { ilAssemblyReference });
            var global = comp.GlobalNamespace;

            comp.VerifyDiagnostics();

            var @class = global.GetMember<SourceNamedTypeSymbol>("Class");

            // explicit implementation copies custom modifiers
            var classMethod1a = @class.GetMethod("CppCli.CppInterface1.Method1");
            AssertAllParametersHaveConstModOpt(classMethod1a);

            // explicit implementation copies custom modifiers
            var classMethod1b = @class.GetMethod("CppCli.CppInterface2.Method1");
            AssertAllParametersHaveConstModOpt(classMethod1b);

            // implicit implementation does not copy custom modifiers
            var classMethod2 = @class.GetMember<MethodSymbol>("Method2");
            AssertNoParameterHasModOpts(classMethod2);

            // bridges method for implicit implementation have custom modifiers
            var method2ExplicitImpls = @class.GetSynthesizedExplicitImplementations(CancellationToken.None);
            Assert.Equal(2, method2ExplicitImpls.Length);
            foreach (var explicitImpl in method2ExplicitImpls)
            {
                Assert.Same(classMethod2, explicitImpl.ImplementingMethod);
                AssertAllParametersHaveConstModOpt(explicitImpl);
            }
        }

        /// <summary>
        /// Test a direct override of a metadata method with custom modifiers.
        /// Also confirm that a source method without custom modifiers can hide
        /// a metadata method with custom modifiers (in the sense that "new" is
        /// required) but does not copy the custom modifiers.
        /// </summary>
        [Fact]
        public void TestSingleOverride()
        {
            var text = @"
class Class : CppCli.CppBase1
{
        //copies custom modifiers
        public override void VirtualMethod(int x) { }

        //new required, does not copy custom modifiers
        public new void NonVirtualMethod(int x) { }
}
";

            var ilAssemblyReference = TestReferences.SymbolsTests.CustomModifiers.CppCli.dll;

            var comp = CreateCompilationWithMscorlib(text, new MetadataReference[] { ilAssemblyReference });
            var global = comp.GlobalNamespace;

            comp.VerifyDiagnostics();

            var @class = global.GetMember<SourceNamedTypeSymbol>("Class");

            // override copies custom modifiers
            var classVirtualMethod = @class.GetMember<MethodSymbol>("VirtualMethod");
            AssertAllParametersHaveConstModOpt(classVirtualMethod);

            // new does not copy custom modifiers
            var classNonVirtualMethod = @class.GetMember<MethodSymbol>("NonVirtualMethod");
            AssertNoParameterHasModOpts(classNonVirtualMethod);

            // no bridge methods
            Assert.Equal(0, @class.GetSynthesizedExplicitImplementations(CancellationToken.None).Length);
        }

        /// <summary>
        /// Test overriding a source method that overrides a metadata method with
        /// custom modifiers.  The custom modifiers should propagate to the second
        /// override as well.
        /// </summary>
        [Fact]
        public void TestRepeatedOverride()
        {
            var text = @"
class Base : CppCli.CppBase1
{
        //copies custom modifiers
        public override void VirtualMethod(int x) { }

        //new required, does not copy custom modifiers
        public new virtual void NonVirtualMethod(int x) { }
}

class Derived : Base
{
        //copies custom modifiers
        public override void VirtualMethod(int x) { }

        //would copy custom modifiers, but there are none
        public override void NonVirtualMethod(int x) { }
}
";

            var ilAssemblyReference = TestReferences.SymbolsTests.CustomModifiers.CppCli.dll;

            var comp = CreateCompilationWithMscorlib(text, new MetadataReference[] { ilAssemblyReference });
            var global = comp.GlobalNamespace;

            comp.VerifyDiagnostics();

            var baseClass = global.GetMember<SourceNamedTypeSymbol>("Base");

            // override copies custom modifiers
            var baseClassVirtualMethod = baseClass.GetMember<MethodSymbol>("VirtualMethod");
            AssertAllParametersHaveConstModOpt(baseClassVirtualMethod);

            // new does not copy custom modifiers
            var baseClassNonVirtualMethod = baseClass.GetMember<MethodSymbol>("NonVirtualMethod");
            AssertNoParameterHasModOpts(baseClassNonVirtualMethod);

            // no bridge methods
            Assert.Equal(0, baseClass.GetSynthesizedExplicitImplementations(CancellationToken.None).Length);

            var derivedClass = global.GetMember<SourceNamedTypeSymbol>("Derived");

            // override copies custom modifiers
            var derivedClassVirtualMethod = derivedClass.GetMember<MethodSymbol>("VirtualMethod");
            AssertAllParametersHaveConstModOpt(derivedClassVirtualMethod);

            // new does not copy custom modifiers
            var derivedClassNonVirtualMethod = derivedClass.GetMember<MethodSymbol>("NonVirtualMethod");
            AssertNoParameterHasModOpts(derivedClassNonVirtualMethod);

            // no bridge methods
            Assert.Equal(0, derivedClass.GetSynthesizedExplicitImplementations(CancellationToken.None).Length);
        }

        /// <summary>
        /// Test copying custom modifiers in/on parameters/return types.
        /// </summary>
        [Fact]
        public void TestMethodOverrideCombinations()
        {
            var text = @"
class Derived : MethodCustomModifierCombinations
{
    public override int[] Method1111(int[] a) { return a; }
    public override int[] Method1110(int[] a) { return a; }
    public override int[] Method1101(int[] a) { return a; }
    public override int[] Method1100(int[] a) { return a; }
    public override int[] Method1011(int[] a) { return a; }
    public override int[] Method1010(int[] a) { return a; }
    public override int[] Method1001(int[] a) { return a; }
    public override int[] Method1000(int[] a) { return a; }
    public override int[] Method0111(int[] a) { return a; }
    public override int[] Method0110(int[] a) { return a; }
    public override int[] Method0101(int[] a) { return a; }
    public override int[] Method0100(int[] a) { return a; }
    public override int[] Method0011(int[] a) { return a; }
    public override int[] Method0010(int[] a) { return a; }
    public override int[] Method0001(int[] a) { return a; }
    public override int[] Method0000(int[] a) { return a; }
}
";

            var ilAssemblyReference = TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll;

            var comp = CreateCompilationWithMscorlib(text, new MetadataReference[] { ilAssemblyReference });
            var global = comp.GlobalNamespace;

            comp.VerifyDiagnostics();

            var @class = global.GetMember<NamedTypeSymbol>("Derived");

            for (int i = 0; i < 0xf; i++)
            {
                CheckMethodCustomModifiers(
                    @class.GetMember<MethodSymbol>("Method" + Convert.ToString(i, 2).PadLeft(4, '0')),
                    inReturnType: (i & 0x8) != 0,
                    onReturnType: (i & 0x4) != 0,
                    inParameterType: (i & 0x2) != 0,
                    onParameterType: (i & 0x1) != 0);
            }
        }

        /// <summary>
        /// Test copying custom modifiers in/on property types.
        /// </summary>
        [Fact]
        public void TestPropertyOverrideCombinations()
        {
            var text = @"
class Derived : PropertyCustomModifierCombinations
{
    public override int[] Property11 { get; set; }
    public override int[] Property10 { get; set; }
    public override int[] Property01 { get; set; }
    public override int[] Property00 { get; set; }
}
";

            var ilAssemblyReference = TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll;

            var comp = CreateCompilationWithMscorlib(text, new MetadataReference[] { ilAssemblyReference });
            var global = comp.GlobalNamespace;

            comp.VerifyDiagnostics();

            var @class = global.GetMember<NamedTypeSymbol>("Derived");

            for (int i = 0; i < 0x4; i++)
            {
                PropertySymbol property = @class.GetMember<PropertySymbol>("Property" + Convert.ToString(i, 2).PadLeft(2, '0'));
                bool inType = (i & 0x2) != 0;
                bool onType = (i & 0x1) != 0;

                CheckPropertyCustomModifiers(property, inType, onType);
                CheckMethodCustomModifiers(
                    property.GetMethod,
                    inReturnType: inType,
                    onReturnType: onType,
                    inParameterType: false,
                    onParameterType: false);
                CheckMethodCustomModifiers(
                    property.SetMethod,
                    inReturnType: false,
                    onReturnType: false,
                    inParameterType: inType,
                    onParameterType: onType);
            }
        }

        /// <summary>
        /// Helper method specifically for TestMethodOverrideCombinations and TestPropertyOverrideCombinations.
        /// </summary>
        /// <param name="method">Must have array return type (or void) and single array parameter (or none).</param>
        /// <param name="inReturnType">True if a custom modifier is expected on the return type array element type.</param>
        /// <param name="onReturnType">True if a custom modifier is expected on the return type.</param>
        /// <param name="inParameterType">True if a custom modifier is expected on the parameter type array element type.</param>
        /// <param name="onParameterType">True if a custom modifier is expected on the parameter type.</param>
        private static void CheckMethodCustomModifiers(MethodSymbol method, bool inReturnType, bool onReturnType, bool inParameterType, bool onParameterType)
        {
            if (!method.ReturnsVoid)
            {
                CheckCustomModifier(inReturnType, ((ArrayTypeSymbol)method.ReturnType).CustomModifiers);
                CheckCustomModifier(onReturnType, method.ReturnTypeCustomModifiers);
            }
            if (method.Parameters.Any())
            {
                CheckCustomModifier(inParameterType, ((ArrayTypeSymbol)method.Parameters.Single().Type).CustomModifiers);
                CheckCustomModifier(onParameterType, method.Parameters.Single().CustomModifiers);
            }
        }

        /// <summary>
        /// Helper method specifically for TestPropertyOverrideCombinations.
        /// </summary>
        /// <param name="property">Must have array type.</param>
        /// <param name="inType">True if a custom modifier is expected on the return type array element type.</param>
        /// <param name="onType">True if a custom modifier is expected on the return type.</param>
        private static void CheckPropertyCustomModifiers(PropertySymbol property, bool inType, bool onType)
        {
            CheckCustomModifier(inType, ((ArrayTypeSymbol)property.Type).CustomModifiers);
            CheckCustomModifier(onType, property.TypeCustomModifiers);
        }

        /// <summary>
        /// True - assert that the list contains a single const modifier.
        /// False - assert that the list is empty.
        /// </summary>
        private static void CheckCustomModifier(bool expectCustomModifier, ImmutableArray<CustomModifier> customModifiers)
        {
            if (expectCustomModifier)
            {
                Assert.Equal(ConstModOptType, customModifiers.Single().Modifier.ToTestDisplayString());
            }
            else
            {
                Assert.False(customModifiers.Any());
            }
        }

        /// <summary>
        /// Test the case of a source type extending a metadata type that could implicitly
        /// implement a metadata interface with custom modifiers.  If the source type does
        /// not implement an interface method, the base method fills in and a bridge method
        /// is synthesized in the source type.  If the source type does implement an interface
        /// method, no bridge method is synthesized.
        /// </summary>
        [Fact]
        public void TestImplicitImplementationInBase()
        {
            var text = @"
class Class1 : CppCli.CppBase2, CppCli.CppInterface1
{
}

class Class2 : CppCli.CppBase2, CppCli.CppInterface1
{
    //copies custom modifiers
    public override void Method1(int x) { }
}

class Class3 : CppCli.CppBase2, CppCli.CppInterface1
{
    //needs a bridge, since custom modifiers are not copied
    public new void Method1(int x) { }
}
";

            var ilAssemblyReference = TestReferences.SymbolsTests.CustomModifiers.CppCli.dll;

            var comp = CreateCompilationWithMscorlib(text, new MetadataReference[] { ilAssemblyReference });
            var global = comp.GlobalNamespace;

            comp.VerifyDiagnostics();

            var baseClass = global.GetMember<NamespaceSymbol>("CppCli").GetMember<NamedTypeSymbol>("CppBase2");

            var class1 = global.GetMember<SourceNamedTypeSymbol>("Class1");

            //both implementations are from the base class
            var class1SynthesizedExplicitImpls = class1.GetSynthesizedExplicitImplementations(CancellationToken.None);
            Assert.Equal(1, class1SynthesizedExplicitImpls.Length); //Don't need a bridge method for the virtual base method.
            foreach (var explicitImpl in class1SynthesizedExplicitImpls)
            {
                Assert.Same(baseClass, explicitImpl.ImplementingMethod.ContainingType);
                AssertAllParametersHaveConstModOpt(explicitImpl, ignoreLast: explicitImpl.MethodKind == MethodKind.PropertySet);
            }

            var class2 = global.GetMember<SourceNamedTypeSymbol>("Class2");

            //Method1 is implemented in the Class2, and no bridge is needed because the custom modifiers are copied
            var class2Method1 = class2.GetMember<MethodSymbol>("Method1");
            AssertAllParametersHaveConstModOpt(class2Method1);

            //Method2 is implemented in the base class
            var class2Method2SynthesizedExplicitImpl = class2.GetSynthesizedExplicitImplementations(CancellationToken.None).Single();
            Assert.Equal("Method2", class2Method2SynthesizedExplicitImpl.ExplicitInterfaceImplementations.Single().Name);
            Assert.Same(baseClass, class2Method2SynthesizedExplicitImpl.ImplementingMethod.ContainingType);
            AssertAllParametersHaveConstModOpt(class2Method2SynthesizedExplicitImpl);

            var class3 = global.GetMember<SourceNamedTypeSymbol>("Class3");

            //Method1 is implemented in Class3, but a bridge is needed because custom modifiers are not copied
            var class3Method1 = class3.GetMember<MethodSymbol>("Method1");
            Assert.False(class3Method1.Parameters.Single().CustomModifiers.Any());

            // GetSynthesizedExplicitImplementations doesn't guarantee order, so sort to make the asserts easier to write.

            var class3SynthesizedExplicitImpls = (from m in class3.GetSynthesizedExplicitImplementations(CancellationToken.None) orderby m.Name select m).ToArray();
            Assert.Equal(2, class3SynthesizedExplicitImpls.Length);

            var class3Method1SynthesizedExplicitImpl = class3SynthesizedExplicitImpls[0];
            Assert.Equal("Method1", class3Method1SynthesizedExplicitImpl.ExplicitInterfaceImplementations.Single().Name);
            Assert.Same(class3Method1, class3Method1SynthesizedExplicitImpl.ImplementingMethod);
            AssertAllParametersHaveConstModOpt(class3Method1SynthesizedExplicitImpl);

            //Method2 is implemented in the base class
            var class3Method2SynthesizedExplicitImpl = class3SynthesizedExplicitImpls[1];
            Assert.Equal("Method2", class3Method2SynthesizedExplicitImpl.ExplicitInterfaceImplementations.Single().Name);
            Assert.Same(baseClass, class3Method2SynthesizedExplicitImpl.ImplementingMethod.ContainingType);
            AssertAllParametersHaveConstModOpt(class3Method2SynthesizedExplicitImpl);
        }

        /// <summary>
        /// Test copying more than one custom modifier on the same element
        /// </summary>
        [Fact]
        public void TestCopyMultipleCustomModifiers()
        {
            var text = @"
class Class : I2
{
    //copy (both) modifiers (even though dev10 doesn't)
    void I2.M1(int x) { }
}
";

            var ilAssemblyReference = TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll;

            var comp = CreateCompilationWithMscorlib(text, new MetadataReference[] { ilAssemblyReference });
            var global = comp.GlobalNamespace;

            comp.VerifyDiagnostics();

            var @class = global.GetMember<SourceNamedTypeSymbol>("Class");

            // explicit implementation copies custom modifiers
            var classMethod1 = @class.GetMethod("I2.M1");
            var classMethod1CustomModifiers = classMethod1.Parameters.Single().CustomModifiers;
            Assert.Equal(2, classMethod1CustomModifiers.Length);
            foreach (var customModifier in classMethod1CustomModifiers)
            {
                Assert.Equal(ConstModOptType, customModifier.Modifier.ToTestDisplayString());
            }

            //no bridge methods
            Assert.False(@class.GetSynthesizedExplicitImplementations(CancellationToken.None).Any());
        }

        /// <summary>
        /// The params keyword is inherited from the overridden method in the same way as
        /// a custom modifier.
        /// </summary>
        [Fact]
        public void TestParamsKeyword()
        {
            var text = @"
public class Base
{
    public virtual void M(params int[] a) { }
    public virtual void N(int[] a) { }
}

public class Derived : Base
{
    public override void M(int[] a) { } //lost 'params'
    public override void N(params int[] a) { } //gained 'params'
}

public class Derived2 : Derived
{
    public override void M(params int[] a) { } //regained 'params'
    public override void N(int[] a) { } //(re)lost 'params'
}
";

            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;

            var baseClass = global.GetMember<NamedTypeSymbol>("Base");
            var baseM = baseClass.GetMember<MethodSymbol>("M");
            var baseN = baseClass.GetMember<MethodSymbol>("N");

            var derivedClass = global.GetMember<NamedTypeSymbol>("Derived");
            var derivedM = derivedClass.GetMember<MethodSymbol>("M");
            var derivedN = derivedClass.GetMember<MethodSymbol>("N");

            var derived2Class = global.GetMember<NamedTypeSymbol>("Derived2");
            var derived2M = derived2Class.GetMember<MethodSymbol>("M");
            var derived2N = derived2Class.GetMember<MethodSymbol>("N");

            Assert.True(baseM.Parameters.Single().IsParams, "Base.M.IsParams should be true");
            Assert.False(baseN.Parameters.Single().IsParams, "Base.N.IsParams should be false");
            Assert.True(derivedM.Parameters.Single().IsParams, "Derived.M.IsParams should be true"); //NB: does not reflect source
            Assert.False(derivedN.Parameters.Single().IsParams, "Derived.N.IsParams should be false"); //NB: does not reflect source
            Assert.True(derived2M.Parameters.Single().IsParams, "Derived2.M.IsParams should be true");
            Assert.False(derived2N.Parameters.Single().IsParams, "Derived2.N.IsParams should be false");
        }

        /// <summary>
        /// Test implementing a single interface with custom modifiers.
        /// </summary>
        [Fact]
        public void TestIndexerExplicitInterfaceImplementation()
        {
            var text = @"
class Explicit : CppCli.CppIndexerInterface
{
    int CppCli.CppIndexerInterface.this[int x]
    {
        get { return 0; }
        set { }
    }
}
";

            var ilAssemblyReference = TestReferences.SymbolsTests.CustomModifiers.CppCli.dll;

            var comp = CreateCompilationWithMscorlib(text, new MetadataReference[] { ilAssemblyReference });
            var global = comp.GlobalNamespace;

            comp.VerifyDiagnostics();

            var @class = global.GetMember<SourceNamedTypeSymbol>("Explicit");

            // explicit implementation copies custom modifiers
            var classIndexer = (PropertySymbol)@class.GetMembers().Where(s => s.Kind == SymbolKind.Property).Single();
            AssertAllParametersHaveConstModOpt(classIndexer);

            Assert.Equal(0, @class.GetSynthesizedExplicitImplementations(CancellationToken.None).Length);
        }

        /// <summary>
        /// Test implementing a single interface with custom modifiers.
        /// </summary>
        [Fact]
        public void TestIndexerImplicitInterfaceImplementation()
        {
            var text = @"
class Implicit : CppCli.CppIndexerInterface
{
    public int this[int x]
    {
        get { return 0; }
        set { }
    }
}
";

            var ilAssemblyReference = TestReferences.SymbolsTests.CustomModifiers.CppCli.dll;

            var comp = CreateCompilationWithMscorlib(text, new MetadataReference[] { ilAssemblyReference });
            var global = comp.GlobalNamespace;

            comp.VerifyDiagnostics();

            var @class = global.GetMember<SourceNamedTypeSymbol>("Implicit");

            // implicit implementation does not copy custom modifiers
            var classIndexer = (PropertySymbol)@class.GetMembers().Where(s => s.Kind == SymbolKind.Property).Single();
            AssertNoParameterHasModOpts(classIndexer);

            // bridge methods for implicit implementations have custom modifiers
            var explicitImpls = @class.GetSynthesizedExplicitImplementations(CancellationToken.None);
            Assert.Equal(2, explicitImpls.Length);

            var explicitGetterImpl = explicitImpls.Where(impl => impl.ImplementingMethod.MethodKind == MethodKind.PropertyGet).Single();
            AssertAllParametersHaveConstModOpt(explicitGetterImpl);

            var explicitSetterImpl = explicitImpls.Where(impl => impl.ImplementingMethod.MethodKind == MethodKind.PropertySet).Single();
            AssertAllParametersHaveConstModOpt(explicitSetterImpl, ignoreLast: true);
        }

        /// <summary>
        /// Test overriding a base type indexer with custom modifiers.
        /// </summary>
        [Fact]
        public void TestOverrideIndexer()
        {
            var text = @"
class Override : CppCli.CppIndexerBase
{
    public override int this[int x]
    {
        get { return 0; }
        set { }
    }
}
";

            var ilAssemblyReference = TestReferences.SymbolsTests.CustomModifiers.CppCli.dll;

            var comp = CreateCompilationWithMscorlib(text, new MetadataReference[] { ilAssemblyReference });
            var global = comp.GlobalNamespace;

            comp.VerifyDiagnostics();

            var @class = global.GetMember<SourceNamedTypeSymbol>("Override");

            // implicit implementation does not copy custom modifiers
            var classIndexer = (PropertySymbol)@class.GetMembers().Where(s => s.Kind == SymbolKind.Property).Single();
            AssertAllParametersHaveConstModOpt(classIndexer);

            Assert.Equal(0, @class.GetSynthesizedExplicitImplementations(CancellationToken.None).Length);
        }

        /// <summary>
        /// The params keyword is inherited from the overridden indexer in the same way as
        /// a custom modifier.
        /// </summary>
        [Fact]
        public void TestParamsKeywordOnIndexer()
        {
            var text = @"
public class Base
{
    public virtual char this[params int[] a] { set { } }
    public virtual char this[long[] a] { set { } }
}

public class Derived : Base
{
    public override char this[int[] a] { set { } } //lost 'params'
    public override char this[params long[] a] { set { } } //gained 'params'
}

public class Derived2 : Derived
{
    public override char this[params int[] a] { set { } } //regained 'params'
    public override char this[long[] a] { set { } } //(re)lost 'params'
}
";

            var comp = CreateCompilationWithMscorlib(text);
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;

            var baseClass = global.GetMember<NamedTypeSymbol>("Base");
            var baseIndexer1 = (PropertySymbol)baseClass.GetMembers().Where(IsPropertyWithSingleParameter(SpecialType.System_Int32, isArrayType: true)).Single();
            var baseIndexer2 = (PropertySymbol)baseClass.GetMembers().Where(IsPropertyWithSingleParameter(SpecialType.System_Int64, isArrayType: true)).Single();

            var derivedClass = global.GetMember<NamedTypeSymbol>("Derived");
            var derivedIndexer1 = (PropertySymbol)derivedClass.GetMembers().Where(IsPropertyWithSingleParameter(SpecialType.System_Int32, isArrayType: true)).Single();
            var derivedIndexer2 = (PropertySymbol)derivedClass.GetMembers().Where(IsPropertyWithSingleParameter(SpecialType.System_Int64, isArrayType: true)).Single();

            var derived2Class = global.GetMember<NamedTypeSymbol>("Derived2");
            var derived2Indexer1 = (PropertySymbol)derived2Class.GetMembers().Where(IsPropertyWithSingleParameter(SpecialType.System_Int32, isArrayType: true)).Single();
            var derived2Indexer2 = (PropertySymbol)derived2Class.GetMembers().Where(IsPropertyWithSingleParameter(SpecialType.System_Int64, isArrayType: true)).Single();

            Assert.True(baseIndexer1.Parameters.Single().IsParams, "Base.Indexer1.IsParams should be true");
            Assert.False(baseIndexer2.Parameters.Single().IsParams, "Base.Indexer2.IsParams should be false");
            Assert.True(derivedIndexer1.Parameters.Single().IsParams, "Derived.Indexer1.IsParams should be true"); //Indexer2B: does not reflect source
            Assert.False(derivedIndexer2.Parameters.Single().IsParams, "Derived.Indexer2.IsParams should be false"); //Indexer2B: does not reflect source
            Assert.True(derived2Indexer1.Parameters.Single().IsParams, "Derived2.Indexer1.IsParams should be true");
            Assert.False(derived2Indexer2.Parameters.Single().IsParams, "Derived2.Indexer2.IsParams should be false");
        }

        [ClrOnlyFact]
        [WorkItem(819774, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819774")]
        public void Repro819774()
        {
            var il = @"
.class interface public abstract auto ansi IBug813305
{
  .method public hidebysig newslot abstract virtual 
          instance void  M(object modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
  } // end of method IBug813305::M
} // end of class IBug813305
";

            var source = @"
using System.Collections.Generic;
 
class Bug813305 : IBug813305
{
    void IBug813305.M(dynamic x)
    {
        x.Foo();
        System.Console.WriteLine(""Bug813305.M"");
    }
 
    public void Foo() {}
}
 
class Test
{
    static void Main()
    {
        IBug813305 x = new Bug813305();
        x.M(x);
    }
}
";
            var comp = CreateCompilationWithCustomILSource(source, il,
                options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All),
                references: new[] { CSharpRef, SystemCoreRef });

            CompileAndVerify(comp, expectedOutput: "Bug813305.M",
                symbolValidator: m =>
                {
                    var Bug813305 = m.GlobalNamespace.GetTypeMember("Bug813305");
                    var method = Bug813305.GetMethod("IBug813305.M");
                    Assert.Equal("Bug813305.IBug813305.M(dynamic)", method.ToDisplayString());
                });
        }

        [Fact]
        [WorkItem(819774, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819774")]
        public void ObjectToDynamic_ImplementationParameter()
        {
            var il = @"
.class interface public abstract auto ansi I
{
  .method public hidebysig newslot abstract virtual 
          instance void  M(object modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
  }
}
";

            var source = @"
class C : I
{
    void I.M(dynamic x) { }
}
";
            var comp = CreateCompilationWithCustomILSource(source, il, new[] { SystemCoreRef });
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;
            var interfaceMethod = global.GetMember<NamedTypeSymbol>("I").GetMember<MethodSymbol>("M");
            var classMethod = global.GetMember<NamedTypeSymbol>("C").GetMethod("I.M");

            Assert.Equal(SpecialType.System_Object, interfaceMethod.ParameterTypes.Single().SpecialType);
            Assert.Equal(TypeKind.Dynamic, classMethod.ParameterTypes.Single().TypeKind);

            Assert.Equal("void C.I.M(dynamic modopt(System.Runtime.CompilerServices.IsLong) x)", classMethod.ToTestDisplayString());
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        [WorkItem(819774, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819774")]
        public void DynamicToObject_ImplementationParameter()
        {
            var il = @"
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core {}

.assembly '<<GeneratedFileName>>'
{
}

.class interface public abstract auto ansi I
{
  .method public hidebysig newslot abstract virtual 
          instance void  M(object modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
    .param [1]
    .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[])
             = {bool[2](false true)}
  }
}
";

            var source = @"
class C : I
{
    void I.M(object x) { }
}
";
            var ilRef = CompileIL(il, appendDefaultHeader: false);
            var comp = CreateCompilation(source, new[] { MscorlibRef, SystemCoreRef, ilRef });
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;
            var interfaceMethod = global.GetMember<MethodSymbol>("I.M");
            var classMethod = global.GetMember<NamedTypeSymbol>("C").GetMethod("I.M");

            Assert.Equal(TypeKind.Dynamic, interfaceMethod.ParameterTypes.Single().TypeKind);
            Assert.Equal(SpecialType.System_Object, classMethod.ParameterTypes.Single().SpecialType);

            Assert.Equal("void C.I.M(System.Object modopt(System.Runtime.CompilerServices.IsLong) x)", classMethod.ToTestDisplayString());
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        [WorkItem(819774, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819774")]
        public void DynamicToObject_ImplementationReturn()
        {
            var il = @"
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core {}

.assembly '<<GeneratedFileName>>'
{
}

.class interface public abstract auto ansi I
{
  .method public hidebysig newslot abstract virtual 
          instance object modopt(int32) M() cil managed
  {
    .param [0]
    .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[])
             = {bool[2](false true)}
  }
}
";

            var source = @"
class C : I
{
    object I.M() { throw null; }
}
";
            var ilRef = CompileIL(il, appendDefaultHeader: false);
            var comp = CreateCompilation(source, new[] { MscorlibRef, SystemCoreRef, ilRef });
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;
            var interfaceMethod = global.GetMember<MethodSymbol>("I.M");
            var classMethod = global.GetMember<NamedTypeSymbol>("C").GetMethod("I.M");

            Assert.Equal(TypeKind.Dynamic, interfaceMethod.ReturnType.TypeKind);
            Assert.Equal(SpecialType.System_Object, classMethod.ReturnType.SpecialType);

            Assert.Equal("System.Object modopt(System.Int32) C.I.M()", classMethod.ToTestDisplayString());
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        [WorkItem(819774, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819774")]
        public void ObjectToDynamic_ImplementationReturn()
        {
            var il = @"
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }

.assembly '<<GeneratedFileName>>'
{
}

.class interface public abstract auto ansi I
{
  .method public hidebysig newslot abstract virtual 
          instance object modopt(int32) M() cil managed
  {
  }
}
";

            var source = @"
class C : I
{
    dynamic I.M() { throw null; }
}
";
            var ilRef = CompileIL(il, appendDefaultHeader: false);
            var comp = CreateCompilation(source, new[] { MscorlibRef, SystemCoreRef, ilRef });
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;
            var interfaceMethod = global.GetMember<NamedTypeSymbol>("I").GetMember<MethodSymbol>("M");
            var classMethod = global.GetMember<NamedTypeSymbol>("C").GetMethod("I.M");

            Assert.Equal(SpecialType.System_Object, interfaceMethod.ReturnType.SpecialType);
            Assert.Equal(TypeKind.Dynamic, classMethod.ReturnType.TypeKind);

            Assert.Equal("dynamic modopt(System.Int32) C.I.M()", classMethod.ToTestDisplayString());
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        [WorkItem(819774, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819774")]
        public void DynamicVsObjectComplexParameter()
        {
            var il = @"
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core {}

.assembly '<<GeneratedFileName>>'
{
}

.class interface public abstract auto ansi I`2<T,U>
{
  .method public hidebysig newslot abstract virtual 
          instance void  M(class I`2<object modopt(int16) [],object modopt(int32) []>& modopt(int64) c) cil managed
  {
    .param [1]
    .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[])
             = {bool[9](false false false false false false false false true)} // i.e. second occurrence of 'object' is really 'dynamic'.
  }

}
";

            var source = @"
public class C : I<byte, char>
{
    void I<byte, char>.M(ref I<dynamic[], object[]> c) { } // object to dynamic and vice versa
}
";
            var ilRef = CompileIL(il, appendDefaultHeader: false);
            var comp = CreateCompilation(source, new[] { MscorlibRef, SystemCoreRef, ilRef });
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;
            var interfaceMethod = global.GetMember<NamedTypeSymbol>("I").GetMember<MethodSymbol>("M");
            var classMethod = global.GetMember<NamedTypeSymbol>("C").GetMembers().OfType<MethodSymbol>().Single(
                m => m.MethodKind == MethodKind.ExplicitInterfaceImplementation);

            Assert.Equal("void I<T, U>.M(ref modopt(System.Int64) I<System.Object modopt(System.Int16) [], dynamic modopt(System.Int32) []> c)", interfaceMethod.ToTestDisplayString());
            Assert.Equal("void C.I<System.Byte, System.Char>.M(ref modopt(System.Int64) I<dynamic modopt(System.Int16) [], System.Object modopt(System.Int32) []> c)", classMethod.ToTestDisplayString());
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        [WorkItem(819774, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819774")]
        public void DynamicVsObjectComplexReturn()
        {
            var il = @"
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core {}

.assembly '<<GeneratedFileName>>'
{
}

.class interface public abstract auto ansi I`2<T,U>
{
  .method public hidebysig newslot abstract virtual 
          instance class I`2<object modopt(int16) [],object modopt(int32) []> modopt(int64)  M() cil managed
  {
    .param [0]
    .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[])
             = {bool[8](false false false false false false false true)} // i.e. second occurrence of 'object' is really 'dynamic'.
  }

}
";

            var source = @"
public class C : I<byte, char>
{
    I<dynamic[], object[]> I<byte, char>.M() { throw null; } // object to dynamic and vice versa
}
";
            var ilRef = CompileIL(il, appendDefaultHeader: false);
            var comp = CreateCompilation(source, new[] { MscorlibRef, SystemCoreRef, ilRef });
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;
            var interfaceMethod = global.GetMember<NamedTypeSymbol>("I").GetMember<MethodSymbol>("M");
            var classMethod = global.GetMember<NamedTypeSymbol>("C").GetMembers().OfType<MethodSymbol>().Single(
                m => m.MethodKind == MethodKind.ExplicitInterfaceImplementation);

            Assert.Equal("I<System.Object modopt(System.Int16) [], dynamic modopt(System.Int32) []> modopt(System.Int64) I<T, U>.M()", interfaceMethod.ToTestDisplayString());
            Assert.Equal("I<dynamic modopt(System.Int16) [], System.Object modopt(System.Int32) []> modopt(System.Int64) C.I<System.Byte, System.Char>.M()", classMethod.ToTestDisplayString());
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        [WorkItem(819774, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819774")]
        public void DynamicToObjectAndViceVersa_OverrideParameter()
        {
            var il = @"
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core {}

.assembly '<<GeneratedFileName>>'
{
}

.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance void  M(object modopt(int16) o,
                           object modopt(int32) d) cil managed
  {
    .param [2]
    .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[])
             = {bool[2](false true)}
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

            var source = @"
class Derived : Base
{
    public override void M(dynamic o, object d) { }
}
";
            var ilRef = CompileIL(il, appendDefaultHeader: false);
            var comp = CreateCompilation(source, new[] { MscorlibRef, SystemCoreRef, ilRef });
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;
            var baseMethod = global.GetMember<NamedTypeSymbol>("Base").GetMember<MethodSymbol>("M");
            var derivedMethod = global.GetMember<NamedTypeSymbol>("Derived").GetMember<MethodSymbol>("M");

            Assert.Equal("void Base.M(System.Object modopt(System.Int16) o, dynamic modopt(System.Int32) d)", baseMethod.ToTestDisplayString());
            Assert.Equal("void Derived.M(dynamic modopt(System.Int16) o, System.Object modopt(System.Int32) d)", derivedMethod.ToTestDisplayString());
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        [WorkItem(819774, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819774")]
        public void DynamicToObject_OverrideReturn()
        {
            var il = @"
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core {}

.assembly '<<GeneratedFileName>>'
{
}

.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance object modopt(int32)  M() cil managed
  {
    .param [0]
    .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[])
             = {bool[2](false true)}
    ldnull
    throw
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

            var source = @"
class Derived : Base
{
    public override object M() { throw null; }
}
";
            var ilRef = CompileIL(il, appendDefaultHeader: false);
            var comp = CreateCompilation(source, new[] { MscorlibRef, SystemCoreRef, ilRef });
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;
            var baseMethod = global.GetMember<NamedTypeSymbol>("Base").GetMember<MethodSymbol>("M");
            var derivedMethod = global.GetMember<NamedTypeSymbol>("Derived").GetMember<MethodSymbol>("M");

            Assert.Equal("dynamic modopt(System.Int32) Base.M()", baseMethod.ToTestDisplayString());
            Assert.Equal("System.Object modopt(System.Int32) Derived.M()", derivedMethod.ToTestDisplayString());
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        [WorkItem(819774, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819774")]
        public void ObjectToDynamic_OverrideReturn()
        {
            var il = @"
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }

.assembly '<<GeneratedFileName>>'
{
}

.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance object modopt(int32)  M() cil managed
  {
    ldnull
    throw
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

            var source = @"
class Derived : Base
{
    public override dynamic M() { throw null; }
}
";
            var ilRef = CompileIL(il, appendDefaultHeader: false);
            var comp = CreateCompilation(source, new[] { MscorlibRef, SystemCoreRef, ilRef });
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;
            var baseMethod = global.GetMember<NamedTypeSymbol>("Base").GetMember<MethodSymbol>("M");
            var derivedMethod = global.GetMember<NamedTypeSymbol>("Derived").GetMember<MethodSymbol>("M");

            Assert.Equal("System.Object modopt(System.Int32) Base.M()", baseMethod.ToTestDisplayString());
            Assert.Equal("dynamic modopt(System.Int32) Derived.M()", derivedMethod.ToTestDisplayString());
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        [WorkItem(830632, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/830632")]
        public void AccessorsAddCustomModifiers_Override()
        {
            var il = @"
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }

.assembly '<<GeneratedFileName>>'
{
}

.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Item')}
  .method public hidebysig newslot specialname virtual instance char modopt(int8)
          get_P() cil managed
  {
    ldnull
    ret
  }

  .method public hidebysig newslot specialname virtual instance void 
          set_P(char modopt(int16) 'value') cil managed
  {
    ret
  }

  .method public hidebysig newslot specialname virtual instance int32 modopt(int8)
          get_Item(bool modopt(int16) x) cil managed
  {
    ldnull
    ret
  }

  .method public hidebysig newslot specialname virtual instance void 
          set_Item(bool modopt(int32) x,
                   int32 modopt(int64) 'value') cil managed
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

  .property instance char P()
  {
    .get instance char modopt(int8) Base::get_P()
    .set instance void Base::set_P(char modopt(int16))
  }

  .property instance int32 Item(bool)
  {
    .get instance int32 modopt(int8) Base::get_Item(bool modopt(int16))
    .set instance void Base::set_Item(bool modopt(int32),
                                      int32  modopt(int64))
  }
} // end of class Base
";

            var source = @"
class Derived : Base
{
    public override char P { get { return 'a'; } set { } }
    public override int this[bool x] { get { return 0; } set { } }
}
";
            var ilRef = CompileIL(il, appendDefaultHeader: false);
            var comp = CreateCompilation(source, new[] { MscorlibRef, SystemCoreRef, ilRef });
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;

            var baseType = global.GetMember<NamedTypeSymbol>("Base");
            var baseProperty = baseType.GetMember<PropertySymbol>("P");
            var baseIndexer = baseType.GetMember<PropertySymbol>(WellKnownMemberNames.Indexer);

            var derivedType = global.GetMember<NamedTypeSymbol>("Derived");
            var derivedProperty = derivedType.GetMember<PropertySymbol>("P");
            var derivedIndexer = derivedType.GetMember<PropertySymbol>(WellKnownMemberNames.Indexer);

            var int8Type = comp.GetSpecialType(SpecialType.System_SByte);
            var int16Type = comp.GetSpecialType(SpecialType.System_Int16);
            var int32Type = comp.GetSpecialType(SpecialType.System_Int32);
            var int64Type = comp.GetSpecialType(SpecialType.System_Int64);

            // None of the properties have custom modifiers - only the accessors do.
            Assert.Equal(0, baseProperty.CustomModifierCount());
            Assert.Equal(0, baseIndexer.CustomModifierCount());
            Assert.Equal(0, derivedProperty.CustomModifierCount());
            Assert.Equal(0, derivedIndexer.CustomModifierCount());

            Assert.Equal(int8Type, baseProperty.GetMethod.ReturnTypeCustomModifiers.Single().Modifier);
            Assert.Equal(int8Type, derivedProperty.GetMethod.ReturnTypeCustomModifiers.Single().Modifier);

            Assert.Equal(int16Type, baseProperty.SetMethod.Parameters.Single().CustomModifiers.Single().Modifier);
            Assert.Equal(int16Type, derivedProperty.SetMethod.Parameters.Single().CustomModifiers.Single().Modifier);

            Assert.Equal(int8Type, baseIndexer.GetMethod.ReturnTypeCustomModifiers.Single().Modifier);
            Assert.Equal(int8Type, derivedIndexer.GetMethod.ReturnTypeCustomModifiers.Single().Modifier);

            Assert.Equal(int16Type, baseIndexer.GetMethod.Parameters.Single().CustomModifiers.Single().Modifier);
            Assert.Equal(int16Type, derivedIndexer.GetMethod.Parameters.Single().CustomModifiers.Single().Modifier);

            Assert.Equal(int32Type, baseIndexer.SetMethod.Parameters[0].CustomModifiers.Single().Modifier);
            Assert.Equal(int32Type, derivedIndexer.SetMethod.Parameters[0].CustomModifiers.Single().Modifier);

            Assert.Equal(int64Type, baseIndexer.SetMethod.Parameters[1].CustomModifiers.Single().Modifier);
            Assert.Equal(int64Type, derivedIndexer.SetMethod.Parameters[1].CustomModifiers.Single().Modifier);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        [WorkItem(830632, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/830632")]
        public void AccessorsRemoveCustomModifiers_Override()
        {
            var il = @"
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }

.assembly '<<GeneratedFileName>>'
{
}

.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Item')}
  .method public hidebysig newslot specialname virtual instance char
          get_P() cil managed
  {
    ldnull
    ret
  }

  .method public hidebysig newslot specialname virtual instance void 
          set_P(char 'value') cil managed
  {
    ret
  }

  .method public hidebysig newslot specialname virtual instance int32
          get_Item(bool x) cil managed
  {
    ldnull
    ret
  }

  .method public hidebysig newslot specialname virtual instance void 
          set_Item(bool x,
                   int32 'value') cil managed
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

  .property instance char  modopt(int8) P()
  {
    .get instance char Base::get_P()
    .set instance void Base::set_P(char)
  }

  .property instance int32 modopt(int8) Item(bool  modopt(int16))
  {
    .get instance int32 Base::get_Item(bool)
    .set instance void Base::set_Item(bool,
                                      int32)
  }
} // end of class Base
";

            var source = @"
class Derived : Base
{
    public override char P { get { return 'a'; } set { } }
    public override int this[bool x] { get { return 0; } set { } }
}
";
            var ilRef = CompileIL(il, appendDefaultHeader: false);
            var comp = CreateCompilation(source, new[] { MscorlibRef, SystemCoreRef, ilRef });
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;

            var baseType = global.GetMember<NamedTypeSymbol>("Base");
            var baseProperty = baseType.GetMember<PropertySymbol>("P");
            var baseIndexer = baseType.GetMember<PropertySymbol>(WellKnownMemberNames.Indexer);

            var derivedType = global.GetMember<NamedTypeSymbol>("Derived");
            var derivedProperty = derivedType.GetMember<PropertySymbol>("P");
            var derivedIndexer = derivedType.GetMember<PropertySymbol>(WellKnownMemberNames.Indexer);

            var int8Type = comp.GetSpecialType(SpecialType.System_SByte);
            var int16Type = comp.GetSpecialType(SpecialType.System_Int16);

            // None of the accessors have custom modifiers - only the properties do.
            Assert.Equal(0, baseProperty.GetMethod.CustomModifierCount());
            Assert.Equal(0, baseProperty.SetMethod.CustomModifierCount());
            Assert.Equal(0, baseIndexer.GetMethod.CustomModifierCount());
            Assert.Equal(0, baseIndexer.SetMethod.CustomModifierCount());
            Assert.Equal(0, derivedProperty.GetMethod.CustomModifierCount());
            Assert.Equal(0, derivedProperty.SetMethod.CustomModifierCount());
            Assert.Equal(0, derivedIndexer.GetMethod.CustomModifierCount());
            Assert.Equal(0, derivedIndexer.SetMethod.CustomModifierCount());

            Assert.Equal(int8Type, baseProperty.TypeCustomModifiers.Single().Modifier);
            Assert.Equal(int8Type, derivedProperty.TypeCustomModifiers.Single().Modifier);

            Assert.Equal(int8Type, baseIndexer.TypeCustomModifiers.Single().Modifier);
            Assert.Equal(int8Type, derivedIndexer.TypeCustomModifiers.Single().Modifier);

            Assert.Equal(int16Type, baseIndexer.Parameters.Single().CustomModifiers.Single().Modifier);
            Assert.Equal(int16Type, derivedIndexer.Parameters.Single().CustomModifiers.Single().Modifier);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        [WorkItem(830632, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/830632")]
        public void AccessorsAddCustomModifiers_ExplicitImplementation()
        {
            var il = @"
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }

.assembly '<<GeneratedFileName>>'
{
}

.class interface public abstract auto ansi I
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Item')}
  .method public hidebysig newslot specialname abstract virtual instance char modopt(int8)
          get_P() cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual instance void 
          set_P(char modopt(int16) 'value') cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual instance int32 modopt(int8)
          get_Item(bool modopt(int16) x) cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual instance void 
          set_Item(bool modopt(int32) x,
                   int32 modopt(int64) 'value') cil managed
  {
  }

  .property instance char P()
  {
    .get instance char modopt(int8) I::get_P()
    .set instance void I::set_P(char modopt(int16))
  }

  .property instance int32 Item(bool)
  {
    .get instance int32 modopt(int8) I::get_Item(bool modopt(int16))
    .set instance void I::set_Item(bool modopt(int32),
                                      int32  modopt(int64))
  }
} // end of class Base
";

            var source = @"
class Implementation : I
{
    char I.P { get { return 'a'; } set { } }
    int I.this[bool x] { get { return 0; } set { } }
}
";
            var ilRef = CompileIL(il, appendDefaultHeader: false);
            var comp = CreateCompilation(source, new[] { MscorlibRef, SystemCoreRef, ilRef });
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;

            var interfaceType = global.GetMember<NamedTypeSymbol>("I");
            var interfaceProperty = interfaceType.GetMember<PropertySymbol>("P");
            var interfaceIndexer = interfaceType.GetMember<PropertySymbol>(WellKnownMemberNames.Indexer);

            var implementationType = global.GetMember<NamedTypeSymbol>("Implementation");
            var implementationProperty = (PropertySymbol)implementationType.FindImplementationForInterfaceMember(interfaceProperty);
            var implementationIndexer = (PropertySymbol)implementationType.FindImplementationForInterfaceMember(interfaceIndexer);

            var int8Type = comp.GetSpecialType(SpecialType.System_SByte);
            var int16Type = comp.GetSpecialType(SpecialType.System_Int16);
            var int32Type = comp.GetSpecialType(SpecialType.System_Int32);
            var int64Type = comp.GetSpecialType(SpecialType.System_Int64);

            // None of the properties have custom modifiers - only the accessors do.
            Assert.Equal(0, interfaceProperty.CustomModifierCount());
            Assert.Equal(0, interfaceIndexer.CustomModifierCount());
            Assert.Equal(0, implementationProperty.CustomModifierCount());
            Assert.Equal(0, implementationIndexer.CustomModifierCount());

            Assert.Equal(int8Type, interfaceProperty.GetMethod.ReturnTypeCustomModifiers.Single().Modifier);
            Assert.Equal(int8Type, implementationProperty.GetMethod.ReturnTypeCustomModifiers.Single().Modifier);

            Assert.Equal(int16Type, interfaceProperty.SetMethod.Parameters.Single().CustomModifiers.Single().Modifier);
            Assert.Equal(int16Type, implementationProperty.SetMethod.Parameters.Single().CustomModifiers.Single().Modifier);

            Assert.Equal(int8Type, interfaceIndexer.GetMethod.ReturnTypeCustomModifiers.Single().Modifier);
            Assert.Equal(int8Type, implementationIndexer.GetMethod.ReturnTypeCustomModifiers.Single().Modifier);

            Assert.Equal(int16Type, interfaceIndexer.GetMethod.Parameters.Single().CustomModifiers.Single().Modifier);
            Assert.Equal(int16Type, implementationIndexer.GetMethod.Parameters.Single().CustomModifiers.Single().Modifier);

            Assert.Equal(int32Type, interfaceIndexer.SetMethod.Parameters[0].CustomModifiers.Single().Modifier);
            Assert.Equal(int32Type, implementationIndexer.SetMethod.Parameters[0].CustomModifiers.Single().Modifier);

            Assert.Equal(int64Type, interfaceIndexer.SetMethod.Parameters[1].CustomModifiers.Single().Modifier);
            Assert.Equal(int64Type, implementationIndexer.SetMethod.Parameters[1].CustomModifiers.Single().Modifier);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        [WorkItem(830632, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/830632")]
        public void AccessorsRemoveCustomModifiers_ExplicitImplementation()
        {
            var il = @"
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }

.assembly '<<GeneratedFileName>>'
{
}

.class interface public abstract auto ansi I
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Item')}
  .method public hidebysig newslot specialname abstract virtual instance char
          get_P() cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual instance void 
          set_P(char 'value') cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual instance int32
          get_Item(bool x) cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual instance void 
          set_Item(bool x,
                   int32 'value') cil managed
  {
  }

  .property instance char  modopt(int8) P()
  {
    .get instance char I::get_P()
    .set instance void I::set_P(char)
  }

  .property instance int32 modopt(int8) Item(bool  modopt(int16))
  {
    .get instance int32 I::get_Item(bool)
    .set instance void I::set_Item(bool,
                                      int32)
  }
} // end of class Base
";

            var source = @"
class Implementation : I
{
    char I.P { get { return 'a'; } set { } }
    int I.this[bool x] { get { return 0; } set { } }
}
";
            var ilRef = CompileIL(il, appendDefaultHeader: false);
            var comp = CreateCompilation(source, new[] { MscorlibRef, SystemCoreRef, ilRef });
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;

            var interfaceType = global.GetMember<NamedTypeSymbol>("I");
            var interfaceProperty = interfaceType.GetMember<PropertySymbol>("P");
            var interfaceIndexer = interfaceType.GetMember<PropertySymbol>(WellKnownMemberNames.Indexer);

            var implementationType = global.GetMember<NamedTypeSymbol>("Implementation");
            var implementationProperty = (PropertySymbol)implementationType.FindImplementationForInterfaceMember(interfaceProperty);
            var implementationIndexer = (PropertySymbol)implementationType.FindImplementationForInterfaceMember(interfaceIndexer);

            var int8Type = comp.GetSpecialType(SpecialType.System_SByte);
            var int16Type = comp.GetSpecialType(SpecialType.System_Int16);

            // None of the accessors have custom modifiers - only the properties do.
            Assert.Equal(0, interfaceProperty.GetMethod.CustomModifierCount());
            Assert.Equal(0, interfaceProperty.SetMethod.CustomModifierCount());
            Assert.Equal(0, interfaceIndexer.GetMethod.CustomModifierCount());
            Assert.Equal(0, interfaceIndexer.SetMethod.CustomModifierCount());
            Assert.Equal(0, implementationProperty.GetMethod.CustomModifierCount());
            Assert.Equal(0, implementationProperty.SetMethod.CustomModifierCount());
            Assert.Equal(0, implementationIndexer.GetMethod.CustomModifierCount());
            Assert.Equal(0, implementationIndexer.SetMethod.CustomModifierCount());

            Assert.Equal(int8Type, interfaceProperty.TypeCustomModifiers.Single().Modifier);
            Assert.Equal(int8Type, implementationProperty.TypeCustomModifiers.Single().Modifier);

            Assert.Equal(int8Type, interfaceIndexer.TypeCustomModifiers.Single().Modifier);
            Assert.Equal(int8Type, implementationIndexer.TypeCustomModifiers.Single().Modifier);

            Assert.Equal(int16Type, interfaceIndexer.Parameters.Single().CustomModifiers.Single().Modifier);
            Assert.Equal(int16Type, implementationIndexer.Parameters.Single().CustomModifiers.Single().Modifier);
        }

        private static Func<Symbol, bool> IsPropertyWithSingleParameter(SpecialType paramSpecialType, bool isArrayType = false)
        {
            return s =>
            {
                if (s.Kind != SymbolKind.Property)
                {
                    return false;
                }
                var paramType = s.GetParameters().Single().Type;
                var comparisonType = isArrayType ? ((ArrayTypeSymbol)paramType).ElementType : paramType;
                return comparisonType.SpecialType == paramSpecialType;
            };
        }

        private static void AssertAllParametersHaveConstModOpt(Symbol member, bool ignoreLast = false)
        {
            int numParameters = member.GetParameterCount();
            var parameters = member.GetParameters();
            for (int i = 0; i < numParameters; i++)
            {
                if (!(ignoreLast && i == numParameters - 1))
                {
                    var param = parameters[i];
                    Assert.Equal(ConstModOptType, param.CustomModifiers.Single().Modifier.ToTestDisplayString());
                }
            }
        }

        private static void AssertNoParameterHasModOpts(Symbol member)
        {
            foreach (var param in member.GetParameters())
            {
                Assert.Equal(0, param.CustomModifiers.Length);
            }
        }
    }
}
