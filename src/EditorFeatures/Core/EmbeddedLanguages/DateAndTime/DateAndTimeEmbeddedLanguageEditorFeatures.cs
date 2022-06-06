// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.EmbeddedLanguages;
using Microsoft.CodeAnalysis.EmbeddedLanguages;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.DateAndTime
{
    internal sealed class DateAndTimeEmbeddedLanguageEditorFeatures : DateAndTimeEmbeddedLanguageFeatures, IEmbeddedLanguageEditorFeatures
    {
        public IBraceMatcher BraceMatcher { get; }

        public DateAndTimeEmbeddedLanguageEditorFeatures(EmbeddedLanguageInfo info)
            : base(info)
        {
        }
    }
}
