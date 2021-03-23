' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.[Shared].Collections
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class DelegateDeclarationStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of DelegateStatementSyntax)

        Protected Overrides Sub CollectBlockSpans(delegateDeclaration As DelegateStatementSyntax,
                                                  ByRef spans As TemporaryArray(Of BlockSpan),
                                                  optionProvider As BlockStructureOptionProvider,
                                                  cancellationToken As CancellationToken)
            CollectCommentsRegions(delegateDeclaration, spans, optionProvider)
        End Sub
    End Class
End Namespace
