// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal static class InteractiveHostEntryPoint
    {
        private static async Task<int> Main(string[] args)
        {
            FatalError.Handler = FailFast.OnFatalException;

            try
            {
                await InteractiveHost.Service.RunServerAsync(args).ConfigureAwait(false);
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return 1;
            }
        }
    }
}
