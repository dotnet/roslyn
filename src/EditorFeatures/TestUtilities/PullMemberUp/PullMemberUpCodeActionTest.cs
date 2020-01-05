using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;

namespace Microsoft.CodeAnalysis.Test.Utilities.PullMemberUp
{
    public abstract class PullMemberUpCodeActionTest : AbstractCodeActionTest
    {
        internal Task TestWithPullMemberDialogAsync(
            string initialMarkUp,
            string expectedResult,
            IEnumerable<(string memberName, bool makeAbstract)> selection = null,
            string target = null,
            int index = 0,
            CodeActionPriority? priority = null,
            TestParameters parameters = default)
        {
            var service = new TestPullMemberUpService(selection, target);

            return TestInRegularAndScript1Async(
                initialMarkUp, expectedResult,
                index, priority,
                parameters.WithFixProviderData(service));
        }
    }
}
