using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
    /// <summary>
    /// The metadata class used to describe a <see cref="ClassificationProvider"/> when using MEF.
    /// </summary>
    internal sealed class ClassificationProviderMetadata : OrderableLanguageMetadata
    {
        public string[] Roles { get; }

        public ClassificationProviderMetadata(IDictionary<string, object> data)
            : base(data)
        {
            this.Roles = (string[])data.GetValueOrDefault("Roles")
                ?? (string[])data.GetValueOrDefault("TextViewRoles");
        }
    }
}
