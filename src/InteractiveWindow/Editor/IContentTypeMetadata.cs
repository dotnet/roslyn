using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Utilities;

namespace Roslyn.Editor.InteractiveWindow
{
    public interface IContentTypeMetadata
    {
        IEnumerable<string> ContentTypes { get; }
    }

    public static class ContentTypeMetadataHelpers
    {
        public static T OfContentType<T>(
            this IEnumerable<Lazy<T, IContentTypeMetadata>> exports,
            IContentType contentType,
            IContentTypeRegistryService contentTypeRegistry)
        {
            return (from export in exports
                    from exportedContentTypeName in export.Metadata.ContentTypes
                    let exportedContentType = contentTypeRegistry.GetContentType(exportedContentTypeName)
                    where exportedContentType.IsOfType(contentType.TypeName)
                    select export.Value).SingleOrDefault();
        }
    }
}
