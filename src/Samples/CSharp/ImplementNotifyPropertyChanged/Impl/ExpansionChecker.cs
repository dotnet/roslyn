// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ImplementNotifyPropertyChangedCS
{
    internal static class ExpansionChecker
    {
        internal static IEnumerable<ExpandablePropertyInfo> GetExpandableProperties(TextSpan span, SyntaxNode root, SemanticModel model)
        {
            var propertiesInTypes = root.DescendantNodes(span)
                       .OfType<PropertyDeclarationSyntax>()
                       .Select(p => GetExpandablePropertyInfo(p, model))
                       .Where(p => p != null)
                       .GroupBy(p => p.PropertyDeclaration.FirstAncestorOrSelf<TypeDeclarationSyntax>());

            return propertiesInTypes.Any()
                ? propertiesInTypes.First()
                : Enumerable.Empty<ExpandablePropertyInfo>();
        }

        /// <summary>
        /// Returns true if the specified <see cref="PropertyDeclarationSyntax"/> can be expanded to include
        /// support for <see cref="INotifyPropertyChanged"/>.
        /// </summary>
        internal static ExpandablePropertyInfo GetExpandablePropertyInfo(PropertyDeclarationSyntax property, SemanticModel model)
        {
            // Don't expand properties with parse errors.
            if (property.ContainsDiagnostics)
            {
                return null;
            }

            if (property.Modifiers.Any(SyntaxKind.StaticKeyword) || property.Modifiers.Any(SyntaxKind.AbstractKeyword))
            {
                return null;
            }

            AccessorDeclarationSyntax getter, setter;
            if (!TryGetAccessors(property, out getter, out setter))
            {
                return null;
            }

            if (getter.Body == null && setter.Body == null)
            {
                return new ExpandablePropertyInfo
                {
                    PropertyDeclaration = property,
                    BackingFieldName = GenerateFieldName(property, model),
                    NeedsBackingField = true,
                    Type = model.GetDeclaredSymbol(property).Type
                };
            }

            IFieldSymbol backingField;
            return (TryGetBackingFieldFromExpandableGetter(getter, model, out backingField)
                && IsExpandableSetter(setter, model, backingField))
                ? new ExpandablePropertyInfo { PropertyDeclaration = property, BackingFieldName = backingField.Name }
                : null;
        }

        /// <summary>
        /// Retrieves the get and set accessor declarations of the specified property.
        /// Returns true if both get and set accessors exist; otherwise false.
        /// </summary>
        internal static bool TryGetAccessors(
            PropertyDeclarationSyntax property,
            out AccessorDeclarationSyntax getter,
            out AccessorDeclarationSyntax setter)
        {
            var accessors = property.AccessorList.Accessors;
            getter = accessors.FirstOrDefault(ad => ad.Kind() == SyntaxKind.GetAccessorDeclaration);
            setter = accessors.FirstOrDefault(ad => ad.Kind() == SyntaxKind.SetAccessorDeclaration);

            return accessors.Count == 2 && getter != null && setter != null;
        }

        private static string GenerateFieldName(PropertyDeclarationSyntax property, SemanticModel semanticModel)
        {
            var baseName = property.Identifier.ValueText;
            baseName = char.ToLower(baseName[0]).ToString() + baseName.Substring(1);

            var propertySymbol = semanticModel.GetDeclaredSymbol(property);
            if (propertySymbol == null ||
                propertySymbol.ContainingType == null)
            {
                return baseName;
            }

            var index = 0;
            var name = baseName;
            while (propertySymbol.ContainingType.MemberNames.Contains(name))
            {
                name = baseName + (++index).ToString();
            }

            return name;
        }

        private static IFieldSymbol GetBackingFieldFromGetter(
            AccessorDeclarationSyntax getter,
            SemanticModel semanticModel)
        {
            // The getter should have a body containing a single return of a backing field.

            if (getter.Body == null)
            {
                return null;
            }

            var statements = getter.Body.Statements;
            if (statements.Count != 1)
            {
                return null;
            }

            var returnStatement = statements.Single() as ReturnStatementSyntax;
            if (returnStatement == null || returnStatement.Expression == null)
            {
                return null;
            }

            return semanticModel.GetSymbolInfo(returnStatement.Expression).Symbol as IFieldSymbol;
        }

        private static bool TryGetBackingFieldFromExpandableGetter(
            AccessorDeclarationSyntax getter,
            SemanticModel semanticModel,
            out IFieldSymbol backingField)
        {
            backingField = GetBackingFieldFromGetter(getter, semanticModel);
            return backingField != null;
        }

        private static bool IsBackingField(ExpressionSyntax expression, IFieldSymbol backingField, SemanticModel semanticModel)
        {
            return semanticModel
                .GetSymbolInfo(expression).Symbol
                .Equals(backingField);
        }

        private static bool IsPropertyValueParameter(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            var parameterSymbol = semanticModel.GetSymbolInfo(expression).Symbol as IParameterSymbol;

            return parameterSymbol != null
                && parameterSymbol.IsImplicitlyDeclared
                && parameterSymbol.Name == "value";
        }

        private static bool IsAssignmentOfPropertyValueParameterToBackingField(
            ExpressionSyntax expression,
            IFieldSymbol backingField,
            SemanticModel semanticModel)
        {
            if (expression.Kind() != SyntaxKind.SimpleAssignmentExpression)
            {
                return false;
            }

            var assignment = (AssignmentExpressionSyntax)expression;

            return IsBackingField(assignment.Left, backingField, semanticModel)
                && IsPropertyValueParameter(assignment.Right, semanticModel);
        }

        private static bool ComparesPropertyValueParameterAndBackingField(
            BinaryExpressionSyntax expression,
            IFieldSymbol backingField,
            SemanticModel semanticModel)
        {
            return (IsPropertyValueParameter(expression.Right, semanticModel) && IsBackingField(expression.Left, backingField, semanticModel))
                || (IsPropertyValueParameter(expression.Left, semanticModel) && IsBackingField(expression.Right, backingField, semanticModel));
        }

        private static bool IsExpandableSetter(AccessorDeclarationSyntax setter, SemanticModel semanticModel, IFieldSymbol backingField)
        {
            // The setter should have a body containing one of the following heuristic patterns or
            // no body at all.
            //
            // Patterns:
            //    field = value;
            //    if (field != value) field = value;
            //    if (field == value) return; field = value;

            if (setter.Body == null)
            {
                return false;
            }

            return IsExpandableSetterPattern1(setter, backingField, semanticModel)
                || IsExpandableSetterPattern2(setter, backingField, semanticModel)
                || IsExpandableSetterPattern3(setter, backingField, semanticModel);
        }

        private static bool IsExpandableSetterPattern1(
            AccessorDeclarationSyntax setter,
            IFieldSymbol backingField,
            SemanticModel semanticModel)
        {
            // Pattern: field = value
            Debug.Assert(setter.Body != null);

            var statements = setter.Body.Statements;
            if (statements.Count != 1)
            {
                return false;
            }

            var expressionStatement = statements[0] as ExpressionStatementSyntax;
            return expressionStatement != null
                && IsAssignmentOfPropertyValueParameterToBackingField(expressionStatement.Expression, backingField, semanticModel);
        }

        private static bool IsExpandableSetterPattern2(
            AccessorDeclarationSyntax setter,
            IFieldSymbol backingField,
            SemanticModel semanticModel)
        {
            // Pattern: if (field != value) field = value;
            Debug.Assert(setter.Body != null);

            var statements = setter.Body.Statements;
            if (statements.Count != 1)
            {
                return false;
            }

            var ifStatement = statements[0] as IfStatementSyntax;
            if (ifStatement == null)
            {
                return false;
            }

            var statement = ifStatement.Statement;
            if (statement is BlockSyntax)
            {
                var blockStatements = ((BlockSyntax)statement).Statements;
                if (blockStatements.Count != 1)
                {
                    return false;
                }

                statement = blockStatements[0];
            }

            var expressionStatement = statement as ExpressionStatementSyntax;
            if (expressionStatement == null)
            {
                return false;
            }

            if (!IsAssignmentOfPropertyValueParameterToBackingField(expressionStatement.Expression, backingField, semanticModel))
            {
                return false;
            }

            var condition = ifStatement.Condition as BinaryExpressionSyntax;
            if (condition == null ||
                condition.Kind() != SyntaxKind.NotEqualsExpression)
            {
                return false;
            }

            return ComparesPropertyValueParameterAndBackingField(condition, backingField, semanticModel);
        }

        private static bool IsExpandableSetterPattern3(
            AccessorDeclarationSyntax setter,
            IFieldSymbol backingField,
            SemanticModel semanticModel)
        {
            // Pattern: if (field == value) return; field = value;

            Debug.Assert(setter.Body != null);

            var statements = setter.Body.Statements;
            if (statements.Count != 2)
            {
                return false;
            }

            var ifStatement = statements[0] as IfStatementSyntax;
            if (ifStatement == null)
            {
                return false;
            }

            var statement = ifStatement.Statement;
            if (statement is BlockSyntax)
            {
                var blockStatements = ((BlockSyntax)statement).Statements;
                if (blockStatements.Count != 1)
                {
                    return false;
                }

                statement = blockStatements[0];
            }

            var returnStatement = statement as ReturnStatementSyntax;
            if (returnStatement == null ||
                returnStatement.Expression != null)
            {
                return false;
            }

            var expressionStatement = statements[1] as ExpressionStatementSyntax;
            if (expressionStatement == null)
            {
                return false;
            }

            if (!IsAssignmentOfPropertyValueParameterToBackingField(expressionStatement.Expression, backingField, semanticModel))
            {
                return false;
            }

            var condition = ifStatement.Condition as BinaryExpressionSyntax;
            if (condition == null ||
                condition.Kind() != SyntaxKind.EqualsExpression)
            {
                return false;
            }

            return ComparesPropertyValueParameterAndBackingField(condition, backingField, semanticModel);
        }
    }
}
