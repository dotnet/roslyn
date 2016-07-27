using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.FindReferences;
using Microsoft.VisualStudio.Shell.FindAllReferences;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.FindReferences
{
    internal partial class StreamingFindReferencesPresenter
    {
        private class RoslynDefinitionBucket : DefinitionBucket
        {
            private readonly StreamingFindReferencesPresenter _presenter;
            private readonly TableDataSourceFindReferencesContext _context;

            public readonly DefinitionItem DefinitionItem;

            public RoslynDefinitionBucket(
                StreamingFindReferencesPresenter presenter,
                TableDataSourceFindReferencesContext context,
                DefinitionItem definitionItem)
                : base(name: "",
                       sourceTypeIdentifier: context.SourceTypeIdentifier,
                       identifier: context.Identifier)
            {
                _presenter = presenter;
                _context = context;
                DefinitionItem = definitionItem;
            }

            public override bool TryGetValue(string key, out object content)
            {
                content = GetValue(key);
                return content != null;
            }

            private object GetValue(string key)
            {
                switch (key)
                {
                case StandardTableKeyNames.Text:
                case StandardTableKeyNames.FullText:
                    return DefinitionItem.DisplayParts.JoinText();

                //case StandardTableKeyNames2.TextInlines:
                //    return DefinitionItem.DisplayParts.ToTextBlock(_presenter._typeMap).Inlines;

                case StandardTableKeyNames2.DefinitionIcon:
                    return DefinitionItem.Tags.GetGlyph().GetImageMoniker();
                }

                return null;
            }
        }
    }
}