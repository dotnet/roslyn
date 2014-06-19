using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    internal static class HasCustomAttributeTag
    {
        internal const int NumberOfBits = 5;
        internal const uint LargeRowSize = 0x00000001 << (16 - HasCustomAttributeTag.NumberOfBits);
        internal const uint Method = 0x00000000;
        internal const uint Field = 0x00000001;
        internal const uint TypeRef = 0x00000002;
        internal const uint TypeDef = 0x00000003;
        internal const uint Param = 0x00000004;
        internal const uint InterfaceImpl = 0x00000005;
        internal const uint MemberRef = 0x00000006;
        internal const uint Module = 0x00000007;
        internal const uint DeclSecurity = 0x00000008;
        internal const uint Property = 0x00000009;
        internal const uint Event = 0x0000000A;
        internal const uint StandAloneSig = 0x0000000B;
        internal const uint ModuleRef = 0x0000000C;
        internal const uint TypeSpec = 0x0000000D;
        internal const uint Assembly = 0x0000000E;
        internal const uint AssemblyRef = 0x0000000F;
        internal const uint File = 0x00000010;
        internal const uint ExportedType = 0x00000011;
        internal const uint ManifestResource = 0x00000012;
        internal const uint GenericParameter = 0x00000013;
        internal const uint TagMask = 0x0000001F;
        internal static uint[] TagToTokenTypeArray =
        {
            TokenTypeIds.MethodDef, TokenTypeIds.FieldDef, TokenTypeIds.TypeRef, TokenTypeIds.TypeDef, TokenTypeIds.ParamDef,
          TokenTypeIds.InterfaceImpl, TokenTypeIds.MemberRef, TokenTypeIds.Module, TokenTypeIds.Permission, TokenTypeIds.Property, TokenTypeIds.Event,
          TokenTypeIds.Signature, TokenTypeIds.ModuleRef, TokenTypeIds.TypeSpec, TokenTypeIds.Assembly, TokenTypeIds.AssemblyRef, TokenTypeIds.File, TokenTypeIds.ExportedType,
          TokenTypeIds.ManifestResource, TokenTypeIds.GenericParam 
        };
        internal const TableMask TablesReferenced =
          TableMask.Method
          | TableMask.Field
          | TableMask.TypeRef
          | TableMask.TypeDef
          | TableMask.Param
          | TableMask.InterfaceImpl
          | TableMask.MemberRef
          | TableMask.Module
          | TableMask.DeclSecurity
          | TableMask.Property
          | TableMask.Event
          | TableMask.StandAloneSig
          | TableMask.ModuleRef
          | TableMask.TypeSpec
          | TableMask.Assembly
          | TableMask.AssemblyRef
          | TableMask.File
          | TableMask.ExportedType
          | TableMask.ManifestResource
          | TableMask.GenericParam;
        internal static uint ConvertToToken(
          uint hasCustomAttribute)
        {
            return HasCustomAttributeTag.TagToTokenTypeArray[hasCustomAttribute & HasCustomAttributeTag.TagMask] | hasCustomAttribute >> HasCustomAttributeTag.NumberOfBits;
        }

        internal static uint ConvertToTag(
          uint token)
        {
            uint tokenType = token & TokenTypeIds.TokenTypeMask;
            uint rowId = token & TokenTypeIds.RIDMask;
            switch (tokenType)
            {
                case TokenTypeIds.MethodDef:
                    return rowId << HasCustomAttributeTag.NumberOfBits | HasCustomAttributeTag.Method;
                case TokenTypeIds.FieldDef:
                    return rowId << HasCustomAttributeTag.NumberOfBits | HasCustomAttributeTag.Field;
                case TokenTypeIds.TypeRef:
                    return rowId << HasCustomAttributeTag.NumberOfBits | HasCustomAttributeTag.TypeRef;
                case TokenTypeIds.TypeDef:
                    return rowId << HasCustomAttributeTag.NumberOfBits | HasCustomAttributeTag.TypeDef;
                case TokenTypeIds.ParamDef:
                    return rowId << HasCustomAttributeTag.NumberOfBits | HasCustomAttributeTag.Param;
                case TokenTypeIds.InterfaceImpl:
                    return rowId << HasCustomAttributeTag.NumberOfBits | HasCustomAttributeTag.InterfaceImpl;
                case TokenTypeIds.MemberRef:
                    return rowId << HasCustomAttributeTag.NumberOfBits | HasCustomAttributeTag.MemberRef;
                case TokenTypeIds.Module:
                    return rowId << HasCustomAttributeTag.NumberOfBits | HasCustomAttributeTag.Module;
                case TokenTypeIds.Permission:
                    return rowId << HasCustomAttributeTag.NumberOfBits | HasCustomAttributeTag.DeclSecurity;
                case TokenTypeIds.Property:
                    return rowId << HasCustomAttributeTag.NumberOfBits | HasCustomAttributeTag.Property;
                case TokenTypeIds.Event:
                    return rowId << HasCustomAttributeTag.NumberOfBits | HasCustomAttributeTag.Event;
                case TokenTypeIds.Signature:
                    return rowId << HasCustomAttributeTag.NumberOfBits | HasCustomAttributeTag.StandAloneSig;
                case TokenTypeIds.ModuleRef:
                    return rowId << HasCustomAttributeTag.NumberOfBits | HasCustomAttributeTag.ModuleRef;
                case TokenTypeIds.TypeSpec:
                    return rowId << HasCustomAttributeTag.NumberOfBits | HasCustomAttributeTag.TypeSpec;
                case TokenTypeIds.Assembly:
                    return rowId << HasCustomAttributeTag.NumberOfBits | HasCustomAttributeTag.Assembly;
                case TokenTypeIds.AssemblyRef:
                    return rowId << HasCustomAttributeTag.NumberOfBits | HasCustomAttributeTag.AssemblyRef;
                case TokenTypeIds.File:
                    return rowId << HasCustomAttributeTag.NumberOfBits | HasCustomAttributeTag.File;
                case TokenTypeIds.ExportedType:
                    return rowId << HasCustomAttributeTag.NumberOfBits | HasCustomAttributeTag.ExportedType;
                case TokenTypeIds.ManifestResource:
                    return rowId << HasCustomAttributeTag.NumberOfBits | HasCustomAttributeTag.ManifestResource;
                case TokenTypeIds.GenericParam:
                    return rowId << HasCustomAttributeTag.NumberOfBits | HasCustomAttributeTag.GenericParameter;
            }

            return 0;
        }
    }
}