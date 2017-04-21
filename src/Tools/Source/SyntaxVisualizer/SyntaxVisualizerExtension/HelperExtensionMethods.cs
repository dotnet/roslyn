using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Roslyn.SyntaxVisualizer.Extension
{
    internal static class HelperExtensionMethods
    {
        internal static IWpfTextView ToWpfTextView(this IVsWindowFrame vsWindowFrame)
        {
            IWpfTextView wpfTextView = null;
            var vsTextView = VsShellUtilities.GetTextView(vsWindowFrame);

            if (vsTextView != null)
            {
                object textViewHost;
                Guid guidTextViewHost = DefGuidList.guidIWpfTextViewHost;
                if (((IVsUserData)vsTextView).GetData(ref guidTextViewHost, out textViewHost) == VSConstants.S_OK && 
                    textViewHost != null)
                {
                    wpfTextView = ((IWpfTextViewHost)textViewHost).TextView;
                }
            }

            return wpfTextView;
        }

        internal static SnapshotSpan ToSnapshotSpan(this Microsoft.CodeAnalysis.Text.TextSpan textSpan, ITextSnapshot snapshot)
        {
            return new SnapshotSpan(snapshot, new Span(textSpan.Start, textSpan.Length));
        }
    }
}
