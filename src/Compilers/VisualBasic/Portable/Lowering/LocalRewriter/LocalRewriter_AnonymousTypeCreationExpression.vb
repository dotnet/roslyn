' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind


Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter

        Public Overrides Function VisitAnonymousTypeCreationExpression(node As BoundAnonymousTypeCreationExpression) As BoundNode
            ' Rewrite anonymous type creation expression into ObjectCreationExpression

            Dim fieldsCount As Integer = node.Arguments.Length
            Debug.Assert(fieldsCount > 0)

            Dim newArguments(fieldsCount - 1) As BoundExpression

            ' Those are lazily created for each field using a local
            Dim locals As ArrayBuilder(Of LocalSymbol) = Nothing

            For index = 0 To fieldsCount - 1
                ' rewrite argument
                newArguments(index) = node.Arguments(index)

                ' if there a local symbol is being used, create assignment
                Dim local As LocalSymbol = If(node.BinderOpt IsNot Nothing,
                                              node.BinderOpt.GetAnonymousTypePropertyLocal(index),
                                              Nothing)
                If local IsNot Nothing Then

                    If locals Is Nothing Then
                        locals = ArrayBuilder(Of LocalSymbol).GetInstance()
                    End If

                    locals.Add(local)

                    Dim boundLocal = New BoundLocal(newArguments(index).Syntax,
                                                    local, True, local.Type)

                    ' replace the argument with assignment expression
                    newArguments(index) = New BoundAssignmentOperator(newArguments(index).Syntax, boundLocal, newArguments(index), True, local.Type)
                End If
                newArguments(index) = VisitExpression(newArguments(index))

            Next

            Dim result As BoundExpression = New BoundObjectCreationExpression(
                                                        node.Syntax,
                                                        DirectCast(node.Type, NamedTypeSymbol).InstanceConstructors(0),
                                                        newArguments.AsImmutableOrNull(),
                                                        Nothing,
                                                        node.Type)
            If locals IsNot Nothing Then
                result = New BoundSequence(
                                node.Syntax,
                                locals.ToImmutableAndFree(),
                                ImmutableArray(Of BoundExpression).Empty,
                                result,
                                node.Type)

            End If

            Return result
        End Function

        Public Overrides Function VisitAnonymousTypePropertyAccess(node As BoundAnonymousTypePropertyAccess) As BoundNode
            ' rewrite anonymous type property access into a bound local

            Dim local As LocalSymbol = node.Binder.GetAnonymousTypePropertyLocal(node.PropertyIndex)

            ' NOTE: if anonymous type property access is to be rewritten, the local 
            '       must be present; see comments on bound node declaration
            Debug.Assert(local IsNot Nothing)

            Return New BoundLocal(node.Syntax, local, False, Me.VisitType(local.Type))
        End Function

        Public Overrides Function VisitAnonymousTypeFieldInitializer(node As BoundAnonymousTypeFieldInitializer) As BoundNode
            Return Visit(node.Value)
        End Function

    End Class
End Namespace
