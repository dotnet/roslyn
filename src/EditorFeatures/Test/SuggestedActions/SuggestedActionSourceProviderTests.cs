// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;
using Microsoft.VisualStudio.Language.Intellisense;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.SuggestedActions
{
    public class SuggestedActionSourceProviderTests
    {
        [Fact]
        public void EnsureAttributesMatchData()
        {
            // Ensure that the list of orderings on this type matches the set we expose in SuggestedActionsSourceProvider.Orderings
            var attributes = typeof(SuggestedActionsSourceProvider).GetCustomAttributes(inherit: false)
                .OfType<SuggestedActionPriorityAttribute>()
                .ToImmutableArray();
            AssertEx.SetEqual(attributes.Select(a => a.Priority), SuggestedActionsSourceProvider.Orderings);
        }
    }
}
