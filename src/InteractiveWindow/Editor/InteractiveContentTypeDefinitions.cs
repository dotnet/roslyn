using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal static class InteractiveContentTypeDefinitions
    {
        [Export, Name(PredefinedInteractiveContentTypes.InteractiveContentTypeName), BaseDefinition("text"), BaseDefinition("projection")]
        internal static readonly ContentTypeDefinition InteractiveContentTypeDefinition;

        [Export, Name(PredefinedInteractiveContentTypes.InteractiveOutputContentTypeName), BaseDefinition("text")]
        internal static readonly ContentTypeDefinition InteractiveOutputContentTypeDefinition;
    }
}