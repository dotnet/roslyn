' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService

Namespace Microsoft.CodeAnalysis.VisualBasic.SelectedMembers
    <ExportLanguageService(GetType(ISelectedMembersService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicSelectedMembersService
        Implements ISelectedMembersService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function GetSelectedMembersAsync(
                tree As SyntaxTree, span As TextSpan, allowPartialSelection As Boolean, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of SyntaxNode)) Implements ISelectedMembersService.GetSelectedMembersAsync
            Return VisualBasicSelectedMembers.Instance.GetSelectedMembersAsync(tree, span, allowPartialSelection, cancellationToken)
        End Function
    End Class
End Namespace
