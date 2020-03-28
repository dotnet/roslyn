' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Organizing.Organizers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Organizing.Organizers
    <ExportSyntaxNodeOrganizer(LanguageNames.VisualBasic), [Shared]>
    Friend Class TypeBlockOrganizer
        Inherits AbstractSyntaxNodeOrganizer(Of TypeBlockSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function Organize(typeBlock As TypeBlockSyntax,
                                              cancellationToken As CancellationToken) As TypeBlockSyntax
            Dim members = MemberDeclarationsOrganizer.Organize(typeBlock.Members, cancellationToken)
            Return typeBlock.WithMembers(members)
        End Function
    End Class
End Namespace
