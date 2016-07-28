using System;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Tagging;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using Roslyn.Utilities;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.FindReferences
{
    //[Export(typeof(IViewTaggerProvider))]
    //[TagType(typeof(TextMarkerTag))]
    //[ContentType("text")]
    //// [ContentType(ContentTypeNames.VisualBasicContentType)]
    ////[ContentType(WellKnownContentTypeNames)]
    //internal partial class DiagnosticsSquiggleTaggerProvider : IViewTaggerProvider
    //{
    //    public static readonly object Key = new object();

    //    public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
    //    {
    //        return new Tagger(buffer) as ITagger<T>;
    //    }

    //    private class Tagger : ITagger<TextMarkerTag>
    //    {
    //        private readonly ITextBuffer buffer;

    //        public Tagger(ITextBuffer buffer)
    //        {
    //            this.buffer = buffer;
    //        }

    //        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

    //        public IEnumerable<ITagSpan<TextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
    //        {
    //            return SpecializedCollections.EmptyEnumerable<ITagSpan<TextMarkerTag>>();
    //        }
    //    }
    //}


    //[Export(typeof(ITableControlEventProcessorProvider)), Shared]
    //[DataSourceType(StreamingFindReferencesPresenter.RoslynFindReferencesTableDataSourceSourceTypeIdentifier)]
    //[DataSource(StreamingFindReferencesPresenter.RoslynFindReferencesTableDataSourceIdentifier)]
    //[Name(nameof(FindReferencesTableControlEventProcessorProvider))]
    //[Order(Before = Priority.Default)]
    ////  [Order(Before=Priority.Default)] 
    ////  Optional. The IWpfTableControl may have multiple event processors
    ////  registered. The Order attribute defines in which order (Before and After) 
    ////  the event processors are called.
    ////  The expected values for Before and After properties are event processor 
    ////  names or predefined orders(such as Priority or StandardTableControlEventProcessors).
    ////  You can ignore it or use[Order(After = Priority.Default, Before = StandardTableControlEventProcessors.Default)]
    //// </remarks>

    //[CLSCompliant(false)]


    //class FindReferencesTableControlEventProcessorProvider
    //{
    //}
}
