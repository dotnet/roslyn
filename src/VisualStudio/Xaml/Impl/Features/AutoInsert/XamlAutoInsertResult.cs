﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Features.AutoInsert
{
    internal class XamlAutoInsertResult
    {
        public TextChange TextChange { get; set; }
        public int? CaretOffset { get; set; }
    }
}
