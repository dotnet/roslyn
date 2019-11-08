// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Roslyn.Utilities;
using EmitContext = Microsoft.CodeAnalysis.Emit.EmitContext;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.Cci
{
    internal abstract class ReferenceIndexerBase : MetadataVisitor
    {
        private readonly HashSet<IReference> _alreadySeen = new HashSet<IReference>();
        private readonly HashSet<IReference> _alreadyHasToken = new HashSet<IReference>();
        protected bool typeReferenceNeedsToken;

        internal ReferenceIndexerBase(EmitContext context)
            : base(context)
        {
        }

        public override void Visit(IAssemblyReference assemblyReference)
        {
            if (assemblyReference != Context.Module.GetContainingAssembly(Context))
            {
                RecordAssemblyReference(assemblyReference);
            }
        }

        protected abstract void RecordAssemblyReference(IAssemblyReference assemblyReference);

        public override void Visit(ICustomModifier customModifier)
        {
            this.typeReferenceNeedsToken = true;
            this.Visit(customModifier.GetModifier(Context));
        }

        public override void Visit(IEventDefinition eventDefinition)
        {
            this.typeReferenceNeedsToken = true;
            this.Visit(eventDefinition.GetType(Context));
            Debug.Assert(!this.typeReferenceNeedsToken);
        }

        public override void Visit(IFieldReference fieldReference)
        {
            if (!_alreadySeen.Add(fieldReference))
            {
                return;
            }

            IUnitReference definingUnit = MetadataWriter.GetDefiningUnitReference(fieldReference.GetContainingType(Context), Context);
            if (definingUnit != null && ReferenceEquals(definingUnit, Context.Module))
            {
                return;
            }

            this.Visit((ITypeMemberReference)fieldReference);
            this.Visit(fieldReference.GetType(Context));
            ReserveFieldToken(fieldReference);
        }

        protected abstract void ReserveFieldToken(IFieldReference fieldReference);

        public override void Visit(IFileReference fileReference)
        {
            RecordFileReference(fileReference);
        }

        protected abstract void RecordFileReference(IFileReference fileReference);

        public override void Visit(IGenericMethodInstanceReference genericMethodInstanceReference)
        {
            this.Visit(genericMethodInstanceReference.GetGenericArguments(Context));
            this.Visit(genericMethodInstanceReference.GetGenericMethod(Context));
        }

        public override void Visit(IGenericParameter genericParameter)
        {
            this.Visit(genericParameter.GetAttributes(Context));
            this.VisitTypeReferencesThatNeedTokens(genericParameter.GetConstraints(Context));
        }

        public override void Visit(IGenericTypeInstanceReference genericTypeInstanceReference)
        {
            // ^ ensures this.path.Count == old(this.path.Count);
            INestedTypeReference nestedType = genericTypeInstanceReference.AsNestedTypeReference;

            if (nestedType != null)
            {
                ITypeReference containingType = nestedType.GetContainingType(Context);

                if (containingType.AsGenericTypeInstanceReference != null ||
                    containingType.AsSpecializedNestedTypeReference != null)
                {
                    this.Visit(nestedType.GetContainingType(Context));
                }
            }

            this.Visit(genericTypeInstanceReference.GetGenericType(Context));
            this.Visit(genericTypeInstanceReference.GetGenericArguments(Context));
        }

        public override void Visit(IMarshallingInformation marshallingInformation)
        {
            // The type references in the marshalling information do not end up in tables, but are serialized as strings.
        }

        public override void Visit(IMethodDefinition method)
        {
            base.Visit(method);
            ProcessMethodBody(method);
        }

        protected abstract void ProcessMethodBody(IMethodDefinition method);

        public override void Visit(IMethodReference methodReference)
        {
            IGenericMethodInstanceReference genericMethodInstanceReference = methodReference.AsGenericMethodInstanceReference;
            if (genericMethodInstanceReference != null)
            {
                this.Visit(genericMethodInstanceReference);
                return;
            }

            if (!_alreadySeen.Add(methodReference))
            {
                return;
            }

            // If we have a ref to a varargs method then we always generate an entry in the MethodRef table,
            // even if it is a method in the current module. (Note that we are not *required* to do so if 
            // in fact the number of extra arguments passed is zero; in that case we are permitted to use
            // an ordinary method def token. We consistently choose to emit a method ref regardless.)

            IUnitReference definingUnit = MetadataWriter.GetDefiningUnitReference(methodReference.GetContainingType(Context), Context);
            if (definingUnit != null && ReferenceEquals(definingUnit, Context.Module) && !methodReference.AcceptsExtraArguments)
            {
                return;
            }

            this.Visit((ITypeMemberReference)methodReference);
            ISpecializedMethodReference specializedMethodReference = methodReference.AsSpecializedMethodReference;
            if (specializedMethodReference != null)
            {
                IMethodReference unspecializedMethodReference = specializedMethodReference.UnspecializedVersion;
                this.Visit(unspecializedMethodReference.GetType(Context));
                this.Visit(unspecializedMethodReference.GetParameters(Context));
                this.Visit(unspecializedMethodReference.RefCustomModifiers);
                this.Visit(unspecializedMethodReference.ReturnValueCustomModifiers);
            }
            else
            {
                this.Visit(methodReference.GetType(Context));
                this.Visit(methodReference.GetParameters(Context));
                this.Visit(methodReference.RefCustomModifiers);
                this.Visit(methodReference.ReturnValueCustomModifiers);
            }

            if (methodReference.AcceptsExtraArguments)
            {
                this.Visit(methodReference.ExtraParameters);
            }

            ReserveMethodToken(methodReference);
        }

        protected abstract void ReserveMethodToken(IMethodReference methodReference);

        public override abstract void Visit(CommonPEModuleBuilder module);

        public override void Visit(IModuleReference moduleReference)
        {
            if (moduleReference != Context.Module)
            {
                RecordModuleReference(moduleReference);
            }
        }

        protected abstract void RecordModuleReference(IModuleReference moduleReference);

        public override abstract void Visit(IPlatformInvokeInformation platformInvokeInformation);

        public override void Visit(INamespaceTypeReference namespaceTypeReference)
        {
            if (!this.typeReferenceNeedsToken && namespaceTypeReference.TypeCode != PrimitiveTypeCode.NotPrimitive)
            {
                return;
            }

            RecordTypeReference(namespaceTypeReference);

            var unit = namespaceTypeReference.GetUnit(Context);

            var assemblyReference = unit as IAssemblyReference;
            if (assemblyReference != null)
            {
                this.Visit(assemblyReference);
            }
            else
            {
                var moduleReference = unit as IModuleReference;
                if (moduleReference != null)
                {
                    // If this is a module from a referenced multi-module assembly,
                    // the assembly should be used as the resolution scope. 
                    assemblyReference = moduleReference.GetContainingAssembly(Context);
                    if (assemblyReference != null && assemblyReference != Context.Module.GetContainingAssembly(Context))
                    {
                        this.Visit(assemblyReference);
                    }
                    else
                    {
                        this.Visit(moduleReference);
                    }
                }
            }
        }

        protected abstract void RecordTypeReference(ITypeReference typeReference);

        public override void Visit(INestedTypeReference nestedTypeReference)
        {
            if (!this.typeReferenceNeedsToken && nestedTypeReference.AsSpecializedNestedTypeReference != null)
            {
                return;
            }

            RecordTypeReference(nestedTypeReference);
        }

        public override void Visit(IPropertyDefinition propertyDefinition)
        {
            this.Visit(propertyDefinition.Parameters);
        }

        public override void Visit(ManagedResource resourceReference)
        {
            this.Visit(resourceReference.Attributes);

            IFileReference file = resourceReference.ExternalFile;
            if (file != null)
            {
                this.Visit(file);
            }
        }

        public override void Visit(SecurityAttribute securityAttribute)
        {
            this.Visit(securityAttribute.Attribute);
        }

        public void VisitTypeDefinitionNoMembers(ITypeDefinition typeDefinition)
        {
            this.Visit(typeDefinition.GetAttributes(Context));

            var baseType = typeDefinition.GetBaseClass(Context);
            if (baseType != null)
            {
                this.typeReferenceNeedsToken = true;
                this.Visit(baseType);
                Debug.Assert(!this.typeReferenceNeedsToken);
            }

            this.Visit(typeDefinition.GetExplicitImplementationOverrides(Context));
            if (typeDefinition.HasDeclarativeSecurity)
            {
                this.Visit(typeDefinition.SecurityAttributes);
            }

            this.VisitTypeReferencesThatNeedTokens(typeDefinition.Interfaces(Context));

            if (typeDefinition.IsGeneric)
            {
                this.Visit(typeDefinition.GenericParameters);
            }
        }

        public override void Visit(ITypeDefinition typeDefinition)
        {
            VisitTypeDefinitionNoMembers(typeDefinition);

            this.Visit(typeDefinition.GetEvents(Context));
            this.Visit(typeDefinition.GetFields(Context));
            this.Visit(typeDefinition.GetMethods(Context));
            this.VisitNestedTypes(typeDefinition.GetNestedTypes(Context));
            this.Visit(typeDefinition.GetProperties(Context));
        }

        public void VisitTypeReferencesThatNeedTokens(IEnumerable<TypeReferenceWithAttributes> refsWithAttributes)
        {
            foreach (var refWithAttributes in refsWithAttributes)
            {
                this.Visit(refWithAttributes.Attributes);
                VisitTypeReferencesThatNeedTokens(refWithAttributes.TypeRef);
            }
        }


        private void VisitTypeReferencesThatNeedTokens(ITypeReference typeReference)
        {
            this.typeReferenceNeedsToken = true;
            this.Visit(typeReference);
            Debug.Assert(!this.typeReferenceNeedsToken);
        }

        public override void Visit(ITypeMemberReference typeMemberReference)
        {
            RecordTypeMemberReference(typeMemberReference);

            //This code was in CCI, but appears wrong to me. There is no need to visit attributes of members that are
            //being referenced, only those being defined. This code causes additional spurious typerefs and memberrefs to be
            //emitted. If the attributes can't be resolved, it causes a NullReference.
            //
            //if ((typeMemberReference.AsDefinition(Context) == null))
            //{
            //    this.Visit(typeMemberReference.GetAttributes(Context));
            //}

            this.typeReferenceNeedsToken = true;
            this.Visit(typeMemberReference.GetContainingType(Context));
            Debug.Assert(!this.typeReferenceNeedsToken);
        }

        protected abstract void RecordTypeMemberReference(ITypeMemberReference typeMemberReference);

        // Array and pointer types might cause deep recursions; visit them iteratively
        // rather than recursively.
        public override void Visit(IArrayTypeReference arrayTypeReference)
        {
            // We don't visit the current array type; it has already been visited.
            // We go straight to the element type and visit it.
            ITypeReference current = arrayTypeReference.GetElementType(Context);
            while (true)
            {
                bool mustVisitChildren = VisitTypeReference(current);
                if (!mustVisitChildren)
                {
                    return;
                }
                else if (current is IArrayTypeReference)
                {
                    // The element type is itself an array type, and we must visit *its* element type.
                    // Iterate rather than recursing.
                    current = ((IArrayTypeReference)current).GetElementType(Context);
                    continue;
                }
                else
                {
                    // The element type is not an array type and we must visit its children.
                    // Dispatch the type in order to visit its children.
                    DispatchAsReference(current);
                    return;
                }
            }
        }

        // Array and pointer types might cause deep recursions; visit them iteratively
        // rather than recursively.
        public override void Visit(IPointerTypeReference pointerTypeReference)
        {
            // We don't visit the current pointer type; it has already been visited.
            // We go straight to the target type and visit it.
            ITypeReference current = pointerTypeReference.GetTargetType(Context);
            while (true)
            {
                bool mustVisitChildren = VisitTypeReference(current);
                if (!mustVisitChildren)
                {
                    return;
                }
                else if (current is IPointerTypeReference)
                {
                    // The target type is itself an pointer type, and we must visit *its* target type.
                    // Iterate rather than recursing.
                    current = ((IPointerTypeReference)current).GetTargetType(Context);
                    continue;
                }
                else
                {
                    // The target type is not an pointer type and we must visit its children.
                    // Dispatch the type in order to visit its children.
                    DispatchAsReference(current);
                    return;
                }
            }
        }

        public override void Visit(ITypeReference typeReference)
        {
            if (VisitTypeReference(typeReference))
            {
                DispatchAsReference(typeReference);
            }
        }

        // Returns true if we need to look at the children, false otherwise.
        private bool VisitTypeReference(ITypeReference typeReference)
        {
            if (!_alreadySeen.Add(typeReference))
            {
                if (!this.typeReferenceNeedsToken)
                {
                    return false;
                }

                this.typeReferenceNeedsToken = false;
                if (!_alreadyHasToken.Add(typeReference))
                {
                    return false;
                }

                RecordTypeReference(typeReference);

                return false;
            }

            INestedTypeReference/*?*/ nestedTypeReference = typeReference.AsNestedTypeReference;
            if (this.typeReferenceNeedsToken || nestedTypeReference != null ||
              (typeReference is {
                  TypeCode: PrimitiveTypeCode.NotPrimitive, AsNamespaceTypeReference: {
                  }
              }))
            {
                ISpecializedNestedTypeReference/*?*/ specializedNestedTypeReference = nestedTypeReference?.AsSpecializedNestedTypeReference;
                if (specializedNestedTypeReference != null)
                {
                    INestedTypeReference unspecializedNestedTypeReference = specializedNestedTypeReference.GetUnspecializedVersion(Context);
                    if (_alreadyHasToken.Add(unspecializedNestedTypeReference))
                    {
                        RecordTypeReference(unspecializedNestedTypeReference);
                    }
                }

                if (this.typeReferenceNeedsToken && _alreadyHasToken.Add(typeReference))
                {
                    RecordTypeReference(typeReference);
                }

                if (nestedTypeReference != null)
                {
                    this.typeReferenceNeedsToken = (typeReference.AsSpecializedNestedTypeReference == null);
                    this.Visit(nestedTypeReference.GetContainingType(Context));
                }
            }

            //This code was in CCI, but appears wrong to me. There is no need to visit attributes of types that are
            //being referenced, only those being defined. This code causes additional spurious typerefs and memberrefs to be
            //emitted. If the attributes can't be resolved, it causes a NullReference.
            //
            //if ((typeReference.AsTypeDefinition(Context) == null))
            //{
            //    this.Visit(typeReference.GetAttributes(Context));
            //}

            this.typeReferenceNeedsToken = false;
            return true;
        }
    }
}
