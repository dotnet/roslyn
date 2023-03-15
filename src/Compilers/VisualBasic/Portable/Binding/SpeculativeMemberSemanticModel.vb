' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
        Public Sub New(parentSemanticModel As SpeculativeSemanticModelWithMemberModel, root As VisualBasicSyntaxNode, binder As Binder)
            MyBase.New(root, binder, containingPublicSemanticModel:=parentSemanticModel)

            Debug.Assert(root IsNot Nothing)
            Debug.Assert(TypeOf root Is TypeSyntax OrElse TypeOf root Is RangeArgumentSyntax)
        End Sub

        Friend Overrides Function TryGetSpeculativeSemanticModelForMethodBodyCore(parentModel As SyntaxTreeSemanticModel, position As Integer, method As MethodBlockBaseSyntax, <Out> ByRef speculativeModel As PublicSemanticModel) As Boolean
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overrides Function TryGetSpeculativeSemanticModelCore(parentModel As SyntaxTreeSemanticModel, position As Integer, statement As ExecutableStatementSyntax, <Out> ByRef speculativeModel As PublicSemanticModel) As Boolean
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overrides Function TryGetSpeculativeSemanticModelCore(parentModel As SyntaxTreeSemanticModel, position As Integer, initializer As EqualsValueSyntax, <Out> ByRef speculativeModel As PublicSemanticModel) As Boolean
            Throw ExceptionUtilities.Unreachable
        End Function
    End Class
End Namespace
