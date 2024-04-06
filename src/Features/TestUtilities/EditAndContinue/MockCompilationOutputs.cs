// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal class MockCompilationOutputs : CompilationOutputs
    {
        private readonly Guid _mvid;

        public Func<Stream?>? OpenAssemblyStreamImpl { get; set; }
        public Func<Stream?>? OpenPdbStreamImpl { get; set; }

        public MockCompilationOutputs(Guid mvid)
            => _mvid = mvid;

        public override string AssemblyDisplayPath => "test-assembly";
        public override string PdbDisplayPath => "test-pdb";

        protected override Stream? OpenAssemblyStream()
            => (OpenAssemblyStreamImpl ?? throw new NotImplementedException())();

        protected override Stream? OpenPdbStream()
            => (OpenPdbStreamImpl ?? throw new NotImplementedException())();

        internal override Guid ReadAssemblyModuleVersionId()
            => _mvid;
    }
}
