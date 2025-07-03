// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class SourceNamedTypeSymbol
    {
        private ExtensionInfo _lazyExtensionInfo;

        private class ExtensionInfo
        {
            public MethodSymbol? LazyExtensionMarker = ErrorMethodSymbol.UnknownMethod;
            public ParameterSymbol? LazyExtensionParameter;
            public ImmutableDictionary<MethodSymbol, MethodSymbol>? LazyImplementationMap;
        }

        internal override string ExtensionName
        {
            get
            {
                if (!IsExtension)
                {
                    throw ExceptionUtilities.Unreachable();
                }

                MergedNamespaceOrTypeDeclaration declaration;
                if (ContainingType is not null)
                {
                    declaration = ((SourceNamedTypeSymbol)this.ContainingType).declaration;
                }
                else
                {
                    declaration = ((SourceNamespaceSymbol)this.ContainingSymbol).MergedDeclaration;
                }

                int index = 0;
                foreach (Declaration child in declaration.Children)
                {
                    if (child == this.declaration)
                    {
                        return GeneratedNames.MakeExtensionName(index);
                    }

                    if (child.Kind == DeclarationKind.Extension)
                    {
                        index++;
                    }
                }

                throw ExceptionUtilities.Unreachable();
            }
        }

        /// <summary>
        /// This name uses an IL-looking format to encode CLR-level information of an extension block (ie. arity, constraints, extended type).
        /// It is meant be to hashed to produce the content-based name for the extension grouping type.
        /// </summary>
        internal string ComputeExtensionGroupingRawName()
        {
            Debug.Assert(this.IsExtension && this.IsDefinition);

            var pooledBuilder = PooledStringBuilder.GetInstance();
            var builder = pooledBuilder.Builder;
            builder.Append("extension");

            if (this.Arity > 0)
            {
                builder.Append("<");

                foreach (var typeParameter in this.TypeParameters)
                {
                    if (typeParameter.Ordinal > 0)
                    {
                        builder.Append(", ");
                    }

                    appendTypeParameterDeclaration(typeParameter, builder);
                }

                builder.Append(">");
            }

            builder.Append("(");
            if (this.ExtensionParameter is { } extensionParameter)
            {
                appendType(extensionParameter.Type, builder);
            }

            builder.Append(")");

            return pooledBuilder.ToStringAndFree();

            static void appendType(TypeSymbol type, StringBuilder builder)
            {
                if (type is NamedTypeSymbol namedType)
                {
                    appendNamedType(type, builder, namedType);
                }
                else if (type is TypeParameterSymbol typeParameter)
                {
                    appendTypeParameterReference(typeParameter, builder);
                }
                else if (type is ArrayTypeSymbol array)
                {
                    appendArrayType(array, builder);
                }
                else if (type is PointerTypeSymbol pointer)
                {
                    appendType(pointer.PointedAtType, builder);
                    builder.Append('*');
                }
                else if (type is FunctionPointerTypeSymbol functionPointer)
                {
                    appendFunctionPointerType(functionPointer, builder);
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(type);
                }
            }

            static void appendNamedType(TypeSymbol type, StringBuilder builder, NamedTypeSymbol namedType)
            {
                Debug.Assert(type.CustomModifierCount() == 0);

                if (namedType.SpecialType == SpecialType.System_Void)
                {
                    builder.Append("void");
                    return;
                }

                // Note: in valid IL, we need a "class" or "valuetype" keyword in many contexts
                appendNamespace(namedType.ContainingNamespace, builder);
                appendContainingType(namedType, builder);
                builder.Append(namedType.MetadataName);
                appendTypeArguments(namedType, builder);
            }

            static void appendNamespace(NamespaceSymbol ns, StringBuilder builder)
            {
                if (ns is not null && !ns.IsGlobalNamespace)
                {
                    appendNamespace(ns.ContainingNamespace, builder);
                    builder.Append(ns.Name);
                    builder.Append('.');
                }
            }

            static void appendContainingType(NamedTypeSymbol namedType, StringBuilder builder)
            {
                // Note: using slash for nested type to match CIL: ECMA-335 I.10.7.2
                if (namedType.ContainingType is { } containingType)
                {
                    appendContainingType(containingType, builder);
                    builder.Append(containingType.MetadataName);
                    builder.Append('/');
                }
            }

            static void appendTypeArguments(NamedTypeSymbol namedType, StringBuilder builder)
            {
                var typeArguments = ArrayBuilder<TypeWithAnnotations>.GetInstance();
                namedType.GetAllTypeArgumentsNoUseSiteDiagnostics(typeArguments);
                if (typeArguments.Count > 0)
                {
                    builder.Append('<');
                    for (int i = 0; i < typeArguments.Count; i++)
                    {
                        if (i > 0)
                        {
                            builder.Append(", ");
                        }

                        appendType(typeArguments[i].Type, builder);
                        Debug.Assert(typeArguments[i].CustomModifiers.IsEmpty);
                    }

                    builder.Append('>');
                }

                typeArguments.Free();
            }

            static void appendTypeParameterDeclaration(TypeParameterSymbol typeParameter, StringBuilder builder)
            {
                Debug.Assert(typeParameter.Variance == VarianceKind.None);

                if (typeParameter.HasReferenceTypeConstraint)
                {
                    builder.Append("class ");
                }
                else if (typeParameter.HasValueTypeConstraint || typeParameter.HasUnmanagedTypeConstraint)
                {
                    builder.Append("valuetype ");
                }

                if (typeParameter.AllowsRefLikeType)
                {
                    builder.Append("byreflike ");
                }

                if (typeParameter.HasConstructorConstraint || typeParameter.HasValueTypeConstraint || typeParameter.HasUnmanagedTypeConstraint)
                {
                    builder.Append(".ctor ");
                }

                appendTypeParameterTypeConstraints(typeParameter, builder);
                // Note: in valid IL, we would need a valid identifier name
                builder.Append(StringExtensions.GetNumeral(typeParameter.Ordinal));
            }

            static void appendTypeParameterReference(TypeParameterSymbol typeParameter, StringBuilder builder)
            {
                if (typeParameter.ContainingType.IsExtension)
                {
                    builder.Append("!");
                    // Note: in valid IL, we would need a valid identifier name
                    builder.Append(StringExtensions.GetNumeral(typeParameter.Ordinal));
                }
                else
                {
                    // error scenario
                    builder.Append("!");
                    builder.Append(typeParameter.Name);
                }
            }

            static void appendTypeParameterTypeConstraints(TypeParameterSymbol typeParameter, StringBuilder builder)
            {
                ImmutableArray<TypeWithAnnotations> typeConstraints = typeParameter.GetConstraintTypes(ConsList<TypeParameterSymbol>.Empty);
                if (typeConstraints.IsEmpty && !typeParameter.HasUnmanagedTypeConstraint && !typeParameter.HasValueTypeConstraint)
                {
                    return;
                }

                var typeConstraintStrings = ArrayBuilder<string>.GetInstance(typeConstraints.Length);
                foreach (var typeConstraint in typeConstraints)
                {
                    var constraintBuilder = PooledStringBuilder.GetInstance();
                    appendType(typeConstraint.Type, constraintBuilder.Builder);
                    typeConstraintStrings.Add(constraintBuilder.ToStringAndFree());
                }

                if (typeParameter.HasUnmanagedTypeConstraint)
                {
                    typeConstraintStrings.Add("System.ValueType modreq(System.Runtime.InteropServices.UnmanagedType)");
                }
                else if (typeParameter.HasValueTypeConstraint)
                {
                    typeConstraintStrings.Add("System.ValueType");
                }

                typeConstraintStrings.Sort(StringComparer.Ordinal); // Actual order doesn't matter - just want to be deterministic

                builder.Append('(');
                for (int i = 0; i < typeConstraintStrings.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(typeConstraintStrings[i]);
                }

                typeConstraintStrings.Free();
                builder.Append(") ");
            }

            static void appendArrayType(ArrayTypeSymbol array, StringBuilder builder)
            {
                Debug.Assert(array.Sizes.IsEmpty && array.LowerBounds.IsEmpty);

                appendType(array.ElementType, builder);
                builder.Append('[');
                for (int i = 0; i < array.Rank; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(',');
                    }
                }

                builder.Append(']');
            }

            static void appendFunctionPointerType(FunctionPointerTypeSymbol functionPointer, StringBuilder builder)
            {
                builder.Append("method ");

                // When calling convention is a single one of the four special calling conventions, we just use flags
                // Otherwise, we use "unmanaged" flag and also add return modifiers below
                var callingConvention = functionPointer.Signature.CallingConvention switch
                {
                    Cci.CallingConvention.Default => null, // managed is the default
                    Cci.CallingConvention.Unmanaged => "unmanaged ",
                    Cci.CallingConvention.CDecl => "unmanaged cdecl ",
                    Cci.CallingConvention.Standard => "unmanaged stdcall ",
                    Cci.CallingConvention.ThisCall => "unmanaged thiscall ",
                    Cci.CallingConvention.FastCall => "unmanaged fastcall ",
                    _ => throw ExceptionUtilities.UnexpectedValue(functionPointer.Signature.CallingConvention)
                };

                builder.Append(callingConvention);

                appendType(functionPointer.Signature.ReturnType, builder);
                if (functionPointer.Signature.RefKind != RefKind.None)
                {
                    builder.Append('&');
                    appendModifiers(functionPointer.Signature.RefCustomModifiers, builder);
                }
                else
                {
                    appendModifiers(functionPointer.Signature.ReturnTypeWithAnnotations.CustomModifiers, builder);
                }

                builder.Append(" *(");
                var parameters = functionPointer.Signature.Parameters;
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }

                    ParameterSymbol parameter = parameters[i];
                    appendType(parameter.Type, builder);
                    Debug.Assert(parameter.TypeWithAnnotations.CustomModifiers.IsEmpty);
                    if (parameter.RefKind != RefKind.None)
                    {
                        builder.Append('&');
                        appendModifiers(parameter.RefCustomModifiers, builder);
                    }
                }

                builder.Append(')');
            }

            static void appendModifiers(ImmutableArray<CustomModifier> customModifiers, StringBuilder builder)
            {
                // Order of modifiers is significant in metadata so we preserve the order.
                var modifierStrings = ArrayBuilder<string>.GetInstance(customModifiers.Length);

                foreach (CustomModifier modifier in customModifiers)
                {
                    var modifierBuilder = PooledStringBuilder.GetInstance();
                    modifierBuilder.Builder.Append(modifier.IsOptional ? " modopt(" : " modreq(");

                    appendType(((CSharpCustomModifier)modifier).ModifierSymbol, modifierBuilder.Builder);
                    modifierBuilder.Builder.Append(')');

                    builder.Append(modifierBuilder.ToStringAndFree());
                }
            }
        }

        internal sealed override ParameterSymbol? ExtensionParameter
        {
            get
            {
                if (!IsExtension)
                {
                    return null;
                }

                var markerMethod = TryGetOrCreateExtensionMarker();

                if (_lazyExtensionInfo.LazyExtensionParameter == null && markerMethod is { Parameters: [var parameter, ..] })
                {
                    Interlocked.CompareExchange(ref _lazyExtensionInfo.LazyExtensionParameter, new ReceiverParameterSymbol(this, parameter), null);
                }

                return _lazyExtensionInfo.LazyExtensionParameter;
            }
        }

        public sealed override MethodSymbol? TryGetCorrespondingExtensionImplementationMethod(MethodSymbol method)
        {
            Debug.Assert(this.IsExtension);
            Debug.Assert(method.IsDefinition);
            Debug.Assert(method.ContainingType == (object)this);

            var containingType = this.ContainingType;

            if (containingType is null)
            {
                return null;
            }

            if (_lazyExtensionInfo is null)
            {
                Interlocked.CompareExchange(ref _lazyExtensionInfo, new ExtensionInfo(), null);
            }

            if (_lazyExtensionInfo.LazyImplementationMap is null)
            {
                var builder = ImmutableDictionary.CreateBuilder<MethodSymbol, MethodSymbol>(ReferenceEqualityComparer.Instance);

                builder.AddRange(
                    containingType.GetMembersUnordered().OfType<SourceExtensionImplementationMethodSymbol>().
                    Select(static m => new KeyValuePair<MethodSymbol, MethodSymbol>(m.UnderlyingMethod, m)));

                Interlocked.CompareExchange(ref _lazyExtensionInfo.LazyImplementationMap, builder.ToImmutable(), null);
            }

            return _lazyExtensionInfo.LazyImplementationMap.GetValueOrDefault(method);
        }

        protected sealed override MethodSymbol? CreateSynthesizedExtensionMarker()
        {
            return TryGetOrCreateExtensionMarker();
        }

        [MemberNotNull(nameof(_lazyExtensionInfo))]
        private MethodSymbol? TryGetOrCreateExtensionMarker()
        {
            Debug.Assert(IsExtension);

            if (_lazyExtensionInfo is null)
            {
                Interlocked.CompareExchange(ref _lazyExtensionInfo, new ExtensionInfo(), null);
            }

            if (_lazyExtensionInfo.LazyExtensionMarker == (object)ErrorMethodSymbol.UnknownMethod)
            {
                Interlocked.CompareExchange(ref _lazyExtensionInfo.LazyExtensionMarker, tryCreateExtensionMarker(), ErrorMethodSymbol.UnknownMethod);
            }

            return _lazyExtensionInfo.LazyExtensionMarker;

            MethodSymbol? tryCreateExtensionMarker()
            {
                var syntax = (ExtensionBlockDeclarationSyntax)this.GetNonNullSyntaxNode();
                var parameterList = syntax.ParameterList;
                Debug.Assert(parameterList is not null);

                if (parameterList is null)
                {
                    return null;
                }

                int count = parameterList.Parameters.Count;
                Debug.Assert(count > 0);

                return new SynthesizedExtensionMarker(this, parameterList);
            }
        }

        internal static Symbol? GetCompatibleSubstitutedMember(CSharpCompilation compilation, Symbol extensionMember, TypeSymbol receiverType)
        {
            Debug.Assert(extensionMember.GetIsNewExtensionMember());

            NamedTypeSymbol extension = extensionMember.ContainingType;
            if (extension.ExtensionParameter is null)
            {
                return null;
            }

            Symbol result;
            if (extensionMember.IsDefinition)
            {
                NamedTypeSymbol? constructedExtension = inferExtensionTypeArguments(extension, receiverType, compilation);
                if (constructedExtension is null)
                {
                    return null;
                }

                result = extensionMember.SymbolAsMember(constructedExtension);
            }
            else
            {
                result = extensionMember;
            }

            Debug.Assert(result.ContainingType.ExtensionParameter is not null);
            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            Conversion conversion = compilation.Conversions.ConvertExtensionMethodThisArg(parameterType: result.ContainingType.ExtensionParameter.Type, receiverType, ref discardedUseSiteInfo, isMethodGroupConversion: false);
            if (!conversion.Exists)
            {
                return null;
            }

            return result;

            static NamedTypeSymbol? inferExtensionTypeArguments(NamedTypeSymbol extension, TypeSymbol receiverType, CSharpCompilation compilation)
            {
                if (extension.Arity == 0)
                {
                    return extension;
                }

                TypeConversions conversions = extension.ContainingAssembly.CorLibrary.TypeConversions;

                // Note: we create a value for purpose of inferring type arguments even when the receiver type is static
                var syntax = (CSharpSyntaxNode)CSharpSyntaxTree.Dummy.GetRoot();
                var receiverValue = new BoundLiteral(syntax, ConstantValue.Bad, receiverType) { WasCompilerGenerated = true };

                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                ImmutableArray<TypeWithAnnotations> typeArguments = MethodTypeInferrer.InferTypeArgumentsFromReceiverType(extension, receiverValue, compilation, conversions, ref discardedUseSiteInfo);
                if (typeArguments.IsDefault || typeArguments.Any(t => !t.HasType))
                {
                    return null;
                }

                var result = extension.Construct(typeArguments);

                var constraintArgs = new ConstraintsHelper.CheckConstraintsArgs(compilation, conversions, includeNullability: false,
                    NoLocation.Singleton, diagnostics: BindingDiagnosticBag.Discarded, template: CompoundUseSiteInfo<AssemblySymbol>.Discarded);

                bool success = result.CheckConstraints(constraintArgs);
                if (!success)
                {
                    return null;
                }

                return result;
            }
        }
    }
}
