// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    internal class MockCSharpCompiler : CSharpCompiler
    {
        public MockCSharpCompiler(string responseFile, string baseDirectory, string[] args)
            : base(CSharpCommandLineParser.Default, responseFile, args, baseDirectory, Environment.GetEnvironmentVariable("LIB"))
        {
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
