using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Test.Utilities.PullMemberUp
{
    public enum DialogState
    {
    }

    [UseExportProvider]
    public class AbstractPullMemberUpTests
    {
        public static async Task TestPullMemberUpCSharpAsync(
            string markup,
            DialogState state,
            string expectedCode = null)
        {
        }
    }
}
