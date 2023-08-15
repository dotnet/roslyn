// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Debugger.Clr;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal abstract class PlaceholderLocalSymbol : EELocalSymbolBase
    {
        private readonly MethodSymbol _method;
        private readonly string _name;
        private readonly TypeWithAnnotations _type;

        internal readonly string DisplayName;

        internal PlaceholderLocalSymbol(MethodSymbol method, string name, string displayName, TypeSymbol type)
        {
            _method = method;
            _name = name;
            _type = TypeWithAnnotations.Create(type);

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

            ReadOnlyCollection<byte> dynamicFlags;
            ReadOnlyCollection<string> tupleElementNames;
            CustomTypeInfo.Decode(alias.CustomTypeInfoId, alias.CustomTypeInfo, out dynamicFlags, out tupleElementNames);

            if (dynamicFlags != null)
            {
                type = DecodeDynamicTypes(type, sourceAssembly, dynamicFlags);
            }

            type = TupleTypeDecoder.DecodeTupleTypesIfApplicable(type, tupleElementNames.AsImmutableOrNull());

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
            get { throw ExceptionUtilities.Unreachable(); }
        }

        public override TypeWithAnnotations TypeWithAnnotations
        {
            get { return _type; }
        }

        internal override bool IsPinned
        {
            get { return false; }
        }

        internal override bool IsKnownToReferToTempIfReferenceType
        {
            get { return false; }
        }

        internal override bool IsCompilerGenerated
        {
            get { return true; }
        }

        public override RefKind RefKind
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

        internal abstract override bool IsWritableVariable { get; }

        internal override EELocalSymbolBase ToOtherMethod(MethodSymbol method, TypeMap typeMap)
        {
            // Placeholders should be rewritten (as method calls)
            // rather than copied as locals to the target method.
            throw ExceptionUtilities.Unreachable();
        }

        /// <summary>
        /// Rewrite the local reference as a call to a synthesized method.
        /// </summary>
        internal abstract BoundExpression RewriteLocal(CSharpCompilation compilation, SyntaxNode syntax, DiagnosticBag diagnostics);

        internal static BoundExpression ConvertToLocalType(CSharpCompilation compilation, BoundExpression expr, TypeSymbol type, DiagnosticBag diagnostics)
        {
            var bindingDiagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: false);

            if (type.IsPointerType())
            {
                var syntax = expr.Syntax;
                var intPtrType = compilation.GetSpecialType(SpecialType.System_IntPtr);
                Binder.ReportUseSite(intPtrType, bindingDiagnostics, syntax);
                MethodSymbol conversionMethod;
                if (Binder.TryGetSpecialTypeMember(compilation, SpecialMember.System_IntPtr__op_Explicit_ToPointer, syntax, bindingDiagnostics, out conversionMethod))
                {
                    var temp = ConvertToLocalTypeHelper(compilation, expr, intPtrType, bindingDiagnostics);
                    expr = BoundCall.Synthesized(
                        syntax,
                        receiverOpt: null,
                        method: conversionMethod,
                        arg0: temp);
                }
                else
                {
                    diagnostics.AddRange(bindingDiagnostics.DiagnosticBag);
                    bindingDiagnostics.Free();

                    return new BoundBadExpression(
                        syntax,
                        LookupResultKind.Empty,
                        ImmutableArray<Symbol>.Empty,
                        ImmutableArray.Create(expr),
                        type);
                }
            }

            var result = ConvertToLocalTypeHelper(compilation, expr, type, bindingDiagnostics);
            diagnostics.AddRange(bindingDiagnostics.DiagnosticBag);
            bindingDiagnostics.Free();

            return result;
        }

        private static BoundExpression ConvertToLocalTypeHelper(CSharpCompilation compilation, BoundExpression expr, TypeSymbol type, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(diagnostics.DiagnosticBag != null);

            // NOTE: This conversion can fail if some of the types involved are from not-yet-loaded modules.
            // For example, if System.Exception hasn't been loaded, then this call will fail for $stowedexception.
            var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, compilation.Assembly);
            var conversion = compilation.Conversions.ClassifyConversionFromExpression(expr, type, isChecked: false, ref useSiteInfo);
            diagnostics.Add(expr.Syntax, useSiteInfo);
            Debug.Assert(conversion.IsValid || diagnostics.HasAnyErrors());

            return BoundConversion.Synthesized(
                expr.Syntax,
                expr,
                conversion,
                @checked: false,
                explicitCastInCode: false,
                conversionGroupOpt: null,
                constantValueOpt: null,
                type: type,
                hasErrors: !conversion.IsValid);
        }

        internal static MethodSymbol GetIntrinsicMethod(CSharpCompilation compilation, string methodName)
        {
            var type = compilation.GetTypeByMetadataName(ExpressionCompilerConstants.IntrinsicAssemblyTypeMetadataName);
            if ((object)type == null)
            {
                return null;
            }
            var members = type.GetMembers(methodName);
            Debug.Assert(members.Length == 1);
            return (MethodSymbol)members[0];
        }

        private static TypeSymbol DecodeDynamicTypes(TypeSymbol type, AssemblySymbol sourceAssembly, ReadOnlyCollection<byte> bytes)
        {
            var builder = ArrayBuilder<bool>.GetInstance();
            DynamicFlagsCustomTypeInfo.CopyTo(bytes, builder);
            var dynamicType = DynamicTypeDecoder.TransformTypeWithoutCustomModifierFlags(
                type,
                sourceAssembly,
                RefKind.None,
                builder.ToImmutableAndFree(),
                checkLength: false);
            Debug.Assert((object)dynamicType != null);
            Debug.Assert(!TypeSymbol.Equals(dynamicType, type, TypeCompareKind.ConsiderEverything2));
            return dynamicType;
        }
    }
}
