using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    internal sealed class SignatureHelpProviderMetadata : OrderableLanguageMetadata
    {
        public string[] Roles { get; }

        public SignatureHelpProviderMetadata(IDictionary<string, object> data)
            : base(data)
        {
            this.Roles = (string[])data.GetValueOrDefault("Roles")
                ?? (string[])data.GetValueOrDefault("TextViewRoles");
        }
    }
}
