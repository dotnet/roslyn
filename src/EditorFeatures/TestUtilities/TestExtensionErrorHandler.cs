// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    [Export(typeof(TestExtensionErrorHandler))]
    [Export(typeof(IExtensionErrorHandler))]
    internal class TestExtensionErrorHandler : IExtensionErrorHandler
    {
        private ImmutableList<Exception> _exceptions = ImmutableList<Exception>.Empty;

        public void HandleError(object sender, Exception exception)
        {
            if (exception == null)
            {
                // Log an exception saying we didn't get an exception. I'd consider throwing here, but double-faults are just caught and consumed by
                // the editor so that won't give a good debugging experience either.
                try
                {
                    ThrowExceptionToGetStackTrace();
                }
                catch (Exception e)
                {
                    exception = e;
                }
            }

            ImmutableInterlocked.Update(
                ref _exceptions,
                (list, item) => list.Add(item),
                exception);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowExceptionToGetStackTrace()
        {
            throw new Exception($"{nameof(TestExtensionErrorHandler)}.{nameof(HandleError)} called with null exception");
        }

        public ImmutableList<Exception> GetExceptions()
        {
            // We'll clear off our list, so that way we don't report this for other tests
            return Interlocked.Exchange(ref _exceptions, ImmutableList<Exception>.Empty);
        }
    }
}
