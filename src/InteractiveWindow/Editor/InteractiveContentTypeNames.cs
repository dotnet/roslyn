using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace Roslyn.Editor.InteractiveWindow
{
    public static class InteractiveContentTypeNames
    {
        public const string InteractiveContentType = "Roslyn Interactive";
        public const string InteractiveOutputContentType = "Roslyn Interactive Output";
        public const string InteractiveCommandContentType = "Roslyn Interactive Command";

        [Export, Name(InteractiveContentType), BaseDefinition("text"), BaseDefinition("projection")]
        internal static readonly ContentTypeDefinition InteractiveContentTypeDefinition;

        [Export, Name(InteractiveOutputContentType), BaseDefinition("text")]
        internal static readonly ContentTypeDefinition InteractiveOutputContentTypeDefinition;

        [Export, Name(InteractiveCommandContentType), BaseDefinition("code")]
        internal static readonly ContentTypeDefinition InteractiveCommandContentTypeDefinition;
    }
}