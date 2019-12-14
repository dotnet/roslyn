' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Editting
    <[UseExportProvider]>
    Public Class AddImportsTests

        Private Const ExtensionAttributeSource =
"Namespace System.Runtime.CompilerServices
    <AttributeUsage(AttributeTargets.Method Or AttributeTargets.[Class] Or AttributeTargets.Assembly)>
    Public NotInheritable Class ExtensionAttribute
        Inherits Attribute
    End Class
End NameSpace"

        Private Async Function GetDocument(code As String, withAnnotations As Boolean, Optional globalImports As String() = Nothing) As Task(Of Document)
            Dim ws As AdhocWorkspace = New AdhocWorkspace()
            Dim project As Project = ws.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Default,
                    "test",
                    "test.dll",
                    LanguageNames.VisualBasic,
                    metadataReferences:={TestReferences.NetFx.v4_0_30319.mscorlib}))

            If globalImports IsNot Nothing Then
                Dim gi = GlobalImport.Parse(globalImports)
                project = project.WithCompilationOptions(DirectCast(project.CompilationOptions, VisualBasicCompilationOptions).WithGlobalImports(gi))
            End If

            Dim extensionAttributeDoc = project.AddDocument("ExtensionAttribute.vb", ExtensionAttributeSource)
            Dim doc = extensionAttributeDoc.Project.AddDocument("test.vb", code)

            If withAnnotations Then
                Dim root = Await doc.GetSyntaxRootAsync()
                Dim model = Await doc.GetSemanticModelAsync()
                root = root.ReplaceNodes(root.DescendantNodesAndSelf().OfType(Of TypeSyntax)(),
                                         Function(o, c)
                                             Dim symbol = model.GetSymbolInfo(o).Symbol
                                             If symbol IsNot Nothing Then
                                                 Return c.WithAdditionalAnnotations(SymbolAnnotation.Create(symbol), Simplifier.Annotation)
                                             End If
                                             Return c
                                         End Function)
                doc = doc.WithSyntaxRoot(root)
            End If

            Return doc
        End Function

        Private Async Function TestAsync(initialText As String, importsAddedText As String, simplifiedText As String, safe As Boolean, useSymbolAnnotations As Boolean, Optional optionsTransform As Func(Of OptionSet, OptionSet) = Nothing, Optional globalImports As String() = Nothing) As Task

            Dim doc = Await GetDocument(initialText, useSymbolAnnotations, globalImports)
            Dim options = doc.Project.Solution.Workspace.Options
            If optionsTransform IsNot Nothing Then
                options = optionsTransform(options)
            End If

            Dim imported = If(
                useSymbolAnnotations,
                Await ImportAdder.AddImportsFromSymbolAnnotationAsync(doc, safe, options),
                Await ImportAdder.AddImportsFromSyntaxesAsync(doc, safe, options))

            If importsAddedText IsNot Nothing Then
                Dim formatted = Await Formatter.FormatAsync(imported, SyntaxAnnotation.ElasticAnnotation, options)
                Dim actualText = (Await formatted.GetTextAsync()).ToString()
                Assert.Equal(importsAddedText, actualText)
            End If

            If simplifiedText IsNot Nothing Then
                Dim reduced = Await Simplifier.ReduceAsync(imported, options)
                Dim formatted = Await Formatter.FormatAsync(reduced, SyntaxAnnotation.ElasticAnnotation, options)
                Dim actualText = (Await formatted.GetTextAsync()).ToString()
                Assert.Equal(simplifiedText, actualText)
            End If
        End Function

        Public Shared TestAllData As Object()() = {
            New Object() {False, False},
            New Object() {False, True},
            New Object() {True, False},
            New Object() {True, True}
        }

        Public Shared TestSyntaxesData As Object()() = {
            New Object() {False, False},
            New Object() {True, False}
        }

        Public Shared TestSymbolsData As Object()() = {
            New Object() {False, True},
            New Object() {True, True}
        }

        <Theory, MemberData(NameOf(TestAllData))>
        Public Async Function TestAddImport(safe As Boolean, useSymbolAnnotations As Boolean) As Task
            Await TestAsync(
"Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
"Imports System.Collections.Generic

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
"Imports System.Collections.Generic

Class C
    Public F As List(Of Integer)
End Class", safe, useSymbolAnnotations)
        End Function

        <Theory, MemberData(NameOf(TestAllData))>
        Public Async Function TestAddSystemImportFirst(safe As Boolean, useSymbolAnnotations As Boolean) As Task
            Await TestAsync(
"Imports N

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
"Imports System.Collections.Generic
Imports N

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
"Imports System.Collections.Generic
Imports N

Class C
    Public F As List(Of Integer)
End Class", safe, useSymbolAnnotations)
        End Function

        <Theory, MemberData(NameOf(TestAllData))>
        Public Async Function TestDontAddSystemImportFirst(safe As Boolean, useSymbolAnnotations As Boolean) As Task
            Await TestAsync(
"Imports N

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
"Imports N
Imports System.Collections.Generic

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
"Imports N
Imports System.Collections.Generic

Class C
    Public F As List(Of Integer)
End Class",
            safe,
            useSymbolAnnotations,
            Function(options) options.WithChangedOption(GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.VisualBasic, False))
        End Function

        <Theory, MemberData(NameOf(TestAllData))>
        Public Async Function TestAddImportsInOrder(safe As Boolean, useSymbolAnnotations As Boolean) As Task
            Await TestAsync(
"Imports System.Collections
Imports System.Diagnostics

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
"Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
"Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics

Class C
    Public F As List(Of Integer)
End Class", safe, useSymbolAnnotations)
        End Function

        <Theory, MemberData(NameOf(TestAllData))>
        Public Async Function TestAddMultipleImportsInOrder(safe As Boolean, useSymbolAnnotations As Boolean) As Task
            Await TestAsync(
"Imports System.Collections
Imports System.Diagnostics

Class C
    Public F As System.Collections.Generic.List(Of Integer)
    Public Handler As System.EventHandler
End Class",
"Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics

Class C
    Public F As System.Collections.Generic.List(Of Integer)
    Public Handler As System.EventHandler
End Class",
"Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics

Class C
    Public F As List(Of Integer)
    Public Handler As EventHandler
End Class", safe, useSymbolAnnotations)
        End Function

        <Theory, MemberData(NameOf(TestAllData))>
        Public Async Function TestImportNotAddedAgainIfAlreadyExists(safe As Boolean, useSymbolAnnotations As Boolean) As Task
            Await TestAsync(
"Imports System.Collections.Generic

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
"Imports System.Collections.Generic

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
"Imports System.Collections.Generic

Class C
    Public F As List(Of Integer)
End Class", safe, useSymbolAnnotations)
        End Function

        <Theory, MemberData(NameOf(TestSyntaxesData))>
        Public Async Function TestBuiltInTypeFromSyntaxes(safe As Boolean, useSymbolAnnotations As Boolean) As Task
            Await TestAsync(
"Class C
    Public F As System.Int32
End Class",
"Imports System

Class C
    Public F As System.Int32
End Class",
"Class C
    Public F As Integer
End Class", safe, useSymbolAnnotations)
        End Function

        <Theory, MemberData(NameOf(TestSymbolsData))>
        Public Async Function TestBuiltInTypeFromSymbols(safe As Boolean, useSymbolAnnotations As Boolean) As Task
            Await TestAsync(
"Class C
    Public F As System.Int32
End Class",
"Class C
    Public F As System.Int32
End Class",
"Class C
    Public F As Integer
End Class", safe, useSymbolAnnotations)
        End Function

        <Theory, MemberData(NameOf(TestAllData))>
        Public Async Function TestImportNotAddedIfGloballyImported(safe As Boolean, useSymbolAnnotations As Boolean) As Task
            Await TestAsync(
    "Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
    "Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
    "Class C
    Public F As List(Of Integer)
End Class",
    safe,
    useSymbolAnnotations,
    globalImports:={"System.Collections.Generic"})

        End Function

        <Theory, MemberData(NameOf(TestAllData))>
        Public Async Function TestImportNotAddedForNamespaceDeclarations(safe As Boolean, useSymbolAnnotations As Boolean) As Task
            Await TestAsync(
"Namespace N
End Namespace",
"Namespace N
End Namespace",
"Namespace N
End Namespace", safe, useSymbolAnnotations)
        End Function

        <Theory, MemberData(NameOf(TestAllData))>
        Public Async Function TestImportAddedAndRemovedForReferencesInsideNamespaceDeclarations(safe As Boolean, useSymbolAnnotations As Boolean) As Task
            Await TestAsync(
        "Namespace N
    Class C
        Private _c As N.C
    End Class
End Namespace",
        "Namespace N
    Class C
        Private _c As N.C
    End Class
End Namespace",
        "Namespace N
    Class C
        Private _c As C
    End Class
End Namespace", safe, useSymbolAnnotations)
        End Function

        <Theory, MemberData(NameOf(TestAllData))>
        Public Async Function TestRemoveImportIfItMakesReferencesAmbiguous(safe As Boolean, useSymbolAnnotations As Boolean) As Task
            ' this is not really an artifact of the AddImports feature, it is due
            ' to Simplifier not reducing the namespace reference because it would 
            ' become ambiguous, thus leaving an unused imports statement

            Await TestAsync(
"Namespace N
    Class C
    End Class
End Namespace

Class C
    Private F As N.C
End Class
",
"Imports N

Namespace N
    Class C
    End Class
End Namespace

Class C
    Private F As N.C
End Class
",
"Namespace N
    Class C
    End Class
End Namespace

Class C
    Private F As N.C
End Class
", safe, useSymbolAnnotations)
        End Function

        <Theory, MemberData(NameOf(TestAllData))>
        Public Async Function TestPartialNamespacesNotUsed(safe As Boolean, useSymbolAnnotations As Boolean) As Task
            Await TestAsync(
"Imports System.Collections

Public Class C
    Public F1 As ArrayList
    Public F2 As System.Collections.Generic.List(Of Integer)
End Class",
"Imports System.Collections
Imports System.Collections.Generic

Public Class C
    Public F1 As ArrayList
    Public F2 As System.Collections.Generic.List(Of Integer)
End Class",
"Imports System.Collections
Imports System.Collections.Generic

Public Class C
    Public F1 As ArrayList
    Public F2 As List(Of Integer)
End Class", safe, useSymbolAnnotations)
        End Function

        <Theory, MemberData(NameOf(TestAllData))>
        Public Async Function TestDontAddImportWithExisitingImportDifferentCase(safe As Boolean, useSymbolAnnotations As Boolean) As Task
            Await TestAsync(
"Imports system.collections.generic

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
"Imports system.collections.generic

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
"Imports system.collections.generic

Class C
    Public F As List(Of Integer)
End Class", safe, useSymbolAnnotations)
        End Function

#Region "AddImports Safe Tests"

        <Theory, InlineData(True), InlineData(False)>
        Public Async Function TestSafeWithMatchingSimpleName(useSymbolAnnotations As Boolean) As Task
            Await TestAsync(
"Imports B

Namespace A
    Class C1
    End Class

    Class C2
    End Class
End Namespace

Namespace B
    Class C1
    End Class
End Namespace

Class C
    Private Function M(ByVal c2 As A.C2) As C1
        Return Nothing
    End Function
End Class",
"Imports A
Imports B

Namespace A
    Class C1
    End Class

    Class C2
    End Class
End Namespace

Namespace B
    Class C1
    End Class
End Namespace

Class C
    Private Function M(ByVal c2 As A.C2) As Global.B.C1
        Return Nothing
    End Function
End Class",
"Imports A
Imports B

Namespace A
    Class C1
    End Class

    Class C2
    End Class
End Namespace

Namespace B
    Class C1
    End Class
End Namespace

Class C
    Private Function M(ByVal c2 As C2) As B.C1
        Return Nothing
    End Function
End Class", safe:=True, useSymbolAnnotations)
        End Function

        <Theory, InlineData(True), InlineData(False)>
        Public Async Function TestSafeWithMatchingSimpleNameDifferentCase(useSymbolAnnotations As Boolean) As Task
            Await TestAsync(
"Imports B

Namespace A
    Class C1
    End Class

    Class C2
    End Class
End Namespace

Namespace B
    Class C1
    End Class
End Namespace

Class C
    Private Function M(ByVal c2 As A.C2) As c1
        Return Nothing
    End Function
End Class",
"Imports A
Imports B

Namespace A
    Class C1
    End Class

    Class C2
    End Class
End Namespace

Namespace B
    Class C1
    End Class
End Namespace

Class C
    Private Function M(ByVal c2 As A.C2) As Global.B.c1
        Return Nothing
    End Function
End Class",
"Imports A
Imports B

Namespace A
    Class C1
    End Class

    Class C2
    End Class
End Namespace

Namespace B
    Class C1
    End Class
End Namespace

Class C
    Private Function M(ByVal c2 As C2) As B.c1
        Return Nothing
    End Function
End Class", safe:=True, useSymbolAnnotations)
        End Function

        <Theory, InlineData(True), InlineData(False)>
        Public Async Function TestSafeWithMatchingGenericName(useSymbolAnnotations As Boolean) As Task
            Await TestAsync(
"Imports B

Namespace A
    Class C1(Of T)
    End Class

    Class C2
    End Class
End Namespace

Namespace B
    Class C1(Of T)
    End Class
End Namespace

Class C
    Private Function M(ByVal c2 As A.C2) As C1(Of Integer)
        Return Nothing
    End Function
End Class",
"Imports A
Imports B

Namespace A
    Class C1(Of T)
    End Class

    Class C2
    End Class
End Namespace

Namespace B
    Class C1(Of T)
    End Class
End Namespace

Class C
    Private Function M(ByVal c2 As A.C2) As Global.B.C1(Of Integer)
        Return Nothing
    End Function
End Class",
"Imports A
Imports B

Namespace A
    Class C1(Of T)
    End Class

    Class C2
    End Class
End Namespace

Namespace B
    Class C1(Of T)
    End Class
End Namespace

Class C
    Private Function M(ByVal c2 As C2) As B.C1(Of Integer)
        Return Nothing
    End Function
End Class", safe:=True, useSymbolAnnotations)
        End Function

        <Theory, InlineData(True), InlineData(False)>
        Public Async Function TestSafeWithMatchingQualifiedName(useSymbolAnnotations As Boolean) As Task
            Await TestAsync(
"Imports B

Namespace A
    Class O
    End Class

    Class C2
    End Class
End Namespace

Namespace B
    Class O
        Public Class C1
        End Class
    End Class
End Namespace

Class C
    Private Function M(ByVal c2 As A.C2) As O.C1
        Return Nothing
    End Function
End Class",
"Imports A
Imports B

Namespace A
    Class O
    End Class

    Class C2
    End Class
End Namespace

Namespace B
    Class O
        Public Class C1
        End Class
    End Class
End Namespace

Class C
    Private Function M(ByVal c2 As A.C2) As Global.B.O.C1
        Return Nothing
    End Function
End Class",
"Imports A
Imports B

Namespace A
    Class O
    End Class

    Class C2
    End Class
End Namespace

Namespace B
    Class O
        Public Class C1
        End Class
    End Class
End Namespace

Class C
    Private Function M(ByVal c2 As C2) As B.O.C1
        Return Nothing
    End Function
End Class", safe:=True, useSymbolAnnotations)
        End Function

        <Theory, InlineData(True), InlineData(False)>
        Public Async Function TestSafeWithMatchingAliasedIdentifierName(useSymbolAnnotations As Boolean) As Task
            Await TestAsync(
"Imports C1 = B.C1

Namespace A
    Class C1
    End Class

    Class C2
    End Class
End Namespace

Namespace B
    Class C1
    End Class
End Namespace

Namespace Inner
    Class C
        Private Function M(ByVal c2 As A.C2) As C1
            Return Nothing
        End Function
    End Class
End Namespace",
"Imports A
Imports B
Imports C1 = B.C1

Namespace A
    Class C1
    End Class

    Class C2
    End Class
End Namespace

Namespace B
    Class C1
    End Class
End Namespace

Namespace Inner
    Class C
        Private Function M(ByVal c2 As A.C2) As Global.B.C1
            Return Nothing
        End Function
    End Class
End Namespace",
"Imports A
Imports C1 = B.C1

Namespace A
    Class C1
    End Class

    Class C2
    End Class
End Namespace

Namespace B
    Class C1
    End Class
End Namespace

Namespace Inner
    Class C
        Private Function M(ByVal c2 As C2) As C1
            Return Nothing
        End Function
    End Class
End Namespace", safe:=True, useSymbolAnnotations)
        End Function

        <Theory, InlineData(True), InlineData(False)>
        Public Async Function TestSafeWithMatchingGenericNameAndTypeArguments(useSymbolAnnotations As Boolean) As Task
            Await TestAsync(
"Imports B

Namespace A
    Class C1(Of T)
    End Class

    Class C2
    End Class

    Class C3
    End Class
End Namespace

Namespace B
    Class C1(Of T)
    End Class

    Class C3
    End Class
End Namespace

Class C
    Private Function M(ByVal c2 As A.C2) As C1(Of C3)
        Return Nothing
    End Function
End Class",
"Imports A
Imports B

Namespace A
    Class C1(Of T)
    End Class

    Class C2
    End Class

    Class C3
    End Class
End Namespace

Namespace B
    Class C1(Of T)
    End Class

    Class C3
    End Class
End Namespace

Class C
    Private Function M(ByVal c2 As A.C2) As Global.B.C1(Of Global.B.C3)
        Return Nothing
    End Function
End Class",
"Imports A
Imports B

Namespace A
    Class C1(Of T)
    End Class

    Class C2
    End Class

    Class C3
    End Class
End Namespace

Namespace B
    Class C1(Of T)
    End Class

    Class C3
    End Class
End Namespace

Class C
    Private Function M(ByVal c2 As C2) As B.C1(Of B.C3)
        Return Nothing
    End Function
End Class", safe:=True, useSymbolAnnotations)
        End Function

        <Theory, InlineData(True), InlineData(False)>
        Public Async Function TestSafeWithMatchingQualifiedNameAndTypeArguments(useSymbolAnnotations As Boolean) As Task
            Await TestAsync(
"Imports B

Namespace A
    Class O
    End Class

    Class C2
    End Class

    Class C3
    End Class
End Namespace

Namespace B
    Class C3
    End Class

    Class O
        Public Class C1(Of T)
        End Class
    End Class
End Namespace

Class C
    Private Function M(ByVal c2 As A.C2) As O.C1(Of C3)
        Return Nothing
    End Function
End Class",
"Imports A
Imports B

Namespace A
    Class O
    End Class

    Class C2
    End Class

    Class C3
    End Class
End Namespace

Namespace B
    Class C3
    End Class

    Class O
        Public Class C1(Of T)
        End Class
    End Class
End Namespace

Class C
    Private Function M(ByVal c2 As A.C2) As Global.B.O.C1(Of Global.B.C3)
        Return Nothing
    End Function
End Class",
"Imports A
Imports B

Namespace A
    Class O
    End Class

    Class C2
    End Class

    Class C3
    End Class
End Namespace

Namespace B
    Class C3
    End Class

    Class O
        Public Class C1(Of T)
        End Class
    End Class
End Namespace

Class C
    Private Function M(ByVal c2 As C2) As B.O.C1(Of B.C3)
        Return Nothing
    End Function
End Class", safe:=True, useSymbolAnnotations)
        End Function

        <Theory, InlineData(True), InlineData(False)>
        Public Async Function TestSafeWithMatchingSimpleNameInAllLocations(useSymbolAnnotations As Boolean) As Task
            Await TestAsync(
"Imports B
Imports System.Collections.Generic

Namespace A
    Class C1
    End Class

    Class C2
    End Class
End Namespace

Namespace B
    Class C1
        Public Shared ReadOnly Property P As C1
    End Class
End Namespace

Class C
	''' <summary>
	''' <see cref=""C1""/>
	''' </summary>
    Private Function M(ByVal c As C1, ByVal c2 As A.C2) As C1
        Dim result As C1
        result = DirectCast(c, C1)
        result = CType(c, C1)
        result = new C1()
        result = C1.P
        result = new C1(0){}(0)
        Dim list = New List(Of C1)()
        Dim tuple As (C1, Integer) = (Nothing, Nothing)
        Dim t = GetType(C1)
        Dim s = NameOf(C1)

        Return result
    End Function
End Class",
"Imports A
Imports B
Imports System.Collections.Generic

Namespace A
    Class C1
    End Class

    Class C2
    End Class
End Namespace

Namespace B
    Class C1
        Public Shared ReadOnly Property P As Global.B.C1
    End Class
End Namespace

Class C
    ''' <summary>
    ''' <see cref=""Global.B.C1""/>
    ''' </summary>
    Private Function M(ByVal c As Global.B.C1, ByVal c2 As A.C2) As Global.B.C1
        Dim result As Global.B.C1
        result = DirectCast(c, Global.B.C1)
        result = CType(c, Global.B.C1)
        result = new Global.B.C1()
        result = Global.B.C1.P
        result = new Global.B.C1(0){}(0)
        Dim list = New List(Of Global.B.C1)()
        Dim tuple As (Global.B.C1, Integer) = (Nothing, Nothing)
        Dim t = GetType(Global.B.C1)
        Dim s = NameOf(Global.B.C1)

        Return result
    End Function
End Class",
"Imports A
Imports B
Imports System.Collections.Generic

Namespace A
    Class C1
    End Class

    Class C2
    End Class
End Namespace

Namespace B
    Class C1
        Public Shared ReadOnly Property P As C1
    End Class
End Namespace

Class C
    ''' <summary>
    ''' <see cref=""B.C1""/>
    ''' </summary>
    Private Function M(ByVal c As B.C1, ByVal c2 As C2) As B.C1
        Dim result As B.C1
        result = DirectCast(c, B.C1)
        result = CType(c, B.C1)
        result = new B.C1()
        result = B.C1.P
        result = new B.C1(0){}(0)
        Dim list = New List(Of B.C1)()
        Dim tuple As (B.C1, Integer) = (Nothing, Nothing)
        Dim t = GetType(B.C1)
        Dim s = NameOf(B.C1)

        Return result
    End Function
End Class", safe:=True, useSymbolAnnotations)
        End Function

        <Theory, InlineData(True), InlineData(False)>
        Public Async Function TestSafeWithMatchingExtensionMethod(useSymbolAnnotations As Boolean) As Task
            Await TestAsync(
"Imports B
Imports System.Runtime.CompilerServices

Namespace A
    Friend Module AExtensions
        <Extension()>
        Sub M(ByVal a As Integer)
        End Sub
    End Module

    Public Class C1
    End Class
End Namespace

Namespace B
    Friend Module BExtensions
        <Extension()>
        Sub M(ByVal a As Integer)
        End Sub
    End Module
End Namespace

Friend Class C
    Private Sub M(ByVal c1 As A.C1)
		Call 42.M()
    End Sub
End Class",
"Imports A
Imports B
Imports System.Runtime.CompilerServices

Namespace A
    Friend Module AExtensions
        <Extension()>
        Sub M(ByVal a As Integer)
        End Sub
    End Module

    Public Class C1
    End Class
End Namespace

Namespace B
    Friend Module BExtensions
        <Extension()>
        Sub M(ByVal a As Integer)
        End Sub
    End Module
End Namespace

Friend Class C
    Private Sub M(ByVal c1 As A.C1)
		Call Global.B.BExtensions.M((CInt((42))))
    End Sub
End Class",
"Imports A
Imports B
Imports System.Runtime.CompilerServices

Namespace A
    Friend Module AExtensions
        <Extension()>
        Sub M(ByVal a As Integer)
        End Sub
    End Module

    Public Class C1
    End Class
End Namespace

Namespace B
    Friend Module BExtensions
        <Extension()>
        Sub M(ByVal a As Integer)
        End Sub
    End Module
End Namespace

Friend Class C
    Private Sub M(ByVal c1 As C1)
		Call BExtensions.M(42)
    End Sub
End Class", safe:=True, useSymbolAnnotations)
        End Function

        <Theory, InlineData(True), InlineData(False)>
        Public Async Function TestSafeWithMatchingExtensionMethodAndArguments(useSymbolAnnotations As Boolean) As Task
            Await TestAsync("Imports B
Imports System.Runtime.CompilerServices

Namespace A
    Friend Module AExtensions
        <Extension()>
        Sub M(ByVal a As Integer, ByVal c2 As C2)
        End Sub
    End Module

    Public Class C1
    End Class

    Public Class C2
    End Class
End Namespace

Namespace B
    Friend Module BExtensions
        <Extension()>
        Sub M(ByVal a As Integer, ByVal c2 As C2)
        End Sub
    End Module

    Public Class C2
    End Class
End Namespace

Friend Class C
    Private Sub M(ByVal c1 As A.C1)
		Call 42.M(New C2())
    End Sub
End Class",
"Imports A
Imports B
Imports System.Runtime.CompilerServices

Namespace A
    Friend Module AExtensions
        <Extension()>
        Sub M(ByVal a As Integer, ByVal c2 As Global.A.C2)
        End Sub
    End Module

    Public Class C1
    End Class

    Public Class C2
    End Class
End Namespace

Namespace B
    Friend Module BExtensions
        <Extension()>
        Sub M(ByVal a As Integer, ByVal c2 As Global.B.C2)
        End Sub
    End Module

    Public Class C2
    End Class
End Namespace

Friend Class C
    Private Sub M(ByVal c1 As A.C1)
		Call Global.B.BExtensions.M((CInt((42))), (CType((New Global.B.C2()), Global.B.C2)))
    End Sub
End Class",
"Imports A
Imports B
Imports System.Runtime.CompilerServices

Namespace A
    Friend Module AExtensions
        <Extension()>
        Sub M(ByVal a As Integer, ByVal c2 As C2)
        End Sub
    End Module

    Public Class C1
    End Class

    Public Class C2
    End Class
End Namespace

Namespace B
    Friend Module BExtensions
        <Extension()>
        Sub M(ByVal a As Integer, ByVal c2 As C2)
        End Sub
    End Module

    Public Class C2
    End Class
End Namespace

Friend Class C
    Private Sub M(ByVal c1 As C1)
		Call 42.M(New B.C2())
    End Sub
End Class", safe:=True, useSymbolAnnotations)
        End Function

        <Theory, InlineData(True), InlineData(False)>
        Public Async Function TestSafeWithMatchingExtensionMethodAndTypeArguments(useSymbolAnnotations As Boolean) As Task
            Await TestAsync(
"Imports B
Imports System.Runtime.CompilerServices

Namespace A
    Friend Module AExtensions
        <Extension()>
        Sub M(Of T)(ByVal a As Integer)
        End Sub
    End Module

    Public Class C1
    End Class

    Public Class C2
    End Class
End Namespace

Namespace B
    Friend Module BExtensions
        <Extension()>
        Sub M(Of T)(ByVal a As Integer)
        End Sub
    End Module

    Public Class C2
    End Class
End Namespace

Friend Class C
    Private Sub M(ByVal c1 As A.C1)
		Call 42.M(Of C2)()
    End Sub
End Class",
"Imports A
Imports B
Imports System.Runtime.CompilerServices

Namespace A
    Friend Module AExtensions
        <Extension()>
        Sub M(Of T)(ByVal a As Integer)
        End Sub
    End Module

    Public Class C1
    End Class

    Public Class C2
    End Class
End Namespace

Namespace B
    Friend Module BExtensions
        <Extension()>
        Sub M(Of T)(ByVal a As Integer)
        End Sub
    End Module

    Public Class C2
    End Class
End Namespace

Friend Class C
    Private Sub M(ByVal c1 As A.C1)
		Call Global.B.BExtensions.M(Of Global.B.C2)((CInt((42))))
    End Sub
End Class",
"Imports A
Imports B
Imports System.Runtime.CompilerServices

Namespace A
    Friend Module AExtensions
        <Extension()>
        Sub M(Of T)(ByVal a As Integer)
        End Sub
    End Module

    Public Class C1
    End Class

    Public Class C2
    End Class
End Namespace

Namespace B
    Friend Module BExtensions
        <Extension()>
        Sub M(Of T)(ByVal a As Integer)
        End Sub
    End Module

    Public Class C2
    End Class
End Namespace

Friend Class C
    Private Sub M(ByVal c1 As C1)
		Call BExtensions.M(Of B.C2)(42)
    End Sub
End Class", safe:=True, useSymbolAnnotations)
        End Function

        <Theory, InlineData(True), InlineData(False)>
        Public Async Function TestWarnsWithMatchingExtensionMethodUsedAsDelegate(useSymbolAnnotations As Boolean) As Task
            Dim source = "Imports System
Imports B
Imports System.Runtime.CompilerServices

Namespace A
    Friend Module AExtensions
        <Extension()>
        Sub M(ByVal a As Integer)
        End Sub
    End Module

    Public Class C1
    End Class
End Namespace

Namespace B
    Friend Module BExtensions
        <Extension()>
        Sub M(ByVal a As Object)
        End Sub
    End Module
End Namespace

Friend Class C
    Private Function M(ByVal c1 As A.C1) As Action
        Return AddressOf 42.M
    End Function
End Class"
            Await TestAsync(
                source,
"Imports System
Imports B
Imports System.Runtime.CompilerServices
Imports A

Namespace A
    Friend Module AExtensions
        <Extension()>
        Sub M(ByVal a As Integer)
        End Sub
    End Module

    Public Class C1
    End Class
End Namespace

Namespace B
    Friend Module BExtensions
        <Extension()>
        Sub M(ByVal a As Object)
        End Sub
    End Module
End Namespace

Friend Class C
    Private Function M(ByVal c1 As A.C1) As Action
        Return AddressOf 42.M
    End Function
End Class",
"Imports System
Imports B
Imports System.Runtime.CompilerServices
Imports A

Namespace A
    Friend Module AExtensions
        <Extension()>
        Sub M(ByVal a As Integer)
        End Sub
    End Module

    Public Class C1
    End Class
End Namespace

Namespace B
    Friend Module BExtensions
        <Extension()>
        Sub M(ByVal a As Object)
        End Sub
    End Module
End Namespace

Friend Class C
    Private Function M(ByVal c1 As C1) As Action
        Return AddressOf 42.M
    End Function
End Class", safe:=True, useSymbolAnnotations)

            Dim doc = Await GetDocument(source, useSymbolAnnotations)
            Dim options As OptionSet = Await doc.GetOptionsAsync()

            Dim imported = Await ImportAdder.AddImportsFromSyntaxesAsync(doc, True, options)
            Dim root = Await imported.GetSyntaxRootAsync()
            Dim nodeWithWarning = root.GetAnnotatedNodes(WarningAnnotation.Kind).Single()

            Assert.Equal("42.M" & vbCrLf, nodeWithWarning.ToFullString())

            Dim warning = nodeWithWarning.GetAnnotations(WarningAnnotation.Kind).Single()
            Dim expectedWarningMessage = String.Format(WorkspacesResources.Warning_adding_imports_will_bring_an_extension_method_into_scope_with_the_same_name_as_member_access, "M")

            Assert.Equal(expectedWarningMessage, WarningAnnotation.GetDescription(warning))
        End Function

        <WorkItem(39592, "https://github.com/dotnet/roslyn/issues/39592")>
        <Theory, InlineData(True), InlineData(False)>
        Public Async Function TestCanExpandCrefSignaturePart(useSymbolAnnotations As Boolean) As Task
            Await TestAsync(
"Imports B

Namespace A
    Class C1
    End Class

    Class C2
    End Class
End Namespace

Namespace B
    Class C1
    End Class
End Namespace

Class C
    ''' <see cref=""M(C1)""/>
    Private Sub M(ByVal c2 As A.C2)
    End Sub
    Private Sub M(ByVal c1 As C1)
    End Sub
End Class",
"Imports A
Imports B

Namespace A
    Class C1
    End Class

    Class C2
    End Class
End Namespace

Namespace B
    Class C1
    End Class
End Namespace

Class C
    ''' <see cref=""M(Global.B.C1)""/>
    Private Sub M(ByVal c2 As A.C2)
    End Sub
    Private Sub M(ByVal c1 As Global.B.C1)
    End Sub
End Class",
"Imports A
Imports B

Namespace A
    Class C1
    End Class

    Class C2
    End Class
End Namespace

Namespace B
    Class C1
    End Class
End Namespace

Class C
    ''' <see cref=""M(B.C1)""/>
    Private Sub M(ByVal c2 As C2)
    End Sub
    Private Sub M(ByVal c1 As B.C1)
    End Sub
End Class", safe:=True, useSymbolAnnotations)
        End Function

#End Region

    End Class
End Namespace
