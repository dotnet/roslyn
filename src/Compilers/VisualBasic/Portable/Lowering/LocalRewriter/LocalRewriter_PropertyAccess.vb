' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitPropertyAccess(node As BoundPropertyAccess) As BoundNode
            ' Rewrite property access into call to getter.
            Debug.Assert(node.AccessKind = PropertyAccessKind.Get)

            Dim receiverOpt = node.ReceiverOpt

            ' check for System.Array.[Length|LongLength] on a single dimensional array,
            ' we have a special node for such cases.
            If receiverOpt IsNot Nothing AndAlso receiverOpt.Type.IsArrayType Then
                Dim asArrayType = DirectCast(receiverOpt.Type, ArrayTypeSymbol)
                If asArrayType.IsSZArray Then
                    ' NOTE: we are not interested in potential badness of Array.Length property.
                    ' If it is bad reference compare will not succeed.
                    If (node.PropertySymbol Is GetSpecialTypeMember(SpecialMember.System_Array__Length) OrElse
                        node.PropertySymbol Is GetSpecialTypeMember(SpecialMember.System_Array__LongLength)) Then

                        Return New BoundArrayLength(node.Syntax, VisitExpressionNode(receiverOpt), node.Type)
                    End If
                End If
            End If

            Dim [property] = node.PropertySymbol
            Dim isMyClassOrMyBase As Boolean = receiverOpt IsNot Nothing AndAlso (receiverOpt.IsMyClassReference OrElse receiverOpt.IsMyBaseReference)
            If _inExpressionLambda AndAlso
                [property].ParameterCount = 0 AndAlso
                [property].ReducedFrom Is Nothing AndAlso
                Not isMyClassOrMyBase Then
                ' All parameterless properties (not accesses via MyBase/MyClass)
                ' should be kept this way to be rewritten in ExpressionLambdaRewriter
                Return MyBase.VisitPropertyAccess(node)
            End If

            Dim getMethod = [property].GetMostDerivedGetMethod()
            Debug.Assert(getMethod IsNot Nothing)
            'EDMAURER the following assert assumes that the overriding property successfully
            'overrode its base. That may not be the case if the declarations are in error.
            'Today (10/6/2011) method bodies are lowered when there are declaration errors
            'when GetDiagnostics() is called.
            'Debug.Assert(Not getMethod.IsOverrides)

            Return RewriteReceiverArgumentsAndGenerateAccessorCall(node.Syntax,
                                                           getMethod,
                                                           receiverOpt,
                                                           node.Arguments,
                                                           node.ConstantValueOpt,
                                                           False,
                                                           getMethod.ReturnType)
        End Function

    End Class
End Namespace
