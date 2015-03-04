// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    internal class CodeChangeProviderMetadata : OrderableMetadata, ILanguagesMetadata
    {
        public IEnumerable<string> Languages { get; }

        public CodeChangeProviderMetadata(IDictionary<string, object> data)
            : base(data)
        {
            this.Languages = ((IReadOnlyDictionary<string, object>)data).GetEnumerableMetadata<string>("Languages");
        }

        public CodeChangeProviderMetadata(string name, IEnumerable<string> after = null, IEnumerable<string> before = null, params string[] languages)
            : base(name, after, before)
        {
            this.Languages = languages;
        }
    }
}
