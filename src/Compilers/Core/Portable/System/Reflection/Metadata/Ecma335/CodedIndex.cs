// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

#if SRM
namespace System.Reflection.Metadata.Ecma335
#else
namespace Roslyn.Reflection.Metadata.Ecma335
#endif
{
#if SRM
    public
#endif
    static class CodedIndex
    {
        private static int ToCodedIndex(this int rowId, HasCustomAttribute tag) => (rowId << (int)HasCustomAttribute.__bits) | (int)tag;
        private static int ToCodedIndex(this int rowId, HasConstant tag) => (rowId << (int)HasConstant.__bits) | (int)tag;
        private static int ToCodedIndex(this int rowId, CustomAttributeType tag) => (rowId << (int)CustomAttributeType.__bits) | (int)tag;
        private static int ToCodedIndex(this int rowId, HasDeclSecurity tag) => (rowId << (int)HasDeclSecurity.__bits) | (int)tag;
        private static int ToCodedIndex(this int rowId, HasFieldMarshal tag) => (rowId << (int)HasFieldMarshal.__bits) | (int)tag;
        private static int ToCodedIndex(this int rowId, HasSemantics tag) => (rowId << (int)HasSemantics.__bits) | (int)tag;
        private static int ToCodedIndex(this int rowId, Implementation tag) => (rowId << (int)Implementation.__bits) | (int)tag;
        private static int ToCodedIndex(this int rowId, MemberForwarded tag) => (rowId << (int)MemberForwarded.__bits) | (int)tag;
        private static int ToCodedIndex(this int rowId, MemberRefParent tag) => (rowId << (int)MemberRefParent.__bits) | (int)tag;
        private static int ToCodedIndex(this int rowId, MethodDefOrRef tag) => (rowId << (int)MethodDefOrRef.__bits) | (int)tag;
        private static int ToCodedIndex(this int rowId, ResolutionScope tag) => (rowId << (int)ResolutionScope.__bits) | (int)tag;
        private static int ToCodedIndex(this int rowId, TypeDefOrRefOrSpec tag) => (rowId << (int)TypeDefOrRefOrSpec.__bits) | (int)tag;
        private static int ToCodedIndex(this int rowId, TypeOrMethodDef tag) => (rowId << (int)TypeOrMethodDef.__bits) | (int)tag;
        private static int ToCodedIndex(this int rowId, HasCustomDebugInformation tag) => (rowId << (int)HasCustomDebugInformation.__bits) | (int)tag;

        public static int ToHasCustomAttribute(EntityHandle handle) => MetadataTokens.GetRowNumber(handle).ToCodedIndex(ToHasCustomAttributeTag(handle.Kind));
        public static int ToHasConstant(EntityHandle handle) => MetadataTokens.GetRowNumber(handle).ToCodedIndex(ToHasConstantTag(handle.Kind));
        public static int ToCustomAttributeType(EntityHandle handle) => MetadataTokens.GetRowNumber(handle).ToCodedIndex(ToCustomAttributeTypeTag(handle.Kind));
        public static int ToHasDeclSecurity(EntityHandle handle) => MetadataTokens.GetRowNumber(handle).ToCodedIndex(ToHasDeclSecurityTag(handle.Kind));
        public static int ToHasFieldMarshal(EntityHandle handle) => MetadataTokens.GetRowNumber(handle).ToCodedIndex(ToHasFieldMarshalTag(handle.Kind));
        public static int ToHasSemantics(EntityHandle handle) => MetadataTokens.GetRowNumber(handle).ToCodedIndex(ToHasSemanticsTag(handle.Kind));
        public static int ToImplementation(EntityHandle handle) => MetadataTokens.GetRowNumber(handle).ToCodedIndex(ToImplementationTag(handle.Kind));
        public static int ToMemberForwarded(EntityHandle handle) => MetadataTokens.GetRowNumber(handle).ToCodedIndex(ToMemberForwardedTag(handle.Kind));
        public static int ToMemberRefParent(EntityHandle handle) => MetadataTokens.GetRowNumber(handle).ToCodedIndex(ToMemberRefParentTag(handle.Kind));
        public static int ToMethodDefOrRef(EntityHandle handle) => MetadataTokens.GetRowNumber(handle).ToCodedIndex(ToMethodDefOrRefTag(handle.Kind));
        public static int ToResolutionScope(EntityHandle handle) => MetadataTokens.GetRowNumber(handle).ToCodedIndex(ToResolutionScopeTag(handle.Kind));
        public static int ToTypeDefOrRefOrSpec(EntityHandle handle) => MetadataTokens.GetRowNumber(handle).ToCodedIndex(ToTypeDefOrRefOrSpecTag(handle.Kind));
        public static int ToTypeOrMethodDef(EntityHandle handle) => MetadataTokens.GetRowNumber(handle).ToCodedIndex(ToTypeOrMethodDefTag(handle.Kind));
        public static int ToHasCustomDebugInformation(EntityHandle handle) => MetadataTokens.GetRowNumber(handle).ToCodedIndex(ToHasCustomDebugInformationTag(handle.Kind));

        private enum HasCustomAttribute
        {
            MethodDef = 0,
            Field = 1,
            TypeRef = 2,
            TypeDef = 3,
            Param = 4,
            InterfaceImpl = 5,
            MemberRef = 6,
            Module = 7,
            DeclSecurity = 8,
            Property = 9,
            Event = 10,
            StandAloneSig = 11,
            ModuleRef = 12,
            TypeSpec = 13,
            Assembly = 14,
            AssemblyRef = 15,
            File = 16,
            ExportedType = 17,
            ManifestResource = 18,
            GenericParam = 19,
            GenericParamConstraint = 20,
            MethodSpec = 21,

            __bits = 5
        }

        private static HasCustomAttribute ToHasCustomAttributeTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.MethodDefinition: return HasCustomAttribute.MethodDef;
                case HandleKind.FieldDefinition: return HasCustomAttribute.Field;
                case HandleKind.TypeReference: return HasCustomAttribute.TypeRef;
                case HandleKind.TypeDefinition: return HasCustomAttribute.TypeDef;
                case HandleKind.Parameter: return HasCustomAttribute.Param;
                case HandleKind.InterfaceImplementation: return HasCustomAttribute.InterfaceImpl;
                case HandleKind.MemberReference: return HasCustomAttribute.MemberRef;
                case HandleKind.ModuleDefinition: return HasCustomAttribute.Module;
                case HandleKind.DeclarativeSecurityAttribute: return HasCustomAttribute.DeclSecurity;
                case HandleKind.PropertyDefinition: return HasCustomAttribute.Property;
                case HandleKind.EventDefinition: return HasCustomAttribute.Event;
                case HandleKind.StandaloneSignature: return HasCustomAttribute.StandAloneSig;
                case HandleKind.ModuleReference: return HasCustomAttribute.ModuleRef;
                case HandleKind.TypeSpecification: return HasCustomAttribute.TypeSpec;
                case HandleKind.AssemblyDefinition: return HasCustomAttribute.Assembly;
                case HandleKind.AssemblyReference: return HasCustomAttribute.AssemblyRef;
                case HandleKind.AssemblyFile: return HasCustomAttribute.File;
                case HandleKind.ExportedType: return HasCustomAttribute.ExportedType;
                case HandleKind.ManifestResource: return HasCustomAttribute.ManifestResource;
                case HandleKind.GenericParameter: return HasCustomAttribute.GenericParam;
                case HandleKind.GenericParameterConstraint: return HasCustomAttribute.GenericParamConstraint;
                case HandleKind.MethodSpecification: return HasCustomAttribute.MethodSpec;

                default:
                    throw new ArgumentException($"Unexpected kind of handle: {kind}");
            }
        }

        private enum HasConstant
        {
            Field = 0,
            Param = 1,
            Property = 2,

            __bits = 2,
            __mask = (1 << __bits) - 1
        }

        private static HasConstant ToHasConstantTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.FieldDefinition: return HasConstant.Field;
                case HandleKind.Parameter: return HasConstant.Param;
                case HandleKind.PropertyDefinition: return HasConstant.Property;

                default:
                    throw new ArgumentException($"Unexpected kind of handle: {kind}");
            }
        }

        private enum CustomAttributeType
        {
            MethodDef = 2,
            MemberRef = 3,

            __bits = 3
        }

        private static CustomAttributeType ToCustomAttributeTypeTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.MethodDefinition: return CustomAttributeType.MethodDef;
                case HandleKind.MemberReference: return CustomAttributeType.MemberRef;

                default:
                    throw new ArgumentException($"Unexpected kind of handle: {kind}");
            }
        }

        private enum HasDeclSecurity
        {
            TypeDef = 0,
            MethodDef = 1,
            Assembly = 2,

            __bits = 2,
            __mask = (1 << __bits) - 1
        }

        private static HasDeclSecurity ToHasDeclSecurityTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.TypeDefinition: return HasDeclSecurity.TypeDef;
                case HandleKind.MethodDefinition: return HasDeclSecurity.MethodDef;
                case HandleKind.AssemblyDefinition: return HasDeclSecurity.Assembly;

                default:
                    throw new ArgumentException($"Unexpected kind of handle: {kind}");
            }
        }

        private enum HasFieldMarshal
        {
            Field = 0,
            Param = 1,

            __bits = 1,
            __mask = (1 << __bits) - 1
        }

        private static HasFieldMarshal ToHasFieldMarshalTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.FieldDefinition: return HasFieldMarshal.Field;
                case HandleKind.Parameter: return HasFieldMarshal.Param;

                default:
                    throw new ArgumentException($"Unexpected kind of handle: {kind}");
            }
        }

        private enum HasSemantics
        {
            Event = 0,
            Property = 1,

            __bits = 1
        }

        private static HasSemantics ToHasSemanticsTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.EventDefinition: return HasSemantics.Event;
                case HandleKind.PropertyDefinition: return HasSemantics.Property;

                default:
                    throw new ArgumentException($"Unexpected kind of handle: {kind}");
            }
        }

        private enum Implementation
        {
            File = 0,
            AssemblyRef = 1,
            ExportedType = 2,

            __bits = 2
        }

        private static Implementation ToImplementationTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.AssemblyFile: return Implementation.File;
                case HandleKind.AssemblyReference: return Implementation.AssemblyRef;
                case HandleKind.ExportedType: return Implementation.ExportedType;

                default:
                    throw new ArgumentException($"Unexpected kind of handle: {kind}");
            }
        }

        private enum MemberForwarded
        {
            Field = 0,
            MethodDef = 1,

            __bits = 1
        }

        private static MemberForwarded ToMemberForwardedTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.FieldDefinition: return MemberForwarded.Field;
                case HandleKind.MethodDefinition: return MemberForwarded.MethodDef;

                default:
                    throw new ArgumentException($"Unexpected kind of handle: {kind}");
            }
        }

        private enum MemberRefParent
        {
            TypeDef = 0,
            TypeRef = 1,
            ModuleRef = 2,
            MethodDef = 3,
            TypeSpec = 4,

            __bits = 3
        }

        private static MemberRefParent ToMemberRefParentTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.TypeDefinition: return MemberRefParent.TypeDef;
                case HandleKind.TypeReference: return MemberRefParent.TypeRef;
                case HandleKind.ModuleReference: return MemberRefParent.ModuleRef;
                case HandleKind.MethodDefinition: return MemberRefParent.MethodDef;
                case HandleKind.TypeSpecification: return MemberRefParent.TypeSpec;

                default:
                    throw new ArgumentException($"Unexpected kind of handle: {kind}");
            }
        }

        private enum MethodDefOrRef
        {
            MethodDef = 0,
            MemberRef = 1,

            __bits = 1
        }

        private static MethodDefOrRef ToMethodDefOrRefTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.MethodDefinition: return MethodDefOrRef.MethodDef;
                case HandleKind.MemberReference: return MethodDefOrRef.MemberRef;

                default:
                    throw new ArgumentException($"Unexpected kind of handle: {kind}");
            }
        }

        private enum ResolutionScope
        {
            Module = 0,
            ModuleRef = 1,
            AssemblyRef = 2,
            TypeRef = 3,

            __bits = 2
        }

        private static ResolutionScope ToResolutionScopeTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.ModuleDefinition: return ResolutionScope.Module;
                case HandleKind.ModuleReference: return ResolutionScope.ModuleRef;
                case HandleKind.AssemblyReference: return ResolutionScope.AssemblyRef;
                case HandleKind.TypeReference: return ResolutionScope.TypeRef;

                default:
                    throw new ArgumentException($"Unexpected kind of handle: {kind}");
            }
        }

        private enum TypeDefOrRefOrSpec
        {
            TypeDef = 0,
            TypeRef = 1,
            TypeSpec = 2,

            __bits = 2
        }

        private static TypeDefOrRefOrSpec ToTypeDefOrRefOrSpecTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.TypeDefinition: return TypeDefOrRefOrSpec.TypeDef;
                case HandleKind.TypeReference: return TypeDefOrRefOrSpec.TypeRef;
                case HandleKind.TypeSpecification: return TypeDefOrRefOrSpec.TypeSpec;

                default:
                    throw new ArgumentException($"Unexpected kind of handle: {kind}");
            }
        }

        private enum TypeOrMethodDef
        {
            TypeDef = 0,
            MethodDef = 1,

            __bits = 1
        }

        private static TypeOrMethodDef ToTypeOrMethodDefTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.TypeDefinition: return TypeOrMethodDef.TypeDef;
                case HandleKind.MethodDefinition: return TypeOrMethodDef.MethodDef;

                default:
                    throw new ArgumentException($"Unexpected kind of handle: {kind}");
            }
        }

        private enum HasCustomDebugInformation
        {
            MethodDef = 0,
            Field = 1,
            TypeRef = 2,
            TypeDef = 3,
            Param = 4,
            InterfaceImpl = 5,
            MemberRef = 6,
            Module = 7,
            DeclSecurity = 8,
            Property = 9,
            Event = 10,
            StandAloneSig = 11,
            ModuleRef = 12,
            TypeSpec = 13,
            Assembly = 14,
            AssemblyRef = 15,
            File = 16,
            ExportedType = 17,
            ManifestResource = 18,
            GenericParam = 19,
            GenericParamConstraint = 20,
            MethodSpec = 21,
            Document = 22,
            LocalScope = 23,
            LocalVariable = 24,
            LocalConstant = 25,
            ImportScope = 26,

            __bits = 5
        }

        private static HasCustomDebugInformation ToHasCustomDebugInformationTag(HandleKind kind)
        {
            switch (kind)
            {
                case HandleKind.MethodDefinition: return HasCustomDebugInformation.MethodDef;
                case HandleKind.FieldDefinition: return HasCustomDebugInformation.Field;
                case HandleKind.TypeReference: return HasCustomDebugInformation.TypeRef;
                case HandleKind.TypeDefinition: return HasCustomDebugInformation.TypeDef;
                case HandleKind.Parameter: return HasCustomDebugInformation.Param;
                case HandleKind.InterfaceImplementation: return HasCustomDebugInformation.InterfaceImpl;
                case HandleKind.MemberReference: return HasCustomDebugInformation.MemberRef;
                case HandleKind.ModuleDefinition: return HasCustomDebugInformation.Module;
                case HandleKind.DeclarativeSecurityAttribute: return HasCustomDebugInformation.DeclSecurity;
                case HandleKind.PropertyDefinition: return HasCustomDebugInformation.Property;
                case HandleKind.EventDefinition: return HasCustomDebugInformation.Event;
                case HandleKind.StandaloneSignature: return HasCustomDebugInformation.StandAloneSig;
                case HandleKind.ModuleReference: return HasCustomDebugInformation.ModuleRef;
                case HandleKind.TypeSpecification: return HasCustomDebugInformation.TypeSpec;
                case HandleKind.AssemblyDefinition: return HasCustomDebugInformation.Assembly;
                case HandleKind.AssemblyReference: return HasCustomDebugInformation.AssemblyRef;
                case HandleKind.AssemblyFile: return HasCustomDebugInformation.File;
                case HandleKind.ExportedType: return HasCustomDebugInformation.ExportedType;
                case HandleKind.ManifestResource: return HasCustomDebugInformation.ManifestResource;
                case HandleKind.GenericParameter: return HasCustomDebugInformation.GenericParam;
                case HandleKind.GenericParameterConstraint: return HasCustomDebugInformation.GenericParamConstraint;
                case HandleKind.MethodSpecification: return HasCustomDebugInformation.MethodSpec;
                case HandleKind.Document: return HasCustomDebugInformation.Document;
                case HandleKind.LocalScope: return HasCustomDebugInformation.LocalScope;
                case (HandleKind)0x33: return HasCustomDebugInformation.LocalVariable; // TODO
                case HandleKind.LocalConstant: return HasCustomDebugInformation.LocalConstant;
                case HandleKind.ImportScope: return HasCustomDebugInformation.ImportScope;
                    
                default:
                    throw new ArgumentException($"Unexpected kind of handle: {kind}");
            }
        }
    }
}
