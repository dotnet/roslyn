// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using EmitContext = Microsoft.CodeAnalysis.Emit.EmitContext;

namespace Microsoft.Cci
{
    /// <summary>
    /// Visitor to force translation of all symbols that will be referred to
    /// in metadata. Allows us to build the set of types that must be embedded
    /// as local types.
    /// </summary>
    internal sealed class NoPiaReferenceIndexer : ReferenceIndexerBase
    {
        internal NoPiaReferenceIndexer(EmitContext context)
            : base(context)
        {
            this.module = context.Module;
        }

        public override void Visit(IAssembly assembly)
        {
            Debug.Assert(assembly == module);
            this.Visit((IModule)assembly);
        }

        public override void Visit(IModule module)
        {
            Debug.Assert(this.module == module);

            //EDMAURER visit these assembly-level attributes even when producing a module.
            //They'll be attached off the "AssemblyAttributesGoHere" typeRef if a module is being produced.

            this.Visit(module.AssemblyAttributes);
            this.Visit(module.AssemblySecurityAttributes);
            this.Visit(module.ModuleAttributes);
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
