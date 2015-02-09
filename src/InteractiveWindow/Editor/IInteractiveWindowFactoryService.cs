using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    /// <summary>
    /// Creates instances of the IInteractiveWindow.  
    /// </summary>
    public interface IInteractiveWindowFactoryService
    {
        /// <summary>
        /// Creates a new interactive window which runs against the provided interactive evaluator.
        /// </summary>
        IInteractiveWindow CreateWindow(IInteractiveEvaluator evaluator);
    }
}
