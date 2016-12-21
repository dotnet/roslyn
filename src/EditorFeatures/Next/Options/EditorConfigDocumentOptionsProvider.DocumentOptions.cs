using System;
using System.Linq;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    internal sealed partial class EditorConfigDocumentOptionsProvider : IDocumentOptionsProvider
    {
        private class DocumentOptions : IDocumentOptions
        {
            private ICodingConventionsSnapshot _codingConventionSnapshot;

            public DocumentOptions(ICodingConventionsSnapshot codingConventionSnapshot)
            {
                _codingConventionSnapshot = codingConventionSnapshot;
            }

            public bool TryGetDocumentOption(Document document, OptionKey option, out object value)
            {
                var editorConfigPersistence = option.Option.StorageLocations.OfType<EditorConfigStorageLocation>().SingleOrDefault();

                if (editorConfigPersistence == null)
                {
                    value = null;
                    return false;
                }

                if (_codingConventionSnapshot.TryGetConventionValue(editorConfigPersistence.KeyName, out value))
                {
                    try
                    {
                        value = editorConfigPersistence.ParseValue(value.ToString(), option.Option.Type);
                        return true;
                    }
                    catch (Exception)
                    {
                        // TODO: report this somewhere?
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
