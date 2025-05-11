// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\Concord\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

namespace Microsoft.VisualStudio.Debugger
{
    //
    // Summary:
    //     Defines the HRESULT codes used by this API.
    public enum DkmExceptionCode
    {
        //
        // Summary:
        //     Unspecified error.
        E_FAIL = -2147467259,
        //
        // Summary:
        //     The process has been terminated.
        E_PROCESS_DESTROYED = -2147221392,
        //
        // Summary:
        //     The network connection to the Visual Studio Remote Debugger was lost.
        E_XAPI_REMOTE_DISCONNECTED = -1898053615,
        //
        // Summary:
        //     The network connection to the Visual Studio Remote Debugger has been closed.
        E_XAPI_REMOTE_CLOSED = -1898053614,
        //
        // Summary:
        //     A data item cannot be for this component found with the given data item ID.
        E_XAPI_DATA_ITEM_NOT_FOUND = -1898053608,
        //
        // Summary:
        //     A component dll could not be found. If failures continue, try disabling any installed
        //     add-ins or repairing your installation.
        E_XAPI_COMPONENT_DLL_NOT_FOUND = -1898053597,
        E_METADATA_UPDATE_DEADLOCK = -1842151264,
    }
}
