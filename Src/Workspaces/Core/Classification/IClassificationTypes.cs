#if false
namespace Roslyn.Services
{
    /// <summary>
    /// Provides access to standard language classifications.
    /// </summary>
    public interface IClassificationTypes
    {
        /// <summary>
        /// The classification for attribute names in documentation comments.
        /// </summary>
        string DocumentationCommentXmlAttributeName { get; }

        /// <summary>
        /// The classification for attribute quotes in documentation comments.
        /// </summary>
        string DocumentationCommentXmlAttributeQuotes { get; }

        /// <summary>
        /// The classification for attribute values in documentation comments.
        /// </summary>
        string DocumentationCommentXmlAttributeValue { get; }

        /// <summary>
        /// The classification for text in documentation comments.
        /// </summary>
        string DocumentationCommentXmlText { get; }

        /// <summary>
        /// The classification for xml delimiter tags in documentation comments.
        /// </summary>
        string DocumentationCommentXmlDelimiter { get; }

        /// <summary>
        /// The classification for xml comments in documentation comments.
        /// </summary>
        string DocumentationCommentXmlComment { get; }

        /// <summary>
        /// The classification for CData sections in documentation comments.
        /// </summary>
        string DocumentationCommentXmlCDataSection { get; }

        /// <summary>
        /// The classification for Comments.
        /// </summary>
        string Comment { get; }

        /// <summary>
        /// The classification for Disabled Code.
        /// </summary>
        string ExcludedCode { get; }

        /// <summary>
        /// The classification for identifiers
        /// </summary>
        string Identifier { get; }

        /// <summary>
        /// The classification for Keywords
        /// </summary>
        string Keyword { get; }

        /// <summary>
        /// The classification for Numerical literals.
        /// </summary>
        string NumericLiteral { get; }

        /// <summary>
        /// The classification for String Literals
        /// </summary>
        string StringLiteral { get; }

        /// <summary>
        /// The classification for Operators
        /// </summary>
        string Operator { get; }

        /// <summary>
        /// The classification for White Space.
        /// </summary>
        string WhiteSpace { get; }

        /// <summary>
        /// The classification for Punctuation
        /// </summary>
        string Punctuation { get; }

        /// <summary>
        /// The classification for Preprocessor keywords
        /// </summary>
        string PreprocessorKeyword { get; }

        /// <summary>
        /// The classification for arbitrary text in a preprocessor directive (e.g. #region text)
        /// </summary>
        string PreprocessorText { get; }

        /// <summary>
        /// The classification for Verbatim strings
        /// </summary>
        string VerbatimStringLiteral { get; }

        /// <summary>
        /// The classification for User types.
        /// </summary>
        string ClassName { get; }

        /// <summary>
        /// The classification for Structure types.
        /// </summary>
        string StructName { get; }

        /// <summary>
        /// The classification for Interface types.
        /// </summary>
        string InterfaceName { get; }

        /// <summary>
        /// The classification for Delegate types
        /// </summary>
        string DelegateName { get; }

        /// <summary>
        /// The classification for Enum types.
        /// </summary>
        string EnumName { get; }

        /// <summary>
        /// The classification for Type parameters.
        /// </summary>
        string TypeParameterName { get; }

        /// <summary>
        /// The classification for Module types.
        /// </summary>
        string ModuleName { get; }

        /// <summary>
        /// The classification for XML names.
        /// </summary>
        string XmlName { get; }

        /// <summary>
        /// The classification for XML text.
        /// </summary>
        string XmlText { get; }

        /// <summary>
        /// The classification for XML processing instructions.
        /// </summary>
        string XmlProcessingInstruction { get; }

        /// <summary>
        /// The classification for XML embedded expressions.
        /// </summary>
        string XmlEmbeddedExpression { get; }

        /// <summary>
        /// The classification for XML delimiters.
        /// </summary>
        string XmlDelimiter { get; }

        /// <summary>
        /// The classification for XML comments.
        /// </summary>
        string XmlComment { get; }

        /// <summary>
        /// The classification for XML CData sections.
        /// </summary>
        string XmlCDataSection { get; }

        /// <summary>
        /// The classification for XML attribute values.
        /// </summary>
        string XmlAttributeValue { get; }

        /// <summary>
        /// The classification for XML attribute quotes.
        /// </summary>
        string XmlAttributeQuotes { get; }

        /// <summary>
        /// The classification for XML attribute names.
        /// </summary>
        string XmlAttributeName { get; }

        /// <summary>
        /// The classification for XML entity references.
        /// </summary>
        string XmlEntityReference { get; }

        /// <summary>
        /// The classification for code that unnecessary and can be removed.
        /// </summary>
        string UnnecessaryCode { get; }
    }
}
#endif