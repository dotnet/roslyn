// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServices.Implementation.Debugging;
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
        {
            this.LanguageDebugInfo.OnDebugModeChanged(_debugMode);
        }
    }
}
