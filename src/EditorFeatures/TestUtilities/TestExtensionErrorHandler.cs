// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    [Export(typeof(IExtensionErrorHandler))]
    internal class TestExtensionErrorHandler : IExtensionErrorHandler
    {
        private ImmutableList<Exception> _exceptions = ImmutableList<Exception>.Empty;

        [ImportingConstructor]
        public TestExtensionErrorHandler()
        {
        }

        public void HandleError(object sender, Exception exception)
        {
            // Work around bug that is fixed in https://devdiv.visualstudio.com/DevDiv/_git/VS-Platform/pullrequest/209513
            if (exception is NullReferenceException && exception.StackTrace.Contains("SpanTrackingWpfToolTipPresenter"))
            {
                return;
            }

            ExceptionUtilities.FailFast(exception);
        }
    }
}
