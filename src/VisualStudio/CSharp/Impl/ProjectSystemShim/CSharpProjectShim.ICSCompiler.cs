// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
{
    internal partial class CSharpProjectShim : ICSCompiler
    {
        public ICSSourceModule CreateSourceModule(ICSSourceText text)
        {
            throw new NotImplementedException();
        }

        public ICSNameTable GetNameTable()
        {
            throw new NotImplementedException();
        }

        public void Shutdown()
        {
            throw new NotImplementedException();
        }

        public ICSCompilerConfig GetConfiguration()
        {
            return this;
        }

        public ICSInputSet AddInputSet()
        {
            throw new NotImplementedException();
        }

        public void RemoveInputSet(ICSInputSet inputSet)
        {
            throw new NotImplementedException();
        }

        public void Compile(ICSCompileProgress progress)
        {
            throw new NotImplementedException();
        }

        public void BuildForEnc(ICSCompileProgress progress, ICSEncProjectServices encService, object pe)
        {
            throw new NotImplementedException();
        }

        public object CreateParser()
        {
            throw new NotImplementedException();
        }

        public object CreateLanguageAnalysisEngine()
        {
            throw new NotImplementedException();
        }

        public void ReleaseReservedMemory()
        {
            throw new NotImplementedException();
        }
    }
}
