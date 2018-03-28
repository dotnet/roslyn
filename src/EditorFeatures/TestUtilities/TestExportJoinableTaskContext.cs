using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    // In 15.3 the editor took a dependency on JoinableTaskContext.
    // JTC appears in the VS MEF composition but does not itself
    // contain an ExportAttribute. The Editor's own unit tests
    // export it from a field and we need to do the same in order
    // to be able to compose in any part of the Editor.
    internal class TestExportJoinableTaskContext : ForegroundThreadAffinitizedObject
    {
        [Export]
        private JoinableTaskContext _joinableTaskContext = new JoinableTaskContext(
            mainThread: ForegroundThreadAffinitizedObject.CurrentForegroundThreadData.Thread);
    }
}
