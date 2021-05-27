// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
{
    internal partial class CSharpProjectShim : ICSCompiler
    {
        public ICSSourceModule CreateSourceModule(ICSSourceText text)
            => throw new NotImplementedException();

        public ICSNameTable GetNameTable()
            => throw new NotImplementedException();

        public void Shutdown()
            => throw new NotImplementedException();

        public ICSCompilerConfig GetConfiguration()
            => this;

        public ICSInputSet AddInputSet()
            => throw new NotImplementedException();

        public void RemoveInputSet(ICSInputSet inputSet)
            => throw new NotImplementedException();

        public void Compile(ICSCompileProgress progress)
            => throw new NotImplementedException();

        public void BuildForEnc(ICSCompileProgress progress, ICSEncProjectServices encService, object pe)
            => throw new NotImplementedException();

        public object CreateParser()
            => throw new NotImplementedException();

        public object CreateLanguageAnalysisEngine()
            => throw new NotImplementedException();

        public void ReleaseReservedMemory()
            => throw new NotImplementedException();
    }
}
