' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Allows asking semantic questions about a TypeSyntax or RangeArgumentSyntax node within a member, that did not appear in the original source code.
    ''' Typically, an instance is obtained by a call to SemanticModel.TryGetSpeculativeSemanticModel. 
    ''' </summary>
    Friend NotInheritable Class SpeculativeMemberSemanticModel
        Inherits MemberSemanticModel

        ''' <summary>
        ''' Creates a speculative SemanticModel for a TypeSyntax or a RangeArgumentSyntax node at a position within an existing MemberSemanticModel.
        ''' </summary>
        Public Sub New(parentSemanticModel As SyntaxTreeSemanticModel, root As VisualBasicSyntaxNode, binder As Binder, position As Integer)
            MyBase.New(root, binder, containingSemanticModelOpt:=Nothing, parentSemanticModelOpt:=parentSemanticModel, speculatedPosition:=position)

            Debug.Assert(root IsNot Nothing)
            Debug.Assert(TypeOf root Is TypeSyntax OrElse TypeOf root Is RangeArgumentSyntax)
        End Sub

        Friend Overrides Function TryGetSpeculativeSemanticModelForMethodBodyCore(parentModel As SyntaxTreeSemanticModel, position As Integer, method As MethodBlockBaseSyntax, <Out> ByRef speculativeModel As SemanticModel) As Boolean
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overrides Function TryGetSpeculativeSemanticModelCore(parentModel As SyntaxTreeSemanticModel, position As Integer, statement As ExecutableStatementSyntax, <Out> ByRef speculativeModel As SemanticModel) As Boolean
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overrides Function TryGetSpeculativeSemanticModelCore(parentModel As SyntaxTreeSemanticModel, position As Integer, initializer As EqualsValueSyntax, <Out> ByRef speculativeModel As SemanticModel) As Boolean
            Throw ExceptionUtilities.Unreachable
        End Function
    End Class
End Namespace
