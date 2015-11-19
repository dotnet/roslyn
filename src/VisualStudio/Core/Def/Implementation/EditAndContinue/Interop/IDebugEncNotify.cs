// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue.Interop
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("347C45E1-5C42-4e0e-9E15-DEFF9CFC7841")]
    internal interface IDebugEncNotify
    {
        // This method allows the ENCManager to tell the package that ENC
        // is not available as soon as it can be determined rather than waiting for a call from the package.
        void __NotifyEncIsUnavailable(/*EncUnavailableReason reason,bool fEditWasApplied*/);

        // This method allows the Lang Service to Notify the package that the Current Statement 
        // must be updated due to an edit.
        void NotifyEncUpdateCurrentStatement();

        // This method allows the Lang Service to Notify the package that an edit was attempted
        // when the debuggee is in a state that cannot accept changes.
        void NotifyEncEditAttemptedAtInvalidStopState();

        // This allows the Lang Service or project to tell the package that it prevented
        // an edit from occurring.
        // The package is responsible for asking the ENC manager why ENC would not be 
        // allowed at this point.
        void NotifyEncEditDisallowedByProject([MarshalAs(UnmanagedType.IUnknown)]object pProject);
    }
}
