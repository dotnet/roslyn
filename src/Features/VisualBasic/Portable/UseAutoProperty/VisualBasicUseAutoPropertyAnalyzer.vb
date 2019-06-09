' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.UseAutoProperty
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseAutoProperty
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicUseAutoPropertyAnalyzer
        Inherits AbstractUseAutoPropertyAnalyzer(Of PropertyBlockSyntax, FieldDeclarationSyntax, ModifiedIdentifierSyntax, ExpressionSyntax)

        Protected Overrides Function SupportsReadOnlyProperties(compilation As Compilation) As Boolean
            Return DirectCast(compilation, VisualBasicCompilation).LanguageVersion >= LanguageVersion.VisualBasic14
        End Function

        Protected Overrides Function SupportsPropertyInitializer(compilation As Compilation) As Boolean
            Return DirectCast(compilation, VisualBasicCompilation).LanguageVersion >= LanguageVersion.VisualBasic10
        End Function

        Protected Overrides Function CanExplicitInterfaceImplementationsBeFixed() As Boolean
            Return True
        End Function

        Protected Overrides Sub AnalyzeCompilationUnit(context As SemanticModelAnalysisContext, root As SyntaxNode, analysisResults As List(Of AnalysisResult))
            AnalyzeMembers(context, DirectCast(root, CompilationUnitSyntax).Members, analysisResults)
        End Sub

        Private Sub AnalyzeMembers(context As SemanticModelAnalysisContext,
                                   members As SyntaxList(Of StatementSyntax),
                                   analysisResults As List(Of AnalysisResult))
            For Each member In members
                AnalyzeMember(context, member, analysisResults)
            Next
        End Sub

        Private Sub AnalyzeMember(context As SemanticModelAnalysisContext,
                                  member As StatementSyntax,
                                  analysisResults As List(Of AnalysisResult))

            If member.Kind() = SyntaxKind.NamespaceBlock Then
                Dim namespaceBlock = DirectCast(member, NamespaceBlockSyntax)
                AnalyzeMembers(context, namespaceBlock.Members, analysisResults)
            End If

            ' If we have a class or struct or module, recurse inwards.
            If member.IsKind(SyntaxKind.ClassBlock) OrElse
               member.IsKind(SyntaxKind.StructureBlock) OrElse
               member.IsKind(SyntaxKind.ModuleBlock) Then

                Dim typeBlock = DirectCast(member, TypeBlockSyntax)
                AnalyzeMembers(context, typeBlock.Members, analysisResults)
            End If

            Dim propertyDeclaration = TryCast(member, PropertyBlockSyntax)
            If propertyDeclaration IsNot Nothing Then
                AnalyzeProperty(context, propertyDeclaration, analysisResults)
            End If
        End Sub

        Protected Overrides Sub RegisterIneligibleFieldsAction(analysisResults As List(Of AnalysisResult), ineligibleFields As HashSet(Of IFieldSymbol), compilation As Compilation, cancellationToken As CancellationToken)
            ' There are no syntactic constructs that make a field ineligible to be replaced with 
            ' a property.  In C# you can't use a property in a ref/out position.  But that restriction
            ' doesn't apply to VB.
        End Sub

        Protected Overrides Function CanConvert(prop As IPropertySymbol) As Boolean
            ' VB auto props cannot specify accessibility for accessors.  So if the original
            ' code had different accessibility between the accessors and property then we 
            ' can't convert it.

            If prop.GetMethod IsNot Nothing AndAlso
               prop.GetMethod.DeclaredAccessibility <> prop.DeclaredAccessibility Then
                Return False
            End If

            If prop.SetMethod IsNot Nothing AndAlso
               prop.SetMethod.DeclaredAccessibility <> prop.DeclaredAccessibility Then
                Return False
            End If

            Return True
        End Function

        Protected Overrides Function GetFieldInitializer(variable As ModifiedIdentifierSyntax, cancellationToken As CancellationToken) As ExpressionSyntax
            Dim declarator = TryCast(variable.Parent, VariableDeclaratorSyntax)
            Return declarator?.Initializer?.Value
        End Function

        Private Function CheckExpressionSyntactically(expression As ExpressionSyntax) As Boolean
            If expression.IsKind(SyntaxKind.SimpleMemberAccessExpression) Then
                Dim memberAccessExpression = DirectCast(expression, MemberAccessExpressionSyntax)
                Return memberAccessExpression.Expression.Kind() = SyntaxKind.MeExpression AndAlso
                    memberAccessExpression.Name.Kind() = SyntaxKind.IdentifierName
            ElseIf expression.IsKind(SyntaxKind.IdentifierName) Then
                Return True
            End If

            Return False
        End Function

        Protected Overrides Function GetGetterExpression(getMethod As IMethodSymbol, cancellationToken As CancellationToken) As ExpressionSyntax
            ' Getter has to be of the form:
            '
            '     Get
            '         Return field
            '     End Get
            ' or
            '     Get
            '         Return Me.field
            '     End Get
            Dim accessor = TryCast(TryCast(getMethod.DeclaringSyntaxReferences(0).GetSyntax(cancellationToken), AccessorStatementSyntax)?.Parent, AccessorBlockSyntax)
            Dim statements = accessor?.Statements
            If statements?.Count = 1 Then
                Dim statement = statements.Value(0)
                If statement.Kind() = SyntaxKind.ReturnStatement Then
                    Dim expr = DirectCast(statement, ReturnStatementSyntax).Expression
                    Return If(CheckExpressionSyntactically(expr), expr, Nothing)
                End If
            End If

            Return Nothing
        End Function

        Protected Overrides Function GetSetterExpression(setMethod As IMethodSymbol, semanticModel As SemanticModel, cancellationToken As CancellationToken) As ExpressionSyntax
            ' Setter has to be of the form:
            '
            '     Set(value)
            '         field = value
            '     End Set
            ' or
            '     Set(value)
            '         Me.field = value
            '     End Set
            Dim setAccessor = TryCast(TryCast(setMethod.DeclaringSyntaxReferences(0).GetSyntax(cancellationToken), AccessorStatementSyntax)?.Parent, AccessorBlockSyntax)
            Dim statements = setAccessor?.Statements
            If statements?.Count = 1 Then
                Dim statement = statements.Value(0)
                If statement.IsKind(SyntaxKind.SimpleAssignmentStatement) Then
                    Dim assignmentStatement = DirectCast(statement, AssignmentStatementSyntax)
                    If assignmentStatement.Right.Kind() = SyntaxKind.IdentifierName Then
                        Dim identifier = DirectCast(assignmentStatement.Right, IdentifierNameSyntax)
                        Dim symbol = semanticModel.GetSymbolInfo(identifier).Symbol
                        If setMethod.Parameters.Contains(TryCast(symbol, IParameterSymbol)) Then
                            Return If(CheckExpressionSyntactically(assignmentStatement.Left), assignmentStatement.Left, Nothing)
                        End If
                    End If
                End If
            End If

            Return Nothing
        End Function

        Protected Overrides Function GetNodeToFade(fieldDeclaration As FieldDeclarationSyntax, identifier As ModifiedIdentifierSyntax) As SyntaxNode
            Return Utilities.GetNodeToRemove(identifier)
        End Function

        Protected Overrides Function IsEligibleHeuristic(field As IFieldSymbol, propertyDeclaration As PropertyBlockSyntax, compilation As Compilation, cancellationToken As CancellationToken) As Boolean
            If propertyDeclaration.Accessors.Any(SyntaxKind.SetAccessorBlock) Then
                ' If this property already has a setter, then we can definitely simplify it to an auto-prop 
                Return True
            End If

            ' the property doesn't have a setter currently. check all the types the field is 
            ' declared in.  If the field is written to outside of a constructor, then this 
            ' field Is Not eligible for replacement with an auto prop.  We'd have to make 
            ' the autoprop read/write, And that could be opening up the property widely 
            ' (in accessibility terms) in a way the user would not want.
            Dim containingType = field.ContainingType
            For Each ref In containingType.DeclaringSyntaxReferences
                Dim containingNode = ref.GetSyntax(cancellationToken)?.Parent
                If containingNode IsNot Nothing Then
                    Dim semanticModel = compilation.GetSemanticModel(containingNode.SyntaxTree)
                    If IsWrittenOutsideOfConstructorOrProperty(field, propertyDeclaration, containingNode, semanticModel, cancellationToken) Then
                        Return False
                    End If
                End If
            Next

            ' No problem simplifying this field.
            Return True
        End Function

        Private Function IsWrittenOutsideOfConstructorOrProperty(field As IFieldSymbol,
                                                                 propertyDeclaration As PropertyBlockSyntax,
                                                                 node As SyntaxNode,
                                                                 semanticModel As SemanticModel,
                                                                 cancellationToken As CancellationToken) As Boolean
            cancellationToken.ThrowIfCancellationRequested()

            If node Is propertyDeclaration Then
                Return False
            End If

            If node.Kind() = SyntaxKind.ConstructorBlock Then
                Return False
            End If

            If node.Kind() = SyntaxKind.IdentifierName Then
                Dim symbolInfo = semanticModel.GetSymbolInfo(node)
                If field.Equals(symbolInfo.Symbol) Then
                    If VisualBasicSemanticFactsService.Instance.IsWrittenTo(semanticModel, node, cancellationToken) Then
                        Return True
                    End If
                End If
            Else
                For Each child In node.ChildNodesAndTokens
                    If child.IsNode Then
                        If IsWrittenOutsideOfConstructorOrProperty(field, propertyDeclaration, child.AsNode(), semanticModel, cancellationToken) Then
                            Return True
                        End If
                    End If
                Next
            End If

            Return False
        End Function
    End Class
End Namespace
