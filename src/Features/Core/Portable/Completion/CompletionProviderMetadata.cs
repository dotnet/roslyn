using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal sealed class CompletionProviderMetadata : OrderableLanguageMetadata
    {
        public string[] Roles { get; }

        public CompletionProviderMetadata(IDictionary<string, object> data)
            : base(data)
        {
            Roles = (string[])data.GetValueOrDefault("Roles")
                ?? (string[])data.GetValueOrDefault("TextViewRoles");
        }
    }
}
