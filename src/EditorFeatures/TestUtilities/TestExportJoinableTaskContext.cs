using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    // Starting with 15.3 the editor took a dependency on JoinableTaskContext
    // in Text.Logic and Intellisense layers as an editor host provided service.
    internal class TestExportJoinableTaskContext
    {
        [ThreadStatic]
        private static JoinableTaskContext s_jtcThreadStatic;

        [Export]
        private JoinableTaskContext JoinableTaskContext
        {
            get
            {
                // Make sure each import gets JTC set up with the calling thread as the "main" thread
                return s_jtcThreadStatic ?? (s_jtcThreadStatic = new JoinableTaskContext());
            }
        }
    }
}
