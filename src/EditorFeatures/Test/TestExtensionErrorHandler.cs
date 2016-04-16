// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    [Export(typeof(TestExtensionErrorHandler))]
    [Export(typeof(IExtensionErrorHandler))]
    internal class TestExtensionErrorHandler : IExtensionErrorHandler
    {
        private List<Exception> _exceptions = new List<Exception>();

        public void HandleError(object sender, Exception exception)
        {
            if (exception is ArgumentOutOfRangeException && ((ArgumentOutOfRangeException)exception).ParamName == "span")
            {
                // TODO: this is known bug 655591, fixed by Jack in changeset 931906
                // Remove this workaround once the fix reaches the DP branch and we all move over.
                return;
            }

            _exceptions.Add(exception);
        }

        public ICollection<Exception> GetExceptions()
        {
            // We'll clear off our list, so that way we don't report this for other tests
            var newExceptions = _exceptions;
            _exceptions = new List<Exception>();
            return newExceptions;
        }
    }
}
