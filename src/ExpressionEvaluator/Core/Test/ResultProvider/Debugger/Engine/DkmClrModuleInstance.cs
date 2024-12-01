// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.MetadataReader;
using Microsoft.VisualStudio.Debugger.Symbols;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Adds;
using System.Text;
using System.Threading;

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

        public object GetMetaDataImport() => new MetadataImportMock(Assembly);

        private class MetadataImportMock : IMetadataImport
        {
            private readonly Assembly _assembly;
            public MetadataImportMock(Assembly assembly)
            {
                _assembly = assembly;
            }

            int IMetadataImport.GetFieldProps(int mb, out int mdTypeDef, StringBuilder szField, int cchField, out int pchField, out FieldAttributes pdwAttr, out EmbeddedBlobPointer ppvSigBlob, out int pcbSigBlob, out int pdwCPlusTypeFlab, out IntPtr ppValue, out int pcchValue)
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
                            pchField = fieldName.Length;

                            if (szField != null && cchField != 0)
                            {
                                if (cchField < pchField)
                                {
                                    return -1;
                                }

                                szField.Append(fieldName);
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

            int IMetadataImport.CountEnum(HCORENUM hEnum, out int pulCount)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.ResetEnum(HCORENUM hEnum, int ulPos)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumTypeDefs(ref HCORENUM phEnum, out int rTypeDefs, uint cMax, out uint pcTypeDefs)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumInterfaceImpls(ref HCORENUM phEnum, int td, out int rImpls, int cMax, ref int pcImpls)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumTypeRefs(ref HCORENUM phEnum, int[] td, uint cMax, uint pcTypeRefs)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.FindTypeDefByName(string szTypeDef, int tkEnclosingClass, out int token)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetScopeProps(StringBuilder szName, int cchName, out int pchName, out Guid mvid)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetModuleFromScope(out int mdModule)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetTypeDefProps(int td, StringBuilder szTypeDef, int cchTypeDef, out int pchTypeDef, out TypeAttributes pdwTypeDefFlags, out int ptkExtends)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetInterfaceImplProps(int iiImpl, out int pClass, out int ptkIface)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetTypeRefProps(int tr, out int ptkResolutionScope, StringBuilder szName, int cchName, out int pchName)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.ResolveTypeRef(int tr, ref Guid riid, out object ppIScope)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumMembers(ref uint phEnum, uint cl, uint[] rMembers, uint cMax, out uint pcTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumMembersWithName(ref uint phEnum, uint cl, string szName, uint[] rMembers, uint cMax, ref uint pcTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumMethods(ref HCORENUM phEnum, int cl, out int mdMethodDef, int cMax, out int pcTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumMethodsWithName(ref HCORENUM phEnum, int cl, string szName, out int mdMethodDef, int cMax, out int pcTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumFields(ref HCORENUM phEnum, int cl, out int mdFieldDef, int cMax, out uint pcTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumFieldsWithName(ref uint phEnum, uint cl, string szName, uint[] rFields, uint cMax, out uint pcTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumParams(ref HCORENUM phEnum, int mdMethodDef, int[] rParams, int cMax, out uint pcTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumMemberRefs(ref uint phEnum, uint tkParent, uint[] rMemberRefs, uint cMax, out uint pcTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumMethodImpls(ref HCORENUM hEnum, Token typeDef, out Token methodBody, out Token methodDecl, int cMax, out int cTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumPermissionSets(ref HCORENUM hEnum, uint tk, uint dwActions, uint[] rPermission, ref uint cMax)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.FindMember(int typeDefToken, string szName, byte[] pvSigBlob, int cbSigBlob, out int memberDefToken)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.FindMethod(int typeDef, string szName, EmbeddedBlobPointer pvSigBlob, int cbSigBlob, out int methodDef)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.FindField(int typeDef, string szName, byte[] pvSigBlob, int cbSigBlob, out int fieldDef)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.FindMemberRef(int typeRef, string szName, byte[] pvSigBlob, int cbSigBlob, out int result)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetMethodProps(uint md, out int pClass, StringBuilder szMethod, int cchMethod, out uint pchMethod, out MethodAttributes pdwAttr, out EmbeddedBlobPointer ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out uint pdwImplFlags)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetMemberRefProps(Token mr, out Token ptk, StringBuilder szMember, int cchMember, out uint pchMember, out EmbeddedBlobPointer ppvSigBlob, out uint pbSig)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumProperties(ref HCORENUM phEnum, int td, out int mdFieldDef, int cMax, out uint pcTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumEvents(ref HCORENUM phEnum, int td, out int mdFieldDef, int cMax, out uint pcEvents)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetEventProps(int ev, out int pClass, StringBuilder szEvent, int cchEvent, out int pchEvent, out int pdwEventFlags, out int ptkEventType, out int pmdAddOn, out int pmdRemoveOn, out int pmdFire, out int rmdOtherMethod, uint cMax, out uint pcOtherMethod)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumMethodSemantics(ref uint phEnum, uint mb, uint[] rEventProp, uint cMax, out uint pcEventProp)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetMethodSemantics(uint mb, uint tkEventProp, out uint pdwSemanticsFlags)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetClassLayout(int typeDef, out uint dwPackSize, COR_FIELD_OFFSET[] rFieldOffset, uint cMax, out uint cFieldOffset, out uint ulClassSize)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetFieldMarshal(int tk, out IntPtr ppvNativeType, out int pcbNativeType)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetRVA(int token, out uint rva, out uint flags)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetPermissionSetProps(uint pm, out uint pdwAction, out IntPtr ppvPermission, out int pcbPermission)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetSigFromToken(int token, out EmbeddedBlobPointer pSig, out int cbSig)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetModuleRefProps(int mur, StringBuilder szName, int cchName, out int pchName)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumModuleRefs(ref HCORENUM phEnum, out int mdModuleRef, int cMax, out uint pcModuleRefs)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetTypeSpecFromToken(Token typeSpec, out EmbeddedBlobPointer pSig, out int cbSig)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetNameFromToken(uint tk, string pszUtf8NamePtr)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumUnresolvedMethods(ref uint phEnum, uint[] rMethods, uint cMax, out uint pcTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetUserString(int stk, char[] szString, int cchString, out int pchString)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetPinvokeMap(uint tk, out uint pdwMappingFlags, StringBuilder szImportName, uint cchImportName, out uint pchImportName, out int pmrImportDLL)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumSignatures(ref uint phEnum, uint[] rSignatures, uint cmax, out uint pcSignatures)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumTypeSpecs(ref uint phEnum, uint[] rTypeSpecs, uint cmax, out uint pcTypeSpecs)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumUserStrings(ref uint phEnum, uint[] rStrings, uint cmax, out uint pcStrings)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetParamForMethodIndex(uint md, uint ulParamSeq, out uint pParam, out int ppd)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.EnumCustomAttributes(ref HCORENUM phEnum, int tk, int tkType, out Token mdCustomAttribute, uint cMax, out uint pcTokens)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetCustomAttributeProps(Token cv, out Token tkObj, out Token tkType, out EmbeddedBlobPointer blob, out int cbSize)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.FindTypeRef(int tkResolutionScope, string szName, out int typeRef)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetMemberProps(uint mb, out uint pClass, StringBuilder szMember, uint cchMember, out uint pchMember, out uint pdwAttr, out IntPtr ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out uint pdwImplFlags, out uint pdwCPlusTypeFlag, out IntPtr ppValue, out uint pcchValue)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetPropertyProps(Token prop, out Token pClass, StringBuilder szProperty, int cchProperty, out int pchProperty, out PropertyAttributes pdwPropFlags, out EmbeddedBlobPointer ppvSig, out int pbSig, out int pdwCPlusTypeFlag, out UnusedIntPtr ppDefaultValue, out int pcchDefaultValue, out Token pmdSetter, out Token pmdGetter, out Token rmdOtherMethod, uint cMax, out uint pcOtherMethod)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetParamProps(int tk, out int pmd, out uint pulSequence, StringBuilder szName, uint cchName, out uint pchName, out uint pdwAttr, out uint pdwCPlusTypeFlag, out UnusedIntPtr ppValue, out uint pcchValue)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetCustomAttributeByName(int tkObj, string szName, out EmbeddedBlobPointer ppData, out uint pcbData)
            {
                throw new NotImplementedException();
            }

            bool IMetadataImport.IsValidToken(uint tk)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetNestedClassProps(int tdNestedClass, out int tdEnclosingClass)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.GetNativeCallConvFromSig(IntPtr pvSig, uint cbSig, out uint pCallConv)
            {
                throw new NotImplementedException();
            }

            int IMetadataImport.IsGlobal(uint pd, out int pbGlobal)
            {
                throw new NotImplementedException();
            }

            #endregion
        }
    }
}
