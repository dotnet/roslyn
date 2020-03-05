' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.RenameTracking
    ' TODO: Remove the ExtensionOrder attributes once a better ordering mechanism is available

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.RenameTracking), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.AddImport)>
    <ExtensionOrder(Before:=PredefinedCodeFixProviderNames.AddMissingReference)>
    <ExtensionOrder(Before:=PredefinedCodeFixProviderNames.FullyQualify)>
    <ExtensionOrder(Before:=PredefinedCodeFixProviderNames.FixIncorrectExitContinue)>
    <ExtensionOrder(Before:=PredefinedCodeFixProviderNames.GenerateConstructor)>
    <ExtensionOrder(Before:=PredefinedCodeFixProviderNames.GenerateEndConstruct)>
    <ExtensionOrder(Before:=PredefinedCodeFixProviderNames.GenerateEnumMember)>
    <ExtensionOrder(Before:=PredefinedCodeFixProviderNames.GenerateEvent)>
    <ExtensionOrder(Before:=PredefinedCodeFixProviderNames.GenerateVariable)>
    <ExtensionOrder(Before:=PredefinedCodeFixProviderNames.GenerateMethod)>
    <ExtensionOrder(Before:=PredefinedCodeFixProviderNames.GenerateType)>
    <ExtensionOrder(Before:=PredefinedCodeFixProviderNames.ImplementAbstractClass)>
    <ExtensionOrder(Before:=PredefinedCodeFixProviderNames.ImplementInterface)>
    <ExtensionOrder(Before:=PredefinedCodeFixProviderNames.MoveToTopOfFile)>
    <ExtensionOrder(Before:=PredefinedCodeFixProviderNames.RemoveUnnecessaryCast)>
    <ExtensionOrder(Before:=PredefinedCodeFixProviderNames.RemoveUnnecessaryImports)>
    <ExtensionOrder(Before:=PredefinedCodeFixProviderNames.SimplifyNames)>
    <ExtensionOrder(Before:=PredefinedCodeFixProviderNames.SpellCheck)>
    Friend NotInheritable Class VisualBasicRenameTrackingCodeFixProvider
        Inherits AbstractRenameTrackingCodeFixProvider

        <ImportingConstructor>
        Public Sub New(undoHistoryRegistry As ITextUndoHistoryRegistry, <ImportMany> refactorNotifyServices As IEnumerable(Of IRefactorNotifyService))
            MyBase.New(undoHistoryRegistry, refactorNotifyServices)
        End Sub
    End Class
End Namespace
