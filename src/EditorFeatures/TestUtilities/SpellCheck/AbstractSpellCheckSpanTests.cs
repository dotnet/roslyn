// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SpellCheck;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.SpellCheck
{
    public abstract class AbstractSpellCheckSpanTests
    {
        protected abstract TestWorkspace CreateWorkspace(string content);

        protected async Task TestAsync(string content)
        {
            using var workspace = CreateWorkspace(content);
            var annotations = workspace.Projects.Single().Documents.Single().AnnotatedSpans;

            var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
            var service = document.GetRequiredLanguageService<ISpellCheckSpanService>();

            var actual = await service.GetSpansAsync(document, CancellationToken.None);
            var expected = Flatten(annotations);

            actual = actual.Sort((s1, s2) => s1.TextSpan.Start - s2.TextSpan.Start);
            expected = expected.Sort((s1, s2) => s1.TextSpan.Start - s2.TextSpan.Start);

            Assert.Equal<SpellCheckSpan>(expected, actual);
        }

        private static ImmutableArray<SpellCheckSpan> Flatten(IDictionary<string, ImmutableArray<TextSpan>> annotations)
        {
            return annotations.SelectMany(kvp => kvp.Value.Select(span => new SpellCheckSpan(span, ConvertKind(kvp.Key)))).ToImmutableArray();
        }

        private static SpellCheckKind ConvertKind(string key)
        {
            return key switch
            {
                "Comment" => SpellCheckKind.Comment,
                "Identifier" => SpellCheckKind.Identifier,
                "String" => SpellCheckKind.String,
                _ => throw ExceptionUtilities.UnexpectedValue(key),
            };
        }
    }
}
