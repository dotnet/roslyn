// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Globalization
{
    public abstract class CA1309CodeFixProviderBase : CodeFixProviderBase
    {
        public sealed override IEnumerable<string> GetFixableDiagnosticIds()
        {
            return SpecializedCollections.SingletonEnumerable(CA1309DiagnosticAnalyzer.RuleId);
        }

        protected sealed override string GetCodeFixDescription(string ruleId)
        {
            return FxCopFixersResources.StringComparisonShouldBeOrdinalOrOrdinalIgnoreCase;
        }

        internal SyntaxNode CreateEqualsExpression(ISyntaxFactoryService syntaxFactoryService, SemanticModel model, SyntaxNode operand1, SyntaxNode operand2, bool isEquals)
        {
            var stringType = model.Compilation.GetSpecialType(SpecialType.System_String);
            var memberAccess = syntaxFactoryService.CreateMemberAccessExpression(
                        syntaxFactoryService.CreateTypeReferenceExpression(stringType),
                        syntaxFactoryService.CreateIdentifierName(CA1309DiagnosticAnalyzer.EqualsMethodName));
            var ordinal = CreateOrdinalMemberAccess(syntaxFactoryService, model);
            var invocation = syntaxFactoryService.CreateInvocationExpression(
                memberAccess,
                operand1,
                operand2.WithTrailingTrivia(),
                ordinal)
                .WithAdditionalAnnotations(Formatter.Annotation);
            if (!isEquals)
            {
                invocation = syntaxFactoryService.CreateLogicalNotExpression(invocation);
            }

            invocation = invocation.WithTrailingTrivia(operand2.GetTrailingTrivia());

            return invocation;
        }

        internal SyntaxNode CreateOrdinalMemberAccess(ISyntaxFactoryService syntaxFactoryService, SemanticModel model)
        {
            var stringComparisonType = WellKnownTypes.StringComparison(model.Compilation);
            return syntaxFactoryService.CreateMemberAccessExpression(
                syntaxFactoryService.CreateTypeReferenceExpression(stringComparisonType),
                syntaxFactoryService.CreateIdentifierName(CA1309DiagnosticAnalyzer.OrdinalText));
        }

        protected bool CanAddStringComparison(IMethodSymbol methodSymbol)
        {
            var parameters = methodSymbol.Parameters;
            switch (methodSymbol.Name)
            {
                case CA1309DiagnosticAnalyzer.EqualsMethodName:
                    // can fix .Equals() with (string), (string, string)
                    switch (parameters.Length)
                    {
                        case 1:
                            return parameters[0].Type.SpecialType == SpecialType.System_String;
                        case 2:
                            return parameters[0].Type.SpecialType == SpecialType.System_String &&
                                parameters[1].Type.SpecialType == SpecialType.System_String;
                    }

                    break;
                case CA1309DiagnosticAnalyzer.CompareMethodName:
                    // can fix .Compare() with (string, string), (string, int, string, int, int)
                    switch (parameters.Length)
                    {
                        case 2:
                            return parameters[0].Type.SpecialType == SpecialType.System_String &&
                                parameters[1].Type.SpecialType == SpecialType.System_String;
                        case 5:
                            return parameters[0].Type.SpecialType == SpecialType.System_String &&
                                parameters[1].Type.SpecialType == SpecialType.System_Int32 &&
                                parameters[2].Type.SpecialType == SpecialType.System_String &&
                                parameters[3].Type.SpecialType == SpecialType.System_Int32 &&
                                parameters[4].Type.SpecialType == SpecialType.System_Int32;
                    }

                    break;
            }

            return false;
        }
    }
}
