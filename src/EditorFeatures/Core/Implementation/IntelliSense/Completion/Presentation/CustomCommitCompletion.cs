// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation
{
    internal sealed class CustomCommitCompletion : Completion3, ICustomCommit
    {
        private static readonly string s_glyphCompletionWarning = "GlyphCompletionWarning";
        private readonly CompletionPresenterSession _completionPresenterSession;
        internal readonly PresentationItem PresentationItem;
        private readonly ImageMoniker _imageMoniker;

        public CustomCommitCompletion(
            CompletionPresenterSession completionPresenterSession, 
            PresentationItem presentationItem)
            : base()
        {
            // PERF: Note that the base class contains a constructor taking the displayText string
            // but we're intentionally NOT using that here because it allocates a private CompletionState
            // object. By overriding the public property getters (DisplayText, InsertionText, etc.) the
            // extra allocation is avoided.
            _completionPresenterSession = completionPresenterSession;
            this.PresentationItem = presentationItem;
            _imageMoniker = ImageMonikers.GetImageMoniker(PresentationItem.Item.Tags);
        }

        public void Commit()
        {
            // If a commit happens through the UI then let the session know.  It will, in turn,
            // let the underlying controller know, and the controller can commit the completion
            // item.
            _completionPresenterSession.OnCompletionItemCommitted(PresentationItem);
        }

        public override string DisplayText
        {
            get
            {
                return this.PresentationItem.Item.DisplayText;
            }
        }

        public override string InsertionText
        {
            get
            {
                return this.DisplayText; // [sic] Same as DisplayText
            }
        }

        public override string Description
        {
            get
            {
                // If the completion item has an async description, then we don't want to force it
                // to be computed here.  That will cause blocking on the UI thread.  Note: the only
                // caller of this is the VS tooltip code which uses the presence of the Description
                // to then decide to show the tooltip.  But once they decide to show the tooltip,
                // they defer to us to get the contents for it asynchronously.  As such, we just want
                // to give them something non-empty so they know to go get the async description.
                return "...";
            }
        }

        public async Task<CompletionDescription> GetDescriptionAsync(CancellationToken cancellationToken)
        {
            var document = await GetDocumentAsync(cancellationToken).ConfigureAwait(false);
            return await this.PresentationItem.GetDescriptionAsync(document, cancellationToken).ConfigureAwait(false);
        }

        private Task<Document> GetDocumentAsync(CancellationToken cancellationToken)
        {
            return _completionPresenterSession.SubjectBuffer.CurrentSnapshot.AsText().GetDocumentWithFrozenPartialSemanticsAsync(cancellationToken);
        }

        public string GetDescription_TestingOnly()
        {
            return GetDescriptionAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None).Text;
        }

        public override ImageMoniker IconMoniker
        {
            get
            {
                return _imageMoniker;
            }
        }

        public override string IconAutomationText
        {
            get
            {
                return _imageMoniker.ToString();
            }
        }

        public override System.Collections.Generic.IEnumerable<CompletionIcon> AttributeIcons
        {
            get
            {
                if (this.PresentationItem.Item.Tags.Contains(CompletionTags.Warning))
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
