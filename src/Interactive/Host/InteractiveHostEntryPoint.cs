// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal static class InteractiveHostEntryPoint
    {
        private static int Main(string[] args)
        {
            try
            {
                InteractiveHost.Service.RunServer(args);
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
