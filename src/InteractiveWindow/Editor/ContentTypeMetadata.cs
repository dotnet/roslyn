using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal class ContentTypeMetadata
    {
        public IEnumerable<string> ContentTypes { get; private set; }

        public ContentTypeMetadata(IDictionary<string, object> data)
        {
            this.ContentTypes = (IEnumerable<string>)data["ContentTypes"];
        }
    }
}
