using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace Roslyn.Editor.InteractiveWindow
{
    public static class InteractiveConstants
    {
        /// <summary>
        /// The additional role found in any REPL editor window.
        /// </summary>
        public const string InteractiveTextViewRole = "REPL";
    }
}