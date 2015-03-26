// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.Shell.Interop
{
    // TODO: Remove this definition of IComWrapperFactory and use the one from the VSSDK
    // (Microsoft.VisualStudio.Shell.Interop.14.0.DesignTime) when it is available.
    [ComImport, Guid("436b402a-a479-41a8-a093-9713ce3ad111"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), TypeIdentifier]
    public interface IComWrapperFactory
    {
        object CreateAggregatedObject(object managedObject);
    }
}
