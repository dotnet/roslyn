// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    internal abstract class ReferenceIndexer : ReferenceIndexerBase
    {
        protected readonly MetadataWriter metadataWriter;

        internal ReferenceIndexer(MetadataWriter metadataWriter)
            : base(metadataWriter.Context)
        {
            this.metadataWriter = metadataWriter;
        }

        public override void Visit(IAssembly assembly)
        {
            this.module = assembly;
            this.Visit((IModule)assembly);
            this.Visit(assembly.GetFiles(Context));
            this.Visit(assembly.GetResources(Context));
        }

        public override void Visit(IModule module)
        {
            this.module = module;

            //EDMAURER visit these assembly-level attributes even when producing a module.
            //They'll be attached off the "AssemblyAttributesGoHere" typeRef if a module is being produced.

            this.Visit(module.AssemblyAttributes);
            this.Visit(module.AssemblySecurityAttributes);

            this.Visit(module.GetAssemblyReferences(Context));
            this.Visit(module.ModuleReferences);
            this.Visit(module.ModuleAttributes);
            this.Visit(module.GetTopLevelTypes(Context));
            this.Visit(module.GetExportedTypes(Context));

            if (module.AsAssembly == null)
            {
                this.Visit(module.GetResources(Context));
            }
        }

        public void VisitMethodBodyReference(IReference reference)
        {
            var typeReference = reference as ITypeReference;
            if (typeReference != null)
            {
                this.typeReferenceNeedsToken = true;
                this.Visit(typeReference);
                Debug.Assert(!this.typeReferenceNeedsToken);
            }
            else
            {
                var fieldReference = reference as IFieldReference;
                if (fieldReference != null)
                {
                    if (fieldReference.IsContextualNamedEntity)
                    {
                        ((IContextualNamedEntity)fieldReference).AssociateWithMetadataWriter(this.metadataWriter);
                    }

                    this.Visit(fieldReference);
                }
                else
                {
                    var methodReference = reference as IMethodReference;
                    if (methodReference != null)
                    {
                        this.Visit(methodReference);
                    }
                }
            }
        }

        protected override void RecordAssemblyReference(IAssemblyReference assemblyReference)
        {
            this.metadataWriter.GetAssemblyRefIndex(assemblyReference);
        }

        protected override void ProcessMethodBody(IMethodDefinition method)
        {
            if (method.HasBody())
            {
                var body = method.GetBody(Context);

                if (body != null)
                {
                    this.Visit(body);
                }
                else if (!metadataWriter.allowMissingMethodBodies)
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }
        }

        protected override void RecordTypeReference(ITypeReference typeReference)
        {
            this.metadataWriter.RecordTypeReference(typeReference);
        }

        protected override void RecordTypeMemberReference(ITypeMemberReference typeMemberReference)
        {
            this.metadataWriter.GetMemberRefIndex(typeMemberReference);
        }

        protected override void RecordFileReference(IFileReference fileReference)
        {
            this.metadataWriter.GetFileRefIndex(fileReference);
        }

        protected override void ReserveMethodToken(IMethodReference methodReference)
        {
            this.metadataWriter.GetMethodToken(methodReference);
        }

        protected override void ReserveFieldToken(IFieldReference fieldReference)
        {
            this.metadataWriter.GetFieldToken(fieldReference);
        }

        protected override void RecordModuleReference(IModuleReference moduleReference)
        {
            this.metadataWriter.GetModuleRefIndex(moduleReference.Name);
        }

        public override void Visit(IPlatformInvokeInformation platformInvokeInformation)
        {
            this.metadataWriter.GetModuleRefIndex(platformInvokeInformation.ModuleName);
        }
    }
}
