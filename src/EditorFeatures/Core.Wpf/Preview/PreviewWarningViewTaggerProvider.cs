// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Preview
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(PreviewWarningTag))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [ContentType(ContentTypeNames.XamlContentType)]
    [TextViewRole(TextViewRoles.PreviewRole)]
    internal class PreviewWarningTaggerProvider
        : AbstractPreviewTaggerProvider<PreviewWarningTag>
    {
        [ImportingConstructor]
        public PreviewWarningTaggerProvider()
            : base(PredefinedPreviewTaggerKeys.WarningSpansKey, PreviewWarningTag.Instance)
        {
        }
    }
}
