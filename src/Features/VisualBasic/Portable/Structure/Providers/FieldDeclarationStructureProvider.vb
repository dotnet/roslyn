' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class FieldDeclarationStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of FieldDeclarationSyntax)

        Protected Overrides Sub CollectBlockSpans(previousToken As SyntaxToken,
                                                  fieldDeclaration As FieldDeclarationSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  options As BlockStructureOptions,
                                                  cancellationToken As CancellationToken)
            CollectCommentsRegions(fieldDeclaration, spans, options)
        End Sub
    End Class
End Namespace
