using System;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// Describes the formatting rules that should be used while generating the description of a symbol.
    /// </summary>
    public class SymbolDisplayFormat
    {
        /// <summary>
        /// Standard format for displaying symbols in compiler error messages.
        /// </summary>
        public static readonly SymbolDisplayFormat ErrorMessageFormat = 
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                propertyStyle: SymbolDisplayPropertyStyle.GetSet,
                localStyle: SymbolDisplayLocalStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeContainingType,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeExtensionThisParameter |
                    SymbolDisplayParameterOptions.IncludeType,
                // Not showing the name is important because we visit parameters to display their
                // types.  If we visited their types directly, we wouldn't get ref/out/params.
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                    SymbolDisplayMiscellaneousOptions.UseAsterisksInMultiDimensionalArrays);

        /// <summary>
        /// A verbose format for displaying symbols (useful for testing).
        /// </summary>
        internal static readonly SymbolDisplayFormat TestFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                propertyStyle: SymbolDisplayPropertyStyle.GetSet,
                localStyle: SymbolDisplayLocalStyle.NameAndType,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions: 
                    SymbolDisplayMemberOptions.IncludeParameters | 
                    SymbolDisplayMemberOptions.IncludeContainingType | 
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeExtensionThisParameter |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeName,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers,
                compilerInternalOptions:
                    SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames |
                    SymbolDisplayCompilerInternalOptions.FlagMissingMetadataTypes);

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
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.HumanReadable,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                localStyle: SymbolDisplayLocalStyle.NameOnly,
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
                localStyle: SymbolDisplayLocalStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeType,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes,
                compilerInternalOptions: SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames);

        //backing fields
        private readonly SymbolDisplayGlobalNamespaceStyle globalNamespaceStyle;
        private readonly SymbolDisplayTypeQualificationStyle typeQualificationStyle;
        private readonly SymbolDisplayGenericsOptions genericsOptions;
        private readonly SymbolDisplayMemberOptions memberOptions;
        private readonly SymbolDisplayParameterOptions parameterOptions;
        private readonly SymbolDisplayPropertyStyle propertyStyle;
        private readonly SymbolDisplayLocalStyle localStyle;
        private readonly SymbolDisplayMiscellaneousOptions miscellaneousOptions;
        private readonly SymbolDisplayCompilerInternalOptions compilerInternalOptions;

        public SymbolDisplayFormat(
            SymbolDisplayGlobalNamespaceStyle globalNamespaceStyle = default(SymbolDisplayGlobalNamespaceStyle),
            SymbolDisplayTypeQualificationStyle typeQualificationStyle = default(SymbolDisplayTypeQualificationStyle),
            SymbolDisplayGenericsOptions genericsOptions = default(SymbolDisplayGenericsOptions),
            SymbolDisplayMemberOptions memberOptions = default(SymbolDisplayMemberOptions),
            SymbolDisplayParameterOptions parameterOptions = default(SymbolDisplayParameterOptions),
            SymbolDisplayPropertyStyle propertyStyle = default(SymbolDisplayPropertyStyle),
            SymbolDisplayLocalStyle localStyle = default(SymbolDisplayLocalStyle),
            SymbolDisplayMiscellaneousOptions miscellaneousOptions = default(SymbolDisplayMiscellaneousOptions))
            : this(
                default(SymbolDisplayCompilerInternalOptions),
                globalNamespaceStyle,
                typeQualificationStyle,
                genericsOptions,
                memberOptions,
                parameterOptions,
                propertyStyle,
                localStyle,
                miscellaneousOptions)
        {
        }

        internal SymbolDisplayFormat(
            SymbolDisplayCompilerInternalOptions compilerInternalOptions,
            SymbolDisplayGlobalNamespaceStyle globalNamespaceStyle = default(SymbolDisplayGlobalNamespaceStyle),
            SymbolDisplayTypeQualificationStyle typeQualificationStyle = default(SymbolDisplayTypeQualificationStyle),
            SymbolDisplayGenericsOptions genericsOptions = default(SymbolDisplayGenericsOptions),
            SymbolDisplayMemberOptions memberOptions = default(SymbolDisplayMemberOptions),
            SymbolDisplayParameterOptions parameterOptions = default(SymbolDisplayParameterOptions),
            SymbolDisplayPropertyStyle propertyStyle = default(SymbolDisplayPropertyStyle),
            SymbolDisplayLocalStyle localStyle = default(SymbolDisplayLocalStyle),
            SymbolDisplayMiscellaneousOptions miscellaneousOptions = default(SymbolDisplayMiscellaneousOptions))
        {
            this.globalNamespaceStyle = globalNamespaceStyle;
            this.typeQualificationStyle = typeQualificationStyle;
            this.genericsOptions = genericsOptions;
            this.memberOptions = memberOptions;
            this.parameterOptions = parameterOptions;
            this.propertyStyle = propertyStyle;
            this.localStyle = localStyle;
            this.miscellaneousOptions = miscellaneousOptions;
            this.compilerInternalOptions = compilerInternalOptions;
        }

        /// <summary>
        /// How to display references to the global namespace.
        /// </summary>
        public SymbolDisplayGlobalNamespaceStyle GlobalNamespaceStyle { get { return globalNamespaceStyle; } }

        /// <summary>
        /// How types are qualified (e.g. Nested vs Containing.Nested vs Namespace.Containing.Nested).
        /// </summary>
        public SymbolDisplayTypeQualificationStyle TypeQualificationStyle { get { return typeQualificationStyle; } }

        /// <summary>
        /// How generics (on types and methods) should be described (i.e. level of detail).
        /// </summary>
        public SymbolDisplayGenericsOptions GenericsOptions { get { return genericsOptions; } }

        /// <summary>
        /// Formatting options that apply to fields, properties, and methods.
        /// </summary>
        public SymbolDisplayMemberOptions MemberOptions { get { return memberOptions; } }

        /// <summary>
        /// Formatting options that apply to mathod and indexer parameters (i.e. level of detail).
        /// </summary>
        public SymbolDisplayParameterOptions ParameterOptions { get { return parameterOptions; } }

        /// <summary>
        /// How properties are displayed (i.e. Prop vs Prop { get; set; })
        /// </summary>
        public SymbolDisplayPropertyStyle PropertyStyle { get { return propertyStyle; } }

        /// <summary>
        /// How local variables are displayed
        /// </summary>
        public SymbolDisplayLocalStyle LocalStyle { get { return localStyle; } }

        /// <summary>
        /// Miscellaneous formatting options.
        /// </summary>
        public SymbolDisplayMiscellaneousOptions MiscellaneousOptions { get { return miscellaneousOptions; } }

        /// <summary>
        /// Flags that can only be set within the compiler.
        /// </summary>
        internal SymbolDisplayCompilerInternalOptions CompilerInternalOptions { get { return compilerInternalOptions; } }
    }

    public enum SymbolDisplayGlobalNamespaceStyle
    {
        /// <summary>
        /// Unless it is required for QualificationStyle.ShortestUnambiguous, omit the global namespace.
        /// </summary>
        Omitted,

        /// <summary>
        /// global::
        /// </summary>
        Code,

        /// <summary>
        /// &lt;global namespace&gt; (localized)
        /// </summary>
        HumanReadable,
    }

    public enum SymbolDisplayTypeQualificationStyle
    {
        /// <summary>
        /// ex) Class1
        /// </summary>
        NameOnly,

        /// <summary>
        /// ParentClass.NestedClass
        /// </summary>
        NameAndContainingTypes,

        /// <summary>
        /// Namespace1.Namespace2.Class1.Class2
        /// </summary>
        NameAndContainingTypesAndNamespaces,

        /// <summary>
        /// Best name at current context
        /// </summary>
        Minimal,
    }

    [Flags]
    public enum SymbolDisplayGenericsOptions
    {
        /// <summary>
        /// Omit generics entirely.
        /// </summary>
        None = 0,

        /// <summary>
        /// Type parameters (e.g. Foo&lt;T&gt;).
        /// </summary>
        IncludeTypeParameters = 1 << 0,

        /// <summary>
        /// Type parameter constraints (e.g. where T : new()).
        /// </summary>
        IncludeTypeConstraints = 1 << 1,

        /// <summary>
        /// Use out/in before type parameter if it has one (e.g. Foo&lt;out T&gt;).
        /// </summary>
        IncludeVariance = 1 << 2,
    }

    [Flags]
    public enum SymbolDisplayMemberOptions
    {
        /// <summary>
        /// Display only the name of the member.
        /// </summary>
        None = 0,

        /// <summary>
        /// Include the (return) type of the method/field/property.
        /// </summary>
        IncludeType = 1 << 0,

        /// <summary>
        /// Include modifiers (e.g. static, readonly).
        /// </summary>
        IncludeModifiers = 1 << 1,

        /// <summary>
        /// Include accessibility (e.g. public).
        /// </summary>
        IncludeAccessibility = 1 << 2,

        /// <summary>
        /// Indicate properties and methods that explicitly implement interfaces (e.g. IFoo.Bar { get; }).
        /// </summary>
        IncludeExplicitInterface = 1 << 3,

        /// <summary>
        /// Include method/indexer parameters.  (See ParameterFlags for fine-grained settings.)
        /// </summary>
        IncludeParameters = 1 << 4,

        /// <summary>
        /// Include the name of the containing type.
        /// </summary>
        IncludeContainingType = 1 << 5,
    }

    [Flags]
    public enum SymbolDisplayParameterOptions
    {
        /// <summary>
        /// If MemberFlags.IncludeParameters is set, but this value is used, then only the
        /// parens will be shown (e.g. M()).
        /// </summary>
        None = 0,

        /// <summary>
        /// Include the this keyword before the first parameter of an extension method.
        /// </summary>
        IncludeExtensionThisParameter = 1 << 0,

        /// <summary>
        /// Include the params/ref/out keyword before params/ref/out parameters (no effect if the type is not included).
        /// </summary>
        IncludeParamsRefOut = 1 << 1,

        /// <summary>
        /// Include the parameter type.
        /// </summary>
        IncludeType = 1 << 2,

        /// <summary>
        /// Include the parameter name.
        /// </summary>
        IncludeName = 1 << 3,

        /// <summary>
        /// Include the parameter default value (no effect if the name is not included).
        /// </summary>
        IncludeDefaultValue = 1 << 4,
    }

    public enum SymbolDisplayPropertyStyle
    {
        /// <summary>
        /// Only show the name of the property (formatted using MemberFlags).
        /// </summary>
        NameOnly,

        /// <summary>
        /// Show the getter and/or setter of the property.
        /// </summary>
        GetSet,
    }

    public enum SymbolDisplayLocalStyle
    {
        /// <summary>
        /// Only show the name of the local (e.g. "x").
        /// </summary>
        NameOnly,

        /// <summary>
        /// Show the name and the type of the local (e.g. "int x").
        /// </summary>
        NameAndType,
    }

    [Flags]
    public enum SymbolDisplayMiscellaneousOptions
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,

        /// <summary>
        /// Use keywords for predefined types("int?" instead of "System.Nullable&lt;System.Int32&gt;")
        /// </summary>
        UseSpecialTypes = 1 << 0,

        /// <summary>
        /// If the typeref is aliased to something, use that name instead.
        /// </summary>
        UseAliases = 1 << 1,

        /// <summary>
        /// "@true" instead of "true"
        /// </summary>
        EscapeKeywordIdentifiers = 1 << 2,

        /// <summary>
        /// "int[][*,*]" instead of "int[][,]"
        /// </summary>
        UseAsterisksInMultiDimensionalArrays = 1 << 3,
    }

    [Flags]
    internal enum SymbolDisplayCompilerInternalOptions
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,

        /// <summary>
        /// ".ctor" instead of "Foo"
        /// </summary>
        UseMetadataMethodNames = 1 << 0,

        /// <summary>
        /// "List`1" instead of "List&lt;T&gt;"
        /// Overrides GenericsOptions on types
        /// </summary>
        UseArityForGenericTypes = 1 << 1,

        /// <summary>
        /// Append "[Missing]" to missing Metadata types (for testing).
        /// </summary>
        FlagMissingMetadataTypes = 1 << 2,
    }
}
