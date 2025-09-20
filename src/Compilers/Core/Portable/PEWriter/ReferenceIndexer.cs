// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.Cci
{
    internal abstract class ReferenceIndexer : ReferenceIndexerBase
    {
        protected readonly MetadataWriter metadataWriter;
        private readonly HashSet<IImportScope> _alreadySeenScopes = new HashSet<IImportScope>();

        internal ReferenceIndexer(MetadataWriter metadataWriter)
            : base(metadataWriter.Context)
        {
            this.metadataWriter = metadataWriter;
        }

        public override void Visit(CommonPEModuleBuilder module)
        {
            // Visit these assembly-level attributes even when producing a module.
            // They'll be attached off the "AssemblyAttributesGoHere" typeRef if a module is being produced.
            Visit(module.GetSourceAssemblyAttributes(Context.IsRefAssembly));
            Visit(module.GetSourceAssemblySecurityAttributes());

            Visit(module.GetAssemblyReferences(Context));
            Visit(module.GetSourceModuleAttributes());
            Visit(module.GetTopLevelTypeDefinitions(Context));

            foreach (var exportedType in module.GetExportedTypes(Context))
            {
                VisitExportedType(exportedType.Type);
            }

            Visit(module.GetResources(Context));
            VisitImports(module.GetImports());
            Visit(module.GetFiles(Context));
        }

        private void VisitExportedType(ITypeReference exportedType)
        {
            // Do not visit the reference to aliased type, it does not get into the type ref table based only on its membership of the exported types collection.
            // but DO visit the reference to assembly (if any) that defines the aliased type. That assembly might not already be in the assembly reference list.
            var definingUnit = MetadataWriter.GetDefiningUnitReference(exportedType, Context);
            var definingAssembly = definingUnit as IAssemblyReference;
            if (definingAssembly != null)
            {
                Visit(definingAssembly);
            }
            else
            {
                definingAssembly = ((IModuleReference)definingUnit).GetContainingAssembly(Context);
                if (definingAssembly != null && !ReferenceEquals(definingAssembly, Context.Module.GetContainingAssembly(Context)))
                {
                    Visit(definingAssembly);
                }
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
            this.metadataWriter.GetAssemblyReferenceHandle(assemblyReference);
        }

        protected override void ProcessMethodBody(IMethodDefinition method)
        {
            if (method.HasBody && !metadataWriter.MetadataOnly)
            {
                var body = method.GetBody(Context);
                Debug.Assert(body != null);

                this.Visit(body);

                for (IImportScope scope = body.ImportScope; scope != null; scope = scope.Parent)
                {
                    if (_alreadySeenScopes.Add(scope))
                    {
                        VisitImports(scope.GetUsedNamespaces(Context));
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private void VisitImports(ImmutableArray<UsedNamespaceOrType> imports)
        {
            // Visit type and assembly references in import scopes.
            // These references are emitted to Portable debug metadata,
            // so they need to be present in the assembly metadata.
            // It may happen that some using/import clause references an assembly/type 
            // that is not actually used in IL. Although rare we need to handle such cases.
            // We include these references regardless of the format for debugging information 
            // to avoid dependency of metadata on the chosen debug format.

            foreach (var import in imports)
            {
                if (import.TargetAssemblyOpt != null)
                {
                    Visit(import.TargetAssemblyOpt);
                }

                if (import.TargetTypeOpt != null)
                {
                    this.typeReferenceNeedsToken = true;
                    Visit(import.TargetTypeOpt);
                    Debug.Assert(!this.typeReferenceNeedsToken);
                }
            }
        }

        protected override void RecordTypeReference(ITypeReference typeReference)
        {
            this.metadataWriter.GetTypeHandle(typeReference);
        }

        protected override void RecordTypeMemberReference(ITypeMemberReference typeMemberReference)
        {
            this.metadataWriter.GetMemberReferenceHandle(typeMemberReference);
        }

        protected override void RecordFileReference(IFileReference fileReference)
        {
            this.metadataWriter.GetAssemblyFileHandle(fileReference);
        }

        protected override void ReserveMethodToken(IMethodReference methodReference)
        {
            this.metadataWriter.GetMethodHandle(methodReference);
        }

        protected override void ReserveFieldToken(IFieldReference fieldReference)
        {
            this.metadataWriter.GetFieldHandle(fieldReference);
        }

        protected override void RecordModuleReference(IModuleReference moduleReference)
        {
            this.metadataWriter.GetModuleReferenceHandle(moduleReference.Name);
        }

        public override void Visit(IPlatformInvokeInformation platformInvokeInformation)
        {
            this.metadataWriter.GetModuleReferenceHandle(platformInvokeInformation.ModuleName);
        }
    }
}
