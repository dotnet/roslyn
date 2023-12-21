// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;
using Xunit;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Roslyn.VisualStudio.NewIntegrationTests.InProcess;

[TestService]
internal partial class MessageBoxInProcess
{
    private static bool s_initializedMessageBoxService;
#pragma warning disable IDE0052 // Remove unread private members
    private static uint s_messageBoxServiceCookie;
#pragma warning restore IDE0052 // Remove unread private members

    private static ImmutableList<MessageBoxHandler> s_handlers = ImmutableList<MessageBoxHandler>.Empty;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();

        if (s_initializedMessageBoxService)
            return;

        s_initializedMessageBoxService = true;
        await JoinableTaskFactory.SwitchToMainThreadAsync();
        var profferService = await GetRequiredGlobalServiceAsync<SProfferService, IProfferService>(CancellationToken.None);
        profferService.ProfferService(typeof(IVsMessageBoxService).GUID, new MessageBoxProxy(), out s_messageBoxServiceCookie);
    }

    public IDisposable HandleMessageBox(Func<string, string, DialogResult> callback)
    {
        var handler = new MessageBoxHandler(callback);
        ImmutableInterlocked.Update(ref s_handlers, static (handlers, handler) => handlers.Add(handler), handler);
        return handler;
    }

    private sealed class MessageBoxHandler(Func<string, string, DialogResult> callback) : IDisposable
    {
        private readonly Func<string, string, DialogResult> _callback = callback;

        public DialogResult Handle(string text, string caption)
        {
            return _callback(text, caption);
        }

        public void Dispose()
        {
            ImmutableInterlocked.Update(ref s_handlers, static (handlers, self) => handlers.Remove(self), this);
        }
    }

    private sealed class MessageBoxProxy : IOleServiceProvider, IVsMessageBoxService
    {
        public int QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject)
        {
            ppvObject = IntPtr.Zero;
            var hr = VSConstants.E_NOTIMPL;

            if (guidService == typeof(IVsMessageBoxService).GUID && riid == typeof(IVsMessageBoxService).GUID)
            {
                var comInterface = Marshal.GetComInterfaceForObject(this, typeof(IVsMessageBoxService));
                if (comInterface != IntPtr.Zero)
                {
                    hr = Marshal.QueryInterface(comInterface, ref riid, out ppvObject);
                    Marshal.Release(comInterface);
                }
            }

            return hr;
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
            foreach (var handler in s_handlers)
            {
                var result = handler.Handle(lpszText, lpszCaption);
                if (result != DialogResult.None)
                {
                    pidButton = (int)result;
                    return VSConstants.S_OK;
                }
            }

            Assert.True(
                false,
                $"""
                Unexpected dialog box appeared.
                Text: {lpszText}
                Caption: {lpszCaption}
                """);
            throw ExceptionUtilities.Unreachable();
        }
    }
}
