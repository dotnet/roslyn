// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    internal class MockCsi : CSharpCompiler
    {
        public MockCsi(string responseFile, string baseDirectory, string[] args)
            : base(CSharpCommandLineParser.Interactive, responseFile, args, Path.GetDirectoryName(typeof(CSharpCompiler).Assembly.Location), baseDirectory, RuntimeEnvironment.GetRuntimeDirectory(), null, new SimpleAnalyzerAssemblyLoader())
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
