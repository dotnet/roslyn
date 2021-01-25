' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.Analyzers.NamespaceMatchFolder.VisualBasicNamespaceMatchFolderDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.NamespaceMatchFolder
    Public Class VisualBasicNamespaceMatchFolderTests
        Private Const DefaultEditorConfig As String = "
is_global=true
build_property.ProjectDir = Test/Directory
build_property.RootNamespace = Test.Root.Namespace
"
        Private Shared Function CreateFolderPath(ParamArray folders() As String) As String
            Return Path.Combine("Test", "Directory", Path.Combine(folders))
        End Function

        Private Shared Function RunTestAsync(source As String, directories As String, Optional editorConfig As String = Nothing) As Task
            Dim sources = New List(Of (String, String))
            Dim fileName = Path.Combine(directories, "Class1.vb")

            sources.Add((fileName, source))

            Return RunTestAsync(sources, editorConfig)
        End Function

        Private Shared Function RunTestAsync(sources As IEnumerable(Of (fileName As String, content As String)), Optional editorConfig As String = Nothing) As Task
            Dim testState = New VerifyVB.Test With
            {
                .EditorConfig = If(editorConfig, DefaultEditorConfig)
            }

            For Each pair As (String, String) In sources
                testState.TestState.Sources.Add(pair)
            Next

            Return testState.RunAsync()
        End Function

        <Fact>
        Public Async Function TestSimpleDiagnostic() As Task
            Dim source = "Namespace [|A|]
    Public Class Class1
    End Class
End Namespace"

            Dim path = CreateFolderPath("A", "B", "C")

            Await RunTestAsync(source, path)
        End Function

        <Fact>
        Public Async Function TestDiagnosticMissing() As Task
            Dim source = "Namespace Test.Root.Namespace.A
    Public Class Class1
    End Class
End Namespace"

            Dim path = CreateFolderPath("A")

            Await RunTestAsync(source, path)
        End Function

        <Fact>
        Public Async Function TestGlobaliagnostic() As Task
            Dim source = "Namespace [|Global.A|]
    Public Class Class1
    End Class
End Namespace"

            Dim path = CreateFolderPath("A", "B", "C")

            Await RunTestAsync(source, path)
        End Function

        <Fact>
        Public Async Function TestSpaces() As Task
            Dim source = "Namespace [|A   .    B   . Not.Correct|]
    Public Class Class1
    End Class
End Namespace"

            Dim path = CreateFolderPath("A", "B", "C")

            Await RunTestAsync(source, path)
        End Function

        <Fact>
        Public Async Function TestInvalidFolderName() As Task
            Dim source = "Namespace [|A|]
    Public Class Class1
    End Class
End Namespace"

            Dim path = CreateFolderPath("3A", ".B", "..C")

            Await RunTestAsync(source, path)
        End Function

        <Fact>
        Public Async Function TestDotFolderName() As Task
            Dim source = "Namespace [|A|]
    Public Class Class1
    End Class
End Namespace"

            Dim path = CreateFolderPath(".A.", "..", "..B", "..C")

            Await RunTestAsync(source, path)
        End Function

        <Fact>
        Public Async Function TestCaseInsensitive() As Task
            Dim source = "Namespace Test.Root.Namespace.a.b.c
    Public Class Class1
    End Class
End Namespace"

            Dim path = CreateFolderPath("A", "B", "C")

            Await RunTestAsync(source, path)
        End Function
    End Class
End Namespace

