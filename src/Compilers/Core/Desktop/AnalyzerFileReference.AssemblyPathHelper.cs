// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public partial class AnalyzerFileReference
    {
        internal static class AssemblyPathHelper
        {
            internal static string GetCandidatePath(string baseDirectory, AssemblyIdentity assemblyIdentity)
            {
                if (!string.IsNullOrEmpty(assemblyIdentity.CultureName))
                {
                    baseDirectory = Path.Combine(baseDirectory, assemblyIdentity.CultureName);
                }

                return Path.Combine(baseDirectory, assemblyIdentity.Name + ".dll");
            }
        }
    }
}