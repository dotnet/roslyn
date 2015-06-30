// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class PortableImportRecord : ImportRecord
    {
        private readonly ImportTargetKind _targetKind;
        private readonly string _alias;
        private readonly AssemblyReferenceHandle _targetAssemblyHandle;
        private readonly EntityHandle _targetTypeHandle;
        private readonly string _targetNamespaceName;

        public override ImportTargetKind TargetKind => _targetKind;
        public override string Alias => _alias;
        public override string TargetString => _targetNamespaceName;

        private PortableImportRecord(
            ImportTargetKind targetKind,
            string alias,
            AssemblyReferenceHandle targetAssemblyHandle,
            EntityHandle targetTypeHandle,
            string targetNamespaceName)
        {
            _targetKind = targetKind;
            _alias = alias;
            _targetAssemblyHandle = targetAssemblyHandle;
            _targetTypeHandle = targetTypeHandle;
            _targetNamespaceName = targetNamespaceName;
        }

        // TODO (https://github.com/dotnet/roslyn/issues/702): Uncomment when ImportDefinition is available in this branch.
        //public static bool TryCreateFromImportDefinition(
        //    ImportDefinition importDefinition,
        //    MetadataReader metadataReader,
        //    out PortableImportRecord record)
        //{
        //    record = null;

        //    var targetAssemblyHandle = importDefinition.TargetAssembly;
        //    var alias = importDefinition.Alias.GetUtf8String(metadataReader);

        //    var targetHandle = importDefinition.TargetType;

        //    Handle targetTypeHandle;
        //    string targetNamespaceName;
        //    if (targetHandle.Kind == HandleKind.Blob)
        //    {
        //        targetTypeHandle = default(Handle);
        //        targetNamespaceName = ((BlobHandle)importDefinition.TargetType).GetUtf8String(metadataReader);
        //    }
        //    else
        //    {
        //        targetTypeHandle = targetHandle;
        //        targetNamespaceName = null;
        //    }

        //    ImportTargetKind targetKind;
        //    switch (importDefinition.Kind)
        //    {
        //        case ImportDefinitionKind.ImportNamespace:
        //            if (targetAssemblyHandle.IsNil &&
        //                alias == null &&
        //                targetNamespaceName != null &&
        //                targetTypeHandle.IsNil)
        //            {
        //                targetKind = ImportTargetKind.Namespace;
        //                break;
        //            }
        //            return false;
        //        case ImportDefinitionKind.AliasNamespace:
        //            if (targetAssemblyHandle.IsNil &&
        //                alias != null &&
        //                targetNamespaceName != null &&
        //                targetTypeHandle.IsNil)
        //            {
        //                targetKind = ImportTargetKind.Namespace;
        //                break;
        //            }
        //            return false;
        //        case ImportDefinitionKind.ImportAssemblyNamespace:
        //            if (!targetAssemblyHandle.IsNil &&
        //                alias == null &&
        //                targetNamespaceName != null &&
        //                targetTypeHandle.IsNil)
        //            {
        //                targetKind = ImportTargetKind.Namespace;
        //                break;
        //            }
        //            return false;
        //        case ImportDefinitionKind.AliasAssemblyNamespace:
        //            if (!targetAssemblyHandle.IsNil &&
        //                alias != null &&
        //                targetNamespaceName != null &&
        //                targetTypeHandle.IsNil)
        //            {
        //                targetKind = ImportTargetKind.Namespace;
        //                break;
        //            }
        //            return false;
        //        case ImportDefinitionKind.ImportType:
        //            if (targetAssemblyHandle.IsNil &&
        //                alias == null &&
        //                targetNamespaceName == null &&
        //                !targetTypeHandle.IsNil)
        //            {
        //                targetKind = ImportTargetKind.Type;
        //                break;
        //            }
        //            return false;
        //        case ImportDefinitionKind.AliasType:
        //            if (targetAssemblyHandle.IsNil &&
        //                alias != null &&
        //                targetNamespaceName == null &&
        //                !targetTypeHandle.IsNil)
        //            {
        //                targetKind = ImportTargetKind.Type;
        //                break;
        //            }
        //            return false;
        //        case ImportDefinitionKind.ImportXmlNamespace:
        //            if (targetAssemblyHandle.IsNil &&
        //                alias != null && // Always non-null, possibly empty.
        //                targetNamespaceName != null &&
        //                targetTypeHandle.IsNil)
        //            {
        //                targetKind = ImportTargetKind.XmlNamespace;
        //                break;
        //            }
        //            return false;
        //        case ImportDefinitionKind.ImportAssemblyReferenceAlias:
        //            if (targetAssemblyHandle.IsNil &&
        //                alias != null &&
        //                targetNamespaceName == null &&
        //                targetTypeHandle.IsNil)
        //            {
        //                targetKind = ImportTargetKind.Assembly;
        //                break;
        //            }
        //            return false;
        //        case ImportDefinitionKind.AliasAssemblyReference:
        //            // Should have created an ExternAliasRecord for this.
        //        default:
        //            throw ExceptionUtilities.UnexpectedValue(importDefinition.Kind);
        //    }

        //    record = new PortableImportRecord(
        //        targetKind,
        //        alias,
        //        targetAssemblyHandle,
        //        targetTypeHandle,
        //        targetNamespaceName);
        //    return true;
        //}

        public TTypeSymbol GetTargetType<TModuleSymbol, TTypeSymbol, TMethodSymbol, TFieldSymbol, TSymbol>(
            MetadataDecoder<TModuleSymbol, TTypeSymbol, TMethodSymbol, TFieldSymbol, TSymbol> metadataDecoder)
            where TModuleSymbol : class, IModuleSymbol
            where TTypeSymbol : class, TSymbol, ITypeSymbol
            where TMethodSymbol : class, TSymbol, IMethodSymbol
            where TFieldSymbol : class, TSymbol, IFieldSymbol
            where TSymbol : class, ISymbol
        {
            return _targetTypeHandle.IsNil
                ? null
                : metadataDecoder.GetTypeOfToken(_targetTypeHandle);
        }

        public TAssemblySymbol GetTargetAssembly<TModuleSymbol, TAssemblySymbol>(
            TModuleSymbol module,
            PEModule peModule)
            where TModuleSymbol : class, IModuleSymbol
            where TAssemblySymbol : class, IAssemblySymbol
        {
            if (_targetAssemblyHandle.IsNil)
            {
                return null;
            }

            var index = peModule.GetAssemblyReferenceIndexOrThrow(_targetAssemblyHandle);
            var referencedAssembly = module.ReferencedAssemblySymbols[index];
            return (TAssemblySymbol)referencedAssembly;
        }
    }
}
