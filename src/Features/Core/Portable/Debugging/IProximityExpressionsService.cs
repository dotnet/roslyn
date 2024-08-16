// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Debugging;

internal interface IProximityExpressionsService : ILanguageService
{
    Task<IList<string>> GetProximityExpressionsAsync(Document document, int position, CancellationToken cancellationToken);
    Task<bool> IsValidAsync(Document document, int position, string expressionValue, CancellationToken cancellationToken);
}
