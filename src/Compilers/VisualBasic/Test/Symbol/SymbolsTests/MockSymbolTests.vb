' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class TestSymbols
        ' Create a trivial compilation with no source or references.
        Private Function TrivialCompilation() As VisualBasicCompilation
            Return VisualBasicCompilation.Create("Test")
        End Function

        <Fact>
        Public Sub TestArrayType()
            Dim compilation As VisualBasicCompilation = TrivialCompilation()
            Dim elementType As NamedTypeSymbol = New MockNamedTypeSymbol("TestClass", Enumerable.Empty(Of Symbol))   ' this can be any type.

            Dim ats1 As ArrayTypeSymbol = ArrayTypeSymbol.CreateVBArray(elementType, Nothing, 1, compilation)
            Assert.Equal(1, ats1.Rank)
            Assert.Same(elementType, ats1.ElementType)
            Assert.Equal(SymbolKind.ArrayType, ats1.Kind)
            Assert.True(ats1.IsReferenceType)
            Assert.False(ats1.IsValueType)
            Assert.Equal("TestClass()", ats1.ToString())

            Dim ats2 As ArrayTypeSymbol = ArrayTypeSymbol.CreateVBArray(elementType, Nothing, 2, compilation)
            Assert.Equal(2, ats2.Rank)
            Assert.Same(elementType, ats2.ElementType)
            Assert.Equal(SymbolKind.ArrayType, ats2.Kind)
            Assert.True(ats2.IsReferenceType)
            Assert.False(ats2.IsValueType)
            Assert.Equal("TestClass(*,*)", ats2.ToString())

            Dim ats3 As ArrayTypeSymbol = ArrayTypeSymbol.CreateVBArray(elementType, Nothing, 3, compilation)
            Assert.Equal(3, ats3.Rank)
            Assert.Equal("TestClass(*,*,*)", ats3.ToString())
        End Sub

        <Fact>
        Public Sub TestMissingMetadataSymbol()
            Dim missingAssemblyName = New AssemblyIdentity("goo")
            Dim assem As AssemblySymbol = New MockAssemblySymbol("banana")
            Dim [module] = New MissingModuleSymbol(assem, -1)
            Dim container As NamedTypeSymbol = New MockNamedTypeSymbol("TestClass", Enumerable.Empty(Of Symbol), TypeKind.Class)

            Dim mms1 = New MissingMetadataTypeSymbol.TopLevel(New MissingAssemblySymbol(missingAssemblyName).Modules(0), "Elvis", "Lives", 2, True)
            Assert.Equal(2, mms1.Arity)
            Assert.Equal("Elvis", mms1.NamespaceName)
            Assert.Equal("Lives", mms1.Name)
            Assert.Equal("Elvis.Lives(Of ,)[missing]", mms1.ToTestDisplayString())
            Assert.Equal("goo", mms1.ContainingAssembly.Identity.Name)

            Dim mms2 = New MissingMetadataTypeSymbol.TopLevel([module], "Elvis.Is", "Cool", 0, True)
            Assert.Equal(0, mms2.Arity)
            Assert.Equal("Elvis.Is", mms2.NamespaceName)
            Assert.Equal("Cool", mms2.Name)
            Assert.Equal("Elvis.Is.Cool[missing]", mms2.ToTestDisplayString())
            Assert.Same(assem, mms2.ContainingAssembly)

            ' TODO: Add test for 3rd constructor.
        End Sub

        <Fact>
        Public Sub TestNamespaceExtent()
            Dim assem1 As AssemblySymbol = New MockAssemblySymbol("goo")

            Dim ne1 As NamespaceExtent = New NamespaceExtent(assem1)
            Assert.Equal(ne1.Kind, NamespaceKind.Assembly)
            Assert.Same(ne1.Assembly, assem1)

            Dim compilation As VisualBasicCompilation = TrivialCompilation()
            Dim ne2 As NamespaceExtent = New NamespaceExtent(compilation)
            Assert.IsType(Of Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation)(ne2.Compilation)
            Assert.Throws(Of InvalidOperationException)(Sub()
                                                            Dim tmp = ne1.Compilation()
                                                        End Sub)
        End Sub

        Private Function CreateMockSymbol(extent As NamespaceExtent, xel As XElement) As Symbol
            Dim result As Symbol
            Dim childSymbols = From childElement In xel.Elements()
                               Select CreateMockSymbol(extent, childElement)

            Dim name As String = xel.Attribute("name").Value
            Select Case xel.Name.LocalName
                Case "ns"
                    result = New MockNamespaceSymbol(name, extent, childSymbols)

                Case "class"
                    result = New MockNamedTypeSymbol(name, childSymbols, TypeKind.Class)

                Case Else
                    Throw New ApplicationException("unexpected xml element")
            End Select

            For Each child As IMockSymbol In childSymbols
                child.SetContainer(result)
            Next

            Return result
        End Function

        Private Sub DumpSymbol(sym As Symbol, builder As StringBuilder, level As Integer)
            If TypeOf sym Is NamespaceSymbol Then
                builder.AppendFormat("Namespace {0} [{1}]", sym.Name, DirectCast(sym, NamespaceSymbol).Extent)
            ElseIf TypeOf sym Is NamedTypeSymbol Then
                builder.AppendFormat("{0} {1}", DirectCast(sym, NamedTypeSymbol).TypeKind.ToString(), sym.Name)
            Else
                Throw New ApplicationException("Unexpected symbol kind")
            End If

            If TypeOf sym Is NamespaceOrTypeSymbol AndAlso DirectCast(sym, NamespaceOrTypeSymbol).GetMembers().Any() Then
                builder.AppendLine(" { ")
                For Each child As Symbol In (From c In DirectCast(sym, NamespaceOrTypeSymbol).GetMembers().AsEnumerable() Order By c.Name)
                    For i = 0 To level
                        builder.Append("    ")
                    Next
                    DumpSymbol(child, builder, level + 1)
                    builder.AppendLine()
                Next
                For i = 0 To level - 1
                    builder.Append("    ")
                Next
                builder.Append("}")
            End If
        End Sub

        Private Function DumpSymbol(sym As Symbol) As String
            Dim builder As New StringBuilder()
            DumpSymbol(sym, builder, 0)
            Return builder.ToString()
        End Function

        <Fact>
        Public Sub TestMergedNamespaces()
            Dim root1 As NamespaceSymbol = DirectCast(CreateMockSymbol(New NamespaceExtent(New MockAssemblySymbol("Assem1")),
<ns name=''>
    <ns name='A'>
        <ns name='D'/>
        <ns name='E'/>
        <ns name='F'>
            <ns name='G'/>
        </ns>
    </ns>
    <ns name='B'/>
    <ns name='C'/>
    <ns name='U'/>
    <class name='X'/>
</ns>), NamespaceSymbol)

            Dim root2 As NamespaceSymbol = DirectCast(CreateMockSymbol(New NamespaceExtent(New MockAssemblySymbol("Assem2")),
<ns name=''>
    <ns name='B'>
        <ns name='K'/>
    </ns>
    <ns name='C'/>
    <class name='X'/>
    <class name='Y'/>
</ns>), NamespaceSymbol)

            Dim root3 As NamespaceSymbol = DirectCast(CreateMockSymbol(New NamespaceExtent(New MockAssemblySymbol("Assem3")),
<ns name=''>
    <ns name='a'>
        <ns name='D'/>
        <ns name='e'>
            <ns name='H'/>
        </ns>
    </ns>
    <ns name='B'>
        <ns name='K'>
            <ns name='L'/>
            <class name='L'/>
        </ns>
    </ns>
    <class name='Z'/>
</ns>), NamespaceSymbol)

            Dim merged As NamespaceSymbol = MergedNamespaceSymbol.CreateForTestPurposes(New MockAssemblySymbol("Merged"),
                                                                         {root1, root2, root3})
            Dim expected As String =
<expected>Namespace  [Assembly: Merged] { 
    Namespace A [Assembly: Merged] { 
        Namespace D [Assembly: Merged]
        Namespace E [Assembly: Merged] { 
            Namespace H [Assembly: Assem3]
        }
        Namespace F [Assembly: Assem1] { 
            Namespace G [Assembly: Assem1]
        }
    }
    Namespace B [Assembly: Merged] { 
        Namespace K [Assembly: Merged] { 
            Class L
            Namespace L [Assembly: Assem3]
        }
    }
    Namespace C [Assembly: Merged]
    Namespace U [Assembly: Assem1]
    Class X
    Class X
    Class Y
    Class Z
}</expected>.Value.Replace(vbLf, Environment.NewLine).
                   Replace("Assembly: Merged", "Assembly: Merged, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").
                   Replace("Assembly: Assem1", "Assembly: Assem1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").
                   Replace("Assembly: Assem3", "Assembly: Assem3, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")

            Assert.Equal(expected, DumpSymbol(merged))

            Dim merged2 As NamespaceSymbol = MergedNamespaceSymbol.CreateForTestPurposes(New MockAssemblySymbol("Merged2"),
                                                                          {root1})
            Assert.Same(merged2, root1)
        End Sub
    End Class

End Namespace

