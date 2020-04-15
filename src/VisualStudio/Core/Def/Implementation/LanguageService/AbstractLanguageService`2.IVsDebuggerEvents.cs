// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal abstract partial class AbstractLanguageService<TPackage, TLanguageService> : IVsDebuggerEvents
    {
        public int OnModeChange(DBGMODE dbgmodeNew)
        {
            var currentDebugMode = _debugMode;
            _debugMode = ConvertDebugMode(dbgmodeNew);

            if (currentDebugMode != _debugMode)
            {
                this.OnDebugModeChanged();
            }

            return VSConstants.S_OK;
        }

        private static DebugMode ConvertDebugMode(DBGMODE dbgmodeNew)
        {
            switch (dbgmodeNew)
            {
                case DBGMODE.DBGMODE_Break:
                    return DebugMode.Break;
                case DBGMODE.DBGMODE_Design:
                    return DebugMode.Design;
                case DBGMODE.DBGMODE_Run:
                    return DebugMode.Run;
                default:
                    throw new ArgumentException();
            }
        }

        private void OnDebugModeChanged()
            => this.LanguageDebugInfo.OnDebugModeChanged(_debugMode);
    }
}
