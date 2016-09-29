// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    public class LoadingProperties : CSharpTestBase
    {
        [Fact]
        public void TestExplicitImplementationSimple()
        {
            var assembly = MetadataTestHelpers.GetSymbolForReference(
                TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Properties.CSharp);

            var globalNamespace = assembly.GlobalNamespace;

            var @interface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("Interface").Single();
            Assert.Equal(TypeKind.Interface, @interface.TypeKind);

            var interfaceProperty = (PropertySymbol)@interface.GetMembers("Property").Single();

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("Class").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);
            Assert.True(@class.Interfaces.Contains(@interface));

            var classProperty = (PropertySymbol)@class.GetMembers("Interface.Property").Single();

            var explicitImpl = classProperty.ExplicitInterfaceImplementations.Single();
            Assert.Equal(interfaceProperty, explicitImpl);
        }

        [Fact]
        public void TestExplicitImplementationGeneric()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                new[]
                {
                    TestReferences.NetFx.v4_0_30319.mscorlib,
                    TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Properties.CSharp,
                });

            var globalNamespace = assemblies.ElementAt(1).GlobalNamespace;

            var @interface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("IGeneric").Single();
            Assert.Equal(TypeKind.Interface, @interface.TypeKind);

            var interfaceProperty = (PropertySymbol)@interface.GetMembers("Property").Single();

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("Generic").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);

            var substitutedInterface = @class.Interfaces.Single();
            Assert.Equal(@interface, substitutedInterface.ConstructedFrom);

            var substitutedInterfaceProperty = (PropertySymbol)substitutedInterface.GetMembers("Property").Single();
            Assert.Equal(interfaceProperty, substitutedInterfaceProperty.OriginalDefinition);

            var classProperty = (PropertySymbol)@class.GetMembers("IGeneric<S>.Property").Single();

            var explicitImpl = classProperty.ExplicitInterfaceImplementations.Single();
            Assert.Equal(substitutedInterfaceProperty, explicitImpl);
        }

        [Fact]
        public void TestExplicitImplementationConstructed()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                new[]
                {
                    TestReferences.NetFx.v4_0_30319.mscorlib,
                    TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Properties.CSharp,
                });

            var globalNamespace = assemblies.ElementAt(1).GlobalNamespace;

            var @interface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("IGeneric").Single();
            Assert.Equal(TypeKind.Interface, @interface.TypeKind);

            var interfaceProperty = (PropertySymbol)@interface.GetMembers("Property").Single();

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("Constructed").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);

            var substitutedInterface = @class.Interfaces.Single();
            Assert.Equal(@interface, substitutedInterface.ConstructedFrom);

            var substitutedInterfaceProperty = (PropertySymbol)substitutedInterface.GetMembers("Property").Single();
            Assert.Equal(interfaceProperty, substitutedInterfaceProperty.OriginalDefinition);

            var classProperty = (PropertySymbol)@class.GetMembers("IGeneric<System.Int32>.Property").Single();

            var explicitImpl = classProperty.ExplicitInterfaceImplementations.Single();
            Assert.Equal(substitutedInterfaceProperty, explicitImpl);
        }

        /// <summary>
        /// A type def explicitly implements an interface, also a type def, but only
        /// indirectly, via a type ref.
        /// </summary>
        [Fact]
        public void TestExplicitImplementationDefRefDef()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                new[]
                {
                    TestReferences.NetFx.v4_0_30319.mscorlib,
                    TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Properties.CSharp,
                });

            var globalNamespace = assemblies.ElementAt(1).GlobalNamespace;

            var defInterface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("Interface").Single();
            Assert.Equal(TypeKind.Interface, defInterface.TypeKind);

            var defInterfaceProperty = (PropertySymbol)defInterface.GetMembers("Property").Single();

            var refInterface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("IGenericInterface").Single();
            Assert.Equal(TypeKind.Interface, defInterface.TypeKind);
            Assert.True(refInterface.Interfaces.Contains(defInterface));

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("IndirectImplementation").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);

            var classInterfacesConstructedFrom = @class.Interfaces.Select(i => i.ConstructedFrom);
            Assert.Equal(2, classInterfacesConstructedFrom.Count());
            Assert.Contains(defInterface, classInterfacesConstructedFrom);
            Assert.Contains(refInterface, classInterfacesConstructedFrom);

            var classProperty = (PropertySymbol)@class.GetMembers("Interface.Property").Single();

            var explicitImpl = classProperty.ExplicitInterfaceImplementations.Single();
            Assert.Equal(defInterfaceProperty, explicitImpl);
        }

        /// <summary>
        /// In metadata, nested types implicitly share all type parameters of their containing types.
        /// This results in some extra computations when mapping a type parameter position to a type
        /// parameter symbol.
        /// </summary>
        [Fact]
        public void TestTypeParameterPositions()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                new[]
                {
                    TestReferences.NetFx.v4_0_30319.mscorlib,
                    TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Properties.CSharp,
                });

            var globalNamespace = assemblies.ElementAt(1).GlobalNamespace;

            var outerInterface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("IGeneric2").Single();
            Assert.Equal(1, outerInterface.Arity);
            Assert.Equal(TypeKind.Interface, outerInterface.TypeKind);

            var outerInterfaceProperty = outerInterface.GetMembers().Single(m => m.Kind == SymbolKind.Property);

            var outerClass = (NamedTypeSymbol)globalNamespace.GetTypeMembers("Outer").Single();
            Assert.Equal(1, outerClass.Arity);
            Assert.Equal(TypeKind.Class, outerClass.TypeKind);

            var innerInterface = (NamedTypeSymbol)outerClass.GetTypeMembers("IInner").Single();
            Assert.Equal(1, innerInterface.Arity);
            Assert.Equal(TypeKind.Interface, innerInterface.TypeKind);

            var innerInterfaceProperty = innerInterface.GetMembers().Single(m => m.Kind == SymbolKind.Property);

            var innerClass1 = (NamedTypeSymbol)outerClass.GetTypeMembers("Inner1").Single();
            CheckInnerClassHelper(innerClass1, "IGeneric2<A>.Property", outerInterfaceProperty);

            var innerClass2 = (NamedTypeSymbol)outerClass.GetTypeMembers("Inner2").Single();
            CheckInnerClassHelper(innerClass2, "IGeneric2<T>.Property", outerInterfaceProperty);

            var innerClass3 = (NamedTypeSymbol)outerClass.GetTypeMembers("Inner3").Single();
            CheckInnerClassHelper(innerClass3, "Outer<T>.IInner<C>.Property", innerInterfaceProperty);

            var innerClass4 = (NamedTypeSymbol)outerClass.GetTypeMembers("Inner4").Single();
            CheckInnerClassHelper(innerClass4, "Outer<T>.IInner<T>.Property", innerInterfaceProperty);
        }

        private static void CheckInnerClassHelper(NamedTypeSymbol innerClass, string methodName, Symbol interfaceProperty)
        {
            var @interface = interfaceProperty.ContainingType;

            Assert.Equal(1, innerClass.Arity);
            Assert.Equal(TypeKind.Class, innerClass.TypeKind);
            Assert.Equal(@interface, innerClass.Interfaces.Single().ConstructedFrom);

            var innerClassProperty = (PropertySymbol)innerClass.GetMembers(methodName).Single();
            var innerClassImplementingProperty = innerClassProperty.ExplicitInterfaceImplementations.Single();
            Assert.Equal(interfaceProperty, innerClassImplementingProperty.OriginalDefinition);
            Assert.Equal(@interface, innerClassImplementingProperty.ContainingType.ConstructedFrom);
        }

        /// <summary>
        /// Interface has 1{g;s}, 2{g;s}, 3{g;s}, 4{g}, 5{s}
        /// Class has 1{g;s}, 2{g;s}
        /// Class 1g implements Interface 1g, 2g, 4g
        /// Class 1s implements Interface 1s, 3s, 5s
        /// Class 2g implements Interface 2s
        /// Class 2s implements Interface 3g
        /// </summary>
        [Fact]
        public void TestExplicitImplementationMultipleAndPartial()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                new[]
                {
                    TestReferences.NetFx.v4_0_30319.mscorlib,
                    TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Properties.IL,
                });

            var globalNamespace = assemblies.ElementAt(1).GlobalNamespace;

            var @interface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("Interface").Single();
            Assert.Equal(TypeKind.Interface, @interface.TypeKind);

            var interfaceProperty1 = (PropertySymbol)@interface.GetMembers("Property1").Single();
            var interfaceProperty2 = (PropertySymbol)@interface.GetMembers("Property2").Single();
            var interfaceProperty3 = (PropertySymbol)@interface.GetMembers("Property3").Single();
            var interfaceProperty4 = (PropertySymbol)@interface.GetMembers("Property4").Single();
            var interfaceProperty5 = (PropertySymbol)@interface.GetMembers("Property5").Single();

            Assert.NotNull(interfaceProperty1.GetMethod);
            Assert.NotNull(interfaceProperty1.SetMethod);

            Assert.NotNull(interfaceProperty2.GetMethod);
            Assert.NotNull(interfaceProperty2.SetMethod);

            Assert.NotNull(interfaceProperty3.GetMethod);
            Assert.NotNull(interfaceProperty3.SetMethod);

            Assert.NotNull(interfaceProperty4.GetMethod);
            Assert.Null(interfaceProperty4.SetMethod);

            Assert.Null(interfaceProperty5.GetMethod);
            Assert.NotNull(interfaceProperty5.SetMethod);

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("Class").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);

            var classProperty1 = (PropertySymbol)@class.GetMembers("Property1").Single();
            var classProperty2 = (PropertySymbol)@class.GetMembers("Property2").Single();

            Assert.NotNull(classProperty1.GetMethod);
            Assert.NotNull(classProperty1.SetMethod);

            Assert.NotNull(classProperty2.GetMethod);
            Assert.NotNull(classProperty2.SetMethod);

            var implementedByProperty1 = ImmutableArray.Create<PropertySymbol>(interfaceProperty1, interfaceProperty4, interfaceProperty5);
            Assert.True(implementedByProperty1.SetEquals(classProperty1.ExplicitInterfaceImplementations, ReferenceEqualityComparer.Instance));

            Assert.Equal(0, classProperty2.ExplicitInterfaceImplementations.Length);

            var implementedByGetter1 = ImmutableArray.Create<MethodSymbol>(interfaceProperty1.GetMethod, interfaceProperty2.GetMethod, interfaceProperty4.GetMethod);
            Assert.True(implementedByGetter1.SetEquals(classProperty1.GetMethod.ExplicitInterfaceImplementations, ReferenceEqualityComparer.Instance));

            var implementedBySetter1 = ImmutableArray.Create<MethodSymbol>(interfaceProperty1.SetMethod, interfaceProperty3.SetMethod, interfaceProperty5.SetMethod);
            Assert.True(implementedBySetter1.SetEquals(classProperty1.SetMethod.ExplicitInterfaceImplementations, ReferenceEqualityComparer.Instance));

            var implementedByGetter2 = ImmutableArray.Create<MethodSymbol>(interfaceProperty3.GetMethod);
            Assert.True(implementedByGetter2.SetEquals(classProperty2.GetMethod.ExplicitInterfaceImplementations, ReferenceEqualityComparer.Instance));

            var implementedBySetter2 = ImmutableArray.Create<MethodSymbol>(interfaceProperty2.SetMethod);
            Assert.True(implementedBySetter2.SetEquals(classProperty2.SetMethod.ExplicitInterfaceImplementations, ReferenceEqualityComparer.Instance));

            Assert.Same(classProperty1, @class.FindImplementationForInterfaceMember(interfaceProperty1));
            Assert.Same(classProperty1, @class.FindImplementationForInterfaceMember(interfaceProperty4));
            Assert.Same(classProperty1, @class.FindImplementationForInterfaceMember(interfaceProperty5));

            Assert.Null(@class.FindImplementationForInterfaceMember(interfaceProperty2));
            Assert.Null(@class.FindImplementationForInterfaceMember(interfaceProperty3));
        }

        [WorkItem(543263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543263")]
        [Fact]
        public void TestMixedAccessorModifiers()
        {
            var assembly = MetadataTestHelpers.GetSymbolForReference(TestReferences.SymbolsTests.Properties);

            var globalNamespace = assembly.GlobalNamespace;

            var type = globalNamespace.GetMember<NamedTypeSymbol>("AccessorModifierMismatch");

            const VirtualnessModifiers @none = VirtualnessModifiers.None;
            const VirtualnessModifiers @abstract = VirtualnessModifiers.Abstract;
            const VirtualnessModifiers @virtual = VirtualnessModifiers.Virtual;
            const VirtualnessModifiers @override = VirtualnessModifiers.Override;
            const VirtualnessModifiers @sealed = VirtualnessModifiers.Sealed;

            VirtualnessModifiers[] modList = new[]
            {
                @none,
                @abstract,
                @virtual,
                @override,
                @sealed,
            };

            int length = 1 + modList.Cast<int>().Max();
            VirtualnessModifiers[,] expected = new VirtualnessModifiers[length, length];

            expected[(int)@none, (int)@none] = @none;
            expected[(int)@none, (int)@abstract] = @abstract;
            expected[(int)@none, (int)@virtual] = @virtual;
            expected[(int)@none, (int)@override] = @override;
            expected[(int)@none, (int)@sealed] = @override; //not both sealed

            expected[(int)@abstract, (int)@none] = @abstract;
            expected[(int)@abstract, (int)@abstract] = @abstract;
            expected[(int)@abstract, (int)@virtual] = @abstract;
            expected[(int)@abstract, (int)@override] = @abstract | @override;
            expected[(int)@abstract, (int)@sealed] = @abstract | @override; //not both sealed

            expected[(int)@virtual, (int)@none] = @virtual;
            expected[(int)@virtual, (int)@abstract] = @abstract;
            expected[(int)@virtual, (int)@virtual] = @virtual;
            expected[(int)@virtual, (int)@override] = @override;
            expected[(int)@virtual, (int)@sealed] = @override; //not both sealed

            expected[(int)@override, (int)@none] = @override;
            expected[(int)@override, (int)@abstract] = @override | @abstract;
            expected[(int)@override, (int)@virtual] = @override;
            expected[(int)@override, (int)@override] = @override;
            expected[(int)@override, (int)@sealed] = @override; //not both sealed

            expected[(int)@sealed, (int)@none] = @override; //not both sealed
            expected[(int)@sealed, (int)@abstract] = @abstract | @override; //not both sealed
            expected[(int)@sealed, (int)@virtual] = @override; //not both sealed
            expected[(int)@sealed, (int)@override] = @override; //not both sealed
            expected[(int)@sealed, (int)@sealed] = @sealed;

            // Table should be symmetrical.
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    Assert.Equal(expected[i, j], expected[j, i]);
                }
            }

            foreach (var mod1 in modList)
            {
                foreach (var mod2 in modList)
                {
                    var property = type.GetMember<PropertySymbol>(mod1.ToString() + mod2.ToString());
                    var getter = property.GetMethod;
                    var setter = property.SetMethod;

                    Assert.Equal(mod1, GetVirtualnessModifiers(getter));
                    Assert.Equal(mod2, GetVirtualnessModifiers(setter));

                    Assert.Equal(expected[(int)mod1, (int)mod2], GetVirtualnessModifiers(property));
                }
            }
        }
        [Flags]

        private enum VirtualnessModifiers
        {
            None = 0,
            Abstract = 1,
            Virtual = 2,
            Override = 4,
            Sealed = 8, //actually indicates sealed override
        }

        private static VirtualnessModifiers GetVirtualnessModifiers(Symbol symbol)
        {
            VirtualnessModifiers mods = VirtualnessModifiers.None;

            if (symbol.IsAbstract) mods |= VirtualnessModifiers.Abstract;
            if (symbol.IsVirtual) mods |= VirtualnessModifiers.Virtual;

            if (symbol.IsSealed) mods |= VirtualnessModifiers.Sealed;
            else if (symbol.IsOverride) mods |= VirtualnessModifiers.Override;

            return mods;
        }
    }
}
