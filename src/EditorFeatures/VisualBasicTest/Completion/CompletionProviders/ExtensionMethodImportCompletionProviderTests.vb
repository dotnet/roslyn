' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Experiments
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
Imports Microsoft.VisualStudio.Composition

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders

    <UseExportProvider>
    Public Class ExtensionMethodImportCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Private Property ShowImportCompletionItemsOptionValue As Boolean = True

        ' -1 would disable timebox, whereas 0 means always timeout.
        Private Property TimeoutInMilliseconds As Integer = -1

        Protected Overrides Sub SetWorkspaceOptions(workspace As TestWorkspace)
            workspace.Options = workspace.Options _
                .WithChangedOption(CompletionOptions.ShowItemsFromUnimportedNamespaces, LanguageNames.VisualBasic, ShowImportCompletionItemsOptionValue) _
                .WithChangedOption(CompletionServiceOptions.TimeoutInMillisecondsForImportCompletion, TimeoutInMilliseconds)
        End Sub

        Protected Overrides Function GetExportProvider() As ExportProvider
            Return ExportProviderCache.GetOrCreateExportProviderFactory(TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithPart(GetType(TestExperimentationService))).CreateExportProvider()
        End Function

        Friend Overrides Function CreateCompletionProvider() As CompletionProvider
            Return New ExtensionMethodImportCompletionProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestExtensionAttribute() As Task

            ' attribute suffix isn't capitalized
            Dim file1 = <Text><![CDATA[
Imports System.Runtime.CompilerServices

Namespace Foo
    Module ExtensionModule

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

        Public Sub ExtensionMethod6(aString As String)
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

            Dim markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.VisualBasic)
            Await VerifyItemExistsAsync(markup, "ExtensionMethod1", glyph:=Glyph.ExtensionMethodPublic, inlineDescription:="Foo")
            Await VerifyItemExistsAsync(markup, "ExtensionMethod2", glyph:=Glyph.ExtensionMethodPublic, inlineDescription:="Foo")
            Await VerifyItemExistsAsync(markup, "ExtensionMethod3", glyph:=Glyph.ExtensionMethodPublic, inlineDescription:="Foo")
            Await VerifyItemExistsAsync(markup, "ExtensionMethod4", glyph:=Glyph.ExtensionMethodPublic, inlineDescription:="Foo")
            Await VerifyItemExistsAsync(markup, "ExtensionMethod5", glyph:=Glyph.ExtensionMethodPublic, inlineDescription:="Foo")
            Await VerifyItemIsAbsentAsync(markup, "ExtensionMethod6", inlineDescription:="Foo")
        End Function
    End Class
End Namespace
