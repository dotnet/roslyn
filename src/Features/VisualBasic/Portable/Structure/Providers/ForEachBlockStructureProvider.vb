' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class ForEachBlockStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of ForEachBlockSyntax)

        Protected Overrides Sub CollectBlockSpans(node As ForEachBlockSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  options As OptionSet,
                                                  cancellationToken As CancellationToken)
            spans.AddIfNotNull(CreateBlockSpanFromBlock(
                               node, node.ForOrForEachStatement, autoCollapse:=False,
                               type:=BlockTypes.Loop, isCollapsible:=True))
        End Sub
    End Class
End Namespace
