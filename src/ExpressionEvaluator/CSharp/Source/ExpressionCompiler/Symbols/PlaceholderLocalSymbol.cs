// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Clr;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal abstract class PlaceholderLocalSymbol : EELocalSymbolBase
    {
        private readonly MethodSymbol _method;
        private readonly string _name;
        private readonly TypeSymbolWithAnnotations _type;

        internal readonly string DisplayName;

        internal PlaceholderLocalSymbol(MethodSymbol method, string name, string displayName, TypeSymbol type)
        {
            _method = method;
            _name = name;
            _type = TypeSymbolWithAnnotations.Create(type);

            this.DisplayName = displayName;
        }

        internal static LocalSymbol Create(
            TypeNameDecoder<PEModuleSymbol, TypeSymbol> typeNameDecoder,
            MethodSymbol containingMethod,
            AssemblySymbol sourceAssembly,
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
                    sourceAssembly,
                    RefKind.None,
                    flagsBuilder.ToImmutableAndFree(),
                    checkLength: false);
                Debug.Assert(dynamicType != null);
                Debug.Assert(dynamicType != type);
                type = dynamicType;
            }

            var name = alias.FullName;
            var displayName = alias.Name;
            switch (alias.Kind)
            {
                case DkmClrAliasKind.Exception:
                    return new ExceptionLocalSymbol(containingMethod, name, displayName, type, ExpressionCompilerConstants.GetExceptionMethodName);
                case DkmClrAliasKind.StowedException:
                    return new ExceptionLocalSymbol(containingMethod, name, displayName, type, ExpressionCompilerConstants.GetStowedExceptionMethodName);
                case DkmClrAliasKind.ReturnValue:
                    {
                        int index;
                        PseudoVariableUtilities.TryParseReturnValueIndex(name, out index);
                        Debug.Assert(index >= 0);
                        return new ReturnValueLocalSymbol(containingMethod, name, displayName, type, index);
                    }
                case DkmClrAliasKind.ObjectId:
                    return new ObjectIdLocalSymbol(containingMethod, type, name, displayName, isWritable: false);
                case DkmClrAliasKind.Variable:
                    return new ObjectIdLocalSymbol(containingMethod, type, name, displayName, isWritable: true);
                default:
                    throw ExceptionUtilities.UnexpectedValue(alias.Kind);
            }
        }

        internal override LocalDeclarationKind DeclarationKind
        {
            get { return LocalDeclarationKind.RegularVariable; }
        }

        internal override SyntaxToken IdentifierToken
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public override TypeSymbolWithAnnotations Type
        {
            get { return _type; }
        }

        internal override bool IsPinned
        {
            get { return false; }
        }

        internal override bool IsCompilerGenerated
        {
            get { return true; }
        }

        internal override RefKind RefKind
        {
            get { return RefKind.None; }
        }

        public override Symbol ContainingSymbol
        {
            get { return _method; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return NoLocations; }
        }

        public override string Name
        {
            get { return _name; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return ImmutableArray<SyntaxReference>.Empty; }
        }

        internal abstract override bool IsWritable { get; }

        internal override EELocalSymbolBase ToOtherMethod(MethodSymbol method, TypeMap typeMap)
        {
            // Placeholders should be rewritten (as method calls)
            // rather than copied as locals to the target method.
            throw ExceptionUtilities.Unreachable;
        }

        /// <summary>
        /// Rewrite the local reference as a call to a synthesized method.
        /// </summary>
        internal abstract BoundExpression RewriteLocal(CSharpCompilation compilation, EENamedTypeSymbol container, CSharpSyntaxNode syntax, DiagnosticBag diagnostics);

        internal static BoundExpression ConvertToLocalType(CSharpCompilation compilation, BoundExpression expr, TypeSymbol type, DiagnosticBag diagnostics)
        {
            if (type.IsPointerType())
            {
                var syntax = expr.Syntax;
                var intPtrType = compilation.GetSpecialType(SpecialType.System_IntPtr);
                Binder.ReportUseSiteDiagnostics(intPtrType, diagnostics, syntax);
                MethodSymbol conversionMethod;
                if (Binder.TryGetSpecialTypeMember(compilation, SpecialMember.System_IntPtr__op_Explicit_ToPointer, syntax, diagnostics, out conversionMethod))
                {
                    var temp = ConvertToLocalTypeHelper(compilation, expr, intPtrType, diagnostics);
                    expr = BoundCall.Synthesized(
                        syntax,
                        receiverOpt: null,
                        method: conversionMethod,
                        arg0: temp);
                }
                else
                {
                    return new BoundBadExpression(
                        syntax,
                        LookupResultKind.Empty,
                        ImmutableArray<Symbol>.Empty,
                        ImmutableArray.Create<BoundNode>(expr),
                        type);
                }
            }

            return ConvertToLocalTypeHelper(compilation, expr, type, diagnostics);
        }

        private static BoundExpression ConvertToLocalTypeHelper(CSharpCompilation compilation, BoundExpression expr, TypeSymbol type, DiagnosticBag diagnostics)
        {
            // NOTE: This conversion can fail if some of the types involved are from not-yet-loaded modules.
            // For example, if System.Exception hasn't been loaded, then this call will fail for $stowedexception.
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var conversion = compilation.Conversions.ClassifyConversionFromExpression(expr, type, ref useSiteDiagnostics);
            diagnostics.Add(expr.Syntax, useSiteDiagnostics);
            Debug.Assert(conversion.IsValid || diagnostics.HasAnyErrors());

            return BoundConversion.Synthesized(
                expr.Syntax,
                expr,
                conversion,
                @checked: false,
                explicitCastInCode: false,
                constantValueOpt: null,
                type: type,
                hasErrors: !conversion.IsValid);
        }

        internal static MethodSymbol GetIntrinsicMethod(CSharpCompilation compilation, string methodName)
        {
            var type = compilation.GetTypeByMetadataName(ExpressionCompilerConstants.IntrinsicAssemblyTypeMetadataName);
            var members = type.GetMembers(methodName);
            Debug.Assert(members.Length == 1);
            return (MethodSymbol)members[0];
        }
    }
}
