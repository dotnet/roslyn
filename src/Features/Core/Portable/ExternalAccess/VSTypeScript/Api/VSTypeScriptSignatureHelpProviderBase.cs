// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SignatureHelp;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

/// <summary>
/// TODO: Ideally, we would export TypeScript service and delegate to an imported TypeScript service implementation.
/// However, TypeScript already exports the service so we would need to coordinate the change.
/// </summary>
internal abstract class VSTypeScriptSignatureHelpProviderBase : ISignatureHelpProvider
{
    public ImmutableArray<char> TriggerCharacters
    {
        get
        {
            if (TypeScriptTriggerCharacters.IsEmpty)
            {
                // Hard coded from https://devdiv.visualstudio.com/DevDiv/_git/TypeScript-VS?path=/VS/LanguageService/TypeScriptLanguageService/Features/SignatureHelp/TypeScriptSignatureHelpProvider.cs&version=GBmain&line=29&lineEnd=30&lineStartColumn=1&lineEndColumn=1&lineStyle=plain&_a=contents
                return ['<', '(', ','];
            }
            else
            {
                return TypeScriptTriggerCharacters;
            }
        }
    }

    public ImmutableArray<char> RetriggerCharacters
    {
        get
        {
            if (TypeScriptRetriggerCharacters.IsEmpty)
            {
                // Hard coded from https://devdiv.visualstudio.com/DevDiv/_git/TypeScript-VS?path=/VS/LanguageService/TypeScriptLanguageService/Features/SignatureHelp/TypeScriptSignatureHelpProvider.cs&version=GBmain&line=29&lineEnd=30&lineStartColumn=1&lineEndColumn=1&lineStyle=plain&_a=contents
                return ['>', ')'];
            }
            else
            {
                return TypeScriptRetriggerCharacters;
            }
        }
    }

    Task<SignatureHelpItems?> ISignatureHelpProvider.GetItemsAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, MemberDisplayOptions options, CancellationToken cancellationToken)
        => GetItemsAsync(document, position, triggerInfo, cancellationToken);

    [Obsolete("Implement TypeScriptTriggerCharacters instead", error: false)]
    public virtual bool IsTriggerCharacter(char ch) => false;

    [Obsolete("Implement TypeScriptRetriggerCharacters instead", error: false)]
    public virtual bool IsRetriggerCharacter(char ch) => false;

    public virtual ImmutableArray<char> TypeScriptTriggerCharacters { get; } = [];

    public virtual ImmutableArray<char> TypeScriptRetriggerCharacters { get; } = [];

    protected abstract Task<SignatureHelpItems?> GetItemsAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, CancellationToken cancellationToken);
}
