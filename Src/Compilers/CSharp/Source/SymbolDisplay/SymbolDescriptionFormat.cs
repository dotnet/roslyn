using System;

namespace Roslyn.Compilers.CSharp.Descriptions
{
    public struct SymbolDescriptionFormat
    {
        public QualificationStyle TypeQualificationStyle { get; internal set; }
        public GenericsFlags GenericsFlags { get; internal set; }
        public MemberFlags MemberFlags { get; internal set; }
        public ParameterFlags ParameterFlags { get; internal set; }
        public PropertyStyle PropertyStyle { get; internal set; }
        public PrettyPrintingFlags PrettyPrintingFlags { get; internal set; }
    }

    public enum QualificationStyle
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
        ShortestUnambiguous,
    }

    [Flags]
    public enum GenericsFlags
    {
        None = 0,
        IncludeTypeParameters = 1 << 0,
        IncludeTypeConstraints = 1 << 1,

        /// <summary>
        /// Use out/in before type parameter if it has one
        /// </summary>
        IncludeVariance = 1 << 2,
    }

    [Flags]
    public enum MemberFlags
    {
        None = 0,
        IncludeType = 1 << 0,
        IncludeModifiers = 1 << 1,
        IncludeAccessibility = 1 << 2,
        IncludeExplicitInterface = 1 << 3,
        IncludeParameters = 1 << 4,
        //TODO: containing type?
    }

    [Flags]
    public enum ParameterFlags
    {
        None = 0,
        IncludeExtensionThisParameter = 1 << 0,
        IncludeRefOut = 1 << 1,
        IncludeType = 1 << 2,
        IncludeName = 1 << 3,
        IncludeDefaultValue = 1 << 4,
    }

    public enum PropertyStyle
    {
        NameOnly,
        GetSet,
    }

    [Flags]
    public enum PrettyPrintingFlags
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,

        /// <summary>
        /// Use keywords for predefined types("int" instead of "System.Int32")
        /// </summary>
        UseSpecialTypeKeywords = 1 << 0,

        /// <summary>
        /// If the typeref is aliased to something, use that name instead.
        /// </summary>
        UseAliases = 1 << 1,

        /// <summary>
        /// use: "System.Nullable&lt;int&gt;" not "int?"
        /// </summary>
        AbbreviateNullable = 1 << 2,

        /// <summary>
        /// "@true" instead of "true"
        /// </summary>
        EscapeKeywordIdentifiers = 1 << 3,
    }
}
