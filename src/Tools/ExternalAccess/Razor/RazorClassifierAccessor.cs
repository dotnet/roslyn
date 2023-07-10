// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal static class RazorClassifierAccessor
    {
        public static async Task<IEnumerable<ClassifiedSpan>> GetClassifiedSpansAsync(Document document, TextSpan textSpan, RazorClassificationOptionsWrapper options, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return Classifier.GetClassifiedSpans(
                document.Project.Services, document.Project, semanticModel, textSpan, options.UnderlyingObject, cancellationToken);
        }
    }
}
