' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.Reflection.Metadata
Imports System.Reflection.Metadata.Ecma335
Imports System.Text
Imports Microsoft.CodeAnalysis.Debugging
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class TypeDefinitionDocumentTests
        Inherits BasicTestBase

        <Fact>
        Public Sub ClassWithMethod()
            Dim source As String = "
Class M
    Public Shared Sub A()
        System.Console.WriteLine()
    End Sub
End Class
"
            TestTypeDefinitionDocuments({source})
        End Sub

        <Fact>
        Public Sub NestedClassWithMethod()
            Dim source As String = "
Class C
    Class N
        Public Shared Sub A()
            System.Console.WriteLine()
        End Sub
    End Class
End Class
"
            TestTypeDefinitionDocuments({source})
        End Sub

        <Fact>
        Public Sub MultiNestedClassWithMethod()
            Dim source As String = "
Class C
    Class N
        Class N2
            Public Shared Sub A()
                System.Console.WriteLine()
            End Sub
        End Class
    End Class
End Class
"
            TestTypeDefinitionDocuments({source})
        End Sub

        <Fact>
        Public Sub PartialMultiNestedClassWithMethod()
            Dim source1 As String = "
Partial Class C
    Partial Class N
        Public Shared Sub A()
            System.Console.WriteLine()
        End Sub
    End Class
End Class
"

            Dim source2 As String = "
Partial Class C
    Partial Class N
    End Class
End Class
"
            TestTypeDefinitionDocuments({source1, source2},
                              ("C", "2.vb"))
        End Sub

        <Fact>
        Public Sub EmptyClass()
            Dim source As String = "
Class O
End Class
"
            TestTypeDefinitionDocuments({source},
                              ("O", "1.vb"))
        End Sub

        <Fact>
        Public Sub EmptyNestedClass()
            Dim source As String = "
Class O
    Class N
    End Class
End Class
"
            TestTypeDefinitionDocuments({source},
                              ("O", "1.vb"))
        End Sub

        <Fact>
        Public Sub EmptyMultiNestedClass()
            Dim source As String = "
Class O
    Class N
        Class N2
        End Class
    End Class
End Class
"
            TestTypeDefinitionDocuments({source},
                              ("O", "1.vb"))
        End Sub

        <Fact>
        Public Sub MultipleClassesAndFiles()
            Dim source1 As String = "
Class M
    Public Shared Sub A()
        System.Console.WriteLine()
    End Sub
End Class

Class N
End Class

Class O
End Class
"
            Dim source2 As String = "
Class C
End Class

Class D
End Class
"
            TestTypeDefinitionDocuments({source1, source2},
                              ("N", "1.vb"),
                              ("O", "1.vb"),
                              ("C", "2.vb"),
                              ("D", "2.vb"))
        End Sub

        <Fact>
        Public Sub PartialClasses()
            Dim source1 As String = "
Partial Class C
End Class
"
            Dim source2 As String = "
Partial Class C
End Class
"
            TestTypeDefinitionDocuments({source1, source2},
                              ("C", "1.vb, 2.vb"))
        End Sub

        <Fact>
        Public Sub PartialClasses2()
            Dim source1 As String = "
Partial Class C
End Class
"
            Dim source2 As String = "
Partial Class C
    Private x As Integer

    Sub M()
    End Sub
End Class
"
            TestTypeDefinitionDocuments({source1, source2},
                              ("C", "1.vb"))
        End Sub

        <Fact>
        Public Sub PartialClasses3()
            Dim source1 As String = "
Partial Class C
End Class
"
            Dim source2 As String = "
Partial Class C
    Private x As Integer = 1

    Sub M()
    End Sub
End Class
"
            TestTypeDefinitionDocuments({source1, source2},
                              ("C", "1.vb"))
        End Sub

        <Fact>
        Public Sub [Property]()
            Dim source As String = "
Class C
    Public Property X As Integer
End Class

Class D
    Public Property Y As Integer = 4
End Class
"
            TestTypeDefinitionDocuments({source},
                              ("C", "1.vb"))
        End Sub

        <Fact>
        Public Sub Fields()
            Dim source As String = "
Class C
    Private x As Integer
    Private Const y As Integer = 3
End Class
"
            TestTypeDefinitionDocuments({source},
                              ("C", "1.vb"))
        End Sub

        <Fact>
        Public Sub Fields_WithInitializer()
            Dim source As String = "
Class C
    Private x As Integer = 1
    Private Const y As Integer = 3
End Class
"
            TestTypeDefinitionDocuments({source})
        End Sub

        <Fact>
        Public Sub AbstractMethod()
            Dim source As String = "
MustInherit Class C
    MustOverride Sub M()
End Class
"
            TestTypeDefinitionDocuments({source},
                              ("C", "1.vb"))
        End Sub

        <Fact>
        Public Sub Interfaces()
            Dim source1 As String = "
Interface I1
End Interface

Partial Interface I2
    Sub M()
End Interface
"
            Dim source2 As String = "
Partial Interface I2
End Interface
"
            TestTypeDefinitionDocuments({source1, source2},
                              ("I1", "1.vb"),
                              ("I2", "1.vb, 2.vb"))
        End Sub

        <Fact>
        Public Sub [Enum]()
            Dim source As String = "
Enum E
    A
    B
End Enum
"
            TestTypeDefinitionDocuments({source},
                              ("E", "1.vb"))
        End Sub

        <Fact>
        Public Sub [Delegate]()
            Dim source As String = "
Delegate Sub D(a As Integer)

Class C
    Public Sub M()
        Dim y = (
        Function(ByRef a As Integer)
            Return a
        End Function
        )
    End Sub
End Class
"
            TestTypeDefinitionDocuments({source},
                              ("D", "1.vb"))
        End Sub

        <Fact>
        Public Sub AnonymousType()
            Dim source As String = "
Public Class C
    Public Sub M()
       Dim x = New With { .Goo = 1, .Bar = ""Hi"" }
    End Sub
End Class
"
            TestTypeDefinitionDocuments({source})
        End Sub

        <Fact>
        Public Sub ExternalSourceDirectives()
            Dim source As String = "
Class C
#ExternalSource (""C.vb"", 1)
    Public Sub M()
    End Sub
#End ExternalSource
End Class

Class D
#ExternalSource (""D.vb"", 1)
    Private _x As Integer = 1
    Private Property Y As Integer = 1
#End ExternalSource
End Class

#ExternalSource (""E.vb"", 1)
Class E
    Public Sub M()
    End Sub
End Class
#End ExternalSource

#ExternalSource (""F.vb"", 1)
Class F
End Class
#End ExternalSource
"
            TestTypeDefinitionDocuments({source},
                              ("C", "1.vb"),
                              ("D", "1.vb"),
                              ("E", "1.vb"),
                              ("F", "1.vb"))
        End Sub

        Public Shared Sub TestTypeDefinitionDocuments(sources As String(), ParamArray expected As (String, String)())
            Dim trees = sources.Select(Function(s, i) SyntaxFactory.ParseSyntaxTree(s, path:=$"{i + 1}.vb", encoding:=Encoding.UTF8)).ToArray()
            Dim compilation = CreateCompilation(trees, options:=TestOptions.DebugDll)

            Dim pdbStream = New MemoryStream()
            Dim pe = compilation.EmitToArray(EmitOptions.[Default].WithDebugInformationFormat(DebugInformationFormat.PortablePdb), pdbStream:=pdbStream)
            pdbStream.Position = 0

            Dim metadata = ModuleMetadata.CreateFromImage(pe)
            Dim metadataReader = metadata.GetMetadataReader()

            Using provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream)
                Dim pdbReader = provider.GetMetadataReader()

                Dim actual = From handle In pdbReader.CustomDebugInformation
                             Let entry = pdbReader.GetCustomDebugInformation(handle)
                             Where pdbReader.GetGuid(entry.Kind).Equals(PortableCustomDebugInfoKinds.TypeDefinitionDocuments)
                             Select (GetTypeName(metadataReader, entry.Parent), GetDocumentNames(pdbReader, entry.Value))

                AssertEx.Equal(expected, actual, itemSeparator:=", ", itemInspector:=Function(i) $"(""{i.Item1}"", ""{i.Item2}"")")
            End Using
        End Sub

        Private Shared Function GetTypeName(metadataReader As MetadataReader, handle As EntityHandle) As String
            Dim typeHandle = CType(handle, TypeDefinitionHandle)
            Dim type = metadataReader.GetTypeDefinition(typeHandle)
            Return metadataReader.GetString(type.Name)
        End Function

        Private Shared Function GetDocumentNames(pdbReader As MetadataReader, value As BlobHandle) As String
            Dim result = New List(Of String)()
            Dim reader = pdbReader.GetBlobReader(value)

            While reader.RemainingBytes > 0
                Dim documentRow = reader.ReadCompressedInteger()

                If documentRow > 0 Then
                    Dim doc = pdbReader.GetDocument(MetadataTokens.DocumentHandle(documentRow))
                    result.Add(pdbReader.GetString(doc.Name))
                End If
            End While

            ' Order can be different in net472 vs net5 :(
            result.Sort()
            Return String.Join(", ", result)
        End Function
    End Class
End Namespace
