// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Interop;
using Microsoft.VisualStudio.Threading;
using WindowsInput;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    [TestService]
    internal partial class InputInProcess
    {
        internal Task SendAsync(params InputKey[] keys)
        {
            return SendAsync(
                simulator =>
                {
                    foreach (var key in keys)
                    {
                        key.Apply(simulator);
                    }
                });
        }

        internal async Task SendAsync(Action<IInputSimulator> callback)
        {
            // AbstractSendKeys runs synchronously, so switch to a background thread before the call
            await TaskScheduler.Default;

            TestServices.JoinableTaskFactory.Run(async () =>
            {
                await TestServices.Editor.ActivateAsync(CancellationToken.None);
            });

            callback(new InputSimulator());

            TestServices.JoinableTaskFactory.Run(async () =>
            {
                await WaitForApplicationIdleAsync(CancellationToken.None);
            });
        }

        internal Task SendWithoutActivateAsync(params InputKey[] keys)
        {
            return SendWithoutActivateAsync(
                simulator =>
                {
                    foreach (var key in keys)
                    {
                        key.Apply(simulator);
                    }
                });
        }

        internal async Task SendWithoutActivateAsync(Action<IInputSimulator> callback)
        {
            // AbstractSendKeys runs synchronously, so switch to a background thread before the call
            await TaskScheduler.Default;

            callback(new InputSimulator());

            TestServices.JoinableTaskFactory.Run(async () =>
            {
                await WaitForApplicationIdleAsync(CancellationToken.None);
            });
        }

        internal Task SendToNavigateToAsync(params InputKey[] keys)
        {
            return SendToNavigateToAsync(
                simulator =>
                {
                    foreach (var key in keys)
                    {
                        key.Apply(simulator);
                    }
                });
        }

        internal async Task SendToNavigateToAsync(Action<IInputSimulator> callback)
        {
            // AbstractSendKeys runs synchronously, so switch to a background thread before the call
            await TaskScheduler.Default;

            // Take no direct action regarding activation, but assert the correct item already has focus
            TestServices.JoinableTaskFactory.Run(async () =>
            {
                await TestServices.JoinableTaskFactory.SwitchToMainThreadAsync();
                var searchBox = Assert.IsAssignableFrom<TextBox>(Keyboard.FocusedElement);
                Assert.Equal("PART_SearchBox", searchBox.Name);
            });

            callback(new InputSimulator());

            TestServices.JoinableTaskFactory.Run(async () =>
            {
                await WaitForApplicationIdleAsync(CancellationToken.None);
            });
        }

        internal async Task MoveMouseAsync(Point point)
        {
            var horizontalResolution = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
            var verticalResolution = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
            var virtualPoint = new ScaleTransform(65535.0 / horizontalResolution, 65535.0 / verticalResolution).Transform(point);

            await SendAsync(simulator => simulator.Mouse.MoveMouseTo(virtualPoint.X, virtualPoint.Y));

            // ⚠ The call to GetCursorPos is required for correct behavior.
            var actualPoint = NativeMethods.GetCursorPos();
            Assert.True(Math.Abs(actualPoint.X - point.X) <= 1, $"Expected '{Math.Abs(actualPoint.X - point.X)}' <= '1'. Move to '({point.X}, {point.Y})' produced '({actualPoint.X}, {actualPoint.Y})'.");
            Assert.True(Math.Abs(actualPoint.Y - point.Y) <= 1, $"Expected '{Math.Abs(actualPoint.Y - point.Y)}' <= '1'. Move to '({point.X}, {point.Y})' produced '({actualPoint.X}, {actualPoint.Y})'.");
        }
    }
}
