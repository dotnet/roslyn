// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    public abstract class ScriptCompilationInfo
    {
        public Type ReturnType { get; }
        public Type GlobalsType { get; }

        internal ScriptCompilationInfo(Type returnType, Type globalsType)
        {
            ReturnType = returnType ?? typeof(object);
            GlobalsType = globalsType;
        }

        public Compilation PreviousScriptCompilation => CommonPreviousScriptCompilation;
        internal abstract Compilation CommonPreviousScriptCompilation { get; }

        public ScriptCompilationInfo WithPreviousScriptCompilation(Compilation compilation) => CommonWithPreviousScriptCompilation(compilation);
        internal abstract ScriptCompilationInfo CommonWithPreviousScriptCompilation(Compilation compilation);
    }
}
