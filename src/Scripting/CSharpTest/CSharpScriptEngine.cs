// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Scripting.CSharp
{
    /// <summary>
    /// Represents a runtime execution context for C# scripts.
    /// </summary>
    internal sealed class CSharpScriptEngine : ScriptEngine
    {
        public CSharpScriptEngine(MetadataFileReferenceProvider metadataReferenceProvider = null)
            : base(ScriptOptions.Default, metadataReferenceProvider)
        {
        }

        internal override Script<T> Create<T>(string code, ScriptOptions options, Type globalsType)
        {
            return CSharpScript.Create<T>(code, options).WithGlobalsType(globalsType).WithBuilder(this.Builder);
        }
    }
}
