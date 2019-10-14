using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense.SymbolSearch;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.CodeAnalysis.Editor.SymbolSearch
{
    internal class RoslynSymbolResult : SymbolSearchResult, IResultInLocalFile, IResultHasClassifiedDefinition, IResultHasClassifiedContext //, IResultOpensWithCustomAction
    {
        private DefinitionItem definition;
        private SourceReferenceItem reference;
        private RoslynSymbolSource source;
        private static ISymbolOriginDefinition LocalSymbolOrigin = null;

        public RoslynSymbolResult(RoslynSymbolSource source, DefinitionItem definition)
        {
            this.source = source;
            this.definition = definition;
            Owner = source;
            Name = definition.ToString();
            Origin = GetLocalOrigin();
            ClassifiedDefinition = new ClassifiedTextElement(definition.NameDisplayParts.Select(n => new ClassifiedTextRun(n.Style.ToString(), n.Text)));
            ClassifiedContext = new ClassifiedTextElement(definition.DisplayParts.Select(n => new ClassifiedTextRun(n.Style.ToString(), n.Text)));
            var sampleLocation = definition.SourceSpans.First(); // TODO: handle all locations
            PersistentSpan = source.ServiceProvider.PersistentSpanFactory.Create(sampleLocation.Document.FilePath, AsSpan(sampleLocation.SourceSpan), SpanTrackingMode.EdgeInclusive);
        }

        public RoslynSymbolResult(RoslynSymbolSource source, SourceReferenceItem reference)
            : this(source, reference.Definition)
        {
            this.reference = reference;
            Name = reference.ToString(); // which one runs first?
        }

        private Span AsSpan(TextSpan sourceSpan)
        {
            return new Span(sourceSpan.Start, sourceSpan.Length);
        }

        private ISymbolOriginDefinition GetLocalOrigin()
        {
            if (LocalSymbolOrigin == null)
            {
                source.ServiceProvider.SymbolSearchBroker.SymbolOriginDefinitions.TryGetValue(PredefinedSymbolOrigins.LocalCode, out LocalSymbolOrigin);
            }
            return LocalSymbolOrigin;
        }

        public IPersistentSpan PersistentSpan { get; private set; }

        public override ISymbolOriginDefinition Origin { get; protected set; }
        public override ISymbolSource Owner { get; protected set; }
        public override string Name { get; protected set; }

        public ClassifiedTextElement ClassifiedDefinition { get; private set; }

        public ClassifiedTextElement ClassifiedContext { get; private set; }

        public Span HighlightSpan => throw new System.NotImplementedException();

        public Task NavigateToAsync(CancellationToken token)
        {
            // how to get the workspace?
            //return definition.TryNavigateTo()
            return Task.CompletedTask;
        }
    }
}
