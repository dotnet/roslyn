' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Roslyn.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports System.Threading.Tasks
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class SymbolMatcherTests
        Inherits EditAndContinueTestBase

        <Fact>
        Public Sub ConcurrentAccess()
            Dim source = "
Class A
    Dim F As B
    Property P As D
    Sub M(a As A, b As B, s As S, i As I) : End Sub
    Delegate Sub D(s As S)
    Class B : End Class
    Structure S : End Structure
    Interface I : End Interface
End Class

Class B
    Function M(Of T, U)() As A 
        Return Nothing
    End Function

    Event E As D
    Delegate Sub D(s As S)
    Structure S : End Structure
    Interface I : End Interface
End Class"
            Dim compilation0 = CreateCompilationWithMscorlib40({source}, options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.Clone()

            compilation0.VerifyDiagnostics()

            Dim builder = New List(Of Symbol)()

            Dim type = compilation1.GetMember(Of NamedTypeSymbol)("A")
            builder.Add(type)
            builder.AddRange(type.GetMembers())

            type = compilation1.GetMember(Of NamedTypeSymbol)("B")
            builder.Add(type)
            builder.AddRange(type.GetMembers())

            Dim members = builder.ToImmutableArray()
            Assert.True(members.Length > 10)

            For i = 0 To 10 - 1
                Dim matcher = CreateMatcher(compilation1, compilation0)

                Dim tasks(10) As Task

                For j = 0 To tasks.Length - 1
                    Dim startAt As Integer = i + j + 1
                    tasks(j) = Task.Run(Sub()
                                            MatchAll(matcher, members, startAt)
                                            Thread.Sleep(10)
                                        End Sub)
                Next

                Task.WaitAll(tasks)
            Next
        End Sub

        Private Shared Sub MatchAll(matcher As VisualBasicSymbolMatcher, members As ImmutableArray(Of Symbol), startAt As Integer)
            Dim n As Integer = members.Length
            For i = 0 To n - 1
                Dim member = members((i + startAt) Mod n)
                Dim other = matcher.MapDefinition(DirectCast(member.GetCciAdapter(), Cci.IDefinition))
                Assert.NotNull(other)
            Next
        End Sub

        <Fact>
        Public Sub TypeArguments()
            Dim source = "
Class A(Of T)
    Class B(Of U)
        Shared Function M(Of V)(x As A(Of U).B(Of T), y As A(Of Object).S) As A(Of V)
            Return Nothing
        End Function

        Shared Function M(Of V)(x As A(Of U).B(Of T), y As A(Of V).S) As A(Of V) 
            Return Nothing
        End Function
    End Class

    Structure S
    End Structure
End Class
"
            Dim compilation0 = CreateCompilationWithMscorlib40({source}, options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.Clone()

            compilation0.VerifyDiagnostics()

            Dim matcher = CreateMatcher(compilation1, compilation0)
            Dim members = compilation1.GetMember(Of NamedTypeSymbol)("A.B").GetMembers("M")

            Assert.Equal(members.Length, 2)
            For Each member In members
                Dim other = matcher.MapDefinition(DirectCast(member.GetCciAdapter(), Cci.IMethodDefinition))
                Assert.NotNull(other)
            Next
        End Sub

        <Fact>
        Public Sub Constraints()
            Dim source = "
Interface I(Of T AS I(Of T))
End Interface

Class C
    Shared Sub M(Of T As I(Of T))(o As I(Of T))
    End Sub
End Class
"
            Dim compilation0 = CreateCompilationWithMscorlib40({source}, options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(source)
            Dim matcher = CreateMatcher(compilation1, compilation0)
            Dim member = compilation1.GetMember(Of MethodSymbol)("C.M")
            Dim other = matcher.MapDefinition(member.GetCciAdapter())
            Assert.NotNull(other)
        End Sub

        <Fact>
        Public Sub CustomModifiers()
            Dim ilSource = "
.class public abstract A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance object modopt(A) [] F(int32 modopt(object) p) { }
}
"
            Dim metadataRef = CompileIL(ilSource)
            Dim source = "
Class B 
    Inherits A

    Public Overrides Function F(p As Integer) As Object()
        Return Nothing
    End Function
End Class
"
            Dim compilation0 = CreateCompilationWithMscorlib40({source}, options:=TestOptions.DebugDll, references:={metadataRef})
            Dim compilation1 = compilation0.Clone()

            compilation0.VerifyDiagnostics()

            Dim member1 = compilation1.GetMember(Of MethodSymbol)("B.F")
            Assert.Equal(DirectCast(member1.ReturnType, ArrayTypeSymbol).CustomModifiers.Length, 1)

            Dim matcher = CreateMatcher(compilation1, compilation0)
            Dim other = DirectCast(matcher.MapDefinition(member1.GetCciAdapter()).GetInternalSymbol(), MethodSymbol)
            Assert.NotNull(other)
            Assert.Equal(DirectCast(other.ReturnType, ArrayTypeSymbol).CustomModifiers.Length, 1)
        End Sub

        <Fact>
        Public Sub VaryingCompilationReferences()
            Dim libSource = "
Public Class D
End Class
"
            Dim source = "
Public Class C
    Public Sub F(a As D)
    End Sub
End Class
"
            Dim lib0 = CreateCompilationWithMscorlib40({libSource}, options:=TestOptions.DebugDll, assemblyName:="Lib")
            Dim lib1 = CreateCompilationWithMscorlib40({libSource}, options:=TestOptions.DebugDll, assemblyName:="Lib")

            Dim compilation0 = CreateCompilationWithMscorlib40({source}, {lib0.ToMetadataReference()}, options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(source).WithReferences(MscorlibRef, lib1.ToMetadataReference())

            Dim matcher = CreateMatcher(compilation1, compilation0)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim mf1 = matcher.MapDefinition(f1.GetCciAdapter()).GetInternalSymbol()
            Assert.Equal(f0, mf1)
        End Sub

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

            Dim compilation0 = CreateCompilationWithMscorlib40(sources0, options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(sources1)
            Dim matcher = CreateMatcher(compilation1, compilation0)
            Dim elementType = compilation1.GetMember(Of TypeSymbol)("C.D")
            Dim member = compilation1.CreateArrayTypeSymbol(elementType)
            Dim other = matcher.MapReference(member.GetCciAdapter())
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

            Dim compilation0 = CreateCompilationWithMscorlib40(sources0, options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(sources1)
            Dim matcher = CreateMatcher(compilation1, compilation0)
            Dim elementType = compilation1.GetMember(Of TypeSymbol)("C.D")
            Dim member = compilation1.CreateArrayTypeSymbol(elementType)
            Dim other = matcher.MapReference(member.GetCciAdapter())
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

            Dim compilation0 = CreateCompilationWithMscorlib40(sources0, options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(sources1)
            Dim matcher = CreateMatcher(compilation1, compilation0)
            Dim member = compilation1.GetMember(Of FieldSymbol)("C.y")
            Dim other = matcher.MapReference(DirectCast(member.Type.GetCciAdapter(), Cci.ITypeReference))
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

            Dim compilation0 = CreateCompilationWithMscorlib40({source0}, options:=TestOptions.DebugDll)

            Dim peRef0 = compilation0.EmitToImageReference()
            Dim peAssemblySymbol0 = DirectCast(CreateCompilationWithMscorlib40({""}, {peRef0}).GetReferencedAssemblySymbol(peRef0), PEAssemblySymbol)
            Dim peModule0 = DirectCast(peAssemblySymbol0.Modules(0), PEModuleSymbol)

            Dim reader0 = peModule0.Module.MetadataReader
            Dim decoder0 = New MetadataDecoder(peModule0)

            Dim synthesizedTypes0 = PEDeltaAssemblyBuilder.GetSynthesizedTypesFromMetadata(reader0, decoder0)
            Dim anonymousTypeMap0 = synthesizedTypes0.AnonymousTypes
            Assert.Equal("VB$AnonymousType_0", anonymousTypeMap0(New AnonymousTypeKey(ImmutableArray.Create(New AnonymousTypeKeyField("A", isKey:=False, ignoreCase:=True)))).Name)
            Assert.Equal("VB$AnonymousType_1", anonymousTypeMap0(New AnonymousTypeKey(ImmutableArray.Create(New AnonymousTypeKeyField("B", isKey:=False, ignoreCase:=True)))).Name)
            Assert.Equal(2, anonymousTypeMap0.Count)

            Dim compilation1 = CreateCompilationWithMscorlib40({source1}, options:=TestOptions.DebugDll)

            Dim testData = New CompilationTestData()
            compilation1.EmitToArray(testData:=testData)
            Dim peAssemblyBuilder = DirectCast(testData.Module, PEAssemblyBuilder)

            Dim c = compilation1.GetMember(Of NamedTypeSymbol)("C")
            Dim displayClass = peAssemblyBuilder.GetSynthesizedTypes(c).Single()
            Assert.Equal("_Closure$__1-0", displayClass.Name)

            Dim emitContext = New EmitContext(peAssemblyBuilder, Nothing, New DiagnosticBag(), metadataOnly:=False, includePrivateMembers:=True)

            Dim fields = displayClass.GetFields(emitContext).ToArray()
            Dim x1 = fields(0)
            Dim x2 = fields(1)
            Assert.Equal("$VB$Local_x1", x1.Name)
            Assert.Equal("$VB$Local_x2", x2.Name)

            Dim matcher = New VisualBasicSymbolMatcher(
                synthesizedTypes0,
                compilation1.SourceAssembly,
                emitContext,
                peAssemblySymbol0)

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

            Dim compilation0 = CreateCompilationWithMscorlib40({source0}, options:=TestOptions.DebugDll)

            Dim peRef0 = compilation0.EmitToImageReference()
            Dim peAssemblySymbol0 = DirectCast(CreateCompilationWithMscorlib40({""}, {peRef0}).GetReferencedAssemblySymbol(peRef0), PEAssemblySymbol)
            Dim peModule0 = DirectCast(peAssemblySymbol0.Modules(0), PEModuleSymbol)

            Dim reader0 = peModule0.Module.MetadataReader
            Dim decoder0 = New MetadataDecoder(peModule0)

            Dim synthesizedTypes0 = PEDeltaAssemblyBuilder.GetSynthesizedTypesFromMetadata(reader0, decoder0)
            Dim anonymousTypeMap0 = synthesizedTypes0.AnonymousTypes
            Assert.Equal("VB$AnonymousType_0", anonymousTypeMap0(New AnonymousTypeKey(ImmutableArray.Create(New AnonymousTypeKeyField("A", isKey:=False, ignoreCase:=True)))).Name)
            Assert.Equal("VB$AnonymousType_1", anonymousTypeMap0(New AnonymousTypeKey(ImmutableArray.Create(New AnonymousTypeKeyField("X", isKey:=False, ignoreCase:=True)))).Name)
            Assert.Equal("VB$AnonymousType_2", anonymousTypeMap0(New AnonymousTypeKey(ImmutableArray.Create(New AnonymousTypeKeyField("Y", isKey:=False, ignoreCase:=True)))).Name)
            Assert.Equal(3, anonymousTypeMap0.Count)

            Dim compilation1 = CreateCompilationWithMscorlib40({source1}, options:=TestOptions.DebugDll)

            Dim testData = New CompilationTestData()
            compilation1.EmitToArray(testData:=testData)
            Dim peAssemblyBuilder = DirectCast(testData.Module, PEAssemblyBuilder)

            Dim c = compilation1.GetMember(Of NamedTypeSymbol)("C")
            Dim displayClass = peAssemblyBuilder.GetSynthesizedTypes(c).Single()
            Assert.Equal("_Closure$__1-0", displayClass.Name)

            Dim emitContext = New EmitContext(peAssemblyBuilder, Nothing, New DiagnosticBag(), metadataOnly:=False, includePrivateMembers:=True)

            Dim fields = displayClass.GetFields(emitContext).ToArray()
            Dim x1 = fields(0)
            Dim x2 = fields(1)
            Assert.Equal("$VB$Local_x1", x1.Name)
            Assert.Equal("$VB$Local_x2", x2.Name)

            Dim matcher = New VisualBasicSymbolMatcher(
                synthesizedTypes0,
                compilation1.SourceAssembly,
                emitContext,
                peAssemblySymbol0)

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

            Dim compilation0 = CreateCompilationWithMscorlib40({source0}, options:=TestOptions.DebugDll)

            Dim peRef0 = compilation0.EmitToImageReference()
            Dim peAssemblySymbol0 = DirectCast(CreateCompilationWithMscorlib40({""}, {peRef0}).GetReferencedAssemblySymbol(peRef0), PEAssemblySymbol)
            Dim peModule0 = DirectCast(peAssemblySymbol0.Modules(0), PEModuleSymbol)

            Dim reader0 = peModule0.Module.MetadataReader
            Dim decoder0 = New MetadataDecoder(peModule0)

            Dim synthesizedTypes0 = PEDeltaAssemblyBuilder.GetSynthesizedTypesFromMetadata(reader0, decoder0)
            Dim anonymousTypeMap0 = synthesizedTypes0.AnonymousTypes
            Assert.Equal("VB$AnonymousDelegate_0", anonymousTypeMap0(New AnonymousTypeKey(ImmutableArray.Create(
                New AnonymousTypeKeyField("A", isKey:=False, ignoreCase:=True),
                New AnonymousTypeKeyField(AnonymousTypeDescriptor.FunctionReturnParameterName, isKey:=False, ignoreCase:=True)), isDelegate:=True)).Name)

            Assert.Equal("VB$AnonymousDelegate_1", anonymousTypeMap0(New AnonymousTypeKey(ImmutableArray.Create(
                New AnonymousTypeKeyField("B", isKey:=False, ignoreCase:=True),
                New AnonymousTypeKeyField(AnonymousTypeDescriptor.FunctionReturnParameterName, isKey:=False, ignoreCase:=True)), isDelegate:=True)).Name)

            Assert.Equal(2, anonymousTypeMap0.Count)

            Dim compilation1 = CreateCompilationWithMscorlib40({source1}, options:=TestOptions.DebugDll)

            Dim testData = New CompilationTestData()
            compilation1.EmitToArray(testData:=testData)
            Dim peAssemblyBuilder = DirectCast(testData.Module, PEAssemblyBuilder)

            Dim c = compilation1.GetMember(Of NamedTypeSymbol)("C")
            Dim displayClasses = peAssemblyBuilder.GetSynthesizedTypes(c).ToArray()
            Assert.Equal("_Closure$__", displayClasses(0).Name)
            Assert.Equal("_Closure$__1-0", displayClasses(1).Name)

            Dim emitContext = New EmitContext(peAssemblyBuilder, Nothing, New DiagnosticBag(), metadataOnly:=False, includePrivateMembers:=True)

            Dim fields = displayClasses(1).GetFields(emitContext).ToArray()
            Dim x1 = fields(0)
            Dim x2 = fields(1)
            Assert.Equal("$VB$Local_x1", x1.Name)
            Assert.Equal("$VB$Local_x2", x2.Name)

            Dim matcher = New VisualBasicSymbolMatcher(
                synthesizedTypes0,
                compilation1.SourceAssembly,
                emitContext,
                peAssemblySymbol0)

            Dim mappedX1 = DirectCast(matcher.MapDefinition(x1), Cci.IFieldDefinition)
            Dim mappedX2 = DirectCast(matcher.MapDefinition(x2), Cci.IFieldDefinition)

            Assert.Equal("$VB$Local_x1", mappedX1.Name)
            Assert.Null(mappedX2)
        End Sub

        <Fact>
        Public Sub Method_RenameParameter()
            Dim source0 = "
Class C
    Public Function X(a As Integer) As Integer
        Return a
    End Function
End Class
"
            Dim source1 = "
Class C
    Public Function X(b As Integer) As Integer
        Return b
    End Function
End Class
"
            Dim compilation0 = CreateCompilationWithMscorlib40(source0, options:=TestOptions.DebugDll, references:=ValueTupleRefs)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim matcher = CreateMatcher(compilation1, compilation0)

            Dim member = compilation1.GetMember(Of MethodSymbol)("C.X")
            Dim other = matcher.MapDefinition(member.GetCciAdapter())
            Assert.NotNull(other)
        End Sub

        <Fact>
        Public Sub TupleField_TypeChange()
            Dim source0 = "
Class C
{  
    Public x As (a As Integer, b As Integer)
}"
            Dim source1 = "
Class C
{
    Public x As (a As Integer, b As Boolean)
}"
            Dim compilation0 = CreateCompilationWithMscorlib40(source0, options:=TestOptions.DebugDll, references:=ValueTupleRefs)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim matcher = CreateMatcher(compilation1, compilation0)

            Dim member = compilation1.GetMember(Of FieldSymbol)("C.x")
            Dim other = matcher.MapDefinition(member.GetCciAdapter())
            ' If a type changes within a tuple, we do not expect types to match.
            Assert.Null(other)
        End Sub

        <Fact>
        Public Sub TupleField_NameChange()

            Dim source0 = "
Class C
{  
    Public x As (a As Integer, b As Integer)
}"
            Dim source1 = "
Class C
{
    Public x As (a As Integer, c As Integer)
}"
            Dim compilation0 = CreateCompilationWithMscorlib40(source0, options:=TestOptions.DebugDll, references:=ValueTupleRefs)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim matcher = CreateMatcher(compilation1, compilation0)

            Dim member = compilation1.GetMember(Of FieldSymbol)("C.x")
            Dim other = matcher.MapDefinition(member.GetCciAdapter())
            ' Types must match because just an element name was changed.
            Dim otherSymbol = DirectCast(other.GetInternalSymbol(), SourceFieldSymbol)
            Assert.NotNull(otherSymbol)
            Assert.Equal("C.x As (a As System.Int32, b As System.Int32)", otherSymbol.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub TupleMethod_TypeToNoTupleChange()
            Dim source0 = "
Class C
    Public Function X() As (a As Integer, b As Integer)
        Return Nothing
    End Function
End Class
"
            Dim source1 = "
Class C
    Public Function X() As Integer()
        Return Nothing
    End Function
End Class
"
            Dim compilation0 = CreateCompilationWithMscorlib40(source0, options:=TestOptions.DebugDll, references:=ValueTupleRefs)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim matcher = CreateMatcher(compilation1, compilation0)

            Dim member = compilation1.GetMember(Of MethodSymbol)("C.X")
            Dim other = matcher.MapDefinition(member.GetCciAdapter())
            ' Types should not match: one is tuple and another is not.
            Assert.Null(other)
        End Sub

        <Fact>
        Public Sub TupleMethod_TypeFromNoTupleChange()
            Dim source0 = "
Class C
    Public Function X() As Integer()
        Return Nothing
    End Function
End Class
"
            Dim source1 = "
Class C
    Public Function X() As (a As Integer, b As Boolean)
        Return Nothing
    End Function
End Class
"
            Dim compilation0 = CreateCompilationWithMscorlib40(source0, options:=TestOptions.DebugDll, references:=ValueTupleRefs)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim matcher = CreateMatcher(compilation1, compilation0)

            Dim member = compilation1.GetMember(Of MethodSymbol)("C.X")
            Dim other = matcher.MapDefinition(member.GetCciAdapter())
            ' Types should not match: one is tuple and another is not.
            Assert.Null(other)
        End Sub

        <Fact>
        Public Sub TupleMethod_TypeChange()
            Dim source0 = "
Class C
    Public Function X() As (a As Integer, b As Integer)
        Return Nothing
    End Function
End Class
"
            Dim source1 = "
Class C
    Public Function X() As (a As Integer, b As Boolean)
        Return Nothing
    End Function
End Class
"
            Dim compilation0 = CreateCompilationWithMscorlib40(source0, options:=TestOptions.DebugDll, references:=ValueTupleRefs)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim matcher = CreateMatcher(compilation1, compilation0)

            Dim member = compilation1.GetMember(Of MethodSymbol)("C.X")
            Dim other = matcher.MapDefinition(member.GetCciAdapter())
            ' If a type changes within a tuple, we do not expect types to match.
            Assert.Null(other)
        End Sub

        <Fact>
        Public Sub TupleMethod_NameChange()
            Dim source0 = "
Class C
    Public Function X() As (a As Integer, b As Integer)
        Return Nothing
    End Function
End Class
"
            Dim source1 = "
Class C
    Public Function X() As (a As Integer, c As Integer)
        Return Nothing
    End Function
End Class
"
            Dim compilation0 = CreateCompilationWithMscorlib40(source0, options:=TestOptions.DebugDll, references:=ValueTupleRefs)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim matcher = CreateMatcher(compilation1, compilation0)

            Dim member = compilation1.GetMember(Of MethodSymbol)("C.X")
            Dim other = matcher.MapDefinition(member.GetCciAdapter())
            ' Types must match because just an element name was changed.
            Dim otherSymbol = DirectCast(other.GetInternalSymbol(), SourceMemberMethodSymbol)
            Assert.NotNull(otherSymbol)
            Assert.Equal("Function C.X() As (a As System.Int32, b As System.Int32)", otherSymbol.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub TupleProperty_TypeChange()
            Dim source0 = "
Class C
    Public ReadOnly Property X As (a As Integer, b As Integer)
        Get
            Return Nothing
        End Get
    End Property
End Class
"
            Dim source1 = "
Class C
    Public ReadOnly Property X As (a As Integer, b As Boolean)
        Get
            Return Nothing
        End Get
    End Property
End Class
"
            Dim compilation0 = CreateCompilationWithMscorlib40(source0, options:=TestOptions.DebugDll, references:=ValueTupleRefs)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim matcher = CreateMatcher(compilation1, compilation0)

            Dim member = compilation1.GetMember(Of PropertySymbol)("C.X")
            Dim other = matcher.MapDefinition(member.GetCciAdapter())
            ' If a type changes within a tuple, we do not expect types to match.
            Assert.Null(other)
        End Sub

        <Fact>
        Public Sub TupleProperty_NameChange()
            Dim source0 = "
Class C
    Public ReadOnly Property X As (a As Integer, b As Integer)
        Get
            Return Nothing
        End Get
    End Property
End Class
"
            Dim source1 = "
Class C
    Public ReadOnly Property X As (a As Integer, c As Integer)
        Get
            Return Nothing
        End Get
    End Property
End Class
"
            Dim compilation0 = CreateCompilationWithMscorlib40(source0, options:=TestOptions.DebugDll, references:=ValueTupleRefs)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim matcher = CreateMatcher(compilation1, compilation0)

            Dim member = compilation1.GetMember(Of PropertySymbol)("C.X")
            Dim other = matcher.MapDefinition(member.GetCciAdapter())
            ' Types must match because just an element name was changed.
            Dim otherSymbol = DirectCast(other.GetInternalSymbol(), SourcePropertySymbol)
            Assert.NotNull(otherSymbol)
            Assert.Equal("ReadOnly Property C.X As (a As System.Int32, b As System.Int32)", otherSymbol.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub TupleStructField_TypeChange()
            Dim source0 = "
Public Structure Vector
    Public Coordinates As (x As Integer, y As Integer)
End Structure
"
            Dim source1 = "
Public Structure Vector
    Public Coordinates As (x As Integer, y As Integer, z As Integer)
End Structure
"
            Dim compilation0 = CreateCompilationWithMscorlib40(source0, options:=TestOptions.DebugDll, references:=ValueTupleRefs)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim matcher = CreateMatcher(compilation1, compilation0)

            Dim member = compilation1.GetMember(Of FieldSymbol)("Vector.Coordinates")
            Dim other = matcher.MapDefinition(member.GetCciAdapter())
            ' If a type changes within a tuple, we do not expect types to match.
            Assert.Null(other)
        End Sub

        <Fact>
        Public Sub TupleStructField_NameChange()
            Dim source0 = "
Public Structure Vector
    Public Coordinates As (x As Integer, y As Integer)
End Structure
"
            Dim source1 = "
Public Structure Vector
    Public Coordinates As (x As Integer, z As Integer)
End Structure
"
            Dim compilation0 = CreateCompilationWithMscorlib40(source0, options:=TestOptions.DebugDll, references:=ValueTupleRefs)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim matcher = CreateMatcher(compilation1, compilation0)

            Dim member = compilation1.GetMember(Of FieldSymbol)("Vector.Coordinates")
            Dim other = matcher.MapDefinition(member.GetCciAdapter())
            ' Types must match because just an element name was changed.
            Dim otherSymbol = DirectCast(other.GetInternalSymbol(), SourceFieldSymbol)
            Assert.NotNull(otherSymbol)
            Assert.Equal("Vector.Coordinates As (x As System.Int32, y As System.Int32)", otherSymbol.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub TupleDelegate_TypeChange()
            Dim source0 = "
Public Class C
    Public Delegate Function F() As (Integer, Integer)
End Class
"
            Dim source1 = "
Public Class C
    Public Delegate Function F() As (Integer, Boolean)
End Class
"
            Dim compilation0 = CreateCompilationWithMscorlib40(source0, options:=TestOptions.DebugDll, references:=ValueTupleRefs)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim matcher = CreateMatcher(compilation1, compilation0)

            Dim member = compilation1.GetMember(Of SourceNamedTypeSymbol)("C.F")
            Dim other = matcher.MapDefinition(member.GetCciAdapter())
            ' Tuple delegate defines a type. We should be able to match old and new types by name.
            Dim otherSymbol = DirectCast(other.GetInternalSymbol(), SourceNamedTypeSymbol)
            Assert.NotNull(otherSymbol)
            Assert.Equal("C.F", otherSymbol.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub TupleDelegate_NameChange()
            Dim source0 = "
Public Class C
    Public Delegate Function F() As (x as Integer, y as Integer)
End Class
"
            Dim source1 = "
Public Class C
    Public Delegate Function F() As (x as Integer, z as Integer)
End Class"
            Dim compilation0 = CreateCompilationWithMscorlib40(source0, options:=TestOptions.DebugDll, references:=ValueTupleRefs)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim matcher = CreateMatcher(compilation1, compilation0)

            Dim member = compilation1.GetMember(Of SourceNamedTypeSymbol)("C.F")
            Dim other = matcher.MapDefinition(member.GetCciAdapter())
            ' Types must match because just an element name was changed.
            Dim otherSymbol = DirectCast(other.GetInternalSymbol(), SourceNamedTypeSymbol)
            Assert.NotNull(otherSymbol)
            Assert.Equal("C.F", otherSymbol.ToTestDisplayString())
        End Sub
    End Class
End Namespace
