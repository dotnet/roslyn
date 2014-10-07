#if false
using System.ComponentModel.Composition;

namespace Roslyn.Services.Classification
{
    [Export(typeof(IClassificationTypes))]
    internal class ClassificationTypes : IClassificationTypes
    {
        public string DocumentationCommentXmlAttributeName { get; private set; }
        public string DocumentationCommentXmlAttributeQuotes { get; private set; }
        public string DocumentationCommentXmlAttributeValue { get; private set; }
        public string DocumentationCommentXmlCDataSection { get; private set; }
        public string DocumentationCommentXmlComment { get; private set; }
        public string DocumentationCommentXmlDelimiter { get; private set; }
        public string DocumentationCommentXmlText { get; private set; }

        public string XmlAttributeName { get; private set; }
        public string XmlAttributeQuotes { get; private set; }
        public string XmlAttributeValue { get; private set; }
        public string XmlCDataSection { get; private set; }
        public string XmlComment { get; private set; }
        public string XmlDelimiter { get; private set; }
        public string XmlText { get; private set; }

        public string XmlEmbeddedExpression { get; private set; }
        public string XmlEntityReference { get; private set; }
        public string XmlName { get; private set; }
        public string XmlProcessingInstruction { get; private set; }

        public string PreprocessorKeyword { get; private set; }
        public string PreprocessorText { get; private set; }

        public string ClassName { get; private set; }
        public string DelegateName { get; private set; }
        public string EnumName { get; private set; }
        public string InterfaceName { get; private set; }
        public string ModuleName { get; private set; }
        public string StructName { get; private set; }
        public string TypeParameterName { get; private set; }

        public string Comment { get; private set; }
        public string ExcludedCode { get; private set; }
        public string Identifier { get; private set; }
        public string Keyword { get; private set; }
        public string NumericLiteral { get; private set; }
        public string StringLiteral { get; private set; }
        public string WhiteSpace { get; private set; }
        public string Operator { get; private set; }
        public string Punctuation { get; private set; }
        public string VerbatimStringLiteral { get; private set; }

        public string UnnecessaryCode { get; private set; }

        [ImportingConstructor]
        public ClassificationTypes()
        {
            this.Comment = ClassificationTypeNames.Comment;
            this.ExcludedCode = ClassificationTypeNames.ExcludedCode;
            this.Identifier = ClassificationTypeNames.Identifier;
            this.Keyword = ClassificationTypeNames.Keyword;
            this.NumericLiteral = ClassificationTypeNames.NumericLiteral;
            this.StringLiteral = ClassificationTypeNames.StringLiteral;
            this.WhiteSpace = ClassificationTypeNames.WhiteSpace;

            this.Operator = ClassificationTypeNames.Operator;
            this.Punctuation = ClassificationTypeNames.Punctuation;
            this.VerbatimStringLiteral = ClassificationTypeNames.VerbatimStringLiteral;

            this.DocumentationCommentXmlAttributeName = ClassificationTypeNames.DocumentationCommentXmlAttributeName;
            this.DocumentationCommentXmlAttributeQuotes = ClassificationTypeNames.DocumentationCommentXmlAttributeQuotes;
            this.DocumentationCommentXmlAttributeValue = ClassificationTypeNames.DocumentationCommentXmlAttributeValue;
            this.DocumentationCommentXmlCDataSection = ClassificationTypeNames.DocumentationCommentXmlCDataSection;
            this.DocumentationCommentXmlComment = ClassificationTypeNames.DocumentationCommentXmlComment;
            this.DocumentationCommentXmlDelimiter = ClassificationTypeNames.DocumentationCommentXmlDelimiter;
            this.DocumentationCommentXmlText = ClassificationTypeNames.DocumentationCommentXmlText;

            this.XmlAttributeName = ClassificationTypeNames.XmlAttributeName;
            this.XmlAttributeQuotes = ClassificationTypeNames.XmlAttributeQuotes;
            this.XmlAttributeValue = ClassificationTypeNames.XmlAttributeValue;
            this.XmlCDataSection = ClassificationTypeNames.XmlCDataSection;
            this.XmlComment = ClassificationTypeNames.XmlComment;
            this.XmlDelimiter = ClassificationTypeNames.XmlDelimiter;
            this.XmlText = ClassificationTypeNames.XmlText;

            this.XmlEmbeddedExpression = ClassificationTypeNames.XmlEmbeddedExpression;
            this.XmlEntityReference = ClassificationTypeNames.XmlEntityReference;
            this.XmlName = ClassificationTypeNames.XmlName;
            this.XmlProcessingInstruction = ClassificationTypeNames.XmlProcessingInstruction;

            this.PreprocessorKeyword = ClassificationTypeNames.PreprocessorKeyword;
            this.PreprocessorText = ClassificationTypeNames.PreprocessorText;

            this.ClassName = ClassificationTypeNames.ClassName;
            this.StructName = ClassificationTypeNames.StructName;
            this.InterfaceName = ClassificationTypeNames.InterfaceName;
            this.DelegateName = ClassificationTypeNames.DelegateName;
            this.EnumName = ClassificationTypeNames.EnumName;
            this.TypeParameterName = ClassificationTypeNames.TypeParameterName;
            this.ModuleName = ClassificationTypeNames.ModuleName;

            this.UnnecessaryCode = ClassificationTypeNames.UnnecessaryCode;
        }
    }
}
#endif