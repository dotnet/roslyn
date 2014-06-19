using System;
using System.Collections.Generic;
using Microsoft.Cci;

namespace Microsoft.Cci
{
    /// <summary>
    /// A visitor base class that provides a dummy body for each method of IVisit.
    /// </summary>
    internal class BaseMetadataVisitor : IMetadataVisitor
    {
        /// <summary>
        /// 
        /// </summary>
        public BaseMetadataVisitor()
        {
        }

        #region IMetadataVisitor Members

        /// <summary>
        /// Visits the specified alias for type.
        /// </summary>
        /// <param name="aliasForType">Type of the alias for.</param>
        public virtual void Visit(IAliasForType aliasForType)
        {
            // IAliasForType is a base interface that should never be implemented directly.
            // Get aliasForType to call the most type specific visitor.
            aliasForType.Dispatch(this);
        }

        /// <summary>
        /// Performs some computation with the given array type reference.
        /// </summary>
        /// <param name="arrayTypeReference"></param>
        public virtual void Visit(IArrayTypeReference arrayTypeReference)
        {
        }

        /// <summary>
        /// Performs some computation with the given assembly.
        /// </summary>
        /// <param name="assembly"></param>
        public virtual void Visit(IAssembly assembly)
        {
        }

        /// <summary>
        /// Performs some computation with the given assembly reference.
        /// </summary>
        /// <param name="assemblyReference"></param>
        public virtual void Visit(IAssemblyReference assemblyReference)
        {
        }

        /// <summary>
        /// Performs some computation with the given custom attribute.
        /// </summary>
        /// <param name="customAttribute"></param>
        public virtual void Visit(ICustomAttribute customAttribute)
        {
        }

        /// <summary>
        /// Performs some computation with the given custom modifier.
        /// </summary>
        /// <param name="customModifier"></param>
        public virtual void Visit(ICustomModifier customModifier)
        {
        }

        /// <summary>
        /// Performs some computation with the given event definition.
        /// </summary>
        /// <param name="eventDefinition"></param>
        public virtual void Visit(IEventDefinition eventDefinition)
        {
        }

        /// <summary>
        /// Performs some computation with the given field definition.
        /// </summary>
        /// <param name="fieldDefinition"></param>
        public virtual void Visit(IFieldDefinition fieldDefinition)
        {
        }

        /// <summary>
        /// Performs some computation with the given field reference.
        /// </summary>
        /// <param name="fieldReference"></param>
        public virtual void Visit(IFieldReference fieldReference)
        {
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
        }

        /// <summary>
        /// Performs some computation with the given generic method instance reference.
        /// </summary>
        /// <param name="genericMethodInstanceReference"></param>
        public virtual void Visit(IGenericMethodInstanceReference genericMethodInstanceReference)
        {
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
        /// Performs some computation with the given generic type instance reference.
        /// </summary>
        /// <param name="genericTypeInstanceReference"></param>
        public virtual void Visit(IGenericTypeInstanceReference genericTypeInstanceReference)
        {
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
        }

        /// <summary>
        /// Performs some computation with the given global method definition.
        /// </summary>
        /// <param name="globalMethodDefinition"></param>
        public virtual void Visit(IGlobalMethodDefinition globalMethodDefinition)
        {
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
        /// <param name="marshallingInformation"></param>
        public virtual void Visit(IMarshallingInformation marshallingInformation)
        {
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
        }

        /// <summary>
        /// Performs some computation with the given metadata expression.
        /// </summary>
        /// <param name="expression"></param>
        public virtual void Visit(IMetadataExpression expression)
        {
            // IMetadataExpression is a base interface that should never be implemented directly.
            // Get expression to call the most type specific visitor.
            expression.Dispatch(this);
        }

        /// <summary>
        /// Performs some computation with the given metadata named argument expression.
        /// </summary>
        /// <param name="namedArgument"></param>
        public virtual void Visit(IMetadataNamedArgument namedArgument)
        {
        }

        /// <summary>
        /// Performs some computation with the given metadata typeof expression.
        /// </summary>
        /// <param name="typeOf"></param>
        public virtual void Visit(IMetadataTypeOf typeOf)
        {
        }

        /// <summary>
        /// Performs some computation with the given method body.
        /// </summary>
        /// <param name="methodBody"></param>
        public virtual void Visit(IMethodBody methodBody)
        {
        }

        /// <summary>
        /// Performs some computation with the given method definition.
        /// </summary>
        /// <param name="method"></param>
        public virtual void Visit(IMethodDefinition method)
        {
        }

        /// <summary>
        /// Performs some computation with the given method implementation.
        /// </summary>
        /// <param name="methodImplementation"></param>
        public virtual void Visit(IMethodImplementation methodImplementation)
        {
        }

        /// <summary>
        /// Performs some computation with the given method reference.
        /// </summary>
        /// <param name="methodReference"></param>
        public virtual void Visit(IMethodReference methodReference)
        {
        }

        /// <summary>
        /// Performs some computation with the given modified type reference.
        /// </summary>
        /// <param name="modifiedTypeReference"></param>
        public virtual void Visit(IModifiedTypeReference modifiedTypeReference)
        {
        }

        /// <summary>
        /// Performs some computation with the given module.
        /// </summary>
        /// <param name="module"></param>
        public virtual void Visit(IModule module)
        {
        }

        /// <summary>
        /// Performs some computation with the given module reference.
        /// </summary>
        /// <param name="moduleReference"></param>
        public virtual void Visit(IModuleReference moduleReference)
        {
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
        }

        /// <summary>
        /// Performs some computation with the given parameter definition.
        /// </summary>
        /// <param name="parameterDefinition"></param>
        public virtual void Visit(IParameterDefinition parameterDefinition)
        {
        }

        /// <summary>
        /// Performs some computation with the given property definition.
        /// </summary>
        /// <param name="propertyDefinition"></param>
        public virtual void Visit(IPropertyDefinition propertyDefinition)
        {
        }

        /// <summary>
        /// Performs some computation with the given parameter type information.
        /// </summary>
        /// <param name="parameterTypeInformation"></param>
        public virtual void Visit(IParameterTypeInformation parameterTypeInformation)
        {
        }

        /// <summary>
        /// Performs some computation with the given pointer type reference.
        /// </summary>
        /// <param name="pointerTypeReference"></param>
        public virtual void Visit(IPointerTypeReference pointerTypeReference)
        {
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
        public virtual void Visit(ISecurityAttribute securityAttribute)
        {
        }

        /// <summary>
        /// Visits the specified type member.
        /// </summary>
        /// <param name="typeMember">The type member.</param>
        public virtual void Visit(ITypeDefinitionMember typeMember)
        {
            // ITypeDefinitionMember is a base interface that should never be implemented directly.
            // Get typeMember to call the most type specific visitor.
            typeMember.Dispatch(this);
        }

        /// <summary>
        /// Visits the specified type reference.
        /// </summary>
        /// <param name="typeReference">The type reference.</param>
        public virtual void Visit(ITypeReference typeReference)
        {
            // ITypeReference is a base interface that should never be implemented directly.
            // Get typeReference to call the most type specific visitor.
            typeReference.Dispatch(this);
        }

        /// <summary>
        /// Visits the specified unit.
        /// </summary>
        /// <param name="unit">The unit.</param>
        public virtual void Visit(IUnit unit)
        {
            // IUnit is a base interface that should never be implemented directly.
            // Get unit to call the most type specific visitor.
            unit.Dispatch(this);
        }

        /// <summary>
        /// Visits the specified unit reference.
        /// </summary>
        /// <param name="unitReference">The unit reference.</param>
        public virtual void Visit(IUnitReference unitReference)
        {
            // IUnitReference is a base interface that should never be implemented directly.
            // Get unitReference to call the most type specific visitor.
            unitReference.Dispatch(this);
        }

        /// <summary>
        /// Performs some computation with the given Win32 resource.
        /// </summary>
        /// <param name="win32Resource"></param>
        public virtual void Visit(IWin32Resource win32Resource)
        {
        }

        #endregion
    }
}