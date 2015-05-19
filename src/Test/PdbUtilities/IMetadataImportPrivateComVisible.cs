// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Roslyn.Utilities.Pdb
{
    // Since we're just blindly passing this interface through managed code to the Symbinder, we don't care about actually
    // importing the specific methods.
    // This needs to be public so that we can call Marshal.GetComInterfaceForObject() on it to get the
    // underlying metadata pointer.
    // That doesn't mean that you should actually use it though because the interface is basically empty.
    [Guid("7DAC8207-D3AE-4c75-9B67-92801A497D44"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    public interface IMetadataImportPrivateComVisible
    {
        // Just need a single placeholder method so that it doesn't complain about an empty interface.
        void Placeholder();
    }
}
