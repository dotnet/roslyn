using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    /// <summary>
    /// Supports test interaction with the new (in Dev15) Find References/Find Implementation window.
    /// </summary>
    public class FindReferencesWindow_OutOfProc : OutOfProcComponent
    {
        private readonly FindReferencesWindow_InProc _inProc;

        public FindReferencesWindow_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _inProc = CreateInProcComponent<FindReferencesWindow_InProc>(visualStudioInstance);
        }

        /// <summary>
        /// Returns the set of currently-displayed results.
        /// <para>
        /// The information currently returned is very basic, essentially just the raw text of the
        /// "Code" column. Also, it only contains the actual references or implementations; the
        /// project and class grouping is not included.
        /// </para>
        /// </summary>
        /// <param name="windowCaption">The name of the window. Generally this will be something like
        /// "'Alpha' references" or "'Beta' implementations".</param>
        public string[] GetContents(string windowCaption)
            => _inProc.GetContents(windowCaption);
    }
}
