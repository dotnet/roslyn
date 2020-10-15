﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal static class InteractiveHostEntryPoint
    {
        [SupportedOSPlatform("windows")]
        private static async Task<int> Main(string[] args)
        {
            FatalError.Handler = FailFast.OnFatalException;

            // Disables Windows Error Reporting for the process, so that the process fails fast.
            SetErrorMode(GetErrorMode() | ErrorMode.SEM_FAILCRITICALERRORS | ErrorMode.SEM_NOOPENFILEERRORBOX | ErrorMode.SEM_NOGPFAULTERRORBOX);

            Control? control = null;
            using (var resetEvent = new ManualResetEventSlim(false))
            {
                var uiThread = new Thread(() =>
                {
                    control = new Control();
                    control.CreateControl();
                    resetEvent.Set();
                    Application.Run();
                });

                uiThread.SetApartmentState(ApartmentState.STA);
                uiThread.IsBackground = true;
                uiThread.Start();
                resetEvent.Wait();
            }

            var invokeOnMainThread = new Func<Func<object>, object>(operation => control!.Invoke(operation));

            try
            {
                await InteractiveHost.Service.RunServerAsync(args, invokeOnMainThread).ConfigureAwait(false);
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return 1;
            }
        }

        [SupportedOSPlatform("windows")]
        [DllImport("kernel32", PreserveSig = true)]
        internal static extern ErrorMode SetErrorMode(ErrorMode mode);

        [SupportedOSPlatform("windows")]
        [DllImport("kernel32", PreserveSig = true)]
        internal static extern ErrorMode GetErrorMode();

        [Flags]
        internal enum ErrorMode : int
        {
            /// <summary>
            /// Use the system default, which is to display all error dialog boxes.
            /// </summary>
            SEM_FAILCRITICALERRORS = 0x0001,

            /// <summary>
            /// The system does not display the critical-error-handler message box. Instead, the system sends the error to the calling process.
            /// Best practice is that all applications call the process-wide SetErrorMode function with a parameter of SEM_FAILCRITICALERRORS at startup. 
            /// This is to prevent error mode dialogs from blocking the application.
            /// </summary>
            SEM_NOGPFAULTERRORBOX = 0x0002,

            /// <summary>
            /// The system automatically fixes memory alignment faults and makes them invisible to the application. 
            /// It does this for the calling process and any descendant processes. This feature is only supported by 
            /// certain processor architectures. For more information, see the Remarks section.
            /// After this value is set for a process, subsequent attempts to clear the value are ignored.
            /// </summary>
            SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,

            /// <summary>
            /// The system does not display a message box when it fails to find a file. Instead, the error is returned to the calling process.
            /// </summary>
            SEM_NOOPENFILEERRORBOX = 0x8000,
        }
    }
}
