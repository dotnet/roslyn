// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal static class ExtractMethodService
    {
        public static Task<ExtractMethodResult> ExtractMethodAsync(Document document, TextSpan textSpan, bool localFunction, ExtractMethodGenerationOptions options, CancellationToken cancellationToken)
            => document.GetRequiredLanguageService<IExtractMethodService>().ExtractMethodAsync(document, textSpan, localFunction, options, cancellationToken);
    }
}
