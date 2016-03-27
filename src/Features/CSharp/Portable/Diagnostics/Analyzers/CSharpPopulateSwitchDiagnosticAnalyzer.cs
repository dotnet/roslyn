using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.PopulateSwitch;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.PopulateSwitch
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpPopulateSwitchDiagnosticAnalyzer : PopulateSwitchDiagnosticAnalyzerBase<SyntaxKind>
    {
        private static readonly ImmutableArray<SyntaxKind> s_kindsOfInterest = ImmutableArray.Create(SyntaxKind.SwitchStatement);

        public override ImmutableArray<SyntaxKind> SyntaxKindsOfInterest
        {
            get
            {
                return s_kindsOfInterest;
            }
        }

        protected override TextSpan GetDiagnosticSpan(SyntaxNode node)
        {
            var switchStatement = (SwitchStatementSyntax)node;
            return switchStatement.Span;
        }

        protected override bool SwitchIsFullyPopulated(SemanticModel model, SyntaxNode node, CancellationToken cancellationToken)
        {
            var switchBlock = (SwitchStatementSyntax)node;

            var enumType = model.GetTypeInfo(switchBlock.Expression).Type as INamedTypeSymbol;
            if (enumType == null || enumType.TypeKind != TypeKind.Enum)
            {
                return true;
            }

            // ignore enums marked with Flags per spec: https://github.com/dotnet/roslyn/issues/6766#issuecomment-156878851
            foreach (var attribute in enumType.GetAttributes())
            {
                var containingClass = attribute.AttributeClass.ToDisplayString();
                if (containingClass == typeof(System.FlagsAttribute).FullName)
                {
                    return true;
                }
            }

            var caseLabels = new List<ExpressionSyntax>();
            var hasDefaultCase = false;

            foreach (var section in switchBlock.Sections)
            {
                foreach (var label in section.Labels)
                {
                    if (label.IsKind(SyntaxKind.CaseSwitchLabel))
                    {
                        caseLabels.Add(((CaseSwitchLabelSyntax)label).Value);
                    }

                    if (label.IsKind(SyntaxKind.DefaultSwitchLabel))
                    {
                        hasDefaultCase = true;
                    }
                }
            }

            if (!hasDefaultCase)
            {
                return false;
            }

            var labelSymbols = new List<ISymbol>();
            foreach (var label in caseLabels)
            {
                labelSymbols.Add(model.GetSymbolInfo(label).Symbol);
            }

            foreach (var member in enumType.GetMembers())
            {
                // skip `.ctor`
                if (member.IsImplicitlyDeclared)
                {
                    continue;
                }

                var switchHasSymbol = false;
                foreach (var symbol in labelSymbols)
                {
                    if (symbol == member)
                    {
                        switchHasSymbol = true;
                        break;
                    }
                }

                if (!switchHasSymbol)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
