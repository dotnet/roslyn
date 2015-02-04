using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Roslyn.Hosting.Diagnostics.Waiters
{
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.TodoCommentList)]
    internal class TodoCommentListWaiter : AsynchronousOperationListener { }
}