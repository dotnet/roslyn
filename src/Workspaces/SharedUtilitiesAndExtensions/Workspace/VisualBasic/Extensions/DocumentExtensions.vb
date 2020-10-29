' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Threading

#If Not CODE_STYLE Then
Imports Microsoft.CodeAnalysis.Host
#End If

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module DocumentExtensions
        <Extension>
        Public Function CanAddImportsStatements(document As Document, node As SyntaxNode, cancellationToken As CancellationToken) As Boolean
            ' Normally we don't allow generation into a hidden region in the file.  However, if we have a
            ' modern span mapper at our disposal, we do allow it as that host span mapper can handle mapping
            ' our edit to their domain appropriate.
#If Not CODE_STYLE Then
            Dim spanMapper = document.Services.GetService(Of ISpanMappingService)()
            Dim allowInHiddenRegions = spanMapper IsNot Nothing AndAlso Not spanMapper.IsLegacy
#Else
            Dim allowInHiddenRegions = false
#End If

            Return node.CanAddImportsStatements(allowInHiddenRegions, cancellationToken)
        End Function
    End Module
End Namespace
