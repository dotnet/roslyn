// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.ConvertIfToSwitch
{
    internal sealed partial class CSharpConvertIfToSwitchCodeRefactoringProvider
    {
        private sealed class CSharpAnalyzer : Analyzer
        {
            public CSharpAnalyzer(ISyntaxFacts syntaxFacts, Feature features)
                : base(syntaxFacts, features)
            {
            }

            public override bool HasUnreachableEndPoint(IOperation operation)
                => !operation.SemanticModel.AnalyzeControlFlow(operation.Syntax).EndPointIsReachable;

            // We do not offer a fix if the if-statement contains a break-statement, e.g.
            //
            //      while (...)
            //      {
            //          if (...) {
            //              break;
            //          }
            //      }
            //
            // When the 'break' moves into the switch, it will have different flow control impact.
            public override bool CanConvert(IConditionalOperation operation)
                => !operation.SemanticModel.AnalyzeControlFlow(operation.Syntax).ExitPoints.Any(n => n.IsKind(SyntaxKind.BreakStatement));

            public override bool CanImplicitlyConvert(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol targetType)
            {
                return syntax is ExpressionSyntax expressionSyntax &&
                    semanticModel.ClassifyConversion(expressionSyntax, targetType).IsImplicit;
            }
        }
    }
}
