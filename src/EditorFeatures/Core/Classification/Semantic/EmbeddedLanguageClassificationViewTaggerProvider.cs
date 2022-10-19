// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
    /// <summary>
    /// This is the tagger we use for classifying the embedded language string literals currently visible in the editor view.
    /// </summary>
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(IClassificationTag))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal partial class EmbeddedLanguageClassificationViewTaggerProvider : AbstractSemanticOrEmbeddedClassificationViewTaggerProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public EmbeddedLanguageClassificationViewTaggerProvider(
            IThreadingContext threadingContext,
            ClassificationTypeMap typeMap,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext, typeMap, globalOptions, listenerProvider, ClassificationType.EmbeddedLanguage)
        {
        }
    }
}
