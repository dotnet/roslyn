using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PopulateSwitch;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.PopulateSwitch
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.PopulateSwitch), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.ImplementInterface)]
    internal class CSharpPopulateSwitchCodeFixProvider : AbstractPopulateSwitchCodeFixProvider<SwitchSectionSyntax>
    {
        protected override int InsertPosition(IReadOnlyList<SwitchSectionSyntax> sections)
        {
            // If the last section has a default label, then we want to be above that.
            // Otherwise, we just get inserted at the end.
            if (sections.Count != 0)
            {
                var lastSection = sections.Last();
                if (lastSection.Labels.Any(label => label.Kind() == SyntaxKind.DefaultSwitchLabel))
                {
                    return sections.Count - 1;
                }
            }

            return sections.Count;
        }
    }
}
