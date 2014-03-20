using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    public class CSharpTrackingDiagnosticAnalyzer : TrackingDiagnosticAnalyzer<SyntaxKind>
    {
        static readonly Regex omittedSyntaxKindRegex =
            new Regex(@"Using|Extern|Parameter|Constraint|Specifier|Initializer|Global|Method|Destructor");

        protected override bool IsOnCodeBlockSupported(SymbolKind symbolKind, MethodKind methodKind, bool returnsVoid)
        {
            return base.IsOnCodeBlockSupported(symbolKind, methodKind, returnsVoid) && methodKind != MethodKind.EventRaise;
        }

        protected override bool IsAnalyzeNodeSupported(SyntaxKind syntaxKind)
        {
            return base.IsAnalyzeNodeSupported(syntaxKind) && !omittedSyntaxKindRegex.IsMatch(syntaxKind.ToString());
        }
    }
}
