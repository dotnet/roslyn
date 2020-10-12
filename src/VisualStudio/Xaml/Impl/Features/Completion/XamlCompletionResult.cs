// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Features.Completion
{
    internal class XamlCompletionResult
    {
        public ImmutableArray<XamlCompletionItem> Completions { get; set; }
        public TextSpan? ApplicableToSpan { get; set; }
    }
}
