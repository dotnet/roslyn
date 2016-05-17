using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            this.Roles = (string[])data.GetValueOrDefault("Roles")
                ?? (string[])data.GetValueOrDefault("TextViewRoles");
        }
    }
}
