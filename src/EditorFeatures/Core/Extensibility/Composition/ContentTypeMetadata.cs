// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    internal class ContentTypeMetadata : IContentTypeMetadata
    {
        public IEnumerable<string> ContentTypes { get; }

        public ContentTypeMetadata(IDictionary<string, object> data)
        {
            this.ContentTypes = (IEnumerable<string>)data.GetValueOrDefault("ContentTypes");
        }
    }
}
