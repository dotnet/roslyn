' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities
Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata.PE

    Public Class NoPiaLocalHideAndTypeSubstitutionTests
        Inherits BasicTestBase

        <Fact>
        Public Sub NoPiaTypeEquivalenceBetweenPIATypeInExternalAndLocalAssembly()
            ' Verify type equivalence between PIA type in external assembly and local assembly
            Dim localConsumerCompilationDef =
<compilation name="Dummy">
    <file name="Dummy.vb">
    </file>
</compilation>

            Dim localConsumer = CompilationUtils.CreateCompilationWithMscorlib(localConsumerCompilationDef)
            localConsumer = localConsumer.AddReferences(TestReferences.SymbolsTests.NoPia.Pia1, TestReferences.SymbolsTests.NoPia.LocalTypes1)

            Dim localConsumerRefsAsm = localConsumer.[Assembly].GetNoPiaResolutionAssemblies()
            Assert.Equal(1, localConsumerRefsAsm.First(Function(arg) arg.Name = "LocalTypes1").Modules.FirstOrDefault().GetReferencedAssemblies().Length)
            Assert.Equal(1, localConsumerRefsAsm.First(Function(arg) arg.Name = "LocalTypes1").Modules.FirstOrDefault().GetReferencedAssemblySymbols().Length)
            Assert.Equal(localConsumerRefsAsm.First(Function(arg) arg.Name = "mscorlib"), localConsumerRefsAsm.First(Function(arg) arg.Name = "LocalTypes1").Modules.FirstOrDefault().GetReferencedAssemblySymbols().ElementAt(0))
            Dim canonicalType1 = localConsumerRefsAsm.First(Function(arg) arg.Name = "Pia1").GlobalNamespace.GetTypeMembers("I1").[Single]()
            Dim canonicalType2 = localConsumerRefsAsm.First(Function(arg) arg.Name = "Pia1").GlobalNamespace.GetMembers("NS1").OfType(Of NamespaceSymbol)().[Single]().GetTypeMembers("I2").[Single]()
            Dim classLocalType As NamedTypeSymbol = localConsumerRefsAsm.First(Function(arg) arg.Name = "LocalTypes1").GlobalNamespace.GetTypeMembers("LocalTypes1").[Single]()
            Dim methodSymbol As MethodSymbol = classLocalType.GetMembers("Test1").OfType(Of MethodSymbol)().[Single]()
            Dim param As ImmutableArray(Of ParameterSymbol) = methodSymbol.Parameters
            Assert.Same(canonicalType1, param.[Where](Function(arg) arg.[Type].Name = "I1").[Select](Function(arg) arg).[Single]().[Type])
            Assert.Same(canonicalType2, param.[Where](Function(arg) arg.[Type].Name = "I2").[Select](Function(arg) arg).[Single]().[Type])
        End Sub

        <Fact>
        Public Sub NoPIALocalTypesEquivalentToEachOtherStructAsMethodParameterType()
            'Structure - As method parameter type in external assembly (test this by passing the parameter with a variable which was declared in the current assembly)

            Dim compilationDef =
<compilation name="Dummy">
    <file name="Dummy.vb">
Module TypeSubstitution

    Dim myOwnVar As FooStruct = Nothing

    public Sub Main()
        myOwnVar = new FooStruct()
        myOwnVar.Structure = -1
        ExternalAsm1.Scen1(myOwnVar)
    End Sub
End Module
    </file>
</compilation>

            Dim localConsumer = CompilationUtils.CreateCompilationWithReferences(
                                    compilationDef,
                                    {TestReferences.SymbolsTests.NoPia.GeneralPia,
                                     TestReferences.SymbolsTests.NoPia.ExternalAsm1})

            Dim localConsumerRefsAsm = localConsumer.[Assembly].GetNoPiaResolutionAssemblies()
            Assert.Equal(3, localConsumerRefsAsm.First(Function(arg) arg.Name = "GeneralPia").Modules.FirstOrDefault().GetReferencedAssemblies().Length)
            Assert.Equal(3, localConsumerRefsAsm.First(Function(arg) arg.Name = "GeneralPia").Modules.FirstOrDefault().GetReferencedAssemblySymbols().Length)
            Dim canonicalType = localConsumerRefsAsm.First(Function(arg) arg.Name = "GeneralPia").GlobalNamespace.GetTypeMembers("FooStruct").[Single]()
            Dim classLocalType As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("TypeSubstitution").[Single]()
            Dim localFieldSymbol As FieldSymbol = classLocalType.GetMembers("myOwnVar").OfType(Of FieldSymbol)().[Single]()
            Dim classRefLocalType As NamedTypeSymbol = localConsumerRefsAsm.First(Function(arg) arg.Name = "ExternalAsm1").GlobalNamespace.GetTypeMembers("ExternalAsm1").[Single]()
            Dim refMethodSymbol As MethodSymbol = classRefLocalType.GetMembers("Scen1").OfType(Of MethodSymbol)().[Single]()
            Dim param As ImmutableArray(Of ParameterSymbol) = refMethodSymbol.Parameters
            Dim missing As NoPiaMissingCanonicalTypeSymbol = DirectCast(param.First().[Type], NoPiaMissingCanonicalTypeSymbol)
            Assert.Same(localConsumerRefsAsm.First(Function(arg) arg.Name = "ExternalAsm1"), missing.EmbeddingAssembly)
            Assert.Null(missing.Guid)
            Assert.Equal(canonicalType.ToTestDisplayString(), missing.FullTypeName)
            Assert.Equal("f9c2d51d-4f44-45f0-9eda-c9d599b58257", missing.Scope)
            Assert.Equal(canonicalType.ToTestDisplayString(), missing.Identifier)
            Assert.Same(canonicalType, localFieldSymbol.[Type])
            Assert.IsType(Of NoPiaMissingCanonicalTypeSymbol)(param(0).[Type])
        End Sub

        <Fact>
        Public Sub NoPIALocalTypesEquivalentToEachOtherInterfaceAsMethodParameterType()
            'Same as previous scenario but with Interface

            Dim compilationDef =
<compilation name="Dummy">
    <file name="Dummy.vb">
class ICBase 
 Inherits InheritanceConflict.IBase

    public Function Bar() As Integer
        return -2
    End Function

    public Sub ConflictMethod(x As Integer)
    End Sub

    public Sub Foo()
    End Sub

    'public string this[object x]
    '{
    '    get { return null; }
    '    set { }
    '}
End Class

Module TypeSubstitution

    Dim myOwnRef As InheritanceConflict.IBase =Nothing
    public Sub Main()
        myOwnRef = new ICBase()
        ExternalAsm1.Scen2(myOwnRef)
    End Sub
End Module
    </file>
</compilation>

            Dim localConsumer = CompilationUtils.CreateCompilationWithReferences(compilationDef,
                                                                                 {TestReferences.SymbolsTests.NoPia.GeneralPia,
                                                                                  TestReferences.SymbolsTests.NoPia.ExternalAsm1})

            Dim localConsumerRefsAsm = localConsumer.[Assembly].GetNoPiaResolutionAssemblies()
            Dim canonicalType = localConsumerRefsAsm.First(Function(arg) arg.Name = "GeneralPia").GlobalNamespace.ChildNamespace("InheritanceConflict")
            Dim canonicalTypeInter = canonicalType.GetTypeMembers("IBase").[Single]()
            Dim classLocalType As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("TypeSubstitution").[Single]()
            Dim localFieldSymbol As FieldSymbol = classLocalType.GetMembers("myOwnRef").OfType(Of FieldSymbol)().[Single]()
            Dim classRefLocalType As NamedTypeSymbol = localConsumerRefsAsm.First(Function(arg) arg.Name = "ExternalAsm1").GlobalNamespace.GetTypeMembers("ExternalAsm1").[Single]()
            Dim refMethodSymbol As MethodSymbol = classRefLocalType.GetMembers("Scen2").OfType(Of MethodSymbol)().[Single]()
            Dim param As ImmutableArray(Of ParameterSymbol) = refMethodSymbol.Parameters
            Assert.Same(canonicalTypeInter, localFieldSymbol.[Type])
            Assert.Same(canonicalTypeInter, param.First().[Type])
            Assert.IsAssignableFrom(GetType(VisualBasic.Symbols.Metadata.PE.PENamedTypeSymbol), param.First().[Type])
        End Sub

        <Fact>
        Public Sub NoPIALocalTypesEquivalentToEachOtherEnumAsReturnTypeInExternalAssembly()
            'Enum - As return type in external assembly

            Dim compilationDef =
<compilation name="Dummy">
    <file name="Dummy.vb">
Module TypeSubstitution

    Dim myLocalType As FooEnum = 0

    public Sub Main()
       FooEnum myLocalType = 0
       myLocalType = ExternalAsm1.Scen3(5)
    End Sub
End Module
    </file>
</compilation>


            Dim localConsumer = CompilationUtils.CreateCompilationWithReferences(compilationDef,
                                                                                 {TestReferences.SymbolsTests.NoPia.GeneralPia,
                                                                                  TestReferences.SymbolsTests.NoPia.ExternalAsm1})

            Dim localConsumerRefsAsm = localConsumer.[Assembly].GetNoPiaResolutionAssemblies()
            Dim canonicalType = localConsumerRefsAsm.First(Function(arg) arg.Name = "GeneralPia").GlobalNamespace.GetTypeMembers("FooEnum").[Single]()
            Dim classLocalType As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("TypeSubstitution").[Single]()
            Dim localFieldSymbol As FieldSymbol = classLocalType.GetMembers("myLocalType").OfType(Of FieldSymbol)().[Single]()
            Dim classRefLocalType As NamedTypeSymbol = localConsumerRefsAsm.First(Function(arg) arg.Name = "ExternalAsm1").GlobalNamespace.GetTypeMembers("ExternalAsm1").[Single]()
            Dim methodSymbol As MethodSymbol = classRefLocalType.GetMembers("Scen3").OfType(Of MethodSymbol)().[Single]()
            Dim missing As NoPiaMissingCanonicalTypeSymbol = DirectCast(methodSymbol.ReturnType, NoPiaMissingCanonicalTypeSymbol)
            Assert.Same(localConsumerRefsAsm.First(Function(arg) arg.Name = "ExternalAsm1"), missing.EmbeddingAssembly)
            Assert.Null(missing.Guid)
            Assert.Equal(canonicalType.ToTestDisplayString(), missing.FullTypeName)
            Assert.Equal("f9c2d51d-4f44-45f0-9eda-c9d599b58257", missing.Scope)
            Assert.Equal(canonicalType.ToTestDisplayString(), missing.Identifier)
            Assert.Same(canonicalType, localFieldSymbol.[Type])
            Assert.IsType(Of NoPiaMissingCanonicalTypeSymbol)(methodSymbol.ReturnType)
        End Sub

        <Fact>
        <WorkItem(531054, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems?_a=edit&id=531054")>
        Public Sub NoPIALocalTypesEquivalentToEachOtherInterfaceAsReturnTypeInExternalAssembly()
            ' Interface - As property in external assembly
            Dim localTypeSource =
<compilation><file>
class TypeSubstitution
    Dim myLocalType As ISubFuncProp = ExternalAsm1.Scen4
End Class
</file></compilation>
            Dim localConsumer = CreateCompilationWithReferences(localTypeSource, references:={TestReferences.SymbolsTests.NoPia.GeneralPia, TestReferences.SymbolsTests.NoPia.ExternalAsm1})
            Dim localConsumerRefsAsm = localConsumer.[Assembly].GetNoPiaResolutionAssemblies()
            Dim canonicalType = localConsumerRefsAsm.First(Function(arg) arg.Name = "GeneralPia").GlobalNamespace.GetTypeMembers("ISubFuncProp").[Single]()
            Dim classLocalType As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("TypeSubstitution").[Single]()
            Dim localFieldSymbol As FieldSymbol = classLocalType.GetMembers("myLocalType").OfType(Of FieldSymbol)().[Single]()
            Dim classRefLocalType As NamedTypeSymbol = localConsumerRefsAsm.First(Function(arg) arg.Name = "ExternalAsm1").GlobalNamespace.GetTypeMembers("ExternalAsm1").[Single]()
            Dim propertySymbol = classRefLocalType.GetMembers("Scen4").OfType(Of PropertySymbol)().[Single]()
            Dim propertType = propertySymbol.Type
            Assert.Equal(canonicalType.ToTestDisplayString(), propertType.Name)
            Assert.Same(canonicalType, localFieldSymbol.[Type])
            Assert.IsAssignableFrom(Of VisualBasic.Symbols.Metadata.PE.PENamedTypeSymbol)(propertySymbol.Type)
        End Sub

        <Fact>
        <WorkItem(531054, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems?_a=edit&id=531054")>
        Public Sub NoPIALocalTypesEquivalentToEachOtherDelegateAsReturnTypeInExternalAssembly()
            'Same as previous scenario but with Delegate
            Dim localTypeSource =
<compilation><file>
class TypeSubstitution
    Dim myLocalType As GeneralEventScenario.EventHandler = ExternalAsm1.Scen5
End Class 
</file></compilation>
            Dim localConsumer = CreateCompilationWithReferences(localTypeSource, references:={TestReferences.SymbolsTests.NoPia.GeneralPia, TestReferences.SymbolsTests.NoPia.ExternalAsm1})
            Dim localConsumerRefsAsm = localConsumer.[Assembly].GetNoPiaResolutionAssemblies()
            Dim canonicalType = localConsumerRefsAsm(0).GlobalNamespace.ChildNamespace("GeneralEventScenario")
            Dim canonicalTypeInter = canonicalType.GetTypeMembers("EventHandler").[Single]()
            Dim classLocalType As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("TypeSubstitution").[Single]()
            Dim localFieldSymbol As FieldSymbol = classLocalType.GetMembers("myLocalType").OfType(Of FieldSymbol)().[Single]()
            Dim classRefLocalType As NamedTypeSymbol = localConsumerRefsAsm.First(Function(arg) arg.Name = "ExternalAsm1").GlobalNamespace.GetTypeMembers("ExternalAsm1").[Single]()
            Dim propertySymbol = classRefLocalType.GetMembers("Scen5").OfType(Of PropertySymbol)().[Single]()
            Dim missing As NoPiaMissingCanonicalTypeSymbol = DirectCast(propertySymbol.Type, NoPiaMissingCanonicalTypeSymbol)
            Assert.Same(localConsumerRefsAsm.First(Function(arg) arg.Name = "ExternalAsm1"), missing.EmbeddingAssembly)
            Assert.Null(missing.Guid)
            Assert.Equal("f9c2d51d-4f44-45f0-9eda-c9d599b58257", missing.Scope)
            Assert.Equal("GeneralEventScenario.EventHandler", missing.Identifier)
            Assert.Same(canonicalTypeInter, localFieldSymbol.[Type])
            Assert.IsType(Of NoPiaMissingCanonicalTypeSymbol)(propertySymbol.Type)
        End Sub

        <Fact>
        Public Sub NoPIATypeSubstitutionForClassThatImplementNoPiaInterface()
            'Check type substitution when a class implement a PIA interface
            Dim compilationDef =
<compilation name="Dummy">
    <file name="Dummy.vb">
    </file>
</compilation>

            Dim localConsumer = CompilationUtils.CreateCompilationWithReferences(compilationDef,
                {TestReferences.SymbolsTests.NoPia.GeneralPia, TestReferences.SymbolsTests.NoPia.ExternalAsm1})

            Dim localConsumerRefsAsm = localConsumer.[Assembly].GetNoPiaResolutionAssemblies()
            Dim canonicalType = localConsumerRefsAsm.First(Function(arg) arg.Name = "GeneralPia").GlobalNamespace.GetTypeMembers("ISubFuncProp").[Single]()
            Dim classRefLocalType As NamedTypeSymbol = localConsumerRefsAsm.First(Function(arg) arg.Name = "ExternalAsm1").GlobalNamespace.GetTypeMembers("SubFuncProp").[Single]()
            Dim methodSymbol As MethodSymbol = classRefLocalType.GetMembers("Foo").OfType(Of MethodSymbol)().[Single]()
            Dim interfaceType = classRefLocalType.Interfaces.First()
            Assert.Same(canonicalType, interfaceType)
            Assert.IsType(Of VisualBasic.Symbols.Metadata.PE.PENamedTypeSymbol)(interfaceType)
        End Sub

    End Class


    Friend Module Extensions

        <Extension()>
        Public Function ChildNamespace(ns As NamespaceSymbol, name As String) As NamespaceSymbol
            Return ns.GetMembers().AsEnumerable().
                Where(Function(n) n.Name.Equals(name)).
                       Cast(Of NamespaceSymbol)().
                       Single()
        End Function
    End Module

End Namespace
