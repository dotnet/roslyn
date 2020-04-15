// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Globalization;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    [Export(typeof(TestCurrentCultureProvider)), Shared]
    [ExportWorkspaceService(typeof(ICurrentCultureProvider), WorkspaceKind.Test), PartNotDiscoverable]
    internal sealed class TestCurrentCultureProvider : ICurrentCultureProvider
    {
        private CultureInfo _currentCulture;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestCurrentCultureProvider()
        {
        }

        public void SetCurrentCulture(CultureInfo currentCulture)
        {
            if (_currentCulture != null)
                throw new InvalidOperationException("Test tried to change current culture multiple times");

            _currentCulture = currentCulture;
        }

        public CultureInfo CurrentCulture => _currentCulture ?? throw new InvalidOperationException("Test failed to set current culture");
    }
}
