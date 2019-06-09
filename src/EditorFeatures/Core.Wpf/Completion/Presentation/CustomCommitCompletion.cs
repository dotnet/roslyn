// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Roslyn.Utilities;
using CompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation
{
    internal sealed class CustomCommitCompletion : Completion4, ICustomCommit
    {
        private const string s_glyphCompletionWarning = "GlyphCompletionWarning";
        private readonly CompletionPresenterSession _completionPresenterSession;
        internal readonly CompletionItem CompletionItem;
        private readonly ImageMoniker _imageMoniker;

        public CustomCommitCompletion(
            CompletionPresenterSession completionPresenterSession,
            CompletionItem completionItem)
            : base(displayText: null, insertionText: null, description: null,
                   iconMoniker: default, suffix: completionItem.InlineDescription)
        {
            // PERF: Note that the base class contains a constructor taking the displayText string
            // but we're intentionally NOT using that here because it allocates a private CompletionState
            // object. By overriding the public property getters (DisplayText, InsertionText, etc.) the
            // extra allocation is avoided.
            _completionPresenterSession = completionPresenterSession;
            this.CompletionItem = completionItem;
            _imageMoniker = ImageMonikers.GetFirstImageMoniker(CompletionItem.Tags);
        }

        public void Commit()
        {
            // If a commit happens through the UI then let the session know.  It will, in turn,
            // let the underlying controller know, and the controller can commit the completion
            // item.
            _completionPresenterSession.OnCompletionItemCommitted(CompletionItem);
        }

        public override string DisplayText { get; set; }

        public override string InsertionText => DisplayText;

        public override string Description =>
                // If the completion item has an async description, then we don't want to force it
                // to be computed here.  That will cause blocking on the UI thread.  Note: the only
                // caller of this is the VS tooltip code which uses the presence of the Description
                // to then decide to show the tooltip.  But once they decide to show the tooltip,
                // they defer to us to get the contents for it asynchronously.  As such, we just want
                // to give them something non-empty so they know to go get the async description.
                "...";

        public Task<CompletionDescription> GetDescriptionAsync(Document document, CancellationToken cancellationToken)
        {
            var service = CompletionService.GetService(document);
            return service == null ?
                Task.FromResult(CompletionDescription.Empty) :
                service.GetDescriptionAsync(document, this.CompletionItem, cancellationToken);
        }

        public override ImageMoniker IconMoniker => _imageMoniker;

        public override string IconAutomationText => _imageMoniker.ToString();

        public override IEnumerable<CompletionIcon> AttributeIcons
        {
            get
            {
                if (this.CompletionItem.Tags.Contains(WellKnownTags.Warning))
                {
                    return new[] { new CompletionIcon2(Glyph.CompletionWarning.GetImageMoniker(), s_glyphCompletionWarning, s_glyphCompletionWarning) };
                }

                return null;
            }

            set
            {
                throw new NotImplementedException();
            }
        }
    }
}
