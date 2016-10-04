' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class SyncLockBlockStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of SyncLockBlockSyntax)

        Protected Overrides Sub CollectBlockSpans(node As SyncLockBlockSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  cancellationToken As CancellationToken)
            spans.AddIfNotNull(CreateRegionFromBlock(
                               node, node.SyncLockStatement, autoCollapse:=False,
                               type:=BlockTypes.Statement, isCollapsible:=True))
        End Sub
    End Class
End Namespace