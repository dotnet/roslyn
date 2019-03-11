// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(TextMarkerTag))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TextViewRole(PredefinedTextViewRoles.Editable)] // TODO (tomat): ?
    internal sealed class ActiveStatementTaggerProvider : ITaggerProvider
    {
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ActiveStatementTaggerProvider(IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
            => new ActiveStatementTagger(_threadingContext, buffer) as ITagger<T>;
    }
}
