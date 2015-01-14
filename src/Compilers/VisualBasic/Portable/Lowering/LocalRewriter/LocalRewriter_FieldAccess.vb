' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitFieldAccess(node As BoundFieldAccess) As BoundNode
            Dim receiverOpt As BoundExpression = If(node.FieldSymbol.IsShared, Nothing, Me.VisitExpressionNode(node.ReceiverOpt))
            Return node.Update(receiverOpt, node.FieldSymbol, node.IsLValue, node.SuppressVirtualCalls, node.ConstantsInProgressOpt, node.Type)
        End Function
    End Class
End Namespace
