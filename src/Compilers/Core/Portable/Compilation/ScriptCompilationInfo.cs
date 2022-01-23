// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    public abstract class ScriptCompilationInfo
    {
        internal Type? ReturnTypeOpt { get; }
        public Type ReturnType => ReturnTypeOpt ?? typeof(object);
        public Type? GlobalsType { get; }

        internal ScriptCompilationInfo(Type? returnType, Type? globalsType)
        {
            ReturnTypeOpt = returnType;
            GlobalsType = globalsType;
        }

        public Compilation? PreviousScriptCompilation => CommonPreviousScriptCompilation;
        internal abstract Compilation? CommonPreviousScriptCompilation { get; }

        public ScriptCompilationInfo WithPreviousScriptCompilation(Compilation? compilation) => CommonWithPreviousScriptCompilation(compilation);
        internal abstract ScriptCompilationInfo CommonWithPreviousScriptCompilation(Compilation? compilation);
    }
}
