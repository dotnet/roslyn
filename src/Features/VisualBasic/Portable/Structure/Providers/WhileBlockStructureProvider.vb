' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class WhileBlockStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of WhileBlockSyntax)

        Protected Overrides Sub CollectBlockSpans(node As WhileBlockSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  cancellationToken As CancellationToken)
            spans.AddIfNotNull(CreateRegionFromBlock(
                               node, node.WhileStatement, autoCollapse:=False,
                               type:=BlockTypes.Loop, isCollapsible:=True))
        End Sub
    End Class
End Namespace