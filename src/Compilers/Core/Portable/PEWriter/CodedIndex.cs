// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.Cci
{
    internal static class CodedIndexExtensions
    {
        public static uint ToCodedIndex(this int rowId, HasCustomAttributeTag tag) => ((uint)rowId << (int)HasCustomAttributeTag.__bits) | (uint)tag;
        public static uint ToCodedIndex(this int rowId, HasConstantTag tag) => ((uint)rowId << (int)HasConstantTag.__bits) | (uint)tag;
        public static uint ToCodedIndex(this int rowId, CustomAttributeTypeTag tag) => ((uint)rowId << (int)CustomAttributeTypeTag.__bits) | (uint)tag;
        public static uint ToCodedIndex(this int rowId, HasDeclSecurityTag tag) => ((uint)rowId << (int)HasDeclSecurityTag.__bits) | (uint)tag;
        public static uint ToCodedIndex(this int rowId, HasFieldMarshalTag tag) => ((uint)rowId << (int)HasFieldMarshalTag.__bits) | (uint)tag;
        public static uint ToCodedIndex(this int rowId, HasSemanticsTag tag) => ((uint)rowId << (int)HasSemanticsTag.__bits) | (uint)tag;
        public static uint ToCodedIndex(this int rowId, ImplementationTag tag) => ((uint)rowId << (int)ImplementationTag.__bits) | (uint)tag;
        public static uint ToCodedIndex(this int rowId, MemberForwardedTag tag) => ((uint)rowId << (int)MemberForwardedTag.__bits) | (uint)tag;
        public static uint ToCodedIndex(this int rowId, MemberRefParentTag tag) => ((uint)rowId << (int)MemberRefParentTag.__bits) | (uint)tag;
        public static uint ToCodedIndex(this int rowId, MethodDefOrRefTag tag) => ((uint)rowId << (int)MethodDefOrRefTag.__bits) | (uint)tag;
        public static uint ToCodedIndex(this int rowId, ResolutionScopeTag tag) => ((uint)rowId << (int)ResolutionScopeTag.__bits) | (uint)tag;
        public static uint ToCodedIndex(this int rowId, TypeDefOrRefTag tag) => ((uint)rowId << (int)TypeDefOrRefTag.__bits) | (uint)tag;
        public static uint ToCodedIndex(this int rowId, TypeOrMethodDefTag tag) => ((uint)rowId << (int)TypeOrMethodDefTag.__bits) | (uint)tag;
    }

    internal enum HasCustomAttributeTag
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

    internal enum HasConstantTag
    {
        Field = 0,
        Param = 1,
        Property = 2,

        __bits = 2,
        __mask = (1 << __bits) - 1
    }

    internal enum CustomAttributeTypeTag
    {
        MethodDef = 2,
        MemberRef = 3,

        __bits = 3
    }

    internal enum HasDeclSecurityTag
    {
        TypeDef = 0,
        MethodDef = 1,
        Assembly = 2,

        __bits = 2,
        __mask = (1 << __bits) - 1
    }

    internal enum HasFieldMarshalTag
    {
        Field = 0,
        Param = 1,

        __bits = 1,
        __mask = (1 << __bits) - 1
    }

    internal enum HasSemanticsTag
    {
        Event = 0,
        Property = 1,

        __bits = 1
    }

    internal enum ImplementationTag
    {
        File = 0,
        AssemblyRef = 1,
        ExportedType = 2,

        __bits = 2
    }

    internal enum MemberForwardedTag
    {
        Field = 0,
        MethodDef = 1,

        __bits = 1
    }

    internal enum MemberRefParentTag
    {
        TypeDef = 0,
        TypeRef = 1,
        ModuleRef = 2,
        MethodDef = 3,
        TypeSpec = 4,

        __bits = 3
    }

    internal enum MethodDefOrRefTag
    {
        MethodDef = 0,
        MemberRef = 1,

        __bits = 1
    }

    internal enum ResolutionScopeTag
    {
        Module = 0,
        ModuleRef = 1,
        AssemblyRef = 2,
        TypeRef = 3,

        __bits = 2
    }

    internal enum TypeDefOrRefTag
    {
        TypeDef = 0,
        TypeRef = 1,
        TypeSpec = 2,

        __bits = 2 
    }

    internal enum TypeOrMethodDefTag
    {
        TypeDef = 0,
        MethodDef = 1,

        __bits = 1
    }
}
