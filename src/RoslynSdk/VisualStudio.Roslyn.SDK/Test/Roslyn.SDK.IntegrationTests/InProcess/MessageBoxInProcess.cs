// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.Shell.Interop;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.CodeAnalysis.Testing.InProcess
{
    [TestService]
    internal sealed partial class MessageBoxInProcess
    {
        private static bool s_initializedMessageBoxService;
#pragma warning disable IDE0052 // Keep the proffered service registered for the lifetime of the Visual Studio process.
        private static uint s_messageBoxServiceCookie;
#pragma warning restore IDE0052

        protected override async Task InitializeCoreAsync()
        {
            await base.InitializeCoreAsync();

            if (s_initializedMessageBoxService)
            {
                return;
            }

            s_initializedMessageBoxService = true;
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var profferService = await GetRequiredGlobalServiceAsync<SProfferService, IProfferService>(CancellationToken.None);
            profferService.ProfferService(typeof(IVsMessageBoxService).GUID, new MessageBoxProxy(), out s_messageBoxServiceCookie);
        }

        private sealed class MessageBoxProxy : IOleServiceProvider, IVsMessageBoxService
        {
            public int QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject)
            {
                ppvObject = IntPtr.Zero;
                var result = VSConstants.E_NOTIMPL;

                if (guidService == typeof(IVsMessageBoxService).GUID && riid == typeof(IVsMessageBoxService).GUID)
                {
                    var comInterface = Marshal.GetComInterfaceForObject(this, typeof(IVsMessageBoxService));
                    if (comInterface != IntPtr.Zero)
                    {
                        result = Marshal.QueryInterface(comInterface, ref riid, out ppvObject);
                        Marshal.Release(comInterface);
                    }
                }

                return result;
            }

            public int ShowMessageBox(
                IntPtr hWndOwner,
                IntPtr hInstance,
                string lpszText,
                string lpszCaption,
                uint dwStyle,
                IntPtr lpszIcon,
                IntPtr dwContextHelpId,
                IntPtr pfnMessageBoxCallback,
                uint dwLangID,
                out int pidButton)
            {
                // A zero result causes Visual Studio to fall back to displaying the modal dialog.
                pidButton = 2;
                throw new InvalidOperationException(
                    $"Unexpected dialog box appeared.{Environment.NewLine}" +
                    $"Text: {lpszText}{Environment.NewLine}" +
                    $"Caption: {lpszCaption}");
            }
        }
    }
}
