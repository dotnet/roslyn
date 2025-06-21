// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion;

internal static class Utilities
{
    public static TextChange Collapse(SourceText newText, ImmutableArray<TextChange> changes)
    {
        if (changes.Length == 0)
        {
            return new TextChange(new TextSpan(0, 0), "");
        }
        else if (changes.Length == 1)
        {
            return changes[0];
        }

        // The span we want to replace goes from the start of the first span to the end of
        // the  last span.
        var totalOldSpan = TextSpan.FromBounds(changes.First().Span.Start, changes.Last().Span.End);

        // We figure out the text we're replacing with by actually just figuring out the
        // new span in the newText and grabbing the text out of that.  The newSpan will
        // start from the same position as the oldSpan, but it's length will be the old
        // span's length + all the deltas we accumulate through each text change.  i.e.
        // if the first change adds 2 characters and the second change adds 4, then 
        // the newSpan will be 2+4=6 characters longer than the old span.
        var sumOfDeltas = changes.Sum(c => c.NewText!.Length - c.Span.Length);
        var totalNewSpan = new TextSpan(totalOldSpan.Start, totalOldSpan.Length + sumOfDeltas);

        return new TextChange(totalOldSpan, newText.ToString(totalNewSpan));
    }

    // This is a temporarily method to support preference of IntelliCode items comparing to non-IntelliCode items.
    // We expect that Editor will introduce this support and we will get rid of relying on the "★" then.
    public static bool IsPreferredItem(this CompletionItem completionItem)
        => completionItem.DisplayText.StartsWith(UnicodeStarAndSpace);

    public const string UnicodeStarAndSpace = "\u2605 ";

    public static async Task<SyntaxContext> CreateSyntaxContextWithExistingSpeculativeModelAsync(Document document, int position, CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(document.SupportsSemanticModel, "Should only be called from C#/VB providers.");
        var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);

        var service = document.GetRequiredLanguageService<ISyntaxContextService>();
        return service.CreateContext(document, semanticModel, position, cancellationToken);
    }
}
