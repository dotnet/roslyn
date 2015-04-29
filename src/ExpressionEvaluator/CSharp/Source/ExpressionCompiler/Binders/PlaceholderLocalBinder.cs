// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class PlaceholderLocalBinder : LocalScopeBinder
    {
        private readonly InspectionContext _inspectionContext;
        private readonly TypeNameDecoder<PEModuleSymbol, TypeSymbol> _typeNameDecoder;
        private readonly CSharpSyntaxNode _syntax;
        private readonly MethodSymbol _containingMethod;

        internal PlaceholderLocalBinder(
            InspectionContext inspectionContext,
            TypeNameDecoder<PEModuleSymbol, TypeSymbol> typeNameDecoder,
            CSharpSyntaxNode syntax,
            MethodSymbol containingMethod,
            Binder next) :
            base(next)
        {
            _inspectionContext = inspectionContext;
            _typeNameDecoder = typeNameDecoder;
            _syntax = syntax;
            _containingMethod = containingMethod;
        }

        internal sealed override void LookupSymbolsInSingleBinder(
            LookupResult result,
            string name,
            int arity,
            ConsList<Symbol> basesBeingResolved,
            LookupOptions options,
            Binder originalBinder,
            bool diagnose,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if ((options & (LookupOptions.NamespaceAliasesOnly | LookupOptions.NamespacesOrTypesOnly | LookupOptions.LabelsOnly)) != 0)
            {
                return;
            }

            var local = this.LookupPlaceholder(name);
            if ((object)local == null)
            {
                base.LookupSymbolsInSingleBinder(result, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteDiagnostics);
            }
            else
            {
                result.MergeEqual(this.CheckViability(local, arity, options, null, diagnose, ref useSiteDiagnostics, basesBeingResolved));
            }
        }

        protected sealed override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo info, LookupOptions options, Binder originalBinder)
        {
            throw new NotImplementedException();
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            var builder = ArrayBuilder<LocalSymbol>.GetInstance();
            var declaration = _syntax as LocalDeclarationStatementSyntax;
            if (declaration != null)
            {
                var kind = declaration.IsConst ? LocalDeclarationKind.Constant : LocalDeclarationKind.RegularVariable;
                foreach (var variable in declaration.Declaration.Variables)
                {
                    var local = SourceLocalSymbol.MakeLocal(_containingMethod, this, declaration.Declaration.Type, variable.Identifier, kind, variable.Initializer);
                    builder.Add(local);
                }
            }
            return builder.ToImmutableAndFree();
        }

        private PlaceholderLocalSymbol LookupPlaceholder(string name)
        {
            if (name.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                var valueText = name.Substring(2);
                ulong address;
                if (!ulong.TryParse(valueText, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out address))
                {
                    // Invalid value should have been caught by Lexer.
                    throw ExceptionUtilities.UnexpectedValue(valueText);
                }
                return new ObjectAddressLocalSymbol(_containingMethod, name, this.Compilation.GetSpecialType(SpecialType.System_Object), address);
            }

            AliasKind kind;
            string id;
            int index;
            if (!PseudoVariableUtilities.TryParseVariableName(name, caseSensitive: true, kind: out kind, id: out id, index: out index))
            {
                return null;
            }

            var typeName = PseudoVariableUtilities.GetTypeName(_inspectionContext, kind, id, index);
            if (typeName == null)
            {
                return null;
            }

            // The old API (GetObjectTypeNameById) doesn't return custom type info,
            // but the new one (GetAliases) will.
            return CreatePlaceholderLocal(_typeNameDecoder, _containingMethod, new Alias(kind, id, id, typeName, default(CustomTypeInfo)));
        }

        internal static PlaceholderLocalSymbol CreatePlaceholderLocal(
            TypeNameDecoder<PEModuleSymbol, TypeSymbol> typeNameDecoder,
            MethodSymbol containingMethod,
            Alias alias)
        {
            var typeName = alias.Type;
            Debug.Assert(typeName.Length > 0);

            var type = typeNameDecoder.GetTypeSymbolForSerializedType(typeName);
            Debug.Assert((object)type != null);

            var dynamicFlagsInfo = alias.CustomTypeInfo.ToDynamicFlagsCustomTypeInfo();
            if (dynamicFlagsInfo.Any())
            {
                var flagsBuilder = ArrayBuilder<bool>.GetInstance();
                dynamicFlagsInfo.CopyTo(flagsBuilder);
                var dynamicType = DynamicTypeDecoder.TransformTypeWithoutCustomModifierFlags(
                    type,
                    containingMethod.ContainingAssembly,
                    RefKind.None,
                    flagsBuilder.ToImmutableAndFree(),
                    checkLength: false);
                Debug.Assert(dynamicType != null);
                Debug.Assert(dynamicType != type);
                type = dynamicType;
            }

            var id = alias.FullName;
            switch (alias.Kind)
            {
                case AliasKind.Exception:
                    return new ExceptionLocalSymbol(containingMethod, id, type, ExpressionCompilerConstants.GetExceptionMethodName);
                case AliasKind.StowedException:
                    return new ExceptionLocalSymbol(containingMethod, id, type, ExpressionCompilerConstants.GetStowedExceptionMethodName);
                case AliasKind.ReturnValue:
                    {
                        int index;
                        PseudoVariableUtilities.TryParseReturnValueIndex(id, out index);
                        Debug.Assert(index >= 0);
                        return new ReturnValueLocalSymbol(containingMethod, id, type, index);
                    }
                case AliasKind.ObjectId:
                    return new ObjectIdLocalSymbol(containingMethod, type, id, isWritable: false);
                case AliasKind.DeclaredLocal:
                    return new ObjectIdLocalSymbol(containingMethod, type, id, isWritable: true);
                default:
                    throw ExceptionUtilities.UnexpectedValue(alias.Kind);
            }
        }
    }
}
