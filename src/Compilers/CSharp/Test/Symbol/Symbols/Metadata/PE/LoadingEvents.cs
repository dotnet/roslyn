// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    public class LoadingEvents : CSharpTestBase
    {
        [Fact]
        public void LoadNonGenericEvents()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                new[]
                {
                    TestReferences.NetFx.v4_0_30319.mscorlib,
                    TestReferences.SymbolsTests.Events,
                });

            var globalNamespace = assemblies.ElementAt(1).GlobalNamespace;

            var @class = globalNamespace.GetMember<NamedTypeSymbol>("NonGeneric");

            CheckInstanceAndStaticEvents(@class, "System.Action");
        }

        [Fact]
        public void LoadGenericEvents()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                new[]
                {
                    TestReferences.NetFx.v4_0_30319.mscorlib,
                    TestReferences.SymbolsTests.Events,
                });

            var globalNamespace = assemblies.ElementAt(1).GlobalNamespace;

            var @class = globalNamespace.GetMember<NamedTypeSymbol>("Generic");

            CheckInstanceAndStaticEvents(@class, "System.Action<T>");
        }

        [Fact]
        public void LoadClosedGenericEvents()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                new[]
                {
                    TestReferences.NetFx.v4_0_30319.mscorlib,
                    TestReferences.SymbolsTests.Events,
                });

            var globalNamespace = assemblies.ElementAt(1).GlobalNamespace;

            var @class = globalNamespace.GetMember<NamedTypeSymbol>("ClosedGeneric");

            CheckInstanceAndStaticEvents(@class, "System.Action<System.Int32>");
        }

        private static void CheckInstanceAndStaticEvents(NamedTypeSymbol @class, string eventTypeDisplayString)
        {
            var instanceEvent = @class.GetMember<EventSymbol>("InstanceEvent");

            Assert.Equal(SymbolKind.Event, instanceEvent.Kind);
            Assert.False(instanceEvent.IsStatic);
            Assert.Equal(eventTypeDisplayString, instanceEvent.Type.ToTestDisplayString());

            CheckAccessorShape(instanceEvent.AddMethod, instanceEvent);
            CheckAccessorShape(instanceEvent.RemoveMethod, instanceEvent);

            var staticEvent = @class.GetMember<EventSymbol>("StaticEvent");

            Assert.Equal(SymbolKind.Event, staticEvent.Kind);
            Assert.True(staticEvent.IsStatic);
            Assert.Equal(eventTypeDisplayString, staticEvent.Type.ToTestDisplayString());

            CheckAccessorShape(staticEvent.AddMethod, staticEvent);
            CheckAccessorShape(staticEvent.RemoveMethod, staticEvent);
        }

        private static void CheckAccessorShape(MethodSymbol accessor, EventSymbol @event)
        {
            Assert.Same(@event, accessor.AssociatedSymbol);

            switch (accessor.MethodKind)
            {
                case MethodKind.EventAdd:
                    Assert.Same(@event.AddMethod, accessor);
                    break;
                case MethodKind.EventRemove:
                    Assert.Same(@event.RemoveMethod, accessor);
                    break;
                default:
                    Assert.False(true, string.Format("Accessor {0} has unexpected MethodKind {1}", accessor, accessor.MethodKind));
                    break;
            }

            Assert.Equal(@event.IsAbstract, accessor.IsAbstract);
            Assert.Equal(@event.IsOverride, @accessor.IsOverride);
            Assert.Equal(@event.IsVirtual, @accessor.IsVirtual);
            Assert.Equal(@event.IsSealed, @accessor.IsSealed);
            Assert.Equal(@event.IsExtern, @accessor.IsExtern);

            Assert.Equal(SpecialType.System_Void, accessor.ReturnType.SpecialType);
            Assert.Equal(@event.Type, accessor.Parameters.Single().Type);
        }

        [Fact]
        public void LoadSignatureMismatchEvents()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                new[]
                {
                    TestReferences.NetFx.v4_0_30319.mscorlib,
                    TestReferences.SymbolsTests.Events,
                });

            var globalNamespace = assemblies.ElementAt(1).GlobalNamespace;

            var @class = globalNamespace.GetMember<NamedTypeSymbol>("SignatureMismatch");
            var mismatchedAddEvent = @class.GetMember<EventSymbol>("AddMismatch");
            var mismatchedRemoveEvent = @class.GetMember<EventSymbol>("RemoveMismatch");

            Assert.NotEqual(mismatchedAddEvent.Type, mismatchedAddEvent.AddMethod.Parameters.Single().Type);
            Assert.True(mismatchedAddEvent.MustCallMethodsDirectly);

            Assert.NotEqual(mismatchedRemoveEvent.Type, mismatchedRemoveEvent.RemoveMethod.Parameters.Single().Type);
            Assert.True(mismatchedRemoveEvent.MustCallMethodsDirectly);
        }

        [Fact]
        public void LoadMissingParameterEvents()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                new[]
                {
                    TestReferences.NetFx.v4_0_30319.mscorlib,
                    TestReferences.SymbolsTests.Events,
                });

            var globalNamespace = assemblies.ElementAt(1).GlobalNamespace;

            var @class = globalNamespace.GetMember<NamedTypeSymbol>("AccessorMissingParameter");
            var noParamAddEvent = @class.GetMember<EventSymbol>("AddNoParam");
            var noParamRemoveEvent = @class.GetMember<EventSymbol>("RemoveNoParam");

            Assert.Equal(0, noParamAddEvent.AddMethod.Parameters.Length);
            Assert.True(noParamAddEvent.MustCallMethodsDirectly);

            Assert.Equal(0, noParamRemoveEvent.RemoveMethod.Parameters.Length);
            Assert.True(noParamRemoveEvent.MustCallMethodsDirectly);
        }

        [Fact]
        public void LoadNonDelegateEvent()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                new[]
                {
                    TestReferences.NetFx.v4_0_30319.mscorlib,
                    TestReferences.SymbolsTests.Events,
                });

            var globalNamespace = assemblies.ElementAt(1).GlobalNamespace;

            var @class = globalNamespace.GetMember<NamedTypeSymbol>("NonDelegateEvent");
            var nonDelegateEvent = @class.GetMember<EventSymbol>("NonDelegate");

            Assert.Equal(SpecialType.System_Int32, nonDelegateEvent.Type.SpecialType);
        }

        [Fact]
        public void TestExplicitImplementationSimple()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                new[]
                {
                    TestReferences.NetFx.v4_0_30319.mscorlib,
                    TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Events.CSharp,
                });

            var globalNamespace = assemblies.ElementAt(1).GlobalNamespace;

            var @interface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("Interface").Single();
            Assert.Equal(TypeKind.Interface, @interface.TypeKind);

            var interfaceEvent = (EventSymbol)@interface.GetMembers("Event").Single();

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("Class").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);
            Assert.True(@class.Interfaces.Contains(@interface));

            var classEvent = (EventSymbol)@class.GetMembers("Interface.Event").Single();

            var explicitImpl = classEvent.ExplicitInterfaceImplementations.Single();
            Assert.Equal(interfaceEvent, explicitImpl);
        }

        [Fact]
        public void TestExplicitImplementationGeneric()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                new[]
                {
                    TestReferences.NetFx.v4_0_30319.mscorlib,
                    TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Events.CSharp,
                });

            var globalNamespace = assemblies.ElementAt(1).GlobalNamespace;

            var @interface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("IGeneric").Single();
            Assert.Equal(TypeKind.Interface, @interface.TypeKind);

            var interfaceEvent = (EventSymbol)@interface.GetMembers("Event").Single();

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("Generic").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);

            var substitutedInterface = @class.Interfaces.Single();
            Assert.Equal(@interface, substitutedInterface.ConstructedFrom);

            var substitutedInterfaceEvent = (EventSymbol)substitutedInterface.GetMembers("Event").Single();
            Assert.Equal(interfaceEvent, substitutedInterfaceEvent.OriginalDefinition);

            var classEvent = (EventSymbol)@class.GetMembers("IGeneric<S>.Event").Single();

            var explicitImpl = classEvent.ExplicitInterfaceImplementations.Single();
            Assert.Equal(substitutedInterfaceEvent, explicitImpl);
        }

        [Fact]
        public void TestExplicitImplementationConstructed()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                new[]
                {
                    TestReferences.NetFx.v4_0_30319.mscorlib,
                    TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Events.CSharp,
                });

            var globalNamespace = assemblies.ElementAt(1).GlobalNamespace;

            var @interface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("IGeneric").Single();
            Assert.Equal(TypeKind.Interface, @interface.TypeKind);

            var interfaceEvent = (EventSymbol)@interface.GetMembers("Event").Single();

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("Constructed").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);

            var substitutedInterface = @class.Interfaces.Single();
            Assert.Equal(@interface, substitutedInterface.ConstructedFrom);

            var substitutedInterfaceEvent = (EventSymbol)substitutedInterface.GetMembers("Event").Single();
            Assert.Equal(interfaceEvent, substitutedInterfaceEvent.OriginalDefinition);

            var classEvent = (EventSymbol)@class.GetMembers("IGeneric<System.Int32>.Event").Single();

            var explicitImpl = classEvent.ExplicitInterfaceImplementations.Single();
            Assert.Equal(substitutedInterfaceEvent, explicitImpl);
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
                    TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Events.CSharp,
                });

            var globalNamespace = assemblies.ElementAt(1).GlobalNamespace;

            var defInterface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("Interface").Single();
            Assert.Equal(TypeKind.Interface, defInterface.TypeKind);

            var defInterfaceEvent = (EventSymbol)defInterface.GetMembers("Event").Single();

            var refInterface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("IGenericInterface").Single();
            Assert.Equal(TypeKind.Interface, defInterface.TypeKind);
            Assert.True(refInterface.Interfaces.Contains(defInterface));

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("IndirectImplementation").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);

            var classInterfacesConstructedFrom = @class.Interfaces.Select(i => i.ConstructedFrom);
            Assert.Equal(2, classInterfacesConstructedFrom.Count());
            Assert.Contains(defInterface, classInterfacesConstructedFrom);
            Assert.Contains(refInterface, classInterfacesConstructedFrom);

            var classEvent = (EventSymbol)@class.GetMembers("Interface.Event").Single();

            var explicitImpl = classEvent.ExplicitInterfaceImplementations.Single();
            Assert.Equal(defInterfaceEvent, explicitImpl);
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
                    TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Events.CSharp,
                });

            var globalNamespace = assemblies.ElementAt(1).GlobalNamespace;

            var outerInterface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("IGeneric2").Single();
            Assert.Equal(1, outerInterface.Arity);
            Assert.Equal(TypeKind.Interface, outerInterface.TypeKind);

            var outerInterfaceEvent = outerInterface.GetMembers().Single(m => m.Kind == SymbolKind.Event);

            var outerClass = (NamedTypeSymbol)globalNamespace.GetTypeMembers("Outer").Single();
            Assert.Equal(1, outerClass.Arity);
            Assert.Equal(TypeKind.Class, outerClass.TypeKind);

            var innerInterface = (NamedTypeSymbol)outerClass.GetTypeMembers("IInner").Single();
            Assert.Equal(1, innerInterface.Arity);
            Assert.Equal(TypeKind.Interface, innerInterface.TypeKind);

            var innerInterfaceEvent = innerInterface.GetMembers().Single(m => m.Kind == SymbolKind.Event);

            var innerClass1 = (NamedTypeSymbol)outerClass.GetTypeMembers("Inner1").Single();
            CheckInnerClassHelper(innerClass1, "IGeneric2<A>.Event", outerInterfaceEvent);

            var innerClass2 = (NamedTypeSymbol)outerClass.GetTypeMembers("Inner2").Single();
            CheckInnerClassHelper(innerClass2, "IGeneric2<T>.Event", outerInterfaceEvent);

            var innerClass3 = (NamedTypeSymbol)outerClass.GetTypeMembers("Inner3").Single();
            CheckInnerClassHelper(innerClass3, "Outer<T>.IInner<C>.Event", innerInterfaceEvent);

            var innerClass4 = (NamedTypeSymbol)outerClass.GetTypeMembers("Inner4").Single();
            CheckInnerClassHelper(innerClass4, "Outer<T>.IInner<T>.Event", innerInterfaceEvent);
        }

        private static void CheckInnerClassHelper(NamedTypeSymbol innerClass, string methodName, Symbol interfaceEvent)
        {
            var @interface = interfaceEvent.ContainingType;

            Assert.Equal(1, innerClass.Arity);
            Assert.Equal(TypeKind.Class, innerClass.TypeKind);
            Assert.Equal(@interface, innerClass.Interfaces.Single().ConstructedFrom);

            var innerClassEvent = (EventSymbol)innerClass.GetMembers(methodName).Single();
            var innerClassImplementingEvent = innerClassEvent.ExplicitInterfaceImplementations.Single();
            Assert.Equal(interfaceEvent, innerClassImplementingEvent.OriginalDefinition);
            Assert.Equal(@interface, innerClassImplementingEvent.ContainingType.ConstructedFrom);
        }

        // NOTE: results differ from corresponding property test.
        [WorkItem(543263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543263")]
        [Fact]
        public void TestMixedAccessorModifiers()
        {
            var assembly = MetadataTestHelpers.GetSymbolForReference(TestReferences.SymbolsTests.Events);

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
            expected[(int)@none, (int)@sealed] = @sealed;

            expected[(int)@abstract, (int)@none] = @abstract;
            expected[(int)@abstract, (int)@abstract] = @abstract;
            expected[(int)@abstract, (int)@virtual] = @abstract;
            expected[(int)@abstract, (int)@override] = @abstract | @override;
            expected[(int)@abstract, (int)@sealed] = @abstract | @sealed;

            expected[(int)@virtual, (int)@none] = @virtual;
            expected[(int)@virtual, (int)@abstract] = @abstract;
            expected[(int)@virtual, (int)@virtual] = @virtual;
            expected[(int)@virtual, (int)@override] = @override;
            expected[(int)@virtual, (int)@sealed] = @sealed;

            expected[(int)@override, (int)@none] = @override;
            expected[(int)@override, (int)@abstract] = @override | @abstract;
            expected[(int)@override, (int)@virtual] = @override;
            expected[(int)@override, (int)@override] = @override;
            expected[(int)@override, (int)@sealed] = @sealed;

            expected[(int)@sealed, (int)@none] = @sealed;
            expected[(int)@sealed, (int)@abstract] = @abstract | @sealed;
            expected[(int)@sealed, (int)@virtual] = @sealed;
            expected[(int)@sealed, (int)@override] = @sealed;
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
                    var @event = type.GetMember<EventSymbol>(mod1.ToString() + mod2.ToString());
                    var addMethod = @event.AddMethod;
                    var removeMethod = @event.RemoveMethod;

                    Assert.Equal(mod1, GetVirtualnessModifiers(addMethod));
                    Assert.Equal(mod2, GetVirtualnessModifiers(removeMethod));

                    Assert.Equal(expected[(int)mod1, (int)mod2], GetVirtualnessModifiers(@event));
                }
            }
        }

        [Fact]
        [WorkItem(1055825, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1055825")]
        public void AssociatedField()
        {
            var source = @"
public class C
{
    public event System.Action E;
}
";
            var reference = CreateCompilationWithMscorlib(source).EmitToImageReference();
            var comp = CreateCompilationWithMscorlib("", new[] { reference }, TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var @event = type.GetMember<PEEventSymbol>("E");
            Assert.True(@event.HasAssociatedField);

            var field = @event.AssociatedField;
            Assert.NotNull(field);

            Assert.Equal(@event, field.AssociatedSymbol);
        }

        [Fact]
        [WorkItem(1055825, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1055825")]
        public void AssociatedField_MultipleFields()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .field private int32 E
  .field private class [mscorlib]System.Action E

  .method public hidebysig specialname instance void 
          add_E(class [mscorlib]System.Action 'value') cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig specialname instance void 
          remove_E(class [mscorlib]System.Action 'value') cil managed
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

  .event [mscorlib]System.Action E
  {
    .addon instance void C::add_E(class [mscorlib]System.Action)
    .removeon instance void C::remove_E(class [mscorlib]System.Action)
  } // end of event C::E
} // end of class C
";
            var ilRef = CompileIL(ilSource);
            var comp = CreateCompilationWithMscorlib("", new[] { ilRef }, TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            comp.VerifyDiagnostics();

            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var @event = type.GetMembers().OfType<PEEventSymbol>().Single();
            Assert.True(@event.HasAssociatedField);

            var field = @event.AssociatedField;
            Assert.NotNull(field);

            Assert.Equal(@event, field.AssociatedSymbol);
            Assert.Equal(@event.Type, field.Type);
        }

        [Fact]
        [WorkItem(1055825, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1055825")]
        public void AssociatedField_DuplicateEvents()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .field private class [mscorlib]System.Action E

  .method public hidebysig specialname instance void 
          add_E(class [mscorlib]System.Action 'value') cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig specialname instance void 
          remove_E(class [mscorlib]System.Action 'value') cil managed
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

  .event [mscorlib]System.Action E
  {
    .addon instance void C::add_E(class [mscorlib]System.Action)
    .removeon instance void C::remove_E(class [mscorlib]System.Action)
  } // end of event C::E

  .event [mscorlib]System.Action E
  {
    .addon instance void C::add_E(class [mscorlib]System.Action)
    .removeon instance void C::remove_E(class [mscorlib]System.Action)
  } // end of event C::E
} // end of class C
";
            var ilRef = CompileIL(ilSource);
            var comp = CreateCompilationWithMscorlib("", new[] { ilRef }, TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            comp.VerifyDiagnostics();

            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var events = type.GetMembers().OfType<PEEventSymbol>();
            Assert.Equal(2, events.Count());
            AssertEx.All(events, e => e.HasAssociatedField);

            var field = events.First().AssociatedField;
            Assert.NotNull(field);
            AssertEx.All(events, e => e.AssociatedField == field);

            Assert.Contains(field.AssociatedSymbol, events);
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
