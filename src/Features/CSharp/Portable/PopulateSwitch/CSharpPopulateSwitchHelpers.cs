using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.PopulateSwitch
{
    internal static class CSharpPopulateSwitchHelpers
    {
        public static List<ExpressionSyntax> GetCaseLabels(SwitchStatementSyntax switchStatement, out bool containsDefaultLabel)
        {
            containsDefaultLabel = false;

            var caseLabels = new List<ExpressionSyntax>();
            foreach (var section in switchStatement.Sections)
            {
                foreach (var label in section.Labels)
                {
                    var caseLabel = label as CaseSwitchLabelSyntax;
                    if (caseLabel != null)
                    {
                        caseLabels.Add(caseLabel.Value);
                    }

                    if (label.IsKind(SyntaxKind.DefaultSwitchLabel))
                    {
                        containsDefaultLabel = true;
                    }
                }
            }

            return caseLabels;
        }
    }
}