// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    public sealed class CSharpScriptCompilationInfo : ScriptCompilationInfo
    {
        public new CSharpCompilation PreviousScriptCompilation { get; }

        internal CSharpScriptCompilationInfo(CSharpCompilation previousCompilationOpt, Type returnType, Type globalsType)
            : base(returnType, globalsType)
        {
            Debug.Assert(previousCompilationOpt == null || previousCompilationOpt.HostObjectType == globalsType);

            PreviousScriptCompilation = previousCompilationOpt;
        }

        internal override Compilation CommonPreviousScriptCompilation => PreviousScriptCompilation;

        public CSharpScriptCompilationInfo WithPreviousScriptCompilation(CSharpCompilation compilation) =>
            (compilation == PreviousScriptCompilation) ? this : new CSharpScriptCompilationInfo(compilation, ReturnType, GlobalsType);

        internal override ScriptCompilationInfo CommonWithPreviousScriptCompilation(Compilation compilation) =>
            WithPreviousScriptCompilation((CSharpCompilation)compilation);
    }
}
