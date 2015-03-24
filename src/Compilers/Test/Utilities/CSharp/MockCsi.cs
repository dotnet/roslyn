// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    internal class MockCsi : CSharpCompiler
    {
        public MockCsi(string responseFIle, string baseDirectory, string[] args)
            : base(CSharpCommandLineParser.Interactive, responseFIle, args, baseDirectory, null)
        {
        }

        public override Assembly LoadAssembly(string fullPath)
        {
            throw new NotImplementedException();
        }

        protected override void CompilerSpecificSqm(IVsSqmMulti sqm, uint sqmSession)
        {
            throw new NotImplementedException();
        }

        protected override uint GetSqmAppID()
        {
            throw new NotImplementedException();
        }
    }
}
