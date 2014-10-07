using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roslyn.Services.Formatting
{
    /// <summary>
    /// option to control AlignTokensOperation behavior
    /// </summary>
    public enum AlignTokensOption
    {
        AlignIndentationOfTokensToBaseToken,
        AlignPositionOfTokensToIndentation,
    }
}
