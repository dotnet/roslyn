// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineHints;

namespace Microsoft.CodeAnalysis.CSharp.InlineHints
{
    [ExportLanguageService(typeof(IInlineTypeHintsService), LanguageNames.CSharp), Shared]
    internal class CSharpInlineTypeHintsService : AbstractInlineTypeHintsService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpInlineTypeHintsService()
        {
        }

        protected override InlineTypeHint? TryGetTypeHint(
            SemanticModel semanticModel,
            SyntaxNode node,
            bool forImplicitVariableTypes,
            bool forLambdaParameterTypes,
            CancellationToken cancellationToken)
        {
            if (forImplicitVariableTypes)
            {
                if (node is VariableDeclarationSyntax variableDeclaration &&
                    variableDeclaration.Type.IsVar &&
                    variableDeclaration.Variables.Count == 1 &&
                    !variableDeclaration.Variables[0].Identifier.IsMissing)
                {
                    var type = semanticModel.GetTypeInfo(variableDeclaration.Type, cancellationToken).Type;
                    if (IsValidType(type))
                        return new InlineTypeHint(type, variableDeclaration.Variables[0].Identifier.SpanStart);
                }
                else if (node is SingleVariableDesignationSyntax { Parent: not DeclarationPatternSyntax } variableDesignation)
                {
                    var local = semanticModel.GetDeclaredSymbol(variableDesignation, cancellationToken) as ILocalSymbol;
                    var type = local?.Type;
                    if (IsValidType(type))
                        return new InlineTypeHint(type, variableDesignation.Identifier.SpanStart);
                }
                else if (node is ForEachStatementSyntax forEachStatement &&
                         forEachStatement.Type.IsVar)
                {
                    var info = semanticModel.GetForEachStatementInfo(forEachStatement);
                    var type = info.ElementType;
                    if (IsValidType(type))
                        return new InlineTypeHint(type, forEachStatement.Identifier.SpanStart);
                }
            }

            if (forLambdaParameterTypes)
            {
                if (node is SimpleLambdaExpressionSyntax simpleLambda)
                {
                    var parameter = semanticModel.GetDeclaredSymbol(simpleLambda.Parameter, cancellationToken);
                    if (IsValidType(parameter?.Type))
                        return new InlineTypeHint(parameter.Type, simpleLambda.Parameter.Identifier.SpanStart);
                }
                else if (node is ParameterSyntax { Type: null } parameterNode)
                {
                    var parameter = semanticModel.GetDeclaredSymbol(parameterNode, cancellationToken);
                    if (IsValidType(parameter?.Type))
                        return new InlineTypeHint(parameter.Type, parameterNode.Identifier.SpanStart);
                }
            }

            return null;
        }

        private static bool IsValidType([NotNullWhen(true)] ITypeSymbol? type)
        {
            return type is not null or IErrorTypeSymbol && type.Name != "var";
        }
    }
}
