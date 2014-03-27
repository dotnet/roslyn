// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents a reference to another C# compilation. 
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal sealed class CSharpCompilationReference : CompilationReference
    {
        /// <summary>
        /// Returns the referenced Compilation.
        /// </summary>
        public new CSharpCompilation Compilation { get; private set; }

        internal override Compilation CompilationCore
        {
            get { return this.Compilation; }
        }

        /// <summary>
        /// Create a metadata reference to a compilation.
        /// </summary>
        /// <param name="compilation">The compilation to reference.</param>
        /// <param name="aliases">Extern aliases for this reference.</param>
        /// <param name="embedInteropTypes">Should interop types be embedded in the created assembly?</param>
        public CSharpCompilationReference(
            CSharpCompilation compilation,
            ImmutableArray<string> aliases = default(ImmutableArray<string>),
            bool embedInteropTypes = false)
            : base(GetProperties(compilation, aliases, embedInteropTypes))
        {
            this.Compilation = compilation;
        }

        private string GetDebuggerDisplay()
        {
            return CSharpResources.CompilationC + this.Compilation.AssemblyName;
        }
    }
}
