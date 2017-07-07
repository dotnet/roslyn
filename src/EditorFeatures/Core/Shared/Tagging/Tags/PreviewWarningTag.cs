// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal class PreviewWarningTag : TextMarkerTag
    {
        public const string TagId = "RoslynPreviewWarningTag";

        public static readonly PreviewWarningTag Instance = new PreviewWarningTag();

        private PreviewWarningTag()
            : base(TagId)
        {
        }
    }
}
