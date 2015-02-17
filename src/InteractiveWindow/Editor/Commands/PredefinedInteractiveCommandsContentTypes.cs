using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow.Commands
{
    public static class PredefinedInteractiveCommandsContentTypes
    {
        public const string InteractiveCommandContentTypeName = "Interactive Command";

        [Export, Name(InteractiveCommandContentTypeName), BaseDefinition("code")]
        internal static readonly ContentTypeDefinition InteractiveCommandContentTypeDefinition;
    }
}
