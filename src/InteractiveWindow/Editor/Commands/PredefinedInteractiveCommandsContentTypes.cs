using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
