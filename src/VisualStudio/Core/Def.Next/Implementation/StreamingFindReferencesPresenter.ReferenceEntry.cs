using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal partial class StreamingFindReferencesPresenter
    {
        private class ReferenceEntry
        {
            private readonly RoslynDefinitionBucket _definitionBucket;

            private readonly INavigableItem _referenceItem;
            private readonly NavigableItemEntryData _entryData;

            public ReferenceEntry(
                RoslynDefinitionBucket definitionBucket,
                INavigableItem referenceItem,
                NavigableItemEntryData entryData)
            {
                _definitionBucket = definitionBucket;
                _referenceItem = referenceItem;
                _entryData = entryData;
            }

            public bool TryGetValue(string keyName, out object content)
            {
                content = GetValue(keyName);
                return content != null;
            }

            private object GetValue(string keyName)
            {
                // Return data specific to a reference.
                switch (keyName)
                {
                    case StandardTableKeyNames2.Definition:
                        return _definitionBucket;
                }

                // Then fall back to the data that's common to references and definitions.
                return _entryData.GetValue(keyName);
            }
        }
    }
}