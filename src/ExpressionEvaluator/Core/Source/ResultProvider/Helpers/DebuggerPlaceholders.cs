// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Debugger.Metadata
{
    // Starting in Visual Studio 2015, Update 1, a new api exists to detect if a type is a function pointer.
    // This placeholder method is a temporary shim to allow Roslyn to avoid taking a dependency on Update 1 debugger
    // binaries until Update 1 ships.  See https://github.com/dotnet/roslyn/issues/5428.
    internal static class DebuggerMetadataExtensions
    {
        public static bool IsFunctionPointer(this Type type)
        {
            // Note: The Visual Studio 2015 RTM version of Microsoft.VisualStudio.Debugger.Metadata.dll does not support function pointers at all,
            // so when running against the RTM version of that dll, this method will always return false.  Against the update 1 version,
            // we can exploit the fact that the only time a pointer will ever have a null element type will be function pointers.
            //
            // Using this shim, rather than simply calling Type.IsFunctionPointer() allows the Update 1 expression evaluator to continue
            // to work against the RTM debugger.
            return type.IsPointer && type.GetElementType() == null;
        }
    }
}