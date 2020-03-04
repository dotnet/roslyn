﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Emit;
using EmitContext = Microsoft.CodeAnalysis.Emit.EmitContext;

namespace Microsoft.Cci
{
    /// <summary>
    /// Visitor to force translation of all symbols that will be referred to
    /// in metadata. Allows us to build the set of types that must be embedded
    /// as local types (for NoPia).
    /// </summary>
    internal sealed class TypeReferenceIndexer : ReferenceIndexerBase
    {
        internal TypeReferenceIndexer(EmitContext context)
            : base(context)
        {
        }

        public override void Visit(CommonPEModuleBuilder module)
        {
            //EDMAURER visit these assembly-level attributes even when producing a module.
            //They'll be attached off the "AssemblyAttributesGoHere" typeRef if a module is being produced.

            this.Visit(module.GetSourceAssemblyAttributes(Context.IsRefAssembly));
            this.Visit(module.GetSourceAssemblySecurityAttributes());
            this.Visit(module.GetSourceModuleAttributes());
        }

        protected override void RecordAssemblyReference(IAssemblyReference assemblyReference)
        {
        }

        protected override void RecordFileReference(IFileReference fileReference)
        {
        }

        protected override void RecordModuleReference(IModuleReference moduleReference)
        {
        }

        public override void Visit(IPlatformInvokeInformation platformInvokeInformation)
        {
        }

        protected override void ProcessMethodBody(IMethodDefinition method)
        {
        }

        protected override void RecordTypeReference(ITypeReference typeReference)
        {
        }

        protected override void ReserveFieldToken(IFieldReference fieldReference)
        {
        }

        protected override void ReserveMethodToken(IMethodReference methodReference)
        {
        }

        protected override void RecordTypeMemberReference(ITypeMemberReference typeMemberReference)
        {
        }
    }
}
