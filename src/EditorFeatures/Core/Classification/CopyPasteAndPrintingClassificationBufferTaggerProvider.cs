// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
    /// <summary>
    /// This is the tagger we use for buffer classification scenarios.  It is only used for 
    /// IAccurateTagger scenarios.  Namely: Copy/Paste and Printing.  We use an 'Accurate' buffer
    /// tagger since these features need to get classification tags for the entire file.
    /// 
    /// i.e. if you're printing, you want semantic classification even for code that's not in view.
    /// The same applies to copy/pasting.
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IClassificationTag))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    internal partial class CopyPasteAndPrintingClassificationBufferTaggerProvider : ForegroundThreadAffinitizedObject, ITaggerProvider
    {
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly ClassificationTypeMap _typeMap;
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CopyPasteAndPrintingClassificationBufferTaggerProvider(
            IThreadingContext threadingContext,
            ClassificationTypeMap typeMap,
            IAsynchronousOperationListenerProvider listenerProvider,
            IGlobalOptionService globalOptions)
            : base(threadingContext)
        {
            _typeMap = typeMap;
            _asyncListener = listenerProvider.GetListener(FeatureAttribute.Classification);
            _globalOptions = globalOptions;
        }

        public IAccurateTagger<T>? CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            this.AssertIsForeground();

            // The LSP client will handle producing tags when running under the LSP editor.
            // Our tagger implementation should return nothing to prevent conflicts.
            if (buffer.IsInLspEditorContext())
            {
                return null;
            }

            return new Tagger(this, buffer, _asyncListener, _globalOptions) as IAccurateTagger<T>;
        }

        ITagger<T>? ITaggerProvider.CreateTagger<T>(ITextBuffer buffer)
            => CreateTagger<T>(buffer);
    }
}
