using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    /// <summary>
    /// Supports test interaction with the new (in Dev15) Find References/Go to Implementation window.
    /// </summary>
    public class FindUsagesWindow_OutOfProc : OutOfProcComponent
    {
        private readonly FindUsagesWindow_InProc _inProc;

        public FindUsagesWindow_OutOfProc(VisualStudioInstance visualStudioInstance)
            :base(visualStudioInstance)
        {
            _inProc = CreateInProcComponent<FindUsagesWindow_InProc>(visualStudioInstance);
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
