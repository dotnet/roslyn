' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend MustInherit Class BoundTreeWalker
        Inherits BoundTreeVisitor

        Protected Sub New()
        End Sub

        Public Overridable Sub VisitList(Of T As BoundNode)(list As ImmutableArray(Of T))
            If Not list.IsDefault Then
                For Each item In list
                    Me.Visit(item)
                Next
            End If
        End Sub
    End Class
End Namespace
