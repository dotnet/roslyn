// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.ConvertIfToSwitch
{
    internal sealed partial class CSharpConvertIfToSwitchCodeRefactoringProvider
    {
        private sealed class CSharpAnalyzer : Analyzer
        {
            public override bool SupportsSwitchExpression => true;
            public override bool SupportsCaseGuard => true;
            public override bool SupportsRangePattern => false;
            public override bool SupportsTypePattern => true;
            public override bool SupportsSourcePattern => true;
            public override bool SupportsRelationalPattern => false;

            public CSharpAnalyzer(ISyntaxFactsService syntaxFacts)
                : base(syntaxFacts)
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
        }
    }
}
