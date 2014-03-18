// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Roslyn.Utilities;
using Cci = Microsoft.Cci;

namespace Microsoft.Cci
{
    /// <summary>
    /// A visitor base class that traverses the object model in depth first, left to right order.
    /// </summary>
    internal abstract class MetadataVisitor
    {
        public readonly Microsoft.CodeAnalysis.Emit.Context Context;

        public MetadataVisitor(Microsoft.CodeAnalysis.Emit.Context context)
        {
            this.Context = context;
        }

        /// <summary>
        /// Visits the specified aliases for types.
        /// </summary>
        /// <param name="aliasesForTypes">The aliases for types.</param>
        public void Visit(IEnumerable<IAliasForType> aliasesForTypes)
        {
            foreach (IAliasForType aliasForType in aliasesForTypes)
            {
                this.Visit(aliasForType);
            }
        }

        /// <summary>
        /// Visits the specified alias for type.
        /// </summary>
        /// <param name="aliasForType">Type of the alias for.</param>
        public virtual void Visit(IAliasForType aliasForType)
        {
            this.Visit(aliasForType.AliasedType);
            this.Visit(aliasForType.GetAttributes(Context));
            aliasForType.Dispatch(this);
        }

        /// <summary>
        /// Performs some computation with the given array type reference.
        /// </summary>
        /// <param name="arrayTypeReference"></param>
        public virtual void Visit(IArrayTypeReference arrayTypeReference)
        {
            this.Visit(arrayTypeReference.GetElementType(Context));
        }

        /// <summary>
        /// Performs some computation with the given assembly.
        /// </summary>
        /// <param name="assembly"></param>
        public abstract void Visit(IAssembly assembly);

        /// <summary>
        /// Visits the specified assembly references.
        /// </summary>
        /// <param name="assemblyReferences">The assembly references.</param>
        public void Visit(IEnumerable<IAssemblyReference> assemblyReferences)
        {
            foreach (IAssemblyReference assemblyReference in assemblyReferences)
            {
                this.Visit((IUnitReference)assemblyReference);
            }
        }

        /// <summary>
        /// Performs some computation with the given assembly reference.
        /// </summary>
        /// <param name="assemblyReference"></param>
        public virtual void Visit(IAssemblyReference assemblyReference)
        {
        }

        /// <summary>
        /// Visits the specified custom attributes.
        /// </summary>
        /// <param name="customAttributes">The custom attributes.</param>
        public void Visit(IEnumerable<ICustomAttribute> customAttributes)
        {
            foreach (ICustomAttribute customAttribute in customAttributes)
            {
                this.Visit(customAttribute);
            }
        }

        /// <summary>
        /// Performs some computation with the given custom attribute.
        /// </summary>
        /// <param name="customAttribute"></param>
        public virtual void Visit(ICustomAttribute customAttribute)
        {
            this.Visit(customAttribute.GetArguments(Context));
            this.Visit(customAttribute.Constructor(Context));
            this.Visit(customAttribute.GetNamedArguments(Context));
        }

        /// <summary>
        /// Visits the specified custom modifiers.
        /// </summary>
        /// <param name="customModifiers">The custom modifiers.</param>
        public void Visit(IEnumerable<ICustomModifier> customModifiers)
        {
            foreach (ICustomModifier customModifier in customModifiers)
            {
                this.Visit(customModifier);
            }
        }

        /// <summary>
        /// Performs some computation with the given custom modifier.
        /// </summary>
        /// <param name="customModifier"></param>
        public virtual void Visit(ICustomModifier customModifier)
        {
            this.Visit(customModifier.GetModifier(Context));
        }

        /// <summary>
        /// Visits the specified events.
        /// </summary>
        /// <param name="events">The events.</param>
        public void Visit(IEnumerable<IEventDefinition> events)
        {
            foreach (IEventDefinition eventDef in events)
            {
                this.Visit((ITypeDefinitionMember)eventDef);
            }
        }

        /// <summary>
        /// Performs some computation with the given event definition.
        /// </summary>
        /// <param name="eventDefinition"></param>
        public virtual void Visit(IEventDefinition eventDefinition)
        {
            this.Visit(eventDefinition.Accessors);
            this.Visit(eventDefinition.GetType(Context));
        }

        /// <summary>
        /// Visits the specified fields.
        /// </summary>
        /// <param name="fields">The fields.</param>
        public void Visit(IEnumerable<IFieldDefinition> fields)
        {
            foreach (IFieldDefinition field in fields)
            {
                this.Visit((ITypeDefinitionMember)field);
            }
        }

        /// <summary>
        /// Performs some computation with the given field definition.
        /// </summary>
        /// <param name="fieldDefinition"></param>
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

        /// <summary>
        /// Performs some computation with the given field reference.
        /// </summary>
        /// <param name="fieldReference"></param>
        public virtual void Visit(IFieldReference fieldReference)
        {
            this.Visit((ITypeMemberReference)fieldReference);
        }

        /// <summary>
        /// Visits the specified file references.
        /// </summary>
        /// <param name="fileReferences">The file references.</param>
        public void Visit(IEnumerable<IFileReference> fileReferences)
        {
            foreach (IFileReference fileReference in fileReferences)
            {
                this.Visit(fileReference);
            }
        }

        /// <summary>
        /// Performs some computation with the given file reference.
        /// </summary>
        /// <param name="fileReference"></param>
        public virtual void Visit(IFileReference fileReference)
        {
        }

        /// <summary>
        /// Performs some computation with the given function pointer type reference.
        /// </summary>
        /// <param name="functionPointerTypeReference"></param>
        public virtual void Visit(IFunctionPointerTypeReference functionPointerTypeReference)
        {
            this.Visit(functionPointerTypeReference.GetType(Context));
            this.Visit(functionPointerTypeReference.GetParameters(Context));
            this.Visit(functionPointerTypeReference.ExtraArgumentTypes);
            if (functionPointerTypeReference.ReturnValueIsModified)
            {
                this.Visit(functionPointerTypeReference.ReturnValueCustomModifiers);
            }
        }

        /// <summary>
        /// Performs some computation with the given generic method instance reference.
        /// </summary>
        /// <param name="genericMethodInstanceReference"></param>
        public virtual void Visit(IGenericMethodInstanceReference genericMethodInstanceReference)
        {
        }

        /// <summary>
        /// Visits the specified generic parameters.
        /// </summary>
        /// <param name="genericParameters">The generic parameters.</param>
        public void Visit(IEnumerable<IGenericMethodParameter> genericParameters)
        {
            foreach (IGenericMethodParameter genericParameter in genericParameters)
            {
                this.Visit((IGenericParameter)genericParameter);
            }
        }

        /// <summary>
        /// Performs some computation with the given generic method parameter.
        /// </summary>
        /// <param name="genericMethodParameter"></param>
        public virtual void Visit(IGenericMethodParameter genericMethodParameter)
        {
        }

        /// <summary>
        /// Performs some computation with the given generic method parameter reference.
        /// </summary>
        /// <param name="genericMethodParameterReference"></param>
        public virtual void Visit(IGenericMethodParameterReference genericMethodParameterReference)
        {
        }

        /// <summary>
        /// Visits the specified generic parameter.
        /// </summary>
        /// <param name="genericParameter">The generic parameter.</param>
        public virtual void Visit(IGenericParameter genericParameter)
        {
            this.Visit(genericParameter.GetAttributes(Context));
            this.Visit(genericParameter.GetConstraints(Context));

            genericParameter.Dispatch(this);
        }

        /// <summary>
        /// Performs some computation with the given generic type instance reference.
        /// </summary>
        /// <param name="genericTypeInstanceReference"></param>
        public abstract void Visit(IGenericTypeInstanceReference genericTypeInstanceReference);

        /// <summary>
        /// Visits the specified generic parameters.
        /// </summary>
        /// <param name="genericParameters">The generic parameters.</param>
        public void Visit(IEnumerable<IGenericParameter> genericParameters)
        {
            foreach (IGenericTypeParameter genericParameter in genericParameters)
            {
                this.Visit((IGenericParameter)genericParameter);
            }
        }

        /// <summary>
        /// Performs some computation with the given generic parameter.
        /// </summary>
        /// <param name="genericTypeParameter"></param>
        public virtual void Visit(IGenericTypeParameter genericTypeParameter)
        {
        }

        /// <summary>
        /// Performs some computation with the given generic type parameter reference.
        /// </summary>
        /// <param name="genericTypeParameterReference"></param>
        public virtual void Visit(IGenericTypeParameterReference genericTypeParameterReference)
        {
        }

        /// <summary>
        /// Performs some computation with the given global field definition.
        /// </summary>
        /// <param name="globalFieldDefinition"></param>
        public virtual void Visit(IGlobalFieldDefinition globalFieldDefinition)
        {
            this.Visit((IFieldDefinition)globalFieldDefinition);
        }

        /// <summary>
        /// Performs some computation with the given global method definition.
        /// </summary>
        /// <param name="globalMethodDefinition"></param>
        public virtual void Visit(IGlobalMethodDefinition globalMethodDefinition)
        {
            this.Visit((IMethodDefinition)globalMethodDefinition);
        }

        /// <summary>
        /// Visits the specified local definitions.
        /// </summary>
        /// <param name="localDefinitions">The local definitions.</param>
        public void Visit(IEnumerable<ILocalDefinition> localDefinitions)
        {
            foreach (ILocalDefinition localDefinition in localDefinitions)
            {
                this.Visit(localDefinition);
            }
        }

        /// <summary>
        /// Visits the specified local definition.
        /// </summary>
        /// <param name="localDefinition">The local definition.</param>
        public virtual void Visit(ILocalDefinition localDefinition)
        {
            if (localDefinition.IsModified)
            {
                this.Visit(localDefinition.CustomModifiers);
            }

            this.Visit(localDefinition.Type);
        }

        /// <summary>
        /// Performs some computation with the given managed pointer type reference.
        /// </summary>
        /// <param name="managedPointerTypeReference"></param>
        public virtual void Visit(IManagedPointerTypeReference managedPointerTypeReference)
        {
        }

        /// <summary>
        /// Performs some computation with the given marshalling information.
        /// </summary>
        public virtual void Visit(IMarshallingInformation marshallingInformation)
        {
            throw ExceptionUtilities.Unreachable;
        }

        /// <summary>
        /// Performs some computation with the given metadata constant.
        /// </summary>
        /// <param name="constant"></param>
        public virtual void Visit(IMetadataConstant constant)
        {
        }

        /// <summary>
        /// Performs some computation with the given metadata array creation expression.
        /// </summary>
        /// <param name="createArray"></param>
        public virtual void Visit(IMetadataCreateArray createArray)
        {
            this.Visit(createArray.ElementType);
            this.Visit(createArray.Elements);
        }

        /// <summary>
        /// Visits the specified expressions.
        /// </summary>
        /// <param name="expressions">The expressions.</param>
        public void Visit(IEnumerable<IMetadataExpression> expressions)
        {
            foreach (IMetadataExpression expression in expressions)
            {
                this.Visit(expression);
            }
        }

        /// <summary>
        /// Performs some computation with the given metadata expression.
        /// </summary>
        /// <param name="expression"></param>
        public virtual void Visit(IMetadataExpression expression)
        {
            this.Visit(expression.Type);
            expression.Dispatch(this);
        }

        /// <summary>
        /// Visits the specified named arguments.
        /// </summary>
        /// <param name="namedArguments">The named arguments.</param>
        public void Visit(IEnumerable<IMetadataNamedArgument> namedArguments)
        {
            foreach (IMetadataNamedArgument namedArgument in namedArguments)
            {
                this.Visit((IMetadataExpression)namedArgument);
            }
        }

        /// <summary>
        /// Performs some computation with the given metadata named argument expression.
        /// </summary>
        /// <param name="namedArgument"></param>
        public virtual void Visit(IMetadataNamedArgument namedArgument)
        {
            this.Visit(namedArgument.ArgumentValue);
        }

        /// <summary>
        /// Performs some computation with the given metadata typeof expression.
        /// </summary>
        /// <param name="typeOf"></param>
        public virtual void Visit(IMetadataTypeOf typeOf)
        {
            if (typeOf.TypeToGet != null)
            {
                this.Visit(typeOf.TypeToGet);
            }
        }

        /// <summary>
        /// Performs some computation with the given method body.
        /// </summary>
        /// <param name="methodBody"></param>
        public virtual void Visit(IMethodBody methodBody)
        {
            this.Visit(methodBody.LocalVariables);
            //this.Visit(methodBody.Operations);    //in Roslyn we don't break out each instruction as it's own operation.
            this.Visit(methodBody.ExceptionRegions);
        }

        /// <summary>
        /// Visits the specified methods.
        /// </summary>
        /// <param name="methods">The methods.</param>
        public void Visit(IEnumerable<IMethodDefinition> methods)
        {
            foreach (IMethodDefinition method in methods)
            {
                this.Visit((ITypeDefinitionMember)method);
            }
        }

        /// <summary>
        /// Performs some computation with the given method definition.
        /// </summary>
        /// <param name="method"></param>
        public virtual void Visit(IMethodDefinition method)
        {
            this.Visit(method.ReturnValueAttributes);
            if (method.ReturnValueIsModified)
            {
                this.Visit(method.ReturnValueCustomModifiers);
            }

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

        /// <summary>
        /// Visits the specified method implementations.
        /// </summary>
        /// <param name="methodImplementations">The method implementations.</param>
        public void Visit(IEnumerable<IMethodImplementation> methodImplementations)
        {
            foreach (IMethodImplementation methodImplementation in methodImplementations)
            {
                this.Visit(methodImplementation);
            }
        }

        /// <summary>
        /// Performs some computation with the given method implementation.
        /// </summary>
        /// <param name="methodImplementation"></param>
        public virtual void Visit(IMethodImplementation methodImplementation)
        {
            this.Visit(methodImplementation.ImplementedMethod);
            this.Visit(methodImplementation.ImplementingMethod);
        }

        /// <summary>
        /// Visits the specified method references.
        /// </summary>
        /// <param name="methodReferences">The method references.</param>
        public void Visit(IEnumerable<IMethodReference> methodReferences)
        {
            foreach (IMethodReference methodReference in methodReferences)
            {
                this.Visit(methodReference);
            }
        }

        /// <summary>
        /// Performs some computation with the given method reference.
        /// </summary>
        /// <param name="methodReference"></param>
        public virtual void Visit(IMethodReference methodReference)
        {
            IGenericMethodInstanceReference/*?*/ genericMethodInstanceReference = methodReference.AsGenericMethodInstanceReference;
            if (genericMethodInstanceReference != null)
            {
                this.Visit(genericMethodInstanceReference);
            }
            else
            {
                this.Visit((ITypeMemberReference)methodReference);
            }
        }

        /// <summary>
        /// Performs some computation with the given modified type reference.
        /// </summary>
        /// <param name="modifiedTypeReference"></param>
        public virtual void Visit(IModifiedTypeReference modifiedTypeReference)
        {
            this.Visit(modifiedTypeReference.CustomModifiers);
            this.Visit(modifiedTypeReference.UnmodifiedType);
        }

        /// <summary>
        /// Performs some computation with the given module.
        /// </summary>
        /// <param name="module"></param>
        public abstract void Visit(IModule module);

        /// <summary>
        /// Visits the specified module references.
        /// </summary>
        /// <param name="moduleReferences">The module references.</param>
        public void Visit(IEnumerable<IModuleReference> moduleReferences)
        {
            foreach (IModuleReference moduleReference in moduleReferences)
            {
                this.Visit((IUnitReference)moduleReference);
            }
        }

        /// <summary>
        /// Performs some computation with the given module reference.
        /// </summary>
        /// <param name="moduleReference"></param>
        public virtual void Visit(IModuleReference moduleReference)
        {
        }

        /// <summary>
        /// Visits the specified types.
        /// </summary>
        /// <param name="types">The types.</param>
        public void Visit(IEnumerable<INamedTypeDefinition> types)
        {
            foreach (INamedTypeDefinition type in types)
            {
                this.Visit(type);
            }
        }

        /// <summary>
        /// Performs some computation with the given namespace type definition.
        /// </summary>
        /// <param name="namespaceTypeDefinition"></param>
        public virtual void Visit(INamespaceTypeDefinition namespaceTypeDefinition)
        {
        }

        /// <summary>
        /// Performs some computation with the given namespace type reference.
        /// </summary>
        /// <param name="namespaceTypeReference"></param>
        public virtual void Visit(INamespaceTypeReference namespaceTypeReference)
        {
        }

        /// <summary>
        /// Visits the specified nested types.
        /// </summary>
        /// <param name="nestedTypes">The nested types.</param>
        public virtual void VisitNestedTypes(IEnumerable<INamedTypeDefinition> nestedTypes)
        {
            foreach (ITypeDefinitionMember nestedType in nestedTypes)
            {
                this.Visit((ITypeDefinitionMember)nestedType);
            }
        }

        /// <summary>
        /// Performs some computation with the given nested type definition.
        /// </summary>
        /// <param name="nestedTypeDefinition"></param>
        public virtual void Visit(INestedTypeDefinition nestedTypeDefinition)
        {
        }

        /// <summary>
        /// Performs some computation with the given nested type reference.
        /// </summary>
        /// <param name="nestedTypeReference"></param>
        public virtual void Visit(INestedTypeReference nestedTypeReference)
        {
            this.Visit(nestedTypeReference.GetContainingType(Context));
        }

        /// <summary>
        /// Visits the specified operation exception informations.
        /// </summary>
        public void Visit(IEnumerable<ExceptionHandlerRegion> exceptionRegions)
        {
            foreach (ExceptionHandlerRegion region in exceptionRegions)
            {
                this.Visit(region);
            }
        }

        /// <summary>
        /// Visits the specified operation exception information.
        /// </summary>
        public virtual void Visit(ExceptionHandlerRegion exceptionRegion)
        {
            var exceptionType = exceptionRegion.ExceptionType;
            if (exceptionType != null)
            {
                this.Visit(exceptionType);
            }
        }

        /// <summary>
        /// Visits the specified parameters.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        public void Visit(IEnumerable<IParameterDefinition> parameters)
        {
            foreach (IParameterDefinition parameter in parameters)
            {
                this.Visit(parameter);
            }
        }

        /// <summary>
        /// Performs some computation with the given parameter definition.
        /// </summary>
        /// <param name="parameterDefinition"></param>
        public virtual void Visit(IParameterDefinition parameterDefinition)
        {
            var marshalling = parameterDefinition.MarshallingInformation;

            Debug.Assert((marshalling != null || !parameterDefinition.MarshallingDescriptor.IsDefaultOrEmpty) == parameterDefinition.IsMarshalledExplicitly);

            this.Visit(parameterDefinition.GetAttributes(Context));
            if (parameterDefinition.IsModified)
            {
                this.Visit(parameterDefinition.CustomModifiers);
            }

            IMetadataConstant defaultValue = parameterDefinition.GetDefaultValue(Context);
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

        /// <summary>
        /// Visits the specified parameter type informations.
        /// </summary>
        /// <param name="parameterTypeInformations">The parameter type informations.</param>
        public void Visit(IEnumerable<IParameterTypeInformation> parameterTypeInformations)
        {
            foreach (IParameterTypeInformation parameterTypeInformation in parameterTypeInformations)
            {
                this.Visit(parameterTypeInformation);
            }
        }

        /// <summary>
        /// Performs some computation with the given parameter type information.
        /// </summary>
        /// <param name="parameterTypeInformation"></param>
        public virtual void Visit(IParameterTypeInformation parameterTypeInformation)
        {
            if (parameterTypeInformation.IsModified)
            {
                this.Visit(parameterTypeInformation.CustomModifiers);
            }

            this.Visit(parameterTypeInformation.GetType(Context));
        }

        /// <summary>
        /// Visits the specified platform invoke information.
        /// </summary>
        /// <param name="platformInvokeInformation">The platform invoke information.</param>
        public virtual void Visit(IPlatformInvokeInformation platformInvokeInformation)
        {

        }

        /// <summary>
        /// Performs some computation with the given pointer type reference.
        /// </summary>
        /// <param name="pointerTypeReference"></param>
        public virtual void Visit(IPointerTypeReference pointerTypeReference)
        {
            this.Visit(pointerTypeReference.GetTargetType(Context));
        }

        /// <summary>
        /// Visits the specified properties.
        /// </summary>
        /// <param name="properties">The properties.</param>
        public void Visit(IEnumerable<IPropertyDefinition> properties)
        {
            foreach (IPropertyDefinition property in properties)
            {
                this.Visit((ITypeDefinitionMember)property);
            }
        }

        /// <summary>
        /// Performs some computation with the given property definition.
        /// </summary>
        /// <param name="propertyDefinition"></param>
        public virtual void Visit(IPropertyDefinition propertyDefinition)
        {
            this.Visit(propertyDefinition.Accessors);
            this.Visit(propertyDefinition.Parameters);
        }

        /// <summary>
        /// Visits the specified resource references.
        /// </summary>
        /// <param name="resourceReferences">The resource references.</param>
        public void Visit(IEnumerable<IResourceReference> resourceReferences)
        {
            foreach (IResourceReference resourceReference in resourceReferences)
            {
                this.Visit(resourceReference);
            }
        }

        /// <summary>
        /// Performs some computation with the given reference to a manifest resource.
        /// </summary>
        /// <param name="resourceReference"></param>
        public virtual void Visit(IResourceReference resourceReference)
        {
        }

        /// <summary>
        /// Performs some computation with the given security attribute.
        /// </summary>
        /// <param name="securityAttribute"></param>
        public virtual void Visit(SecurityAttribute securityAttribute)
        {
            this.Visit(securityAttribute.Attribute);
        }

        /// <summary>
        /// Visits the specified security attributes.
        /// </summary>
        /// <param name="securityAttributes">The security attributes.</param>
        public void Visit(IEnumerable<SecurityAttribute> securityAttributes)
        {
            foreach (SecurityAttribute securityAttribute in securityAttributes)
            {
                this.Visit(securityAttribute);
            }
        }

        /// <summary>
        /// Visits the specified type members.
        /// </summary>
        /// <param name="typeMembers">The type members.</param>
        public void Visit(IEnumerable<ITypeDefinitionMember> typeMembers)
        {
            foreach (ITypeDefinitionMember typeMember in typeMembers)
            {
                this.Visit(typeMember);
            }
        }

        /// <summary>
        /// Visits the specified types.
        /// </summary>
        /// <param name="types">The types.</param>
        public void Visit(IEnumerable<ITypeDefinition> types)
        {
            foreach (ITypeDefinition type in types)
            {
                this.Visit(type);
            }
        }

        /// <summary>
        /// Visits the specified type definition.
        /// </summary>
        /// <param name="typeDefinition">The type definition.</param>
        public abstract void Visit(ITypeDefinition typeDefinition);

        /// <summary>
        /// Visits the specified type member.
        /// </summary>
        /// <param name="typeMember">The type member.</param>
        public virtual void Visit(ITypeDefinitionMember typeMember)
        {
            ITypeDefinition/*?*/ nestedType = typeMember as INestedTypeDefinition;
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

        /// <summary>
        /// Visits the specified type member reference.
        /// </summary>
        /// <param name="typeMemberReference">The type member reference.</param>
        public virtual void Visit(ITypeMemberReference typeMemberReference)
        {
            if (typeMemberReference.AsDefinition(Context) == null)
            {
                this.Visit(typeMemberReference.GetAttributes(Context)); // In principle, refererences can have attributes that are distinct from the definitions they refer to.
            }
        }

        /// <summary>
        /// Visits the specified type references.
        /// </summary>
        /// <param name="typeReferences">The type references.</param>
        public void Visit(IEnumerable<ITypeReference> typeReferences)
        {
            foreach (ITypeReference typeReference in typeReferences)
            {
                this.Visit(typeReference);
            }
        }

        /// <summary>
        /// Visits the specified type reference.
        /// </summary>
        /// <param name="typeReference">The type reference.</param>
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
            INamespaceTypeReference/*?*/ namespaceTypeReference = typeReference.AsNamespaceTypeReference;
            if (namespaceTypeReference != null)
            {
                this.Visit(namespaceTypeReference);
                return;
            }

            IGenericTypeInstanceReference/*?*/ genericTypeInstanceReference = typeReference.AsGenericTypeInstanceReference;
            if (genericTypeInstanceReference != null)
            {
                this.Visit(genericTypeInstanceReference);
                return;
            }

            INestedTypeReference/*?*/ nestedTypeReference = typeReference.AsNestedTypeReference;
            if (nestedTypeReference != null)
            {
                this.Visit(nestedTypeReference);
                return;
            }

            IArrayTypeReference/*?*/ arrayTypeReference = typeReference as IArrayTypeReference;
            if (arrayTypeReference != null)
            {
                this.Visit(arrayTypeReference);
                return;
            }

            IGenericTypeParameterReference/*?*/ genericTypeParameterReference = typeReference.AsGenericTypeParameterReference;
            if (genericTypeParameterReference != null)
            {
                this.Visit(genericTypeParameterReference);
                return;
            }

            IGenericMethodParameterReference/*?*/ genericMethodParameterReference = typeReference.AsGenericMethodParameterReference;
            if (genericMethodParameterReference != null)
            {
                this.Visit(genericMethodParameterReference);
                return;
            }

            IPointerTypeReference/*?*/ pointerTypeReference = typeReference as IPointerTypeReference;
            if (pointerTypeReference != null)
            {
                this.Visit(pointerTypeReference);
                return;
            }

            IFunctionPointerTypeReference/*?*/ functionPointerTypeReference = typeReference as IFunctionPointerTypeReference;
            if (functionPointerTypeReference != null)
            {
                this.Visit(functionPointerTypeReference);
                return;
            }

            IModifiedTypeReference/*?*/ modifiedTypeReference = typeReference as IModifiedTypeReference;
            if (modifiedTypeReference != null)
            {
                this.Visit(modifiedTypeReference);
                return;
            }
        }

        /// <summary>
        /// Visits the specified unit references.
        /// </summary>
        /// <param name="unitReferences">The unit references.</param>
        public void Visit(IEnumerable<IUnitReference> unitReferences)
        {
            foreach (IUnitReference unitReference in unitReferences)
            {
                this.Visit(unitReference);
            }
        }

        /// <summary>
        /// Visits the specified unit reference.
        /// </summary>
        /// <param name="unitReference">The unit reference.</param>
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
            IAssemblyReference/*?*/ assemblyReference = unitReference as IAssemblyReference;
            if (assemblyReference != null)
            {
                this.Visit(assemblyReference);
                return;
            }

            IModuleReference/*?*/ moduleReference = unitReference as IModuleReference;
            if (moduleReference != null)
            {
                this.Visit(moduleReference);
                return;
            }
        }

        /// <summary>
        /// Performs some computation with the given Win32 resource.
        /// </summary>
        /// <param name="win32Resource"></param>
        public virtual void Visit(IWin32Resource win32Resource)
        {
        }
    }
}

