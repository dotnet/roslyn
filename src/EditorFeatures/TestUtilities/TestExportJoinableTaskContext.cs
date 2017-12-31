using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    // Starting with 15.3 the editor took a dependency on JoinableTaskContext
    // in Text.Logic and Intellisense layers as an editor host provided service.
    [PartCreationPolicy(CreationPolicy.NonShared)] // JTC is "main thread" affinitized so should not be shared
    internal class TestExportJoinableTaskContext : ForegroundThreadAffinitizedObject
    {
        [ThreadStatic]
        private static JoinableTaskContext s_joinableTaskContext;

        [Export]
        private JoinableTaskContext _joinableTaskContext = s_joinableTaskContext ?? (s_joinableTaskContext = new JoinableTaskContext());
    }
}
