// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal static class CompilerServerUtils
    {
        internal static bool ParseCommandLine(string[] args, out string pipeName, out bool shutdown)
        {
            pipeName = null;
            shutdown = false;

            foreach (var arg in args)
            {
                const string pipeArgPrefix = "-pipename:";
                if (arg.StartsWith(pipeArgPrefix, StringComparison.Ordinal))
                {
                    pipeName = arg.Substring(pipeArgPrefix.Length);
                }
                else if (arg == "-shutdown")
                {
                    shutdown = true;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}
