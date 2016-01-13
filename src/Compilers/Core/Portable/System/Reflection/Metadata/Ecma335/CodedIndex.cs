// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public static uint ToCodedIndex(this int rowId, HasCustomAttribute tag) => ((uint)rowId << (int)HasCustomAttribute.__bits) | (uint)tag;
        public static uint ToCodedIndex(this int rowId, HasConstant tag) => ((uint)rowId << (int)HasConstant.__bits) | (uint)tag;
        public static uint ToCodedIndex(this int rowId, CustomAttributeType tag) => ((uint)rowId << (int)CustomAttributeType.__bits) | (uint)tag;
        public static uint ToCodedIndex(this int rowId, HasDeclSecurity tag) => ((uint)rowId << (int)HasDeclSecurity.__bits) | (uint)tag;
        public static uint ToCodedIndex(this int rowId, FieldMarshal tag) => ((uint)rowId << (int)FieldMarshal.__bits) | (uint)tag;
        public static uint ToCodedIndex(this int rowId, HasSemantics tag) => ((uint)rowId << (int)HasSemantics.__bits) | (uint)tag;
        public static uint ToCodedIndex(this int rowId, Implementation tag) => ((uint)rowId << (int)Implementation.__bits) | (uint)tag;
        public static uint ToCodedIndex(this int rowId, MemberForwarded tag) => ((uint)rowId << (int)MemberForwarded.__bits) | (uint)tag;
        public static uint ToCodedIndex(this int rowId, MemberRefParent tag) => ((uint)rowId << (int)MemberRefParent.__bits) | (uint)tag;
        public static uint ToCodedIndex(this int rowId, MethodDefOrRef tag) => ((uint)rowId << (int)MethodDefOrRef.__bits) | (uint)tag;
        public static uint ToCodedIndex(this int rowId, ResolutionScope tag) => ((uint)rowId << (int)ResolutionScope.__bits) | (uint)tag;
        public static uint ToCodedIndex(this int rowId, TypeDefOrRef tag) => ((uint)rowId << (int)TypeDefOrRef.__bits) | (uint)tag;
        public static uint ToCodedIndex(this int rowId, TypeOrMethodDef tag) => ((uint)rowId << (int)TypeOrMethodDef.__bits) | (uint)tag;

        public enum HasCustomAttribute
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

        public enum HasConstant
        {
            Field = 0,
            Param = 1,
            Property = 2,

            __bits = 2,
            __mask = (1 << __bits) - 1
        }

        public enum CustomAttributeType
        {
            MethodDef = 2,
            MemberRef = 3,

            __bits = 3
        }

        public enum HasDeclSecurity
        {
            TypeDef = 0,
            MethodDef = 1,
            Assembly = 2,

            __bits = 2,
            __mask = (1 << __bits) - 1
        }

        public enum FieldMarshal
        {
            Field = 0,
            Param = 1,

            __bits = 1,
            __mask = (1 << __bits) - 1
        }

        public enum HasSemantics
        {
            Event = 0,
            Property = 1,

            __bits = 1
        }

        public enum Implementation
        {
            File = 0,
            AssemblyRef = 1,
            ExportedType = 2,

            __bits = 2
        }

        public enum MemberForwarded
        {
            Field = 0,
            MethodDef = 1,

            __bits = 1
        }

        public enum MemberRefParent
        {
            TypeDef = 0,
            TypeRef = 1,
            ModuleRef = 2,
            MethodDef = 3,
            TypeSpec = 4,

            __bits = 3
        }

        public enum MethodDefOrRef
        {
            MethodDef = 0,
            MemberRef = 1,

            __bits = 1
        }

        public enum ResolutionScope
        {
            Module = 0,
            ModuleRef = 1,
            AssemblyRef = 2,
            TypeRef = 3,

            __bits = 2
        }

        public enum TypeDefOrRef
        {
            TypeDef = 0,
            TypeRef = 1,
            TypeSpec = 2,

            __bits = 2
        }

        public enum TypeOrMethodDef
        {
            TypeDef = 0,
            MethodDef = 1,

            __bits = 1
        }
    }
}
