﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure

    Friend Class ObjectCreationInitializerStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of ObjectCreationInitializerSyntax)

        Protected Overrides Sub CollectBlockSpans(node As ObjectCreationInitializerSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  options As OptionSet,
                                                  cancellationToken As CancellationToken)

            ' ObjectCreationInitializerSyntax is either "With { ... }" or "From { ... }"
            ' Parent Is something Like
            '
            '      New Dictionary(Of int, string) From {
            '          ...
            '      }
            '
            ' The collapsed textspan should be from the   )   to the   }
            '
            ' However, the hint span should be the entire object creation.
            Dim previousToken = node.GetFirstToken().GetPreviousToken()
            spans.Add(New BlockSpan(
                isCollapsible:=True,
                textSpan:=TextSpan.FromBounds(previousToken.Span.End, node.Span.End),
                hintSpan:=node.Parent.Span,
                type:=BlockTypes.Expression))
        End Sub
    End Class
End Namespace
