//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;

// ^ using Microsoft.Contracts;

// TODO: Sometime make the methods and properties of dummy objects Explicit impls so
// that we can track addition and removal of methods and properties.
namespace Microsoft.Cci
{
#pragma warning disable 1591

    internal static class Dummy
    {
        public static IAliasForType AliasForType
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.aliasForType == null)
                {
                    Dummy.aliasForType = new DummyAliasForType();
                }
                
                return Dummy.aliasForType;
            }
        }

        private static IAliasForType/*?*/ aliasForType;

        public static IMetadataConstant Constant
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.constant == null)
                {
                    Dummy.constant = new DummyMetadataConstant();
                }
                
                return Dummy.constant;
            }
        }

        private static IMetadataConstant/*?*/ constant;

        public static ICustomModifier CustomModifier
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.customModifier == null)
                {
                    Dummy.customModifier = new DummyCustomModifier();
                }
                
                return Dummy.customModifier;
            }
        }

        private static ICustomModifier/*?*/ customModifier;

        public static IEventDefinition Event
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.@event == null)
                {
                    Dummy.@event = new DummyEventDefinition();
                }
                
                return Dummy.@event;
            }
        }

        private static IEventDefinition/*?*/ @event;

        public static IFieldDefinition Field
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.field == null)
                {
                    Dummy.field = new DummyFieldDefinition();
                }
                
                return Dummy.field;
            }
        }

        private static IFieldDefinition/*?*/ field;

        public static IMetadataExpression Expression
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.expression == null)
                {
                    Dummy.expression = new DummyMetadataExpression();
                }

                return Dummy.expression;
            }
        }

        private static IMetadataExpression/*?*/ expression;

        public static IFunctionPointer FunctionPointer
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.functionPointer == null)
                {
                    Dummy.functionPointer = new DummyFunctionPointerType();
                }
                
                return Dummy.functionPointer;
            }
        }

        private static IFunctionPointer/*?*/ functionPointer;

        public static IGenericMethodParameter GenericMethodParameter
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.genericMethodParameter == null)
                {
                    Dummy.genericMethodParameter = new DummyGenericMethodParameter();
                }
                
                return Dummy.genericMethodParameter;
            }
        }

        private static DummyGenericMethodParameter/*?*/ genericMethodParameter;

        public static IGenericTypeInstanceReference GenericTypeInstance
        {
            [DebuggerNonUserCode]
            get
            {
                // ^ ensures !result.IsGeneric;
                if (Dummy.genericTypeInstance == null)
                {
                    Dummy.genericTypeInstance = new DummyGenericTypeInstance();
                }

                DummyGenericTypeInstance result = Dummy.genericTypeInstance;

                // ^ assume !result.IsGeneric; // the post condition says so
                return result;
            }
        }

        private static DummyGenericTypeInstance/*?*/ genericTypeInstance;

        public static IGenericTypeParameter GenericTypeParameter
        {
            get
            {
                if (Dummy.genericTypeParameter == null)
                {
                    Dummy.genericTypeParameter = new DummyGenericTypeParameter();
                }

                return Dummy.genericTypeParameter;
            }
        }

        private static IGenericTypeParameter/*?*/ genericTypeParameter;

        public static IMethodDefinition Method
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.method == null)
                {
                    Dummy.method = new DummyMethodDefinition();
                }

                return Dummy.method;
            }
        }

        private static IMethodDefinition/*?*/ method;

        public static IMethodBody MethodBody
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.methodBody == null)
                {
                    Dummy.methodBody = new DummyMethodBody();
                }

                return Dummy.methodBody;
            }
        }

        private static IMethodBody/*?*/ methodBody;

        public static string Name
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.name == null)
                {
                    Dummy.name = String.Empty;
                }

                return Dummy.name;
            }
        }

        private static string/*?*/ name;

        public static IMetadataNamedArgument NamedArgument
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.namedArgument == null)
                {
                    Dummy.namedArgument = new DummyNamedArgument();
                }

                return Dummy.namedArgument;
            }
        }

        private static IMetadataNamedArgument/*?*/ namedArgument;

        public static IPropertyDefinition Property
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.property == null)
                {
                    Dummy.property = new DummyPropertyDefinition();
                }

                return Dummy.property;
            }
        }

        private static IPropertyDefinition/*?*/ property;

        public static ITypeDefinition Type
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.type == null)
                {
                    Dummy.type = new DummyType();
                }

                return Dummy.type;
            }
        }

        private static ITypeDefinition/*?*/ type;

        public static ITypeReference TypeReference
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.typeReference == null)
                {
                    Dummy.typeReference = new DummyTypeReference();
                }

                return Dummy.typeReference;
            }
        }

        private static ITypeReference/*?*/ typeReference;

        public static IUnit Unit
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.unit == null)
                {
                    Dummy.unit = new DummyUnit();
                }

                return Dummy.unit;
            }
        }

        private static IUnit/*?*/ unit;

        public static IModule Module
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.module == null)
                {
                    Dummy.module = new DummyModule();
                }

                return Dummy.module;
            }
        }

        private static IModule/*?*/ module;

        // Issue: This is kind of bad thing to do. What happens to IModule m = loadAssembly(...)   m
        // != Dummy.Module?!?
        public static IAssembly Assembly
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.assembly == null)
                {
                    Dummy.assembly = new DummyAssembly();
                }

                return Dummy.assembly;
            }
        }

        private static IAssembly/*?*/ assembly;

        public static IMethodReference MethodReference
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.methodReference == null)
                {
                    Dummy.methodReference = new DummyMethodReference();
                }

                return Dummy.methodReference;
            }
        }

        private static IMethodReference/*?*/ methodReference;

        public static Version Version
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.version == null)
                {
                    Dummy.version = new Version(0, 0);
                }

                return Dummy.version;
            }
        }

        private static Version/*?*/ version;

        public static ICustomAttribute CustomAttribute
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.customAttribute == null)
                {
                    Dummy.customAttribute = new DummyCustomAttribute();
                }

                return Dummy.customAttribute;
            }
        }

        private static ICustomAttribute/*?*/ customAttribute;

        public static IFileReference FileReference
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.fileReference == null)
                {
                    Dummy.fileReference = new DummyFileReference();
                }

                return Dummy.fileReference;
            }
        }

        private static IFileReference/*?*/ fileReference;

        public static IResource Resource
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.resource == null)
                {
                    Dummy.resource = new DummyResource();
                }

                return Dummy.resource;
            }
        }

        private static IResource/*?*/ resource;

        public static IModuleReference ModuleReference
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.moduleReference == null)
                {
                    Dummy.moduleReference = new DummyModuleReference();
                }

                return Dummy.moduleReference;
            }
        }

        private static IModuleReference/*?*/ moduleReference;

        public static IAssemblyReference AssemblyReference
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.assemblyReference == null)
                {
                    Dummy.assemblyReference = new DummyAssemblyReference();
                }

                return Dummy.assemblyReference;
            }
        }

        private static IAssemblyReference/*?*/ assemblyReference;

        public static IMarshallingInformation MarshallingInformation
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.marshallingInformation == null)
                {
                    Dummy.marshallingInformation = new DummyMarshallingInformation();
                }

                return Dummy.marshallingInformation;
            }
        }

        private static IMarshallingInformation/*?*/ marshallingInformation;

        public static ISecurityAttribute SecurityAttribute
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.securityAttribute == null)
                {
                    Dummy.securityAttribute = new DummySecurityAttribute();
                }

                return Dummy.securityAttribute;
            }
        }

        private static ISecurityAttribute/*?*/ securityAttribute;

        public static IParameterTypeInformation ParameterTypeInformation
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.parameterTypeInformation == null)
                {
                    Dummy.parameterTypeInformation = new DummyParameterTypeInformation();
                }

                return Dummy.parameterTypeInformation;
            }
        }

        private static IParameterTypeInformation/*?*/ parameterTypeInformation;

        public static INamespaceTypeDefinition NamespaceTypeDefinition
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.namespaceTypeDefinition == null)
                {
                    Dummy.namespaceTypeDefinition = new DummyNamespaceTypeDefinition();
                }

                return Dummy.namespaceTypeDefinition;
            }
        }

        private static INamespaceTypeDefinition/*?*/ namespaceTypeDefinition;

        public static ISpecializedPropertyDefinition SpecializedPropertyDefinition
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.specializedPropertyDefinition == null)
                {
                    Dummy.specializedPropertyDefinition = new DummySpecializedPropertyDefinition();
                }

                return Dummy.specializedPropertyDefinition;
            }
        }

        private static ISpecializedPropertyDefinition/*?*/ specializedPropertyDefinition;

        public static ILocalDefinition LocalVariable
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.localVariable == null)
                {
                    Dummy.localVariable = new DummyLocalVariable();
                }

                return Dummy.localVariable;
            }
        }

        private static ILocalDefinition/*?*/ localVariable;

        public static IFieldReference FieldReference
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.fieldReference == null)
                {
                    Dummy.fieldReference = new DummyFieldReference();
                }

                return Dummy.fieldReference;
            }
        }

        private static IFieldReference/*?*/ fieldReference;

        public static IParameterDefinition ParameterDefinition
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.parameterDefinition == null)
                {
                    Dummy.parameterDefinition = new DummyParameterDefinition();
                }

                return Dummy.parameterDefinition;
            }
        }

        private static IParameterDefinition/*?*/ parameterDefinition;

        public static ISectionBlock SectionBlock
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.sectionBlock == null)
                {
                    Dummy.sectionBlock = new DummySectionBlock();
                }

                return Dummy.sectionBlock;
            }
        }

        private static ISectionBlock/*?*/ sectionBlock;

        public static IPlatformInvokeInformation PlatformInvokeInformation
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.platformInvokeInformation == null)
                {
                    Dummy.platformInvokeInformation = new DummyPlatformInvokeInformation();
                }

                return Dummy.platformInvokeInformation;
            }
        }

        private static IPlatformInvokeInformation/*?*/ platformInvokeInformation;

        public static IGlobalMethodDefinition GlobalMethod
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.globalMethodDefinition == null)
                {
                    Dummy.globalMethodDefinition = new DummyGlobalMethodDefinition();
                }

                return Dummy.globalMethodDefinition;
            }
        }

        private static IGlobalMethodDefinition/*?*/ globalMethodDefinition;

        public static IGlobalFieldDefinition GlobalField
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.globalFieldDefinition == null)
                {
                    Dummy.globalFieldDefinition = new DummyGlobalFieldDefinition();
                }

                return Dummy.globalFieldDefinition;
            }
        }

        private static IGlobalFieldDefinition/*?*/ globalFieldDefinition;

        public static IOperation Operation
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.operation == null)
                {
                    Dummy.operation = new DummyOperation();
                }

                return Dummy.operation;
            }
        }

        private static IOperation/*?*/ operation;

        public static ILocation Location
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.location == null)
                {
                    Dummy.location = new DummyLocation();
                }

                return Dummy.location;
            }
        }

        private static ILocation/*?*/ location;

        public static IDocument Document
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.document == null)
                {
                    Dummy.document = new DummyDocument();
                }

                return Dummy.document;
            }
        }

        private static IDocument/*?*/ document;

        public static IOperationExceptionInformation OperationExceptionInformation
        {
            [DebuggerNonUserCode]
            get
            {
                if (Dummy.operationExceptionInformation == null)
                {
                    Dummy.operationExceptionInformation = new DummyOperationExceptionInformation();
                }

                return Dummy.operationExceptionInformation;
            }
        }

        private static IOperationExceptionInformation/*?*/ operationExceptionInformation;
    }
#pragma warning restore 1591
}