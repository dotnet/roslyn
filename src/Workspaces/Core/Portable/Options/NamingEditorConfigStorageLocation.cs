using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// This type is used when you need to indicate that the entire editorconfig options
    /// dictionary should be handled by EditorConfigNamingStyleParser
    /// </summary>
    internal sealed class NamingEditorConfigStorageLocation : OptionStorageLocation { }
}
