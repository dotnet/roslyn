// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Text;
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
        [ImportingConstructor]
        public ActiveStatementTaggerProvider()
        {
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            Workspace workspace;
            if (!Workspace.TryGetWorkspace(buffer.AsTextContainer(), out workspace))
            {
                return null;
            }

            var trackingService = workspace.Services.GetService<IActiveStatementTrackingService>();
            if (trackingService == null)
            {
                return null;
            }

            return new ActiveStatementTagger(trackingService, buffer) as ITagger<T>;
        }
    }
}
