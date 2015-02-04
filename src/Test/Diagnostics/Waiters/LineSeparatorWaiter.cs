using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Roslyn.Hosting.Diagnostics.Waiters
{
    // This doesn't need to be an EditorAdornmentWaiter, since we control our own 
    // adornment layer
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.LineSeparators)]
    internal class LineSeparatorWaiter : AsynchronousOperationListener { }
}