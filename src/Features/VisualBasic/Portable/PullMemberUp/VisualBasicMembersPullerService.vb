' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp
    <ExportLanguageService(GetType(IMembersPullerService), LanguageNames.VisualBasic), [Shared]>
    Friend Class ViualBasicMembersPullerService
        Inherits AbstractMembersPullerService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function FilterType(node As SyntaxNode) As Boolean
            Return TypeOf node Is IdentifierNameSyntax Or TypeOf node Is ObjectCreationExpressionSyntax
        End Function
    End Class
End Namespace

