' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Friend Module VisualBasicInferredMemberNameSimplifier

        Friend Function CanSimplifyTupleName(node As SimpleArgumentSyntax, parseOptions As VisualBasicParseOptions) As Boolean
            ' Tuple elements are arguments in a tuple expression
            If node.NameColonEquals Is Nothing OrElse Not node.IsParentKind(SyntaxKind.TupleExpression) Then
                Return False
            End If

            If parseOptions.LanguageVersion < LanguageVersion.VisualBasic15_3 Then
                Return False
            End If

            If RemovalCausesAmbiguity(DirectCast(node.Parent, TupleExpressionSyntax).Arguments, node) Then
                Return False
            End If

            Dim inferredName = node.Expression.TryGetInferredMemberName()
            If inferredName Is Nothing OrElse
                Not CaseInsensitiveComparison.Equals(inferredName, node.NameColonEquals.Name.Identifier.ValueText) Then

                Return False
            End If

            Return True
        End Function

        Friend Function CanSimplifyNamedFieldInitializer(node As NamedFieldInitializerSyntax) As Boolean
            Dim parentMemberInitializer As ObjectMemberInitializerSyntax = DirectCast(node.Parent, ObjectMemberInitializerSyntax)

            ' Spec requires explicit names for object creation expressions (unlike anonymous objects and tuples which can infer them)
            Dim requiresExplicitNames = parentMemberInitializer.IsParentKind(SyntaxKind.ObjectCreationExpression)
            If requiresExplicitNames OrElse RemovalCausesAmbiguity(parentMemberInitializer.Initializers, node) Then
                Return False
            End If

            Dim inferredName = node.Expression.TryGetInferredMemberName()
            If inferredName Is Nothing OrElse
                    Not CaseInsensitiveComparison.Equals(inferredName, node.Name.Identifier.ValueText) Then
                Return False
            End If

            Return True
        End Function

        ' An explicit name cannot be removed if some other position would produce it as inferred name
        Private Function RemovalCausesAmbiguity(arguments As SeparatedSyntaxList(Of SimpleArgumentSyntax), toRemove As SimpleArgumentSyntax) As Boolean
            Dim name = toRemove.NameColonEquals.Name.Identifier.ValueText
            For Each argument In arguments

                If argument Is toRemove Then
                    Continue For
                End If

                If argument.NameColonEquals Is Nothing AndAlso CaseInsensitiveComparison.Equals(argument.Expression.TryGetInferredMemberName(), name) Then
                    Return True
                End If
            Next

            Return False
        End Function

        ' An explicit name cannot be removed if some other position would produce it as inferred name
        Private Function RemovalCausesAmbiguity(initializers As SeparatedSyntaxList(Of FieldInitializerSyntax), toRemove As NamedFieldInitializerSyntax) As Boolean
            Dim name = toRemove.Name.Identifier.ValueText
            For Each initializer In initializers

                If initializer Is toRemove Then
                    Continue For
                End If

                Dim inferredInitializer = TryCast(initializer, InferredFieldInitializerSyntax)
                If inferredInitializer IsNot Nothing AndAlso CaseInsensitiveComparison.Equals(inferredInitializer.Expression.TryGetInferredMemberName(), name) Then
                    Return True
                End If
            Next

            Return False
        End Function
    End Module
End Namespace
