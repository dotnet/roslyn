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
    }
}
