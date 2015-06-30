﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Describes the formatting rules that should be used when displaying symbols.
    /// </summary>
    public class SymbolDisplayFormat
    {
        /// <summary>
        /// Formats a symbol description as in a C# compiler error message.
        /// </summary>
        public static SymbolDisplayFormat CSharpErrorMessageFormat { get; } =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeType,
                // Not showing the name is important because we visit parameters to display their
                // types.  If we visited their types directly, we wouldn't get ref/out/params.
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                    SymbolDisplayMiscellaneousOptions.UseAsterisksInMultiDimensionalArrays |
                    SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName);

        /// <summary>
        /// Formats a symbol description as in a C# compiler short error message.
        /// </summary>
        public static SymbolDisplayFormat CSharpShortErrorMessageFormat { get; } =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeType,
                // Not showing the name is important because we visit parameters to display their
                // types.  If we visited their types directly, we wouldn't get ref/out/params.
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                    SymbolDisplayMiscellaneousOptions.UseAsterisksInMultiDimensionalArrays |
                    SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName);

        /// <summary>
        /// Formats a symbol description as in a Visual Basic compiler error message.
        /// </summary>
        public static SymbolDisplayFormat VisualBasicErrorMessageFormat { get; } =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                genericsOptions:
                    SymbolDisplayGenericsOptions.IncludeTypeParameters |
                    SymbolDisplayGenericsOptions.IncludeTypeConstraints |
                    SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeModifiers,
                kindOptions:
                    SymbolDisplayKindOptions.IncludeMemberKeyword,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeExtensionThis |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeName |
                    SymbolDisplayParameterOptions.IncludeOptionalBrackets |
                    SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                    SymbolDisplayMiscellaneousOptions.UseAsterisksInMultiDimensionalArrays |
                    SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName);

        /// <summary>
        /// Formats a symbol description as in a Visual Basic compiler short error message.
        /// </summary>
        public static SymbolDisplayFormat VisualBasicShortErrorMessageFormat { get; } =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                genericsOptions:
                    SymbolDisplayGenericsOptions.IncludeTypeParameters |
                    SymbolDisplayGenericsOptions.IncludeTypeConstraints |
                    SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeModifiers,
                kindOptions:
                    SymbolDisplayKindOptions.IncludeMemberKeyword,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeExtensionThis |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeName |
                    SymbolDisplayParameterOptions.IncludeOptionalBrackets |
                    SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                    SymbolDisplayMiscellaneousOptions.UseAsterisksInMultiDimensionalArrays |
                    SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName);

        /// <summary>
        /// Formats the names of all types and namespaces in a fully qualified style (including the global alias).
        /// </summary>
        public static SymbolDisplayFormat FullyQualifiedFormat { get; } =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        /// <summary>
        /// Formats a symbol description in a form that suits <see cref="ISymbol.ToMinimalDisplayString"/>.
        /// </summary>
        public static SymbolDisplayFormat MinimallyQualifiedFormat { get; } =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeContainingType,
                kindOptions:
                    SymbolDisplayKindOptions.IncludeMemberKeyword,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeName |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeDefaultValue,
                localOptions: SymbolDisplayLocalOptions.IncludeType,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        /// <summary>
        /// A verbose format for displaying symbols (useful for testing).
        /// </summary>
        internal static readonly SymbolDisplayFormat TestFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                localOptions: SymbolDisplayLocalOptions.IncludeType,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface,
                kindOptions:
                    SymbolDisplayKindOptions.IncludeMemberKeyword,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeOptionalBrackets |
                    SymbolDisplayParameterOptions.IncludeDefaultValue |
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeExtensionThis |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeName,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName,
                compilerInternalOptions:
                    SymbolDisplayCompilerInternalOptions.IncludeScriptType |
                    SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames |
                    SymbolDisplayCompilerInternalOptions.FlagMissingMetadataTypes |
                    SymbolDisplayCompilerInternalOptions.IncludeCustomModifiers);

        /// <summary>
        /// this.QualifiedNameOnly = containingSymbol.QualifiedNameOnly + "." + this.Name
        /// </summary>
        internal static readonly SymbolDisplayFormat QualifiedNameOnlyFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        /// <summary>
        /// this.QualifiedNameArity = containingSymbol.QualifiedNameArity + "." + this.Name + "`" + this.Arity
        /// </summary>
        internal static readonly SymbolDisplayFormat QualifiedNameArityFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                compilerInternalOptions: SymbolDisplayCompilerInternalOptions.UseArityForGenericTypes);

        /// <summary>
        /// A succinct format for displaying symbols.
        /// </summary>
        internal static readonly SymbolDisplayFormat ShortFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                parameterOptions: SymbolDisplayParameterOptions.IncludeName,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        /// <summary>
        /// The format used for displaying symbols when visualizing IL.
        /// </summary>
        internal static readonly SymbolDisplayFormat ILVisualizationFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                memberOptions: SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType,
                kindOptions: SymbolDisplayKindOptions.IncludeMemberKeyword,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeType,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes,
                compilerInternalOptions: SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames);

        /// <summary>
        /// Used to normalize explicit interface implementation member names.
        /// Only expected to be applied to interface types (and their type arguments).
        /// </summary>
        internal static readonly SymbolDisplayFormat ExplicitInterfaceImplementationFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers,
                compilerInternalOptions: SymbolDisplayCompilerInternalOptions.ReverseArrayRankSpecifiers);

        /// <summary>
        /// Determines how the global namespace is displayed.
        /// </summary>
        public SymbolDisplayGlobalNamespaceStyle GlobalNamespaceStyle { get; }

        /// <summary>
        /// Determines how types are qualified (e.g. Nested vs Containing.Nested vs Namespace.Containing.Nested).
        /// </summary>
        public SymbolDisplayTypeQualificationStyle TypeQualificationStyle { get; }

        /// <summary>
        /// Determines how generics (on types and methods) should be described (i.e. the level of detail).
        /// </summary>
        public SymbolDisplayGenericsOptions GenericsOptions { get; }

        /// <summary>
        /// Determines how fields, properties, events, and methods are displayed.
        /// </summary>
        public SymbolDisplayMemberOptions MemberOptions { get; }

        /// <summary>
        /// Determines how parameters (of methods, properties/indexers, and delegates) are displayed.
        /// </summary>
        public SymbolDisplayParameterOptions ParameterOptions { get; }

        /// <summary>
        /// Determines how delegates are displayed (e.g. name vs full signature).
        /// </summary>
        public SymbolDisplayDelegateStyle DelegateStyle { get; }

        /// <summary>
        /// Determines how extension methods are displayed.
        /// </summary>
        public SymbolDisplayExtensionMethodStyle ExtensionMethodStyle { get; }

        /// <summary>
        /// Determines how properties are displayed. 
        /// For example, "Prop" vs "Prop { get; set; }" in C# or "Prop" vs. "ReadOnly Prop" in Visual Basic.
        /// </summary>
        public SymbolDisplayPropertyStyle PropertyStyle { get; }

        /// <summary>
        /// Determines how local variables are displayed.
        /// </summary>
        public SymbolDisplayLocalOptions LocalOptions { get; }

        /// <summary>
        /// Determines which kind keywords should be included when displaying symbols.
        /// </summary>
        public SymbolDisplayKindOptions KindOptions { get; }

        /// <summary>
        /// Determines other characteristics of how symbols are displayed.
        /// </summary>
        public SymbolDisplayMiscellaneousOptions MiscellaneousOptions { get; }

        /// <summary>
        /// Flags that can only be set within the compiler.
        /// </summary>
        internal SymbolDisplayCompilerInternalOptions CompilerInternalOptions { get; }

        /// <summary>
        /// Constructs a new instance of <see cref="SymbolDisplayFormat"/> accepting a variety of optional parameters.
        /// </summary>
        /// <param name="globalNamespaceStyle">
        /// The settings that determine how the global namespace is displayed.
        /// </param>
        /// <param name="typeQualificationStyle">
        /// The settings that determine how types are qualified (e.g. Nested vs Containing.Nested vs Namespace.Containing.Nested).
        /// </param>
        /// <param name="genericsOptions">
        /// The settings that determine how generics (on types and methods) should be described (i.e. the level of detail).
        /// </param>
        /// <param name="memberOptions">
        /// The settings that determine how fields, properties, events, and methods are displayed.
        /// </param>
        /// <param name="delegateStyle">
        /// The settings that determine how delegates are displayed (e.g. name vs full signature).
        /// </param>
        /// <param name="extensionMethodStyle">
        /// The settings that determine how extension methods are displayed.
        /// </param>
        /// <param name="parameterOptions">
        /// The settings that determine how parameters (of methods, properties/indexers, and delegates) are displayed.
        /// </param>
        /// <param name="propertyStyle">
        /// The settings that determine how properties are displayed. 
        /// For example, "Prop" vs "Prop { get; set; }" in C# or "Prop" vs. "ReadOnly Prop" in Visual Basic.
        /// </param>
        /// <param name="localOptions">
        /// The settings that determine how local variables are displayed.
        /// </param>
        /// <param name="kindOptions">
        /// The settings that determine which kind keywords should be included when displaying symbols.
        /// </param>
        /// <param name="miscellaneousOptions">
        /// The settings that determine other characteristics of how symbols are displayed.
        /// </param>
        public SymbolDisplayFormat(
            SymbolDisplayGlobalNamespaceStyle globalNamespaceStyle = default(SymbolDisplayGlobalNamespaceStyle),
            SymbolDisplayTypeQualificationStyle typeQualificationStyle = default(SymbolDisplayTypeQualificationStyle),
            SymbolDisplayGenericsOptions genericsOptions = default(SymbolDisplayGenericsOptions),
            SymbolDisplayMemberOptions memberOptions = default(SymbolDisplayMemberOptions),
            SymbolDisplayDelegateStyle delegateStyle = default(SymbolDisplayDelegateStyle),
            SymbolDisplayExtensionMethodStyle extensionMethodStyle = default(SymbolDisplayExtensionMethodStyle),
            SymbolDisplayParameterOptions parameterOptions = default(SymbolDisplayParameterOptions),
            SymbolDisplayPropertyStyle propertyStyle = default(SymbolDisplayPropertyStyle),
            SymbolDisplayLocalOptions localOptions = default(SymbolDisplayLocalOptions),
            SymbolDisplayKindOptions kindOptions = default(SymbolDisplayKindOptions),
            SymbolDisplayMiscellaneousOptions miscellaneousOptions = default(SymbolDisplayMiscellaneousOptions))
            : this(
                default(SymbolDisplayCompilerInternalOptions),
                globalNamespaceStyle,
                typeQualificationStyle,
                genericsOptions,
                memberOptions,
                parameterOptions,
                delegateStyle,
                extensionMethodStyle,
                propertyStyle,
                localOptions,
                kindOptions,
                miscellaneousOptions)
        {
        }

        /// <summary>
        /// This version also accepts <see cref="SymbolDisplayCompilerInternalOptions"/>.
        /// </summary>
        internal SymbolDisplayFormat(
            SymbolDisplayCompilerInternalOptions compilerInternalOptions,
            SymbolDisplayGlobalNamespaceStyle globalNamespaceStyle = default(SymbolDisplayGlobalNamespaceStyle),
            SymbolDisplayTypeQualificationStyle typeQualificationStyle = default(SymbolDisplayTypeQualificationStyle),
            SymbolDisplayGenericsOptions genericsOptions = default(SymbolDisplayGenericsOptions),
            SymbolDisplayMemberOptions memberOptions = default(SymbolDisplayMemberOptions),
            SymbolDisplayParameterOptions parameterOptions = default(SymbolDisplayParameterOptions),
            SymbolDisplayDelegateStyle delegateStyle = default(SymbolDisplayDelegateStyle),
            SymbolDisplayExtensionMethodStyle extensionMethodStyle = default(SymbolDisplayExtensionMethodStyle),
            SymbolDisplayPropertyStyle propertyStyle = default(SymbolDisplayPropertyStyle),
            SymbolDisplayLocalOptions localOptions = default(SymbolDisplayLocalOptions),
            SymbolDisplayKindOptions kindOptions = default(SymbolDisplayKindOptions),
            SymbolDisplayMiscellaneousOptions miscellaneousOptions = default(SymbolDisplayMiscellaneousOptions))
        {
            this.GlobalNamespaceStyle = globalNamespaceStyle;
            this.TypeQualificationStyle = typeQualificationStyle;
            this.GenericsOptions = genericsOptions;
            this.MemberOptions = memberOptions;
            this.ParameterOptions = parameterOptions;
            this.DelegateStyle = delegateStyle;
            this.ExtensionMethodStyle = extensionMethodStyle;
            this.PropertyStyle = propertyStyle;
            this.LocalOptions = localOptions;
            this.KindOptions = kindOptions;
            this.MiscellaneousOptions = miscellaneousOptions;
            this.CompilerInternalOptions = compilerInternalOptions;
        }

        /// <summary>
        /// Creates a copy of the SymbolDisplayFormat but with replaced set of <seealso cref="SymbolDisplayMiscellaneousOptions"/>.
        /// </summary>
        /// <param name="options">
        /// An object representing how miscellaneous symbols will be formatted.
        /// </param>
        /// <returns>A duplicate of the SymbolDisplayFormat, with a replaced set of <seealso cref="SymbolDisplayMiscellaneousOptions"/>.</returns>
        public SymbolDisplayFormat WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions options)
        {
            return new SymbolDisplayFormat(
                this.GlobalNamespaceStyle,
                this.TypeQualificationStyle,
                this.GenericsOptions,
                this.MemberOptions,
                this.DelegateStyle,
                this.ExtensionMethodStyle,
                this.ParameterOptions,
                this.PropertyStyle,
                this.LocalOptions,
                this.KindOptions,
                options
            );
        }

        /// <summary>
        /// Creates a copy of the SymbolDisplayFormat but with an additional set of <seealso cref="SymbolDisplayMiscellaneousOptions"/>.
        /// </summary>
        /// <param name="options">
        /// An object specifying additional parameters for how miscellaneous symbols will be formatted.
        /// </param>
        /// <returns>A duplicate of the SymbolDisplayFormat, with an additional set of <seealso cref="SymbolDisplayMiscellaneousOptions"/>.</returns>
        public SymbolDisplayFormat AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions options)
        {
            return this.WithMiscellaneousOptions(this.MiscellaneousOptions | options);
        }

        /// <summary>
        /// Creates a copy of the SymbolDisplayFormat but with replaced set of <seealso cref="SymbolDisplayGenericsOptions"/>.
        /// </summary>
        /// <param name="options">
        /// An object specifying how generic symbols will be formatted.
        /// </param>
        /// <returns>A duplicate of the SymbolDisplayFormat, with a replaced set of <seealso cref="SymbolDisplayGenericsOptions"/>.</returns>
        public SymbolDisplayFormat WithGenericsOptions(SymbolDisplayGenericsOptions options)
        {
            return new SymbolDisplayFormat(
                this.GlobalNamespaceStyle,
                this.TypeQualificationStyle,
                options,
                this.MemberOptions,
                this.DelegateStyle,
                this.ExtensionMethodStyle,
                this.ParameterOptions,
                this.PropertyStyle,
                this.LocalOptions,
                this.KindOptions,
                this.MiscellaneousOptions);
        }

        /// <summary>
        /// Creates a copy of the SymbolDisplayFormat but with an additional set of <seealso cref="SymbolDisplayGenericsOptions"/>.
        /// </summary>
        /// <param name="options">
        /// An object specifying additional parameters for how generic symbols will be formatted.
        /// </param>
        /// <returns>A duplicate of the SymbolDisplayFormat, with an additional set of <seealso cref="SymbolDisplayGenericsOptions"/>.</returns>
        public SymbolDisplayFormat AddGenericsOptions(SymbolDisplayGenericsOptions options)
        {
            return this.WithGenericsOptions(this.GenericsOptions | options);
        }

        /// <summary>
        /// Creates a copy of the SymbolDisplayFormat but with replaced set of <seealso cref="SymbolDisplayMemberOptions"/>.
        /// </summary>
        /// <param name="options">
        /// An object specifying how members will be formatted.
        /// </param>
        /// <returns>A duplicate of the SymbolDisplayFormat, with a replaced set of <seealso cref="SymbolDisplayMemberOptions"/>.</returns>
        public SymbolDisplayFormat WithMemberOptions(SymbolDisplayMemberOptions options)
        {
            return new SymbolDisplayFormat(
                this.GlobalNamespaceStyle,
                this.TypeQualificationStyle,
                this.GenericsOptions,
                options,
                this.DelegateStyle,
                this.ExtensionMethodStyle,
                this.ParameterOptions,
                this.PropertyStyle,
                this.LocalOptions,
                this.KindOptions,
                this.MiscellaneousOptions);
        }

        /// <summary>
        /// Creates a copy of the SymbolDisplayFormat but with an additional set of <seealso cref="SymbolDisplayMemberOptions"/>.
        /// </summary>
        /// <param name="options">
        /// An object specifying additional parameters for how members will be formatted.
        /// </param>
        /// <returns>
        /// A duplicate of the SymbolDisplayFormat, with an additional set of <seealso cref="SymbolDisplayMemberOptions"/>.
        /// </returns>
        public SymbolDisplayFormat AddMemberOptions(SymbolDisplayMemberOptions options)
        {
            return this.WithMemberOptions(this.MemberOptions | options);
        }

        /// <summary>
        /// Creates a copy of the SymbolDisplayFormat but with a set of <seealso cref="SymbolDisplayMemberOptions"/> stripped away from the original object.
        /// </summary>
        /// <param name="options">
        /// An object specifying which parameters should not be applied to how members will be formatted.
        /// </param>
        /// <returns>
        /// A duplicate of the SymbolDisplayFormat, with a set of <seealso cref="SymbolDisplayMemberOptions"/> stripped away from the original object.
        /// </returns>
        public SymbolDisplayFormat RemoveMemberOptions(SymbolDisplayMemberOptions options)
        {
            return this.WithMemberOptions(this.MemberOptions & ~options);
        }

        /// <summary>
        /// Creates a copy of the SymbolDisplayFormat but with replaced set of <seealso cref="SymbolDisplayKindOptions"/>.
        /// </summary>
        /// <param name="options">
        /// An object specifying parameters with which symbols belonging to kind keywords should be formatted.
        /// </param>
        /// <returns>
        /// A duplicate of the SymbolDisplayFormat, with a replaced set of <seealso cref="SymbolDisplayKindOptions"/>.
        /// </returns>
        public SymbolDisplayFormat WithKindOptions(SymbolDisplayKindOptions options)
        {
            return new SymbolDisplayFormat(
                this.GlobalNamespaceStyle,
                this.TypeQualificationStyle,
                this.GenericsOptions,
                this.MemberOptions,
                this.DelegateStyle,
                this.ExtensionMethodStyle,
                this.ParameterOptions,
                this.PropertyStyle,
                this.LocalOptions,
                options,
                this.MiscellaneousOptions);
        }

        /// <summary>
        /// Creates a copy of the SymbolDisplayFormat but with an additional set of <seealso cref="SymbolDisplayKindOptions"/>.
        /// </summary>
        /// <param name="options">
        /// An object specifying additional parameters with which symbols belonging to kind keywords should be formatted.
        /// </param>
        /// <returns>
        /// A duplicate of the SymbolDisplayFormat, with an additional set of <seealso cref="SymbolDisplayKindOptions"/>.
        /// </returns>
        public SymbolDisplayFormat AddKindOptions(SymbolDisplayKindOptions options)
        {
            return this.WithKindOptions(this.KindOptions | options);
        }

        /// <summary>
        /// Creates a copy of the SymbolDisplayFormat but with a set of <seealso cref="SymbolDisplayKindOptions"/> stripped away from the original object.
        /// </summary>
        /// <param name="options">
        /// The settings that determine other characteristics of how symbols are displayed.
        /// </param>
        /// <returns>
        /// A duplicate of the SymbolDisplayFormat, with a set of <seealso cref="SymbolDisplayKindOptions"/> stripped away from the original object.
        /// </returns>
        public SymbolDisplayFormat RemoveKindOptions(SymbolDisplayKindOptions options)
        {
            return this.WithKindOptions(this.KindOptions & ~options);
        }

        /// <summary>
        /// Creates a copy of the SymbolDisplayFormat but with replaced set of <seealso cref="SymbolDisplayParameterOptions"/>.
        /// </summary>
        /// <param name="options">
        /// An object specifying how parameters should be formatted.
        /// </param>
        /// <returns>A duplicate of the SymbolDisplayFormat, with a replaced set of <seealso cref="SymbolDisplayParameterOptions"/>.</returns>
        public SymbolDisplayFormat WithParameterOptions(SymbolDisplayParameterOptions options)
        {
            return new SymbolDisplayFormat(
                this.GlobalNamespaceStyle,
                this.TypeQualificationStyle,
                this.GenericsOptions,
                this.MemberOptions,
                this.DelegateStyle,
                this.ExtensionMethodStyle,
                options,
                this.PropertyStyle,
                this.LocalOptions,
                this.KindOptions,
                this.MiscellaneousOptions);
        }

        /// <summary>
        /// Creates a copy of the SymbolDisplayFormat but with an additional set of <seealso cref="SymbolDisplayParameterOptions"/>.
        /// </summary>
        /// <param name="options">
        /// An object specifying additional parameters on how parameters should be formatted.
        /// </param>
        /// <returns>
        /// A duplicate of the SymbolDisplayFormat, with an additional set of <seealso cref="SymbolDisplayParameterOptions"/>.
        /// </returns>
        public SymbolDisplayFormat AddParameterOptions(SymbolDisplayParameterOptions options)
        {
            return this.WithParameterOptions(this.ParameterOptions | options);
        }

        /// <summary>
        /// Creates a copy of the SymbolDisplayFormat but with a set of <seealso cref="SymbolDisplayParameterOptions"/> stripped away from the original object.
        /// </summary>
        /// <param name="options">
        /// An object specifying parameters that should not be applied when formatting parameters.
        /// </param>
        /// <returns>
        /// A duplicate of the SymbolDisplayFormat, with a set of <seealso cref="SymbolDisplayParameterOptions"/> stripped away from the original object.
        /// </returns>
        public SymbolDisplayFormat RemoveParameterOptions(SymbolDisplayParameterOptions options)
        {
            return this.WithParameterOptions(this.ParameterOptions & ~options);
        }

        /// <summary>
        /// Creates a copy of the SymbolDisplayFormat but with replaced <seealso cref="SymbolDisplayGlobalNamespaceStyle"/>.
        /// </summary>
        /// <param name="style">
        /// An object specifying parameters on how namespace symbols should be formatted.
        /// </param>
        /// <returns>A duplicate of the SymbolDisplayFormat, with a replaced set of <seealso cref="SymbolDisplayGlobalNamespaceStyle"/>.</returns>
        public SymbolDisplayFormat WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle style)
        {
            return new SymbolDisplayFormat(
                style,
                this.TypeQualificationStyle,
                this.GenericsOptions,
                this.MemberOptions,
                this.DelegateStyle,
                this.ExtensionMethodStyle,
                this.ParameterOptions,
                this.PropertyStyle,
                this.LocalOptions,
                this.KindOptions,
                this.MiscellaneousOptions);
        }

        /// <summary>
        /// Creates a copy of the SymbolDisplayFormat but with replaced set of <seealso cref="SymbolDisplayLocalOptions"/>.
        /// </summary>
        /// <param name="options">
        /// An object specifying parameters on how symbols belonging to locals should be formatted.
        /// </param>
        /// <returns>A duplicate of the SymbolDisplayFormat, with a replaced set of <seealso cref="SymbolDisplayLocalOptions"/>.</returns>
        public SymbolDisplayFormat WithLocalOptions(SymbolDisplayLocalOptions options)
        {
            return new SymbolDisplayFormat(
                this.GlobalNamespaceStyle,
                this.TypeQualificationStyle,
                this.GenericsOptions,
                this.MemberOptions,
                this.DelegateStyle,
                this.ExtensionMethodStyle,
                this.ParameterOptions,
                this.PropertyStyle,
                options,
                this.KindOptions,
                this.MiscellaneousOptions);
        }

        /// <summary>
        /// Creates a copy of the SymbolDisplayFormat but with an additional set of <seealso cref="SymbolDisplayParameterOptions"/>.
        /// </summary>
        /// <param name="options">
        /// An object specifying additional parameters on how symbols belonging to locals should be formatted.
        /// </param>
        /// <returns>
        /// A duplicate of the SymbolDisplayFormat, with an additional set of <seealso cref="SymbolDisplayParameterOptions"/>.
        /// </returns>
        public SymbolDisplayFormat AddLocalOptions(SymbolDisplayLocalOptions options)
        {
            return this.WithLocalOptions(this.LocalOptions | options);
        }
    }
}
