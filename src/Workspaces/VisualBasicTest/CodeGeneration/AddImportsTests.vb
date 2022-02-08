' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.AddImport
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Formatting
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

        Private Shared Async Function GetDocument(code As String, withAnnotations As Boolean, Optional globalImports As String() = Nothing) As Task(Of Document)
            Dim ws As AdhocWorkspace = New AdhocWorkspace()
            Dim project As Project = ws.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Default,
                    "test",
                    "test.dll",
                    LanguageNames.VisualBasic,
                    metadataReferences:={TestMetadata.Net451.mscorlib}))

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

        Private Shared Function TestNoImportsAddedAsync(
            initialText As String,
            useSymbolAnnotations As Boolean,
            Optional placeSystemNamespaceFirst As Boolean = False,
            Optional globalImports As String() = Nothing) As Task

            Return TestAsync(initialText, initialText, initialText, useSymbolAnnotations, placeSystemNamespaceFirst, globalImports, performCheck:=False)
        End Function

        Private Shared Async Function TestAsync(
                initialText As String,
                importsAddedText As String,
                simplifiedText As String,
                useSymbolAnnotations As Boolean,
                Optional placeSystemNamespaceFirst As Boolean = True,
                Optional globalImports As String() = Nothing,
                Optional performCheck As Boolean = True) As Task

            Dim doc = Await GetDocument(initialText, useSymbolAnnotations, globalImports)

            Dim addImportOptions = New AddImportPlacementOptions(
                PlaceSystemNamespaceFirst:=placeSystemNamespaceFirst,
                PlaceImportsInsideNamespaces:=False,
                AllowInHiddenRegions:=False)

            Dim imported = If(
                    useSymbolAnnotations,
                    Await ImportAdder.AddImportsFromSymbolAnnotationAsync(doc, addImportOptions, CancellationToken.None),
                    Await ImportAdder.AddImportsFromSyntaxesAsync(doc, addImportOptions, CancellationToken.None))

            If importsAddedText IsNot Nothing Then
                Dim formatted = Await Formatter.FormatAsync(imported, SyntaxAnnotation.ElasticAnnotation)
                Dim actualText = (Await formatted.GetTextAsync()).ToString()
                Assert.Equal(importsAddedText, actualText)
            End If

            If simplifiedText IsNot Nothing Then
                Dim reduced = Await Simplifier.ReduceAsync(imported)
                Dim formatted = Await Formatter.FormatAsync(reduced, SyntaxAnnotation.ElasticAnnotation)
                Dim actualText = (Await formatted.GetTextAsync()).ToString()
                Assert.Equal(simplifiedText, actualText)
            End If

            If performCheck Then
                If initialText = importsAddedText AndAlso importsAddedText = simplifiedText Then
                    Throw New Exception($"use {NameOf(TestNoImportsAddedAsync)}")
                End If
            End If
        End Function

        Public Shared TestAllData As Object()() = {
            New Object() {False},
            New Object() {True}
        }

        <Theory, MemberData(NameOf(TestAllData))>
        Public Async Function TestAddImport(useSymbolAnnotations As Boolean) As Task
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
End Class", useSymbolAnnotations)
        End Function

        <Theory, MemberData(NameOf(TestAllData))>
        Public Async Function TestAddSystemImportFirst(useSymbolAnnotations As Boolean) As Task
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
End Class", useSymbolAnnotations)
        End Function

        <Theory, MemberData(NameOf(TestAllData))>
        Public Async Function TestDontAddSystemImportFirst(useSymbolAnnotations As Boolean) As Task
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
            useSymbolAnnotations,
            placeSystemNamespaceFirst:=False)
        End Function

        <Theory, MemberData(NameOf(TestAllData))>
        Public Async Function TestAddImportsInOrder(useSymbolAnnotations As Boolean) As Task
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
End Class", useSymbolAnnotations)
        End Function

        <Theory, MemberData(NameOf(TestAllData))>
        Public Async Function TestAddMultipleImportsInOrder(useSymbolAnnotations As Boolean) As Task
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
End Class", useSymbolAnnotations)
        End Function

        <Theory, MemberData(NameOf(TestAllData))>
        Public Async Function TestImportNotAddedAgainIfAlreadyExists(useSymbolAnnotations As Boolean) As Task
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
End Class", useSymbolAnnotations)
        End Function

        <Fact>
        Public Async Function TestBuiltInTypeFromSyntaxes() As Task
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
End Class", useSymbolAnnotations:=False)
        End Function

        <Fact>
        Public Async Function TestBuiltInTypeFromSymbols() As Task
            Await TestAsync(
"Class C
    Public F As System.Int32
End Class",
"Class C
    Public F As System.Int32
End Class",
"Class C
    Public F As Integer
End Class", useSymbolAnnotations:=True)
        End Function

        <Theory, MemberData(NameOf(TestAllData))>
        Public Async Function TestImportNotAddedIfGloballyImported(useSymbolAnnotations As Boolean) As Task
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
    useSymbolAnnotations,
    globalImports:={"System.Collections.Generic"})

        End Function

        <Theory, MemberData(NameOf(TestAllData))>
        Public Async Function TestImportNotAddedForNamespaceDeclarations(useSymbolAnnotations As Boolean) As Task
            Await TestNoImportsAddedAsync(
"Namespace N
End Namespace", useSymbolAnnotations)
        End Function

        <Theory, MemberData(NameOf(TestAllData))>
        Public Async Function TestImportAddedAndRemovedForReferencesInsideNamespaceDeclarations(useSymbolAnnotations As Boolean) As Task
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
End Namespace", useSymbolAnnotations)
        End Function

        <Theory, MemberData(NameOf(TestAllData))>
        Public Async Function TestRemoveImportIfItMakesReferencesAmbiguous(useSymbolAnnotations As Boolean) As Task
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
", useSymbolAnnotations)
        End Function

        <Theory, MemberData(NameOf(TestAllData))>
        Public Async Function TestPartialNamespacesNotUsed(useSymbolAnnotations As Boolean) As Task
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
End Class", useSymbolAnnotations)
        End Function

        <Theory, MemberData(NameOf(TestAllData))>
        Public Async Function TestDontAddImportWithExisitingImportDifferentCase(useSymbolAnnotations As Boolean) As Task
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
End Class", useSymbolAnnotations)
        End Function

#Region "AddImports Safe Tests"

        <Fact>
        Public Async Function TestSafeWithMatchingSimpleName() As Task
            Await TestNoImportsAddedAsync(
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
End Class", useSymbolAnnotations:=True)
        End Function

        <Fact>
        Public Async Function TestSafeWithMatchingSimpleName_CaseInsensitive() As Task
            Await TestNoImportsAddedAsync(
"Imports B

Namespace A
    Class c1
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
End Class", useSymbolAnnotations:=True)
        End Function

        <Fact>
        Public Async Function TestSafeWithMatchingSimpleNameDifferentCase() As Task
            Await TestNoImportsAddedAsync(
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
End Class", useSymbolAnnotations:=True)
        End Function

        <Fact>
        Public Async Function TestSafeWithMatchingGenericName() As Task
            Await TestNoImportsAddedAsync(
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
End Class", useSymbolAnnotations:=True)
        End Function

        <Fact>
        Public Async Function TestSafeWithMatchingGenericName_DifferentArity() As Task
            Await TestAsync(
"Imports B

Namespace A
    Class C1(Of T, X)
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
    Class C1(Of T, X)
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
    Class C1(Of T, X)
    End Class

    Class C2
    End Class
End Namespace

Namespace B
    Class C1(Of T)
    End Class
End Namespace

Class C
    Private Function M(ByVal c2 As C2) As C1(Of Integer)
        Return Nothing
    End Function
End Class",
useSymbolAnnotations:=True)
        End Function

        <Fact>
        Public Async Function TestSafeWithMatchingQualifiedName() As Task
            Await TestNoImportsAddedAsync(
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
End Class", useSymbolAnnotations:=True)
        End Function

        <Fact>
        Public Async Function TestSafeWithMatchingAliasedIdentifierName() As Task
            Await TestNoImportsAddedAsync(
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
End Namespace", useSymbolAnnotations:=True)
        End Function

        <Fact>
        Public Async Function TestSafeWithMatchingGenericNameAndTypeArguments() As Task
            Await TestNoImportsAddedAsync(
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
End Class", useSymbolAnnotations:=True)
        End Function

        <Fact>
        Public Async Function TestSafeWithMatchingQualifiedNameAndTypeArguments() As Task
            Await TestNoImportsAddedAsync(
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
End Class", useSymbolAnnotations:=True)
        End Function

        <Fact>
        Public Async Function TestSafeWithMatchingSimpleNameInAllLocations() As Task
            Await TestNoImportsAddedAsync(
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
End Class", useSymbolAnnotations:=True)
        End Function

        <Fact>
        Public Async Function TestSafeWithMatchingExtensionMethod() As Task
            Await TestNoImportsAddedAsync(
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
End Class", useSymbolAnnotations:=True)
        End Function

        <Fact>
        Public Async Function TestSafeWithMatchingExtensionMethod_CaseInsensitive() As Task
            Await TestNoImportsAddedAsync(
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
		Call 42.m()
    End Sub
End Class", useSymbolAnnotations:=True)
        End Function

        <Fact>
        Public Async Function TestSafeWithMatchingExtensionMethodAndArguments() As Task
            Await TestNoImportsAddedAsync(
"Imports B
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
End Class", useSymbolAnnotations:=True)
        End Function

        <Fact>
        Public Async Function TestSafeWithMatchingExtensionMethodAndTypeArguments() As Task
            Await TestNoImportsAddedAsync(
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
End Class", useSymbolAnnotations:=True)
        End Function

        '        <Theory, InlineData(True), InlineData(False)>
        '        Public Async Function TestWarnsWithMatchingExtensionMethodUsedAsDelegate(useSymbolAnnotations As Boolean) As Task
        '            Dim source = "Imports System
        'Imports B
        'Imports System.Runtime.CompilerServices

        'Namespace A
        '    Friend Module AExtensions
        '        <Extension()>
        '        Sub M(ByVal a As Integer)
        '        End Sub
        '    End Module

        '    Public Class C1
        '    End Class
        'End Namespace

        'Namespace B
        '    Friend Module BExtensions
        '        <Extension()>
        '        Sub M(ByVal a As Object)
        '        End Sub
        '    End Module
        'End Namespace

        'Friend Class C
        '    Private Function M(ByVal c1 As A.C1) As Action
        '        Return AddressOf 42.M
        '    End Function
        'End Class"
        '            Await TestAsync(
        '                source,
        '"Imports System
        'Imports B
        'Imports System.Runtime.CompilerServices
        'Imports A

        'Namespace A
        '    Friend Module AExtensions
        '        <Extension()>
        '        Sub M(ByVal a As Integer)
        '        End Sub
        '    End Module

        '    Public Class C1
        '    End Class
        'End Namespace

        'Namespace B
        '    Friend Module BExtensions
        '        <Extension()>
        '        Sub M(ByVal a As Object)
        '        End Sub
        '    End Module
        'End Namespace

        'Friend Class C
        '    Private Function M(ByVal c1 As A.C1) As Action
        '        Return AddressOf 42.M
        '    End Function
        'End Class",
        '"Imports System
        'Imports B
        'Imports System.Runtime.CompilerServices
        'Imports A

        'Namespace A
        '    Friend Module AExtensions
        '        <Extension()>
        '        Sub M(ByVal a As Integer)
        '        End Sub
        '    End Module

        '    Public Class C1
        '    End Class
        'End Namespace

        'Namespace B
        '    Friend Module BExtensions
        '        <Extension()>
        '        Sub M(ByVal a As Object)
        '        End Sub
        '    End Module
        'End Namespace

        'Friend Class C
        '    Private Function M(ByVal c1 As C1) As Action
        '        Return AddressOf 42.M
        '    End Function
        'End Class", useSymbolAnnotations)

        '            Dim doc = Await GetDocument(source, useSymbolAnnotations)
        '            Dim options As OptionSet = Await doc.GetOptionsAsync()

        '            Dim imported = Await ImportAdder.AddImportsFromSyntaxesAsync(doc, True, options)
        '            Dim root = Await imported.GetSyntaxRootAsync()
        '            Dim nodeWithWarning = root.GetAnnotatedNodes(WarningAnnotation.Kind).Single()

        '            Assert.Equal("42.M" & vbCrLf, nodeWithWarning.ToFullString())

        '            Dim warning = nodeWithWarning.GetAnnotations(WarningAnnotation.Kind).Single()
        '            Dim expectedWarningMessage = String.Format(WorkspacesResources.Warning_adding_imports_will_bring_an_extension_method_into_scope_with_the_same_name_as_member_access, "M")

        '            Assert.Equal(expectedWarningMessage, WarningAnnotation.GetDescription(warning))
        '        End Function

        <Fact, WorkItem(39592, "https://github.com/dotnet/roslyn/issues/39592")>
        Public Async Function TestCanExpandCrefSignaturePart() As Task
            Await TestNoImportsAddedAsync(
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
End Class", useSymbolAnnotations:=True)
        End Function

        <Fact, WorkItem(39592, "https://github.com/dotnet/roslyn/issues/39592")>
        Public Async Function TestSafeWithLambdaExtensionMethodAmbiguity() As Task
            Await TestNoImportsAddedAsync(
"Imports System
Imports System.Runtime.CompilerServices

Class C
    ' Don't add a using for N even though it is used here.
    Public x As N.Other

    Public Sub Main()
        M(Sub(x) x.M1())
    End Sub

    Public Shared Sub M(a As Action(Of C))
    End Sub
    Public Shared Sub M(a As Action(Of Integer))
    End Sub

    Public Sub M1()
    End Sub
End Class

Namespace N
    Public Class Other
    End Class

    Public Module Extensions
        <Extension>
        Public Sub M1(a As Integer)
        End Sub
    End Module
End Namespace", useSymbolAnnotations:=True)
        End Function

#End Region

    End Class
End Namespace
