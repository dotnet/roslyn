// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// A program location in metadata.
    /// </summary>
    public class MetadataLocation : Location
    {
        private ModuleSymbol module;

        internal MetadataLocation(ModuleSymbol module)
        {
            this.module = module;
        }

        /// <summary>
        /// Gets the metadata module that that this location refers to.
        /// </summary>
        public ModuleSymbol Module
        {
            get
            {
                return module;
            }
        }

        /// <summary>
        /// Gets the assembly that this location refers to.
        /// </summary>
        public AssemblySymbol Assembly
        {
            get
            {
                return module.ContainingAssembly;
            }
        }
    }
}
