// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    internal sealed class XunitDisposeHook : MarshalByRefObject
    {
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

                var method = appDomainManagerType.GetMethod("Dispose");
                RuntimeHelpers.PrepareMethod(method.MethodHandle);
                var functionPointer = method.MethodHandle.GetFunctionPointer();
                if (IntPtr.Size == 4)
                {
                    // Overwrite the compiled method to just return

                    var offset = 0;

                    // push ebp
                    Marshal.WriteByte(functionPointer, offset++, 0x55);

                    // mov ebp,esp
                    Marshal.WriteByte(functionPointer, offset++, 0x8B);
                    Marshal.WriteByte(functionPointer, offset++, 0xEC);

                    // lea esp,[ebp-8]
                    Marshal.WriteByte(functionPointer, offset++, 0x8D);
                    Marshal.WriteByte(functionPointer, offset++, 0x65);
                    Marshal.WriteByte(functionPointer, offset++, 0xF8);

                    // pop ebp
                    Marshal.WriteByte(functionPointer, offset++, 0x5D);

                    // ret
                    Marshal.WriteByte(functionPointer, offset++, 0xC3);
                }
                else
                {
                    // Overwrite the compiled method to just return

                    // ret
                    Marshal.WriteByte(functionPointer, 0xC3);
                }
            }
        }
    }
}
