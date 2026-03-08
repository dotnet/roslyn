// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;

#if Unified_ExternalAccess
namespace Microsoft.CodeAnalysis.ExternalAccess.Unified.Copilot.Completion;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Completion;
#endif

internal interface ICSharpCopilotContextProviderService
{
    IAsyncEnumerable<IContextItem> GetContextItemsAsync(Document document, int position, IReadOnlyDictionary<string, object> activeExperiments, CancellationToken cancellationToken);
}
