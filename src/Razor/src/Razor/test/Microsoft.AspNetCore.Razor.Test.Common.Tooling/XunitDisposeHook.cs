// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET472

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Razor.Test.Common;

// Resolves issues with "System.AppDomainUnloadedException: Attempted to access an unloaded AppDomain." after a test run has finished,
// failing the CI leg/console test run, without any failing tests.
// Copied from https://github.com/dotnet/roslyn/pull/49355

internal sealed class XunitDisposeHook : MarshalByRefObject
{
    internal static void Initialize()
    {
        // Overwrite xunit's app domain handling to not call AppDomain.Unload
        var getDefaultDomain = typeof(AppDomain).GetMethod("GetDefaultDomain", BindingFlags.NonPublic | BindingFlags.Static);
        var defaultDomain = (AppDomain)getDefaultDomain.Invoke(null, null);
        var hook = (XunitDisposeHook)defaultDomain.CreateInstanceFromAndUnwrap(typeof(XunitDisposeHook).Assembly.CodeBase, typeof(XunitDisposeHook).FullName, ignoreCase: false, BindingFlags.CreateInstance, binder: null, args: null, culture: null, activationAttributes: null);
        hook.Execute();
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Invoked across app domains")]
    public void Execute()
    {
        if (!AppDomain.CurrentDomain.IsDefaultAppDomain())
            throw new InvalidOperationException();

        var xunitUtilities = AppDomain.CurrentDomain.GetAssemblies().Where(static assembly => assembly.GetName().Name.StartsWith("xunit.runner.utility")).ToArray();
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
                case Architecture.Arm64:
                    // This place is not a place of honor. No highly esteemed deed is commemorated here.
                    Marshal.WriteInt64(functionPointer, 0xD65F03C0);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}

#endif
