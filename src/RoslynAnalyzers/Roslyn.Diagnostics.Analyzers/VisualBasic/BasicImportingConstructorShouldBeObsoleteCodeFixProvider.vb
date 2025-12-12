' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes

Namespace Roslyn.Diagnostics.Analyzers
    <ExportCodeFixProvider(LanguageNames.VisualBasic, NameOf(BasicImportingConstructorShouldBeObsoleteCodeFixProvider)), [Shared]>
    Public NotInheritable Class BasicImportingConstructorShouldBeObsoleteCodeFixProvider
        Inherits AbstractImportingConstructorShouldBeObsoleteCodeFixProvider

        <ImportingConstructor>
        <Obsolete("This exported object must be obtained through the MEF export provider.", True)>
        Public Sub New()
        End Sub

        Protected Overrides Function IsOnPrimaryConstructorTypeDeclaration(attributeName As SyntaxNode, ByRef typeDeclaration As SyntaxNode) As Boolean
            Return False
        End Function

        Protected Overrides Function AddMethodTarget(attributeList As SyntaxNode) As SyntaxNode
            Return attributeList
        End Function
    End Class
End Namespace
