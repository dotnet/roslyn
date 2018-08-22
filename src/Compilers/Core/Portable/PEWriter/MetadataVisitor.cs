// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Roslyn.Utilities;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.CodeGen;

namespace Microsoft.Cci
{
    /// <summary>
    /// A visitor base class that traverses the object model in depth first, left to right order.
    /// </summary>
    internal abstract class MetadataVisitor
    {
        public readonly EmitContext Context;

        public MetadataVisitor(EmitContext context)
        {
            this.Context = context;
        }

        public virtual void Visit(IArrayTypeReference arrayTypeReference)
        {
            this.Visit(arrayTypeReference.GetElementType(Context));
        }

        public void Visit(IEnumerable<IAssemblyReference> assemblyReferences)
        {
            foreach (IAssemblyReference assemblyReference in assemblyReferences)
            {
                this.Visit((IUnitReference)assemblyReference);
            }
        }

        public virtual void Visit(IAssemblyReference assemblyReference)
        {
        }

        public void Visit(IEnumerable<ICustomAttribute> customAttributes)
        {
            foreach (ICustomAttribute customAttribute in customAttributes)
            {
                this.Visit(customAttribute);
            }
        }

        public virtual void Visit(ICustomAttribute customAttribute)
        {
            IMethodReference constructor = customAttribute.Constructor(Context, reportDiagnostics: false);
            if (constructor is null)
            {
                return;
            }

            this.Visit(customAttribute.GetArguments(Context));
            this.Visit(constructor);
            this.Visit(customAttribute.GetNamedArguments(Context));
        }

        public void Visit(ImmutableArray<ICustomModifier> customModifiers)
        {
            foreach (ICustomModifier customModifier in customModifiers)
            {
                this.Visit(customModifier);
            }
        }

        public virtual void Visit(ICustomModifier customModifier)
        {
            this.Visit(customModifier.GetModifier(Context));
        }

        public void Visit(IEnumerable<IEventDefinition> events)
        {
            foreach (IEventDefinition eventDef in events)
            {
                this.Visit((ITypeDefinitionMember)eventDef);
            }
        }

        public virtual void Visit(IEventDefinition eventDefinition)
        {
            this.Visit(eventDefinition.GetAccessors(Context));
            this.Visit(eventDefinition.GetType(Context));
        }

        public void Visit(IEnumerable<IFieldDefinition> fields)
        {
            foreach (IFieldDefinition field in fields)
            {
                this.Visit((ITypeDefinitionMember)field);
            }
        }

        public virtual void Visit(IFieldDefinition fieldDefinition)
        {
            var constant = fieldDefinition.GetCompileTimeValue(Context);
            var marshalling = fieldDefinition.MarshallingInformation;

            Debug.Assert((constant != null) == fieldDefinition.IsCompileTimeConstant);
            Debug.Assert((marshalling != null || !fieldDefinition.MarshallingDescriptor.IsDefaultOrEmpty) == fieldDefinition.IsMarshalledExplicitly);

            if (constant != null)
            {
                this.Visit((IMetadataExpression)constant);
            }

            if (marshalling != null)
            {
                // Note, we are not visiting MarshallingDescriptor. It is used only for 
                // NoPia embedded/local types and VB Dev11 simply copies the bits without
                // cracking them.
                this.Visit(marshalling);
            }

            this.Visit(fieldDefinition.GetType(Context));
        }

        public virtual void Visit(IFieldReference fieldReference)
        {
            this.Visit((ITypeMemberReference)fieldReference);
        }

        public void Visit(IEnumerable<IFileReference> fileReferences)
        {
            foreach (IFileReference fileReference in fileReferences)
            {
                this.Visit(fileReference);
            }
        }

        public virtual void Visit(IFileReference fileReference)
        {
        }

        public virtual void Visit(IGenericMethodInstanceReference genericMethodInstanceReference)
        {
        }

        public void Visit(IEnumerable<IGenericMethodParameter> genericParameters)
        {
            foreach (IGenericMethodParameter genericParameter in genericParameters)
            {
                this.Visit((IGenericParameter)genericParameter);
            }
        }

        public virtual void Visit(IGenericMethodParameter genericMethodParameter)
        {
        }

        public virtual void Visit(IGenericMethodParameterReference genericMethodParameterReference)
        {
        }

        public virtual void Visit(IGenericParameter genericParameter)
        {
            this.Visit(genericParameter.GetAttributes(Context));
            this.Visit(genericParameter.GetConstraints(Context));

            genericParameter.Dispatch(this);
        }

        public abstract void Visit(IGenericTypeInstanceReference genericTypeInstanceReference);

        public void Visit(IEnumerable<IGenericParameter> genericParameters)
        {
            foreach (IGenericTypeParameter genericParameter in genericParameters)
            {
                this.Visit((IGenericParameter)genericParameter);
            }
        }

        public virtual void Visit(IGenericTypeParameter genericTypeParameter)
        {
        }

        public virtual void Visit(IGenericTypeParameterReference genericTypeParameterReference)
        {
        }

        public virtual void Visit(IGlobalFieldDefinition globalFieldDefinition)
        {
            this.Visit((IFieldDefinition)globalFieldDefinition);
        }

        public virtual void Visit(IGlobalMethodDefinition globalMethodDefinition)
        {
            this.Visit((IMethodDefinition)globalMethodDefinition);
        }

        public void Visit(ImmutableArray<ILocalDefinition> localDefinitions)
        {
            foreach (ILocalDefinition localDefinition in localDefinitions)
            {
                this.Visit(localDefinition);
            }
        }

        public virtual void Visit(ILocalDefinition localDefinition)
        {
            this.Visit(localDefinition.CustomModifiers);
            this.Visit(localDefinition.Type);
        }

        public virtual void Visit(IMarshallingInformation marshallingInformation)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public virtual void Visit(MetadataConstant constant)
        {
        }

        public virtual void Visit(MetadataCreateArray createArray)
        {
            this.Visit(createArray.ElementType);
            this.Visit(createArray.Elements);
        }

        public void Visit(IEnumerable<IMetadataExpression> expressions)
        {
            foreach (IMetadataExpression expression in expressions)
            {
                this.Visit(expression);
            }
        }

        public virtual void Visit(IMetadataExpression expression)
        {
            this.Visit(expression.Type);
            expression.Dispatch(this);
        }

        public void Visit(IEnumerable<IMetadataNamedArgument> namedArguments)
        {
            foreach (IMetadataNamedArgument namedArgument in namedArguments)
            {
                this.Visit((IMetadataExpression)namedArgument);
            }
        }

        public virtual void Visit(IMetadataNamedArgument namedArgument)
        {
            this.Visit(namedArgument.ArgumentValue);
        }

        public virtual void Visit(MetadataTypeOf typeOf)
        {
            if (typeOf.TypeToGet != null)
            {
                this.Visit(typeOf.TypeToGet);
            }
        }

        public virtual void Visit(IMethodBody methodBody)
        {
            foreach (var scope in methodBody.LocalScopes)
            {
                this.Visit(scope.Constants);
            }

            this.Visit(methodBody.LocalVariables);
            //this.Visit(methodBody.Operations);    //in Roslyn we don't break out each instruction as it's own operation.
            this.Visit(methodBody.ExceptionRegions);
        }

        public void Visit(IEnumerable<IMethodDefinition> methods)
        {
            foreach (IMethodDefinition method in methods)
            {
                this.Visit((ITypeDefinitionMember)method);
            }
        }

        public virtual void Visit(IMethodDefinition method)
        {
            this.Visit(method.GetReturnValueAttributes(Context));
            this.Visit(method.RefCustomModifiers);
            this.Visit(method.ReturnValueCustomModifiers);

            if (method.HasDeclarativeSecurity)
            {
                this.Visit(method.SecurityAttributes);
            }

            if (method.IsGeneric)
            {
                this.Visit(method.GenericParameters);
            }

            this.Visit(method.GetType(Context));
            this.Visit(method.Parameters);
            if (method.IsPlatformInvoke)
            {
                this.Visit(method.PlatformInvokeData);
            }
        }

        public void Visit(IEnumerable<MethodImplementation> methodImplementations)
        {
            foreach (MethodImplementation methodImplementation in methodImplementations)
            {
                this.Visit(methodImplementation);
            }
        }

        public virtual void Visit(MethodImplementation methodImplementation)
        {
            this.Visit(methodImplementation.ImplementedMethod);
            this.Visit(methodImplementation.ImplementingMethod);
        }

        public void Visit(IEnumerable<IMethodReference> methodReferences)
        {
            foreach (IMethodReference methodReference in methodReferences)
            {
                this.Visit(methodReference);
            }
        }

        public virtual void Visit(IMethodReference methodReference)
        {
            IGenericMethodInstanceReference genericMethodInstanceReference = methodReference.AsGenericMethodInstanceReference;
            if (genericMethodInstanceReference != null)
            {
                this.Visit(genericMethodInstanceReference);
            }
            else
            {
                this.Visit((ITypeMemberReference)methodReference);
            }
        }

        public virtual void Visit(IModifiedTypeReference modifiedTypeReference)
        {
            this.Visit(modifiedTypeReference.CustomModifiers);
            this.Visit(modifiedTypeReference.UnmodifiedType);
        }

        public abstract void Visit(CommonPEModuleBuilder module);

        public void Visit(IEnumerable<IModuleReference> moduleReferences)
        {
            foreach (IModuleReference moduleReference in moduleReferences)
            {
                this.Visit((IUnitReference)moduleReference);
            }
        }

        public virtual void Visit(IModuleReference moduleReference)
        {
        }

        public void Visit(IEnumerable<INamedTypeDefinition> types)
        {
            foreach (INamedTypeDefinition type in types)
            {
                this.Visit(type);
            }
        }

        public virtual void Visit(INamespaceTypeDefinition namespaceTypeDefinition)
        {
        }

        public virtual void Visit(INamespaceTypeReference namespaceTypeReference)
        {
        }

        public void VisitNestedTypes(IEnumerable<INamedTypeDefinition> nestedTypes)
        {
            foreach (ITypeDefinitionMember nestedType in nestedTypes)
            {
                this.Visit(nestedType);
            }
        }

        public virtual void Visit(INestedTypeDefinition nestedTypeDefinition)
        {
        }

        public virtual void Visit(INestedTypeReference nestedTypeReference)
        {
            this.Visit(nestedTypeReference.GetContainingType(Context));
        }

        public void Visit(ImmutableArray<ExceptionHandlerRegion> exceptionRegions)
        {
            foreach (ExceptionHandlerRegion region in exceptionRegions)
            {
                this.Visit(region);
            }
        }

        public virtual void Visit(ExceptionHandlerRegion exceptionRegion)
        {
            var exceptionType = exceptionRegion.ExceptionType;
            if (exceptionType != null)
            {
                this.Visit(exceptionType);
            }
        }

        public void Visit(ImmutableArray<IParameterDefinition> parameters)
        {
            foreach (IParameterDefinition parameter in parameters)
            {
                this.Visit(parameter);
            }
        }

        public virtual void Visit(IParameterDefinition parameterDefinition)
        {
            var marshalling = parameterDefinition.MarshallingInformation;

            Debug.Assert((marshalling != null || !parameterDefinition.MarshallingDescriptor.IsDefaultOrEmpty) == parameterDefinition.IsMarshalledExplicitly);

            this.Visit(parameterDefinition.GetAttributes(Context));
            this.Visit(parameterDefinition.RefCustomModifiers);
            this.Visit(parameterDefinition.CustomModifiers);

            MetadataConstant defaultValue = parameterDefinition.GetDefaultValue(Context);
            if (defaultValue != null)
            {
                this.Visit((IMetadataExpression)defaultValue);
            }

            if (marshalling != null)
            {
                // Note, we are not visiting MarshallingDescriptor. It is used only for 
                // NoPia embedded/local types and VB Dev11 simply copies the bits without
                // cracking them.
                this.Visit(marshalling);
            }

            this.Visit(parameterDefinition.GetType(Context));
        }

        public void Visit(ImmutableArray<IParameterTypeInformation> parameterTypeInformations)
        {
            foreach (IParameterTypeInformation parameterTypeInformation in parameterTypeInformations)
            {
                this.Visit(parameterTypeInformation);
            }
        }

        public virtual void Visit(IParameterTypeInformation parameterTypeInformation)
        {
            this.Visit(parameterTypeInformation.RefCustomModifiers);
            this.Visit(parameterTypeInformation.CustomModifiers);
            this.Visit(parameterTypeInformation.GetType(Context));
        }

        public virtual void Visit(IPlatformInvokeInformation platformInvokeInformation)
        {
        }

        public virtual void Visit(IPointerTypeReference pointerTypeReference)
        {
            this.Visit(pointerTypeReference.GetTargetType(Context));
        }

        public void Visit(IEnumerable<IPropertyDefinition> properties)
        {
            foreach (IPropertyDefinition property in properties)
            {
                this.Visit((ITypeDefinitionMember)property);
            }
        }

        public virtual void Visit(IPropertyDefinition propertyDefinition)
        {
            this.Visit(propertyDefinition.GetAccessors(Context));
            this.Visit(propertyDefinition.Parameters);
        }

        public void Visit(IEnumerable<ManagedResource> resources)
        {
            foreach (var resource in resources)
            {
                this.Visit(resource);
            }
        }

        public virtual void Visit(ManagedResource resource)
        {
        }

        public virtual void Visit(SecurityAttribute securityAttribute)
        {
            this.Visit(securityAttribute.Attribute);
        }

        public void Visit(IEnumerable<SecurityAttribute> securityAttributes)
        {
            foreach (SecurityAttribute securityAttribute in securityAttributes)
            {
                this.Visit(securityAttribute);
            }
        }

        public void Visit(IEnumerable<ITypeDefinitionMember> typeMembers)
        {
            foreach (ITypeDefinitionMember typeMember in typeMembers)
            {
                this.Visit(typeMember);
            }
        }

        public void Visit(IEnumerable<ITypeDefinition> types)
        {
            foreach (ITypeDefinition type in types)
            {
                this.Visit(type);
            }
        }

        public abstract void Visit(ITypeDefinition typeDefinition);

        public virtual void Visit(ITypeDefinitionMember typeMember)
        {
            ITypeDefinition nestedType = typeMember as INestedTypeDefinition;
            if (nestedType != null)
            {
                this.Visit(nestedType);
            }
            else
            {
                this.Visit(typeMember.GetAttributes(Context));

                typeMember.Dispatch(this);
            }
        }

        public virtual void Visit(ITypeMemberReference typeMemberReference)
        {
            if (typeMemberReference.AsDefinition(Context) == null)
            {
                this.Visit(typeMemberReference.GetAttributes(Context)); // In principle, references can have attributes that are distinct from the definitions they refer to.
            }
        }

        public void Visit(IEnumerable<ITypeReference> typeReferences)
        {
            foreach (ITypeReference typeReference in typeReferences)
            {
                this.Visit(typeReference);
            }
        }

        public void Visit(IEnumerable<TypeReferenceWithAttributes> typeRefsWithAttributes)
        {
            foreach (var typeRefWithAttributes in typeRefsWithAttributes)
            {
                this.Visit(typeRefWithAttributes.TypeRef);
                this.Visit(typeRefWithAttributes.Attributes);
            }
        }

        public virtual void Visit(ITypeReference typeReference)
        {
            this.DispatchAsReference(typeReference);
        }

        /// <summary>
        /// Use this routine, rather than ITypeReference.Dispatch, to call the appropriate derived overload of an ITypeReference.
        /// The former routine will call Visit(INamespaceTypeDefinition) rather than Visit(INamespaceTypeReference), etc., 
        /// in the case where a definition is used as a reference to itself.
        /// </summary>
        /// <param name="typeReference">A reference to a type definition. Note that a type definition can serve as a reference to itself.</param>
        protected void DispatchAsReference(ITypeReference typeReference)
        {
            INamespaceTypeReference namespaceTypeReference = typeReference.AsNamespaceTypeReference;
            if (namespaceTypeReference != null)
            {
                this.Visit(namespaceTypeReference);
                return;
            }

            IGenericTypeInstanceReference genericTypeInstanceReference = typeReference.AsGenericTypeInstanceReference;
            if (genericTypeInstanceReference != null)
            {
                this.Visit(genericTypeInstanceReference);
                return;
            }

            INestedTypeReference nestedTypeReference = typeReference.AsNestedTypeReference;
            if (nestedTypeReference != null)
            {
                this.Visit(nestedTypeReference);
                return;
            }

            IArrayTypeReference arrayTypeReference = typeReference as IArrayTypeReference;
            if (arrayTypeReference != null)
            {
                this.Visit(arrayTypeReference);
                return;
            }

            IGenericTypeParameterReference genericTypeParameterReference = typeReference.AsGenericTypeParameterReference;
            if (genericTypeParameterReference != null)
            {
                this.Visit(genericTypeParameterReference);
                return;
            }

            IGenericMethodParameterReference genericMethodParameterReference = typeReference.AsGenericMethodParameterReference;
            if (genericMethodParameterReference != null)
            {
                this.Visit(genericMethodParameterReference);
                return;
            }

            IPointerTypeReference pointerTypeReference = typeReference as IPointerTypeReference;
            if (pointerTypeReference != null)
            {
                this.Visit(pointerTypeReference);
                return;
            }

            IModifiedTypeReference modifiedTypeReference = typeReference as IModifiedTypeReference;
            if (modifiedTypeReference != null)
            {
                this.Visit(modifiedTypeReference);
                return;
            }
        }

        public void Visit(IEnumerable<IUnitReference> unitReferences)
        {
            foreach (IUnitReference unitReference in unitReferences)
            {
                this.Visit(unitReference);
            }
        }

        public virtual void Visit(IUnitReference unitReference)
        {
            this.DispatchAsReference(unitReference);
        }

        /// <summary>
        /// Use this routine, rather than IUnitReference.Dispatch, to call the appropriate derived overload of an IUnitReference.
        /// The former routine will call Visit(IAssembly) rather than Visit(IAssemblyReference), etc.
        /// in the case where a definition is used as the reference to itself.
        /// </summary>
        /// <param name="unitReference">A reference to a unit. Note that a unit can serve as a reference to itself.</param>
        private void DispatchAsReference(IUnitReference unitReference)
        {
            IAssemblyReference assemblyReference = unitReference as IAssemblyReference;
            if (assemblyReference != null)
            {
                this.Visit(assemblyReference);
                return;
            }

            IModuleReference moduleReference = unitReference as IModuleReference;
            if (moduleReference != null)
            {
                this.Visit(moduleReference);
                return;
            }
        }

        public virtual void Visit(IWin32Resource win32Resource)
        {
        }
    }
}

