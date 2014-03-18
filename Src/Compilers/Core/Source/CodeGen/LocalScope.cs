// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Cci = Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// Scope representation for PDB emit.
    /// </summary>
    internal class LocalScope : Cci.ILocalScope
    {
        private readonly uint offset;
        private readonly uint length;

        private readonly Cci.IMethodDefinition parent;
        private readonly IEnumerable<Cci.ILocalDefinition> constantSymbols;
        private readonly IEnumerable<Cci.ILocalDefinition> localSymbols;

        internal LocalScope(uint begin, uint end, Cci.IMethodDefinition parent, IEnumerable<Cci.ILocalDefinition> constantSymbols, IEnumerable<Cci.ILocalDefinition> localSymbols)
        {
            //we should not create 0-length scopes as they are useless.
            //however we will allow the case of "begin == end" as that is how edge inclusive scopes of length 1 are represented.
            System.Diagnostics.Debug.Assert(begin <= end);

            this.offset = begin;
            this.length = end - begin;
            this.parent = parent;
            this.constantSymbols = constantSymbols;
            this.localSymbols = localSymbols;
        }

        public uint Offset
        {
            get { return offset; }
        }

        public uint Length
        {
            get { return length; }
        }

        //TODO: this seems unused
        public Cci.IMethodDefinition MethodDefinition
        {
            get { return this.parent; }
        }

        public IEnumerable<Cci.ILocalDefinition> Constants
        {
            get { return constantSymbols; }
        }
        public IEnumerable<Cci.ILocalDefinition> Variables
        {
            get { return localSymbols; }
        }
    }
}
