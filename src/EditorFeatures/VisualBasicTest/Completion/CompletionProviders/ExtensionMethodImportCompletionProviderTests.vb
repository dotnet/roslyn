' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders

    <UseExportProvider>
    Public Class ExtensionMethodImportCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Public Sub New()
            ShowImportCompletionItemsOptionValue = True
            ForceExpandedCompletionIndexCreation = True
        End Sub

        Friend Overrides Function GetCompletionProviderType() As Type
            Return GetType(ExtensionMethodImportCompletionProvider)
        End Function

        Public Enum ReferenceType
            None
            Project
            Metadata
        End Enum

        Public Shared Function ReferenceTypeData() As IEnumerable(Of Object())
            Return (New ReferenceType() {ReferenceType.None, ReferenceType.Project, ReferenceType.Metadata}).Select(Function(refType)
                                                                                                                        Return New Object() {refType}
                                                                                                                    End Function)
        End Function

        Private Shared Function GetMarkup(current As String, referenced As String, refType As ReferenceType, Optional currentLanguage As String = LanguageNames.VisualBasic, Optional referencedLanguage As String = LanguageNames.VisualBasic) As String
            If refType = ReferenceType.None Then
                Return CreateMarkupForSingleProject(current, referenced, currentLanguage)
            ElseIf refType = ReferenceType.Project Then
                Return GetMarkupWithReference(current, referenced, currentLanguage, referencedLanguage, True)
            ElseIf refType = ReferenceType.Metadata Then
                Return GetMarkupWithReference(current, referenced, currentLanguage, referencedLanguage, False)
            Else
                Return Nothing
            End If
        End Function

        <InlineData(ReferenceType.None)>
        <InlineData(ReferenceType.Project)>
        <Theory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestExtensionAttribute(refType As ReferenceType) As Task

            ' attribute suffix isn't capitalized
            Dim file1 = <Text><![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Namespace Foo
    Public Module ExtensionModule

        <System.Runtime.CompilerServices.Extension()>
        Public Sub ExtensionMethod1(aString As String)
            Console.WriteLine(aString)
        End Sub

        <Extension>
        Public Sub ExtensionMethod2(aString As String)
            Console.WriteLine(aString)
        End Sub

        <ExtensionAttribute>
        Public Sub ExtensionMethod3(aString As String)
            Console.WriteLine(aString)
        End Sub
        
        <Extension()>
        Public Sub ExtensionMethod4(aString As String)
            Console.WriteLine(aString)
        End Sub

        <System.Runtime.CompilerServices.ExtensionAttribute>
        Public Sub ExtensionMethod5(aString As String)
            Console.WriteLine(aString)
        End Sub

        <extension()>
        Public Sub ExtensionMethod6(aString As String)
            Console.WriteLine(aString)
        End Sub

        Public Sub ExtensionMethod7(aString As String)
            Console.WriteLine(aString)
        End Sub
    End Module
End Namespace]]></Text>.Value

            Dim file2 = <Text><![CDATA[
Public Class Bar
    Sub Main()
        dim x = ""
        x.$$
    End Sub
End Class]]></Text>.Value

            Dim markup = GetMarkup(file2, file1, refType)
            Await VerifyItemExistsAsync(markup, "ExtensionMethod1", glyph:=Glyph.ExtensionMethodPublic, inlineDescription:="Foo")
            Await VerifyItemExistsAsync(markup, "ExtensionMethod2", glyph:=Glyph.ExtensionMethodPublic, inlineDescription:="Foo")
            Await VerifyItemExistsAsync(markup, "ExtensionMethod3", glyph:=Glyph.ExtensionMethodPublic, inlineDescription:="Foo")
            Await VerifyItemExistsAsync(markup, "ExtensionMethod4", glyph:=Glyph.ExtensionMethodPublic, inlineDescription:="Foo")
            Await VerifyItemExistsAsync(markup, "ExtensionMethod5", glyph:=Glyph.ExtensionMethodPublic, inlineDescription:="Foo")
            Await VerifyItemExistsAsync(markup, "ExtensionMethod6", glyph:=Glyph.ExtensionMethodPublic, inlineDescription:="Foo")
            Await VerifyItemIsAbsentAsync(markup, "ExtensionMethod7", inlineDescription:="Foo")
        End Function

        <InlineData(ReferenceType.None)>
        <InlineData(ReferenceType.Project)>
        <Theory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCaseMismatchInTargetType(refType As ReferenceType) As Task

            ' attribute suffix isn't capitalized
            Dim file1 = <Text><![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Namespace Foo
    Public Module ExtensionModule

        <Extension>
        Public Sub ExtensionMethod1(exp As exception)
        End Sub

        <Extension>
        Public Sub ExtensionMethod2(exp As Exception)
            Console.WriteLine(aString)
        End Sub
    End Module
End Namespace]]></Text>.Value

            Dim file2 = <Text><![CDATA[
Imports System

Public Class Bar
    Sub M(x as exception)
        x.$$
    End Sub
End Class]]></Text>.Value

            Dim markup = GetMarkup(file2, file1, refType)
            Await VerifyItemExistsAsync(markup, "ExtensionMethod1", glyph:=Glyph.ExtensionMethodPublic, inlineDescription:="Foo")
            Await VerifyItemExistsAsync(markup, "ExtensionMethod2", glyph:=Glyph.ExtensionMethodPublic, inlineDescription:="Foo")
        End Function

        <InlineData(ReferenceType.None)>
        <InlineData(ReferenceType.Project)>
        <Theory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCaseMismatchInNamespaceImport(refType As ReferenceType) As Task

            ' attribute suffix isn't capitalized
            Dim file1 = <Text><![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Namespace foo
    Public Module ExtensionModule

        <Extension>
        Public Sub ExtensionMethod1(exp As exception)
        End Sub

        <Extension>
        Public Sub ExtensionMethod2(exp As Exception)
            Console.WriteLine(aString)
        End Sub
    End Module
End Namespace]]></Text>.Value

            Dim file2 = <Text><![CDATA[
Imports System
Imports Foo

Public Class Bar
    Sub M(x as exception)
        x.$$
    End Sub
End Class]]></Text>.Value

            Dim markup = GetMarkup(file2, file1, refType)
            Await VerifyItemIsAbsentAsync(markup, "ExtensionMethod1", inlineDescription:="Foo")
            Await VerifyItemIsAbsentAsync(markup, "ExtensionMethod2", inlineDescription:="Foo")
        End Function

        <InlineData(ReferenceType.None)>
        <InlineData(ReferenceType.Project)>
        <Theory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplicitTarget1(refType As ReferenceType) As Task

            Dim file1 = <Text><![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Namespace NS
    Public Module Foo
        <Extension>
        Public Function ExtentionMethod(x As Bar) As Boolean
            Return True
        End Function
    End Module

    Public Class Bar
        Public X As Boolean
    End Class
End Namespace]]></Text>.Value

            Dim file2 = <Text><![CDATA[
Imports System

Public Class Baz
    Sub M()
        Dim x = New Bar() {.$$}
    End Sub
End Class]]></Text>.Value

            Dim markup = GetMarkup(file2, file1, refType)
            Await VerifyItemIsAbsentAsync(markup, "ExtentionMethod", inlineDescription:="NS")
        End Function

        <InlineData(ReferenceType.None)>
        <InlineData(ReferenceType.Project)>
        <Theory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestImplicitTarget2(refType As ReferenceType) As Task

            Dim file1 = <Text><![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Namespace NS
    Public Module Foo
        <Extension>
        Public Function ExtentionMethod(x As Bar) As Boolean
            Return True
        End Function
    End Module

    Public Class Bar
        Public X As Boolean
    End Class
End Namespace]]></Text>.Value

            Dim file2 = <Text><![CDATA[
Imports System

Public Class Baz
    Sub M()
        Dim x = New Bar() {.X = .$$}
    End Sub
End Class]]></Text>.Value

            Dim markup = GetMarkup(file2, file1, refType)
            Await VerifyItemIsAbsentAsync(markup, "ExtentionMethod", inlineDescription:="NS")
        End Function

        <InlineData(ReferenceType.Project, "()", "ExtentionMethod2")>
        <InlineData(ReferenceType.Project, "()()", "ExtentionMethod3")>
        <InlineData(ReferenceType.Project, "(,)", "ExtentionMethod4")>
        <InlineData(ReferenceType.Project, "()(,)", "ExtentionMethod5")>
        <InlineData(ReferenceType.None, "()", "ExtentionMethod2")>
        <InlineData(ReferenceType.None, "()()", "ExtentionMethod3")>
        <InlineData(ReferenceType.None, "(,)", "ExtentionMethod4")>
        <InlineData(ReferenceType.None, "()(,)", "ExtentionMethod5")>
        <Theory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestExtensionMethodsForArrayType(refType As ReferenceType, rank As String, expectedName As String) As Task

            Dim file1 = <Text><![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Namespace NS
    Public Module Foo
        <Extension>
        Public Function ExtentionMethod1(x As Integer) As Boolean
            Return True
        End Function
        <Extension>
        Public Function ExtentionMethod2(x As Integer()) As Boolean
            Return True
        End Function
        <Extension>
        Public Function ExtentionMethod3(x As Integer()()) As Boolean
            Return True
        End Function
        <Extension>
        Public Function ExtentionMethod4(x As Integer(,)) As Boolean
            Return True
        End Function
        <Extension>
        Public Function ExtentionMethod5(x As Integer()(,)) As Boolean
            Return True
        End Function
    End Module
End Namespace]]></Text>.Value

            Dim file2 As String = $"
Imports System

Public Class Baz
    Sub M(x As Integer{rank})
        x.$$
    End Sub
End Class"

            Dim markup = GetMarkup(file2, file1, refType)
            Await VerifyItemExistsAsync(markup, expectedName, glyph:=Glyph.ExtensionMethodPublic, inlineDescription:="NS")
        End Function
    End Class
End Namespace
