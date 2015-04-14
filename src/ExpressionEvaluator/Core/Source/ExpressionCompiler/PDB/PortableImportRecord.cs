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
        private readonly Handle _targetTypeHandle;
        private readonly string _targetNamespaceName;

        public override ImportTargetKind TargetKind => _targetKind;
        public override string Alias => _alias;
        public override string TargetString => _targetNamespaceName;

        internal PortableImportRecord(
            ImportTargetKind targetKind,
            string alias,
            AssemblyReferenceHandle targetAssemblyHandle,
            Handle targetTypeHandle,
            string targetNamespaceName)
        {
            _targetKind = targetKind;
            _alias = alias;
            _targetAssemblyHandle = targetAssemblyHandle;
            _targetTypeHandle = targetTypeHandle;
            _targetNamespaceName = targetNamespaceName;
        }

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
