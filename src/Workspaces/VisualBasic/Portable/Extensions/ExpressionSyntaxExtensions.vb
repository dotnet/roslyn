' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Rename.ConflictEngine
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module ExpressionSyntaxExtensions

        Public ReadOnly typeNameFormatWithGenerics As New SymbolDisplayFormat(
                globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Included,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions:=SymbolDisplayMemberOptions.IncludeContainingType,
                localOptions:=SymbolDisplayLocalOptions.IncludeType,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers,
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces)

        Public ReadOnly typeNameFormatWithoutGenerics As New SymbolDisplayFormat(
                globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Included,
                genericsOptions:=SymbolDisplayGenericsOptions.None,
                memberOptions:=SymbolDisplayMemberOptions.IncludeContainingType,
                localOptions:=SymbolDisplayLocalOptions.IncludeType,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers,
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces)

        <Extension()>
        Public Function WalkUpParentheses(expression As ExpressionSyntax) As ExpressionSyntax
            While expression.IsParentKind(SyntaxKind.ParenthesizedExpression)
                expression = DirectCast(expression.Parent, ExpressionSyntax)
            End While

            Return expression
        End Function

        <Extension()>
        Public Function WalkDownParentheses(expression As ExpressionSyntax) As ExpressionSyntax
            While expression.IsKind(SyntaxKind.ParenthesizedExpression)
                expression = DirectCast(expression, ParenthesizedExpressionSyntax).Expression
            End While

            Return expression
        End Function

        <Extension()>
        Public Function Parenthesize(expression As ExpressionSyntax) As ParenthesizedExpressionSyntax
            Return SyntaxFactory.ParenthesizedExpression(expression.WithoutTrivia()) _
                                .WithTriviaFrom(expression) _
                                .WithAdditionalAnnotations(Simplifier.Annotation)
        End Function


        <Extension()>
        Public Function IsAliasReplaceableExpression(expression As ExpressionSyntax) As Boolean
            If expression.Kind = SyntaxKind.IdentifierName OrElse
               expression.Kind = SyntaxKind.QualifiedName Then
                Return True
            End If

            If expression.Kind = SyntaxKind.SimpleMemberAccessExpression Then
                Dim memberAccess = DirectCast(expression, MemberAccessExpressionSyntax)

                Return memberAccess.Expression IsNot Nothing AndAlso memberAccess.Expression.IsAliasReplaceableExpression()
            End If

            Return False
        End Function

        <Extension()>
        Public Function IsMemberAccessExpressionName(expression As ExpressionSyntax) As Boolean
            Return expression.IsParentKind(SyntaxKind.SimpleMemberAccessExpression) AndAlso
                   DirectCast(expression.Parent, MemberAccessExpressionSyntax).Name Is expression
        End Function

        <Extension()>
        Public Function IsAnyMemberAccessExpressionName(expression As ExpressionSyntax) As Boolean
            Return expression IsNot Nothing AndAlso
                   TypeOf expression.Parent Is MemberAccessExpressionSyntax AndAlso
                   DirectCast(expression.Parent, MemberAccessExpressionSyntax).Name Is expression
        End Function

        <Extension()>
        Public Function IsRightSideOfDotOrBang(expression As ExpressionSyntax) As Boolean
            Return expression.IsAnyMemberAccessExpressionName() OrElse expression.IsRightSideOfQualifiedName()
        End Function

        <Extension()>
        Public Function IsRightSideOfDot(expression As ExpressionSyntax) As Boolean
            Return expression.IsMemberAccessExpressionName() OrElse expression.IsRightSideOfQualifiedName()
        End Function

        <Extension()>
        Public Function IsRightSideOfQualifiedName(expression As ExpressionSyntax) As Boolean
            Return expression.IsParentKind(SyntaxKind.QualifiedName) AndAlso
                   DirectCast(expression.Parent, QualifiedNameSyntax).Right Is expression
        End Function

        <Extension()>
        Public Function IsLeftSideOfQualifiedName(expression As ExpressionSyntax) As Boolean
            Return expression.IsParentKind(SyntaxKind.QualifiedName) AndAlso
                   DirectCast(expression.Parent, QualifiedNameSyntax).Left Is expression
        End Function

        <Extension()>
        Public Function IsAnyLiteralExpression(expression As ExpressionSyntax) As Boolean
            Return expression.IsKind(SyntaxKind.CharacterLiteralExpression) OrElse
                expression.IsKind(SyntaxKind.DateLiteralExpression) OrElse
                expression.IsKind(SyntaxKind.FalseLiteralExpression) OrElse
                expression.IsKind(SyntaxKind.NothingLiteralExpression) OrElse
                expression.IsKind(SyntaxKind.NumericLiteralExpression) OrElse
                expression.IsKind(SyntaxKind.StringLiteralExpression) OrElse
                expression.IsKind(SyntaxKind.TrueLiteralExpression)
        End Function

        ''' <summary>
        ''' Decompose a name or member access expression into its component parts.
        ''' </summary>
        ''' <param name="expression">The name or member access expression.</param>
        ''' <param name="qualifier">The qualifier (or left-hand-side) of the name expression. This may be null if there is no qualifier.</param>
        ''' <param name="name">The name of the expression.</param>
        ''' <param name="arity">The number of generic type parameters.</param>
        <Extension()>
        Public Sub DecomposeName(expression As ExpressionSyntax, ByRef qualifier As ExpressionSyntax, ByRef name As String, ByRef arity As Integer)
            Select Case expression.Kind
                Case SyntaxKind.SimpleMemberAccessExpression
                    Dim memberAccess = DirectCast(expression, MemberAccessExpressionSyntax)
                    qualifier = memberAccess.Expression
                    name = memberAccess.Name.Identifier.ValueText
                    arity = memberAccess.Name.Arity
                Case SyntaxKind.QualifiedName
                    Dim qualifiedName = DirectCast(expression, QualifiedNameSyntax)
                    qualifier = qualifiedName.Left
                    name = qualifiedName.Right.Identifier.ValueText
                    arity = qualifiedName.Arity
                Case SyntaxKind.GenericName
                    Dim genericName = DirectCast(expression, GenericNameSyntax)
                    qualifier = Nothing
                    name = genericName.Identifier.ValueText
                    arity = genericName.Arity
                Case SyntaxKind.IdentifierName
                    Dim identifierName = DirectCast(expression, IdentifierNameSyntax)
                    qualifier = Nothing
                    name = identifierName.Identifier.ValueText
                    arity = 0
                Case Else
                    qualifier = Nothing
                    name = Nothing
                    arity = 0
            End Select
        End Sub

        <Extension()>
        Public Function TryGetNameParts(expression As ExpressionSyntax, ByRef parts As IList(Of String)) As Boolean
            Dim partsList = New List(Of String)
            If Not expression.TryGetNameParts(partsList) Then
                parts = Nothing
                Return False
            End If

            parts = partsList
            Return True
        End Function

        <Extension()>
        Public Function TryGetNameParts(expression As ExpressionSyntax, parts As List(Of String)) As Boolean
            If expression.IsKind(SyntaxKind.SimpleMemberAccessExpression) Then
                Dim memberAccess = DirectCast(expression, MemberAccessExpressionSyntax)
                If Not memberAccess.Name.TryGetNameParts(parts) Then
                    Return False
                End If

                Return AddSimpleName(memberAccess.Name, parts)
            ElseIf expression.IsKind(SyntaxKind.QualifiedName) Then
                Dim qualifiedName = DirectCast(expression, QualifiedNameSyntax)
                If Not qualifiedName.Left.TryGetNameParts(parts) Then
                    Return False
                End If

                Return AddSimpleName(qualifiedName.Right, parts)
            ElseIf TypeOf expression Is SimpleNameSyntax Then
                Return AddSimpleName(DirectCast(expression, SimpleNameSyntax), parts)
            Else
                Return False
            End If
        End Function

        Private Function AddSimpleName(simpleName As SimpleNameSyntax, parts As List(Of String)) As Boolean
            If Not simpleName.IsKind(SyntaxKind.IdentifierName) Then
                Return False
            End If

            parts.Add(simpleName.Identifier.ValueText)
            Return True
        End Function

        <Extension()>
        Public Function IsLeftSideOfDot(expression As ExpressionSyntax) As Boolean
            Return _
                (expression.IsParentKind(SyntaxKind.QualifiedName) AndAlso DirectCast(expression.Parent, QualifiedNameSyntax).Left Is expression) OrElse
                (expression.IsParentKind(SyntaxKind.SimpleMemberAccessExpression) AndAlso DirectCast(expression.Parent, MemberAccessExpressionSyntax).Expression Is expression)
        End Function

        <Extension()>
        Public Function Cast(
            expression As ExpressionSyntax,
            targetType As ITypeSymbol,
            <Out> ByRef isResultPredefinedCast As Boolean) As ExpressionSyntax

            ' Parenthesize the expression, except for collection initializers and interpolated strings,
            ' where parenthesizing changes semantics.
            Dim newExpression = expression

            If Not expression.IsKind(SyntaxKind.CollectionInitializer, SyntaxKind.InterpolatedStringExpression) Then
                newExpression = expression.Parenthesize()
            End If

            Dim leadingTrivia = newExpression.GetLeadingTrivia()
            Dim trailingTrivia = newExpression.GetTrailingTrivia()

            Dim stripped = newExpression.WithoutLeadingTrivia().WithoutTrailingTrivia()

            Dim castKeyword = targetType.SpecialType.GetPredefinedCastKeyword()
            If castKeyword = SyntaxKind.None Then
                isResultPredefinedCast = False
                Return SyntaxFactory.CTypeExpression(
                    expression:=stripped,
                    type:=targetType.GenerateTypeSyntax()) _
                .WithLeadingTrivia(leadingTrivia) _
                .WithTrailingTrivia(trailingTrivia) _
                .WithAdditionalAnnotations(Simplifier.Annotation)
            Else
                isResultPredefinedCast = True
                Return SyntaxFactory.PredefinedCastExpression(
                    keyword:=SyntaxFactory.Token(castKeyword),
                    expression:=stripped) _
                .WithLeadingTrivia(leadingTrivia) _
                .WithTrailingTrivia(trailingTrivia) _
                .WithAdditionalAnnotations(Simplifier.Annotation)
            End If
        End Function

        <Extension()>
        Public Function CastIfPossible(
            expression As ExpressionSyntax,
            targetType As ITypeSymbol,
            position As Integer,
            semanticModel As SemanticModel,
            <Out> ByRef wasCastAdded As Boolean
        ) As ExpressionSyntax
            wasCastAdded = False

            If targetType.ContainsAnonymousType() OrElse expression.IsParentKind(SyntaxKind.AsNewClause) Then
                Return expression
            End If

            Dim typeSyntax = targetType.GenerateTypeSyntax()
            Dim type = semanticModel.GetSpeculativeTypeInfo(
                position,
                typeSyntax,
                SpeculativeBindingOption.BindAsTypeOrNamespace).Type

            If Not targetType.Equals(type) Then
                Return expression
            End If

            Dim isResultPredefinedCast As Boolean = False
            Dim castExpression = expression.Cast(targetType, isResultPredefinedCast)

            ' Ensure that inserting the cast doesn't change the semantics.
            Dim specAnalyzer = New SpeculationAnalyzer(expression, castExpression, semanticModel, CancellationToken.None)
            Dim speculativeSemanticModel = specAnalyzer.SpeculativeSemanticModel
            If speculativeSemanticModel Is Nothing Then
                Return expression
            End If

            Dim speculatedCastExpression = specAnalyzer.ReplacedExpression
            Dim speculatedCastInnerExpression = If(isResultPredefinedCast,
                                                   DirectCast(speculatedCastExpression, PredefinedCastExpressionSyntax).Expression,
                                                   DirectCast(speculatedCastExpression, CastExpressionSyntax).Expression)
            If Not CastAnalyzer.IsUnnecessary(speculatedCastExpression, speculatedCastInnerExpression, speculativeSemanticModel, True, CancellationToken.None) Then
                Return expression
            End If

            wasCastAdded = True
            Return castExpression
        End Function

        <Extension()>
        Public Function IsNewOnRightSideOfDotOrBang(expression As ExpressionSyntax) As Boolean
            Dim identifierName = TryCast(expression, IdentifierNameSyntax)
            If identifierName Is Nothing Then
                Return False
            End If

            If String.Compare(identifierName.Identifier.ToString(), "New", StringComparison.OrdinalIgnoreCase) <> 0 Then
                Return False
            End If

            Return identifierName.IsRightSideOfDotOrBang()
        End Function

        <Extension()>
        Public Function IsObjectCreationWithoutArgumentList(expression As ExpressionSyntax) As Boolean
            Return _
                TypeOf expression Is ObjectCreationExpressionSyntax AndAlso
                DirectCast(expression, ObjectCreationExpressionSyntax).ArgumentList Is Nothing
        End Function

        <Extension()>
        Public Function IsInOutContext(expression As ExpressionSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As Boolean
            ' NOTE(cyrusn): VB has no concept of an out context.  Even when a parameter has an
            ' '<Out>' attribute on it, it's still treated as ref by VB.  So we always return false
            ' here.
            Return False
        End Function

        <Extension()>
        Public Function IsInRefContext(expression As ExpressionSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As Boolean
            Dim simpleArgument = TryCast(expression.Parent, SimpleArgumentSyntax)

            If simpleArgument Is Nothing Then
                Return False
            ElseIf simpleArgument.IsNamed Then
                Dim info = semanticModel.GetSymbolInfo(simpleArgument.NameColonEquals.Name, cancellationToken)

                Dim parameter = TryCast(info.GetAnySymbol(), IParameterSymbol)
                Return parameter IsNot Nothing AndAlso parameter.RefKind <> RefKind.None

            Else
                Dim argumentList = TryCast(simpleArgument.Parent, ArgumentListSyntax)

                If argumentList IsNot Nothing Then
                    Dim parent = argumentList.Parent
                    Dim index = argumentList.Arguments.IndexOf(simpleArgument)

                    Dim info = semanticModel.GetSymbolInfo(parent, cancellationToken)
                    Dim symbol = info.GetAnySymbol()

                    If TypeOf symbol Is IMethodSymbol Then
                        Dim method = DirectCast(symbol, IMethodSymbol)
                        If index < method.Parameters.Length Then
                            Return method.Parameters(index).RefKind <> RefKind.None
                        End If
                    ElseIf TypeOf symbol Is IPropertySymbol Then
                        Dim prop = DirectCast(symbol, IPropertySymbol)
                        If index < prop.Parameters.Length Then
                            Return prop.Parameters(index).RefKind <> RefKind.None
                        End If
                    End If
                End If

            End If

            Return False
        End Function

        <Extension()>
        Public Function IsOnlyWrittenTo(expression As ExpressionSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As Boolean
            If expression.IsRightSideOfDot() Then
                expression = TryCast(expression.Parent, ExpressionSyntax)
            End If

            If expression IsNot Nothing Then
                If expression.IsInOutContext(semanticModel, cancellationToken) Then
                    Return True
                End If

                If expression.IsParentKind(SyntaxKind.SimpleAssignmentStatement) Then
                    Dim assignmentStatement = DirectCast(expression.Parent, AssignmentStatementSyntax)
                    If expression Is assignmentStatement.Left Then
                        Return True
                    End If
                End If

                If expression.IsChildNode(Of NamedFieldInitializerSyntax)(Function(n) n.Name) Then
                    Return True
                End If

                Return False
            End If

            Return False
        End Function

        <Extension()>
        Public Function IsWrittenTo(expression As ExpressionSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As Boolean
            If IsOnlyWrittenTo(expression, semanticModel, cancellationToken) Then
                Return True
            End If

            If expression.IsRightSideOfDot() Then
                expression = TryCast(expression.Parent, ExpressionSyntax)
            End If

            If expression IsNot Nothing Then
                If expression.IsInRefContext(semanticModel, cancellationToken) Then
                    Return True
                End If

                If TypeOf expression.Parent Is AssignmentStatementSyntax Then
                    Dim assignmentStatement = DirectCast(expression.Parent, AssignmentStatementSyntax)
                    If expression Is assignmentStatement.Left Then
                        Return True
                    End If
                End If

                If expression.IsChildNode(Of NamedFieldInitializerSyntax)(Function(n) n.Name) Then
                    Return True
                End If

                Return False
            End If

            Return False
        End Function

        <Extension()>
        Public Function IsMeMyBaseOrMyClass(expression As ExpressionSyntax) As Boolean
            If expression Is Nothing Then
                Return False
            End If

            Return expression.Kind = SyntaxKind.MeExpression OrElse
                   expression.Kind = SyntaxKind.MyBaseExpression OrElse
                   expression.Kind = SyntaxKind.MyClassExpression
        End Function

        <Extension()>
        Public Function IsFirstStatementInCtor(expression As ExpressionSyntax) As Boolean
            Dim statement = expression.FirstAncestorOrSelf(Of StatementSyntax)()
            If statement Is Nothing Then
                Return False
            End If

            If Not statement.IsParentKind(SyntaxKind.ConstructorBlock) Then
                Return False
            End If

            Return DirectCast(statement.Parent, ConstructorBlockSyntax).Statements(0) Is statement
        End Function

        <Extension()>
        Public Function IsNamedArgumentIdentifier(expression As ExpressionSyntax) As Boolean
            Dim simpleArgument = TryCast(expression.Parent, SimpleArgumentSyntax)
            Return simpleArgument IsNot Nothing AndAlso simpleArgument.NameColonEquals.Name Is expression
        End Function

        Private Function IsUnnecessaryCast(
            castNode As ExpressionSyntax,
            castExpressionNode As ExpressionSyntax,
            semanticModel As SemanticModel,
            assumeCallKeyword As Boolean,
            cancellationToken As CancellationToken
        ) As Boolean

            Return CastAnalyzer.IsUnnecessary(castNode, castExpressionNode, semanticModel, assumeCallKeyword, cancellationToken)
        End Function

        <Extension>
        Public Function IsUnnecessaryCast(
            node As CastExpressionSyntax,
            semanticModel As SemanticModel,
            cancellationToken As CancellationToken,
            Optional assumeCallKeyword As Boolean = False
        ) As Boolean

            Return IsUnnecessaryCast(node, node.Expression, semanticModel, assumeCallKeyword, cancellationToken)
        End Function

        <Extension>
        Public Function IsUnnecessaryCast(
            node As PredefinedCastExpressionSyntax,
            semanticModel As SemanticModel,
            cancellationToken As CancellationToken,
            Optional assumeCallKeyword As Boolean = False
        ) As Boolean

            Return IsUnnecessaryCast(node, node.Expression, semanticModel, assumeCallKeyword, cancellationToken)
        End Function

        Private Function CanReplace(symbol As ISymbol) As Boolean
            Select Case symbol.Kind
                Case SymbolKind.Field,
                     SymbolKind.Local,
                     SymbolKind.Method,
                     SymbolKind.Parameter,
                     SymbolKind.Property,
                     SymbolKind.RangeVariable
                    Return True
            End Select

            Return False
        End Function

        <Extension>
        Public Function CanReplaceWithRValue(expression As ExpressionSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As Boolean
            Return expression IsNot Nothing AndAlso
                Not expression.IsWrittenTo(semanticModel, cancellationToken) AndAlso
                expression.CanReplaceWithLValue(semanticModel, cancellationToken)
        End Function

        <Extension>
        Public Function CanReplaceWithLValue(expression As ExpressionSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As Boolean
#If False Then
            ' Things that are definitely illegal to replace
            If ContainsImplicitMemberAccess(expression) Then
                Return False
            End If
#End If

            If expression.IsKind(SyntaxKind.MyBaseExpression) OrElse
               expression.IsKind(SyntaxKind.MyClassExpression) Then
                Return False
            End If

            If Not (TypeOf expression Is ObjectCreationExpressionSyntax) AndAlso
                   Not (TypeOf expression Is AnonymousObjectCreationExpressionSyntax) Then
                Dim symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken)
                If Not symbolInfo.GetBestOrAllSymbols().All(AddressOf CanReplace) Then
                    ' If the expression is actually a reference to a type, then it can't be replaced
                    ' with an arbitrary expression.
                    Return False
                End If
            End If

            ' Technically, you could introduce an LValue for "Foo" in "Foo()" even if "Foo" binds
            ' to a method.  (i.e. by assigning to a Func<...> type).  However, this is so contrived
            ' and none of the features that use this extension consider this replaceable.
            If TypeOf expression.Parent Is InvocationExpressionSyntax Then

                ' If something is being invoked, then it's either something like Foo(), Foo.Bar(), or
                ' SomeExpr() (i.e. Blah[1]()).  In the first and second case, we only allow
                ' replacement if Foo and Foo.Bar didn't bind to a method.  If we can't bind it, we'll
                ' assume it's a method and we don't allow it to be replaced either.  However, if it's
                ' an arbitrary expression, we do allow replacement.
                If expression.IsKind(SyntaxKind.IdentifierName) OrElse expression.IsKind(SyntaxKind.SimpleMemberAccessExpression) Then
                    Dim symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken)
                    If Not symbolInfo.GetBestOrAllSymbols().Any() Then
                        Return False
                    End If

                    ' don't allow it to be replaced if it is bound to an indexed property
                    Return Not symbolInfo.GetBestOrAllSymbols().OfType(Of IMethodSymbol)().Any() AndAlso
                           Not symbolInfo.GetBestOrAllSymbols().OfType(Of IPropertySymbol)().Any()
                Else
                    Return True
                End If
            End If

            ' expression in next statement's control variables should match one in the head
            Dim nextStatement = expression.FirstAncestorOrSelf(Of NextStatementSyntax)()
            If nextStatement IsNot Nothing Then
                Return False
            End If

            ' Direct parent kind checks.
            If expression.IsParentKind(SyntaxKind.EqualsValue) OrElse
               expression.IsParentKind(SyntaxKind.ParenthesizedExpression) OrElse
               expression.IsParentKind(SyntaxKind.SelectStatement) OrElse
               expression.IsParentKind(SyntaxKind.SyncLockStatement) OrElse
               expression.IsParentKind(SyntaxKind.CollectionInitializer) OrElse
               expression.IsParentKind(SyntaxKind.InferredFieldInitializer) OrElse
               expression.IsParentKind(SyntaxKind.BinaryConditionalExpression) OrElse
               expression.IsParentKind(SyntaxKind.TernaryConditionalExpression) OrElse
               expression.IsParentKind(SyntaxKind.ReturnStatement) OrElse
               expression.IsParentKind(SyntaxKind.YieldStatement) OrElse
               expression.IsParentKind(SyntaxKind.XmlEmbeddedExpression) OrElse
               expression.IsParentKind(SyntaxKind.ThrowStatement) OrElse
               expression.IsParentKind(SyntaxKind.IfStatement) OrElse
               expression.IsParentKind(SyntaxKind.WhileStatement) OrElse
               expression.IsParentKind(SyntaxKind.ElseIfStatement) OrElse
               expression.IsParentKind(SyntaxKind.ForEachStatement) OrElse
               expression.IsParentKind(SyntaxKind.ForStatement) OrElse
               expression.IsParentKind(SyntaxKind.ConditionalAccessExpression) OrElse
               expression.IsParentKind(SyntaxKind.TypeOfIsExpression) OrElse
               expression.IsParentKind(SyntaxKind.TypeOfIsNotExpression) Then

                Return True
            End If

            ' Parent type checks
            If TypeOf expression.Parent Is BinaryExpressionSyntax OrElse
               TypeOf expression.Parent Is AssignmentStatementSyntax OrElse
               TypeOf expression.Parent Is WhileOrUntilClauseSyntax OrElse
               TypeOf expression.Parent Is SingleLineLambdaExpressionSyntax OrElse
               TypeOf expression.Parent Is AwaitExpressionSyntax Then
                Return True
            End If

            ' Specific child checks.
            If expression.CheckParent(Of NamedFieldInitializerSyntax)(Function(n) n.Expression Is expression) OrElse
               expression.CheckParent(Of MemberAccessExpressionSyntax)(Function(m) m.Expression Is expression) OrElse
               expression.CheckParent(Of TryCastExpressionSyntax)(Function(t) t.Expression Is expression) OrElse
               expression.CheckParent(Of CatchFilterClauseSyntax)(Function(c) c.Filter Is expression) OrElse
               expression.CheckParent(Of SimpleArgumentSyntax)(Function(n) n.Expression Is expression) OrElse
               expression.CheckParent(Of DirectCastExpressionSyntax)(Function(d) d.Expression Is expression) OrElse
               expression.CheckParent(Of FunctionAggregationSyntax)(Function(f) f.Argument Is expression) OrElse
               expression.CheckParent(Of RangeArgumentSyntax)(Function(r) r.UpperBound Is expression) Then
                Return True
            End If

            ' Misc checks
            If TypeOf expression.Parent Is ExpressionRangeVariableSyntax AndAlso
               TypeOf expression.Parent.Parent Is QueryClauseSyntax Then
                Dim rangeVariable = DirectCast(expression.Parent, ExpressionRangeVariableSyntax)
                Dim selectClause = TryCast(rangeVariable.Parent, SelectClauseSyntax)

                ' Can't replace the expression in a select unless its the last select clause *or*
                ' it's a select of the form "select a = <expr>"
                If selectClause IsNot Nothing Then
                    If rangeVariable.NameEquals IsNot Nothing Then
                        Return True
                    End If

                    Dim queryExpression = TryCast(selectClause.Parent, QueryExpressionSyntax)
                    If queryExpression IsNot Nothing Then
                        Return queryExpression.Clauses.Last() Is selectClause
                    End If

                    Dim aggregateClause = TryCast(selectClause.Parent, AggregateClauseSyntax)
                    If aggregateClause IsNot Nothing Then
                        Return aggregateClause.AdditionalQueryOperators().Last() Is selectClause
                    End If

                    Return False
                End If

                ' Any other query type is ok.  Note(cyrusn): This may be too broad.
                Return True
            End If

            Return False
        End Function

        <Extension>
        Public Function ContainsImplicitMemberAccess(expression As ExpressionSyntax) As Boolean
            Return ContainsImplicitMemberAccessWorker(expression)
        End Function

        <Extension>
        Public Function ContainsImplicitMemberAccess(statement As StatementSyntax) As Boolean
            Return ContainsImplicitMemberAccessWorker(statement)
        End Function

        <Extension>
        Public Function GetImplicitMemberAccessExpressions(expression As SyntaxNode, span As TextSpan) As IEnumerable(Of ExpressionSyntax)
            ' We don't want to allow a variable to be introduced if the expression contains an
            ' implicit member access.  i.e. ".Blah.ToString()" as that .Blah refers to the containing
            ' object creation or anonymous type and we can't make a local for it.  So we get all the
            ' descendants and we suppress ourselves. 

            ' Note: if we hit a with block or an anonymous type, then we do not look deeper.  Any
            ' implicit member accesses will refer to that thing and we *can* introduce a variable
            Dim descendentExpressions = expression.DescendantNodesAndSelf().OfType(Of ExpressionSyntax).Where(Function(e) span.Contains(e.Span)).ToSet()

            Return descendentExpressions.OfType(Of MemberAccessExpressionSyntax).
                                         Select(Function(m) m.GetExpressionOfMemberAccessExpression()).
                                         Where(Function(e) Not descendentExpressions.Contains(e))
        End Function

        <Extension>
        Public Function GetImplicitMemberAccessExpressions(expression As SyntaxNode) As IEnumerable(Of ExpressionSyntax)
            Return GetImplicitMemberAccessExpressions(expression, expression.FullSpan)
        End Function

        Private Function ContainsImplicitMemberAccessWorker(expression As SyntaxNode) As Boolean
            Return GetImplicitMemberAccessExpressions(expression).Any()
        End Function

        <Extension>
        Public Function GetOperatorPrecedence(expression As ExpressionSyntax) As OperatorPrecedence
            Select Case expression.Kind
                Case SyntaxKind.ExponentiateExpression
                    Return OperatorPrecedence.PrecedenceExponentiate
                Case SyntaxKind.UnaryMinusExpression,
                     SyntaxKind.UnaryPlusExpression
                    Return OperatorPrecedence.PrecedenceNegate
                Case SyntaxKind.MultiplyExpression,
                     SyntaxKind.DivideExpression
                    Return OperatorPrecedence.PrecedenceMultiply
                Case SyntaxKind.IntegerDivideExpression
                    Return OperatorPrecedence.PrecedenceIntegerDivide
                Case SyntaxKind.ModuloExpression
                    Return OperatorPrecedence.PrecedenceModulus
                Case SyntaxKind.AddExpression,
                     SyntaxKind.SubtractExpression
                    Return OperatorPrecedence.PrecedenceAdd
                Case SyntaxKind.ConcatenateExpression
                    Return OperatorPrecedence.PrecedenceConcatenate
                Case SyntaxKind.LeftShiftExpression,
                     SyntaxKind.RightShiftExpression
                    Return OperatorPrecedence.PrecedenceShift
                Case SyntaxKind.EqualsExpression,
                     SyntaxKind.NotEqualsExpression,
                     SyntaxKind.LessThanExpression,
                     SyntaxKind.GreaterThanExpression,
                     SyntaxKind.LessThanOrEqualExpression,
                     SyntaxKind.GreaterThanOrEqualExpression,
                     SyntaxKind.LikeExpression,
                     SyntaxKind.IsExpression,
                     SyntaxKind.IsNotExpression
                    Return OperatorPrecedence.PrecedenceRelational
                Case SyntaxKind.NotExpression
                    Return OperatorPrecedence.PrecedenceNot
                Case SyntaxKind.AndExpression,
                     SyntaxKind.AndAlsoExpression
                    Return OperatorPrecedence.PrecedenceAnd
                Case SyntaxKind.OrExpression,
                     SyntaxKind.OrElseExpression
                    Return OperatorPrecedence.PrecedenceOr
                Case SyntaxKind.ExclusiveOrExpression
                    Return OperatorPrecedence.PrecedenceXor
                Case Else
                    Return OperatorPrecedence.PrecedenceNone
            End Select
        End Function

        <Extension()>
        Public Function DetermineType(expression As ExpressionSyntax,
                                      semanticModel As SemanticModel,
                                      cancellationToken As CancellationToken) As ITypeSymbol
            ' If a parameter appears to have a void return type, then just use 'object' instead.
            If expression IsNot Nothing Then
                Dim typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken)
                Dim symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken)
                If typeInfo.Type IsNot Nothing AndAlso typeInfo.Type.SpecialType = SpecialType.System_Void Then
                    Return semanticModel.Compilation.ObjectType
                End If

                Dim symbol = If(typeInfo.Type, symbolInfo.GetAnySymbol())
                If symbol IsNot Nothing Then
                    Return symbol.ConvertToType(semanticModel.Compilation)
                End If

                If TypeOf expression Is CollectionInitializerSyntax Then
                    Dim collectionInitializer = DirectCast(expression, CollectionInitializerSyntax)
                    Return DetermineType(collectionInitializer, semanticModel, cancellationToken)
                End If
            End If

            Return semanticModel.Compilation.ObjectType
        End Function

        <Extension()>
        Private Function DetermineType(collectionInitializer As CollectionInitializerSyntax,
                                      semanticModel As SemanticModel,
                                      cancellationToken As CancellationToken) As ITypeSymbol
            Dim rank = 1
            While collectionInitializer.Initializers.Count > 0 AndAlso
                  collectionInitializer.Initializers(0).Kind = SyntaxKind.CollectionInitializer
                rank += 1
                collectionInitializer = DirectCast(collectionInitializer.Initializers(0), CollectionInitializerSyntax)
            End While

            Dim type = collectionInitializer.Initializers.FirstOrDefault().DetermineType(semanticModel, cancellationToken)
            Return semanticModel.Compilation.CreateArrayTypeSymbol(type, rank)
        End Function

        <Extension()>
        Public Function TryReduceVariableDeclaratorWithoutType(
            variableDeclarator As VariableDeclaratorSyntax,
            semanticModel As SemanticModel,
            <Out()> ByRef replacementNode As SyntaxNode,
            <Out()> ByRef issueSpan As TextSpan,
            optionSet As OptionSet,
            cancellationToken As CancellationToken
            ) As Boolean

            replacementNode = Nothing
            issueSpan = Nothing

            ' Failfast Conditions
            If Not optionSet.GetOption(SimplificationOptions.PreferImplicitTypeInLocalDeclaration) OrElse
                variableDeclarator.AsClause Is Nothing OrElse
                Not variableDeclarator.Parent.IsKind(
                    SyntaxKind.LocalDeclarationStatement,
                    SyntaxKind.UsingStatement,
                    SyntaxKind.ForStatement,
                    SyntaxKind.ForEachStatement,
                    SyntaxKind.FieldDeclaration) Then
                Return False
            End If

            If variableDeclarator.Names.Count <> 1 Then
                Return False
            End If

            Dim parent = variableDeclarator.Parent
            Dim modifiedIdentifier = variableDeclarator.Names.Single()

            Dim simpleAsClause = TryCast(variableDeclarator.AsClause, SimpleAsClauseSyntax)
            If simpleAsClause Is Nothing Then
                Return False
            End If

            If (parent.IsKind(SyntaxKind.LocalDeclarationStatement, SyntaxKind.UsingStatement, SyntaxKind.FieldDeclaration) AndAlso
                variableDeclarator.Initializer IsNot Nothing) Then

                ' Type Check

                Dim declaredSymbolType As ITypeSymbol = Nothing
                If Not HasValidDeclaredTypeSymbol(modifiedIdentifier, semanticModel, declaredSymbolType) Then
                    Return False
                End If

                Dim initializerType As ITypeSymbol = Nothing

                If declaredSymbolType.IsArrayType() AndAlso variableDeclarator.Initializer.Value.Kind() = SyntaxKind.CollectionInitializer Then
                    ' Get type of the array literal in context without the target type
                    initializerType = semanticModel.GetSpeculativeTypeInfo(variableDeclarator.Initializer.Value.SpanStart, variableDeclarator.Initializer.Value, SpeculativeBindingOption.BindAsExpression).ConvertedType
                Else
                    initializerType = semanticModel.GetTypeInfo(variableDeclarator.Initializer.Value).Type
                End If

                If Not declaredSymbolType.Equals(initializerType) Then
                    Return False
                End If

                Dim newModifiedIdentifier = SyntaxFactory.ModifiedIdentifier(modifiedIdentifier.Identifier) ' LeadingTrivia is copied here
                replacementNode = SyntaxFactory.VariableDeclarator(SyntaxFactory.SingletonSeparatedList(newModifiedIdentifier.WithTrailingTrivia(variableDeclarator.AsClause.GetTrailingTrivia())),
                                                                   asClause:=Nothing,
                                                                   initializer:=variableDeclarator.Initializer) 'TrailingTrivia is copied here
                issueSpan = variableDeclarator.Span
                Return True
            End If

            If (parent.IsKind(SyntaxKind.ForEachStatement, SyntaxKind.ForStatement)) Then
                ' Type Check for ForStatement
                If parent.IsKind(SyntaxKind.ForStatement) Then
                    Dim declaredSymbolType As ITypeSymbol = Nothing
                    If Not HasValidDeclaredTypeSymbol(modifiedIdentifier, semanticModel, declaredSymbolType) Then
                        Return False
                    End If

                    Dim valueType = semanticModel.GetTypeInfo(DirectCast(parent, ForStatementSyntax).ToValue).Type

                    If Not valueType.Equals(declaredSymbolType) Then
                        Return False
                    End If
                End If

                If parent.IsKind(SyntaxKind.ForEachStatement) Then
                    Dim forEachStatementInfo = semanticModel.GetForEachStatementInfo(DirectCast(parent, ForEachStatementSyntax))
                    If Not forEachStatementInfo.ElementConversion.IsIdentity Then
                        Return False
                    End If
                End If

                Dim newIdentifierName = SyntaxFactory.IdentifierName(modifiedIdentifier.Identifier) ' Leading Trivia is copied here
                replacementNode = newIdentifierName.WithTrailingTrivia(variableDeclarator.AsClause.GetTrailingTrivia()) ' Trailing Trivia is copied here
                issueSpan = variableDeclarator.Span
                Return True
            End If

            Return False
        End Function

        Private Function HasValidDeclaredTypeSymbol(
            modifiedIdentifier As ModifiedIdentifierSyntax,
            semanticModel As SemanticModel,
            <Out()> ByRef typeSymbol As ITypeSymbol) As Boolean

            Dim declaredSymbol = semanticModel.GetDeclaredSymbol(modifiedIdentifier)
            If declaredSymbol Is Nothing OrElse
               (Not TypeOf declaredSymbol Is ILocalSymbol AndAlso Not TypeOf declaredSymbol Is IFieldSymbol) Then
                Return False
            End If

            Dim localSymbol = TryCast(declaredSymbol, ILocalSymbol)
            If localSymbol IsNot Nothing AndAlso TypeOf localSymbol IsNot IErrorTypeSymbol AndAlso TypeOf localSymbol.Type IsNot IErrorTypeSymbol Then
                typeSymbol = localSymbol.Type
                Return True
            End If

            Dim fieldSymbol = TryCast(declaredSymbol, IFieldSymbol)
            If fieldSymbol IsNot Nothing AndAlso TypeOf fieldSymbol IsNot IErrorTypeSymbol AndAlso TypeOf fieldSymbol.Type IsNot IErrorTypeSymbol Then
                typeSymbol = fieldSymbol.Type
                Return True
            End If

            Return False
        End Function

        <Extension()>
        Public Function TryReduceOrSimplifyExplicitName(
            expression As ExpressionSyntax,
            semanticModel As SemanticModel,
            <Out()> ByRef replacementNode As ExpressionSyntax,
            <Out()> ByRef issueSpan As TextSpan,
            optionSet As OptionSet,
            cancellationToken As CancellationToken
        ) As Boolean
            If expression.TryReduceExplicitName(semanticModel, replacementNode, issueSpan, optionSet, cancellationToken) Then
                Return True
            End If

            Return expression.TrySimplify(semanticModel, optionSet, replacementNode, issueSpan)
        End Function

        <Extension()>
        Public Function TryReduceExplicitName(
            expression As ExpressionSyntax,
            semanticModel As SemanticModel,
            <Out()> ByRef replacementNode As ExpressionSyntax,
            <Out()> ByRef issueSpan As TextSpan,
            optionSet As OptionSet,
            cancellationToken As CancellationToken
        ) As Boolean
            replacementNode = Nothing
            issueSpan = Nothing

            If expression.Kind = SyntaxKind.SimpleMemberAccessExpression Then
                Dim memberAccess = DirectCast(expression, MemberAccessExpressionSyntax)
                Return memberAccess.TryReduce(semanticModel,
                                              replacementNode,
                                              issueSpan,
                                              optionSet,
                                              cancellationToken)
            ElseIf TypeOf (expression) Is NameSyntax Then
                Dim name = DirectCast(expression, NameSyntax)
                Return name.TryReduce(semanticModel,
                                      replacementNode,
                                      issueSpan,
                                      optionSet,
                                      cancellationToken)
            End If

            Return False
        End Function

        <Extension()>
        Private Function TryReduce(
            memberAccess As MemberAccessExpressionSyntax,
            semanticModel As SemanticModel,
            <Out()> ByRef replacementNode As ExpressionSyntax,
            <Out()> ByRef issueSpan As TextSpan,
            optionSet As OptionSet,
            cancellationToken As CancellationToken
        ) As Boolean
            If memberAccess.Expression Is Nothing OrElse memberAccess.Name Is Nothing Then
                Return False
            End If

            If memberAccess.Expression.IsKind(SyntaxKind.MeExpression) AndAlso
                Not SimplificationHelpers.ShouldSimplifyMemberAccessExpression(semanticModel, memberAccess.Name, optionSet) Then
                Return False
            End If

            If memberAccess.HasAnnotations(SpecialTypeAnnotation.Kind) Then
                replacementNode = SyntaxFactory.PredefinedType(
                    SyntaxFactory.Token(
                        GetPredefinedKeywordKind(SpecialTypeAnnotation.GetSpecialType(memberAccess.GetAnnotations(SpecialTypeAnnotation.Kind).First())))) _
                            .WithLeadingTrivia(memberAccess.GetLeadingTrivia())

                issueSpan = memberAccess.Span

                Return True
            Else

                If Not memberAccess.IsRightSideOfDot() Then
                    Dim aliasReplacement As IAliasSymbol = Nothing

                    If memberAccess.TryReplaceWithAlias(semanticModel, aliasReplacement, optionSet.GetOption(SimplificationOptions.PreferAliasToQualification)) Then
                        Dim identifierToken = SyntaxFactory.Identifier(
                                memberAccess.GetLeadingTrivia(),
                                aliasReplacement.Name,
                                memberAccess.GetTrailingTrivia())

                        identifierToken = VisualBasicSimplificationService.TryEscapeIdentifierToken(
                                            identifierToken,
                                            semanticModel)
                        replacementNode = SyntaxFactory.IdentifierName(identifierToken)

                        issueSpan = memberAccess.Span

                        ' In case the alias name is the same as the last name of the alias target, we only include 
                        ' the left part of the name in the unnecessary span to Not confuse uses.
                        If memberAccess.Name.Identifier.ValueText = identifierToken.ValueText Then
                            issueSpan = memberAccess.Expression.Span
                        End If

                        Return True
                    End If

                    If PreferPredefinedTypeKeywordInMemberAccess(memberAccess, optionSet) Then
                        Dim symbol = semanticModel.GetSymbolInfo(memberAccess).Symbol
                        If (symbol IsNot Nothing AndAlso symbol.IsKind(SymbolKind.NamedType)) Then
                            Dim keywordKind = GetPredefinedKeywordKind(DirectCast(symbol, INamedTypeSymbol).SpecialType)
                            If keywordKind <> SyntaxKind.None Then
                                replacementNode = SyntaxFactory.PredefinedType(
                                                SyntaxFactory.Token(
                                                    memberAccess.GetLeadingTrivia(),
                                                    keywordKind,
                                                    memberAccess.GetTrailingTrivia()))

                                issueSpan = memberAccess.Span

                                Return True
                            End If
                        End If
                    End If
                End If

                ' a module name was inserted by the name expansion, so removing this should be tried first.
                If memberAccess.HasAnnotation(SimplificationHelpers.SimplifyModuleNameAnnotation) Then
                    If TryOmitModuleName(memberAccess, semanticModel, replacementNode, issueSpan, cancellationToken) Then
                        Return True
                    End If
                End If

                replacementNode = memberAccess.Name
                replacementNode = DirectCast(replacementNode, SimpleNameSyntax) _
                    .WithIdentifier(VisualBasicSimplificationService.TryEscapeIdentifierToken(
                        memberAccess.Name.Identifier,
                        semanticModel)) _
                    .WithLeadingTrivia(memberAccess.GetLeadingTriviaForSimplifiedMemberAccess()) _
                    .WithTrailingTrivia(memberAccess.GetTrailingTrivia())
                issueSpan = memberAccess.Expression.Span

                If memberAccess.CanReplaceWithReducedName(replacementNode, semanticModel, cancellationToken) Then
                    Return True
                End If

                If optionSet.GetOption(SimplificationOptions.PreferOmittingModuleNamesInQualification) Then
                    If TryOmitModuleName(memberAccess, semanticModel, replacementNode, issueSpan, cancellationToken) Then
                        Return True
                    End If
                End If
            End If

            Return False
        End Function

        <Extension>
        Private Function GetLeadingTriviaForSimplifiedMemberAccess(memberAccess As MemberAccessExpressionSyntax) As SyntaxTriviaList
            ' We want to include any user-typed trivia that may be present between the 'Expression', 'OperatorToken' and 'Identifier' of the MemberAccessExpression.
            ' However, we don't want to include any elastic trivia that may have been introduced by the expander in these locations. This is to avoid triggering
            ' aggressive formatting. Otherwise, formatter will see this elastic trivia added by the expander And use that as a cue to introduce unnecessary blank lines
            ' etc. around the user's original code.
            Return memberAccess.GetLeadingTrivia().
                AddRange(memberAccess.Expression.GetTrailingTrivia().WithoutElasticTrivia()).
                AddRange(memberAccess.OperatorToken.LeadingTrivia.WithoutElasticTrivia()).
                AddRange(memberAccess.OperatorToken.TrailingTrivia.WithoutElasticTrivia()).
                AddRange(memberAccess.Name.GetLeadingTrivia().WithoutElasticTrivia())
        End Function

        <Extension>
        Private Function WithoutElasticTrivia(list As IEnumerable(Of SyntaxTrivia)) As IEnumerable(Of SyntaxTrivia)
            Return list.Where(Function(t) Not t.IsElastic())
        End Function

        Private Function InsideCrefReference(expr As ExpressionSyntax) As Boolean
            Dim crefAttribute = expr.FirstAncestorOrSelf(Of XmlCrefAttributeSyntax)()
            Return crefAttribute IsNot Nothing
        End Function

        Private Function InsideNameOfExpression(expr As ExpressionSyntax) As Boolean
            Dim nameOfExpression = expr.FirstAncestorOrSelf(Of NameOfExpressionSyntax)()
            Return nameOfExpression IsNot Nothing
        End Function

        Private Function PreferPredefinedTypeKeywordInMemberAccess(memberAccess As ExpressionSyntax, optionSet As OptionSet) As Boolean
            Return (((memberAccess.Parent IsNot Nothing) AndAlso (TypeOf memberAccess.Parent Is MemberAccessExpressionSyntax)) OrElse
                    (InsideCrefReference(memberAccess) AndAlso Not memberAccess.IsLeftSideOfQualifiedName)) AndAlso ' Bug 1012713: Compiler has a bug due to which it doesn't support <PredefinedType>.Member inside crefs (i.e. System.Int32.MaxValue is supported but Integer.MaxValue isn't). Until this bug is fixed, we don't support simplifying types names like System.Int32.MaxValue to Integer.MaxValue.
                   (Not InsideNameOfExpression(memberAccess)) AndAlso
                   optionSet.GetOption(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, LanguageNames.VisualBasic)
        End Function

        Private Function PreferPredefinedTypeKeywordInDeclarations(name As NameSyntax, optionSet As OptionSet) As Boolean
            Return (name.Parent IsNot Nothing) AndAlso (TypeOf name.Parent IsNot MemberAccessExpressionSyntax) AndAlso (Not InsideCrefReference(name)) AndAlso
                   (Not InsideNameOfExpression(name)) AndAlso
                   optionSet.GetOption(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, LanguageNames.VisualBasic)
        End Function

        <Extension>
        Public Function GetRightmostName(node As ExpressionSyntax) As NameSyntax
            Dim memberAccess = TryCast(node, MemberAccessExpressionSyntax)
            If memberAccess IsNot Nothing AndAlso memberAccess.Name IsNot Nothing Then
                Return memberAccess.Name
            End If

            Dim qualified = TryCast(node, QualifiedNameSyntax)
            If qualified IsNot Nothing AndAlso qualified.Right IsNot Nothing Then
                Return qualified.Right
            End If

            Dim simple = TryCast(node, SimpleNameSyntax)
            If simple IsNot Nothing Then
                Return simple
            End If

            Return Nothing
        End Function

        Private Function TryOmitModuleName(memberAccess As MemberAccessExpressionSyntax, semanticModel As SemanticModel, <Out()> ByRef replacementNode As ExpressionSyntax, <Out()> ByRef issueSpan As TextSpan, cancellationToken As CancellationToken) As Boolean
            If memberAccess.IsParentKind(SyntaxKind.SimpleMemberAccessExpression) Then
                Dim symbolForMemberAccess = semanticModel.GetSymbolInfo(DirectCast(memberAccess.Parent, MemberAccessExpressionSyntax)).Symbol
                If symbolForMemberAccess.IsModuleMember Then
                    replacementNode = memberAccess.Expression.WithLeadingTrivia(memberAccess.GetLeadingTrivia())
                    issueSpan = memberAccess.Name.Span

                    Dim parent = DirectCast(memberAccess.Parent, MemberAccessExpressionSyntax)
                    Dim parentReplacement = parent.ReplaceNode(parent.Expression, replacementNode)

                    If parent.CanReplaceWithReducedName(parentReplacement, semanticModel, cancellationToken) Then
                        Return True
                    End If
                End If
            End If

            Return False
        End Function

        <Extension()>
        Private Function CanReplaceWithReducedName(
            memberAccess As MemberAccessExpressionSyntax,
            reducedNode As ExpressionSyntax,
            semanticModel As SemanticModel,
            cancellationToken As CancellationToken
        ) As Boolean
            If Not IsMeOrNamedTypeOrNamespace(memberAccess.Expression, semanticModel) Then
                Return False
            End If

            ' See if we can simplify a member access expression of the form E.M or E.M() to M or M()
            Dim speculationAnalyzer = New SpeculationAnalyzer(memberAccess, reducedNode, semanticModel, cancellationToken)
            If Not speculationAnalyzer.SymbolsForOriginalAndReplacedNodesAreCompatible() OrElse
                speculationAnalyzer.ReplacementChangesSemantics() Then
                Return False
            End If

            If memberAccess.Expression.IsKind(SyntaxKind.MyBaseExpression) Then
                Dim enclosingNamedType = semanticModel.GetEnclosingNamedType(memberAccess.SpanStart, cancellationToken)
                Dim symbol = semanticModel.GetSymbolInfo(memberAccess.Name).Symbol
                If enclosingNamedType IsNot Nothing AndAlso
                    Not enclosingNamedType.IsSealed AndAlso
                    symbol IsNot Nothing AndAlso
                    symbol.IsOverridable() Then
                    Return False
                End If
            End If

            Return True
        End Function

        <Extension()>
        Private Function TryReduce(
            name As NameSyntax,
            semanticModel As SemanticModel,
            <Out()> ByRef replacementNode As ExpressionSyntax,
            <Out()> ByRef issueSpan As TextSpan,
            optionSet As OptionSet,
            cancellationToken As CancellationToken
        ) As Boolean

            ' do not simplify names of a namespace declaration
            If IsPartOfNamespaceDeclarationName(name) Then
                Return False
            End If

            ' see whether binding the name binds to a symbol/type. if not, it is ambiguous and
            ' nothing we can do here.
            Dim symbol = SimplificationHelpers.GetOriginalSymbolInfo(semanticModel, name)
            If SimplificationHelpers.IsValidSymbolInfo(symbol) Then
                If symbol.Kind = SymbolKind.Method AndAlso symbol.IsConstructor() Then
                    symbol = symbol.ContainingType
                End If

                If symbol.Kind = SymbolKind.Method AndAlso name.Kind = SyntaxKind.GenericName Then
                    If Not optionSet.GetOption(SimplificationOptions.PreferImplicitTypeInference) Then
                        Return False
                    End If

                    Dim genericName = DirectCast(name, GenericNameSyntax)
                    replacementNode = SyntaxFactory.IdentifierName(genericName.Identifier).WithLeadingTrivia(genericName.GetLeadingTrivia()).WithTrailingTrivia(genericName.GetTrailingTrivia())

                    issueSpan = genericName.TypeArgumentList.Span
                    Return name.CanReplaceWithReducedName(replacementNode, semanticModel, cancellationToken)
                End If

                If Not TypeOf symbol Is INamespaceOrTypeSymbol Then
                    Return False
                End If
            Else
                Return False
            End If

            If name.HasAnnotations(SpecialTypeAnnotation.Kind) Then
                replacementNode = SyntaxFactory.PredefinedType(
                    SyntaxFactory.Token(name.GetLeadingTrivia(),
                                 GetPredefinedKeywordKind(SpecialTypeAnnotation.GetSpecialType(name.GetAnnotations(SpecialTypeAnnotation.Kind).First())),
                                 name.GetTrailingTrivia()))

                issueSpan = name.Span

                Return name.CanReplaceWithReducedNameInContext(replacementNode, semanticModel, cancellationToken)
            Else

                If Not name.IsRightSideOfDot() Then

                    Dim aliasReplacement As IAliasSymbol = Nothing
                    If name.TryReplaceWithAlias(semanticModel, aliasReplacement, optionSet.GetOption(SimplificationOptions.PreferAliasToQualification)) Then
                        Dim identifierToken = SyntaxFactory.Identifier(
                                name.GetLeadingTrivia(),
                                aliasReplacement.Name,
                                name.GetTrailingTrivia())

                        identifierToken = VisualBasicSimplificationService.TryEscapeIdentifierToken(
                                            identifierToken,
                                            semanticModel)

                        replacementNode = SyntaxFactory.IdentifierName(identifierToken)

                        Dim annotatedNodesOrTokens = name.GetAnnotatedNodesAndTokens(RenameAnnotation.Kind)
                        For Each annotatedNodeOrToken In annotatedNodesOrTokens
                            If annotatedNodeOrToken.IsToken Then
                                identifierToken = annotatedNodeOrToken.AsToken().CopyAnnotationsTo(identifierToken)
                            Else
                                replacementNode = annotatedNodeOrToken.AsNode().CopyAnnotationsTo(replacementNode)
                            End If
                        Next

                        annotatedNodesOrTokens = name.GetAnnotatedNodesAndTokens(AliasAnnotation.Kind)
                        For Each annotatedNodeOrToken In annotatedNodesOrTokens
                            If annotatedNodeOrToken.IsToken Then
                                identifierToken = annotatedNodeOrToken.AsToken().CopyAnnotationsTo(identifierToken)
                            Else
                                replacementNode = annotatedNodeOrToken.AsNode().CopyAnnotationsTo(replacementNode)
                            End If
                        Next

                        replacementNode = DirectCast(replacementNode, SimpleNameSyntax).WithIdentifier(identifierToken)
                        issueSpan = name.Span

                        ' In case the alias name is the same as the last name of the alias target, we only include 
                        ' the left part of the name in the unnecessary span to Not confuse uses.
                        If name.Kind = SyntaxKind.QualifiedName Then
                            Dim qualifiedName As QualifiedNameSyntax = DirectCast(name, QualifiedNameSyntax)

                            If qualifiedName.Right.Identifier.ValueText = identifierToken.ValueText Then
                                issueSpan = qualifiedName.Left.Span
                            End If
                        End If

                        If name.CanReplaceWithReducedNameInContext(replacementNode, semanticModel, cancellationToken) Then

                            ' check if the alias name ends with an Attribute suffix that can be omitted.
                            Dim replacementNodeWithoutAttributeSuffix As ExpressionSyntax = Nothing
                            Dim issueSpanWithoutAttributeSuffix As TextSpan = Nothing
                            If TryReduceAttributeSuffix(name, identifierToken, semanticModel, aliasReplacement IsNot Nothing, optionSet.GetOption(SimplificationOptions.PreferAliasToQualification), replacementNodeWithoutAttributeSuffix, issueSpanWithoutAttributeSuffix, cancellationToken) Then
                                If name.CanReplaceWithReducedName(replacementNodeWithoutAttributeSuffix, semanticModel, cancellationToken) Then
                                    replacementNode = replacementNode.CopyAnnotationsTo(replacementNodeWithoutAttributeSuffix)
                                    issueSpan = issueSpanWithoutAttributeSuffix
                                End If
                            End If

                            Return True
                        End If

                        Return False
                    End If

                    Dim nameHasNoAlias = False

                    If TypeOf name Is SimpleNameSyntax Then
                        Dim simpleName = DirectCast(name, SimpleNameSyntax)
                        If Not simpleName.Identifier.HasAnnotations(AliasAnnotation.Kind) Then
                            nameHasNoAlias = True
                        End If
                    End If

                    If TypeOf name Is QualifiedNameSyntax Then
                        Dim qualifiedNameSyntax = DirectCast(name, QualifiedNameSyntax)
                        If Not qualifiedNameSyntax.Right.Identifier.HasAnnotations(AliasAnnotation.Kind) Then
                            nameHasNoAlias = True
                        End If
                    End If

                    Dim aliasInfo = semanticModel.GetAliasInfo(name, cancellationToken)

                    ' Don't simplify to predefined type if name is part of a QualifiedName.
                    ' QualifiedNames can't contain PredefinedTypeNames (although MemberAccessExpressions can).
                    ' In other words, the left side of a QualifiedName can't be a PredefinedTypeName.
                    If nameHasNoAlias AndAlso aliasInfo Is Nothing AndAlso Not name.Parent.IsKind(SyntaxKind.QualifiedName) Then
                        Dim type = semanticModel.GetTypeInfo(name).Type
                        If type IsNot Nothing Then
                            Dim keywordKind = GetPredefinedKeywordKind(type.SpecialType)
                            If keywordKind <> SyntaxKind.None Then
                                ' But do simplify to predefined type if not simplifying results in just the addition of escaping
                                '   brackets.  E.g., even if specified otherwise, prefer `String` to `[String]`.
                                Dim token = SyntaxFactory.Token(
                                    name.GetLeadingTrivia(),
                                    keywordKind,
                                    name.GetTrailingTrivia())
                                Dim valueText = TryCast(name, IdentifierNameSyntax)?.Identifier.ValueText
                                If token.Text = valueText OrElse
                                   PreferPredefinedTypeKeywordInDeclarations(name, optionSet) OrElse
                                   PreferPredefinedTypeKeywordInMemberAccess(name, optionSet) Then
                                    replacementNode = SyntaxFactory.PredefinedType(token)
                                    issueSpan = name.Span
                                    Return name.CanReplaceWithReducedNameInContext(replacementNode, semanticModel, cancellationToken)
                                End If
                            End If
                        End If
                    End If

                    ' Nullable rewrite: Nullable(Of Integer) -> Integer?
                    ' Don't rewrite in the case where Nullable(Of Integer) is part of some qualified name like Nullable(Of Integer).Something
                    If (symbol.Kind = SymbolKind.NamedType) AndAlso (Not name.IsLeftSideOfQualifiedName) Then
                        Dim type = DirectCast(symbol, INamedTypeSymbol)
                        If aliasInfo Is Nothing AndAlso CanSimplifyNullable(type, name) Then
                            Dim genericName As GenericNameSyntax
                            If name.Kind = SyntaxKind.QualifiedName Then
                                genericName = DirectCast(DirectCast(name, QualifiedNameSyntax).Right, GenericNameSyntax)
                            Else
                                genericName = DirectCast(name, GenericNameSyntax)
                            End If

                            Dim oldType = genericName.TypeArgumentList.Arguments.First()
                            replacementNode = SyntaxFactory.NullableType(oldType).WithLeadingTrivia(name.GetLeadingTrivia())

                            issueSpan = name.Span

                            If name.CanReplaceWithReducedNameInContext(replacementNode, semanticModel, cancellationToken) Then
                                Return True
                            End If
                        End If
                    End If
                End If

                Select Case name.Kind
                    Case SyntaxKind.QualifiedName
                        ' a module name was inserted by the name expansion, so removing this should be tried first.
                        Dim qualifiedName = DirectCast(name, QualifiedNameSyntax)
                        If qualifiedName.HasAnnotation(SimplificationHelpers.SimplifyModuleNameAnnotation) Then
                            If TryOmitModuleName(qualifiedName, semanticModel, replacementNode, issueSpan, cancellationToken) Then
                                Return True
                            End If
                        End If

                        replacementNode = qualifiedName.Right.WithLeadingTrivia(name.GetLeadingTrivia())
                        replacementNode = DirectCast(replacementNode, SimpleNameSyntax) _
                            .WithIdentifier(VisualBasicSimplificationService.TryEscapeIdentifierToken(
                                            DirectCast(replacementNode, SimpleNameSyntax).Identifier,
                                            semanticModel))
                        issueSpan = qualifiedName.Left.Span


                        If name.CanReplaceWithReducedName(replacementNode, semanticModel, cancellationToken) Then
                            Return True
                        End If

                        If optionSet.GetOption(SimplificationOptions.PreferOmittingModuleNamesInQualification) Then
                            If TryOmitModuleName(qualifiedName, semanticModel, replacementNode, issueSpan, cancellationToken) Then
                                Return True
                            End If
                        End If

                    Case SyntaxKind.IdentifierName
                        Dim identifier = DirectCast(name, IdentifierNameSyntax).Identifier
                        TryReduceAttributeSuffix(name, identifier, semanticModel, False, optionSet.GetOption(SimplificationOptions.PreferAliasToQualification), replacementNode, issueSpan, cancellationToken)
                End Select
            End If

            If replacementNode Is Nothing Then
                Return False
            End If

            Return name.CanReplaceWithReducedName(replacementNode, semanticModel, cancellationToken)
        End Function

        Private Function CanSimplifyNullable(type As INamedTypeSymbol, name As NameSyntax) As Boolean
            If Not type.IsNullable Then
                Return False
            End If

            If type.IsUnboundGenericType Then
                ' Don't simplify unbound generic type "Nullable(Of )".
                Return False
            End If

            If InsideNameOfExpression(name) Then
                ' Nullable(Of T) can't be simplified to T? in nameof expressions.
                Return False
            End If

            If Not InsideCrefReference(name) Then
                ' Nullable(Of T) can always be simplified to T? outside crefs.
                Return True
            End If

            ' Inside crefs, if the T in this Nullable(Of T) is being declared right here
            ' then this Nullable(Of T) is not a constructed generic type and we should
            ' not offer to simplify this to T?.
            '
            ' For example, we should not offer the simplification in the following cases where
            ' T does not bind to an existing type / type parameter in the user's code.
            ' - <see cref="Nullable(Of T)"/>
            ' - <see cref="System.Nullable(Of T).Value"/>
            '
            ' And we should offer the simplification in the following cases where SomeType and
            ' SomeMethod bind to a type and method declared elsewhere in the users code.
            ' - <see cref="SomeType.SomeMethod(Nullable(Of SomeType))"/>

            If name.IsKind(SyntaxKind.GenericName) Then
                If (name.IsParentKind(SyntaxKind.CrefReference)) OrElse ' cref="Nullable(Of T)"
                   (name.IsParentKind(SyntaxKind.QualifiedName) AndAlso name.Parent?.IsParentKind(SyntaxKind.CrefReference)) OrElse ' cref="System.Nullable(Of T)"
                   (name.IsParentKind(SyntaxKind.QualifiedName) AndAlso name.Parent?.IsParentKind(SyntaxKind.QualifiedName) AndAlso name.Parent?.Parent?.IsParentKind(SyntaxKind.CrefReference)) Then  ' cref="System.Nullable(Of T).Value"
                    ' Unfortunately, unlike in corresponding C# case, we need syntax based checking to detect these cases because of bugs in the VB SemanticModel.
                    ' See https://github.com/dotnet/roslyn/issues/2196, https://github.com/dotnet/roslyn/issues/2197
                    Return False
                End If
            End If

            Dim argument = type.TypeArguments.SingleOrDefault()
            If argument Is Nothing OrElse argument.IsErrorType() Then
                Return False
            End If

            Dim argumentDecl = argument.DeclaringSyntaxReferences.FirstOrDefault()
            If argumentDecl Is Nothing Then
                ' The type argument is a type from metadata - so this is a constructed generic nullable type that can be simplified (e.g. Nullable(Of Integer)).
                Return True
            End If

            Return Not name.Span.Contains(argumentDecl.Span)
        End Function

        Private Function TryReduceAttributeSuffix(
            name As NameSyntax,
            identifierToken As SyntaxToken,
            semanticModel As SemanticModel,
            isIdentifierNameFromAlias As Boolean,
            preferAliasToQualification As Boolean,
            <Out()> ByRef replacementNode As ExpressionSyntax,
            <Out()> ByRef issueSpan As TextSpan,
            cancellationToken As CancellationToken
        ) As Boolean
            If SyntaxFacts.IsAttributeName(name) AndAlso Not isIdentifierNameFromAlias Then

                ' When the replacement is an Alias we don't want the "Attribute" Suffix to be removed because this will result in symbol change
                Dim aliasSymbol = semanticModel.GetAliasInfo(name, cancellationToken)
                If (aliasSymbol IsNot Nothing AndAlso preferAliasToQualification AndAlso
                    String.Compare(aliasSymbol.Name, identifierToken.ValueText, StringComparison.OrdinalIgnoreCase) = 0) Then
                    Return False
                End If

                If name.Parent.Kind = SyntaxKind.Attribute OrElse name.IsRightSideOfDot() Then
                    Dim newIdentifierText = String.Empty

                    ' an attribute that should keep it (unnecessary "Attribute" suffix should be annotated with a DontSimplifyAnnotation
                    If identifierToken.HasAnnotation(SimplificationHelpers.DontSimplifyAnnotation) Then
                        newIdentifierText = identifierToken.ValueText + "Attribute"
                    ElseIf identifierToken.ValueText.TryReduceAttributeSuffix(newIdentifierText) Then
                        issueSpan = New TextSpan(name.Span.End - 9, 9)
                    Else
                        Return False
                    End If

                    ' escape it (VB allows escaping even for abbreviated identifiers, C# does not!)
                    Dim newIdentifierToken = identifierToken.CopyAnnotationsTo(
                                             SyntaxFactory.Identifier(
                                                 identifierToken.LeadingTrivia,
                                                 newIdentifierText,
                                                 identifierToken.TrailingTrivia))
                    newIdentifierToken = VisualBasicSimplificationService.TryEscapeIdentifierToken(newIdentifierToken, semanticModel)
                    replacementNode = SyntaxFactory.IdentifierName(newIdentifierToken).WithLeadingTrivia(name.GetLeadingTrivia())
                    Return True
                End If
            End If

            Return False
        End Function

        ''' <summary>
        ''' Checks if the SyntaxNode is a name of a namespace declaration. To be a namespace name, the syntax
        ''' must be parented by an namespace declaration and the node itself must be equal to the declaration's Name
        ''' property.
        ''' </summary>
        Private Function IsPartOfNamespaceDeclarationName(node As SyntaxNode) As Boolean

            Dim nextNode As SyntaxNode = node

            Do While nextNode IsNot Nothing

                Select Case nextNode.Kind

                    Case SyntaxKind.IdentifierName, SyntaxKind.QualifiedName
                        node = nextNode
                        nextNode = nextNode.Parent

                    Case SyntaxKind.NamespaceStatement
                        Dim namespaceStatement = DirectCast(nextNode, NamespaceStatementSyntax)
                        Return namespaceStatement.Name Is node

                    Case Else
                        Return False

                End Select

            Loop

            Return False
        End Function

        Private Function TryOmitModuleName(name As QualifiedNameSyntax, semanticModel As SemanticModel, <Out()> ByRef replacementNode As ExpressionSyntax, <Out()> ByRef issueSpan As TextSpan, cancellationToken As CancellationToken) As Boolean
            If name.IsParentKind(SyntaxKind.QualifiedName) Then
                Dim symbolForName = semanticModel.GetSymbolInfo(DirectCast(name.Parent, QualifiedNameSyntax)).Symbol

                ' in case this QN is used in a "New NSName.ModuleName.MemberName()" expression
                ' the returned symbol is a constructor. Then we need to get the containing type.
                If symbolForName.IsConstructor Then
                    symbolForName = symbolForName.ContainingType
                End If

                If symbolForName.IsModuleMember Then

                    replacementNode = name.Left.WithLeadingTrivia(name.GetLeadingTrivia())
                    issueSpan = name.Right.Span

                    Dim parent = DirectCast(name.Parent, QualifiedNameSyntax)
                    Dim parentReplacement = parent.ReplaceNode(parent.Left, replacementNode)

                    If parent.CanReplaceWithReducedName(parentReplacement, semanticModel, cancellationToken) Then
                        Return True
                    End If
                End If
            End If

            Return False
        End Function

        <Extension()>
        Private Function TrySimplify(
            expression As ExpressionSyntax,
            semanticModel As SemanticModel,
            optionSet As OptionSet,
            <Out()> ByRef replacementNode As ExpressionSyntax,
            <Out()> ByRef issueSpan As TextSpan
        ) As Boolean
            replacementNode = Nothing
            issueSpan = Nothing

            Select Case expression.Kind
                Case SyntaxKind.SimpleMemberAccessExpression
                    If True Then
                        Dim memberAccess = DirectCast(expression, MemberAccessExpressionSyntax)
                        Dim newLeft As ExpressionSyntax = Nothing
                        If TrySimplifyMemberAccessOrQualifiedName(memberAccess.Expression, memberAccess.Name, semanticModel, optionSet, newLeft, issueSpan) Then
                            ' replacement node might not be in it's simplest form, so add simplify annotation to it.
                            replacementNode = memberAccess.Update(memberAccess.Kind, newLeft, memberAccess.OperatorToken, memberAccess.Name).WithAdditionalAnnotations(Simplifier.Annotation)

                            ' Ensure that replacement doesn't change semantics.
                            Return Not ReplacementChangesSemantics(memberAccess, replacementNode, semanticModel)
                        End If

                        Return False
                    End If

                Case SyntaxKind.QualifiedName
                    If True Then
                        Dim qualifiedName = DirectCast(expression, QualifiedNameSyntax)
                        Dim newLeft As ExpressionSyntax = Nothing
                        If TrySimplifyMemberAccessOrQualifiedName(qualifiedName.Left, qualifiedName.Right, semanticModel, optionSet, newLeft, issueSpan) Then
                            If Not TypeOf newLeft Is NameSyntax Then
                                Contract.Fail("QualifiedName Left = " + qualifiedName.Left.ToString() + " and QualifiedName Right = " + qualifiedName.Right.ToString() + " . Left is tried to be replaced with the PredefinedType " + replacementNode.ToString())
                            End If

                            ' replacement node might not be in it's simplest form, so add simplify annotation to it.
                            replacementNode = qualifiedName.Update(DirectCast(newLeft, NameSyntax), qualifiedName.DotToken, qualifiedName.Right).WithAdditionalAnnotations(Simplifier.Annotation)

                            ' Ensure that replacement doesn't change semantics.
                            Return Not ReplacementChangesSemantics(qualifiedName, replacementNode, semanticModel)
                        End If

                        Return False
                    End If
            End Select

            Return False
        End Function

        Private Function ReplacementChangesSemantics(originalExpression As ExpressionSyntax, replacedExpression As ExpressionSyntax, semanticModel As SemanticModel) As Boolean
            Dim speculationAnalyzer = New SpeculationAnalyzer(originalExpression, replacedExpression, semanticModel, CancellationToken.None)
            Return speculationAnalyzer.ReplacementChangesSemantics()
        End Function

        ' Note: The caller needs to verify that replacement doesn't change semantics of the original expression.
        Private Function TrySimplifyMemberAccessOrQualifiedName(
            left As ExpressionSyntax,
            right As ExpressionSyntax,
            semanticModel As SemanticModel,
            optionSet As OptionSet,
            <Out()> ByRef replacementNode As ExpressionSyntax,
            <Out()> ByRef issueSpan As TextSpan
        ) As Boolean
            replacementNode = Nothing
            issueSpan = Nothing

            If left IsNot Nothing AndAlso right IsNot Nothing Then
                Dim leftSymbol = SimplificationHelpers.GetOriginalSymbolInfo(semanticModel, left)
                If leftSymbol IsNot Nothing AndAlso leftSymbol.Kind = SymbolKind.NamedType Then
                    Dim rightSymbol = SimplificationHelpers.GetOriginalSymbolInfo(semanticModel, right)
                    If rightSymbol IsNot Nothing AndAlso (rightSymbol.IsStatic OrElse rightSymbol.Kind = SymbolKind.NamedType) Then
                        ' Static member access or nested type member access.
                        Dim containingType As INamedTypeSymbol = rightSymbol.ContainingType
                        Dim isInCref = left.Ancestors(ascendOutOfTrivia:=True).OfType(Of CrefReferenceSyntax)().Any()

                        ' Crefs in VB , irrespective of the expression are parsed as QualifiedName (no MemberAccessExpression)
                        ' Hence the Left can never be a PredefinedType (or anything other than NameSyntax)
                        If isInCref AndAlso TypeOf rightSymbol Is IMethodSymbol AndAlso Not containingType.SpecialType = SpecialType.None Then
                            Return False
                        End If

                        If containingType IsNot Nothing AndAlso Not containingType.Equals(leftSymbol) Then

                            Dim namedType = TryCast(leftSymbol, INamedTypeSymbol)
                            If namedType IsNot Nothing Then
                                If ((namedType.GetBaseTypes().Contains(containingType) AndAlso
                                    Not optionSet.GetOption(SimplificationOptions.AllowSimplificationToBaseType)) OrElse
                                    (Not optionSet.GetOption(SimplificationOptions.AllowSimplificationToGenericType) AndAlso
                                    containingType.TypeArguments.Count() <> 0)) Then
                                    Return False
                                End If
                            End If

                            ' We have a static member access or a nested type member access using a more derived type.
                            ' Simplify syntax so as to use accessed member's most immediate containing type instead of the derived type.
                            replacementNode = containingType.GenerateTypeSyntax().WithLeadingTrivia(left.GetLeadingTrivia()).WithTrailingTrivia(left.GetTrailingTrivia()).WithAdditionalAnnotations(Simplifier.Annotation)
                            issueSpan = left.Span
                            Return True
                        End If
                    End If
                End If
            End If

            Return False
        End Function

        <Extension>
        Private Function TryReplaceWithAlias(
        node As ExpressionSyntax,
        semanticModel As SemanticModel,
        <Out> ByRef aliasReplacement As IAliasSymbol,
        Optional preferAliasToQualifiedName As Boolean = False) As Boolean
            aliasReplacement = Nothing

            If Not node.IsAliasReplaceableExpression() Then
                Return False
            End If

            Dim symbol = semanticModel.GetSymbolInfo(node).Symbol

            If (symbol.IsConstructor()) Then
                symbol = symbol.ContainingType
            End If

            ' The following condition checks if the user has used alias in the original code and
            ' if so the expression is replaced with the Alias
            If TypeOf node Is QualifiedNameSyntax Then
                Dim qualifiedNameNode = DirectCast(node, QualifiedNameSyntax)
                If qualifiedNameNode.Right.Identifier.HasAnnotations(AliasAnnotation.Kind) Then
                    Dim aliasAnnotationInfo = qualifiedNameNode.Right.Identifier.GetAnnotations(AliasAnnotation.Kind).Single()

                    Dim aliasName = AliasAnnotation.GetAliasName(aliasAnnotationInfo)
                    Dim aliasIdentifier = SyntaxFactory.IdentifierName(aliasName)

                    Dim aliasTypeInfo = semanticModel.GetSpeculativeAliasInfo(node.SpanStart, aliasIdentifier, SpeculativeBindingOption.BindAsTypeOrNamespace)

                    If Not aliasTypeInfo Is Nothing Then
                        aliasReplacement = aliasTypeInfo
                        Return ValidateAliasForTarget(aliasReplacement, semanticModel, node, symbol)
                    End If
                End If
            End If

            If node.Kind = SyntaxKind.IdentifierName AndAlso semanticModel.GetAliasInfo(DirectCast(node, IdentifierNameSyntax)) IsNot Nothing Then
                Return False
            End If

            ' an alias can only replace a type Or namespace
            If symbol Is Nothing OrElse
                (symbol.Kind <> SymbolKind.Namespace AndAlso symbol.Kind <> SymbolKind.NamedType) Then

                Return False
            End If

            If symbol Is Nothing OrElse Not TypeOf (symbol) Is INamespaceOrTypeSymbol Then
                Return False
            End If

            If TypeOf node Is QualifiedNameSyntax Then
                Dim qualifiedName = DirectCast(node, QualifiedNameSyntax)
                If Not qualifiedName.Right.HasAnnotation(Simplifier.SpecialTypeAnnotation) Then
                    Dim type = semanticModel.GetTypeInfo(node).Type
                    If Not type Is Nothing Then
                        Dim keywordKind = GetPredefinedKeywordKind(type.SpecialType)
                        If keywordKind <> SyntaxKind.None Then
                            preferAliasToQualifiedName = False
                        End If
                    End If
                End If
            End If

            aliasReplacement = DirectCast(symbol, INamespaceOrTypeSymbol).GetAliasForSymbol(node, semanticModel)
            If aliasReplacement IsNot Nothing And preferAliasToQualifiedName Then
                Return ValidateAliasForTarget(aliasReplacement, semanticModel, node, symbol)
            End If

            Return False
        End Function

        ' We must verify that the alias actually binds back to the thing it's aliasing.
        ' It's possible there is another symbol with the same name as the alias that binds first
        Private Function ValidateAliasForTarget(aliasReplacement As IAliasSymbol, semanticModel As SemanticModel, node As ExpressionSyntax, symbol As ISymbol) As Boolean
            Dim aliasName = aliasReplacement.Name
            Dim boundSymbols = semanticModel.LookupNamespacesAndTypes(node.SpanStart, name:=aliasName)
            If boundSymbols.Length = 1 Then
                Dim boundAlias = TryCast(boundSymbols(0), IAliasSymbol)
                If boundAlias IsNot Nothing And aliasReplacement.Target.Equals(symbol) Then
                    If symbol.IsAttribute Then
                        boundSymbols = semanticModel.LookupNamespacesAndTypes(node.Span.Start, name:=aliasName + "Attribute")
                        Return boundSymbols.IsEmpty
                    End If

                    Return True
                End If
            End If
            Return False
        End Function

        <Extension()>
        Private Function CanReplaceWithReducedName(
            name As NameSyntax,
            replacementNode As ExpressionSyntax,
            semanticModel As SemanticModel,
            cancellationToken As CancellationToken
        ) As Boolean
            Dim speculationAnalyzer = New SpeculationAnalyzer(name, replacementNode, semanticModel, cancellationToken)
            If speculationAnalyzer.ReplacementChangesSemantics() Then
                Return False
            End If

            Return name.CanReplaceWithReducedNameInContext(replacementNode, semanticModel, cancellationToken)
        End Function

        <Extension>
        Private Function CanReplaceWithReducedNameInContext(name As NameSyntax, replacementNode As ExpressionSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As Boolean

            ' Special case.  if this new minimal name parses out to a predefined type, then we
            ' have to make sure that we're not in a using alias.   That's the one place where the
            ' language doesn't allow predefined types.  You have to use the fully qualified name
            ' instead.
            Dim invalidTransformation1 = IsNonNameSyntaxInImportsDirective(name, replacementNode)
            Dim invalidTransformation2 = IsReservedNameInAttribute(name, replacementNode)

            Dim invalidTransformation3 = IsNullableTypeSyntaxLeftOfDotInMemberAccess(name, replacementNode)

            Return Not (invalidTransformation1 OrElse invalidTransformation2 OrElse invalidTransformation3)
        End Function

        Private Function IsMeOrNamedTypeOrNamespace(expression As ExpressionSyntax, semanticModel As SemanticModel) As Boolean
            If expression.Kind = SyntaxKind.MeExpression Then
                Return True
            End If

            Dim expressionInfo = semanticModel.GetSymbolInfo(expression)
            If SimplificationHelpers.IsValidSymbolInfo(expressionInfo.Symbol) Then
                If TypeOf expressionInfo.Symbol Is INamespaceOrTypeSymbol Then
                    Return True
                End If

                If expressionInfo.Symbol.IsThisParameter() Then
                    Return True
                End If
            End If

            Return False
        End Function

        ''' <summary>
        ''' Returns the predefined keyword kind for a given special type.
        ''' </summary>
        ''' <param name="type">The specialtype of this type.</param>
        ''' <returns>The keyword kind for a given special type, or SyntaxKind.None if the type name is not a predefined type.</returns>
        Private Function GetPredefinedKeywordKind(type As SpecialType) As SyntaxKind
            Select Case type
                Case SpecialType.System_Boolean
                    Return SyntaxKind.BooleanKeyword
                Case SpecialType.System_Byte
                    Return SyntaxKind.ByteKeyword
                Case SpecialType.System_SByte
                    Return SyntaxKind.SByteKeyword
                Case SpecialType.System_Int32
                    Return SyntaxKind.IntegerKeyword
                Case SpecialType.System_UInt32
                    Return SyntaxKind.UIntegerKeyword
                Case SpecialType.System_Int16
                    Return SyntaxKind.ShortKeyword
                Case SpecialType.System_UInt16
                    Return SyntaxKind.UShortKeyword
                Case SpecialType.System_Int64
                    Return SyntaxKind.LongKeyword
                Case SpecialType.System_UInt64
                    Return SyntaxKind.ULongKeyword
                Case SpecialType.System_Single
                    Return SyntaxKind.SingleKeyword
                Case SpecialType.System_Double
                    Return SyntaxKind.DoubleKeyword
                Case SpecialType.System_Decimal
                    Return SyntaxKind.DecimalKeyword
                Case SpecialType.System_String
                    Return SyntaxKind.StringKeyword
                Case SpecialType.System_Char
                    Return SyntaxKind.CharKeyword
                Case SpecialType.System_Object
                    Return SyntaxKind.ObjectKeyword
                Case SpecialType.System_DateTime
                    Return SyntaxKind.DateKeyword
                Case Else
                    Return SyntaxKind.None
            End Select
        End Function

        Private Function IsNullableTypeSyntaxLeftOfDotInMemberAccess(expression As ExpressionSyntax, simplifiedNode As ExpressionSyntax) As Boolean
            Return expression.IsParentKind(SyntaxKind.SimpleMemberAccessExpression) AndAlso
                simplifiedNode.Kind = SyntaxKind.NullableType
        End Function

        Private Function IsNonNameSyntaxInImportsDirective(expression As ExpressionSyntax, simplifiedNode As ExpressionSyntax) As Boolean
            Return TypeOf expression.Parent Is ImportsClauseSyntax AndAlso
                Not TypeOf simplifiedNode Is NameSyntax
        End Function

        Public Function IsReservedNameInAttribute(originalName As NameSyntax, simplifiedNode As ExpressionSyntax) As Boolean
            Dim attribute = originalName.GetAncestorOrThis(Of AttributeSyntax)()
            If attribute Is Nothing Then
                Return False
            End If

            Dim identifier As SimpleNameSyntax
            If simplifiedNode.Kind = SyntaxKind.IdentifierName Then
                identifier = DirectCast(simplifiedNode, SimpleNameSyntax)
            ElseIf simplifiedNode.Kind = SyntaxKind.QualifiedName Then
                identifier = DirectCast(DirectCast(simplifiedNode, QualifiedNameSyntax).Left, SimpleNameSyntax)
            Else
                Return False
            End If

            If identifier.Identifier.IsBracketed Then
                Return False
            End If

            If attribute.Target Is Nothing Then
                Dim identifierValue = SyntaxFacts.MakeHalfWidthIdentifier(identifier.Identifier.ValueText)

                If CaseInsensitiveComparison.Equals(identifierValue, "Assembly") OrElse
                   CaseInsensitiveComparison.Equals(identifierValue, "Module") Then
                    Return True
                End If
            End If

            Return False
        End Function
    End Module
End Namespace
