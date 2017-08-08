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
            if (exception == null)
            {
                // Log an exception saying we didn't get an exception. I'd consider throwing here, but double-faults are just caught and consumed by
                // the editor so that won't give a good debugging experience either.
                _exceptions.Add(new Exception($"{nameof(TestExtensionErrorHandler)}.{nameof(HandleError)} called with null exception"));
            }
            else
            {
                _exceptions.Add(exception);
            }
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
