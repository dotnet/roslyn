// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
        /// <exception cref="ArgumentOutOfRangeException">Thrown when appending to a StringBuilder past the <see cref="StringBuilder.MaxCapacity"/> limit.</exception>
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
                if (extensionParameter.RefKind != RefKind.None)
                {
                    builder.Append('&');
                }
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
                else if (type is DynamicTypeSymbol)
                {
                    builder.Append("System.Object");
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

                if (namedType.Name == "void" && namedType.IsTopLevelType() && namedType.ContainingNamespace.IsGlobalNamespace)
                {
                    builder.Append("'void'");
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

                // Note: skipping identifier and variance
                if (builder[builder.Length - 1] == ' ')
                {
                    builder.Remove(startIndex: builder.Length - 1, length: 1);
                }
            }

            static void appendTypeParameterReference(TypeParameterSymbol typeParameter, StringBuilder builder)
            {
                if (typeParameter.ContainingType.IsExtension)
                {
                    builder.Append("!");
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
                builder.Append(")");
            }

            static void appendArrayType(ArrayTypeSymbol array, StringBuilder builder)
            {
                Debug.Assert(array.Sizes.IsEmpty && array.LowerBounds.IsDefault); // We only deal with source array types

                appendType(array.ElementType, builder);
                builder.Append('[');
                for (int i = 1; i < array.Rank; i++)
                {
                    builder.Append(',');
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
                    Debug.Assert(functionPointer.Signature.ReturnTypeWithAnnotations.CustomModifiers.IsEmpty); // We're only dealing with source function pointers
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

        /// <summary>
        /// This name uses a C#-looking format to encode C#-level information of an extension block (ie. arity, constraints, extended type, attributes and C#-isms like tuple names).
        /// It is meant be to hashed to produce the content-based name for the extension marker type.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when appending to a StringBuilder past the <see cref="StringBuilder.MaxCapacity"/> limit.</exception>
        internal string ComputeExtensionMarkerRawName()
        {
            Debug.Assert(this.IsExtension && this.IsDefinition);

            var pooledStringBuilder = PooledStringBuilder.GetInstance();
            StringBuilder builder = pooledStringBuilder.Builder;
            builder.Append("extension");

            if (this.Arity > 0)
            {
                builder.Append("<");

                foreach (TypeParameterSymbol typeParameter in this.TypeParameters)
                {
                    if (typeParameter.Ordinal > 0)
                    {
                        builder.Append(", ");
                    }

                    appendAttributes(typeParameter.GetAttributes(), builder);
                    appendIdentifier(typeParameter.Name, builder);
                }

                builder.Append(">");
            }

            builder.Append("(");
            if (this.ExtensionParameter is { } extensionParameter)
            {
                // We intentionally ignore "scoped" and "params"
                appendRefKind(extensionParameter.RefKind, builder, forParameter: true);
                appendAttributes(extensionParameter.GetAttributes(), builder);
                appendTypeWithAnnotation(extensionParameter.TypeWithAnnotations, builder);

                string name = extensionParameter.Name;
                if (name != "")
                {
                    builder.Append(' ');
                    appendIdentifier(name, builder);
                }
            }

            builder.Append(")");

            foreach (TypeParameterSymbol typeParameter in this.TypeParameters)
            {
                if (typeParameterHasConstraints(typeParameter))
                {
                    appendTypeParameterConstraints(typeParameter, builder);
                }
            }

            return pooledStringBuilder.ToStringAndFree();

            static bool typeParameterHasConstraints(TypeParameterSymbol typeParameter)
            {
                return !typeParameter.ConstraintTypesNoUseSiteDiagnostics.IsEmpty || typeParameter.HasConstructorConstraint ||
                    typeParameter.HasReferenceTypeConstraint || typeParameter.HasValueTypeConstraint ||
                    typeParameter.HasNotNullConstraint || typeParameter.AllowsRefLikeType;
            }

            static void appendTypeWithAnnotation(TypeWithAnnotations type, StringBuilder builder)
            {
                appendType(type.Type, builder);

                if (!type.Type.IsValueType)
                {
                    appendAnnotation(builder, type.NullableAnnotation);
                }
            }

            static void appendAnnotation(StringBuilder builder, NullableAnnotation annotation)
            {
                switch (annotation)
                {
                    case NullableAnnotation.Annotated:
                        builder.Append('?');
                        break;
                    case NullableAnnotation.NotAnnotated:
                        builder.Append('!');
                        break;
                    case NullableAnnotation.Oblivious:
                        break;
                    case NullableAnnotation.Ignored:
                        Debug.Assert(false);
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(annotation);
                }
            }

            static void appendType(TypeSymbol type, StringBuilder builder)
            {
                if (type is NamedTypeSymbol namedType)
                {
                    appendNamedType(type, builder, namedType);
                }
                else if (type is TypeParameterSymbol { Name: var typeParameterName })
                {
                    appendIdentifier(typeParameterName, builder, forTypeConstraint: true);
                }
                else if (type is ArrayTypeSymbol array)
                {
                    appendArrayType(array, builder);
                }
                else if (type is PointerTypeSymbol pointer)
                {
                    appendTypeWithAnnotation(pointer.PointedAtTypeWithAnnotations, builder);
                    builder.Append('*');
                }
                else if (type is FunctionPointerTypeSymbol functionPointer)
                {
                    appendFunctionPointerType(functionPointer, builder);
                }
                else if (type is DynamicTypeSymbol)
                {
                    builder.Append("dynamic");
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

                if (namedType.IsTopLevelType() && namedType.ContainingNamespace.IsGlobalNamespace)
                {
                    if (namedType.Name == "dynamic")
                    {
                        builder.Append("@dynamic");
                        return;
                    }

                    if (namedType.Name == "unmanaged")
                    {
                        // We technically only need to escape in type constraints, but do it generally for simplicity
                        builder.Append("@unmanaged");
                        return;
                    }

                    if (namedType.Name == "notnull")
                    {
                        // We technically only need to escape in type constraints, but do it generally for simplicity
                        builder.Append("@notnull");
                        return;
                    }
                }

                if (namedType.IsTupleType)
                {
                    builder.Append('(');
                    ImmutableArray<string?> elementNames = namedType.TupleElementNames;
                    for (int i = 0; i < namedType.TupleElementTypesWithAnnotations.Length; i++)
                    {
                        TypeWithAnnotations elementType = namedType.TupleElementTypesWithAnnotations[i];
                        if (i > 0)
                        {
                            builder.Append(", ");
                        }

                        appendTypeWithAnnotation(elementType, builder);

                        if (!elementNames.IsDefault && elementNames[i] is { } name)
                        {
                            builder.Append(" ");
                            appendIdentifier(name, builder);
                        }

                        Debug.Assert(elementType.CustomModifiers.IsEmpty);
                    }

                    builder.Append(')');
                    return;
                }

                appendNamespace(namedType.ContainingNamespace, builder);
                appendContainingType(namedType, builder);
                appendIdentifier(namedType.Name, builder);
                appendTypeArguments(namedType, builder);
            }

            static void appendIdentifier(string name, StringBuilder builder, bool forTypeConstraint = false)
            {
                if ((forTypeConstraint && name is "unmanaged" or "notnull")
                    || SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None)
                {
                    builder.Append('@');
                }

                builder.Append(name);
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
                if (namedType.ContainingType is { } containingType)
                {
                    appendContainingType(containingType, builder);
                    builder.Append(containingType.Name);
                    appendTypeArguments(containingType, builder);
                    builder.Append('.');
                }
            }

            static void appendTypeArguments(NamedTypeSymbol namedType, StringBuilder builder)
            {
                ImmutableArray<TypeWithAnnotations> typeArguments = namedType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;
                if (typeArguments.Length > 0)
                {
                    builder.Append('<');
                    for (int i = 0; i < typeArguments.Length; i++)
                    {
                        if (i > 0)
                        {
                            builder.Append(", ");
                        }

                        appendTypeWithAnnotation(typeArguments[i], builder);
                        Debug.Assert(typeArguments[i].CustomModifiers.IsEmpty);
                    }

                    builder.Append('>');
                }
            }

            static void appendTypeParameterConstraints(TypeParameterSymbol typeParam, StringBuilder builder)
            {
                builder.Append(" where ");
                appendIdentifier(typeParam.Name, builder);
                builder.Append(" : ");

                bool needComma = false;

                if (typeParam.HasReferenceTypeConstraint)
                {
                    builder.Append("class");

                    switch (typeParam.ReferenceTypeConstraintIsNullable)
                    {
                        case true:
                            builder.Append('?');
                            break;

                        case false:
                            builder.Append('!');
                            break;
                    }

                    needComma = true;
                }
                else if (typeParam.HasUnmanagedTypeConstraint)
                {
                    builder.Append("unmanaged");
                    needComma = true;
                }
                else if (typeParam.HasValueTypeConstraint)
                {
                    builder.Append("struct");
                    needComma = true;
                }
                else if (typeParam.HasNotNullConstraint)
                {
                    builder.Append("notnull");
                    needComma = true;
                }

                if (typeParam.ConstraintTypesNoUseSiteDiagnostics.Length > 0)
                {
                    appendTypeConstraints(typeParam, builder, ref needComma);
                }

                if (typeParam.HasConstructorConstraint)
                {
                    if (needComma)
                    {
                        builder.Append(", ");
                    }

                    builder.Append("new()");
                    needComma = true;
                }

                if (typeParam.AllowsRefLikeType)
                {
                    if (needComma)
                    {
                        builder.Append(", ");
                    }

                    builder.Append("allows ref struct");
                }
            }

            static void appendTypeConstraints(TypeParameterSymbol typeParam, StringBuilder builder, ref bool needComma)
            {
                var typeConstraintsBuilder = ArrayBuilder<string>.GetInstance();
                for (int i = 0; i < typeParam.ConstraintTypesNoUseSiteDiagnostics.Length; i++)
                {
                    var stringBuilder = PooledStringBuilder.GetInstance();
                    appendTypeWithAnnotation(typeParam.ConstraintTypesNoUseSiteDiagnostics[i], stringBuilder.Builder);
                    typeConstraintsBuilder.Add(stringBuilder.ToStringAndFree());
                }

                typeConstraintsBuilder.Sort(StringComparer.Ordinal); // Actual order doesn't matter - just want to be deterministic

                foreach (string typeConstraint in typeConstraintsBuilder)
                {
                    if (needComma)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(typeConstraint);
                    needComma = true;
                }

                typeConstraintsBuilder.Free();
            }

            static void appendArrayType(ArrayTypeSymbol array, StringBuilder builder)
            {
                // Note: we're using the inner dimensions first order (whether nullability annotations are present or not)
                appendTypeWithAnnotation(array.ElementTypeWithAnnotations, builder);
                builder.Append('[');
                for (int i = 1; i < array.Rank; i++)
                {
                    builder.Append(',');
                }

                builder.Append(']');
            }

            static void appendFunctionPointerType(FunctionPointerTypeSymbol functionPointer, StringBuilder builder)
            {
                builder.Append("delegate*");

                FunctionPointerMethodSymbol signature = functionPointer.Signature;
                string? callingConvention = signature.CallingConvention switch
                {
                    Cci.CallingConvention.Default => null, // managed is the default
                    Cci.CallingConvention.Unmanaged => " unmanaged",
                    Cci.CallingConvention.CDecl => " unmanaged[CDecl]",
                    Cci.CallingConvention.Standard => " unmanaged[Stdcall]",
                    Cci.CallingConvention.ThisCall => " unmanaged[Thiscall]",
                    Cci.CallingConvention.FastCall => " unmanaged[Fastcall]",
                    _ => throw ExceptionUtilities.UnexpectedValue(signature.CallingConvention)
                };

                builder.Append(callingConvention);
                appendCallingConventionTypes(signature.UnmanagedCallingConventionTypes, builder);

                bool needComma = false;
                builder.Append('<');
                ImmutableArray<ParameterSymbol> parameters = signature.Parameters;
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (needComma)
                    {
                        builder.Append(", ");
                    }

                    ParameterSymbol parameter = parameters[i];
                    appendRefKind(parameter.RefKind, builder, forParameter: true);
                    appendTypeWithAnnotation(parameter.TypeWithAnnotations, builder);
                    Debug.Assert(parameter.TypeWithAnnotations.CustomModifiers.IsEmpty);
                    needComma = true;
                }

                if (needComma)
                {
                    builder.Append(", ");
                }

                appendRefKind(signature.RefKind, builder);
                appendTypeWithAnnotation(signature.ReturnTypeWithAnnotations, builder);

                builder.Append('>');
            }

            static void appendRefKind(RefKind refKind, StringBuilder builder, bool forParameter = false)
            {
                builder.Append(refKind switch
                {
                    RefKind.None => "",
                    RefKind.Ref => "ref ",
                    RefKind.Out => "out ",
                    RefKind.In when forParameter => "in ",
                    RefKind.RefReadOnly => "ref readonly ",
                    RefKind.RefReadOnlyParameter => "ref readonly ",
                    _ => throw ExceptionUtilities.UnexpectedValue(refKind)
                });
            }

            static void appendCallingConventionTypes(ImmutableArray<NamedTypeSymbol> callingConventionTypes, StringBuilder builder)
            {
                if (callingConventionTypes.IsEmpty)
                {
                    return;
                }

                builder.Append('[');
                for (int i = 0; i < callingConventionTypes.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }

                    Debug.Assert(callingConventionTypes[i].Name.StartsWith("CallConv", StringComparison.Ordinal));

                    builder.Append(callingConventionTypes[i].Name.Remove(0, "CallConv".Length));
                }

                builder.Append(']');
            }

            static void appendAttributes(ImmutableArray<CSharpAttributeData> attributes, StringBuilder builder)
            {
                if (attributes.IsEmpty)
                {
                    return;
                }

                ArrayBuilder<string> attributesBuilder = ArrayBuilder<string>.GetInstance(attributes.Length);

                foreach (CSharpAttributeData attribute in attributes)
                {
                    var stringBuilder = PooledStringBuilder.GetInstance();
                    appendAttribute(attribute, stringBuilder.Builder);
                    attributesBuilder.Add(stringBuilder.ToStringAndFree());
                }

                attributesBuilder.Sort(StringComparer.Ordinal); // Actual order doesn't matter - just want to be deterministic

                for (int i = 0; i < attributesBuilder.Count; i++)
                {
                    builder.Append('[');
                    builder.Append(attributesBuilder[i]);
                    builder.Append("] ");
                }

                attributesBuilder.Free();
            }

            static void appendAttribute(CSharpAttributeData attribute, StringBuilder builder)
            {
                Debug.Assert(attribute.AttributeClass is not null);
                appendType(attribute.AttributeClass, builder);

                if (attribute.CommonConstructorArguments.IsEmpty && attribute.CommonNamedArguments.IsEmpty)
                {
                    return;
                }

                builder.Append('(');
                bool needComma = false;
                foreach (TypedConstant argument in attribute.CommonConstructorArguments)
                {
                    if (needComma)
                    {
                        builder.Append(", ");
                    }

                    appendAttributeArgument(argument, builder);
                    needComma = true;
                }

                if (!attribute.CommonNamedArguments.IsEmpty)
                {
                    var namedArgumentsBuilder = ArrayBuilder<string>.GetInstance(attribute.CommonNamedArguments.Length);
                    foreach (KeyValuePair<string, TypedConstant> namedArgument in attribute.CommonNamedArguments)
                    {
                        var stringBuilder = PooledStringBuilder.GetInstance();
                        appendAttributeNamedArgument(namedArgument, stringBuilder.Builder);
                        namedArgumentsBuilder.Add(stringBuilder.ToStringAndFree());
                    }

                    namedArgumentsBuilder.Sort(StringComparer.Ordinal); // Actual order doesn't matter - just want to be deterministic

                    foreach (string namedArgument in namedArgumentsBuilder)
                    {
                        if (needComma)
                        {
                            builder.Append(", ");
                        }

                        builder.Append(namedArgument);
                        needComma = true;
                    }

                    namedArgumentsBuilder.Free();
                }

                builder.Append(')');
            }

            static void appendAttributeArgument(TypedConstant argument, StringBuilder builder)
            {
                if (argument.Kind == TypedConstantKind.Error)
                {
                    builder.Append("error");
                    return;
                }

                if (argument.IsNull)
                {
                    Debug.Assert(argument.Type is not null);
                    builder.Append('(');
                    appendType(((PublicModel.TypeSymbol)argument.Type).UnderlyingTypeSymbol, builder);
                    builder.Append(')');
                    builder.Append("null");
                    return;
                }

                switch (argument.Kind)
                {
                    case TypedConstantKind.Primitive:
                        appendPrimitive(argument.Value, builder, withCast: true);
                        break;

                    case TypedConstantKind.Enum:
                        Debug.Assert(argument.Type is not null);
                        builder.Append('(');
                        appendType(((PublicModel.TypeSymbol)argument.Type).UnderlyingTypeSymbol, builder);
                        builder.Append(')');
                        appendPrimitive(argument.Value, builder, withCast: false);
                        break;

                    case TypedConstantKind.Type:
                        Debug.Assert(argument.Value is not null);
                        builder.Append("typeof(");
                        appendType(((PublicModel.TypeSymbol)argument.Value).UnderlyingTypeSymbol, builder);
                        builder.Append(')');
                        break;

                    case TypedConstantKind.Array:
                        Debug.Assert(argument.Type is not null);
                        if (argument.Values.IsEmpty)
                        {
                            builder.Append("new ");
                            appendType(((PublicModel.TypeSymbol)argument.Type).UnderlyingTypeSymbol, builder);
                            builder.Append(" { }");
                        }
                        else
                        {
                            builder.Append('[');
                            bool needComma = false;
                            foreach (TypedConstant element in argument.Values)
                            {
                                if (needComma)
                                {
                                    builder.Append(", ");
                                }

                                appendAttributeArgument(element, builder);
                                needComma = true;
                            }

                            builder.Append(']');
                        }
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(argument.Kind);
                }
            }

            static void appendAttributeNamedArgument(KeyValuePair<string, TypedConstant> namedArgument, StringBuilder builder)
            {
                appendIdentifier(namedArgument.Key, builder);
                builder.Append(" = ");
                appendAttributeArgument(namedArgument.Value, builder);
            }

            static void appendPrimitive(object? value, StringBuilder builder, bool withCast)
            {
                var options = ObjectDisplayOptions.UseQuotes | ObjectDisplayOptions.EscapeNonPrintableCharacters;
                if (withCast)
                {
                    string cast = value switch
                    {
                        sbyte => "(sbyte)",
                        short => "(short)",
                        int => "(int)",
                        long => "(long)",
                        byte => "(byte)",
                        ushort => "(ushort)",
                        uint => "(uint)",
                        ulong => "(ulong)",
                        float => "(float)",
                        double => "(double)",
                        _ => "",
                    };
                    builder.Append(cast);
                }

                switch (value)
                {
                    case bool b:
                        builder.Append(b ? "true" : "false");
                        break;
                    case sbyte sb:
                        builder.Append(sb.ToString(CultureInfo.InvariantCulture));
                        break;
                    case short sh:
                        builder.Append(sh.ToString(CultureInfo.InvariantCulture));
                        break;
                    case int i:
                        builder.Append(i.ToString(CultureInfo.InvariantCulture));
                        break;
                    case long l:
                        builder.Append(l.ToString(CultureInfo.InvariantCulture));
                        break;
                    case byte by:
                        builder.Append(by.ToString(CultureInfo.InvariantCulture));
                        break;
                    case ushort us:
                        builder.Append(us.ToString(CultureInfo.InvariantCulture));
                        break;
                    case uint ui:
                        builder.Append(ui.ToString(CultureInfo.InvariantCulture));
                        break;
                    case ulong ul:
                        builder.Append(ul.ToString(CultureInfo.InvariantCulture));
                        break;
                    case float f:
                        // Note: we're printing as bits to avoid any loss
                        builder.Append(BitConverter.ToInt32(BitConverter.GetBytes(f), startIndex: 0));
                        break;
                    case double d:
                        // Note: we're printing as bits to avoid any loss
                        builder.Append(BitConverter.DoubleToInt64Bits(d));
                        break;
                    case char c:
                        builder.Append(ObjectDisplay.FormatLiteral(c, options));
                        break;
                    case string s:
                        builder.Append(ObjectDisplay.FormatLiteral(s, options));
                        break;
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
