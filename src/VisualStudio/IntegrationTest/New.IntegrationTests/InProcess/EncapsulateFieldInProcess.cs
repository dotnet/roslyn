// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    [TestService]
    internal partial class EncapsulateFieldInProcess
    {
        public string DialogName => "Preview Changes - Encapsulate Field";

        internal Task InvokeAsync(CancellationToken cancellationToken)
        {
            // Cancellation is not currently supported by SendAsync
            _ = cancellationToken;

            return TestServices.Input.SendAsync(new KeyPress(VirtualKey.R, ShiftState.Ctrl), new KeyPress(VirtualKey.E, ShiftState.Ctrl));
        }
    }
}
