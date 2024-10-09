' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Reflection
Imports System.Reflection.Metadata
Imports System.Reflection.Metadata.Ecma335
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Roslyn.Test.Utilities
Imports Roslyn.Test.Utilities.TestMetadata
Imports Basic.Reference.Assemblies

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit

    Public Class EmitMetadata
        Inherits BasicTestBase

        <Fact, WorkItem(547015, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547015")>
        Public Sub IncorrectCustomAssemblyTableSize_TooManyMethodSpecs()
            Dim source = TestResources.MetadataTests.Invalid.ManyMethodSpecs
            CompileAndVerify(VisualBasicCompilation.Create("Goo", syntaxTrees:={Parse(source)}, references:={MscorlibRef, SystemCoreRef, MsvbRef}))
        End Sub

        <Fact>
        Public Sub InstantiatedGenerics()
            Dim mscorlibRef = Net40.References.mscorlib
            Dim source As String = <text> 
Class A(Of T)

    Public Class B 
        Inherits A(Of T)

        Friend Class C 
            Inherits B
        End Class

        Protected y1 As B
        Protected y2 As A(Of D).B
    End Class

    Public Class H(Of S)
    
        Public Class I 
            Inherits A(Of T).H(Of S)
        End Class
    End Class

    Friend x1 As A(Of T) 
    Friend x2 As A(Of D)
End Class

Public Class D

    Public Class K(Of T)
    
        Public Class L 
            Inherits K(Of T)
        End Class
    End Class
End Class

Namespace NS1

    Class E 
        Inherits D
    End Class
End Namespace

Class F 
    Inherits A(Of D)
End Class

Class G 
    Inherits A(Of NS1.E).B
End Class

Class J 
    Inherits A(Of D).H(Of D)
End Class

Public Class M
End Class

Public Class N 
    Inherits D.K(Of M)
End Class 
</text>.Value

            Dim c1 = VisualBasicCompilation.Create("VB_EmitTest1",
                                        {VisualBasicSyntaxTree.ParseText(source)},
                                        {mscorlibRef},
                                        TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal))

            CompileAndVerify(c1, symbolValidator:=
                Sub([Module])
                    Dim dump = DumpTypeInfo([Module]).ToString()

                    AssertEx.AssertEqualToleratingWhitespaceDifferences("
<Global>
<type name=""&lt;Module&gt;"" />
<type name=""A"" Of=""T"" base=""System.Object"">
  <field name=""x1"" type=""A(Of T)"" />
  <field name=""x2"" type=""A(Of D)"" />
  <type name=""B"" base=""A(Of T)"">
  <field name=""y1"" type=""A(Of T).B"" />
  <field name=""y2"" type=""A(Of D).B"" />
  <type name=""C"" base=""A(Of T).B"" />
  </type>
  <type name=""H"" Of=""S"" base=""System.Object"">
  <type name=""I"" base=""A(Of T).H(Of S)"" />
  </type>
</type>
<type name=""D"" base=""System.Object"">
  <type name=""K"" Of=""T"" base=""System.Object"">
  <type name=""L"" base=""D.K(Of T)"" />
  </type>
</type>
<type name=""F"" base=""A(Of D)"" />
<type name=""G"" base=""A(Of NS1.E).B"" />
<type name=""J"" base=""A(Of D).H(Of D)"" />
<type name=""M"" base=""System.Object"" />
<type name=""N"" base=""D.K(Of M)"" />
<NS1>
  <type name=""E"" base=""D"" />
</NS1>
</Global>
", dump)
                End Sub)
        End Sub

        Private Shared Function DumpTypeInfo(moduleSymbol As ModuleSymbol) As Xml.Linq.XElement
            Return LoadChildNamespace(moduleSymbol.GlobalNamespace)
        End Function

        Friend Shared Function LoadChildNamespace(n As NamespaceSymbol) As Xml.Linq.XElement

            Dim elem As Xml.Linq.XElement = New System.Xml.Linq.XElement((If(n.Name.Length = 0, "Global", n.Name)))

            Dim childrenTypes = n.GetTypeMembers().AsEnumerable().OrderBy(Function(t) t, New TypeComparer())

            elem.Add(From t In childrenTypes Select LoadChildType(t))

            Dim childrenNS = n.GetMembers().
                               Select(Function(m) (TryCast(m, NamespaceSymbol))).
                               Where(Function(m) m IsNot Nothing).
                               OrderBy(Function(child) child.Name, StringComparer.OrdinalIgnoreCase)

            elem.Add(From c In childrenNS Select LoadChildNamespace(c))

            Return elem
        End Function

        Private Shared Function LoadChildType(t As NamedTypeSymbol) As Xml.Linq.XElement
            Dim elem As Xml.Linq.XElement = New System.Xml.Linq.XElement("type")

            elem.Add(New System.Xml.Linq.XAttribute("name", t.Name))

            If t.Arity > 0 Then
                Dim typeParams As String = String.Empty
                For Each param In t.TypeParameters
                    If typeParams.Length > 0 Then
                        typeParams += ","
                    End If
                    typeParams += param.Name
                Next

                elem.Add(New System.Xml.Linq.XAttribute("Of", typeParams))
            End If

            If t.BaseType IsNot Nothing Then
                elem.Add(New System.Xml.Linq.XAttribute("base", t.BaseType.ToTestDisplayString()))
            End If

            Dim fields = t.GetMembers().
                         Where(Function(m) m.Kind = SymbolKind.Field).
                         OrderBy(Function(f) f.Name).Cast(Of FieldSymbol)()

            elem.Add(From f In fields Select LoadField(f))

            Dim childrenTypes = t.GetTypeMembers().AsEnumerable().OrderBy(Function(c) c, New TypeComparer())

            elem.Add(From c In childrenTypes Select LoadChildType(c))

            Return elem
        End Function

        Private Shared Function LoadField(f As FieldSymbol) As Xml.Linq.XElement
            Dim elem As Xml.Linq.XElement = New System.Xml.Linq.XElement("field")
            elem.Add(New System.Xml.Linq.XAttribute("name", f.Name))
            elem.Add(New System.Xml.Linq.XAttribute("type", f.Type.ToTestDisplayString()))
            Return elem
        End Function

        <Fact>
        Public Sub FakeILGen()
            Dim comp = CompilationUtils.CreateEmptyCompilationWithReferences(
<compilation>
    <file name="a.vb"> 
Public Class D

    Public Sub New()
    End Sub

    Public Shared Sub Main()

        System.Console.WriteLine(65536)

        'arrayField = new string[] {&quot;string1&quot;, &quot;string2&quot;}
        'System.Console.WriteLine(arrayField[1])
        'System.Console.WriteLine(arrayField[0])
        System.Console.WriteLine(&quot;string2&quot;)
        System.Console.WriteLine(&quot;string1&quot;)
    End Sub

    Shared arrayField As String()
End Class 
</file>
</compilation>, {Net40.References.mscorlib}, TestOptions.ReleaseExe)

            CompileAndVerify(comp,
                             expectedOutput:=
                                "65536" & Environment.NewLine &
                                "string2" & Environment.NewLine &
                                "string1" & Environment.NewLine)
        End Sub

        <Fact>
        Public Sub AssemblyRefs()
            Dim mscorlibRef = Net40.References.mscorlib
            Dim metadataTestLib1 = TestReferences.SymbolsTests.MDTestLib1
            Dim metadataTestLib2 = TestReferences.SymbolsTests.MDTestLib2

            Dim source As String = <text> 
Public Class Test 
    Inherits C107
End Class
</text>.Value

            Dim c1 = VisualBasicCompilation.Create("VB_EmitAssemblyRefs",
                                        {VisualBasicSyntaxTree.ParseText(source)},
                                        {mscorlibRef, metadataTestLib1, metadataTestLib2},
                                        TestOptions.ReleaseDll)

            Dim dllImage = CompileAndVerify(c1).EmittedAssemblyData

            Using metadata = AssemblyMetadata.CreateFromImage(dllImage)
                Dim emitAssemblyRefs As PEAssembly = metadata.GetAssembly

                Dim refs = emitAssemblyRefs.Modules(0).ReferencedAssemblies.AsEnumerable().OrderBy(Function(r) r.Name).ToArray()

                Assert.Equal(2, refs.Count)
                Assert.Equal(refs(0).Name, "MDTestLib1", StringComparer.OrdinalIgnoreCase)
                Assert.Equal(refs(1).Name, "mscorlib", StringComparer.OrdinalIgnoreCase)
            End Using

            Dim multiModule = TestReferences.SymbolsTests.MultiModule.Assembly

            Dim source2 As String = <text> 
Public Class Test 
    Inherits Class2
End Class 
</text>.Value

            Dim c2 = VisualBasicCompilation.Create("VB_EmitAssemblyRefs2",
                                        {VisualBasicSyntaxTree.ParseText(source2)},
                                        {mscorlibRef, multiModule},
                                        TestOptions.ReleaseDll)

            ' ILVerify: The method or operation is not implemented.
            dllImage = CompileAndVerify(c2, verify:=Verification.FailsILVerify).EmittedAssemblyData

            Using metadata = AssemblyMetadata.CreateFromImage(dllImage)
                Dim emitAssemblyRefs2 As PEAssembly = metadata.GetAssembly
                Dim refs2 = emitAssemblyRefs2.Modules(0).ReferencedAssemblies.AsEnumerable().OrderBy(Function(r) r.Name).ToArray()

                Assert.Equal(2, refs2.Count)
                Assert.Equal(refs2(1).Name, "MultiModule", StringComparer.OrdinalIgnoreCase)
                Assert.Equal(refs2(0).Name, "mscorlib", StringComparer.OrdinalIgnoreCase)

                Dim metadataReader = emitAssemblyRefs2.GetMetadataReader()

                Assert.Equal(0, metadataReader.GetTableRowCount(TableIndex.File))
                Assert.Equal(0, metadataReader.GetTableRowCount(TableIndex.ModuleRef))
            End Using
        End Sub

        <Fact>
        Public Sub AddModule()
            Dim mscorlibRef = Net40.References.mscorlib
            Dim netModule1 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.netModule1)
            Dim netModule2 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.netModule2)

            Dim source As String = <text> 
Public Class Test 
    Inherits Class1
End Class
</text>.Value

            Dim c1 = VisualBasicCompilation.Create("VB_EmitAddModule",
                                        {VisualBasicSyntaxTree.ParseText(source)},
                                        {mscorlibRef, netModule1.GetReference(), netModule2.GetReference()},
                                        TestOptions.ReleaseDll)

            Dim class1 = c1.GlobalNamespace.GetMembers("Class1")
            Assert.Equal(1, class1.Count())

            ' ILVerify: Assembly or module not found: netModule1
            Dim manifestModule = CompileAndVerify(c1, verify:=Verification.FailsILVerify).EmittedAssemblyData

            Using metadata = AssemblyMetadata.Create(ModuleMetadata.CreateFromImage(manifestModule), netModule1, netModule2)
                Dim emitAddModule As PEAssembly = metadata.GetAssembly

                Assert.Equal(3, emitAddModule.Modules.Length)

                Dim reader = emitAddModule.GetMetadataReader()

                Assert.Equal(2, reader.GetTableRowCount(TableIndex.File))
                Dim file1 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(1))
                Dim file2 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(2))

                Assert.Equal("netModule1.netmodule", reader.GetString(file1.Name))
                Assert.Equal("netModule2.netmodule", reader.GetString(file2.Name))
                Assert.False(file1.HashValue.IsNil)
                Assert.False(file2.HashValue.IsNil)

                Dim moduleRefName = reader.GetModuleReference(reader.GetModuleReferences().Single()).Name
                Assert.Equal("netModule1.netmodule", reader.GetString(moduleRefName))

                Dim actual = From h In reader.ExportedTypes
                             Let et = reader.GetExportedType(h)
                             Select $"{reader.GetString(et.NamespaceDefinition)}.{reader.GetString(et.Name)} 0x{MetadataTokens.GetToken(et.Implementation):X8} ({et.Implementation.Kind}) 0x{CInt(et.Attributes):X4}"

                AssertEx.Equal(
                {
                    "NS1.Class4 0x26000001 (AssemblyFile) 0x0001",
                    ".Class7 0x27000001 (ExportedType) 0x0002",
                    ".Class1 0x26000001 (AssemblyFile) 0x0001",
                    ".Class3 0x27000003 (ExportedType) 0x0002",
                    ".Class2 0x26000002 (AssemblyFile) 0x0001"
                }, actual)
            End Using
        End Sub

        <Fact>
        Public Sub ImplementingAnInterface()
            Dim mscorlibRef = Net40.References.mscorlib

            Dim source As String = <text>
Public Interface I1
End Interface

Public Class A 
    Implements I1
End Class

Public Interface I2
    Sub M2()
End Interface

Public Interface I3
    Sub M3()
End Interface

Public MustInherit Class B 
    Implements I2, I3

    Public MustOverride Sub M2() Implements I2.M2
    Public MustOverride Sub M3() Implements I3.M3
End Class 
</text>.Value

            Dim c1 = VisualBasicCompilation.Create("VB_ImplementingAnInterface",
                                                   {VisualBasicSyntaxTree.ParseText(source)},
                                                   {mscorlibRef},
                                                   TestOptions.ReleaseDll)

            CompileAndVerify(c1, symbolValidator:=
                Sub([module])
                    Dim classA = [module].GlobalNamespace.GetTypeMembers("A").Single()
                    Dim classB = [module].GlobalNamespace.GetTypeMembers("B").Single()
                    Dim i1 = [module].GlobalNamespace.GetTypeMembers("I1").Single()
                    Dim i2 = [module].GlobalNamespace.GetTypeMembers("I2").Single()
                    Dim i3 = [module].GlobalNamespace.GetTypeMembers("I3").Single()

                    Assert.Equal(TypeKind.Interface, i1.TypeKind)
                    Assert.Equal(TypeKind.Interface, i2.TypeKind)
                    Assert.Equal(TypeKind.Interface, i3.TypeKind)
                    Assert.Equal(TypeKind.Class, classA.TypeKind)
                    Assert.Equal(TypeKind.Class, classB.TypeKind)
                    Assert.Same(i1, classA.Interfaces.Single())

                    Dim interfaces = classB.Interfaces

                    Assert.Same(i2, interfaces(0))
                    Assert.Same(i3, interfaces(1))
                    Assert.Equal(1, i2.GetMembers("M2").Length)
                    Assert.Equal(1, i3.GetMembers("M3").Length)
                End Sub)
        End Sub

        <Fact>
        Public Sub Types()
            Dim mscorlibRef = Net40.References.mscorlib
            Dim source As String = <text>
Public MustInherit Class A

    Public MustOverride Function M1(ByRef p1 As System.Array) As A()
    Public MustOverride Function M2(p2 As System.Boolean) As A(,)
    Public MustOverride Function M3(p3 As System.Char) As A(,,)
    Public MustOverride Sub M4(p4 As System.SByte,
        p5 As System.Single,
        p6 As System.Double,
        p7 As System.Int16,
        p8 As System.Int32,
        p9 As System.Int64,
        p10 As System.IntPtr,
        p11 As System.String,
        p12 As System.Byte,
        p13 As System.UInt16,
        p14 As System.UInt32,
        p15 As System.UInt64,
        p16 As System.UIntPtr)

    Public MustOverride Sub M5(Of T, S)(p17 As T, p18 As S)
End Class

Friend NotInheritable class B
End Class

Class C

    Public Class D
    End Class

    Friend Class E
    End Class

    Protected Class F
    End Class

    Private Class G
    End Class

    Protected Friend Class H
    End Class
End Class 
</text>.Value

            Dim c1 = VisualBasicCompilation.Create("VB_Types",
                                                   {VisualBasicSyntaxTree.ParseText(source)},
                                                   {mscorlibRef},
                                                   TestOptions.ReleaseDll)

            Dim validator =
                Function(isFromSource As Boolean) _
                    Sub([Module] As ModuleSymbol)
                        Dim classA = [Module].GlobalNamespace.GetTypeMembers("A").Single()
                        Dim m1 = classA.GetMembers("M1").OfType(Of MethodSymbol)().Single()
                        Dim m2 = classA.GetMembers("M2").OfType(Of MethodSymbol)().Single()
                        Dim m3 = classA.GetMembers("M3").OfType(Of MethodSymbol)().Single()
                        Dim m4 = classA.GetMembers("M4").OfType(Of MethodSymbol)().Single()
                        Dim m5 = classA.GetMembers("M5").OfType(Of MethodSymbol)().Single()

                        Dim method1Ret = DirectCast(m1.ReturnType, ArrayTypeSymbol)
                        Dim method2Ret = DirectCast(m2.ReturnType, ArrayTypeSymbol)
                        Dim method3Ret = DirectCast(m3.ReturnType, ArrayTypeSymbol)

                        Assert.Equal(1, method1Ret.Rank)
                        Assert.True(method1Ret.IsSZArray)
                        Assert.Same(classA, method1Ret.ElementType)
                        Assert.Equal(2, method2Ret.Rank)
                        Assert.False(method2Ret.IsSZArray)
                        Assert.Same(classA, method2Ret.ElementType)
                        Assert.Equal(3, method3Ret.Rank)
                        Assert.Same(classA, method3Ret.ElementType)

                        Assert.Null(method1Ret.ContainingSymbol)
                        Assert.Equal(ImmutableArray.Create(Of Location)(), method1Ret.Locations)
                        Assert.Equal(ImmutableArray.Create(Of SyntaxReference)(), method1Ret.DeclaringSyntaxReferences)

                        Assert.True(classA.IsMustInherit)
                        Assert.Equal(Accessibility.Public, classA.DeclaredAccessibility)
                        Dim classB = [Module].GlobalNamespace.GetTypeMembers("B").Single()
                        Assert.True(classB.IsNotInheritable)
                        Assert.Equal(Accessibility.Friend, classB.DeclaredAccessibility)
                        Dim classC = [Module].GlobalNamespace.GetTypeMembers("C").Single()
                        'Assert.True(classC.IsStatic)
                        Assert.Equal(Accessibility.Friend, classC.DeclaredAccessibility)

                        Dim classD = classC.GetTypeMembers("D").Single()
                        Dim classE = classC.GetTypeMembers("E").Single()
                        Dim classF = classC.GetTypeMembers("F").Single()
                        Dim classH = classC.GetTypeMembers("H").Single()

                        Assert.Equal(Accessibility.Public, classD.DeclaredAccessibility)
                        Assert.Equal(Accessibility.Friend, classE.DeclaredAccessibility)
                        Assert.Equal(Accessibility.Protected, classF.DeclaredAccessibility)
                        Assert.Equal(Accessibility.ProtectedOrFriend, classH.DeclaredAccessibility)

                        If isFromSource Then
                            Dim classG = classC.GetTypeMembers("G").Single()
                            Assert.Equal(Accessibility.Private, classG.DeclaredAccessibility)
                        End If

                        Dim parameter1 = m1.Parameters.Single()
                        Dim parameter1Type = parameter1.Type

                        Assert.True(parameter1.IsByRef)
                        Assert.Same([Module].GetCorLibType(SpecialType.System_Array), parameter1Type)
                        Assert.Same([Module].GetCorLibType(SpecialType.System_Boolean), m2.Parameters.Single().Type)
                        Assert.Same([Module].GetCorLibType(SpecialType.System_Char), m3.Parameters.Single().Type)

                        Dim method4ParamTypes = m4.Parameters.Select(Function(p) p.Type).ToArray()

                        Assert.Same([Module].GetCorLibType(SpecialType.System_Void), m4.ReturnType)
                        Assert.Same([Module].GetCorLibType(SpecialType.System_SByte), method4ParamTypes(0))
                        Assert.Same([Module].GetCorLibType(SpecialType.System_Single), method4ParamTypes(1))
                        Assert.Same([Module].GetCorLibType(SpecialType.System_Double), method4ParamTypes(2))
                        Assert.Same([Module].GetCorLibType(SpecialType.System_Int16), method4ParamTypes(3))
                        Assert.Same([Module].GetCorLibType(SpecialType.System_Int32), method4ParamTypes(4))
                        Assert.Same([Module].GetCorLibType(SpecialType.System_Int64), method4ParamTypes(5))
                        Assert.Same([Module].GetCorLibType(SpecialType.System_IntPtr), method4ParamTypes(6))
                        Assert.Same([Module].GetCorLibType(SpecialType.System_String), method4ParamTypes(7))
                        Assert.Same([Module].GetCorLibType(SpecialType.System_Byte), method4ParamTypes(8))
                        Assert.Same([Module].GetCorLibType(SpecialType.System_UInt16), method4ParamTypes(9))
                        Assert.Same([Module].GetCorLibType(SpecialType.System_UInt32), method4ParamTypes(10))
                        Assert.Same([Module].GetCorLibType(SpecialType.System_UInt64), method4ParamTypes(11))
                        Assert.Same([Module].GetCorLibType(SpecialType.System_UIntPtr), method4ParamTypes(12))

                        Assert.True(m5.IsGenericMethod)
                        Assert.Same(m5.TypeParameters(0), m5.Parameters(0).Type)
                        Assert.Same(m5.TypeParameters(1), m5.Parameters(1).Type)

                        If Not isFromSource Then
                            Dim peReader = (DirectCast([Module], PEModuleSymbol)).Module.GetMetadataReader()

                            Dim list = New List(Of String)()
                            For Each typeRef In peReader.TypeReferences
                                list.Add(peReader.GetString(peReader.GetTypeReference(typeRef).Name))
                            Next

                            AssertEx.SetEqual({"CompilationRelaxationsAttribute", "RuntimeCompatibilityAttribute", "DebuggableAttribute", "DebuggingModes", "Object", "Array"}, list)
                        End If
                    End Sub

            CompileAndVerify(c1, symbolValidator:=validator(False), sourceSymbolValidator:=validator(True))
        End Sub

        <Fact>
        Public Sub Fields()
            Dim mscorlibRef = Net40.References.mscorlib
            Dim source As String = <text> 
Public Class A
    public F1 As Integer
End Class 
</text>.Value

            Dim c1 = VisualBasicCompilation.Create("VB_Fields",
                                                   {VisualBasicSyntaxTree.ParseText(source)},
                                                   {mscorlibRef},
                                                   TestOptions.ReleaseDll)

            CompileAndVerify(c1, symbolValidator:=
                Sub(m)
                    Dim classA = m.GlobalNamespace.GetTypeMembers("A").Single()

                    Dim f1 = classA.GetMembers("F1").OfType(Of FieldSymbol)().Single()

                    Assert.Equal(0, f1.CustomModifiers.Length)
                End Sub)
        End Sub

        <Fact()>
        Public Sub EmittedModuleTable()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Public Class A_class
End Class
    </file>
</compilation>, validator:=AddressOf EmittedModuleRecordValidator)
        End Sub

        Private Sub EmittedModuleRecordValidator(assembly As PEAssembly)
            Dim reader = assembly.GetMetadataReader()

            Dim typeDefs As TypeDefinitionHandle() = reader.TypeDefinitions.AsEnumerable().ToArray()
            Assert.Equal(2, typeDefs.Length)

            Assert.Equal("<Module>", reader.GetString(reader.GetTypeDefinition(typeDefs(0)).Name))
            Assert.Equal("A_class", reader.GetString(reader.GetTypeDefinition(typeDefs(1)).Name))

            '  Expected: 0 which is equal to [.class private auto ansi <Module>]
            Assert.Equal(0, reader.GetTypeDefinition(typeDefs(0)).Attributes)
        End Sub

        <WorkItem(543517, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543517")>
        <Fact()>
        Public Sub EmitBeforeFieldInit()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Public Class A_class
End Class

Public Class B_class
    Shared Sub New()
    End Sub
End Class

Public Class C_class
    Shared Fld As Integer = 123
    Shared Sub New()
    End Sub
End Class

Public Class D_class
    Const Fld As Integer = 123
End Class

Public Class E_class
    Const Fld As Date = #12:00:00 AM#
    Shared Sub New()
    End Sub
End Class

Public Class F_class
    Shared Fld As Date = #12:00:00 AM#
End Class

Public Class G_class
    Const Fld As DateTime = #11/04/2008#
End Class
    </file>
</compilation>, validator:=AddressOf EmitBeforeFieldInitValidator)
        End Sub

        Private Sub EmitBeforeFieldInitValidator(assembly As PEAssembly)
            Dim reader = assembly.GetMetadataReader()
            Dim typeDefs = reader.TypeDefinitions.AsEnumerable().ToArray()

            Assert.Equal(8, typeDefs.Length)

            Dim row = reader.GetTypeDefinition(typeDefs(0))
            Assert.Equal("<Module>", reader.GetString(row.Name))
            Assert.Equal(0, row.Attributes)

            row = reader.GetTypeDefinition(typeDefs(1))
            Assert.Equal("A_class", reader.GetString(row.Name))
            Assert.Equal(1, row.Attributes)

            row = reader.GetTypeDefinition(typeDefs(2))
            Assert.Equal("B_class", reader.GetString(row.Name))
            Assert.Equal(1, row.Attributes)

            row = reader.GetTypeDefinition(typeDefs(3))
            Assert.Equal("C_class", reader.GetString(row.Name))
            Assert.Equal(1, row.Attributes)

            row = reader.GetTypeDefinition(typeDefs(4))
            Assert.Equal("D_class", reader.GetString(row.Name))
            Assert.Equal(1, row.Attributes)

            row = reader.GetTypeDefinition(typeDefs(5))
            Assert.Equal("E_class", reader.GetString(row.Name))
            Assert.Equal(1, row.Attributes)

            row = reader.GetTypeDefinition(typeDefs(6))
            Assert.Equal("F_class", reader.GetString(row.Name))
            Assert.Equal(TypeAttributes.BeforeFieldInit Or TypeAttributes.Public, row.Attributes)

            row = reader.GetTypeDefinition(typeDefs(7))
            Assert.Equal("G_class", reader.GetString(row.Name))
            Assert.Equal(TypeAttributes.BeforeFieldInit Or TypeAttributes.Public, row.Attributes)
        End Sub

        <Fact()>
        Public Sub GenericMethods2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">


Module Module1
    Sub Main()
        Dim x As TC1 = New TC1()
        System.Console.WriteLine(x.GetType())
        Dim y As TC2(Of Byte) = New TC2(Of Byte)()
        System.Console.WriteLine(y.GetType())
        Dim z As TC3(Of Byte).TC4 = New TC3(Of Byte).TC4()
        System.Console.WriteLine(z.GetType())
    End Sub
End Module

Class TC1
    Sub TM1(Of T1)()
        TM1(Of T1)()
    End Sub

    Sub TM2(Of T2)()
        TM2(Of Integer)()
    End Sub
End Class

Class TC2(Of T3)

    Sub TM3(Of T4)()
        TM3(Of T4)()
        TM3(Of T4)()
    End Sub

    Sub TM4(Of T5)()
        TM4(Of Integer)()
        TM4(Of Integer)()
    End Sub

    Shared Sub TM5(Of T6)(x As T6)
        TC2(Of Integer).TM5(Of T6)(x)
    End Sub

    Shared Sub TM6(Of T7)(x As T7)
        TC2(Of Integer).TM6(Of Integer)(1)
    End Sub

    Sub TM9()
        TM9()
        TM9()
    End Sub

End Class

Class TC3(Of T8)

    Public Class TC4

        Sub TM7(Of T9)()
            TM7(Of T9)()
            TM7(Of Integer)()
        End Sub

        Shared Sub TM8(Of T10)(x As T10)
            TC3(Of Integer).TC4.TM8(Of T10)(x)
            TC3(Of Integer).TC4.TM8(Of Integer)(1)
        End Sub
    End Class
End Class
    </file>
</compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
TC1
TC2`1[System.Byte]
TC3`1+TC4[System.Byte]
]]>)

        End Sub

        <Fact()>
        Public Sub Constructors()
            Dim sources = <compilation>
                              <file name="c.vb">
Namespace N
    MustInherit Class C
        Shared Sub New()
        End Sub
        Protected Sub New()
        End Sub
    End Class
End Namespace
    </file>
                          </compilation>
            Dim validator =
                Function(isFromSource As Boolean) _
                    Sub([module] As ModuleSymbol)
                        Dim type = [module].GlobalNamespace.GetNamespaceMembers().Single.GetTypeMembers("C").Single()
                        Dim ctor = type.GetMethod(".ctor")

                        Assert.NotNull(ctor)
                        Assert.Equal(WellKnownMemberNames.InstanceConstructorName, ctor.Name)
                        Assert.Equal(MethodKind.Constructor, ctor.MethodKind)
                        Assert.Equal(Accessibility.Protected, ctor.DeclaredAccessibility)
                        Assert.True(ctor.IsDefinition)
                        Assert.False(ctor.IsShared)
                        Assert.False(ctor.IsMustOverride)
                        Assert.False(ctor.IsNotOverridable)
                        Assert.False(ctor.IsOverridable)
                        Assert.False(ctor.IsOverrides)
                        Assert.False(ctor.IsGenericMethod)
                        Assert.False(ctor.IsExtensionMethod)
                        Assert.True(ctor.IsSub)
                        Assert.False(ctor.IsVararg)
                        ' Bug - 2067
                        Assert.Equal("Sub N.C." + WellKnownMemberNames.InstanceConstructorName + "()", ctor.ToTestDisplayString())
                        Assert.Equal(0, ctor.TypeParameters.Length)
                        Assert.Equal("Void", ctor.ReturnType.Name)

                        If isFromSource Then
                            Dim cctor = type.GetMethod(".cctor")
                            Assert.NotNull(cctor)
                            Assert.Equal(WellKnownMemberNames.StaticConstructorName, cctor.Name)
                            Assert.Equal(MethodKind.SharedConstructor, cctor.MethodKind)
                            Assert.Equal(Accessibility.Private, cctor.DeclaredAccessibility)
                            Assert.True(cctor.IsDefinition)
                            Assert.True(cctor.IsShared)
                            Assert.False(cctor.IsMustOverride)
                            Assert.False(cctor.IsNotOverridable)
                            Assert.False(cctor.IsOverridable)
                            Assert.False(cctor.IsOverrides)
                            Assert.False(cctor.IsGenericMethod)
                            Assert.False(cctor.IsExtensionMethod)
                            Assert.True(cctor.IsSub)
                            Assert.False(cctor.IsVararg)

                            ' Bug - 2067
                            Assert.Equal("Sub N.C." + WellKnownMemberNames.StaticConstructorName + "()", cctor.ToTestDisplayString())
                            Assert.Equal(0, cctor.TypeArguments.Length)
                            Assert.Equal(0, cctor.Parameters.Length)
                            Assert.Equal("Void", cctor.ReturnType.Name)
                        Else
                            Assert.Equal(0, type.GetMembers(".cctor").Length)
                        End If
                    End Sub
            CompileAndVerify(sources, sourceSymbolValidator:=validator(True), symbolValidator:=validator(False))
        End Sub

        <Fact()>
        Public Sub DoNotImportPrivateMembers()
            Dim sources = <compilation>
                              <file name="c.vb">
Namespace [Namespace]
    Public Class [Public]
    End Class
    Friend Class [Friend]
    End Class
End Namespace
Class Types
    Public Class [Public]
    End Class
    Friend Class [Friend]
    End Class
    Protected Class [Protected]
    End Class
    Protected Friend Class ProtectedFriend
    End Class
    Private Class [Private]
    End Class
End Class
Class FIelds
    Public [Public]
    Friend [Friend]
    Protected [Protected]
    Protected Friend ProtectedFriend
    Private [Private]
End Class
Class Methods
    Public Sub [Public]()
    End Sub
    Friend Sub [Friend]()
    End Sub
    Protected Sub [Protected]()
    End Sub
    Protected Friend Sub ProtectedFriend()
    End Sub
    Private Sub [Private]()
    End Sub
End Class
Class Properties
    Public Property [Public]
    Friend Property [Friend]
    Protected Property [Protected]
    Protected Friend Property ProtectedFriend
    Private Property [Private]
End Class
    </file>
                          </compilation>

            Dim validator = Function(isFromSource As Boolean) _
                                Sub([module] As ModuleSymbol)
                                    Dim nmspace = [module].GlobalNamespace.GetNamespaceMembers().Single()
                                    Assert.NotNull(nmspace.GetTypeMembers("Public").SingleOrDefault())
                                    Assert.NotNull(nmspace.GetTypeMembers("Friend").SingleOrDefault())

                                    CheckPrivateMembers([module].GlobalNamespace.GetTypeMembers("Types").Single(), isFromSource, True)
                                    CheckPrivateMembers([module].GlobalNamespace.GetTypeMembers("Fields").Single(), isFromSource, False)
                                    CheckPrivateMembers([module].GlobalNamespace.GetTypeMembers("Methods").Single(), isFromSource, False)
                                    CheckPrivateMembers([module].GlobalNamespace.GetTypeMembers("Properties").Single(), isFromSource, False)
                                End Sub

            CompileAndVerify(sources, sourceSymbolValidator:=validator(True), symbolValidator:=validator(False), options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal))
        End Sub

        Private Sub CheckPrivateMembers(type As NamedTypeSymbol, isFromSource As Boolean, importPrivates As Boolean)
            Assert.NotNull(type.GetMembers("Public").SingleOrDefault())
            Assert.NotNull(type.GetMembers("Friend").SingleOrDefault())
            Assert.NotNull(type.GetMembers("Protected").SingleOrDefault())
            Assert.NotNull(type.GetMembers("ProtectedFriend").SingleOrDefault())
            Dim member = type.GetMembers("Private").SingleOrDefault()
            If importPrivates OrElse isFromSource Then
                Assert.NotNull(member)
            Else
                Assert.Null(member)
            End If
        End Sub

        <Fact()>
        Public Sub DoNotImportInternalMembers()
            Dim sources = <compilation>
                              <file name="c.vb">
Class FIelds
    Public [Public]
    Friend [Friend]
End Class
Class Methods
    Public Sub [Public]()
    End Sub
    Friend Sub [Friend]()
    End Sub
End Class
Class Properties
    Public Property [Public]
    Friend Property [Friend]
End Class
    </file>
                          </compilation>
            Dim validator = Function(isFromSource As Boolean) _
                Sub([module] As ModuleSymbol)
                    CheckInternalMembers([module].GlobalNamespace.GetTypeMembers("Fields").Single(), isFromSource)
                    CheckInternalMembers([module].GlobalNamespace.GetTypeMembers("Methods").Single(), isFromSource)
                    CheckInternalMembers([module].GlobalNamespace.GetTypeMembers("Properties").Single(), isFromSource)
                End Sub
            CompileAndVerify(sources, sourceSymbolValidator:=validator(True), symbolValidator:=validator(False))
        End Sub

        Private Sub CheckInternalMembers(type As NamedTypeSymbol, isFromSource As Boolean)
            Assert.NotNull(type.GetMembers("Public").SingleOrDefault())
            Dim member = type.GetMembers("Friend").SingleOrDefault()
            If isFromSource Then
                Assert.NotNull(member)
            Else
                Assert.Null(member)
            End If

        End Sub

        <Fact,
         WorkItem(6190, "https://github.com/dotnet/roslyn/issues/6190"),
         WorkItem(90, "https://github.com/dotnet/roslyn/issues/90")>
        Public Sub EmitWithNoResourcesAllPlatforms()
            Dim comp = CreateCompilationWithMscorlib40(
                <compilation>
                    <file>
Class Test
    Shared Sub Main()
    End Sub
End Class
                    </file>
                </compilation>)

            VerifyEmitWithNoResources(comp.WithAssemblyName("EmitWithNoResourcesAllPlatforms_AnyCpu"), Platform.AnyCpu)
            VerifyEmitWithNoResources(comp.WithAssemblyName("EmitWithNoResourcesAllPlatforms_AnyCpu32BitPreferred"), Platform.AnyCpu32BitPreferred)
            VerifyEmitWithNoResources(comp.WithAssemblyName("EmitWithNoResourcesAllPlatforms_Arm"), Platform.Arm)     ' broken before fix
            VerifyEmitWithNoResources(comp.WithAssemblyName("EmitWithNoResourcesAllPlatforms_Itanium"), Platform.Itanium) ' broken before fix
            VerifyEmitWithNoResources(comp.WithAssemblyName("EmitWithNoResourcesAllPlatforms_X64"), Platform.X64)     ' broken before fix
            VerifyEmitWithNoResources(comp.WithAssemblyName("EmitWithNoResourcesAllPlatforms_X86"), Platform.X86)
        End Sub

        Private Sub VerifyEmitWithNoResources(comp As VisualBasicCompilation, platform As Platform)
            Dim options = TestOptions.ReleaseExe.WithPlatform(platform)
            CompileAndVerify(comp.WithOptions(options))
        End Sub
    End Class
End Namespace
