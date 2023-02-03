﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal sealed class DocumentOutlineOptionsMetadata
    {
        public static readonly Option2<bool> EnableDocumentOutline = new("DocumentOutlineOptions_EnableDocumentOutline", defaultValue: false);
    }
}
