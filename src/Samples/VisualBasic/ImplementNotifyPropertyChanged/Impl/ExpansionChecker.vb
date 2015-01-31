' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Friend Class ExpansionChecker
    Friend Shared Function GetExpandableProperties(span As TextSpan, root As SyntaxNode, model As SemanticModel) As IEnumerable(Of ExpandablePropertyInfo)
        Dim propertiesInTypes = root.DescendantNodes(span) _
            .OfType(Of PropertyStatementSyntax) _
            .Select(Function(p) GetExpandablePropertyInfo(p, model)) _
            .Where(Function(p) p IsNot Nothing) _
            .GroupBy(Function(p) p.PropertyDeclaration.FirstAncestorOrSelf(Of TypeBlockSyntax))

        Return If(propertiesInTypes.Any(),
            propertiesInTypes.First(),
            Enumerable.Empty(Of ExpandablePropertyInfo))
    End Function

    ''' <summary> Returns true if the specified <see cref="PropertyBlockSyntax"/>  can be expanded to
    ''' include support for <see cref="INotifyPropertyChanged"/> . </summary>
    Friend Shared Function GetExpandablePropertyInfo(propertyStatement As PropertyStatementSyntax, model As SemanticModel) As ExpandablePropertyInfo
        If propertyStatement.ContainsDiagnostics Then
            Return Nothing
        End If

        If propertyStatement.Modifiers.Any(SyntaxKind.SharedKeyword) OrElse
           propertyStatement.Modifiers.Any(SyntaxKind.MustOverrideKeyword) Then
            Return Nothing
        End If

        If propertyStatement.AsClause Is Nothing Then
            Return Nothing
        End If

        Dim propertyBlock = TryCast(propertyStatement.Parent, PropertyBlockSyntax)
        If propertyBlock Is Nothing Then
            ' We're an auto property, we can be expanded.
            Return New ExpandablePropertyInfo With
            {
                .PropertyDeclaration = propertyStatement,
                .BackingFieldName = GenerateFieldName(propertyStatement, model),
                .NeedsBackingField = True,
                .Type = model.GetDeclaredSymbol(propertyStatement).Type
            }
        End If

        ' Not an auto property, look more closely.
        If propertyBlock.ContainsDiagnostics Then
            Return Nothing
        End If

        ' Only expand properties with both a getter and a setter.
        Dim getter As AccessorBlockSyntax = Nothing
        Dim setter As AccessorBlockSyntax = Nothing
        If Not TryGetAccessors(propertyBlock, getter, setter) Then
            Return Nothing
        End If

        Dim backingField As IFieldSymbol = Nothing
        Return If(IsExpandableGetter(getter, model, backingField) AndAlso IsExpandableSetter(setter, model, backingField),
            New ExpandablePropertyInfo With {.PropertyDeclaration = propertyBlock, .BackingFieldName = backingField.Name},
            Nothing)
    End Function

    ''' <summary> Retrieves the get and set accessor declarations of the specified property.
    ''' Returns true if both get and set accessors exist; otherwise false. </summary>
    Friend Shared Function TryGetAccessors(propertyBlock As PropertyBlockSyntax,
                                    ByRef getter As AccessorBlockSyntax,
                                    ByRef setter As AccessorBlockSyntax) As Boolean
        Dim accessors = propertyBlock.Accessors
        getter = accessors.FirstOrDefault(Function(ad) ad.AccessorStatement.Kind() = SyntaxKind.GetAccessorStatement)
        setter = accessors.FirstOrDefault(Function(ad) ad.AccessorStatement.Kind() = SyntaxKind.SetAccessorStatement)
        Return getter IsNot Nothing AndAlso setter IsNot Nothing
    End Function

    Private Shared Function IsExpandableGetter(getter As AccessorBlockSyntax,
                                       semanticModel As SemanticModel,
                                       ByRef backingField As IFieldSymbol) As Boolean
        backingField = GetBackingFieldFromGetter(getter, semanticModel)
        Return backingField IsNot Nothing
    End Function

    Private Shared Function GetBackingFieldFromGetter(getter As AccessorBlockSyntax, semanticModel As SemanticModel) As IFieldSymbol
        If Not getter.Statements.Any() Then
            Return Nothing
        End If

        Dim statements = getter.Statements
        If statements.Count <> 1 Then
            Return Nothing
        End If

        Dim returnStatement = TryCast(statements.Single(), ReturnStatementSyntax)
        If returnStatement Is Nothing OrElse returnStatement.Expression Is Nothing Then
            Return Nothing
        End If

        Return TryCast(semanticModel.GetSymbolInfo(returnStatement.Expression).Symbol, IFieldSymbol)
    End Function

    Private Shared Function GenerateFieldName(propertyStatement As PropertyStatementSyntax, semanticModel As SemanticModel) As String
        Dim baseName = propertyStatement.Identifier.ValueText
        baseName = "_" & Char.ToLower(baseName(0)) & baseName.Substring(1)
        Dim propertySymbol = TryCast(semanticModel.GetDeclaredSymbol(propertyStatement), IPropertySymbol)
        If propertySymbol Is Nothing OrElse propertySymbol.ContainingType Is Nothing Then
            Return baseName
        End If

        Dim index = 0
        Dim name = baseName
        While DirectCast(propertySymbol.ContainingType, INamedTypeSymbol).MemberNames.Contains(name, StringComparer.OrdinalIgnoreCase)
            name = baseName & index.ToString()
            index += 1
        End While

        Return name
    End Function

    Private Shared Function IsExpandableSetter(setter As AccessorBlockSyntax,
                                        semanticModel As SemanticModel,
                                        backingField As IFieldSymbol) As Boolean
        Return IsExpandableSetterPattern1(setter, backingField, semanticModel) OrElse
            IsExpandableSetterPattern2(setter, backingField, semanticModel) OrElse
            IsExpandableSetterPattern3(setter, backingField, semanticModel)
    End Function

    Private Shared Function IsExpandableSetterPattern1(setter As AccessorBlockSyntax,
                                                backingField As IFieldSymbol,
                                                semanticModel As SemanticModel) As Boolean
        Dim statements = setter.Statements
        If statements.Count <> 1 Then
            Return False
        End If

        Dim expressionStatement = statements.First()
        Return IsAssignmentOfPropertyValueParameterToBackingField(expressionStatement, backingField, semanticModel)
    End Function

    Private Shared Function IsExpandableSetterPattern2(setter As AccessorBlockSyntax,
                                                backingField As IFieldSymbol,
                                                semanticModel As SemanticModel) As Boolean
        Dim statements = setter.Statements
        If statements.Count <> 1 Then
            Return False
        End If

        Dim statement As StatementSyntax = Nothing
        Dim condition As ExpressionSyntax = Nothing

        If Not GetConditionAndSingleStatementFromIfStatement(statements(0), statement, condition) Then
            Return False
        End If

        If Not IsAssignmentOfPropertyValueParameterToBackingField(statement, backingField, semanticModel) Then
            Return False
        End If

        If condition Is Nothing OrElse condition.Kind() <> SyntaxKind.NotEqualsExpression Then
            Return False
        End If

        Return ComparesPropertyValueParameterAndBackingField(DirectCast(condition, BinaryExpressionSyntax),
                                                             backingField,
                                                             semanticModel)
    End Function

    Private Shared Function IsExpandableSetterPattern3(setter As AccessorBlockSyntax,
                                                backingField As IFieldSymbol,
                                                semanticModel As SemanticModel) As Boolean
        Dim statements = setter.Statements
        If statements.Count <> 2 Then
            Return False
        End If

        Dim statement As StatementSyntax = Nothing
        Dim condition As ExpressionSyntax = Nothing

        If Not GetConditionAndSingleStatementFromIfStatement(statements(0), statement, condition) Then
            Return False
        End If

        Dim returnStatement = TryCast(statement, ReturnStatementSyntax)
        If returnStatement Is Nothing OrElse returnStatement.Expression IsNot Nothing Then
            Return False
        End If

        If Not IsAssignmentOfPropertyValueParameterToBackingField(statements(1), backingField, semanticModel) Then
            Return False
        End If

        If condition.Kind() <> SyntaxKind.EqualsExpression Then
            Return False
        End If

        Return ComparesPropertyValueParameterAndBackingField(DirectCast(condition, BinaryExpressionSyntax),
                                                             backingField,
                                                             semanticModel)
    End Function

    Private Shared Function IsAssignmentOfPropertyValueParameterToBackingField(statement As StatementSyntax,
                                                                        backingField As IFieldSymbol, semanticModel As SemanticModel) As Boolean
        If statement.Kind() <> SyntaxKind.SimpleAssignmentStatement Then
            Return False
        End If

        Dim assignment = DirectCast(statement, AssignmentStatementSyntax)
        Return IsBackingField(assignment.Left, backingField, semanticModel) AndAlso IsPropertyValueParameter(assignment.Right, semanticModel)
    End Function

    Private Shared Function GetConditionAndSingleStatementFromIfStatement(ifStatement As StatementSyntax,
                                                                   ByRef statement As StatementSyntax,
                                                                   ByRef condition As ExpressionSyntax) As Boolean
        Dim multiLineIfStatement = TryCast(ifStatement, MultiLineIfBlockSyntax)
        If multiLineIfStatement IsNot Nothing Then
            If multiLineIfStatement.Statements.Count <> 1 Then
                Return False
            End If

            statement = multiLineIfStatement.Statements.First()
            condition = multiLineIfStatement.IfStatement.Condition
            Return True
        Else
            Dim singleLineIfStatement = TryCast(ifStatement, SingleLineIfStatementSyntax)
            If singleLineIfStatement IsNot Nothing Then
                If singleLineIfStatement.Statements.Count <> 1 Then
                    Return False
                End If

                statement = singleLineIfStatement.Statements.First()
                condition = singleLineIfStatement.Condition
                Return True
            End If
            Return False
        End If
    End Function

    Private Shared Function IsBackingField(expression As ExpressionSyntax,
                                    backingField As IFieldSymbol,
                                    semanticModel As SemanticModel) As Boolean
        Return Object.Equals(semanticModel.GetSymbolInfo(expression).Symbol, backingField)
    End Function

    Private Shared Function IsPropertyValueParameter(expression As ExpressionSyntax,
                                              semanticModel As SemanticModel) As Boolean
        Dim symbol = semanticModel.GetSymbolInfo(expression).Symbol

        Return symbol IsNot Nothing AndAlso
            symbol.Kind = SymbolKind.Parameter AndAlso
            symbol.ContainingSymbol.Kind = SymbolKind.Method AndAlso
            DirectCast(symbol.ContainingSymbol, IMethodSymbol).MethodKind = MethodKind.PropertySet
    End Function

    Private Shared Function ComparesPropertyValueParameterAndBackingField(expression As BinaryExpressionSyntax,
                                                                   backingField As IFieldSymbol,
                                                                   semanticModel As SemanticModel) As Boolean

        Return (IsPropertyValueParameter(expression.Right, semanticModel) AndAlso IsBackingField(expression.Left, backingField, semanticModel)) OrElse
            (IsPropertyValueParameter(expression.Left, semanticModel) AndAlso IsBackingField(expression.Right, backingField, semanticModel))
    End Function

End Class
