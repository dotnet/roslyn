// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Completion;

internal interface ICSharpCopilotContextProviderService
{
    IAsyncEnumerable<IContextItem> GetContextItemsAsync(Document document, int position, IReadOnlyDictionary<string, object> activeExperiments, CancellationToken cancellationToken);
}
