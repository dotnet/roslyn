// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.PdbSourceDocument
{
    [Export(typeof(IPdbFileLocatorService)), Shared]
    internal class PdbFileLocatorService : IPdbFileLocatorService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PdbFileLocatorService()
        {
        }

        public Task<Stream?> GetPdbPathAsync(string dllPath, CancellationToken cancellationToken)
        {
            // TODO: Call the debugger to get this

            // Debugger needs:
            // -PDB MVID
            // - PDB Age
            // - PDB TimeStamp
            // - PDB Path
            // - DLL Path
            // 
            // Most of this info comes from the CodeView Debug Directory from the dll

            var pdbPath = Path.ChangeExtension(dllPath, ".pdb");

            if (File.Exists(pdbPath))
            {
                return Task.FromResult<Stream?>(File.OpenRead(pdbPath));
            }

            return Task.FromResult<Stream?>(null);
        }
    }
}
