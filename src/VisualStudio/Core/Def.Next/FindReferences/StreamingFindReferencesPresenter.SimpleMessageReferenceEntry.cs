using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.FindReferences
{
    internal partial class StreamingFindReferencesPresenter
    {
        private class SimpleMessageReferenceEntry : ReferenceEntry
        {
            private readonly string _message;

            private SimpleMessageReferenceEntry(
                RoslynDefinitionBucket definitionBucket,
                string message)
                : base(definitionBucket)
            {
                _message = message;
            }

            public static Task<ReferenceEntry> CreateAsync(
                RoslynDefinitionBucket definitionBucket,
                string message)
            {
                var referenceEntry = new SimpleMessageReferenceEntry(definitionBucket, message);
                return Task.FromResult<ReferenceEntry>(referenceEntry);
            }

            protected override object GetValueWorker(string keyName)
            {
                switch (keyName)
                {
                case StandardTableKeyNames.Text:
                    return _message; 
                }

                return null;
            }
        }
    }
}
