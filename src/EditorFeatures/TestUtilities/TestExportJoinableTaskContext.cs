using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    // Starting with 15.3 the editor took a dependency on JoinableTaskContext
    // in Text.Logic and Intellisense layers as an editor host provided service.
    [PartCreationPolicy(CreationPolicy.NonShared)] // JTC is "main thread" affinitized so should not be shared
    internal class TestExportJoinableTaskContext : ForegroundThreadAffinitizedObject
    {
        private static readonly ConcurrentDictionary<Thread, JoinableTaskContext> s_jtcPerThread = new ConcurrentDictionary<Thread, JoinableTaskContext>();

        [Export]
        private JoinableTaskContext _joinableTaskContext = s_jtcPerThread.GetOrAdd(ForegroundThreadAffinitizedObject.CurrentForegroundThreadData.Thread,
            (thread) => new JoinableTaskContext(mainThread: thread));
    }
}
