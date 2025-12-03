// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Adds;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.MetadataReader.PortableInterop;
using Microsoft.VisualStudio.Debugger.Symbols;
using Token = System.Reflection.Adds.Token;

namespace Microsoft.VisualStudio.Debugger.Clr
{
    public class DkmClrModuleInstance : DkmModuleInstance
    {
        internal readonly Assembly Assembly;
        private int _resolveTypeNameFailures;

        public DkmClrModuleInstance(DkmClrRuntimeInstance runtimeInstance, Assembly assembly, DkmModule module) :
            base(module)
        {
            RuntimeInstance = runtimeInstance;
            this.Assembly = assembly;
        }

        public Guid Mvid
        {
            get { return this.Assembly.Modules.First().ModuleVersionId; }
        }

        public DkmClrRuntimeInstance RuntimeInstance { get; }

        public DkmProcess Process => RuntimeInstance.Process;

        public DkmClrType ResolveTypeName(string typeName, ReadOnlyCollection<DkmClrType> typeArguments)
        {
            var type = this.Assembly.GetType(typeName);
            if (type == null)
            {
                Interlocked.Increment(ref _resolveTypeNameFailures);
                throw new ArgumentException();
            }
            Debug.Assert(typeArguments.Count == type.GetGenericArguments().Length);
            if (typeArguments.Count > 0)
            {
                var typeArgs = typeArguments.Select(t => ((TypeImpl)t.GetLmrType()).Type).ToArray();
                type = type.MakeGenericType(typeArgs);
            }
            return RuntimeInstance.GetType((TypeImpl)type);
        }

        internal int ResolveTypeNameFailures
        {
            get { return _resolveTypeNameFailures; }
        }

        public DkmMetadataImportHolder GetMetaDataImportHolder() => new DkmMetadataImportHolder(new MetadataImportMock(Assembly));

        private class MetadataImportMock : IMetadataImport
        {
            private readonly Assembly _assembly;
            public MetadataImportMock(Assembly assembly)
            {
                _assembly = assembly;
            }

            int IMetadataImport.GetFieldProps(Token mb, out Token mdTypeDef, char[] szField, uint cchField, out uint pchField, out FieldAttributes pdwAttr, out EmbeddedBlobPointer ppvSigBlob, out uint pcbSigBlob, out uint pdwCPlusTypeFlab, out IntPtr ppValue, out uint pcchValue)
            {
                mdTypeDef = default;
                pchField = default;
                pdwAttr = default;
                ppvSigBlob = default;
                pcbSigBlob = default;
                pdwCPlusTypeFlab = default;
                ppValue = default;
                pcchValue = default;

                // Iterate over all types in the assembly to find the field matching the token.
                foreach (var type in _assembly.GetTypes())
                {
                    foreach (var field in type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        // Found a match.
                        if (field.MetadataToken == mb)
                        {
                            var fieldName = field.Name;
                            pchField = (uint)fieldName.Length + 1; // this API returns the length including null terminator

                            if (szField != null && cchField != 0)
                            {
                                if (cchField < pchField)
                                {
                                    return -1;
                                }

                                fieldName.CopyTo(0, szField, 0, fieldName.Length);
                            }

                            return 0;
                        }
                    }
                }

                return 1;
            }

            #region IMetadataImport - throws NotImplementedException
            void IMetadataImport.CloseEnum(IntPtr hEnum)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.CountEnum(HCORENUM hEnum, out uint pulCount)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.ResetEnum(HCORENUM hEnum, uint ulPos)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumTypeDefs(ref HCORENUM phEnum, out Token rTypeDefs, uint cMax, out uint pcTypeDefs)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumInterfaceImpls(ref HCORENUM phEnum, int td, out Token rImpls, uint cMax, out uint pcImpls)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumTypeRefs(ref HCORENUM phEnum, out Token td, uint cMax, out uint pcTypeRefs)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.FindTypeDefByName(string szTypeDef, Token tkEnclosingClass, out Token token)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetScopeProps(char[] szName, uint cchName, out uint pchName, out Guid mvid)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetModuleFromScope(out Token mdModule)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetTypeDefProps(Token td, char[] szTypeDef, uint cchTypeDef, out uint pchTypeDef, out TypeAttributes pdwTypeDefFlags, out Token ptkExtends)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetInterfaceImplProps(Token iiImpl, out Token pClass, out Token ptkIface)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetTypeRefProps(Token tr, out Token ptkResolutionScope, char[] szName, uint cchName, out uint pchName)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.ResolveTypeRef(Token tr, ref Guid riid, out object ppIScope, out Token ptd)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumMembers(ref HCORENUM phEnum, Token cl, out Token rMembers, uint cMax, out uint pcTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumMembersWithName(ref HCORENUM phEnum, Token cl, string szName, out Token rMembers, uint cMax, out uint pcTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumMethods(ref HCORENUM phEnum, Token cl, out Token mdMethodDef, uint cMax, out uint pcTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumMethodsWithName(ref HCORENUM phEnum, Token cl, string szName, out Token mdMethodDef, uint cMax, out uint pcTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumFields(ref HCORENUM phEnum, Token cl, out Token mdFieldDef, uint cMax, out uint pcTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumFieldsWithName(ref HCORENUM phEnum, Token cl, string szName, out Token rFields, uint cMax, out uint pcTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumParams(ref HCORENUM phEnum, Token mdMethodDef, out Token rParams, uint cMax, out uint pcTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumMemberRefs(ref HCORENUM phEnum, Token tkParent, out Token rMemberRefs, uint cMax, out uint pcTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumMethodImpls(ref HCORENUM hEnum, Token typeDef, out Token methodBody, out Token methodDecl, uint cMax, out uint cTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumPermissionSets(ref HCORENUM hEnum, Token tk, uint dwActions, out Token rPermission, uint cMax, out uint pcTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.FindMember(Token typeDefToken, string szName, byte[] pvSigBlob, uint cbSigBlob, out Token memberDefToken)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.FindMethod(Token typeDef, string szName, EmbeddedBlobPointer pvSigBlob, uint cbSigBlob, out Token methodDef)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.FindField(Token typeDef, string szName, byte[] pvSigBlob, uint cbSigBlob, out Token fieldDef)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.FindMemberRef(Token typeRef, string szName, byte[] pvSigBlob, uint cbSigBlob, out Token result)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetMethodProps(Token md, out Token pClass, char[] szMethod, uint cchMethod, out uint pchMethod, out MethodAttributes pdwAttr, out EmbeddedBlobPointer ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out uint pdwImplFlags)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetMemberRefProps(Token mr, out Token ptk, char[] szMember, uint cchMember, out uint pchMember, out EmbeddedBlobPointer ppvSigBlob, out uint pbSig)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumProperties(ref HCORENUM phEnum, Token td, out Token mdFieldDef, uint cMax, out uint pcTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumEvents(ref HCORENUM phEnum, Token td, out Token mdFieldDef, uint cMax, out uint pcEvents)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetEventProps(Token ev, out Token pClass, char[] szEvent, uint cchEvent, out uint pchEvent, out EventAttributes pdwEventFlags, out Token ptkEventType, out Token pmdAddOn, out Token pmdRemoveOn, out Token pmdFire, out Token rmdOtherMethod, uint cMax, out uint pcOtherMethod)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumMethodSemantics(ref HCORENUM phEnum, Token mb, out Token rEventProp, uint cMax, out uint pcEventProp)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetMethodSemantics(Token mb, Token tkEventProp, out uint pdwSemanticsFlags)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetClassLayout(Token typeDef, out uint dwPackSize, MetadataReader.COR_FIELD_OFFSET[] rFieldOffset, uint cMax, out uint cFieldOffset, out uint ulClassSize)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetFieldMarshal(Token tk, out EmbeddedBlobPointer ppvNativeType, out uint pcbNativeType)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetRVA(Token token, out uint rva, out uint flags)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetPermissionSetProps(Token pm, out uint pdwAction, out IntPtr ppvPermission, out uint pcbPermission)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetSigFromToken(Token token, out EmbeddedBlobPointer pSig, out uint cbSig)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetModuleRefProps(Token mur, char[] szName, uint cchName, out uint pchName)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumModuleRefs(ref HCORENUM phEnum, out Token mdModuleRef, uint cMax, out uint pcModuleRefs)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetTypeSpecFromToken(Token typeSpec, out EmbeddedBlobPointer pSig, out uint cbSig)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetNameFromToken(Token tk, out IntPtr pszUtf8NamePtr)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumUnresolvedMethods(ref HCORENUM phEnum, out Token rMethods, uint cMax, out uint pcTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetUserString(Token stk, char[] szString, uint cchString, out uint pchString)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetPinvokeMap(Token tk, out uint pdwMappingFlags, char[] szImportName, uint cchImportName, out uint pchImportName, out Token pmrImportDLL)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumSignatures(ref HCORENUM phEnum, out Token rSignatures, uint cmax, out uint pcSignatures)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumTypeSpecs(ref HCORENUM phEnum, out Token rTypeSpecs, uint cmax, out uint pcTypeSpecs)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumUserStrings(ref HCORENUM phEnum, out Token rStrings, uint cmax, out uint pcStrings)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetParamForMethodIndex(Token md, uint ulParamSeq, out Token ppd)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumCustomAttributes(ref HCORENUM phEnum, Token tk, Token tkType, out Token mdCustomAttribute, uint cMax, out uint pcTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetCustomAttributeProps(Token cv, out Token tkObj, out Token tkType, out EmbeddedBlobPointer blob, out uint cbSize)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.FindTypeRef(Token tkResolutionScope, string szName, out Token typeRef)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetMemberProps(Token mb, out Token pClass, char[] szMember, uint cchMember, out uint pchMember, out uint pdwAttr, out EmbeddedBlobPointer ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out uint pdwImplFlags, out uint pdwCPlusTypeFlag, out IntPtr ppValue, out uint pcchValue)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetPropertyProps(Token prop, out Token pClass, char[] szProperty, uint cchProperty, out uint pchProperty, out PropertyAttributes pdwPropFlags, out EmbeddedBlobPointer ppvSig, out uint pbSig, out uint pdwCPlusTypeFlag, out MetadataReader.UnusedIntPtr ppDefaultValue, out uint pcchDefaultValue, out Token pmdSetter, out Token pmdGetter, out Token rmdOtherMethod, uint cMax, out uint pcOtherMethod)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetParamProps(Token tk, out Token pmd, out uint pulSequence, char[] szName, uint cchName, out uint pchName, out ParameterAttributes pdwAttr, out uint pdwCPlusTypeFlag, out MetadataReader.UnusedIntPtr ppValue, out uint pcchValue)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetCustomAttributeByName(Token tkObj, string szName, out EmbeddedBlobPointer ppData, out uint pcbData)
            {
                throw new NotImplementedException();
            }

            bool IMetadataImport.IsValidToken(Token tk)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetNestedClassProps(Token tdNestedClass, out Token tdEnclosingClass)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetNativeCallConvFromSig(IntPtr pvSig, uint cbSig, out uint pCallConv)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.IsGlobal(Token pd, out int pbGlobal)
            {
                throw new NotImplementedException();
            }

            #endregion
        }
    }
}
