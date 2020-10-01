// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public partial class DiagnosticCreationTests
    {
        [Fact, WorkItem(547049, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=547049")]
        public void TestDiagnosticCreationWithOverriddenSeverity()
        {
            var defaultSeverity = DiagnosticSeverity.Info;
            var effectiveSeverity = DiagnosticSeverity.Error;
            var descriptor = new DiagnosticDescriptor("ID", "Title", "MessageFormat", "Category", defaultSeverity, isEnabledByDefault: true);
            var diagnostic = Diagnostic.Create(descriptor, Location.None, effectiveSeverity, additionalLocations: null, properties: null);
            Assert.Equal(effectiveSeverity, diagnostic.Severity);
            Assert.Equal(0, diagnostic.WarningLevel);
        }
    }
}
