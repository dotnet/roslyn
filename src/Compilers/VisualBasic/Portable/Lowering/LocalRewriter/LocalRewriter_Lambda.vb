' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitLambda(node As BoundLambda) As BoundNode
            ' we only make a note that lambdas are present
            ' they will be dealt with in a different rewriter.
            Me._hasLambdas = True

            Dim originalMethodOrLambda = Me._currentMethodOrLambda
            Me._currentMethodOrLambda = node.LambdaSymbol

            Dim result = MyBase.VisitLambda(node)

            Me._currentMethodOrLambda = originalMethodOrLambda

            Return result
        End Function
    End Class
End Namespace
