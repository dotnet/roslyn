// *********************************************************
//
// Copyright © Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0 
//
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
// OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
// OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache 2 License for the specific language
// governing permissions and limitations under the License.
//
// *********************************************************

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MakeConstCS
{
    // Implementing syntax node analyzer because the make const diagnostics in one method body are not dependent on the contents of other method bodies.

    [DiagnosticAnalyzer]
    [ExportDiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class DiagnosticAnalyzer : ISyntaxNodeAnalyzer<SyntaxKind>
    {
        public const string MakeConstDiagnosticId = "MakeConstCS";
        public static readonly DiagnosticDescriptor MakeConstRule = new DiagnosticDescriptor(MakeConstDiagnosticId,
                                                                                             "Make Constant",
                                                                                             "Can be made const",
                                                                                             "Usage",
                                                                                             DiagnosticSeverity.Warning,
                                                                                             isEnabledByDefault: true);

        public ImmutableArray<SyntaxKind> SyntaxKindsOfInterest { get { return ImmutableArray.Create(SyntaxKind.LocalDeclarationStatement); } }

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(MakeConstRule); } }

        private static bool CanBeMadeConst(LocalDeclarationStatementSyntax localDeclaration, SemanticModel semanticModel)
        {
            // already const?
            if (localDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword))
            {
                return false;
            }

            // Ensure that all variables in the local declaration have initializers that
            // are assigned with constant values.
            foreach (var variable in localDeclaration.Declaration.Variables)
            {
                var initializer = variable.Initializer;
                if (initializer == null)
                {
                    return false;
                }

                var constantValue = semanticModel.GetConstantValue(initializer.Value);
                if (!constantValue.HasValue)
                {
                    return false;
                }

                var variableTypeName = localDeclaration.Declaration.Type;
                var variableType = semanticModel.GetTypeInfo(variableTypeName).ConvertedType;

                // Ensure that the initializer value can be converted to the type of the
                // local declaration without a user-defined conversion.
                var conversion = semanticModel.ClassifyConversion(initializer.Value, variableType);
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
            var dataFlowAnalysis = semanticModel.AnalyzeDataFlow(localDeclaration);

            // Retrieve the local symbol for each variable in the local declaration
            // and ensure that it is not written outside of the data flow analysis region.
            foreach (var variable in localDeclaration.Declaration.Variables)
            {
                var variableSymbol = semanticModel.GetDeclaredSymbol(variable);
                if (dataFlowAnalysis.WrittenOutside.Contains(variableSymbol))
                {
                    return false;
                }
            }

            return true;
        }

        public void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            if (CanBeMadeConst((LocalDeclarationStatementSyntax)node, semanticModel))
            {
                addDiagnostic(Diagnostic.Create(MakeConstRule, node.GetLocation()));
            }
        }
    }
}