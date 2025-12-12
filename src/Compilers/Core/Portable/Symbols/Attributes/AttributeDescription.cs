// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection.Metadata;
using System.Collections.Immutable;
using Roslyn.Utilities;

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
            RoslynDebug.Assert(@namespace != null);
            RoslynDebug.Assert(name != null);
            RoslynDebug.Assert(signatures != null);

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
            Debug.Assert(signature[0] == (byte)SignatureAttributes.Instance);

            // parameter count is the second element of the signature:
            return signature[1];
        }

        // shortcuts for signature elements supported by our signature comparer:
        private const byte Void = (byte)SignatureTypeCode.Void;
        private const byte Boolean = (byte)SignatureTypeCode.Boolean;
        private const byte Byte = (byte)SignatureTypeCode.Byte;
        private const byte Int16 = (byte)SignatureTypeCode.Int16;
        private const byte Int32 = (byte)SignatureTypeCode.Int32;
        private const byte UInt32 = (byte)SignatureTypeCode.UInt32;
        private const byte Int64 = (byte)SignatureTypeCode.Int64;
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

        internal readonly struct TypeHandleTargetInfo
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

        private static readonly byte[] s_signature_HasThis_Void = new byte[] { (byte)SignatureAttributes.Instance, 0, Void };
        private static readonly byte[] s_signature_HasThis_Void_Byte = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, Byte };
        private static readonly byte[] s_signature_HasThis_Void_Int16 = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, Int16 };
        private static readonly byte[] s_signature_HasThis_Void_Int32 = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, Int32 };
        private static readonly byte[] s_signature_HasThis_Void_UInt32 = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, UInt32 };
        private static readonly byte[] s_signature_HasThis_Void_Int32_Int32 = new byte[] { (byte)SignatureAttributes.Instance, 2, Void, Int32, Int32 };
        private static readonly byte[] s_signature_HasThis_Void_Int32_Int32_Int32_Int32 = new byte[] { (byte)SignatureAttributes.Instance, 4, Void, Int32, Int32, Int32, Int32 };
        private static readonly byte[] s_signature_HasThis_Void_String = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, String };
        private static readonly byte[] s_signature_HasThis_Void_Object = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, Object };
        private static readonly byte[] s_signature_HasThis_Void_String_String = new byte[] { (byte)SignatureAttributes.Instance, 2, Void, String, String };
        private static readonly byte[] s_signature_HasThis_Void_String_Boolean = new byte[] { (byte)SignatureAttributes.Instance, 2, Void, String, Boolean };
        private static readonly byte[] s_signature_HasThis_Void_String_String_String = new byte[] { (byte)SignatureAttributes.Instance, 3, Void, String, String, String };
        private static readonly byte[] s_signature_HasThis_Void_String_String_String_String = new byte[] { (byte)SignatureAttributes.Instance, 4, Void, String, String, String, String };
        private static readonly byte[] s_signature_HasThis_Void_AttributeTargets = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, TypeHandle, (byte)TypeHandleTarget.AttributeTargets };
        private static readonly byte[] s_signature_HasThis_Void_AssemblyNameFlags = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, TypeHandle, (byte)TypeHandleTarget.AssemblyNameFlags };
        private static readonly byte[] s_signature_HasThis_Void_MethodImplOptions = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, TypeHandle, (byte)TypeHandleTarget.MethodImplOptions };
        private static readonly byte[] s_signature_HasThis_Void_CharSet = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, TypeHandle, (byte)TypeHandleTarget.CharSet };
        private static readonly byte[] s_signature_HasThis_Void_LayoutKind = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, TypeHandle, (byte)TypeHandleTarget.LayoutKind };
        private static readonly byte[] s_signature_HasThis_Void_UnmanagedType = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, TypeHandle, (byte)TypeHandleTarget.UnmanagedType };
        private static readonly byte[] s_signature_HasThis_Void_TypeLibTypeFlags = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, TypeHandle, (byte)TypeHandleTarget.TypeLibTypeFlags };
        private static readonly byte[] s_signature_HasThis_Void_ClassInterfaceType = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, TypeHandle, (byte)TypeHandleTarget.ClassInterfaceType };
        private static readonly byte[] s_signature_HasThis_Void_ComInterfaceType = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, TypeHandle, (byte)TypeHandleTarget.ComInterfaceType };
        private static readonly byte[] s_signature_HasThis_Void_CompilationRelaxations = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, TypeHandle, (byte)TypeHandleTarget.CompilationRelaxations };
        private static readonly byte[] s_signature_HasThis_Void_DebuggingModes = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, TypeHandle, (byte)TypeHandleTarget.DebuggingModes };
        private static readonly byte[] s_signature_HasThis_Void_SecurityCriticalScope = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, TypeHandle, (byte)TypeHandleTarget.SecurityCriticalScope };
        private static readonly byte[] s_signature_HasThis_Void_CallingConvention = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, TypeHandle, (byte)TypeHandleTarget.CallingConvention };
        private static readonly byte[] s_signature_HasThis_Void_AssemblyHashAlgorithm = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, TypeHandle, (byte)TypeHandleTarget.AssemblyHashAlgorithm };
        private static readonly byte[] s_signature_HasThis_Void_Int64 = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, Int64 };
        private static readonly byte[] s_signature_HasThis_Void_UInt8_UInt8_UInt32_UInt32_UInt32 = new byte[] {
            (byte)SignatureAttributes.Instance, 5, Void, Byte, Byte, UInt32, UInt32, UInt32 };
        private static readonly byte[] s_signature_HasThis_Void_UIn8_UInt8_Int32_Int32_Int32 = new byte[] {
            (byte)SignatureAttributes.Instance, 5, Void, Byte, Byte, Int32, Int32, Int32 };

        private static readonly byte[] s_signature_HasThis_Void_Boolean = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, Boolean };
        private static readonly byte[] s_signature_HasThis_Void_Boolean_Boolean = new byte[] { (byte)SignatureAttributes.Instance, 2, Void, Boolean, Boolean };

        private static readonly byte[] s_signature_HasThis_Void_Boolean_TransactionOption = new byte[] { (byte)SignatureAttributes.Instance, 2, Void, Boolean, TypeHandle, (byte)TypeHandleTarget.TransactionOption };
        private static readonly byte[] s_signature_HasThis_Void_Boolean_TransactionOption_Int32 = new byte[] { (byte)SignatureAttributes.Instance, 3, Void, Boolean, TypeHandle, (byte)TypeHandleTarget.TransactionOption, Int32 };
        private static readonly byte[] s_signature_HasThis_Void_Boolean_TransactionOption_Int32_Boolean = new byte[] { (byte)SignatureAttributes.Instance, 4, Void, Boolean, TypeHandle, (byte)TypeHandleTarget.TransactionOption, Int32, Boolean };

        private static readonly byte[] s_signature_HasThis_Void_SecurityAction = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, TypeHandle, (byte)TypeHandleTarget.SecurityAction };
        private static readonly byte[] s_signature_HasThis_Void_Type = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, TypeHandle, (byte)TypeHandleTarget.SystemType };
        private static readonly byte[] s_signature_HasThis_Void_Type_Type = new byte[] { (byte)SignatureAttributes.Instance, 2, Void, TypeHandle, (byte)TypeHandleTarget.SystemType, TypeHandle, (byte)TypeHandleTarget.SystemType };
        private static readonly byte[] s_signature_HasThis_Void_Type_Type_Type = new byte[] { (byte)SignatureAttributes.Instance, 3, Void, TypeHandle, (byte)TypeHandleTarget.SystemType, TypeHandle, (byte)TypeHandleTarget.SystemType, TypeHandle, (byte)TypeHandleTarget.SystemType };
        private static readonly byte[] s_signature_HasThis_Void_Type_Type_Type_Type = new byte[] { (byte)SignatureAttributes.Instance, 4, Void, TypeHandle, (byte)TypeHandleTarget.SystemType, TypeHandle, (byte)TypeHandleTarget.SystemType, TypeHandle, (byte)TypeHandleTarget.SystemType, TypeHandle, (byte)TypeHandleTarget.SystemType };
        private static readonly byte[] s_signature_HasThis_Void_Type_Int32 = new byte[] { (byte)SignatureAttributes.Instance, 2, Void, TypeHandle, (byte)TypeHandleTarget.SystemType, Int32 };
        private static readonly byte[] s_signature_HasThis_Void_Type_String = new byte[] { (byte)SignatureAttributes.Instance, 2, Void, TypeHandle, (byte)TypeHandleTarget.SystemType, String };

        private static readonly byte[] s_signature_HasThis_Void_String_Int32_Int32 = new byte[] { (byte)SignatureAttributes.Instance, 3, Void, String, Int32, Int32 };
        private static readonly byte[] s_signature_HasThis_Void_Int32_String = new byte[] { (byte)SignatureAttributes.Instance, 2, Void, Int32, String };

        private static readonly byte[] s_signature_HasThis_Void_SzArray_Boolean = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, SzArray, Boolean };
        private static readonly byte[] s_signature_HasThis_Void_SzArray_Byte = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, SzArray, Byte };
        private static readonly byte[] s_signature_HasThis_Void_SzArray_String = new byte[] { (byte)SignatureAttributes.Instance, 1, Void, SzArray, String };
        private static readonly byte[] s_signature_HasThis_Void_Boolean_SzArray_String = new byte[] { (byte)SignatureAttributes.Instance, 2, Void, Boolean, SzArray, String };
        private static readonly byte[] s_signature_HasThis_Void_Boolean_String = new byte[] { (byte)SignatureAttributes.Instance, 2, Void, Boolean, String };

        private static readonly byte[] s_signature_HasThis_Void_String_DeprecationType_UInt32 = new byte[] { (byte)SignatureAttributes.Instance, 3, Void, String, TypeHandle, (byte)TypeHandleTarget.DeprecationType, UInt32 };
        private static readonly byte[] s_signature_HasThis_Void_String_DeprecationType_UInt32_Platform = new byte[] { (byte)SignatureAttributes.Instance, 4, Void, String, TypeHandle, (byte)TypeHandleTarget.DeprecationType, UInt32, TypeHandle, (byte)TypeHandleTarget.Platform };
        private static readonly byte[] s_signature_HasThis_Void_String_DeprecationType_UInt32_Type = new byte[] { (byte)SignatureAttributes.Instance, 4, Void, String, TypeHandle, (byte)TypeHandleTarget.DeprecationType, UInt32, TypeHandle, (byte)TypeHandleTarget.SystemType };
        private static readonly byte[] s_signature_HasThis_Void_String_DeprecationType_UInt32_String = new byte[] { (byte)SignatureAttributes.Instance, 4, Void, String, TypeHandle, (byte)TypeHandleTarget.DeprecationType, UInt32, String };

        private static readonly byte[][] s_signatures_HasThis_Void_Only = { s_signature_HasThis_Void };
        private static readonly byte[][] s_signatures_HasThis_Void_String_Only = { s_signature_HasThis_Void_String };
        private static readonly byte[][] s_signatures_HasThis_Void_Type_Only = { s_signature_HasThis_Void_Type };
        private static readonly byte[][] s_signatures_HasThis_Void_Boolean_Only = { s_signature_HasThis_Void_Boolean };
        private static readonly byte[][] s_signatures_HasThis_Void_Int32_Only = { s_signature_HasThis_Void_Int32 };

        private static readonly byte[][] s_signaturesOfTypeIdentifierAttribute = { s_signature_HasThis_Void, s_signature_HasThis_Void_String_String };
        private static readonly byte[][] s_signaturesOfAttributeUsage = { s_signature_HasThis_Void_AttributeTargets };
        private static readonly byte[][] s_signaturesOfAssemblySignatureKeyAttribute = { s_signature_HasThis_Void_String_String };
        private static readonly byte[][] s_signaturesOfAssemblyFlagsAttribute =
        {
            s_signature_HasThis_Void_AssemblyNameFlags,
            s_signature_HasThis_Void_Int32,
            s_signature_HasThis_Void_UInt32
        };
        private static readonly byte[][] s_signaturesOfDefaultParameterValueAttribute = { s_signature_HasThis_Void_Object };
        private static readonly byte[][] s_signaturesOfDateTimeConstantAttribute = { s_signature_HasThis_Void_Int64 };
        private static readonly byte[][] s_signaturesOfDecimalConstantAttribute = { s_signature_HasThis_Void_UInt8_UInt8_UInt32_UInt32_UInt32, s_signature_HasThis_Void_UIn8_UInt8_Int32_Int32_Int32 };
        private static readonly byte[][] s_signaturesOfSecurityPermissionAttribute = { s_signature_HasThis_Void_SecurityAction };

        private static readonly byte[][] s_signaturesOfMethodImplAttribute =
        {
            s_signature_HasThis_Void,
            s_signature_HasThis_Void_Int16,
            s_signature_HasThis_Void_MethodImplOptions,
        };

        private static readonly byte[][] s_signaturesOfDefaultCharSetAttribute = { s_signature_HasThis_Void_CharSet };

        private static readonly byte[][] s_signaturesOfMemberNotNullAttribute = { s_signature_HasThis_Void_String, s_signature_HasThis_Void_SzArray_String };
        private static readonly byte[][] s_signaturesOfMemberNotNullWhenAttribute = { s_signature_HasThis_Void_Boolean_String, s_signature_HasThis_Void_Boolean_SzArray_String };
        private static readonly byte[][] s_signaturesOfFixedBufferAttribute = { s_signature_HasThis_Void_Type_Int32 };
        private static readonly byte[][] s_signaturesOfInterceptsLocationAttribute = { s_signature_HasThis_Void_String_Int32_Int32, s_signature_HasThis_Void_Int32_String };
        private static readonly byte[][] s_signaturesOfPrincipalPermissionAttribute = { s_signature_HasThis_Void_SecurityAction };
        private static readonly byte[][] s_signaturesOfPermissionSetAttribute = { s_signature_HasThis_Void_SecurityAction };

        private static readonly byte[][] s_signaturesOfStructLayoutAttribute =
        {
            s_signature_HasThis_Void_Int16,
            s_signature_HasThis_Void_LayoutKind,
        };

        private static readonly byte[][] s_signaturesOfMarshalAsAttribute =
        {
            s_signature_HasThis_Void_Int16,
            s_signature_HasThis_Void_UnmanagedType,
        };

        private static readonly byte[][] s_signaturesOfTypeLibTypeAttribute =
        {
            s_signature_HasThis_Void_Int16,
            s_signature_HasThis_Void_TypeLibTypeFlags,
        };

        private static readonly byte[][] s_signaturesOfWebMethodAttribute =
        {
            s_signature_HasThis_Void,
            s_signature_HasThis_Void_Boolean,
            s_signature_HasThis_Void_Boolean_TransactionOption,
            s_signature_HasThis_Void_Boolean_TransactionOption_Int32,
            s_signature_HasThis_Void_Boolean_TransactionOption_Int32_Boolean
        };

        private static readonly byte[][] s_signaturesOfHostProtectionAttribute =
        {
            s_signature_HasThis_Void,
            s_signature_HasThis_Void_SecurityAction
        };

        private static readonly byte[][] s_signaturesOfVisualBasicComClassAttribute =
        {
            s_signature_HasThis_Void,
            s_signature_HasThis_Void_String,
            s_signature_HasThis_Void_String_String,
            s_signature_HasThis_Void_String_String_String
        };

        private static readonly byte[][] s_signaturesOfClassInterfaceAttribute =
        {
            s_signature_HasThis_Void_Int16,
            s_signature_HasThis_Void_ClassInterfaceType
        };

        private static readonly byte[][] s_signaturesOfInterfaceTypeAttribute =
        {
            s_signature_HasThis_Void_Int16,
            s_signature_HasThis_Void_ComInterfaceType
        };

        private static readonly byte[][] s_signaturesOfCompilationRelaxationsAttribute =
        {
            s_signature_HasThis_Void_Int32,
            s_signature_HasThis_Void_CompilationRelaxations
        };

        private static readonly byte[][] s_signaturesOfDebuggableAttribute =
        {
            s_signature_HasThis_Void_Boolean_Boolean,
            s_signature_HasThis_Void_DebuggingModes
        };

        private static readonly byte[][] s_signaturesOfComSourceInterfacesAttribute =
        {
            s_signature_HasThis_Void_String,
            s_signature_HasThis_Void_Type,
            s_signature_HasThis_Void_Type_Type,
            s_signature_HasThis_Void_Type_Type_Type,
            s_signature_HasThis_Void_Type_Type_Type_Type
        };

        private static readonly byte[][] s_signaturesOfTypeLibVersionAttribute = { s_signature_HasThis_Void_Int32_Int32 };
        private static readonly byte[][] s_signaturesOfComCompatibleVersionAttribute = { s_signature_HasThis_Void_Int32_Int32_Int32_Int32 };
        private static readonly byte[][] s_signaturesOfObsoleteAttribute = { s_signature_HasThis_Void, s_signature_HasThis_Void_String, s_signature_HasThis_Void_String_Boolean };
        private static readonly byte[][] s_signaturesOfDynamicAttribute = { s_signature_HasThis_Void, s_signature_HasThis_Void_SzArray_Boolean };
        private static readonly byte[][] s_signaturesOfTupleElementNamesAttribute = { s_signature_HasThis_Void, s_signature_HasThis_Void_SzArray_String };

        private static readonly byte[][] s_signaturesOfSecurityCriticalAttribute =
        {
            s_signature_HasThis_Void,
            s_signature_HasThis_Void_SecurityCriticalScope
        };

        private static readonly byte[][] s_signaturesOfMyGroupCollectionAttribute = { s_signature_HasThis_Void_String_String_String_String };
        private static readonly byte[][] s_signaturesOfComEventInterfaceAttribute = { s_signature_HasThis_Void_Type_Type };
        private static readonly byte[][] s_signaturesOfUnmanagedFunctionPointerAttribute = { s_signature_HasThis_Void_CallingConvention };
        private static readonly byte[][] s_signaturesOfPrimaryInteropAssemblyAttribute = { s_signature_HasThis_Void_Int32_Int32 };
        private static readonly byte[][] s_signaturesOfAssemblyAlgorithmIdAttribute =
        {
            s_signature_HasThis_Void_AssemblyHashAlgorithm,
            s_signature_HasThis_Void_UInt32
        };

        private static readonly byte[][] s_signaturesOfDeprecatedAttribute =
        {
            s_signature_HasThis_Void_String_DeprecationType_UInt32,
            s_signature_HasThis_Void_String_DeprecationType_UInt32_Platform,
            s_signature_HasThis_Void_String_DeprecationType_UInt32_Type,
            s_signature_HasThis_Void_String_DeprecationType_UInt32_String,
        };

        private static readonly byte[][] s_signaturesOfNullableAttribute = { s_signature_HasThis_Void_Byte, s_signature_HasThis_Void_SzArray_Byte };
        private static readonly byte[][] s_signaturesOfNullableContextAttribute = { s_signature_HasThis_Void_Byte };
        private static readonly byte[][] s_signaturesOfNativeIntegerAttribute = { s_signature_HasThis_Void, s_signature_HasThis_Void_SzArray_Boolean };
        private static readonly byte[][] s_signaturesOfInterpolatedStringArgumentAttribute = { s_signature_HasThis_Void_String, s_signature_HasThis_Void_SzArray_String };
        private static readonly byte[][] s_signaturesOfCollectionBuilderAttribute = { s_signature_HasThis_Void_Type_String };

        // early decoded attributes:
        internal static readonly AttributeDescription OptionalAttribute = new AttributeDescription("System.Runtime.InteropServices", "OptionalAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription ComImportAttribute = new AttributeDescription("System.Runtime.InteropServices", "ComImportAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription AttributeUsageAttribute = new AttributeDescription("System", "AttributeUsageAttribute", s_signaturesOfAttributeUsage);
        internal static readonly AttributeDescription ConditionalAttribute = new AttributeDescription("System.Diagnostics", "ConditionalAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription CaseInsensitiveExtensionAttribute = new AttributeDescription("System.Runtime.CompilerServices", "ExtensionAttribute", s_signatures_HasThis_Void_Only, matchIgnoringCase: true);
        internal static readonly AttributeDescription CaseSensitiveExtensionAttribute = new AttributeDescription("System.Runtime.CompilerServices", "ExtensionAttribute", s_signatures_HasThis_Void_Only, matchIgnoringCase: false);

        internal static readonly AttributeDescription InternalsVisibleToAttribute = new AttributeDescription("System.Runtime.CompilerServices", "InternalsVisibleToAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription AssemblySignatureKeyAttribute = new AttributeDescription("System.Reflection", "AssemblySignatureKeyAttribute", s_signaturesOfAssemblySignatureKeyAttribute);
        internal static readonly AttributeDescription AssemblyKeyFileAttribute = new AttributeDescription("System.Reflection", "AssemblyKeyFileAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription AssemblyKeyNameAttribute = new AttributeDescription("System.Reflection", "AssemblyKeyNameAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription ParamArrayAttribute = new AttributeDescription("System", "ParamArrayAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription ParamCollectionAttribute = new AttributeDescription("System.Runtime.CompilerServices", "ParamCollectionAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription DefaultMemberAttribute = new AttributeDescription("System.Reflection", "DefaultMemberAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription IndexerNameAttribute = new AttributeDescription("System.Runtime.CompilerServices", "IndexerNameAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription AssemblyDelaySignAttribute = new AttributeDescription("System.Reflection", "AssemblyDelaySignAttribute", s_signatures_HasThis_Void_Boolean_Only);
        internal static readonly AttributeDescription AssemblyVersionAttribute = new AttributeDescription("System.Reflection", "AssemblyVersionAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription AssemblyFileVersionAttribute = new AttributeDescription("System.Reflection", "AssemblyFileVersionAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription AssemblyTitleAttribute = new AttributeDescription("System.Reflection", "AssemblyTitleAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription AssemblyDescriptionAttribute = new AttributeDescription("System.Reflection", "AssemblyDescriptionAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription AssemblyCultureAttribute = new AttributeDescription("System.Reflection", "AssemblyCultureAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription AssemblyCompanyAttribute = new AttributeDescription("System.Reflection", "AssemblyCompanyAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription AssemblyProductAttribute = new AttributeDescription("System.Reflection", "AssemblyProductAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription AssemblyInformationalVersionAttribute = new AttributeDescription("System.Reflection", "AssemblyInformationalVersionAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription AssemblyCopyrightAttribute = new AttributeDescription("System.Reflection", "AssemblyCopyrightAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription SatelliteContractVersionAttribute = new AttributeDescription("System.Resources", "SatelliteContractVersionAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription AssemblyTrademarkAttribute = new AttributeDescription("System.Reflection", "AssemblyTrademarkAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription AssemblyFlagsAttribute = new AttributeDescription("System.Reflection", "AssemblyFlagsAttribute", s_signaturesOfAssemblyFlagsAttribute);
        internal static readonly AttributeDescription DecimalConstantAttribute = new AttributeDescription("System.Runtime.CompilerServices", "DecimalConstantAttribute", s_signaturesOfDecimalConstantAttribute);
        internal static readonly AttributeDescription IUnknownConstantAttribute = new AttributeDescription("System.Runtime.CompilerServices", "IUnknownConstantAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription CallerFilePathAttribute = new AttributeDescription("System.Runtime.CompilerServices", "CallerFilePathAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription CallerLineNumberAttribute = new AttributeDescription("System.Runtime.CompilerServices", "CallerLineNumberAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription CallerMemberNameAttribute = new AttributeDescription("System.Runtime.CompilerServices", "CallerMemberNameAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription CallerArgumentExpressionAttribute = new AttributeDescription("System.Runtime.CompilerServices", "CallerArgumentExpressionAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription IDispatchConstantAttribute = new AttributeDescription("System.Runtime.CompilerServices", "IDispatchConstantAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription DefaultParameterValueAttribute = new AttributeDescription("System.Runtime.InteropServices", "DefaultParameterValueAttribute", s_signaturesOfDefaultParameterValueAttribute);
        internal static readonly AttributeDescription UnverifiableCodeAttribute = new AttributeDescription("System.Runtime.InteropServices", "UnverifiableCodeAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription SecurityPermissionAttribute = new AttributeDescription("System.Runtime.InteropServices", "SecurityPermissionAttribute", s_signaturesOfSecurityPermissionAttribute);
        internal static readonly AttributeDescription DllImportAttribute = new AttributeDescription("System.Runtime.InteropServices", "DllImportAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription MethodImplAttribute = new AttributeDescription("System.Runtime.CompilerServices", "MethodImplAttribute", s_signaturesOfMethodImplAttribute);
        internal static readonly AttributeDescription PreserveSigAttribute = new AttributeDescription("System.Runtime.InteropServices", "PreserveSigAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription DefaultCharSetAttribute = new AttributeDescription("System.Runtime.InteropServices", "DefaultCharSetAttribute", s_signaturesOfDefaultCharSetAttribute);
        internal static readonly AttributeDescription SpecialNameAttribute = new AttributeDescription("System.Runtime.CompilerServices", "SpecialNameAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription SerializableAttribute = new AttributeDescription("System", "SerializableAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription NonSerializedAttribute = new AttributeDescription("System", "NonSerializedAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription StructLayoutAttribute = new AttributeDescription("System.Runtime.InteropServices", "StructLayoutAttribute", s_signaturesOfStructLayoutAttribute);
        internal static readonly AttributeDescription FieldOffsetAttribute = new AttributeDescription("System.Runtime.InteropServices", "FieldOffsetAttribute", s_signatures_HasThis_Void_Int32_Only);
        internal static readonly AttributeDescription FixedBufferAttribute = new AttributeDescription("System.Runtime.CompilerServices", "FixedBufferAttribute", s_signaturesOfFixedBufferAttribute);
        internal static readonly AttributeDescription InterceptsLocationAttribute = new AttributeDescription("System.Runtime.CompilerServices", "InterceptsLocationAttribute", s_signaturesOfInterceptsLocationAttribute);
        internal static readonly AttributeDescription AllowNullAttribute = new AttributeDescription("System.Diagnostics.CodeAnalysis", "AllowNullAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription DisallowNullAttribute = new AttributeDescription("System.Diagnostics.CodeAnalysis", "DisallowNullAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription MaybeNullAttribute = new AttributeDescription("System.Diagnostics.CodeAnalysis", "MaybeNullAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription MaybeNullWhenAttribute = new AttributeDescription("System.Diagnostics.CodeAnalysis", "MaybeNullWhenAttribute", s_signatures_HasThis_Void_Boolean_Only);
        internal static readonly AttributeDescription NotNullAttribute = new AttributeDescription("System.Diagnostics.CodeAnalysis", "NotNullAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription MemberNotNullAttribute = new AttributeDescription("System.Diagnostics.CodeAnalysis", "MemberNotNullAttribute", s_signaturesOfMemberNotNullAttribute);
        internal static readonly AttributeDescription MemberNotNullWhenAttribute = new AttributeDescription("System.Diagnostics.CodeAnalysis", "MemberNotNullWhenAttribute", s_signaturesOfMemberNotNullWhenAttribute);
        internal static readonly AttributeDescription NotNullIfNotNullAttribute = new AttributeDescription("System.Diagnostics.CodeAnalysis", "NotNullIfNotNullAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription NotNullWhenAttribute = new AttributeDescription("System.Diagnostics.CodeAnalysis", "NotNullWhenAttribute", s_signatures_HasThis_Void_Boolean_Only);
        internal static readonly AttributeDescription DoesNotReturnIfAttribute = new AttributeDescription("System.Diagnostics.CodeAnalysis", "DoesNotReturnIfAttribute", s_signatures_HasThis_Void_Boolean_Only);
        internal static readonly AttributeDescription DoesNotReturnAttribute = new AttributeDescription("System.Diagnostics.CodeAnalysis", "DoesNotReturnAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription MarshalAsAttribute = new AttributeDescription("System.Runtime.InteropServices", "MarshalAsAttribute", s_signaturesOfMarshalAsAttribute);
        internal static readonly AttributeDescription InAttribute = new AttributeDescription("System.Runtime.InteropServices", "InAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription OutAttribute = new AttributeDescription("System.Runtime.InteropServices", "OutAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription IsReadOnlyAttribute = new AttributeDescription("System.Runtime.CompilerServices", "IsReadOnlyAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription RequiresLocationAttribute = new AttributeDescription("System.Runtime.CompilerServices", "RequiresLocationAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription IsUnmanagedAttribute = new AttributeDescription("System.Runtime.CompilerServices", "IsUnmanagedAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription CoClassAttribute = new AttributeDescription("System.Runtime.InteropServices", "CoClassAttribute", s_signatures_HasThis_Void_Type_Only);
        internal static readonly AttributeDescription GuidAttribute = new AttributeDescription("System.Runtime.InteropServices", "GuidAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription CLSCompliantAttribute = new AttributeDescription("System", "CLSCompliantAttribute", s_signatures_HasThis_Void_Boolean_Only);
        internal static readonly AttributeDescription HostProtectionAttribute = new AttributeDescription("System.Security.Permissions", "HostProtectionAttribute", s_signaturesOfHostProtectionAttribute);
        internal static readonly AttributeDescription SuppressUnmanagedCodeSecurityAttribute = new AttributeDescription("System.Security", "SuppressUnmanagedCodeSecurityAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription PrincipalPermissionAttribute = new AttributeDescription("System.Security.Permissions", "PrincipalPermissionAttribute", s_signaturesOfPrincipalPermissionAttribute);
        internal static readonly AttributeDescription PermissionSetAttribute = new AttributeDescription("System.Security.Permissions", "PermissionSetAttribute", s_signaturesOfPermissionSetAttribute);
        internal static readonly AttributeDescription TypeIdentifierAttribute = new AttributeDescription("System.Runtime.InteropServices", "TypeIdentifierAttribute", s_signaturesOfTypeIdentifierAttribute);
        internal static readonly AttributeDescription VisualBasicEmbeddedAttribute = new AttributeDescription("Microsoft.VisualBasic", "Embedded", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription CodeAnalysisEmbeddedAttribute = new AttributeDescription("Microsoft.CodeAnalysis", "EmbeddedAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription VisualBasicComClassAttribute = new AttributeDescription("Microsoft.VisualBasic", "ComClassAttribute", s_signaturesOfVisualBasicComClassAttribute);
        internal static readonly AttributeDescription StandardModuleAttribute = new AttributeDescription("Microsoft.VisualBasic.CompilerServices", "StandardModuleAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription OptionCompareAttribute = new AttributeDescription("Microsoft.VisualBasic.CompilerServices", "OptionCompareAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription AccessedThroughPropertyAttribute = new AttributeDescription("System.Runtime.CompilerServices", "AccessedThroughPropertyAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription WebMethodAttribute = new AttributeDescription("System.Web.Services", "WebMethodAttribute", s_signaturesOfWebMethodAttribute);
        internal static readonly AttributeDescription DateTimeConstantAttribute = new AttributeDescription("System.Runtime.CompilerServices", "DateTimeConstantAttribute", s_signaturesOfDateTimeConstantAttribute);
        internal static readonly AttributeDescription ClassInterfaceAttribute = new AttributeDescription("System.Runtime.InteropServices", "ClassInterfaceAttribute", s_signaturesOfClassInterfaceAttribute);
        internal static readonly AttributeDescription ComSourceInterfacesAttribute = new AttributeDescription("System.Runtime.InteropServices", "ComSourceInterfacesAttribute", s_signaturesOfComSourceInterfacesAttribute);
        internal static readonly AttributeDescription ComVisibleAttribute = new AttributeDescription("System.Runtime.InteropServices", "ComVisibleAttribute", s_signatures_HasThis_Void_Boolean_Only);
        internal static readonly AttributeDescription DispIdAttribute = new AttributeDescription("System.Runtime.InteropServices", "DispIdAttribute", s_signatures_HasThis_Void_Int32_Only);
        internal static readonly AttributeDescription TypeLibVersionAttribute = new AttributeDescription("System.Runtime.InteropServices", "TypeLibVersionAttribute", s_signaturesOfTypeLibVersionAttribute);
        internal static readonly AttributeDescription ComCompatibleVersionAttribute = new AttributeDescription("System.Runtime.InteropServices", "ComCompatibleVersionAttribute", s_signaturesOfComCompatibleVersionAttribute);
        internal static readonly AttributeDescription InterfaceTypeAttribute = new AttributeDescription("System.Runtime.InteropServices", "InterfaceTypeAttribute", s_signaturesOfInterfaceTypeAttribute);
        internal static readonly AttributeDescription WindowsRuntimeImportAttribute = new AttributeDescription("System.Runtime.InteropServices.WindowsRuntime", "WindowsRuntimeImportAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription DynamicSecurityMethodAttribute = new AttributeDescription("System.Security", "DynamicSecurityMethodAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription RequiredAttributeAttribute = new AttributeDescription("System.Runtime.CompilerServices", "RequiredAttributeAttribute", s_signatures_HasThis_Void_Type_Only);
        internal static readonly AttributeDescription AsyncMethodBuilderAttribute = new AttributeDescription("System.Runtime.CompilerServices", "AsyncMethodBuilderAttribute", s_signatures_HasThis_Void_Type_Only);
        internal static readonly AttributeDescription AsyncStateMachineAttribute = new AttributeDescription("System.Runtime.CompilerServices", "AsyncStateMachineAttribute", s_signatures_HasThis_Void_Type_Only);
        internal static readonly AttributeDescription IteratorStateMachineAttribute = new AttributeDescription("System.Runtime.CompilerServices", "IteratorStateMachineAttribute", s_signatures_HasThis_Void_Type_Only);
        internal static readonly AttributeDescription AsyncIteratorStateMachineAttribute = new AttributeDescription("System.Runtime.CompilerServices", "AsyncIteratorStateMachineAttribute", s_signatures_HasThis_Void_Type_Only);
        internal static readonly AttributeDescription CompilationRelaxationsAttribute = new AttributeDescription("System.Runtime.CompilerServices", "CompilationRelaxationsAttribute", s_signaturesOfCompilationRelaxationsAttribute);
        internal static readonly AttributeDescription ReferenceAssemblyAttribute = new AttributeDescription("System.Runtime.CompilerServices", "ReferenceAssemblyAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription RuntimeCompatibilityAttribute = new AttributeDescription("System.Runtime.CompilerServices", "RuntimeCompatibilityAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription DebuggableAttribute = new AttributeDescription("System.Diagnostics", "DebuggableAttribute", s_signaturesOfDebuggableAttribute);
        internal static readonly AttributeDescription TypeForwardedToAttribute = new AttributeDescription("System.Runtime.CompilerServices", "TypeForwardedToAttribute", s_signatures_HasThis_Void_Type_Only);
        internal static readonly AttributeDescription STAThreadAttribute = new AttributeDescription("System", "STAThreadAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription MTAThreadAttribute = new AttributeDescription("System", "MTAThreadAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription ObsoleteAttribute = new AttributeDescription("System", "ObsoleteAttribute", s_signaturesOfObsoleteAttribute);
        internal static readonly AttributeDescription TypeLibTypeAttribute = new AttributeDescription("System.Runtime.InteropServices", "TypeLibTypeAttribute", s_signaturesOfTypeLibTypeAttribute);
        internal static readonly AttributeDescription DynamicAttribute = new AttributeDescription("System.Runtime.CompilerServices", "DynamicAttribute", s_signaturesOfDynamicAttribute);
        internal static readonly AttributeDescription TupleElementNamesAttribute = new AttributeDescription("System.Runtime.CompilerServices", "TupleElementNamesAttribute", s_signaturesOfTupleElementNamesAttribute);
        internal static readonly AttributeDescription IsByRefLikeAttribute = new AttributeDescription("System.Runtime.CompilerServices", "IsByRefLikeAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription DebuggerHiddenAttribute = new AttributeDescription("System.Diagnostics", "DebuggerHiddenAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription DebuggerNonUserCodeAttribute = new AttributeDescription("System.Diagnostics", "DebuggerNonUserCodeAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription DebuggerStepperBoundaryAttribute = new AttributeDescription("System.Diagnostics", "DebuggerStepperBoundaryAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription DebuggerStepThroughAttribute = new AttributeDescription("System.Diagnostics", "DebuggerStepThroughAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription SecurityCriticalAttribute = new AttributeDescription("System.Security", "SecurityCriticalAttribute", s_signaturesOfSecurityCriticalAttribute);
        internal static readonly AttributeDescription SecuritySafeCriticalAttribute = new AttributeDescription("System.Security", "SecuritySafeCriticalAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription DesignerGeneratedAttribute = new AttributeDescription("Microsoft.VisualBasic.CompilerServices", "DesignerGeneratedAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription MyGroupCollectionAttribute = new AttributeDescription("Microsoft.VisualBasic", "MyGroupCollectionAttribute", s_signaturesOfMyGroupCollectionAttribute);
        internal static readonly AttributeDescription ComEventInterfaceAttribute = new AttributeDescription("System.Runtime.InteropServices", "ComEventInterfaceAttribute", s_signaturesOfComEventInterfaceAttribute);
        internal static readonly AttributeDescription BestFitMappingAttribute = new AttributeDescription("System.Runtime.InteropServices", "BestFitMappingAttribute", s_signatures_HasThis_Void_Boolean_Only);
        internal static readonly AttributeDescription FlagsAttribute = new AttributeDescription("System", "FlagsAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription LCIDConversionAttribute = new AttributeDescription("System.Runtime.InteropServices", "LCIDConversionAttribute", s_signatures_HasThis_Void_Int32_Only);
        internal static readonly AttributeDescription UnmanagedFunctionPointerAttribute = new AttributeDescription("System.Runtime.InteropServices", "UnmanagedFunctionPointerAttribute", s_signaturesOfUnmanagedFunctionPointerAttribute);
        internal static readonly AttributeDescription PrimaryInteropAssemblyAttribute = new AttributeDescription("System.Runtime.InteropServices", "PrimaryInteropAssemblyAttribute", s_signaturesOfPrimaryInteropAssemblyAttribute);
        internal static readonly AttributeDescription ImportedFromTypeLibAttribute = new AttributeDescription("System.Runtime.InteropServices", "ImportedFromTypeLibAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription DefaultEventAttribute = new AttributeDescription("System.ComponentModel", "DefaultEventAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription AssemblyConfigurationAttribute = new AttributeDescription("System.Reflection", "AssemblyConfigurationAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription AssemblyAlgorithmIdAttribute = new AttributeDescription("System.Reflection", "AssemblyAlgorithmIdAttribute", s_signaturesOfAssemblyAlgorithmIdAttribute);
        internal static readonly AttributeDescription DeprecatedAttribute = new AttributeDescription("Windows.Foundation.Metadata", "DeprecatedAttribute", s_signaturesOfDeprecatedAttribute);
        internal static readonly AttributeDescription NullableAttribute = new AttributeDescription("System.Runtime.CompilerServices", "NullableAttribute", s_signaturesOfNullableAttribute);
        internal static readonly AttributeDescription NullableContextAttribute = new AttributeDescription("System.Runtime.CompilerServices", "NullableContextAttribute", s_signaturesOfNullableContextAttribute);
        internal static readonly AttributeDescription NullablePublicOnlyAttribute = new AttributeDescription("System.Runtime.CompilerServices", "NullablePublicOnlyAttribute", s_signatures_HasThis_Void_Boolean_Only);
        internal static readonly AttributeDescription WindowsExperimentalAttribute = new AttributeDescription("Windows.Foundation.Metadata", "ExperimentalAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription ExperimentalAttribute = new AttributeDescription("System.Diagnostics.CodeAnalysis", "ExperimentalAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription ExcludeFromCodeCoverageAttribute = new AttributeDescription("System.Diagnostics.CodeAnalysis", "ExcludeFromCodeCoverageAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription EnumeratorCancellationAttribute = new AttributeDescription("System.Runtime.CompilerServices", "EnumeratorCancellationAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription SkipLocalsInitAttribute = new AttributeDescription("System.Runtime.CompilerServices", "SkipLocalsInitAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription NativeIntegerAttribute = new AttributeDescription("System.Runtime.CompilerServices", "NativeIntegerAttribute", s_signaturesOfNativeIntegerAttribute);
        internal static readonly AttributeDescription ScopedRefAttribute = new AttributeDescription("System.Runtime.CompilerServices", "ScopedRefAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription RefSafetyRulesAttribute = new AttributeDescription("System.Runtime.CompilerServices", "RefSafetyRulesAttribute", s_signatures_HasThis_Void_Int32_Only);
        internal static readonly AttributeDescription ModuleInitializerAttribute = new AttributeDescription("System.Runtime.CompilerServices", "ModuleInitializerAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription UnmanagedCallersOnlyAttribute = new AttributeDescription("System.Runtime.InteropServices", "UnmanagedCallersOnlyAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription InterpolatedStringHandlerAttribute = new AttributeDescription("System.Runtime.CompilerServices", "InterpolatedStringHandlerAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription InterpolatedStringHandlerArgumentAttribute = new AttributeDescription("System.Runtime.CompilerServices", "InterpolatedStringHandlerArgumentAttribute", s_signaturesOfInterpolatedStringArgumentAttribute);
        internal static readonly AttributeDescription RequiredMemberAttribute = new AttributeDescription("System.Runtime.CompilerServices", "RequiredMemberAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription SetsRequiredMembersAttribute = new AttributeDescription("System.Diagnostics.CodeAnalysis", "SetsRequiredMembersAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription CompilerFeatureRequiredAttribute = new AttributeDescription("System.Runtime.CompilerServices", "CompilerFeatureRequiredAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription UnscopedRefAttribute = new AttributeDescription("System.Diagnostics.CodeAnalysis", "UnscopedRefAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription InlineArrayAttribute = new AttributeDescription("System.Runtime.CompilerServices", "InlineArrayAttribute", s_signatures_HasThis_Void_Int32_Only);
        internal static readonly AttributeDescription CollectionBuilderAttribute = new AttributeDescription("System.Runtime.CompilerServices", "CollectionBuilderAttribute", s_signaturesOfCollectionBuilderAttribute);
        internal static readonly AttributeDescription OverloadResolutionPriorityAttribute = new AttributeDescription("System.Runtime.CompilerServices", "OverloadResolutionPriorityAttribute", s_signatures_HasThis_Void_Int32_Only);
        internal static readonly AttributeDescription CompilerLoweringPreserveAttribute = new AttributeDescription("System.Runtime.CompilerServices", "CompilerLoweringPreserveAttribute", s_signatures_HasThis_Void_Only);
        internal static readonly AttributeDescription ExtensionMarkerAttribute = new AttributeDescription("System.Runtime.CompilerServices", "ExtensionMarkerAttribute", s_signatures_HasThis_Void_String_Only);
        internal static readonly AttributeDescription RuntimeAsyncMethodGenerationAttribute = new AttributeDescription("System.Runtime.CompilerServices", "RuntimeAsyncMethodGenerationAttribute", s_signatures_HasThis_Void_Boolean_Only);
        internal static readonly AttributeDescription MetadataUpdateDeletedAttribute = new AttributeDescription("System.Runtime.CompilerServices", "MetadataUpdateDeletedAttribute", s_signatures_HasThis_Void_Only);
    }
}
