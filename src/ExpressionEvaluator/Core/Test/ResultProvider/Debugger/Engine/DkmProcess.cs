// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

namespace Microsoft.VisualStudio.Debugger
{
    public class DkmProcess
    {
        public readonly DkmEngineSettings EngineSettings = new DkmEngineSettings();

        private readonly bool _nativeDebuggingEnabled;

        public DkmProcess(bool enableNativeDebugging)
        {
            _nativeDebuggingEnabled = enableNativeDebugging;
        }

        public DkmRuntimeInstance GetNativeRuntimeInstance()
        {
            if (!_nativeDebuggingEnabled)
            {
                throw new DkmException(DkmExceptionCode.E_XAPI_DATA_ITEM_NOT_FOUND);
            }

            return null; // Value isn't required for testing
        }
    }

    public class DkmThread
    {
    }
}
