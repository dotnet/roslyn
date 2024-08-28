// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeFixes.FullyQualify;

internal abstract class AbstractFullyQualifyCodeFixProvider : CodeFixProvider
{
    public override FixAllProvider? GetFixAllProvider()
    {
        // Fix All is not supported by this code fix
        // https://github.com/dotnet/roslyn/issues/34465
        return null;
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var cancellationToken = context.CancellationToken;
        var document = context.Document;

        var service = document.GetRequiredLanguageService<IFullyQualifyService>();
        var optFixData = await service.GetFixDataAsync(document, context.Span, cancellationToken).ConfigureAwait(false);
        if (optFixData is null)
            return;

        var fixData = optFixData.Value;
        if (fixData.IndividualFixData.Length == 0)
            return;

        var codeActions = fixData.IndividualFixData.SelectAsArray(
            d => CodeAction.Create(
                d.Title,
                async cancellationToken =>
                {
                    var sourceText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                    var newText = sourceText.WithChanges(d.TextChanges);
                    return document.WithText(newText);
                },
                d.Title));

        if (codeActions.Length >= 2)
        {
            // Wrap the actions into a single top level suggestion so as to not clutter the list.
            context.RegisterCodeFix(CodeAction.Create(
                string.Format(FeaturesResources.Fully_qualify_0, fixData.Name),
                codeActions,
                isInlinable: true), context.Diagnostics);
        }
        else
        {
            context.RegisterFixes(codeActions, context.Diagnostics);
        }
    }
}
