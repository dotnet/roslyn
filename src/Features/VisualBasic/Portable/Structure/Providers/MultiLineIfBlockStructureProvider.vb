' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class MultiLineIfBlockStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of MultiLineIfBlockSyntax)

        Protected Overrides Sub CollectBlockSpans(node As MultiLineIfBlockSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  cancellationToken As CancellationToken)
            spans.AddIfNotNull(CreateRegionFromBlock(
                               node, node.IfStatement, autoCollapse:=False,
                               type:=BlockTypes.Conditional, isCollapsible:=True))
        End Sub
    End Class
End Namespace