// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.Test.Utilities;

internal sealed class XunitDisposeHook : MarshalByRefObject
{
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Invoked across app domains")]
    public void Execute()
    {
        if (!AppDomain.CurrentDomain.IsDefaultAppDomain())
            throw new InvalidOperationException();

        var xunitUtilities = AppDomain.CurrentDomain.GetAssemblies().Where(static assembly => assembly.GetName().Name.StartsWith("xunit.runner.visualstudio")).ToArray();
        foreach (var xunitUtility in xunitUtilities)
        {
            var appDomainManagerType = xunitUtility.GetType("Xunit.AppDomainManager_AppDomain");
            if (appDomainManagerType is null)
                continue;

            // AppDomainManager_AppDomain.Dispose() calls AppDomain.Unload(), which is unfortunately not reliable
            // when the test creates STA COM objects. Since this call to Unload() only occurs at the end of testing
            // (immediately before the process is going to close anyway), we can simply hot-patch the executable
            // code in Dispose() to return without taking any action.
            //
            // This is a workaround for https://github.com/xunit/xunit/issues/2097. The fix in
            // https://github.com/xunit/xunit/pull/2192 was not viable because xunit v2 is no longer shipping
            // updates. Once xunit v3 is available, it will no longer be necessary.
            var method = appDomainManagerType.GetMethod("Dispose");
            RuntimeHelpers.PrepareMethod(method.MethodHandle);
            var functionPointer = method.MethodHandle.GetFunctionPointer();

            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X86:
                case Architecture.X64:
                    // 😱 Overwrite the compiled method to just return.
                    // Note that the same sequence works for x86 and x64.

                    // ret
                    Marshal.WriteByte(functionPointer, 0xC3);
                    break;

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
