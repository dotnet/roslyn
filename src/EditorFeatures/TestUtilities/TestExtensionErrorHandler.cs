// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
#pragma warning disable RS0032 // Test exports should not be discoverable
    [Export(typeof(IExtensionErrorHandler))]
#pragma warning restore RS0032 // Test exports should not be discoverable
    internal class TestExtensionErrorHandler : IExtensionErrorHandler
    {
        private ImmutableList<Exception> _exceptions = ImmutableList<Exception>.Empty;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestExtensionErrorHandler()
        {
        }

        public void HandleError(object sender, Exception exception)
        {
            ExceptionUtilities.FailFast(exception);
        }
    }
}
