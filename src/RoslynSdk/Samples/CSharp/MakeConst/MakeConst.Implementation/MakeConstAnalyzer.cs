// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MakeConst
{
    // Implementing syntax node analyzer because the make const diagnostics in one method body are not dependent on the contents of other method bodies.
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MakeConstAnalyzer : DiagnosticAnalyzer
    {
        public const string MakeConstDiagnosticId = "MakeConst";

        public static readonly DiagnosticDescriptor MakeConstRule =
            new DiagnosticDescriptor(MakeConstDiagnosticId,
                                     "Make Constant",
                                     "Can be made const",
                                     "Usage",
                                     DiagnosticSeverity.Warning,
                                     isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(MakeConstRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.LocalDeclarationStatement);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (CanBeMadeConst((LocalDeclarationStatementSyntax)context.Node, context.SemanticModel))
            {
                context.ReportDiagnostic(Diagnostic.Create(MakeConstRule, context.Node.GetLocation()));
            }
        }

        private bool CanBeMadeConst(LocalDeclarationStatementSyntax localDeclaration, SemanticModel semanticModel)
        {
            // already const?
            if (localDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword))
            {
                return false;
            }

            // Ensure that all variables in the local declaration have initializers that
            // are assigned with constant values.
            foreach (VariableDeclaratorSyntax variable in localDeclaration.Declaration.Variables)
            {
                EqualsValueClauseSyntax initializer = variable.Initializer;
                if (initializer == null)
                {
                    return false;
                }

                Optional<object> constantValue = semanticModel.GetConstantValue(initializer.Value);
                if (!constantValue.HasValue)
                {
                    return false;
                }

                TypeSyntax variableTypeName = localDeclaration.Declaration.Type;
                ITypeSymbol variableType = semanticModel.GetTypeInfo(variableTypeName).ConvertedType;

                // Ensure that the initializer value can be converted to the type of the
                // local declaration without a user-defined conversion.
                Conversion conversion = semanticModel.ClassifyConversion(initializer.Value, variableType);
                if (!conversion.Exists || conversion.IsUserDefined)
                {
                    return false;
                }

                // Special cases:
                //   * If the constant value is a string, the type of the local declaration
                //     must be System.String.
                //   * If the constant value is null, the type of the local declaration must
                //     be a reference type.
                if (constantValue.Value is string)
                {
                    if (variableType.SpecialType != SpecialType.System_String)
                    {
                        return false;
                    }
                }
                else if (variableType.IsReferenceType && constantValue.Value != null)
                {
                    return false;
                }
            }

            // Perform data flow analysis on the local declaration.
            DataFlowAnalysis dataFlowAnalysis = semanticModel.AnalyzeDataFlow(localDeclaration);

            // Retrieve the local symbol for each variable in the local declaration
            // and ensure that it is not written outside of the data flow analysis region.
            foreach (VariableDeclaratorSyntax variable in localDeclaration.Declaration.Variables)
            {
                ISymbol variableSymbol = semanticModel.GetDeclaredSymbol(variable);
                if (dataFlowAnalysis.WrittenOutside.Contains(variableSymbol))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
