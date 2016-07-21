using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal partial class AsyncFindReferencesPresenter
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