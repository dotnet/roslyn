' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Organizing.Organizers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Organizing.Organizers
    <ExportSyntaxNodeOrganizer(LanguageNames.VisualBasic), [Shared]>
    Friend Class TypeBlockOrganizer
        Inherits AbstractSyntaxNodeOrganizer(Of TypeBlockSyntax)

        Protected Overrides Function Organize(typeBlock As TypeBlockSyntax,
                                              cancellationToken As CancellationToken) As TypeBlockSyntax
            Dim members = MemberDeclarationsOrganizer.Organize(typeBlock.Members, cancellationToken)
            Return typeBlock.WithMembers(members)
        End Function
    End Class
End Namespace
