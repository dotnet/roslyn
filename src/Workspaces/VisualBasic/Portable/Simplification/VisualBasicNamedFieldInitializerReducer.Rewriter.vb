' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicNamedFieldInitializerReducer
        Private Class Rewriter
            Inherits AbstractExpressionRewriter

            Public Sub New(optionSet As OptionSet, cancellationToken As CancellationToken)
                MyBase.New(optionSet, cancellationToken)
            End Sub

            Public Overrides Function VisitNamedFieldInitializer(node As NamedFieldInitializerSyntax) As SyntaxNode
                Return SimplifyNode(node, MyBase.VisitNamedFieldInitializer(node), node.Parent, AddressOf SimplifyNamedFieldInitializer)
            End Function
        End Class
    End Class
End Namespace
