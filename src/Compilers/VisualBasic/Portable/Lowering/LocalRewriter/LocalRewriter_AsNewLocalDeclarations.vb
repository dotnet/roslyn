' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitAsNewLocalDeclarations(node As BoundAsNewLocalDeclarations) As BoundNode
            Dim builder = ArrayBuilder(Of BoundStatement).GetInstance()

            Dim localDeclarations = node.LocalDeclarations
            For declarationIndex = 0 To localDeclarations.Length - 1
                Dim localDeclaration = localDeclarations(declarationIndex)
                Debug.Assert(localDeclaration.InitializerOpt Is Nothing)

                Dim rewrittenInitializer As BoundNode

                Dim localSymbol = localDeclaration.LocalSymbol
                Dim staticLocalBackingFields As KeyValuePair(Of SynthesizedStaticLocalBackingField, SynthesizedStaticLocalBackingField) = Nothing

                If localSymbol.IsStatic Then
                    staticLocalBackingFields = CreateBackingFieldsForStaticLocal(localSymbol, hasInitializer:=True)
                End If

                Dim initializerToRewrite As BoundExpression

                If declarationIndex = 0 Then
                    initializerToRewrite = node.Initializer
                Else
                    ' For all variables except the first one we rebind the initializer
                    ' and throw away diagnostics, those are supposed to be reported in the first
                    ' binding and don't need to be duplicated as Dev10/11 does
                    '
                    ' Note that we have to rebind the initializers because current implementation of lambda 
                    ' rewriter does not handle correctly blocks and locals which are reused in bound tree. 
                    ' Actually it seems to heavily rely on an assumption that the bound tree is a tree, not 
                    ' a DAG, with just minor deviations. Thus, the natural way of just rewriting bound 
                    ' initializer stored in node.Initializer simply does not work because rewriting *may* 
                    ' keep many blocks and locals unchanged and reuse them in all rewritten initializers, 
                    ' in which case if we have a lambda inside the initializer lambda rewriter simply throws.
                    '
                    ' Another option to satisfy lambda rewriter would be to deep-clone bound tree, but 
                    ' in this case we will also have to clone all locals and all references to locals.
                    ' We might want to look into this option later.

                    Debug.Assert(node.Syntax IsNot Nothing)
                    Debug.Assert(node.Syntax.Kind = SyntaxKind.VariableDeclarator)

                    Dim varDecl = DirectCast(node.Syntax, VariableDeclaratorSyntax)
                    Dim asNew = DirectCast(varDecl.AsClause, AsNewClauseSyntax)

                    ' Rebind and discard diagnostics
                    initializerToRewrite = node.Binder.BindVariableDeclaration(varDecl, varDecl.Names(declarationIndex), asNew, Nothing, BindingDiagnosticBag.Discarded, skipAsNewInitializer:=False).InitializerOpt
                End If

                ' The initializer expression may contain placeholder values and typically they are replaced with bound locals that
                ' get created in the local rewriter when rewriting the expression. 
                ' 
                ' There is one case where the placeholder does not get replaced by a bound temporary and needs to be replaced by
                ' the currently declared local. This is the case for an object creation expression with a object initializer if it's
                ' used in an AsNew declaration and the type is a value type, e.g.:
                ' Dim x As New ValType() With {...} 
                '     or
                ' Dim x, y As New ValType() With {...} 
                ' Because only this method knows the bound local that the placeholder will be replaced with, we need to fill 
                ' the replacement map here, before rewriting the local declaration (there is a special case, because the first 
                ' example will be bound to a BoundLocalDeclaration, see LocalRewriter.VisitLocalDeclaration for the special case).
                '
                Dim objectInitializer As BoundObjectInitializerExpression = GetBoundObjectInitializerFromInitializer(initializerToRewrite)

                ' rewrite the initializer for each declared variable because the initializer may contain placeholders that need
                ' to be replaced with the current bound local

                If objectInitializer IsNot Nothing Then
                    Debug.Assert(objectInitializer.PlaceholderOpt IsNot Nothing)

                    Dim placeholder As BoundWithLValueExpressionPlaceholder = objectInitializer.PlaceholderOpt

                    If Not objectInitializer.CreateTemporaryLocalForInitialization Then
                        AddPlaceholderReplacement(placeholder,
                                                  VisitExpressionNode(New BoundLocal(localDeclaration.Syntax,
                                                                      localSymbol,
                                                                      localSymbol.Type)))
                    End If

                    rewrittenInitializer = Me.VisitAndGenerateObjectCloneIfNeeded(initializerToRewrite)

                    If Not objectInitializer.CreateTemporaryLocalForInitialization Then
                        RemovePlaceholderReplacement(placeholder)
                    End If

                Else
                    rewrittenInitializer = Me.VisitAndGenerateObjectCloneIfNeeded(initializerToRewrite)
                End If

                Dim initialization = RewriteLocalDeclarationAsInitializer(
                    localDeclaration,
                    DirectCast(rewrittenInitializer, BoundExpression),
                    staticLocalBackingFields,
                    objectInitializer Is Nothing OrElse objectInitializer.CreateTemporaryLocalForInitialization)

                builder.Add(initialization)
            Next

            Return New BoundStatementList(node.Syntax, builder.ToImmutableAndFree())
        End Function

        Private Shared Function GetBoundObjectInitializerFromInitializer(initializer As BoundExpression) As BoundObjectInitializerExpression
            If initializer IsNot Nothing AndAlso (initializer.Kind = BoundKind.ObjectCreationExpression OrElse initializer.Kind = BoundKind.NewT) Then
                Dim objectCreationExpression = DirectCast(initializer, BoundObjectCreationExpressionBase)
                Return TryCast(objectCreationExpression.InitializerOpt, BoundObjectInitializerExpression)
            End If
            Return Nothing
        End Function

    End Class
End Namespace
