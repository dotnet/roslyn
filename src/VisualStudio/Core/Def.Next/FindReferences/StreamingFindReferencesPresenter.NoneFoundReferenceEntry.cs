using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.FindReferences
{
    internal partial class StreamingFindReferencesPresenter
    {
        private class NoneFoundReferenceEntry : ReferenceEntry
        {
            public NoneFoundReferenceEntry(RoslynDefinitionBucket definitionBucket)
                : base(definitionBucket)
            {
            }

            protected override object GetValueWorker(string keyName)
            {
                switch (keyName)
                {
                case StandardTableKeyNames.Text:
                    return $"No references found to '{DefinitionBucket.DefinitionItem.DisplayParts.JoinText()}'";
                }

                return null;
            }
        }
    }
}
