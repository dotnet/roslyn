' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService

Namespace Roslyn.Diagnostics.Analyzers
    <ExportCodeFixProvider(LanguageNames.VisualBasic, NameOf(BasicImportingConstructorShouldBeObsoleteCodeFixProvider)), [Shared]>
    Public NotInheritable Class BasicImportingConstructorShouldBeObsoleteCodeFixProvider
        Inherits AbstractImportingConstructorShouldBeObsoleteCodeFixProvider

        <ImportingConstructor>
        <Obsolete("This exported object must be obtained through the MEF export provider.", True)>
        Public Sub New()
        End Sub

        Private Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts = VisualBasicSyntaxFacts.Instance

        Private Protected Overrides ReadOnly Property SyntaxGeneratorInternal As SyntaxGeneratorInternal = VisualBasicSyntaxGeneratorInternal.Instance
    End Class
End Namespace
