' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports CompilationCreationTestHelpers
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Roslyn.Test.Utilities.TestMetadata

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata.PE

    Public Class LoadingEvents : Inherits BasicTestBase
        <Fact>
        Public Sub LoadNonGenericEvents()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences({Net451.mscorlib, TestReferences.SymbolsTests.Events})

            Dim globalNamespace = assemblies.ElementAt(1).GlobalNamespace
            Dim [class] = globalNamespace.GetMember(Of NamedTypeSymbol)("NonGeneric")
            CheckInstanceAndStaticEvents([class], "System.Action")
        End Sub

        <Fact>
        Public Sub LoadGenericEvents()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences({Net451.mscorlib, TestReferences.SymbolsTests.Events})

            Dim globalNamespace = assemblies.ElementAt(1).GlobalNamespace
            Dim [class] = globalNamespace.GetMember(Of NamedTypeSymbol)("Generic")
            CheckInstanceAndStaticEvents([class], "System.Action(Of T)")
        End Sub

        <Fact>
        Public Sub LoadClosedGenericEvents()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences({Net451.mscorlib, TestReferences.SymbolsTests.Events})

            Dim globalNamespace = assemblies.ElementAt(1).GlobalNamespace
            Dim [class] = globalNamespace.GetMember(Of NamedTypeSymbol)("ClosedGeneric")
            CheckInstanceAndStaticEvents([class], "System.Action(Of System.Int32)")
        End Sub

        <Fact>
        Public Sub Error_Regress40025DLL()
            Dim source =
<compilation name="Error_Regress40025DLL1">
    <file name="a.vb">
Module Program
    Sub Main()
        Dim d As Regress40025DLL.Regress40025DLL.AClass = nothing

        'COMPILEERROR: BC30005, "ee"
        AddHandler d.ee, Nothing
    End Sub
End Module


    </file>
</compilation>
            Dim ref = MetadataReference.CreateFromImage(TestResources.General.Regress40025DLL.AsImmutableOrNull())
            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, {ref}, TestOptions.ReleaseExe)
            CompilationUtils.AssertTheseDiagnostics(c1,
<errors>
BC30005: Reference required to assembly 'System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' containing the definition for event 'Public Event ee As PlayRecordCallback'. Add one to your project.
        AddHandler d.ee, Nothing
                   ~~~~
</errors>)

            Dim diagnostics = c1.GetDiagnostics()
            Assert.True(diagnostics.Any(Function(d) d.Code = ERRID.ERR_UnreferencedAssemblyEvent3))

            For Each d In diagnostics
                If d.Code = ERRID.ERR_UnreferencedAssemblyEvent3 Then
                    Dim actualAssemblyId = c1.GetUnreferencedAssemblyIdentities(d).Single()
                    Dim expectedAssemblyId As AssemblyIdentity = Nothing
                    AssemblyIdentity.TryParseDisplayName("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", expectedAssemblyId)

                    Assert.Equal(actualAssemblyId, expectedAssemblyId)
                End If
            Next
        End Sub

        Private Shared Sub CheckInstanceAndStaticEvents([class] As NamedTypeSymbol, eventTypeDisplayString As String)
            Dim instanceEvent = [class].GetMember(Of EventSymbol)("InstanceEvent")
            Assert.Equal(SymbolKind.[Event], instanceEvent.Kind)
            Assert.[False](instanceEvent.IsShared)
            Assert.Equal(eventTypeDisplayString, instanceEvent.Type.ToTestDisplayString())

            CheckAccessorShape(instanceEvent.AddMethod, instanceEvent)
            CheckAccessorShape(instanceEvent.RemoveMethod, instanceEvent)

            Dim staticEvent = [class].GetMember(Of EventSymbol)("StaticEvent")
            Assert.Equal(SymbolKind.[Event], staticEvent.Kind)
            Assert.[True](staticEvent.IsShared)
            Assert.Equal(eventTypeDisplayString, staticEvent.Type.ToTestDisplayString())
            CheckAccessorShape(staticEvent.AddMethod, staticEvent)
            CheckAccessorShape(staticEvent.RemoveMethod, staticEvent)
        End Sub

        Private Shared Sub CheckAccessorShape(accessor As MethodSymbol, [event] As EventSymbol)
            Assert.Same([event], accessor.AssociatedSymbol)
            Select Case accessor.MethodKind
                Case MethodKind.EventAdd
                    Assert.Same([event].AddMethod, accessor)

                Case MethodKind.EventRemove
                    Assert.Same([event].RemoveMethod, accessor)

                Case Else
                    Assert.[False](True, String.Format("Accessor {0} has unexpected MethodKind {1}", accessor, accessor.MethodKind))

            End Select

            Assert.Equal([event].IsMustOverride, accessor.IsMustOverride)
            Assert.Equal([event].IsOverrides, accessor.IsOverrides)
            Assert.Equal([event].IsOverridable, accessor.IsOverridable)
            Assert.Equal([event].IsNotOverridable, accessor.IsNotOverridable)
            ' TODO: do we need to support Extern events?
            '            Assert.Equal([event].IsExtern, accessor.IsExtern)
            Assert.Equal(SpecialType.System_Void, accessor.ReturnType.SpecialType)
            Assert.Equal([event].Type, accessor.Parameters.Single().Type)
        End Sub

        <Fact>
        Public Sub LoadSignatureMismatchEvents()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences({Net451.mscorlib, TestReferences.SymbolsTests.Events})
            Dim globalNamespace = assemblies.ElementAt(1).GlobalNamespace
            Dim [class] = globalNamespace.GetMember(Of NamedTypeSymbol)("SignatureMismatch")
            Dim mismatchedAddEvent = [class].GetMember(Of EventSymbol)("AddMismatch")
            Dim mismatchedRemoveEvent = [class].GetMember(Of EventSymbol)("RemoveMismatch")
            Assert.NotEqual(mismatchedAddEvent.Type, mismatchedAddEvent.AddMethod.Parameters.Single().Type)
            Assert.NotEqual(mismatchedRemoveEvent.Type, mismatchedRemoveEvent.RemoveMethod.Parameters.Single().Type)
        End Sub

        <Fact>
        Public Sub LoadMissingParameterEvents()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences({Net451.mscorlib, TestReferences.SymbolsTests.Events})
            Dim globalNamespace = assemblies.ElementAt(1).GlobalNamespace
            Dim [class] = globalNamespace.GetMember(Of NamedTypeSymbol)("AccessorMissingParameter")
            Dim noParamAddEvent = [class].GetMember(Of EventSymbol)("AddNoParam")
            Dim noParamRemoveEvent = [class].GetMember(Of EventSymbol)("RemoveNoParam")
            Assert.Equal(0, noParamAddEvent.AddMethod.Parameters.Length)
            Assert.Equal(0, noParamRemoveEvent.RemoveMethod.Parameters.Length)
        End Sub

        <Fact>
        Public Sub LoadNonDelegateEvent()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences({Net451.mscorlib, TestReferences.SymbolsTests.Events})
            Dim globalNamespace = assemblies.ElementAt(1).GlobalNamespace
            Dim [class] = globalNamespace.GetMember(Of NamedTypeSymbol)("NonDelegateEvent")
            Dim nonDelegateEvent = [class].GetMember(Of EventSymbol)("NonDelegate")
            Assert.Equal(SpecialType.System_Int32, nonDelegateEvent.Type.SpecialType)
        End Sub

        <Fact>
        Public Sub TestExplicitImplementationSimple()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences({Net451.mscorlib, TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Events.CSharp})
            Dim globalNamespace = assemblies.ElementAt(1).GlobalNamespace
            Dim [interface] = DirectCast(globalNamespace.GetTypeMembers("Interface").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.[Interface], [interface].TypeKind)
            Dim interfaceEvent = DirectCast([interface].GetMembers("Event").Single(), EventSymbol)
            Dim [class] = DirectCast(globalNamespace.GetTypeMembers("Class").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.[Class], [class].TypeKind)
            Assert.[True]([class].Interfaces.Contains([interface]))
            Dim classEvent = DirectCast([class].GetMembers("Interface.Event").Single(), EventSymbol)
            Dim explicitImpl = classEvent.ExplicitInterfaceImplementations.Single()
            Assert.Equal(interfaceEvent, explicitImpl)
        End Sub

        <Fact>
        Public Sub TestExplicitImplementationGeneric()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences({Net451.mscorlib, TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Events.CSharp})
            Dim globalNamespace = assemblies.ElementAt(1).GlobalNamespace
            Dim [interface] = DirectCast(globalNamespace.GetTypeMembers("IGeneric").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.[Interface], [interface].TypeKind)
            Dim interfaceEvent = DirectCast([interface].GetMembers("Event").Single(), EventSymbol)
            Dim [class] = DirectCast(globalNamespace.GetTypeMembers("Generic").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.[Class], [class].TypeKind)
            Dim substitutedInterface = [class].Interfaces.Single()
            Assert.Equal([interface], substitutedInterface.ConstructedFrom)
            Dim substitutedInterfaceEvent = DirectCast(substitutedInterface.GetMembers("Event").Single(), EventSymbol)
            Assert.Equal(interfaceEvent, substitutedInterfaceEvent.OriginalDefinition)
            Dim classEvent = DirectCast([class].GetMembers("IGeneric<S>.Event").Single(), EventSymbol)
            Dim explicitImpl = classEvent.ExplicitInterfaceImplementations.Single()
            Assert.Equal(substitutedInterfaceEvent, explicitImpl)
        End Sub

        <Fact>
        Public Sub TestExplicitImplementationConstructed()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences({Net451.mscorlib, TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Events.CSharp})
            Dim globalNamespace = assemblies.ElementAt(1).GlobalNamespace
            Dim [interface] = DirectCast(globalNamespace.GetTypeMembers("IGeneric").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.[Interface], [interface].TypeKind)
            Dim interfaceEvent = DirectCast([interface].GetMembers("Event").Single(), EventSymbol)
            Dim [class] = DirectCast(globalNamespace.GetTypeMembers("Constructed").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.[Class], [class].TypeKind)
            Dim substitutedInterface = [class].Interfaces.Single()
            Assert.Equal([interface], substitutedInterface.ConstructedFrom)
            Dim substitutedInterfaceEvent = DirectCast(substitutedInterface.GetMembers("Event").Single(), EventSymbol)
            Assert.Equal(interfaceEvent, substitutedInterfaceEvent.OriginalDefinition)
            Dim classEvent = DirectCast([class].GetMembers("IGeneric<System.Int32>.Event").Single(), EventSymbol)
            Dim explicitImpl = classEvent.ExplicitInterfaceImplementations.Single()
            Assert.Equal(substitutedInterfaceEvent, explicitImpl)
        End Sub

        <Fact>
        Public Sub TestExplicitImplementationDefRefDef()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences({Net451.mscorlib, TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Events.CSharp})
            Dim globalNamespace = assemblies.ElementAt(1).GlobalNamespace
            Dim defInterface = DirectCast(globalNamespace.GetTypeMembers("Interface").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.[Interface], defInterface.TypeKind)
            Dim defInterfaceEvent = DirectCast(defInterface.GetMembers("Event").Single(), EventSymbol)
            Dim refInterface = DirectCast(globalNamespace.GetTypeMembers("IGenericInterface").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.[Interface], defInterface.TypeKind)
            Assert.[True](refInterface.Interfaces.Contains(defInterface))
            Dim [class] = DirectCast(globalNamespace.GetTypeMembers("IndirectImplementation").Single(), NamedTypeSymbol)
            Assert.Equal(TypeKind.[Class], [class].TypeKind)
            Dim classInterfacesConstructedFrom = [class].Interfaces.[Select](Function(i) i.ConstructedFrom)
            Assert.Equal(2, classInterfacesConstructedFrom.Count())
            Assert.Contains(defInterface, classInterfacesConstructedFrom)
            Assert.Contains(refInterface, classInterfacesConstructedFrom)
            Dim classEvent = DirectCast([class].GetMembers("Interface.Event").Single(), EventSymbol)
            Dim explicitImpl = classEvent.ExplicitInterfaceImplementations.Single()
            Assert.Equal(defInterfaceEvent, explicitImpl)
        End Sub

        <Fact>
        Public Sub TestTypeParameterPositions()
            Dim assemblies = MetadataTestHelpers.GetSymbolsForReferences({Net451.mscorlib, TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Events.CSharp})
            Dim globalNamespace = assemblies.ElementAt(1).GlobalNamespace
            Dim outerInterface = DirectCast(globalNamespace.GetTypeMembers("IGeneric2").Single(), NamedTypeSymbol)
            Assert.Equal(1, outerInterface.Arity)
            Assert.Equal(TypeKind.[Interface], outerInterface.TypeKind)
            Dim outerInterfaceEvent = outerInterface.GetMembers().Where(Function(m) m.Kind = SymbolKind.[Event]).Single()
            Dim outerClass = DirectCast(globalNamespace.GetTypeMembers("Outer").Single(), NamedTypeSymbol)
            Assert.Equal(1, outerClass.Arity)
            Assert.Equal(TypeKind.[Class], outerClass.TypeKind)
            Dim innerInterface = DirectCast(outerClass.GetTypeMembers("IInner").Single(), NamedTypeSymbol)
            Assert.Equal(1, innerInterface.Arity)
            Assert.Equal(TypeKind.[Interface], innerInterface.TypeKind)
            Dim innerInterfaceEvent = innerInterface.GetMembers().Where(Function(m) m.Kind = SymbolKind.[Event]).Single()
            Dim innerClass1 = DirectCast(outerClass.GetTypeMembers("Inner1").Single(), NamedTypeSymbol)
            CheckInnerClassHelper(innerClass1, "IGeneric2<A>.Event", outerInterfaceEvent)
            Dim innerClass2 = DirectCast(outerClass.GetTypeMembers("Inner2").Single(), NamedTypeSymbol)
            CheckInnerClassHelper(innerClass2, "IGeneric2<T>.Event", outerInterfaceEvent)
            Dim innerClass3 = DirectCast(outerClass.GetTypeMembers("Inner3").Single(), NamedTypeSymbol)
            CheckInnerClassHelper(innerClass3, "Outer<T>.IInner<C>.Event", innerInterfaceEvent)
            Dim innerClass4 = DirectCast(outerClass.GetTypeMembers("Inner4").Single(), NamedTypeSymbol)
            CheckInnerClassHelper(innerClass4, "Outer<T>.IInner<T>.Event", innerInterfaceEvent)
        End Sub

        Private Shared Sub CheckInnerClassHelper(innerClass As NamedTypeSymbol, methodName As String, interfaceEvent As Symbol)
            Dim [interface] = interfaceEvent.ContainingType
            Assert.Equal(1, innerClass.Arity)
            Assert.Equal(TypeKind.[Class], innerClass.TypeKind)
            Assert.Equal([interface], innerClass.Interfaces.Single().ConstructedFrom)
            Dim innerClassEvent = DirectCast(innerClass.GetMembers(methodName).Single(), EventSymbol)
            Dim innerClassImplementingEvent = innerClassEvent.ExplicitInterfaceImplementations.Single()
            Assert.Equal(interfaceEvent, innerClassImplementingEvent.OriginalDefinition)
            Assert.Equal([interface], innerClassImplementingEvent.ContainingType.ConstructedFrom)
        End Sub

    End Class

End Namespace

