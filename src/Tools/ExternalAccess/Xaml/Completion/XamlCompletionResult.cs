// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml.Completion;

internal class XamlCompletionResult(ImmutableArray<XamlCompletionItem> completions, TextSpan? applicableToSpan)
{
    public ImmutableArray<XamlCompletionItem> Completions { get; } = completions;
    public TextSpan? ApplicableToSpan { get; } = applicableToSpan;
}
