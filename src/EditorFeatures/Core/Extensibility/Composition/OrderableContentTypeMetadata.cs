// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    internal class OrderableContentTypeMetadata : OrderableMetadata, IContentTypeMetadata
    {
        public IEnumerable<string> ContentTypes { get; }

        public OrderableContentTypeMetadata(IDictionary<string, object> data)
            : base(data)
        {
            this.ContentTypes = (IEnumerable<string>)data.GetValueOrDefault("ContentTypes");
        }
    }
}
