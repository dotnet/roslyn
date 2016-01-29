using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.AddImport;
using Microsoft.CodeAnalysis.Editor.Implementation.SearchNuGet;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SearchNuGet
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.OfferNuGetSearch), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.AddUsingOrImport)]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.GenerateVariable)]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.GenerateMethod)]
    internal class CSharpOfferNuGetSearchCodeFixProvider : AbstractOfferNuGetSearchCodeFixProvider
    {
        [ImportingConstructor]
        public CSharpOfferNuGetSearchCodeFixProvider(ILightBulbBroker lightBulbBroker)
            : base(lightBulbBroker)
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds => AddImportDiagnosticIds.FixableTypeIds;
    }
}
