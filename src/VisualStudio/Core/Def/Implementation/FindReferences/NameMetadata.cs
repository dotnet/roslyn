// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    internal class NameMetadata
    {
        public string Name { get; }

        public NameMetadata(IDictionary<string, object> data)
        {
            this.Name = (string)data.GetValueOrDefault(nameof(Name));
        }
    }
}
