using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    public static class InteractiveWindowExtensions
    {
        /// <summary>
        /// Gets the interactive window associated with the text buffer if the text
        /// buffer is being hosted in the interactive window.
        /// 
        /// Returns null if the text buffer is not hosted in the interactive window.
        /// </summary>
        public static IInteractiveWindow GetInteractiveWindow(this ITextBuffer buffer)
        {
            return InteractiveWindow.FromBuffer(buffer);
        }
    }
}
