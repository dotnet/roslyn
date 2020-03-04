﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
