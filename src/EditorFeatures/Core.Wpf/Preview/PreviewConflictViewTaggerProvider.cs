// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Preview
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(ConflictTag))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [ContentType(ContentTypeNames.XamlContentType)]
    [TextViewRole(TextViewRoles.PreviewRole)]
    internal class PreviewConflictTaggerProvider
        : AbstractPreviewTaggerProvider<ConflictTag>
    {
        [ImportingConstructor]
        public PreviewConflictTaggerProvider()
            : base(PredefinedPreviewTaggerKeys.ConflictSpansKey, ConflictTag.Instance)
        {
        }
    }
}
