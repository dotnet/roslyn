using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public partial class ImmediateWindow_OutOfProc
    {
        public class Verifier
        {
            private readonly ImmediateWindow_OutOfProc _immediateWindow;
            public Verifier(ImmediateWindow_OutOfProc immediateWindow)
            {
                _immediateWindow = immediateWindow;
            }

            public void ValidateCommand(string command, string expectedResult)
            {
                throw new NotImplementedException();
            }
        }
    }
}
