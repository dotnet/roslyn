' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class DelegateDeclarationStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of DelegateStatementSyntax)

        Protected Overrides Sub CollectBlockSpans(delegateDeclaration As DelegateStatementSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  cancellationToken As CancellationToken)
            CollectCommentsRegions(delegateDeclaration, spans)
        End Sub
    End Class
End Namespace