// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public abstract class AttributeData
    {
        protected AttributeData()
        {
        }

        /// <summary>
        /// The attribute class.
        /// </summary>
        public INamedTypeSymbol? AttributeClass { get { return CommonAttributeClass; } }
        protected abstract INamedTypeSymbol? CommonAttributeClass { get; }

        /// <summary>
        /// The constructor on the attribute class.
        /// </summary>
        public IMethodSymbol? AttributeConstructor { get { return CommonAttributeConstructor; } }
        protected abstract IMethodSymbol? CommonAttributeConstructor { get; }

        public SyntaxReference? ApplicationSyntaxReference { get { return CommonApplicationSyntaxReference; } }
        protected abstract SyntaxReference? CommonApplicationSyntaxReference { get; }

        /// <summary>
        /// Constructor arguments on the attribute.
        /// </summary>
        public ImmutableArray<TypedConstant> ConstructorArguments { get { return CommonConstructorArguments; } }
        protected internal abstract ImmutableArray<TypedConstant> CommonConstructorArguments { get; }

        /// <summary>
        /// Named (property value) arguments on the attribute. 
        /// </summary>
        public ImmutableArray<KeyValuePair<string, TypedConstant>> NamedArguments { get { return CommonNamedArguments; } }
        protected internal abstract ImmutableArray<KeyValuePair<string, TypedConstant>> CommonNamedArguments { get; }

        /// <summary>
        /// Attribute is conditionally omitted if it is a source attribute and both the following are true:
        /// (a) It has at least one applied conditional attribute AND
        /// (b) None of conditional symbols are true at the attribute source location.
        /// </summary>
        internal virtual bool IsConditionallyOmitted
        {
            get { return false; }
        }

        [MemberNotNullWhen(true, nameof(AttributeClass), nameof(AttributeConstructor))]
        internal virtual bool HasErrors
        {
            get { return false; }
        }

        /// <summary>
        /// Checks if an applied attribute with the given attributeType matches the namespace name and type name of the given early attribute's description
        /// and the attribute description has a signature with parameter count equal to the given attributeArgCount.
        /// NOTE: We don't allow early decoded attributes to have optional parameters.
        /// </summary>
        internal static bool IsTargetEarlyAttribute(INamedTypeSymbolInternal attributeType, int attributeArgCount, AttributeDescription description)
        {
            if (attributeType.ContainingSymbol?.Kind != SymbolKind.Namespace)
            {
                return false;
            }

            int attributeCtorsCount = description.Signatures.Length;
            for (int i = 0; i < attributeCtorsCount; i++)
            {
                int parameterCount = description.GetParameterCount(signatureIndex: i);

                // NOTE: Below assumption disallows early decoding well-known attributes with optional parameters.
                if (attributeArgCount == parameterCount)
                {
                    StringComparison options = description.MatchIgnoringCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                    return attributeType.Name.Equals(description.Name, options) && namespaceMatch(attributeType.ContainingNamespace, description.Namespace, options);
                }
            }

            return false;

            static bool namespaceMatch(INamespaceSymbolInternal container, string namespaceName, StringComparison options)
            {
                int index = namespaceName.Length;
                bool expectDot = false;

                while (true)
                {
                    if (container.IsGlobalNamespace)
                    {
                        return index == 0;
                    }

                    if (expectDot)
                    {
                        index--;

                        if (index < 0 || namespaceName[index] != '.')
                        {
                            return false;
                        }
                    }
                    else
                    {
                        expectDot = true;
                    }

                    string name = container.Name;
                    int nameLength = name.Length;
                    index -= nameLength;
                    if (index < 0 || string.Compare(namespaceName, index, name, 0, nameLength, options) != 0)
                    {
                        return false;
                    }

                    container = container.ContainingNamespace;

                    if (container is null)
                    {
                        return false;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the value of a constructor argument as type <typeparamref name="T"/>.
        /// Throws if no constructor argument exists or the argument cannot be converted to the type.
        /// </summary>
        internal T? GetConstructorArgument<T>(int i, SpecialType specialType)
        {
            var constructorArgs = this.CommonConstructorArguments;
            return constructorArgs[i].DecodeValue<T>(specialType);
        }

        /// <summary>
        /// Returns named attribute argument with the given <paramref name="name"/> as type <typeparamref name="T"/>.
        /// If there is more than one named argument with this name, it returns the last one.
        /// If no named argument is found then the <paramref name="defaultValue"/> is returned.
        /// </summary>
        /// <param name="name">The metadata property or field name. This name is case sensitive (both VB and C#).</param>
        /// <param name="specialType">SpecialType of the named argument.</param>
        /// <param name="defaultValue">Default value for the named argument.</param>
        /// <remarks>
        /// For user defined attributes VB allows duplicate named arguments and uses the last value.
        /// Dev11 reports an error for pseudo-custom attributes when emitting metadata. We don't.
        /// </remarks>
        internal T? DecodeNamedArgument<T>(string name, SpecialType specialType, T? defaultValue = default)
        {
            return DecodeNamedArgument<T>(CommonNamedArguments, name, specialType, defaultValue);
        }

        private static T? DecodeNamedArgument<T>(ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments, string name, SpecialType specialType, T? defaultValue = default)
        {
            int index = IndexOfNamedArgument(namedArguments, name);
            return index >= 0 ? namedArguments[index].Value.DecodeValue<T>(specialType) : defaultValue;
        }

        private static int IndexOfNamedArgument(ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments, string name)
        {
            // For user defined attributes VB allows duplicate named arguments and uses the last value.
            // Dev11 reports an error for pseudo-custom attributes when emitting metadata. We don't.
            for (int i = namedArguments.Length - 1; i >= 0; i--)
            {
                // even for VB this is case sensitive comparison:
                if (string.Equals(namedArguments[i].Key, name, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        #region Decimal and DateTime Constant Decoding

        internal ConstantValue DecodeDecimalConstantValue()
        {
            // There are two decimal constant attribute ctors:
            // (byte scale, byte sign, uint high, uint mid, uint low) and
            // (byte scale, byte sign, int high, int mid, int low) 
            // The dev10 compiler only honors the first; Roslyn honours both.

            // We should not end up in this code path unless we know we have one of them.

            Debug.Assert(AttributeConstructor is object);
            var parameters = AttributeConstructor.Parameters;
            ImmutableArray<TypedConstant> args = this.CommonConstructorArguments;

            Debug.Assert(parameters.Length == 5);
            Debug.Assert(parameters[0].Type.SpecialType == SpecialType.System_Byte);
            Debug.Assert(parameters[1].Type.SpecialType == SpecialType.System_Byte);

            int low, mid, high;

            byte scale = args[0].DecodeValue<byte>(SpecialType.System_Byte);
            bool isNegative = args[1].DecodeValue<byte>(SpecialType.System_Byte) != 0;

            if (parameters[2].Type.SpecialType == SpecialType.System_Int32)
            {
                Debug.Assert(parameters[2].Type.SpecialType == SpecialType.System_Int32);
                Debug.Assert(parameters[3].Type.SpecialType == SpecialType.System_Int32);
                Debug.Assert(parameters[4].Type.SpecialType == SpecialType.System_Int32);

                high = args[2].DecodeValue<int>(SpecialType.System_Int32);
                mid = args[3].DecodeValue<int>(SpecialType.System_Int32);
                low = args[4].DecodeValue<int>(SpecialType.System_Int32);
            }
            else
            {
                Debug.Assert(parameters[2].Type.SpecialType == SpecialType.System_UInt32);
                Debug.Assert(parameters[3].Type.SpecialType == SpecialType.System_UInt32);
                Debug.Assert(parameters[4].Type.SpecialType == SpecialType.System_UInt32);

                high = unchecked((int)args[2].DecodeValue<uint>(SpecialType.System_UInt32));
                mid = unchecked((int)args[3].DecodeValue<uint>(SpecialType.System_UInt32));
                low = unchecked((int)args[4].DecodeValue<uint>(SpecialType.System_UInt32));
            }

            return ConstantValue.Create(new decimal(low, mid, high, isNegative, scale));
        }

        internal ConstantValue DecodeDateTimeConstantValue()
        {
            long value = this.CommonConstructorArguments[0].DecodeValue<long>(SpecialType.System_Int64);

            // if value is outside this range, DateTime would throw when constructed
            if (value < DateTime.MinValue.Ticks || value > DateTime.MaxValue.Ticks)
            {
                return ConstantValue.Bad;
            }

            return ConstantValue.Create(new DateTime(value));
        }

        #endregion

        internal ObsoleteAttributeData DecodeObsoleteAttribute(ObsoleteAttributeKind kind)
        {
            switch (kind)
            {
                case ObsoleteAttributeKind.Obsolete:
                    return DecodeObsoleteAttribute();
                case ObsoleteAttributeKind.Deprecated:
                    return DecodeDeprecatedAttribute();
                case ObsoleteAttributeKind.Experimental:
                    return DecodeExperimentalAttribute();
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        /// <summary>
        /// Decode the arguments to ObsoleteAttribute. ObsoleteAttribute can have 0, 1 or 2 arguments.
        /// </summary>
        private ObsoleteAttributeData DecodeObsoleteAttribute()
        {
            ImmutableArray<TypedConstant> args = this.CommonConstructorArguments;

            // ObsoleteAttribute() 
            string? message = null;
            bool isError = false;

            if (args.Length > 0)
            {
                // ObsoleteAttribute(string) 
                // ObsoleteAttribute(string, bool)

                Debug.Assert(args.Length <= 2);
                message = (string?)args[0].ValueInternal;

                if (args.Length == 2)
                {
                    Debug.Assert(args[1].ValueInternal is object);
                    isError = (bool)args[1].ValueInternal!;
                }
            }

            string? diagnosticId = null;
            string? urlFormat = null;
            foreach (var (name, value) in this.CommonNamedArguments)
            {
                if (diagnosticId is null && name == ObsoleteAttributeData.DiagnosticIdPropertyName && IsStringProperty(ObsoleteAttributeData.DiagnosticIdPropertyName))
                {
                    diagnosticId = value.ValueInternal as string;
                }
                else if (urlFormat is null && name == ObsoleteAttributeData.UrlFormatPropertyName && IsStringProperty(ObsoleteAttributeData.UrlFormatPropertyName))
                {
                    urlFormat = value.ValueInternal as string;
                }

                if (diagnosticId is object && urlFormat is object)
                {
                    break;
                }
            }

            return new ObsoleteAttributeData(ObsoleteAttributeKind.Obsolete, message, isError, diagnosticId, urlFormat);
        }

        // Note: it is disallowed to declare a property and a field
        // with the same name in C# or VB source, even if it is allowed in IL.
        //
        // We use a virtual method and override to prevent having to realize the public symbols just to decode obsolete attributes.
        // Ideally we would use an abstract method, but that would require making the method visible to
        // public consumers who inherit from this class, which we don't want to do.
        // Therefore we just make it a 'private protected virtual' method instead.
        private protected virtual bool IsStringProperty(string memberName) => throw ExceptionUtilities.Unreachable;

        /// <summary>
        /// Decode the arguments to DeprecatedAttribute. DeprecatedAttribute can have 3 or 4 arguments.
        /// </summary>
        private ObsoleteAttributeData DecodeDeprecatedAttribute()
        {
            var args = this.CommonConstructorArguments;

            // DeprecatedAttribute() 
            string? message = null;
            bool isError = false;

            if (args.Length == 3 || args.Length == 4)
            {
                // DeprecatedAttribute(String, DeprecationType, UInt32) 
                // DeprecatedAttribute(String, DeprecationType, UInt32, Platform) 
                // DeprecatedAttribute(String, DeprecationType, UInt32, String) 

                Debug.Assert(args[1].ValueInternal is object);
                message = (string?)args[0].ValueInternal;
                isError = ((int)args[1].ValueInternal! == 1);
            }

            return new ObsoleteAttributeData(ObsoleteAttributeKind.Deprecated, message, isError, diagnosticId: null, urlFormat: null);
        }

        /// <summary>
        /// Decode the arguments to ExperimentalAttribute. ExperimentalAttribute has 0 arguments.
        /// </summary>
        private ObsoleteAttributeData DecodeExperimentalAttribute()
        {
            // ExperimentalAttribute() 
            Debug.Assert(this.CommonConstructorArguments.Length == 0);
            return ObsoleteAttributeData.Experimental;
        }

        internal static void DecodeMethodImplAttribute<T, TAttributeSyntaxNode, TAttributeData, TAttributeLocation>(
            ref DecodeWellKnownAttributeArguments<TAttributeSyntaxNode, TAttributeData, TAttributeLocation> arguments,
            CommonMessageProvider messageProvider)
            where T : CommonMethodWellKnownAttributeData, new()
            where TAttributeSyntaxNode : SyntaxNode
            where TAttributeData : AttributeData
        {
            Debug.Assert(arguments.AttributeSyntaxOpt is object);

            MethodImplOptions options;
            var attribute = arguments.Attribute;
            if (attribute.CommonConstructorArguments.Length == 1)
            {
                Debug.Assert(attribute.AttributeConstructor is object);
                if (attribute.AttributeConstructor.Parameters[0].Type.SpecialType == SpecialType.System_Int16)
                {
                    options = (MethodImplOptions)attribute.CommonConstructorArguments[0].DecodeValue<short>(SpecialType.System_Int16);
                }
                else
                {
                    options = attribute.CommonConstructorArguments[0].DecodeValue<MethodImplOptions>(SpecialType.System_Enum);
                }

                // low two bits should only be set via MethodCodeType property
                if (((int)options & 3) != 0)
                {
                    messageProvider.ReportInvalidAttributeArgument(arguments.Diagnostics, arguments.AttributeSyntaxOpt, 0, attribute);
                    options = options & ~(MethodImplOptions)3;
                }
            }
            else
            {
                options = default;
            }

            MethodImplAttributes codeType = MethodImplAttributes.IL;
            int position = attribute.CommonConstructorArguments.Length;
            foreach (var namedArg in attribute.CommonNamedArguments)
            {
                if (namedArg.Key == "MethodCodeType")
                {
                    var value = (MethodImplAttributes)namedArg.Value.DecodeValue<int>(SpecialType.System_Enum);
                    if (value < 0 || value > MethodImplAttributes.Runtime)
                    {
                        Debug.Assert(attribute.AttributeClass is object);
                        messageProvider.ReportInvalidNamedArgument(arguments.Diagnostics, arguments.AttributeSyntaxOpt, position, attribute.AttributeClass, "MethodCodeType");
                    }
                    else
                    {
                        codeType = value;
                    }
                }

                position++;
            }

            arguments.GetOrCreateData<T>().SetMethodImplementation(arguments.Index, (MethodImplAttributes)options | codeType);
        }

        internal static void DecodeStructLayoutAttribute<TTypeWellKnownAttributeData, TAttributeSyntaxNode, TAttributeData, TAttributeLocation>(
            ref DecodeWellKnownAttributeArguments<TAttributeSyntaxNode, TAttributeData, TAttributeLocation> arguments,
            CharSet defaultCharSet,
            int defaultAutoLayoutSize,
            CommonMessageProvider messageProvider)
            where TTypeWellKnownAttributeData : CommonTypeWellKnownAttributeData, new()
            where TAttributeSyntaxNode : SyntaxNode
            where TAttributeData : AttributeData
        {
            Debug.Assert(arguments.AttributeSyntaxOpt is object);

            var attribute = arguments.Attribute;
            Debug.Assert(attribute.AttributeClass is object);

            CharSet charSet = (defaultCharSet != Cci.Constants.CharSet_None) ? defaultCharSet : CharSet.Ansi;
            int? size = null;
            int? alignment = null;
            bool hasErrors = false;

            LayoutKind kind = attribute.CommonConstructorArguments[0].DecodeValue<LayoutKind>(SpecialType.System_Enum);
            switch (kind)
            {
                case LayoutKind.Auto:
                case LayoutKind.Explicit:
                case LayoutKind.Sequential:
                    break;

                default:
                    messageProvider.ReportInvalidAttributeArgument(arguments.Diagnostics, arguments.AttributeSyntaxOpt, 0, attribute);
                    hasErrors = true;
                    break;
            }

            int position = attribute.CommonConstructorArguments.Length;
            foreach (var namedArg in attribute.CommonNamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "CharSet":
                        charSet = namedArg.Value.DecodeValue<CharSet>(SpecialType.System_Enum);
                        switch (charSet)
                        {
                            case Cci.Constants.CharSet_None:
                                charSet = CharSet.Ansi;
                                break;

                            case CharSet.Ansi:
                            case Cci.Constants.CharSet_Auto:
                            case CharSet.Unicode:
                                break;

                            default:
                                messageProvider.ReportInvalidNamedArgument(arguments.Diagnostics, arguments.AttributeSyntaxOpt, position, attribute.AttributeClass, namedArg.Key);
                                hasErrors = true;
                                break;
                        }

                        break;

                    case "Pack":
                        alignment = namedArg.Value.DecodeValue<int>(SpecialType.System_Int32);

                        // only powers of 2 less or equal to 128 are allowed:
                        if (alignment > 128 || (alignment & (alignment - 1)) != 0)
                        {
                            messageProvider.ReportInvalidNamedArgument(arguments.Diagnostics, arguments.AttributeSyntaxOpt, position, attribute.AttributeClass, namedArg.Key);
                            hasErrors = true;
                        }

                        break;

                    case "Size":
                        size = namedArg.Value.DecodeValue<int>(Microsoft.CodeAnalysis.SpecialType.System_Int32);
                        if (size < 0)
                        {
                            messageProvider.ReportInvalidNamedArgument(arguments.Diagnostics, arguments.AttributeSyntaxOpt, position, attribute.AttributeClass, namedArg.Key);
                            hasErrors = true;
                        }

                        break;
                }

                position++;
            }

            if (!hasErrors)
            {
                if (kind == LayoutKind.Auto && size == null && alignment != null)
                {
                    // If size is unspecified
                    //   C# emits size=0
                    //   VB emits size=1 if the type is a struct, auto-layout and has alignment specified; 0 otherwise
                    size = defaultAutoLayoutSize;
                }

                arguments.GetOrCreateData<TTypeWellKnownAttributeData>().SetStructLayout(new TypeLayout(kind, size ?? 0, (byte)(alignment ?? 0)), charSet);
            }
        }

        internal AttributeUsageInfo DecodeAttributeUsageAttribute()
        {
            return DecodeAttributeUsageAttribute(this.CommonConstructorArguments[0], this.CommonNamedArguments);
        }

        internal static AttributeUsageInfo DecodeAttributeUsageAttribute(TypedConstant positionalArg, ImmutableArray<KeyValuePair<string, TypedConstant>> namedArgs)
        {
            // BREAKING CHANGE (C#):
            //   If the well known attribute class System.AttributeUsage is overridden in source,
            //   we will use the overriding AttributeUsage type for attribute usage validation,
            //   i.e. we try to find a constructor in that type with signature AttributeUsage(AttributeTargets)
            //   and public bool properties Inherited and AllowMultiple.
            //   If we are unable to find any of these, we use their default values.
            //   
            //   This behavior matches the approach chosen by native VB and Roslyn VB compilers,
            //   but breaks compatibility with native C# compiler.
            //   Native C# compiler preloads all the well known attribute types from mscorlib prior to binding,
            //   hence it uses the AttributeUsage type defined in mscorlib for attribute usage validation.
            //   
            //   See Roslyn Bug 8603: ETA crashes with InvalidOperationException on duplicate attributes for details.

            Debug.Assert(positionalArg.ValueInternal is object);
            var validOn = (AttributeTargets)positionalArg.ValueInternal;
            bool allowMultiple = DecodeNamedArgument(namedArgs, "AllowMultiple", SpecialType.System_Boolean, false);
            bool inherited = DecodeNamedArgument(namedArgs, "Inherited", SpecialType.System_Boolean, true);

            return new AttributeUsageInfo(validOn, allowMultiple, inherited);
        }
    }
}
