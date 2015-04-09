// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal class ContentTypeMetadata
    {
        public IEnumerable<string> ContentTypes { get; }

        public ContentTypeMetadata(IDictionary<string, object> data)
        {
            this.ContentTypes = (IEnumerable<string>)data["ContentTypes"];
        }
    }
}
