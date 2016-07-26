using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.VisualStudio.Shell.FindAllReferences;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal partial class StreamingFindReferencesPresenter
    {
        private class RoslynDefinitionBucket : DefinitionBucket
        {
            private readonly TableDataSourceFindReferencesContext _context;
            private readonly INavigableItem _definitionItem;
            private readonly NavigableItemEntryData _entryData;

            public RoslynDefinitionBucket(
                TableDataSourceFindReferencesContext context,
                INavigableItem definitionItem,
                NavigableItemEntryData entryData)
                : base(name: "",
                       sourceTypeIdentifier: context.SourceTypeIdentifier,
                       identifier: context.Identifier)
            {
                _context = context;
                _definitionItem = definitionItem;
                _entryData = entryData;
            }

            public override bool TryGetValue(string key, out object content)
            {
                content = GetValue(key);
                return content != null;
            }

            private object GetValue(string key)
            {
                // Return data specific to a definition.

                // Then fall back to the data that's common to references and definitions.
                return _entryData.GetValue(key);
            }
        }
    }
}