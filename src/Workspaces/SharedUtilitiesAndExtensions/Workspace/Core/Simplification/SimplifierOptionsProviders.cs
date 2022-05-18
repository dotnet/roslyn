// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Simplification;

internal static partial class SimplifierOptionsProviders
{
    internal static async ValueTask<SimplifierOptions> GetSimplifierOptionsAsync(this Document document, ISimplification simplification, SimplifierOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
    {
#if CODE_STYLE
        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        return simplification.GetSimplifierOptions(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree), fallbackOptions: null);
#else
        return await document.GetSimplifierOptionsAsync(fallbackOptionsProvider, cancellationToken).ConfigureAwait(false);
#endif
    }
}
