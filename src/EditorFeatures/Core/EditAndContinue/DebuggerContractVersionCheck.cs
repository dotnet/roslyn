// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Allow us to run integration tests on older VS than build that has the required version of Microsoft.VisualStudio.Debugger.Contracts.
    /// </summary>
    internal static class DebuggerContractVersionCheck
    {
        public static bool IsRequiredDebuggerContractVersionAvailable()
        {
            try
            {
                _ = LoadContracts();
                return true;
            }
            catch
            {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static Type LoadContracts()
            => typeof(ManagedActiveStatementUpdate);
    }
}
