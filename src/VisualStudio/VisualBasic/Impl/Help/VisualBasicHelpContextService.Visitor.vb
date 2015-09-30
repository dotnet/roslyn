' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Help
    Partial Friend Class VisualBasicHelpContextService
        Private Class Visitor
            Inherits VisualBasicSyntaxVisitor

            Public result As String = Nothing
            Private ReadOnly _span As TextSpan
            Private ReadOnly _semanticModel As SemanticModel
            Private ReadOnly _service As VisualBasicHelpContextService
            Private ReadOnly _isNotMetadata As Boolean
            Private ReadOnly _cancellationToken As CancellationToken

            Public Sub New(span As TextSpan, semanticModel As SemanticModel, isNotMetadata As Boolean, service As VisualBasicHelpContextService, cancellationToken As CancellationToken)
                Me._span = span
                Me._semanticModel = semanticModel
                Me._isNotMetadata = isNotMetadata
                Me._service = service
                Me._cancellationToken = cancellationToken
            End Sub

            Private Function Keyword(text As String) As String
                Return "vb." + text
            End Function

            Private Function Keyword(kind As SyntaxKind) As String
                Return Keyword(kind.GetText())
            End Function

            Public Overrides Sub Visit(node As SyntaxNode)
                If node IsNot Nothing Then
                    DirectCast(node, VisualBasicSyntaxNode).Accept(Me)

                    If result Is Nothing AndAlso node IsNot Nothing Then
                        Visit(node.Parent)
                    End If

                End If
            End Sub

            Public Overrides Sub DefaultVisit(node As SyntaxNode)
                If node IsNot Nothing Then
                    Visit(node.Parent)
                End If
            End Sub

            Public Overrides Sub VisitEventStatement(node As EventStatementSyntax)
                If Not TryGetDeclaredSymbol(node.Identifier) Then
                    result = Keyword("Event")
                End If
            End Sub

            Public Overrides Sub VisitAttributeTarget(node As AttributeTargetSyntax)
                If node.AttributeModifier.Kind() = SyntaxKind.ModuleKeyword Then
                    result = HelpKeywords.ModuleAttribute
                ElseIf node.AttributeModifier.Kind() = SyntaxKind.AssemblyKeyword Then
                    result = HelpKeywords.AssemblyAttribute
                End If
            End Sub

            Public Overrides Sub VisitAddRemoveHandlerStatement(node As AddRemoveHandlerStatementSyntax)
                result = Keyword(node.AddHandlerOrRemoveHandlerKeyword.ValueText)
            End Sub

            Public Overrides Sub VisitLocalDeclarationStatement(node As LocalDeclarationStatementSyntax)
                SelectModifier(node.Modifiers)
            End Sub

            Public Overrides Sub VisitEndBlockStatement(node As EndBlockStatementSyntax)
                If Not node.BlockKeyword.IsMissing Then
                    Select Case node.BlockKeyword.Kind()
                        Case SyntaxKind.AddHandlerKeyword
                            result = Keyword(SyntaxKind.AddHandlerKeyword)
                        Case SyntaxKind.RaiseEventKeyword
                            result = Keyword(SyntaxKind.RaiseEventKeyword)
                        Case SyntaxKind.RemoveHandlerKeyword
                            result = Keyword(SyntaxKind.RemoveHandlerKeyword)
                        Case SyntaxKind.SubKeyword
                        Case SyntaxKind.FunctionKeyword
                            If node.GetAncestor(Of MultiLineLambdaExpressionSyntax)() IsNot Nothing Then
                                result = HelpKeywords.LambdaFunction
                            End If
                        Case Else
                    End Select

                    If result Is Nothing Then
                        result = Keyword(node.BlockKeyword.ValueText)
                    End If
                Else
                    result = HelpKeywords.EndDefinition
                End If
            End Sub

            Public Overrides Sub VisitArrayCreationExpression(node As ArrayCreationExpressionSyntax)
                result = "vb.Array"
            End Sub

            Public Overrides Sub VisitAggregateClause(node As AggregateClauseSyntax)
                If Not node.IntoKeyword.IsMissing Then
                    result = HelpKeywords.QueryAggregateInto
                End If
                result = HelpKeywords.QueryAggregate
            End Sub

            Public Overrides Sub VisitAssignmentStatement(node As AssignmentStatementSyntax)
                result = Keyword(node.OperatorToken.Kind())
            End Sub

            Public Overrides Sub VisitAttributeList(node As AttributeListSyntax)
                result = HelpKeywords.Attributes
            End Sub

            Public Overrides Sub VisitBinaryExpression(node As BinaryExpressionSyntax)
                result = Keyword(node.OperatorToken.Text)
            End Sub

            Public Overrides Sub VisitCallStatement(node As CallStatementSyntax)
                result = Keyword(node.CallKeyword.Text)
            End Sub

            Public Overrides Sub VisitCatchFilterClause(node As CatchFilterClauseSyntax)
                result = Keyword(node.WhenKeyword.Text)
            End Sub

            Public Overrides Sub VisitCollectionInitializer(node As CollectionInitializerSyntax)
                If TypeOf (node.Parent) Is ArrayCreationExpressionSyntax Then
                    Visit(node.Parent)
                Else
                    result = HelpKeywords.CollectionInitializer
                End If
            End Sub

            Public Overrides Sub VisitContinueStatement(node As ContinueStatementSyntax)
                result = Keyword(node.ContinueKeyword.Text)
            End Sub

            Public Overrides Sub VisitDeclareStatement(node As DeclareStatementSyntax)
                ' TODO
                result = Keyword(node.DeclareKeyword.Text)
            End Sub

            Public Overrides Sub VisitDelegateStatement(node As DelegateStatementSyntax)
                If Not SelectModifier(node.Modifiers) Then
                    result = Keyword(node.DelegateKeyword.Text)
                End If
            End Sub

            Public Overrides Sub VisitDistinctClause(node As DistinctClauseSyntax)
                result = HelpKeywords.QueryDistinct
            End Sub

            Public Overrides Sub VisitDoLoopBlock(node As DoLoopBlockSyntax)
                result = Keyword("Do")
            End Sub

            Public Overrides Sub VisitIfStatement(node As IfStatementSyntax)
                If node.ThenKeyword.Span.IntersectsWith(_span) Then
                    result = Keyword(node.ThenKeyword.Text)
                Else
                    result = Keyword("If")
                End If
            End Sub

            Public Overrides Sub VisitElseIfStatement(node As ElseIfStatementSyntax)
                If node.ThenKeyword.Span.IntersectsWith(_span) Then
                    result = Keyword(node.ThenKeyword.Text)
                Else
                    result = Keyword("ElseIf")
                End If
            End Sub

            Public Overrides Sub VisitSingleLineIfStatement(node As SingleLineIfStatementSyntax)
                If node.ThenKeyword.Span.IntersectsWith(_span) Then
                    result = Keyword(node.ThenKeyword.Text)
                Else
                    result = Keyword("If")
                End If
            End Sub

            Public Overrides Sub VisitSingleLineElseClause(node As SingleLineElseClauseSyntax)
                result = Keyword("Else")
            End Sub

            Public Overrides Sub VisitObjectCreationExpression(node As ObjectCreationExpressionSyntax)
                result = Keyword("New")
            End Sub

            Public Overrides Sub VisitParameter(node As ParameterSyntax)
                SelectModifier(node.Modifiers)
            End Sub

            Public Overrides Sub VisitPredefinedCastExpression(node As PredefinedCastExpressionSyntax)
                result = Keyword(node.Keyword.Text)
            End Sub

            Public Overrides Sub VisitSelectBlock(node As SelectBlockSyntax)
                result = Keyword("Select")
            End Sub

            Public Overrides Sub VisitSimpleAsClause(node As SimpleAsClauseSyntax)
                result = Keyword(node.AsKeyword.Text)
            End Sub

            Public Overrides Sub VisitTryBlock(node As TryBlockSyntax)
                result = Keyword("Try")
            End Sub

            Public Overrides Sub VisitEnumBlock(node As EnumBlockSyntax)
                result = Keyword("Enum")
            End Sub

            Public Overrides Sub VisitEqualsValue(node As EqualsValueSyntax)
                result = Keyword(SyntaxKind.EqualsToken)
            End Sub

            Public Overrides Sub VisitEraseStatement(node As EraseStatementSyntax)
                result = Keyword("Erase")
            End Sub

            Public Overrides Sub VisitElseStatement(node As ElseStatementSyntax)
                result = Keyword("Else")
            End Sub

            Public Overrides Sub VisitErrorStatement(node As ErrorStatementSyntax)
                result = Keyword("Error")
            End Sub

            Public Overrides Sub VisitEventBlock(node As EventBlockSyntax)
                result = Keyword("Event")
            End Sub

            Public Overrides Sub VisitExitStatement(node As ExitStatementSyntax)
                result = Keyword("Exit")
            End Sub

            Public Overrides Sub VisitFieldDeclaration(node As FieldDeclarationSyntax)
                Dim modifier = node.Modifiers.FirstOrDefault(Function(m) m.Span.IntersectsWith(_span))

                If modifier <> Nothing Then
                    result = Keyword(modifier.Text)
                End If
            End Sub

            Public Overrides Sub VisitForEachStatement(node As ForEachStatementSyntax)
                If node.InKeyword.Span.IntersectsWith(_span) Then
                    result = Keyword("In")
                ElseIf node.EachKeyword.Span.IntersectsWith(_span) Then
                    result = Keyword("Each")
                Else
                    result = HelpKeywords.ForEach
                End If
            End Sub

            Public Overrides Sub VisitForStatement(node As ForStatementSyntax)
                If node.ToKeyword.Span.IntersectsWith(_span) Then
                    result = Keyword("To")
                ElseIf node.StepClause.StepKeyword.Span.IntersectsWith(_span) Then
                    result = Keyword("Step")
                Else
                    result = Keyword("For")
                End If
            End Sub

            Public Overrides Sub VisitFromClause(node As FromClauseSyntax)
                result = HelpKeywords.QueryFrom
            End Sub

            Public Overrides Sub VisitTypeParameter(node As TypeParameterSyntax)
                If node.VarianceKeyword.Span.IntersectsWith(_span) Then
                    If node.VarianceKeyword.Kind() = SyntaxKind.OutKeyword Then
                        result = HelpKeywords.VarianceOut
                    Else
                        result = HelpKeywords.VarianceIn
                    End If
                End If
            End Sub

            Public Overrides Sub VisitGetTypeExpression(node As GetTypeExpressionSyntax)
                result = Keyword("GetType")
            End Sub

            Public Overrides Sub VisitGetXmlNamespaceExpression(node As GetXmlNamespaceExpressionSyntax)
                result = Keyword(node.GetXmlNamespaceKeyword.Text)
            End Sub

            Public Overrides Sub VisitGlobalName(node As GlobalNameSyntax)
                result = Keyword("Global")
            End Sub

            Public Overrides Sub VisitGoToStatement(node As GoToStatementSyntax)
                result = Keyword("GoTo")
            End Sub

            Public Overrides Sub VisitGroupByClause(node As GroupByClauseSyntax)
                If node.IntoKeyword.Span.IntersectsWith(_span) Then
                    result = HelpKeywords.QueryGroupByInto
                Else
                    result = HelpKeywords.QueryGroupBy
                End If
            End Sub

            Public Overrides Sub VisitGroupJoinClause(node As GroupJoinClauseSyntax)
                If node.OnKeyword.Span.IntersectsWith(_span) Then
                    result = HelpKeywords.QueryGroupJoinOn
                ElseIf node.IntoKeyword.Span.IntersectsWith(_span) Then
                    result = HelpKeywords.QueryGroupJoinInto
                Else
                    result = HelpKeywords.QueryGroupJoin
                End If
            End Sub

            Public Overrides Sub VisitGroupAggregation(node As GroupAggregationSyntax)
                result = HelpKeywords.QueryGroupRef
            End Sub

            Public Overrides Sub VisitBinaryConditionalExpression(node As BinaryConditionalExpressionSyntax)
                result = HelpKeywords.IfOperator
            End Sub

            Public Overrides Sub VisitTernaryConditionalExpression(node As TernaryConditionalExpressionSyntax)
                result = HelpKeywords.IfOperator
            End Sub

            Public Overrides Sub VisitImplementsStatement(node As ImplementsStatementSyntax)
                result = Keyword("Implements")
            End Sub

            Public Overrides Sub VisitInheritsStatement(node As InheritsStatementSyntax)
                result = Keyword("Inherits")
            End Sub

            Public Overrides Sub VisitInferredFieldInitializer(node As InferredFieldInitializerSyntax)
                If node.KeyKeyword <> Nothing Then
                    result = HelpKeywords.AnonymousKey
                End If
            End Sub

            Public Overrides Sub VisitTypeOfExpression(node As TypeOfExpressionSyntax)
                result = Keyword("TypeOf")
            End Sub

            Public Overrides Sub VisitLambdaHeader(node As LambdaHeaderSyntax)
                If Not SelectModifier(node.Modifiers) Then
                    result = HelpKeywords.LambdaFunction
                End If
            End Sub

            Public Overrides Sub VisitSubNewStatement(node As SubNewStatementSyntax)
                If Not TryGetDeclaredSymbol(node.NewKeyword) Then
                    result = HelpKeywords.Constructor
                End If
            End Sub

            Public Overrides Sub VisitConstructorBlock(node As ConstructorBlockSyntax)
                result = HelpKeywords.Constructor
            End Sub

            Public Overrides Sub VisitLetClause(node As LetClauseSyntax)
                result = HelpKeywords.QueryLet
            End Sub

            Public Overrides Sub VisitMethodStatement(node As MethodStatementSyntax)
                If SelectModifier(node.Modifiers) Then
                    If result = "vb.Partial" Then
                        result = HelpKeywords.PartialMethod
                    End If

                ElseIf node.Identifier.Span.IntersectsWith(_span) AndAlso
                        node.Parent.Parent.Kind() = SyntaxKind.ModuleBlock AndAlso
                        node.Identifier.GetIdentifierText().Equals("Main", StringComparison.CurrentCultureIgnoreCase) Then

                    result = HelpKeywords.Main
                Else
                    Select Case node.DeclarationKeyword.Kind()
                        Case SyntaxKind.AddHandlerKeyword
                            result = HelpKeywords.AddHandlerMethod
                        Case SyntaxKind.RaiseEventKeyword
                            result = HelpKeywords.RaiseEventMethod
                        Case SyntaxKind.RemoveHandlerKeyword
                            result = HelpKeywords.RemoveHandlerMethod
                        Case Else
                            result = Keyword(node.DeclarationKeyword.Text)
                    End Select

                End If

                TryGetDeclaredSymbol(node.Identifier)
            End Sub

            Public Overrides Sub VisitMeExpression(node As MeExpressionSyntax)
                result = Keyword("Me")
            End Sub

            Public Overrides Sub VisitMidExpression(node As MidExpressionSyntax)
                result = Keyword("Mid")
            End Sub

            Public Overrides Sub VisitMyBaseExpression(node As MyBaseExpressionSyntax)
                result = Keyword("MyBase")
            End Sub

            Public Overrides Sub VisitMyClassExpression(node As MyClassExpressionSyntax)
                result = Keyword("MyClass")
            End Sub

            Public Overrides Sub VisitNamedFieldInitializer(node As NamedFieldInitializerSyntax)
                If node.KeyKeyword.Span.IntersectsWith(_span) Then
                    result = HelpKeywords.AnonymousKey
                End If
            End Sub

            Public Overrides Sub VisitIdentifierName(node As IdentifierNameSyntax)
                Select Case node.Identifier.Kind()
                    Case SyntaxKind.MyBaseKeyword
                    Case SyntaxKind.MyClassKeyword
                    Case SyntaxKind.MeKeyword
                        result = Keyword(node.Identifier.Kind())
                        Return
                End Select

                If _isNotMetadata Then
                    If Not TypeOf node.Parent Is InheritsOrImplementsStatementSyntax Then
                        If TypeOf node.Parent Is DeclarationStatementSyntax OrElse TypeOf node.Parent Is FieldDeclarationSyntax Then
                            Return
                        End If
                    End If
                End If

                Dim symbol = _semanticModel.GetSymbolInfo(node, _cancellationToken).Symbol

                If symbol Is Nothing Then
                    symbol = _semanticModel.GetMemberGroup(node, _cancellationToken).FirstOrDefault()
                End If

                If symbol Is Nothing OrElse symbol.IsKind(SymbolKind.RangeVariable) Then
                    symbol = _semanticModel.GetTypeInfo(node, _cancellationToken).Type
                End If

                If symbol IsNot Nothing Then
                    If symbol.Name.Equals("My", StringComparison.CurrentCultureIgnoreCase) Then
                        result = HelpKeywords.MyNamespaceKeyword
                    ElseIf TypeOf symbol Is ITypeSymbol AndAlso DirectCast(symbol, ITypeSymbol).SpecialType <> SpecialType.None Then
                        result = "vb." + symbol.Name
                    Else
                        result = _service.FormatSymbol(symbol)
                    End If
                End If
            End Sub

            Public Overrides Sub VisitNamespaceBlock(node As NamespaceBlockSyntax)
                result = Keyword("Namespace")
            End Sub

            Public Overrides Sub VisitAnonymousObjectCreationExpression(node As AnonymousObjectCreationExpressionSyntax)
                result = HelpKeywords.AnonymousType
            End Sub

            Public Overrides Sub VisitObjectCollectionInitializer(node As ObjectCollectionInitializerSyntax)
                result = HelpKeywords.CollectionInitializer
            End Sub

            Public Overrides Sub VisitNextStatement(node As NextStatementSyntax)
                result = Keyword("Next")
            End Sub

            Public Overrides Sub VisitOnErrorGoToStatement(node As OnErrorGoToStatementSyntax)
                result = HelpKeywords.OnError
            End Sub

            Public Overrides Sub VisitOnErrorResumeNextStatement(node As OnErrorResumeNextStatementSyntax)
                result = HelpKeywords.OnError
            End Sub

            Public Overrides Sub VisitOptionStatement(node As OptionStatementSyntax)
                If Not node.NameKeyword.IsMissing Then
                    Select Case node.NameKeyword.Kind()
                        Case SyntaxKind.ExplicitKeyword
                            result = HelpKeywords.OptionExplicit
                        Case SyntaxKind.InferKeyword
                            result = HelpKeywords.OptionInfer
                        Case SyntaxKind.StrictKeyword
                            result = HelpKeywords.OptionStrict
                        Case SyntaxKind.CompareKeyword
                            result = HelpKeywords.OptionCompare
                    End Select
                Else
                    result = Keyword(SyntaxKind.OptionKeyword)
                End If
            End Sub

            Public Overrides Sub VisitOrdering(node As OrderingSyntax)
                If node.AscendingOrDescendingKeyword.IsKind(SyntaxKind.AscendingKeyword) Then
                    result = HelpKeywords.QueryAscending
                Else
                    result = HelpKeywords.QueryDescending
                End If
            End Sub

            Public Overrides Sub VisitOrderByClause(node As OrderByClauseSyntax)
                result = HelpKeywords.QueryOrderBy
            End Sub

            Public Overrides Sub VisitIfDirectiveTrivia(node As IfDirectiveTriviaSyntax)
                result = HelpKeywords.PreprocessorIf
            End Sub

            Public Overrides Sub VisitRegionDirectiveTrivia(node As RegionDirectiveTriviaSyntax)
                If node.Name.Span.IntersectsWith(_span) Then
                    result = Keyword(SyntaxKind.StringKeyword)
                Else
                    result = HelpKeywords.Region
                End If
            End Sub

            Public Overrides Sub VisitConstDirectiveTrivia(node As ConstDirectiveTriviaSyntax)
                result = HelpKeywords.PreprocessorConst
            End Sub

            Public Overrides Sub VisitStopOrEndStatement(node As StopOrEndStatementSyntax)
                If node.StopOrEndKeyword.Kind() = SyntaxKind.EndKeyword Then
                    result = Keyword(SyntaxKind.EndKeyword)
                Else
                    result = Keyword(SyntaxKind.StopKeyword)
                End If
            End Sub

            Public Overrides Sub VisitStructureStatement(node As StructureStatementSyntax)
                If Not SelectModifier(node.Modifiers) AndAlso Not TryGetDeclaredSymbol(node.Identifier) Then
                    result = Keyword(SyntaxKind.StructureKeyword)
                End If
            End Sub

            Public Overrides Sub VisitModuleStatement(node As ModuleStatementSyntax)
                If Not SelectModifier(node.Modifiers) AndAlso Not TryGetDeclaredSymbol(node.Identifier) Then
                    result = Keyword("Module")
                End If
            End Sub

            Public Overrides Sub VisitPropertyStatement(node As PropertyStatementSyntax)
                If Not SelectModifier(node.Modifiers) AndAlso Not TryGetDeclaredSymbol(node.Identifier) Then
                    If node.Parent.Kind() <> SyntaxKind.PropertyBlock Then
                        result = HelpKeywords.AutoProperty
                    Else
                        result = Keyword("Property")
                    End If
                End If
            End Sub

            Public Overrides Sub VisitClassStatement(node As ClassStatementSyntax)
                If Not SelectModifier(node.Modifiers) AndAlso Not TryGetDeclaredSymbol(node.Identifier) Then
                    result = Keyword("Class")
                End If
            End Sub

            Public Overrides Sub VisitAccessorStatement(node As AccessorStatementSyntax)
                If Not SelectModifier(node.Modifiers) AndAlso Not TryGetDeclaredSymbol(node.DeclarationKeyword) Then
                    result = Keyword(node.DeclarationKeyword.Kind())
                End If
            End Sub

            Public Overrides Sub VisitImportsStatement(node As ImportsStatementSyntax)
                result = Keyword(SyntaxKind.ImportsKeyword)
            End Sub

            Public Overrides Sub VisitInterfaceStatement(node As InterfaceStatementSyntax)
                If Not SelectModifier(node.Modifiers) AndAlso Not TryGetDeclaredSymbol(node.Identifier) Then
                    result = Keyword(SyntaxKind.InterfaceKeyword)
                End If
            End Sub

            Public Overrides Sub VisitNamespaceStatement(node As NamespaceStatementSyntax)
                If Not TryGetDeclaredSymbol(node.GetNameToken()) Then
                    result = Keyword(SyntaxKind.NamespaceKeyword)
                End If
            End Sub

            Public Overrides Sub VisitLabelStatement(node As LabelStatementSyntax)
                result = HelpKeywords.Colon
            End Sub

            Public Overrides Sub VisitModifiedIdentifier(node As ModifiedIdentifierSyntax)
                If node.Nullable.Kind() = SyntaxKind.QuestionToken Then
                    result = HelpKeywords.Nullable
                Else
                    Dim symbol = _semanticModel.GetDeclaredSymbol(node, _cancellationToken)

                    If symbol IsNot Nothing Then
                        result = _service.FormatSymbol(symbol)
                    End If
                End If
            End Sub

            Public Overrides Sub VisitSpecialConstraint(node As SpecialConstraintSyntax)
                Select Case node.ConstraintKeyword.Kind()
                    Case SyntaxKind.NewKeyword
                        result = HelpKeywords.NewConstraint
                    Case SyntaxKind.ClassKeyword
                        result = HelpKeywords.ClassConstraint
                    Case SyntaxKind.StructureKeyword
                        result = HelpKeywords.StructureConstraint
                End Select
            End Sub

            Public Overrides Sub VisitObjectMemberInitializer(node As ObjectMemberInitializerSyntax)
                If Not node.Parent.IsKind(SyntaxKind.AnonymousObjectCreationExpression) Then
                    result = HelpKeywords.ObjectInitializer
                End If
            End Sub

            Public Overrides Sub VisitYieldStatement(node As YieldStatementSyntax)
                result = Keyword(SyntaxKind.YieldKeyword)
            End Sub

            Public Overrides Sub VisitElseDirectiveTrivia(node As ElseDirectiveTriviaSyntax)
                result = HelpKeywords.PreprocessorIf
            End Sub

            Public Overrides Sub VisitEndIfDirectiveTrivia(node As EndIfDirectiveTriviaSyntax)
                result = HelpKeywords.PreprocessorIf
            End Sub

            Public Overrides Sub VisitEndRegionDirectiveTrivia(node As EndRegionDirectiveTriviaSyntax)
                result = HelpKeywords.Region
            End Sub

            Public Overrides Sub VisitSyncLockStatement(node As SyncLockStatementSyntax)
                result = "vb.SyncLock"
            End Sub

            Public Overrides Sub VisitUnaryExpression(node As UnaryExpressionSyntax)
                If node.OperatorToken.IsKind(SyntaxKind.MinusToken) Then
                    result = HelpKeywords.Negate
                End If

                If node.OperatorToken.IsKind(SyntaxKind.AddressOfKeyword) Then
                    result = Keyword(SyntaxKind.AddressOfKeyword)
                End If
            End Sub

            Public Overrides Sub VisitUsingStatement(node As UsingStatementSyntax)
                result = Keyword(SyntaxKind.UsingKeyword)
            End Sub

            Public Overrides Sub VisitReDimStatement(node As ReDimStatementSyntax)
                result = HelpKeywords.Redim
            End Sub

            Public Overrides Sub VisitReturnStatement(node As ReturnStatementSyntax)
                result = Keyword(SyntaxKind.ReturnKeyword)
            End Sub

            Public Overrides Sub VisitRaiseEventStatement(node As RaiseEventStatementSyntax)
                result = Keyword(SyntaxKind.RaiseEventKeyword)
            End Sub

            Public Overrides Sub VisitThrowStatement(node As ThrowStatementSyntax)
                result = Keyword(SyntaxKind.ThrowKeyword)
            End Sub

            Public Overrides Sub VisitResumeStatement(node As ResumeStatementSyntax)
                result = Keyword(SyntaxKind.ResumeKeyword)
            End Sub

            Public Overrides Sub VisitPredefinedType(node As PredefinedTypeSyntax)
                result = Keyword(node.Keyword.ValueText)
            End Sub

            Public Overrides Sub VisitDocumentationCommentTrivia(node As DocumentationCommentTriviaSyntax)
                result = HelpKeywords.XmlDocComment
            End Sub

            Public Overrides Sub VisitLiteralExpression(node As LiteralExpressionSyntax)
                Select Case node.Token.Kind()
                    Case SyntaxKind.IntegerLiteralToken
                        Dim typeInfo = _semanticModel.GetTypeInfo(node, _cancellationToken).Type

                        If typeInfo IsNot Nothing Then
                            result = "vb." + typeInfo.ToDisplayString(TypeFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes))
                        End If
                    Case SyntaxKind.DecimalLiteralToken
                        result = Keyword(SyntaxKind.DecimalKeyword)
                    Case SyntaxKind.FloatingLiteralToken
                        result = Keyword(SyntaxKind.DoubleKeyword)
                End Select

                If result = Nothing Then
                    Select Case node.Kind()
                        Case SyntaxKind.CharacterLiteralExpression
                            result = Keyword(SyntaxKind.CharKeyword)
                        Case SyntaxKind.TrueLiteralExpression
                            result = Keyword(SyntaxKind.TrueKeyword)
                        Case SyntaxKind.FalseLiteralExpression
                            result = Keyword(SyntaxKind.FalseKeyword)
                        Case SyntaxKind.DateLiteralExpression
                            result = Keyword(SyntaxKind.DateKeyword)
                        Case SyntaxKind.StringLiteralExpression
                            result = Keyword(SyntaxKind.StringKeyword)
                        Case SyntaxKind.NothingLiteralExpression
                            result = Keyword(SyntaxKind.NothingKeyword)
                    End Select
                End If
            End Sub

            Public Overrides Sub VisitPartitionClause(node As PartitionClauseSyntax)
                If node.IsKind(SyntaxKind.SkipClause) Then
                    result = HelpKeywords.QuerySkip
                End If

                If node.IsKind(SyntaxKind.TakeClause) Then
                    result = HelpKeywords.QueryTake
                End If
            End Sub

            Public Overrides Sub VisitPartitionWhileClause(node As PartitionWhileClauseSyntax)
                If node.IsKind(SyntaxKind.SkipWhileClause) Then
                    result = HelpKeywords.QuerySkipWhile
                End If

                If node.IsKind(SyntaxKind.TakeWhileClause) Then
                    result = HelpKeywords.QueryTakeWhile
                End If
            End Sub

            Public Overrides Sub VisitWhereClause(node As WhereClauseSyntax)
                result = HelpKeywords.QueryWhere
            End Sub

            Public Overrides Sub VisitXmlCDataSection(node As XmlCDataSectionSyntax)
                result = HelpKeywords.XmlLiteralCdata
            End Sub

            Public Overrides Sub VisitXmlDocument(node As XmlDocumentSyntax)
                result = HelpKeywords.XmlLiteralDocument
            End Sub

            Public Overrides Sub VisitXmlComment(node As XmlCommentSyntax)
                result = HelpKeywords.XmlLiteralComment
            End Sub

            Public Overrides Sub VisitXmlElement(node As XmlElementSyntax)
                If node.GetAncestor(Of DocumentationCommentTriviaSyntax)() IsNot Nothing Then
                    result = HelpKeywords.XmlDocComment
                Else
                    result = HelpKeywords.XmlLiteralElement
                End If
            End Sub

            Public Overrides Sub VisitXmlEmbeddedExpression(node As XmlEmbeddedExpressionSyntax)
                result = HelpKeywords.XmlEmbeddedExpression
            End Sub

            Public Overrides Sub VisitXmlProcessingInstruction(node As XmlProcessingInstructionSyntax)
                result = HelpKeywords.XmlLiteralProcessingInstruction
            End Sub

            Private Function SelectModifier(list As SyntaxTokenList) As Boolean
                Dim modifier = list.FirstOrDefault(Function(t) t.Span.IntersectsWith(_span))
                If modifier <> Nothing Then
                    result = Keyword(modifier.Text)
                    Return True
                End If

                Return False
            End Function

            Public Overrides Sub VisitVariableDeclarator(node As VariableDeclaratorSyntax)
                Dim bestName = node.Names.FirstOrDefault(Function(n) n.Span.IntersectsWith(_span))
                If bestName Is Nothing Then
                    bestName = node.Names.FirstOrDefault()
                End If

                If bestName IsNot Nothing Then
                    Dim local = TryCast(_semanticModel.GetDeclaredSymbol(bestName, _cancellationToken), ILocalSymbol)
                    If local IsNot Nothing Then
                        If local.Type.IsAnonymousType Then
                            result = HelpKeywords.AnonymousType
                        Else
                            result = _service.FormatSymbol(local.Type)
                        End If
                    End If
                End If

            End Sub

            Public Overrides Sub VisitTypeParameterList(node As TypeParameterListSyntax)
                If node.OfKeyword.Span.IntersectsWith(_span) Then
                    result = Keyword(SyntaxKind.OfKeyword)
                End If
            End Sub

            Public Overrides Sub VisitGenericName(node As GenericNameSyntax)
                Dim symbol = _semanticModel.GetSymbolInfo(node, _cancellationToken).Symbol
                If symbol Is Nothing Then
                    symbol = _semanticModel.GetTypeInfo(node, _cancellationToken).Type
                End If

                If symbol IsNot Nothing Then
                    result = _service.FormatSymbol(symbol)
                End If

            End Sub

            Public Overrides Sub VisitQualifiedName(node As QualifiedNameSyntax)
                ' Bind the thing on the right
                Dim symbol = _semanticModel.GetSymbolInfo(node.Right, _cancellationToken).Symbol
                If symbol Is Nothing Then
                    symbol = _semanticModel.GetTypeInfo(node.Right, _cancellationToken).Type
                End If

                If symbol IsNot Nothing Then
                    result = _service.FormatSymbol(symbol)
                End If
            End Sub

            Public Overrides Sub VisitAsNewClause(node As AsNewClauseSyntax)
                result = Keyword(SyntaxKind.AsKeyword)
            End Sub

            Public Overrides Sub VisitAwaitExpression(node As AwaitExpressionSyntax)
                result = HelpKeywords.Await
            End Sub

            Public Overrides Sub VisitInvocationExpression(node As InvocationExpressionSyntax)
                Dim info = _semanticModel.GetSymbolInfo(node.Expression, _cancellationToken)

                ' Array indexing
                If info.Symbol IsNot Nothing Then
                    Dim symbolType = TryCast(info.Symbol.GetSymbolType(), IArrayTypeSymbol)
                    If symbolType IsNot Nothing Then
                        While symbolType.ElementType IsNot Nothing AndAlso TypeOf symbolType.ElementType Is IArrayTypeSymbol
                            symbolType = DirectCast(symbolType.ElementType, IArrayTypeSymbol)
                        End While

                        result = _service.FormatSymbol(symbolType.ElementType)
                        Return
                    End If
                End If

                result = Keyword(SyntaxKind.CallKeyword)
            End Sub

            Public Overrides Sub VisitXmlNamespaceImportsClause(node As XmlNamespaceImportsClauseSyntax)
                result = HelpKeywords.ImportsXmlns
            End Sub

            Public Overrides Sub VisitMemberAccessExpression(node As MemberAccessExpressionSyntax)
                If _span.Start <= node.OperatorToken.Span.Start Then
                    Visit(node.Expression)
                Else
                    Visit(node.Name)
                End If
            End Sub

            Public Overrides Sub VisitCTypeExpression(node As CTypeExpressionSyntax)
                result = Keyword(SyntaxKind.CTypeKeyword)
            End Sub

            Public Overrides Sub VisitNullableType(node As NullableTypeSyntax)
                result = HelpKeywords.Nullable
            End Sub

            Public Overrides Sub VisitXmlEmptyElement(node As XmlEmptyElementSyntax)
                result = HelpKeywords.XmlLiteralElement
            End Sub

            Public Overrides Sub VisitWhileStatement(node As WhileStatementSyntax)
                result = Keyword(SyntaxKind.WhileKeyword)
            End Sub

            Public Overrides Sub VisitImplementsClause(node As ImplementsClauseSyntax)
                result = HelpKeywords.ImplementsClause
            End Sub

            Public Overrides Sub VisitJoinCondition(node As JoinConditionSyntax)
                result = "vb.Equals"
            End Sub

            Public Overrides Sub VisitSelectClause(node As SelectClauseSyntax)
                result = HelpKeywords.QuerySelect
            End Sub

            Public Overrides Sub VisitCollectionRangeVariable(node As CollectionRangeVariableSyntax)
                If node.InKeyword.Span.IntersectsWith(_span) Then
                    If node.Parent.IsKind(SyntaxKind.GroupJoinClause) Then
                        result = HelpKeywords.QueryGroupJoinIn
                    End If
                End If
            End Sub

            Public Overrides Sub VisitOperatorStatement(node As OperatorStatementSyntax)
                If Not SelectModifier(node.Modifiers) Then
                    If node.OperatorToken.Span.IntersectsWith(_span) Then
                        result = Keyword(node.OperatorToken.ValueText)
                    End If

                    If node.DeclarationKeyword.Span.IntersectsWith(_span) Then
                        result = Keyword(SyntaxKind.OperatorKeyword)
                    End If
                End If
            End Sub

            Private Function TryGetDeclaredSymbol(token As SyntaxToken) As Boolean
                If _isNotMetadata Then
                    Return False
                End If

                If Not token.Span.IntersectsWith(_span) Then
                    Return False
                End If

                Dim symbol = _semanticModel.GetDeclaredSymbol(token.Parent, _cancellationToken)
                If symbol IsNot Nothing Then
                    result = _service.FormatSymbol(symbol)
                    Return True
                End If

                Return False
            End Function

        End Class
    End Class
End Namespace
