' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings
    <ExportLanguageService(GetType(ICodeRefactoringService), LanguageNames.VisualBasic)>
    Friend Class VisualBasicCodeRefactoringService
        Inherits AbstractCodeRefactoringService

        Private ReadOnly lazyCodeRefactoringProviders As IEnumerable(Of Lazy(Of CodeRefactoringProvider, OrderableLanguageMetadata))

        Private defaultCodeRefactoringProviders As IEnumerable(Of CodeRefactoringProvider)

        <ImportingConstructor>
        Public Sub New(<ImportMany> codeRefactoringProviders As IEnumerable(Of Lazy(Of CodeRefactoringProvider, OrderableLanguageMetadata)))
            Me.lazyCodeRefactoringProviders = ExtensionOrderer.Order(codeRefactoringProviders.Where(Function(p) p.Metadata.Language = LanguageNames.VisualBasic)).ToImmutableList()
        End Sub

        Public Overrides Function GetDefaultCodeRefactoringProviders() As IEnumerable(Of CodeRefactoringProvider)
            If (Me.defaultCodeRefactoringProviders Is Nothing) Then
                Threading.Interlocked.CompareExchange(Of IEnumerable(Of CodeRefactoringProvider))(Me.defaultCodeRefactoringProviders, Me.lazyCodeRefactoringProviders.Select(Function(lz) lz.Value).ToImmutableList(), Nothing)
            End If

            Return defaultCodeRefactoringProviders
        End Function
    End Class
End Namespace
