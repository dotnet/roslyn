// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace GenerateDocumentationAndConfigFilesForBrokenRuntime
{
    public static class Program
    {
        public static Task<int> Main(string[] args)
        {
            // Delegate to the actual tool implementation
            return GenerateDocumentationAndConfigFiles.Program.Main(args);
        }
    }
}
