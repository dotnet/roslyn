using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    // Starting with 15.3 the editor took a dependency on JoinableTaskContext
    // in Text.Logic and Intellisense layers as an editor host provided service.
    [PartCreationPolicy(CreationPolicy.NonShared)] // JTC is "main thread" affinitized so should not be shared
    internal class TestExportJoinableTaskContext
    {
        [ThreadStatic]
        private static JoinableTaskContext s_jtcThreadStatic;

        [Export]
        private JoinableTaskContext _joinableTaskContext = s_jtcThreadStatic ?? (s_jtcThreadStatic = new JoinableTaskContext());
    }
}
