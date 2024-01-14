// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.IntegrationTest.Setup
{
    /// <summary>This class causes a crash if an exception is encountered by the editor.</summary>
    [Shared, Export(typeof(IExtensionErrorHandler)), Export(typeof(TestExtensionErrorHandler))]
    public class TestExtensionErrorHandler : IExtensionErrorHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestExtensionErrorHandler()
        {
        }

        public void HandleError(object sender, Exception exception)
        {
            if (exception is ArgumentException argumentException
                && argumentException.Message.Contains("SnapshotPoint")
                && argumentException.StackTrace.Contains("Microsoft.VisualStudio.Text.Editor.Implementation.WpfTextView.ValidateBufferPosition"))
            {
                // Known issue https://github.com/dotnet/roslyn/issues/35123
                return;
            }

            if (exception is TaskCanceledException taskCanceledException
                && taskCanceledException.StackTrace.Contains("Microsoft.CodeAnalysis.Editor.Implementation.Suggestions.SuggestedActionsSourceProvider.SuggestedActionsSource.GetSuggestedActions"))
            {
                // Workaround for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1070469
                return;
            }

            if (exception is ObjectDisposedException objectDisposedException
                && objectDisposedException.StackTrace.Contains("Microsoft.VisualStudio.Text.IntraTextTaggerAggregator.Implementation.IntraTextAdornmentTagger"))
            {
                // Workaround for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1935805
                return;
            }

            FatalError.ReportAndPropagate(exception);
            TestTraceListener.Instance.AddException(exception);
        }
    }
}
