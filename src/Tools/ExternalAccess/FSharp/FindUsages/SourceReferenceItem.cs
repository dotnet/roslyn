using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.FindUsages
{
    public class SourceReferenceItem
    {
        private readonly Microsoft.CodeAnalysis.FindUsages.SourceReferenceItem _roslynSourceReferenceItem;

        private SourceReferenceItem(Microsoft.CodeAnalysis.FindUsages.SourceReferenceItem roslynDefinitionItem)
        {
            _roslynSourceReferenceItem = roslynDefinitionItem;
        }

        public SourceReferenceItem(DefinitionItem definition, DocumentSpan sourceSpan)
        {
            _roslynSourceReferenceItem = new Microsoft.CodeAnalysis.FindUsages.SourceReferenceItem(definition.RoslynDefinitionItem, sourceSpan.ToRoslynDocumentSpan(), ImmutableDictionary<string, ImmutableArray<string>>.Empty);
        }

        internal Microsoft.CodeAnalysis.FindUsages.SourceReferenceItem RoslynSourceReferenceItem
        {
            get
            {
                return _roslynSourceReferenceItem;
            }
        }
    }
}
