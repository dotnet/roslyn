// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility.Testing;
using WindowsInput.Native;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    [TestService]
    internal partial class EncapsulateFieldInProcess
    {
        public string DialogName => "Preview Changes - Encapsulate Field";

        internal Task InvokeAsync(CancellationToken cancellationToken)
        {
            return TestServices.Input.SendAsync([(VirtualKeyCode.VK_R, VirtualKeyCode.CONTROL), (VirtualKeyCode.VK_E, VirtualKeyCode.CONTROL)], cancellationToken);
        }
    }
}
