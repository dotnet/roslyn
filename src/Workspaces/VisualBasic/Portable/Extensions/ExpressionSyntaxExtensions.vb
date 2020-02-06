' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
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
        Public Function Parenthesize(expression As ExpressionSyntax, Optional addSimplifierAnnotation As Boolean = True) As ParenthesizedExpressionSyntax
            Dim result = SyntaxFactory.ParenthesizedExpression(expression.WithoutTrivia()) _
                                      .WithTriviaFrom(expression)
            Return If(addSimplifierAnnotation,
                      result.WithAdditionalAnnotations(Simplifier.Annotation),
                      result)
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
            If expression Is Nothing Then
                Return False
            End If

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
            <Out> ByRef wasCastAdded As Boolean,
            cancellationToken As CancellationToken) As ExpressionSyntax

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
            Dim specAnalyzer = New SpeculationAnalyzer(expression, castExpression, semanticModel, cancellationToken)
            Dim speculativeSemanticModel = specAnalyzer.SpeculativeSemanticModel
            If speculativeSemanticModel Is Nothing Then
                Return expression
            End If

            Dim speculatedCastExpression = specAnalyzer.ReplacedExpression
            Dim speculatedCastInnerExpression = If(isResultPredefinedCast,
                                                   DirectCast(speculatedCastExpression, PredefinedCastExpressionSyntax).Expression,
                                                   DirectCast(speculatedCastExpression, CastExpressionSyntax).Expression)
            If Not CastAnalyzer.IsUnnecessary(speculatedCastExpression, speculatedCastInnerExpression, speculativeSemanticModel, True, cancellationToken) Then
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
        Public Function IsInOutContext(expression As ExpressionSyntax) As Boolean
            ' NOTE(cyrusn): VB has no concept of an out context.  Even when a parameter has an
            ' '<Out>' attribute on it, it's still treated as ref by VB.  So we always return false
            ' here.
            Return False
        End Function

        <Extension()>
        Public Function IsInRefContext(expression As ExpressionSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As Boolean
            Dim simpleArgument = TryCast(expression?.Parent, SimpleArgumentSyntax)

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
        Public Function IsInInContext(expression As ExpressionSyntax) As Boolean
            ' NOTE: VB does not support in parameters. Always return False here.
            Return False
        End Function

        <Extension()>
        Public Function IsOnlyWrittenTo(expression As ExpressionSyntax) As Boolean
            If expression.IsRightSideOfDot() Then
                expression = TryCast(expression.Parent, ExpressionSyntax)
            End If

            If expression IsNot Nothing Then
                If expression.IsInOutContext() Then
                    Return True
                End If

                If expression.IsParentKind(SyntaxKind.SimpleAssignmentStatement) Then
                    Dim assignmentStatement = DirectCast(expression.Parent, AssignmentStatementSyntax)
                    If expression Is assignmentStatement.Left Then
                        Return True
                    End If
                End If

                If expression.IsParentKind(SyntaxKind.NameColonEquals) AndAlso
                   expression.Parent.IsParentKind(SyntaxKind.SimpleArgument) Then

                    ' <C(Prop:=1)>
                    ' this is only a write to Prop
                    Return True
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
            If IsOnlyWrittenTo(expression) Then
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

            ' Technically, you could introduce an LValue for "Goo" in "Goo()" even if "Goo" binds
            ' to a method.  (i.e. by assigning to a Func<...> type).  However, this is so contrived
            ' and none of the features that use this extension consider this replaceable.
            If TypeOf expression.Parent Is InvocationExpressionSyntax Then

                ' If something is being invoked, then it's either something like Goo(), Goo.Bar(), or
                ' SomeExpr() (i.e. Blah[1]()).  In the first and second case, we only allow
                ' replacement if Goo and Goo.Bar didn't bind to a method.  If we can't bind it, we'll
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
                                         Select(Function(m) m.GetExpressionOfMemberAccessExpression(allowImplicitTarget:=True)).
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

        <Extension>
        Public Function InsideCrefReference(expression As ExpressionSyntax) As Boolean
            Dim crefAttribute = expression.FirstAncestorOrSelf(Of XmlCrefAttributeSyntax)()
            Return crefAttribute IsNot Nothing
        End Function

        <Extension>
        Public Function IsDirectChildOfMemberAccessExpression(expression As ExpressionSyntax) As Boolean
            Return TypeOf expression?.Parent Is MemberAccessExpressionSyntax
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

        <Extension>
        Public Function IsNameOfArgumentExpression(expression As ExpressionSyntax) As Boolean
            Return expression.IsParentKind(SyntaxKind.NameOfExpression)
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
