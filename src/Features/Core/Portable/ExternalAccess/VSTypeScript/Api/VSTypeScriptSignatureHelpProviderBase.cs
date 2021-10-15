// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SignatureHelp;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    /// <summary>
    /// TODO: Ideally, we would export TypeScript service and delegate to an imported TypeScript service implementation.
    /// However, TypeScript already exports the service so we would need to coordinate the change.
    /// </summary>
    internal abstract class VSTypeScriptSignatureHelpProviderBase : ISignatureHelpProvider
    {
        Task<SignatureHelpItems?> ISignatureHelpProvider.GetItemsAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, SignatureHelpOptions options, CancellationToken cancellationToken)
            => GetItemsAsync(document, position, triggerInfo, cancellationToken);

        public abstract bool IsTriggerCharacter(char ch);
        public abstract bool IsRetriggerCharacter(char ch);

        protected abstract Task<SignatureHelpItems?> GetItemsAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, CancellationToken cancellationToken);
    }
}
