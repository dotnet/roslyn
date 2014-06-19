// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Reflection.Metadata;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    internal struct AttributeDescription
    {
        public readonly string Namespace;
        public readonly string Name;
        public readonly byte[][] Signatures;

        // VB matches ExtensionAttribute name and namespace ignoring case (it's the only attribute that matches its name case-insensitively)
        public readonly bool MatchIgnoringCase;

        public AttributeDescription(string @namespace, string name, byte[][] signatures, bool matchIgnoringCase = false)
        {
            Debug.Assert(@namespace != null);
            Debug.Assert(name != null);
            Debug.Assert(signatures != null);

            this.Namespace = @namespace;
            this.Name = name;
            this.Signatures = signatures;
            this.MatchIgnoringCase = matchIgnoringCase;
        }

        public string FullName
        {
            get { return Namespace + "." + Name; }
        }

        public override string ToString()
        {
            return FullName + "(" + Signatures.Length + ")";
        }

        internal int GetParameterCount(int signatureIndex)
        {
            var signature = this.Signatures[signatureIndex];

            // only instance ctors are allowed:
            Debug.Assert(signature[0] == SignatureHeader.HasThis);

            // parameter count is the second element of the signature:
            return signature[1];
        }

        // shortcuts for signature elements supported by our signature comparer:
        private const byte Void = (byte)SignatureTypeCode.Void;
        private const byte Boolean = (byte)SignatureTypeCode.Boolean;
        private const byte Char = (byte)SignatureTypeCode.Char;
        private const byte SByte = (byte)SignatureTypeCode.SByte;
        private const byte Byte = (byte)SignatureTypeCode.Byte;
        private const byte Int16 = (byte)SignatureTypeCode.Int16;
        private const byte UInt16 = (byte)SignatureTypeCode.UInt16;
        private const byte Int32 = (byte)SignatureTypeCode.Int32;
        private const byte UInt32 = (byte)SignatureTypeCode.UInt32;
        private const byte Int64 = (byte)SignatureTypeCode.Int64;
        private const byte UInt64 = (byte)SignatureTypeCode.UInt64;
        private const byte Single = (byte)SignatureTypeCode.Single;
        private const byte Double = (byte)SignatureTypeCode.Double;
        private const byte String = (byte)SignatureTypeCode.String;
        private const byte Object = (byte)SignatureTypeCode.Object;
        private const byte SzArray = (byte)SignatureTypeCode.SZArray;
        private const byte TypeHandle = (byte)SignatureTypeCode.TypeHandle;

        internal enum TypeHandleTarget : byte
        {
            AttributeTargets,
            AssemblyNameFlags,
            MethodImplOptions,
            CharSet,
            LayoutKind,
            UnmanagedType,
            TypeLibTypeFlags,
            ClassInterfaceType,
            ComInterfaceType,
            CompilationRelaxations,
            DebuggingModes,
            SecurityCriticalScope,
            CallingConvention,
            AssemblyHashAlgorithm,
            TransactionOption,
            SecurityAction,
            SystemType,
            DeprecationType,
            Platform
        }

        internal struct TypeHandleTargetInfo
        {
            public readonly string Namespace;
            public readonly string Name;
            public readonly SerializationTypeCode Underlying;

            public TypeHandleTargetInfo(string @namespace, string name, SerializationTypeCode underlying)
            {
                Namespace = @namespace;
                Name = name;
                Underlying = underlying;
            }
        }

        internal static ImmutableArray<TypeHandleTargetInfo> TypeHandleTargets;

        static AttributeDescription()
        {
            const string system = "System";
            const string compilerServices = "System.Runtime.CompilerServices";
            const string interopServices = "System.Runtime.InteropServices";

            TypeHandleTargets = (new[] {
                 new TypeHandleTargetInfo(system,"AttributeTargets", SerializationTypeCode.Int32)
                ,new TypeHandleTargetInfo("System.Reflection","AssemblyNameFlags", SerializationTypeCode.Int32)
                ,new TypeHandleTargetInfo(compilerServices,"MethodImplOptions", SerializationTypeCode.Int32)
                ,new TypeHandleTargetInfo(interopServices,"CharSet", SerializationTypeCode.Int32)
                ,new TypeHandleTargetInfo(interopServices,"LayoutKind", SerializationTypeCode.Int32)
                ,new TypeHandleTargetInfo(interopServices,"UnmanagedType", SerializationTypeCode.Int32)
                ,new TypeHandleTargetInfo(interopServices,"TypeLibTypeFlags", SerializationTypeCode.Int32)
                ,new TypeHandleTargetInfo(interopServices,"ClassInterfaceType", SerializationTypeCode.Int32)
                ,new TypeHandleTargetInfo(interopServices,"ComInterfaceType", SerializationTypeCode.Int32)
                ,new TypeHandleTargetInfo(compilerServices,"CompilationRelaxations", SerializationTypeCode.Int32)
                ,new TypeHandleTargetInfo("System.Diagnostics.DebuggableAttribute","DebuggingModes", SerializationTypeCode.Int32)
                ,new TypeHandleTargetInfo("System.Security","SecurityCriticalScope", SerializationTypeCode.Int32)
                ,new TypeHandleTargetInfo(interopServices,"CallingConvention", SerializationTypeCode.Int32)
                ,new TypeHandleTargetInfo("System.Configuration.Assemblies","AssemblyHashAlgorithm", SerializationTypeCode.Int32)
                ,new TypeHandleTargetInfo("System.EnterpriseServices","TransactionOption", SerializationTypeCode.Int32)
                ,new TypeHandleTargetInfo("System.Security.Permissions","SecurityAction", SerializationTypeCode.Int32)
                ,new TypeHandleTargetInfo(system,"Type", SerializationTypeCode.Type)
                ,new TypeHandleTargetInfo("Windows.Foundation.Metadata","DeprecationType", SerializationTypeCode.Int32)
                ,new TypeHandleTargetInfo("Windows.Foundation.Metadata","Platform", SerializationTypeCode.Int32)
            }).AsImmutable();
        }

        private static readonly byte[] Signature_HasThis_Void = new byte[] { SignatureHeader.HasThis, 0, Void };
        private static readonly byte[] Signature_HasThis_Void_Int16 = new byte[] { SignatureHeader.HasThis, 1, Void, Int16 };
        private static readonly byte[] Signature_HasThis_Void_Int32 = new byte[] { SignatureHeader.HasThis, 1, Void, Int32 };
        private static readonly byte[] Signature_HasThis_Void_Int32_Int32_Int32 = new byte[] { SignatureHeader.HasThis, 3, Void, Int32, Int32, Int32 };
        private static readonly byte[] Signature_HasThis_Void_UInt32 = new byte[] { SignatureHeader.HasThis, 1, Void, UInt32 };
        private static readonly byte[] Signature_HasThis_Void_Int32_Int32 = new byte[] { SignatureHeader.HasThis, 2, Void, Int32, Int32 };
        private static readonly byte[] Signature_HasThis_Void_Int32_Int32_Int32_Int32 = new byte[] { SignatureHeader.HasThis, 4, Void, Int32, Int32, Int32, Int32 };
        private static readonly byte[] Signature_HasThis_Void_String = new byte[] { SignatureHeader.HasThis, 1, Void, String };
        private static readonly byte[] Signature_HasThis_Void_Object = new byte[] { SignatureHeader.HasThis, 1, Void, Object };
        private static readonly byte[] Signature_HasThis_Void_String_String = new byte[] { SignatureHeader.HasThis, 2, Void, String, String };
        private static readonly byte[] Signature_HasThis_Void_String_Boolean = new byte[] { SignatureHeader.HasThis, 2, Void, String, Boolean };
        private static readonly byte[] Signature_HasThis_Void_String_String_String = new byte[] { SignatureHeader.HasThis, 3, Void, String, String, String };
        private static readonly byte[] Signature_HasThis_Void_String_String_String_String = new byte[] { SignatureHeader.HasThis, 4, Void, String, String, String, String };
        private static readonly byte[] Signature_HasThis_Void_AttributeTargets = new byte[] { SignatureHeader.HasThis, 1, Void, TypeHandle, (byte)TypeHandleTarget.AttributeTargets };
        private static readonly byte[] Signature_HasThis_Void_AssemblyNameFlags = new byte[] { SignatureHeader.HasThis, 1, Void, TypeHandle, (byte)TypeHandleTarget.AssemblyNameFlags };
        private static readonly byte[] Signature_HasThis_Void_MethodImplOptions = new byte[] { SignatureHeader.HasThis, 1, Void, TypeHandle, (byte)TypeHandleTarget.MethodImplOptions };
        private static readonly byte[] Signature_HasThis_Void_CharSet = new byte[] { SignatureHeader.HasThis, 1, Void, TypeHandle, (byte)TypeHandleTarget.CharSet };
        private static readonly byte[] Signature_HasThis_Void_LayoutKind = new byte[] { SignatureHeader.HasThis, 1, Void, TypeHandle, (byte)TypeHandleTarget.LayoutKind };
        private static readonly byte[] Signature_HasThis_Void_UnmanagedType = new byte[] { SignatureHeader.HasThis, 1, Void, TypeHandle, (byte)TypeHandleTarget.UnmanagedType };
        private static readonly byte[] Signature_HasThis_Void_TypeLibTypeFlags = new byte[] { SignatureHeader.HasThis, 1, Void, TypeHandle, (byte)TypeHandleTarget.TypeLibTypeFlags };
        private static readonly byte[] Signature_HasThis_Void_ClassInterfaceType = new byte[] { SignatureHeader.HasThis, 1, Void, TypeHandle, (byte)TypeHandleTarget.ClassInterfaceType };
        private static readonly byte[] Signature_HasThis_Void_ComInterfaceType = new byte[] { SignatureHeader.HasThis, 1, Void, TypeHandle, (byte)TypeHandleTarget.ComInterfaceType };
        private static readonly byte[] Signature_HasThis_Void_CompilationRelaxations = new byte[] { SignatureHeader.HasThis, 1, Void, TypeHandle, (byte)TypeHandleTarget.CompilationRelaxations };
        private static readonly byte[] Signature_HasThis_Void_DebuggingModes = new byte[] { SignatureHeader.HasThis, 1, Void, TypeHandle, (byte)TypeHandleTarget.DebuggingModes };
        private static readonly byte[] Signature_HasThis_Void_SecurityCriticalScope = new byte[] { SignatureHeader.HasThis, 1, Void, TypeHandle, (byte)TypeHandleTarget.SecurityCriticalScope };
        private static readonly byte[] Signature_HasThis_Void_CallingConvention = new byte[] { SignatureHeader.HasThis, 1, Void, TypeHandle, (byte)TypeHandleTarget.CallingConvention };
        private static readonly byte[] Signature_HasThis_Void_AssemblyHashAlgorithm = new byte[] { SignatureHeader.HasThis, 1, Void, TypeHandle, (byte)TypeHandleTarget.AssemblyHashAlgorithm };
        private static readonly byte[] Signature_HasThis_Void_Int64 = new byte[] { SignatureHeader.HasThis, 1, Void, Int64 };
        private static readonly byte[] Signature_HasThis_Void_UInt8_UInt8_UInt32_UInt32_UInt32 = new byte[] {
            SignatureHeader.HasThis, 5, Void, Byte, Byte, UInt32, UInt32, UInt32 };
        private static readonly byte[] Signature_HasThis_Void_UIn8_UInt8_Int32_Int32_Int32 = new byte[] {
            SignatureHeader.HasThis, 5, Void, Byte, Byte, Int32, Int32, Int32 };


        private static readonly byte[] Signature_HasThis_Void_Boolean = new byte[] { SignatureHeader.HasThis, 1, Void, Boolean };
        private static readonly byte[] Signature_HasThis_Void_Boolean_Boolean = new byte[] { SignatureHeader.HasThis, 2, Void, Boolean, Boolean };

        private static readonly byte[] Signature_HasThis_Void_Boolean_TransactionOption = new byte[] { SignatureHeader.HasThis, 2, Void, Boolean, TypeHandle, (byte)TypeHandleTarget.TransactionOption };
        private static readonly byte[] Signature_HasThis_Void_Boolean_TransactionOption_Int32 = new byte[] { SignatureHeader.HasThis, 3, Void, Boolean, TypeHandle, (byte)TypeHandleTarget.TransactionOption, Int32 };
        private static readonly byte[] Signature_HasThis_Void_Boolean_TransactionOption_Int32_Boolean = new byte[] { SignatureHeader.HasThis, 4, Void, Boolean, TypeHandle, (byte)TypeHandleTarget.TransactionOption, Int32, Boolean };

        private static readonly byte[] Signature_HasThis_Void_SecurityAction = new byte[] { SignatureHeader.HasThis, 1, Void, TypeHandle, (byte)TypeHandleTarget.SecurityAction };
        private static readonly byte[] Signature_HasThis_Void_Type = new byte[] { SignatureHeader.HasThis, 1, Void, TypeHandle, (byte)TypeHandleTarget.SystemType };
        private static readonly byte[] Signature_HasThis_Void_Type_Type = new byte[] { SignatureHeader.HasThis, 2, Void, TypeHandle, (byte)TypeHandleTarget.SystemType, TypeHandle, (byte)TypeHandleTarget.SystemType };
        private static readonly byte[] Signature_HasThis_Void_Type_Type_Type = new byte[] { SignatureHeader.HasThis, 3, Void, TypeHandle, (byte)TypeHandleTarget.SystemType, TypeHandle, (byte)TypeHandleTarget.SystemType, TypeHandle, (byte)TypeHandleTarget.SystemType };
        private static readonly byte[] Signature_HasThis_Void_Type_Type_Type_Type = new byte[] { SignatureHeader.HasThis, 4, Void, TypeHandle, (byte)TypeHandleTarget.SystemType, TypeHandle, (byte)TypeHandleTarget.SystemType, TypeHandle, (byte)TypeHandleTarget.SystemType, TypeHandle, (byte)TypeHandleTarget.SystemType };
        private static readonly byte[] Signature_HasThis_Void_Type_Int32 = new byte[] { SignatureHeader.HasThis, 2, Void, TypeHandle, (byte)TypeHandleTarget.SystemType, Int32 };

        private static readonly byte[] Signature_HasThis_Void_SzArray_Boolean = new byte[] { SignatureHeader.HasThis, 1, Void, SzArray, Boolean };

        private static readonly byte[] Signature_HasThis_Void_String_DeprecationType_UInt32 = new byte[] { SignatureHeader.HasThis, 3, Void, String, TypeHandle, (byte)TypeHandleTarget.DeprecationType, UInt32 };
        private static readonly byte[] Signature_HasThis_Void_String_DeprecationType_UInt32_Platform = new byte[] { SignatureHeader.HasThis, 4, Void, String, TypeHandle, (byte)TypeHandleTarget.DeprecationType, UInt32, TypeHandle, (byte)TypeHandleTarget.Platform };

        // TODO: We should reuse the byte arrays for well-known attributes with same signatures.

        private static readonly byte[][] signaturesOfTypeIdentifierAttribute = { Signature_HasThis_Void, Signature_HasThis_Void_String_String };
        private static readonly byte[][] signaturesOfStandardModuleAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfExtensionAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfAttributeUsage = { Signature_HasThis_Void_AttributeTargets };
        private static readonly byte[][] signaturesOfAssemblySignatureKeyAttribute = { Signature_HasThis_Void_String_String };
        private static readonly byte[][] signaturesOfAssemblyKeyFileAttribute = { Signature_HasThis_Void_String };
        private static readonly byte[][] signaturesOfAssemblyKeyNameAttribute = { Signature_HasThis_Void_String };
        private static readonly byte[][] signaturesOfAssemblyDelaySignAttribute = { Signature_HasThis_Void_Boolean };
        private static readonly byte[][] signaturesOfAssemblyVersionAttribute = { Signature_HasThis_Void_String };
        private static readonly byte[][] signaturesOfAssemblyFileVersionAttribute = { Signature_HasThis_Void_String };
        private static readonly byte[][] signaturesOfAssemblyTitleAttribute = { Signature_HasThis_Void_String };
        private static readonly byte[][] signaturesOfAssemblyDescriptionAttribute = { Signature_HasThis_Void_String };
        private static readonly byte[][] signaturesOfAssemblyCultureAttribute = { Signature_HasThis_Void_String };
        private static readonly byte[][] signaturesOfAssemblyCompanyAttribute = { Signature_HasThis_Void_String };
        private static readonly byte[][] signaturesOfAssemblyProductAttribute = { Signature_HasThis_Void_String };
        private static readonly byte[][] signaturesOfAssemblyInformationalVersionAttribute = { Signature_HasThis_Void_String };
        private static readonly byte[][] signaturesOfAssemblyCopyrightAttribute = { Signature_HasThis_Void_String };
        private static readonly byte[][] signaturesOfSatelliteContractVersionAttribute = { Signature_HasThis_Void_String };
        private static readonly byte[][] signaturesOfAssemblyTrademarkAttribute = { Signature_HasThis_Void_String };
        private static readonly byte[][] signaturesOfAssemblyFlagsAttribute =
        {
            Signature_HasThis_Void_AssemblyNameFlags,
            Signature_HasThis_Void_Int32,
            Signature_HasThis_Void_UInt32
        };
        private static readonly byte[][] signaturesOfDefaultMemberAttribute = { Signature_HasThis_Void_String };
        private static readonly byte[][] signaturesOfAccessedThroughPropertyAttribute = { Signature_HasThis_Void_String };
        private static readonly byte[][] signaturesOfIndexerNameAttribute = { Signature_HasThis_Void_String };
        private static readonly byte[][] signaturesOfInternalsVisibleToAttribute = { Signature_HasThis_Void_String };
        private static readonly byte[][] signaturesOfOptionalAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfDefaultParameterValueAttribute = { Signature_HasThis_Void_Object };
        private static readonly byte[][] signaturesOfDateTimeConstantAttribute = { Signature_HasThis_Void_Int64 };
        private static readonly byte[][] signaturesOfDecimalConstantAttribute = { Signature_HasThis_Void_UInt8_UInt8_UInt32_UInt32_UInt32, Signature_HasThis_Void_UIn8_UInt8_Int32_Int32_Int32 };
        private static readonly byte[][] signaturesOfIUnknownConstantAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfCallerFilePathAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfCallerLineNumberAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfCallerMemberNameAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfIDispatchConstantAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfParamArrayAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfDllImportAttribute = { Signature_HasThis_Void_String };
        private static readonly byte[][] signaturesOfUnverifiableCodeAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfSecurityPermissionAttribute = { Signature_HasThis_Void_SecurityAction };
        private static readonly byte[][] signaturesOfCoClassAttribute = { Signature_HasThis_Void_Type };
        private static readonly byte[][] signaturesOfComImportAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfGuidAttribute = { Signature_HasThis_Void_String };
        private static readonly byte[][] signaturesOfCLSCompliantAttribute = { Signature_HasThis_Void_Boolean };

        private static readonly byte[][] signaturesOfMethodImplAttribute =
        {
            Signature_HasThis_Void,
            Signature_HasThis_Void_Int16,
            Signature_HasThis_Void_MethodImplOptions,
        };

        private static readonly byte[][] signaturesOfPreserveSigAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfDefaultCharSetAttribute = { Signature_HasThis_Void_CharSet };

        private static readonly byte[][] signaturesOfSpecialNameAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfNonSerializedAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfFieldOffsetAttribute = { Signature_HasThis_Void_Int32 };
        private static readonly byte[][] signaturesOfSerializableAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfInAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfOutAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfFixedBufferAttribute = { Signature_HasThis_Void_Type_Int32 };
        private static readonly byte[][] signaturesOfSuppressUnmanagedCodeSecurityAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfPrincipalPermissionAttribute = { Signature_HasThis_Void_SecurityAction };
        private static readonly byte[][] signaturesOfPermissionSetAttribute = { Signature_HasThis_Void_SecurityAction };

        private static readonly byte[][] signaturesOfStructLayoutAttribute =
        {
            Signature_HasThis_Void_Int16,
            Signature_HasThis_Void_LayoutKind,
        };

        private static readonly byte[][] signaturesOfMarshalAsAttribute =
        {
            Signature_HasThis_Void_Int16,
            Signature_HasThis_Void_UnmanagedType,
        };

        private static readonly byte[][] signaturesOfTypeLibTypeAttribute =
        {
            Signature_HasThis_Void_Int16,
            Signature_HasThis_Void_TypeLibTypeFlags,
        };

        private static readonly byte[][] signaturesOfWebMethodAttribute =
        {
            Signature_HasThis_Void,
            Signature_HasThis_Void_Boolean,
            Signature_HasThis_Void_Boolean_TransactionOption,
            Signature_HasThis_Void_Boolean_TransactionOption_Int32,
            Signature_HasThis_Void_Boolean_TransactionOption_Int32_Boolean
        };

        private static readonly byte[][] signaturesOfHostProtectionAttribute =
        {
            Signature_HasThis_Void,
            Signature_HasThis_Void_SecurityAction
        };

        private static readonly byte[][] signaturesOfVisualBasicEmbedded = { Signature_HasThis_Void };

        private static readonly byte[][] signaturesOfVisualBasicComClassAttribute =
        {
            Signature_HasThis_Void,
            Signature_HasThis_Void_String,
            Signature_HasThis_Void_String_String,
            Signature_HasThis_Void_String_String_String
        };

        private static readonly byte[][] signaturesOfClassInterfaceAttribute =
        {
            Signature_HasThis_Void_Int16,
            Signature_HasThis_Void_ClassInterfaceType
        };

        private static readonly byte[][] signaturesOfInterfaceTypeAttribute =
        {
            Signature_HasThis_Void_Int16,
            Signature_HasThis_Void_ComInterfaceType
        };

        private static readonly byte[][] signaturesOfCompilationRelaxationsAttribute =
        {
            Signature_HasThis_Void_Int32,
            Signature_HasThis_Void_CompilationRelaxations
        };

        private static readonly byte[][] signaturesOfDebuggableAttribute =
        {
            Signature_HasThis_Void_Boolean_Boolean,
            Signature_HasThis_Void_DebuggingModes
        };

        private static readonly byte[][] signaturesOfComSourceInterfacesAttribute =
        {
            Signature_HasThis_Void_String,
            Signature_HasThis_Void_Type,
            Signature_HasThis_Void_Type_Type,
            Signature_HasThis_Void_Type_Type_Type,
            Signature_HasThis_Void_Type_Type_Type_Type
        };

        private static readonly byte[][] signaturesOfComVisibleAttribute = { Signature_HasThis_Void_Boolean };
        private static readonly byte[][] signaturesOfConditionalAttribute = { Signature_HasThis_Void_String };
        private static readonly byte[][] signaturesOfTypeLibVersionAttribute = { Signature_HasThis_Void_Int32_Int32 };
        private static readonly byte[][] signaturesOfComCompatibleVersionAttribute = { Signature_HasThis_Void_Int32_Int32_Int32_Int32 };
        private static readonly byte[][] signaturesOfWindowsRuntimeImportAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfDynamicSecurityMethodAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfRequiredAttributeAttribute = { Signature_HasThis_Void_Type };
        private static readonly byte[][] signaturesOfRuntimeCompatibilityAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfTypeForwardedToAttribute = { Signature_HasThis_Void_Type };
        private static readonly byte[][] signaturesOfSTAThreadAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfMTAThreadAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfOptionCompareAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfObsoleteAttribute = { Signature_HasThis_Void, Signature_HasThis_Void_String, Signature_HasThis_Void_String_Boolean };
        private static readonly byte[][] signaturesOfDynamicAttribute = { Signature_HasThis_Void, Signature_HasThis_Void_SzArray_Boolean };
        private static readonly byte[][] signaturesOfDebuggerHiddenAttribute = { Signature_HasThis_Void };

        private static readonly byte[][] signaturesOfSecurityCriticalAttribute =
        {
            Signature_HasThis_Void,
            Signature_HasThis_Void_SecurityCriticalScope
        };

        private static readonly byte[][] signaturesOfSecuritySafeCriticalAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfDesignerGeneratedAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfMyGroupCollectionAttribute = { Signature_HasThis_Void_String_String_String_String };
        private static readonly byte[][] signaturesOfComEventInterfaceAttribute = { Signature_HasThis_Void_Type_Type };
        private static readonly byte[][] signaturesOfBestFitMappingAttribute = { Signature_HasThis_Void_Boolean };
        private static readonly byte[][] signaturesOfFlagsAttribute = { Signature_HasThis_Void };
        private static readonly byte[][] signaturesOfLCIDConversionAttribute = { Signature_HasThis_Void_Int32 };
        private static readonly byte[][] signaturesOfUnmanagedFunctionPointerAttribute = { Signature_HasThis_Void_CallingConvention };
        private static readonly byte[][] signaturesOfPrimaryInteropAssemblyAttribute = { Signature_HasThis_Void_Int32_Int32 };
        private static readonly byte[][] signaturesOfImportedFromTypeLibAttribute = { Signature_HasThis_Void_String };
        private static readonly byte[][] signaturesOfDefaultEventAttribute = { Signature_HasThis_Void_String };

        private static readonly byte[][] signaturesOfAssemblyConfigurationAttribute = { Signature_HasThis_Void_String };
        private static readonly byte[][] signaturesOfAssemblyAlgorithmIdAttribute =
        {
            Signature_HasThis_Void_AssemblyHashAlgorithm,
            Signature_HasThis_Void_UInt32
        };

        private static readonly byte[][] signaturesOfDeprecatedAttribute =
        {
            Signature_HasThis_Void_String_DeprecationType_UInt32,
            Signature_HasThis_Void_String_DeprecationType_UInt32_Platform
        };

        private static readonly byte[][] signaturesOfFSharpInterfaceDataVersionAttribute = { Signature_HasThis_Void_Int32_Int32_Int32 };

        // early decoded attributes:
        internal static readonly AttributeDescription OptionalAttribute = new AttributeDescription("System.Runtime.InteropServices", "OptionalAttribute", signaturesOfOptionalAttribute);
        internal static readonly AttributeDescription ComImportAttribute = new AttributeDescription("System.Runtime.InteropServices", "ComImportAttribute", signaturesOfComImportAttribute);
        internal static readonly AttributeDescription AttributeUsageAttribute = new AttributeDescription("System", "AttributeUsageAttribute", signaturesOfAttributeUsage);
        internal static readonly AttributeDescription ConditionalAttribute = new AttributeDescription("System.Diagnostics", "ConditionalAttribute", signaturesOfConditionalAttribute);
        internal static readonly AttributeDescription CaseInsensitiveExtensionAttribute = new AttributeDescription("System.Runtime.CompilerServices", "ExtensionAttribute", signaturesOfExtensionAttribute, matchIgnoringCase: true);
        internal static readonly AttributeDescription CaseSensitiveExtensionAttribute = new AttributeDescription("System.Runtime.CompilerServices", "ExtensionAttribute", signaturesOfExtensionAttribute, matchIgnoringCase: false);
        internal static readonly AttributeDescription FSharpInterfaceDataVersionAttribute = new AttributeDescription("Microsoft.FSharp.Core", "FSharpInterfaceDataVersionAttribute", signaturesOfFSharpInterfaceDataVersionAttribute);

        internal static readonly AttributeDescription InternalsVisibleToAttribute = new AttributeDescription("System.Runtime.CompilerServices", "InternalsVisibleToAttribute", signaturesOfInternalsVisibleToAttribute);
        internal static readonly AttributeDescription AssemblySignatureKeyAttribute = new AttributeDescription("System.Reflection", "AssemblySignatureKeyAttribute", signaturesOfAssemblySignatureKeyAttribute);
        internal static readonly AttributeDescription AssemblyKeyFileAttribute = new AttributeDescription("System.Reflection", "AssemblyKeyFileAttribute", signaturesOfAssemblyKeyFileAttribute);
        internal static readonly AttributeDescription AssemblyKeyNameAttribute = new AttributeDescription("System.Reflection", "AssemblyKeyNameAttribute", signaturesOfAssemblyKeyNameAttribute);
        internal static readonly AttributeDescription ParamArrayAttribute = new AttributeDescription("System", "ParamArrayAttribute", signaturesOfParamArrayAttribute);
        internal static readonly AttributeDescription DefaultMemberAttribute = new AttributeDescription("System.Reflection", "DefaultMemberAttribute", signaturesOfDefaultMemberAttribute);
        internal static readonly AttributeDescription IndexerNameAttribute = new AttributeDescription("System.Runtime.CompilerServices", "IndexerNameAttribute", signaturesOfIndexerNameAttribute);
        internal static readonly AttributeDescription AssemblyDelaySignAttribute = new AttributeDescription("System.Reflection", "AssemblyDelaySignAttribute", signaturesOfAssemblyDelaySignAttribute);
        internal static readonly AttributeDescription AssemblyVersionAttribute = new AttributeDescription("System.Reflection", "AssemblyVersionAttribute", signaturesOfAssemblyVersionAttribute);
        internal static readonly AttributeDescription AssemblyFileVersionAttribute = new AttributeDescription("System.Reflection", "AssemblyFileVersionAttribute", signaturesOfAssemblyFileVersionAttribute);
        internal static readonly AttributeDescription AssemblyTitleAttribute = new AttributeDescription("System.Reflection", "AssemblyTitleAttribute", signaturesOfAssemblyTitleAttribute);
        internal static readonly AttributeDescription AssemblyDescriptionAttribute = new AttributeDescription("System.Reflection", "AssemblyDescriptionAttribute", signaturesOfAssemblyDescriptionAttribute);
        internal static readonly AttributeDescription AssemblyCultureAttribute = new AttributeDescription("System.Reflection", "AssemblyCultureAttribute", signaturesOfAssemblyCultureAttribute);
        internal static readonly AttributeDescription AssemblyCompanyAttribute = new AttributeDescription("System.Reflection", "AssemblyCompanyAttribute", signaturesOfAssemblyCompanyAttribute);
        internal static readonly AttributeDescription AssemblyProductAttribute = new AttributeDescription("System.Reflection", "AssemblyProductAttribute", signaturesOfAssemblyProductAttribute);
        internal static readonly AttributeDescription AssemblyInformationalVersionAttribute = new AttributeDescription("System.Reflection", "AssemblyInformationalVersionAttribute", signaturesOfAssemblyInformationalVersionAttribute);
        internal static readonly AttributeDescription AssemblyCopyrightAttribute = new AttributeDescription("System.Reflection", "AssemblyCopyrightAttribute", signaturesOfAssemblyCopyrightAttribute);
        internal static readonly AttributeDescription SatelliteContractVersionAttribute = new AttributeDescription("System.Resources", "SatelliteContractVersionAttribute", signaturesOfSatelliteContractVersionAttribute);
        internal static readonly AttributeDescription AssemblyTrademarkAttribute = new AttributeDescription("System.Reflection", "AssemblyTrademarkAttribute", signaturesOfAssemblyTrademarkAttribute);
        internal static readonly AttributeDescription AssemblyFlagsAttribute = new AttributeDescription("System.Reflection", "AssemblyFlagsAttribute", signaturesOfAssemblyFlagsAttribute);
        internal static readonly AttributeDescription DecimalConstantAttribute = new AttributeDescription("System.Runtime.CompilerServices", "DecimalConstantAttribute", signaturesOfDecimalConstantAttribute);
        internal static readonly AttributeDescription IUnknownConstantAttribute = new AttributeDescription("System.Runtime.CompilerServices", "IUnknownConstantAttribute", signaturesOfIUnknownConstantAttribute);
        internal static readonly AttributeDescription CallerFilePathAttribute = new AttributeDescription("System.Runtime.CompilerServices", "CallerFilePathAttribute", signaturesOfCallerFilePathAttribute);
        internal static readonly AttributeDescription CallerLineNumberAttribute = new AttributeDescription("System.Runtime.CompilerServices", "CallerLineNumberAttribute", signaturesOfCallerLineNumberAttribute);
        internal static readonly AttributeDescription CallerMemberNameAttribute = new AttributeDescription("System.Runtime.CompilerServices", "CallerMemberNameAttribute", signaturesOfCallerMemberNameAttribute);
        internal static readonly AttributeDescription IDispatchConstantAttribute = new AttributeDescription("System.Runtime.CompilerServices", "IDispatchConstantAttribute", signaturesOfIDispatchConstantAttribute);
        internal static readonly AttributeDescription DefaultParameterValueAttribute = new AttributeDescription("System.Runtime.InteropServices", "DefaultParameterValueAttribute", signaturesOfDefaultParameterValueAttribute);
        internal static readonly AttributeDescription UnverifiableCodeAttribute = new AttributeDescription("System.Runtime.InteropServices", "UnverifiableCodeAttribute", signaturesOfUnverifiableCodeAttribute);
        internal static readonly AttributeDescription SecurityPermissionAttribute = new AttributeDescription("System.Runtime.InteropServices", "SecurityPermissionAttribute", signaturesOfSecurityPermissionAttribute);
        internal static readonly AttributeDescription DllImportAttribute = new AttributeDescription("System.Runtime.InteropServices", "DllImportAttribute", signaturesOfDllImportAttribute);
        internal static readonly AttributeDescription MethodImplAttribute = new AttributeDescription("System.Runtime.CompilerServices", "MethodImplAttribute", signaturesOfMethodImplAttribute);
        internal static readonly AttributeDescription PreserveSigAttribute = new AttributeDescription("System.Runtime.InteropServices", "PreserveSigAttribute", signaturesOfPreserveSigAttribute);
        internal static readonly AttributeDescription DefaultCharSetAttribute = new AttributeDescription("System.Runtime.InteropServices", "DefaultCharSetAttribute", signaturesOfDefaultCharSetAttribute);
        internal static readonly AttributeDescription SpecialNameAttribute = new AttributeDescription("System.Runtime.CompilerServices", "SpecialNameAttribute", signaturesOfSpecialNameAttribute);
        internal static readonly AttributeDescription SerializableAttribute = new AttributeDescription("System", "SerializableAttribute", signaturesOfSerializableAttribute);
        internal static readonly AttributeDescription NonSerializedAttribute = new AttributeDescription("System", "NonSerializedAttribute", signaturesOfNonSerializedAttribute);
        internal static readonly AttributeDescription StructLayoutAttribute = new AttributeDescription("System.Runtime.InteropServices", "StructLayoutAttribute", signaturesOfStructLayoutAttribute);
        internal static readonly AttributeDescription FieldOffsetAttribute = new AttributeDescription("System.Runtime.InteropServices", "FieldOffsetAttribute", signaturesOfFieldOffsetAttribute);
        internal static readonly AttributeDescription FixedBufferAttribute = new AttributeDescription("System.Runtime.CompilerServices", "FixedBufferAttribute", signaturesOfFixedBufferAttribute);
        internal static readonly AttributeDescription MarshalAsAttribute = new AttributeDescription("System.Runtime.InteropServices", "MarshalAsAttribute", signaturesOfMarshalAsAttribute);
        internal static readonly AttributeDescription InAttribute = new AttributeDescription("System.Runtime.InteropServices", "InAttribute", signaturesOfInAttribute);
        internal static readonly AttributeDescription OutAttribute = new AttributeDescription("System.Runtime.InteropServices", "OutAttribute", signaturesOfOutAttribute);
        internal static readonly AttributeDescription CoClassAttribute = new AttributeDescription("System.Runtime.InteropServices", "CoClassAttribute", signaturesOfCoClassAttribute);
        internal static readonly AttributeDescription GuidAttribute = new AttributeDescription("System.Runtime.InteropServices", "GuidAttribute", signaturesOfGuidAttribute);
        internal static readonly AttributeDescription CLSCompliantAttribute = new AttributeDescription("System", "CLSCompliantAttribute", signaturesOfCLSCompliantAttribute);
        internal static readonly AttributeDescription HostProtectionAttribute = new AttributeDescription("System.Security.Permissions", "HostProtectionAttribute", signaturesOfHostProtectionAttribute);
        internal static readonly AttributeDescription SuppressUnmanagedCodeSecurityAttribute = new AttributeDescription("System.Security", "SuppressUnmanagedCodeSecurityAttribute", signaturesOfSuppressUnmanagedCodeSecurityAttribute);
        internal static readonly AttributeDescription PrincipalPermissionAttribute = new AttributeDescription("System.Security.Permissions", "PrincipalPermissionAttribute", signaturesOfPrincipalPermissionAttribute);
        internal static readonly AttributeDescription PermissionSetAttribute = new AttributeDescription("System.Security.Permissions", "PermissionSetAttribute", signaturesOfPermissionSetAttribute);
        internal static readonly AttributeDescription TypeIdentifierAttribute = new AttributeDescription("System.Runtime.InteropServices", "TypeIdentifierAttribute", signaturesOfTypeIdentifierAttribute);
        internal static readonly AttributeDescription VisualBasicEmbeddedAttribute = new AttributeDescription("Microsoft.VisualBasic", "Embedded", signaturesOfVisualBasicEmbedded);
        internal static readonly AttributeDescription VisualBasicComClassAttribute = new AttributeDescription("Microsoft.VisualBasic", "ComClassAttribute", signaturesOfVisualBasicComClassAttribute);
        internal static readonly AttributeDescription StandardModuleAttribute = new AttributeDescription("Microsoft.VisualBasic.CompilerServices", "StandardModuleAttribute", signaturesOfStandardModuleAttribute);
        internal static readonly AttributeDescription OptionCompareAttribute = new AttributeDescription("Microsoft.VisualBasic.CompilerServices", "OptionCompareAttribute", signaturesOfOptionCompareAttribute);
        internal static readonly AttributeDescription AccessedThroughPropertyAttribute = new AttributeDescription("System.Runtime.CompilerServices", "AccessedThroughPropertyAttribute", signaturesOfAccessedThroughPropertyAttribute);
        internal static readonly AttributeDescription WebMethodAttribute = new AttributeDescription("System.Web.Services", "WebMethodAttribute", signaturesOfWebMethodAttribute);
        internal static readonly AttributeDescription DateTimeConstantAttribute = new AttributeDescription("System.Runtime.CompilerServices", "DateTimeConstantAttribute", signaturesOfDateTimeConstantAttribute);
        internal static readonly AttributeDescription ClassInterfaceAttribute = new AttributeDescription("System.Runtime.InteropServices", "ClassInterfaceAttribute", signaturesOfClassInterfaceAttribute);
        internal static readonly AttributeDescription ComSourceInterfacesAttribute = new AttributeDescription("System.Runtime.InteropServices", "ComSourceInterfacesAttribute", signaturesOfComSourceInterfacesAttribute);
        internal static readonly AttributeDescription ComVisibleAttribute = new AttributeDescription("System.Runtime.InteropServices", "ComVisibleAttribute", signaturesOfComVisibleAttribute);
        internal static readonly AttributeDescription DispIdAttribute = new AttributeDescription("System.Runtime.InteropServices", "DispIdAttribute", new byte[][] { Signature_HasThis_Void_Int32 });
        internal static readonly AttributeDescription TypeLibVersionAttribute = new AttributeDescription("System.Runtime.InteropServices", "TypeLibVersionAttribute", signaturesOfTypeLibVersionAttribute);
        internal static readonly AttributeDescription ComCompatibleVersionAttribute = new AttributeDescription("System.Runtime.InteropServices", "ComCompatibleVersionAttribute", signaturesOfComCompatibleVersionAttribute);
        internal static readonly AttributeDescription InterfaceTypeAttribute = new AttributeDescription("System.Runtime.InteropServices", "InterfaceTypeAttribute", signaturesOfInterfaceTypeAttribute);
        internal static readonly AttributeDescription WindowsRuntimeImportAttribute = new AttributeDescription("System.Runtime.InteropServices.WindowsRuntime", "WindowsRuntimeImportAttribute", signaturesOfWindowsRuntimeImportAttribute);
        internal static readonly AttributeDescription DynamicSecurityMethodAttribute = new AttributeDescription("System.Security", "DynamicSecurityMethodAttribute", signaturesOfDynamicSecurityMethodAttribute);
        internal static readonly AttributeDescription RequiredAttributeAttribute = new AttributeDescription("System.Runtime.CompilerServices", "RequiredAttributeAttribute", signaturesOfRequiredAttributeAttribute);
        internal static readonly AttributeDescription CompilationRelaxationsAttribute = new AttributeDescription("System.Runtime.CompilerServices", "CompilationRelaxationsAttribute", signaturesOfCompilationRelaxationsAttribute);
        internal static readonly AttributeDescription RuntimeCompatibilityAttribute = new AttributeDescription("System.Runtime.CompilerServices", "RuntimeCompatibilityAttribute", signaturesOfRuntimeCompatibilityAttribute);
        internal static readonly AttributeDescription DebuggableAttribute = new AttributeDescription("System.Diagnostics", "DebuggableAttribute", signaturesOfDebuggableAttribute);
        internal static readonly AttributeDescription TypeForwardedToAttribute = new AttributeDescription("System.Runtime.CompilerServices", "TypeForwardedToAttribute", signaturesOfTypeForwardedToAttribute);
        internal static readonly AttributeDescription STAThreadAttribute = new AttributeDescription("System", "STAThreadAttribute", signaturesOfSTAThreadAttribute);
        internal static readonly AttributeDescription MTAThreadAttribute = new AttributeDescription("System", "MTAThreadAttribute", signaturesOfMTAThreadAttribute);
        internal static readonly AttributeDescription ObsoleteAttribute = new AttributeDescription("System", "ObsoleteAttribute", signaturesOfObsoleteAttribute);
        internal static readonly AttributeDescription TypeLibTypeAttribute = new AttributeDescription("System.Runtime.InteropServices", "TypeLibTypeAttribute", signaturesOfTypeLibTypeAttribute);
        internal static readonly AttributeDescription DynamicAttribute = new AttributeDescription("System.Runtime.CompilerServices", "DynamicAttribute", signaturesOfDynamicAttribute);
        internal static readonly AttributeDescription DebuggerHiddenAttribute = new AttributeDescription("System.Diagnostics", "DebuggerHiddenAttribute", signaturesOfDebuggerHiddenAttribute);
        internal static readonly AttributeDescription SecurityCriticalAttribute = new AttributeDescription("System.Security", "SecurityCriticalAttribute", signaturesOfSecurityCriticalAttribute);
        internal static readonly AttributeDescription SecuritySafeCriticalAttribute = new AttributeDescription("System.Security", "SecuritySafeCriticalAttribute", signaturesOfSecuritySafeCriticalAttribute);
        internal static readonly AttributeDescription DesignerGeneratedAttribute = new AttributeDescription("Microsoft.VisualBasic.CompilerServices", "DesignerGeneratedAttribute", signaturesOfDesignerGeneratedAttribute);
        internal static readonly AttributeDescription MyGroupCollectionAttribute = new AttributeDescription("Microsoft.VisualBasic", "MyGroupCollectionAttribute", signaturesOfMyGroupCollectionAttribute);
        internal static readonly AttributeDescription ComEventInterfaceAttribute = new AttributeDescription("System.Runtime.InteropServices", "ComEventInterfaceAttribute", signaturesOfComEventInterfaceAttribute);
        internal static readonly AttributeDescription BestFitMappingAttribute = new AttributeDescription("System.Runtime.InteropServices", "BestFitMappingAttribute", signaturesOfBestFitMappingAttribute);
        internal static readonly AttributeDescription FlagsAttribute = new AttributeDescription("System", "FlagsAttribute", signaturesOfFlagsAttribute);
        internal static readonly AttributeDescription LCIDConversionAttribute = new AttributeDescription("System.Runtime.InteropServices", "LCIDConversionAttribute", signaturesOfLCIDConversionAttribute);
        internal static readonly AttributeDescription UnmanagedFunctionPointerAttribute = new AttributeDescription("System.Runtime.InteropServices", "UnmanagedFunctionPointerAttribute", signaturesOfUnmanagedFunctionPointerAttribute);
        internal static readonly AttributeDescription PrimaryInteropAssemblyAttribute = new AttributeDescription("System.Runtime.InteropServices", "PrimaryInteropAssemblyAttribute", signaturesOfPrimaryInteropAssemblyAttribute);
        internal static readonly AttributeDescription ImportedFromTypeLibAttribute = new AttributeDescription("System.Runtime.InteropServices", "ImportedFromTypeLibAttribute", signaturesOfImportedFromTypeLibAttribute);
        internal static readonly AttributeDescription DefaultEventAttribute = new AttributeDescription("System.ComponentModel", "DefaultEventAttribute", signaturesOfDefaultEventAttribute);
        internal static readonly AttributeDescription AssemblyConfigurationAttribute = new AttributeDescription("System.Reflection", "AssemblyConfigurationAttribute", signaturesOfAssemblyConfigurationAttribute);
        internal static readonly AttributeDescription AssemblyAlgorithmIdAttribute = new AttributeDescription("System.Reflection", "AssemblyAlgorithmIdAttribute", signaturesOfAssemblyAlgorithmIdAttribute);
        internal static readonly AttributeDescription DeprecatedAttribute = new AttributeDescription("Windows.Foundation.Metadata", "DeprecatedAttribute", signaturesOfDeprecatedAttribute);
    }
}
