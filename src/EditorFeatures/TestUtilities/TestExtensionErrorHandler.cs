// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    [Export(typeof(IExtensionErrorHandler))]
    [Export(typeof(ITestErrorHandler))]
    internal class TestExtensionErrorHandler : IExtensionErrorHandler, ITestErrorHandler
    {
        public ImmutableList<Exception> Exceptions { get; private set; } = ImmutableList<Exception>.Empty;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestExtensionErrorHandler()
        {
        }

        public void HandleError(object sender, Exception exception)
        {
            // Work around bug that is fixed in https://devdiv.visualstudio.com/DevDiv/_git/VS-Platform/pullrequest/209513
            if (exception is NullReferenceException &&
                exception.StackTrace.Contains("SpanTrackingWpfToolTipPresenter"))
            {
                return;
            }

            // Work around for https://github.com/dotnet/roslyn/issues/42982
            if (exception is NullReferenceException &&
                exception.StackTrace.Contains("Microsoft.CodeAnalysis.Completion.Providers.AbstractEmbeddedLanguageCompletionProvider.GetLanguageProviders"))
            {
                return;
            }

            // Work around for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1091056
            if (exception is InvalidOperationException &&
                exception.StackTrace.Contains("Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Implementation.CompletionTelemetryHost"))
            {
                return;
            }

            // This exception is unexpected and as such we want the containing test case to
            // fail. Unfortuntately throwing an exception here is not going to help because
            // the editor is going to catch and swallow it. Store it here and wait for the 
            // containing workspace to notice it and throw.
            Exceptions = Exceptions.Add(exception);
        }
    }
}
