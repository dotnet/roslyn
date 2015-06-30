// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Scripting.CSharp
{
    /// <summary>
    /// Represents a runtime execution context for C# scripts.
    /// </summary>
    internal sealed class CSharpScriptEngine : ScriptEngine
    {
        public CSharpScriptEngine(MetadataFileReferenceProvider metadataReferenceProvider = null)
            : base(metadataReferenceProvider)
        {
        }

        internal override Script Create(string code, ScriptOptions options, Type globalsType, Type returnType)
        {
            return CSharpScript.Create(code, options).WithGlobalsType(globalsType).WithReturnType(returnType).WithBuilder(this.Builder);
        }
    }
}
