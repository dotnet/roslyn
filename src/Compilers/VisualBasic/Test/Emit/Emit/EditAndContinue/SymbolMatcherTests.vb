' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Roslyn.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class SymbolMatcherTests
        Inherits EditAndContinueTestBase

        <WorkItem(1533, "https://github.com/dotnet/roslyn/issues/1533")>
        <Fact>
        Public Sub PreviousType_ArrayType()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Shared Sub M()
        Dim x As Integer = 0
    End Sub
    Class D : End Class
End Class
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Shared Sub M()
        Dim x() As D = Nothing
    End Sub
    Class D : End Class
End Class
]]></file>
                           </compilation>

            Dim compilation0 = CreateCompilationWithMscorlib(sources0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(sources1)
            Dim matcher = New VisualBasicSymbolMatcher(
                Nothing,
                compilation1.SourceAssembly,
                Nothing,
                compilation0.SourceAssembly,
                Nothing,
                Nothing)
            Dim elementType = compilation1.GetMember(Of TypeSymbol)("C.D")
            Dim member = compilation1.CreateArrayTypeSymbol(elementType)
            Dim other = matcher.MapReference(member)
            Assert.NotNull(other)
        End Sub

        <WorkItem(1533, "https://github.com/dotnet/roslyn/issues/1533")>
        <Fact>
        Public Sub NoPreviousType_ArrayType()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Shared Sub M()
        Dim x As Integer = 0
    End Sub
End Class
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Shared Sub M()
        Dim x() As D = Nothing
    End Sub
    Class D : End Class
End Class
]]></file>
                           </compilation>

            Dim compilation0 = CreateCompilationWithMscorlib(sources0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(sources1)
            Dim matcher = New VisualBasicSymbolMatcher(
                Nothing,
                compilation1.SourceAssembly,
                Nothing,
                compilation0.SourceAssembly,
                Nothing,
                Nothing)
            Dim elementType = compilation1.GetMember(Of TypeSymbol)("C.D")
            Dim member = compilation1.CreateArrayTypeSymbol(elementType)
            Dim other = matcher.MapReference(member)
            ' For a newly added type, there is no match in the previous generation.
            Assert.Null(other)
        End Sub

        <WorkItem(1533, "https://github.com/dotnet/roslyn/issues/1533")>
        <Fact>
        Public Sub NoPreviousType_GenericType()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Collections.Generic
Class C
    Shared Sub M()
        Dim x As Integer = 0
    End Sub
End Class
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Collections.Generic
Class C
    Shared Sub M()
        Dim x As List(Of D) = Nothing
    End Sub
    Class D : End Class
    Dim y As List(Of D)
End Class
]]></file>
                           </compilation>

            Dim compilation0 = CreateCompilationWithMscorlib(sources0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(sources1)
            Dim matcher = New VisualBasicSymbolMatcher(
                Nothing,
                compilation1.SourceAssembly,
                Nothing,
                compilation0.SourceAssembly,
                Nothing,
                Nothing)
            Dim member = compilation1.GetMember(Of FieldSymbol)("C.y")
            Dim other = matcher.MapReference(DirectCast(member.Type, Cci.ITypeReference))
            ' For a newly added type, there is no match in the previous generation.
            Assert.Null(other)
        End Sub

        <Fact>
        Public Sub HoistedAnonymousTypes()
            Dim source0 = "
Imports System

Class C
    Shared Sub F()
        Dim x1 = New With { .A = 1 }
        Dim x2 = New With { .B = 1 }
        Dim y = New Func(Of Integer)(Function() x1.A + x2.B)
    End Sub
End Class
"
            Dim source1 = "
Imports System

Class C
    Shared Sub F()
        Dim x1 = New With { .A = 1 }
        Dim x2 = New With { .C = 1 }
        Dim y = New Func(Of Integer)(Function() x1.A + x2.C)
    End Sub
End Class
"

            Dim compilation0 = CreateCompilationWithMscorlib({source0}, options:=TestOptions.DebugDll)

            Dim peRef0 = compilation0.EmitToImageReference()
            Dim peAssemblySymbol0 = DirectCast(CreateCompilationWithMscorlib({""}, {peRef0}).GetReferencedAssemblySymbol(peRef0), PEAssemblySymbol)
            Dim peModule0 = DirectCast(peAssemblySymbol0.Modules(0), PEModuleSymbol)

            Dim reader0 = peModule0.Module.MetadataReader
            Dim decoder0 = New MetadataDecoder(peModule0)

            Dim anonymousTypeMap0 = PEDeltaAssemblyBuilder.GetAnonymousTypeMapFromMetadata(reader0, decoder0)
            Assert.Equal("VB$AnonymousType_0", anonymousTypeMap0(New AnonymousTypeKey(ImmutableArray.Create(New AnonymousTypeKeyField("A", isKey:=False, ignoreCase:=True)))).Name)
            Assert.Equal("VB$AnonymousType_1", anonymousTypeMap0(New AnonymousTypeKey(ImmutableArray.Create(New AnonymousTypeKeyField("B", isKey:=False, ignoreCase:=True)))).Name)
            Assert.Equal(2, anonymousTypeMap0.Count)

            Dim compilation1 = CreateCompilationWithMscorlib({source1}, options:=TestOptions.DebugDll)

            Dim testData = New CompilationTestData()
            compilation1.EmitToArray(testData:=testData)
            Dim peAssemblyBuilder = DirectCast(testData.Module, PEAssemblyBuilder)

            Dim c = compilation1.GetMember(Of NamedTypeSymbol)("C")
            Dim displayClass = peAssemblyBuilder.GetSynthesizedTypes(c).Single()
            Assert.Equal("_Closure$__1-0", displayClass.Name)

            Dim emitContext = New EmitContext(peAssemblyBuilder, Nothing, New DiagnosticBag())

            Dim fields = displayClass.GetFields(emitContext).ToArray()
            Dim x1 = fields(0)
            Dim x2 = fields(1)
            Assert.Equal("$VB$Local_x1", x1.Name)
            Assert.Equal("$VB$Local_x2", x2.Name)

            Dim matcher = New VisualBasicSymbolMatcher(anonymousTypeMap0, compilation1.SourceAssembly, emitContext, peAssemblySymbol0)

            Dim mappedX1 = DirectCast(matcher.MapDefinition(x1), Cci.IFieldDefinition)
            Dim mappedX2 = DirectCast(matcher.MapDefinition(x2), Cci.IFieldDefinition)

            Assert.Equal("$VB$Local_x1", mappedX1.Name)
            Assert.Null(mappedX2)
        End Sub

        <Fact>
        Public Sub HoistedAnonymousTypes_Complex()
            Dim source0 = "
Imports System

Class C
    Shared Sub F()
        Dim x1 = { New With { .A = New With { .X = 1 } } }
        Dim x2 = { New With { .A = New With { .Y = 1 } } }
        Dim y = New Func(Of Integer)(Function() x1(0).A.X + x2(0).A.Y)
    End Sub
End Class
"
            Dim source1 = "
Imports System

Class C
    Shared Sub F()
        Dim x1 = { New With { .A = New With { .X = 1 } } }
        Dim x2 = { New With { .A = New With { .Z = 1 } } }
        Dim y = New Func(Of Integer)(Function() x1(0).A.X + x2(0).A.Z)
    End Sub
End Class
"

            Dim compilation0 = CreateCompilationWithMscorlib({source0}, options:=TestOptions.DebugDll)

            Dim peRef0 = compilation0.EmitToImageReference()
            Dim peAssemblySymbol0 = DirectCast(CreateCompilationWithMscorlib({""}, {peRef0}).GetReferencedAssemblySymbol(peRef0), PEAssemblySymbol)
            Dim peModule0 = DirectCast(peAssemblySymbol0.Modules(0), PEModuleSymbol)

            Dim reader0 = peModule0.Module.MetadataReader
            Dim decoder0 = New MetadataDecoder(peModule0)

            Dim anonymousTypeMap0 = PEDeltaAssemblyBuilder.GetAnonymousTypeMapFromMetadata(reader0, decoder0)
            Assert.Equal("VB$AnonymousType_0", anonymousTypeMap0(New AnonymousTypeKey(ImmutableArray.Create(New AnonymousTypeKeyField("A", isKey:=False, ignoreCase:=True)))).Name)
            Assert.Equal("VB$AnonymousType_1", anonymousTypeMap0(New AnonymousTypeKey(ImmutableArray.Create(New AnonymousTypeKeyField("X", isKey:=False, ignoreCase:=True)))).Name)
            Assert.Equal("VB$AnonymousType_2", anonymousTypeMap0(New AnonymousTypeKey(ImmutableArray.Create(New AnonymousTypeKeyField("Y", isKey:=False, ignoreCase:=True)))).Name)
            Assert.Equal(3, anonymousTypeMap0.Count)

            Dim compilation1 = CreateCompilationWithMscorlib({source1}, options:=TestOptions.DebugDll)

            Dim testData = New CompilationTestData()
            compilation1.EmitToArray(testData:=testData)
            Dim peAssemblyBuilder = DirectCast(testData.Module, PEAssemblyBuilder)

            Dim c = compilation1.GetMember(Of NamedTypeSymbol)("C")
            Dim displayClass = peAssemblyBuilder.GetSynthesizedTypes(c).Single()
            Assert.Equal("_Closure$__1-0", displayClass.Name)

            Dim emitContext = New EmitContext(peAssemblyBuilder, Nothing, New DiagnosticBag())

            Dim fields = displayClass.GetFields(emitContext).ToArray()
            Dim x1 = fields(0)
            Dim x2 = fields(1)
            Assert.Equal("$VB$Local_x1", x1.Name)
            Assert.Equal("$VB$Local_x2", x2.Name)

            Dim matcher = New VisualBasicSymbolMatcher(anonymousTypeMap0, compilation1.SourceAssembly, emitContext, peAssemblySymbol0)

            Dim mappedX1 = DirectCast(matcher.MapDefinition(x1), Cci.IFieldDefinition)
            Dim mappedX2 = DirectCast(matcher.MapDefinition(x2), Cci.IFieldDefinition)

            Assert.Equal("$VB$Local_x1", mappedX1.Name)
            Assert.Null(mappedX2)
        End Sub

        <Fact>
        Public Sub HoistedAnonymousDelegate()
            Dim source0 = "
Imports System

Class C
    Shared Sub F()
        Dim x1 = Function(a As Integer) 1
        Dim x2 = Function(b As Integer) 1
        Dim y = New Func(Of Integer)(Function() x1(1) + x2(1))
    End Sub
End Class
"
            Dim source1 = "
Imports System

Class C
    Shared Sub F()
        Dim x1 = Function(a As Integer) 1
        Dim x2 = Function(c As Integer) 1
        Dim y = New Func(Of Integer)(Function() x1(1) + x2(1))
    End Sub
End Class
"

            Dim compilation0 = CreateCompilationWithMscorlib({source0}, options:=TestOptions.DebugDll)

            Dim peRef0 = compilation0.EmitToImageReference()
            Dim peAssemblySymbol0 = DirectCast(CreateCompilationWithMscorlib({""}, {peRef0}).GetReferencedAssemblySymbol(peRef0), PEAssemblySymbol)
            Dim peModule0 = DirectCast(peAssemblySymbol0.Modules(0), PEModuleSymbol)

            Dim reader0 = peModule0.Module.MetadataReader
            Dim decoder0 = New MetadataDecoder(peModule0)

            Dim anonymousTypeMap0 = PEDeltaAssemblyBuilder.GetAnonymousTypeMapFromMetadata(reader0, decoder0)
            Assert.Equal("VB$AnonymousDelegate_0", anonymousTypeMap0(New AnonymousTypeKey(ImmutableArray.Create(
                New AnonymousTypeKeyField("A", isKey:=False, ignoreCase:=True),
                New AnonymousTypeKeyField(AnonymousTypeDescriptor.FunctionReturnParameterName, isKey:=False, ignoreCase:=True)), isDelegate:=True)).Name)

            Assert.Equal("VB$AnonymousDelegate_1", anonymousTypeMap0(New AnonymousTypeKey(ImmutableArray.Create(
                New AnonymousTypeKeyField("B", isKey:=False, ignoreCase:=True),
                New AnonymousTypeKeyField(AnonymousTypeDescriptor.FunctionReturnParameterName, isKey:=False, ignoreCase:=True)), isDelegate:=True)).Name)

            Assert.Equal(2, anonymousTypeMap0.Count)

            Dim compilation1 = CreateCompilationWithMscorlib({source1}, options:=TestOptions.DebugDll)

            Dim testData = New CompilationTestData()
            compilation1.EmitToArray(testData:=testData)
            Dim peAssemblyBuilder = DirectCast(testData.Module, PEAssemblyBuilder)

            Dim c = compilation1.GetMember(Of NamedTypeSymbol)("C")
            Dim displayClasses = peAssemblyBuilder.GetSynthesizedTypes(c).ToArray()
            Assert.Equal("_Closure$__1-0", displayClasses(0).Name)
            Assert.Equal("_Closure$__", displayClasses(1).Name)

            Dim emitContext = New EmitContext(peAssemblyBuilder, Nothing, New DiagnosticBag())

            Dim fields = displayClasses(0).GetFields(emitContext).ToArray()
            Dim x1 = fields(0)
            Dim x2 = fields(1)
            Assert.Equal("$VB$Local_x1", x1.Name)
            Assert.Equal("$VB$Local_x2", x2.Name)

            Dim matcher = New VisualBasicSymbolMatcher(anonymousTypeMap0, compilation1.SourceAssembly, emitContext, peAssemblySymbol0)

            Dim mappedX1 = DirectCast(matcher.MapDefinition(x1), Cci.IFieldDefinition)
            Dim mappedX2 = DirectCast(matcher.MapDefinition(x2), Cci.IFieldDefinition)

            Assert.Equal("$VB$Local_x1", mappedX1.Name)
            Assert.Null(mappedX2)
        End Sub
    End Class
End Namespace
