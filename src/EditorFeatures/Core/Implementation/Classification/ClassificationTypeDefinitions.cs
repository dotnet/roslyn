// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    internal sealed class ClassificationTypeDefinitions
    {
        #region Preprocessor Text 
        [Export]
        [Name(ClassificationTypeNames.PreprocessorText)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal ClassificationTypeDefinition PreprocessorTextTypeDefinition { get; set; }
        #endregion
        #region Punctuation
        [Export]
        [Name(ClassificationTypeNames.Punctuation)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal ClassificationTypeDefinition PunctuationTypeDefinition;
        #endregion
        #region String - Verbatim
        [Export]
        [Name(ClassificationTypeNames.VerbatimStringLiteral)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition StringVerbatimTypeDefinition;

        [Export]
        [Name(ClassificationTypeNames.StringEscapeCharacter)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition StringEscapeCharacterTypeDefinition;
        #endregion
        #region Keyword - Control
        // Keyword - Control sets its BaseDefinitions to be Keyword so that
        // in the absence of specific styling they will appear as keywords.  
        [Export]
        [Name(ClassificationTypeNames.ControlKeyword)]
        [BaseDefinition(PredefinedClassificationTypeNames.Keyword)]
        internal ClassificationTypeDefinition ControlKeywordTypeDefinition;
        #endregion

        #region User Types - Classes
        [Export]
        [Name(ClassificationTypeNames.ClassName)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition UserTypeClassesTypeDefinition;
        #endregion
        #region User Types - Delegates 
        [Export]
        [Name(ClassificationTypeNames.DelegateName)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition UserTypeDelegatesTypeDefinition;
        #endregion
        #region User Types - Enums 
        [Export]
        [Name(ClassificationTypeNames.EnumName)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition UserTypeEnumsTypeDefinition;
        #endregion
        #region User Types - Interfaces 
        [Export]
        [Name(ClassificationTypeNames.InterfaceName)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition UserTypeInterfacesTypeDefinition;
        #endregion
        #region User Types - Modules 
        [Export]
        [Name(ClassificationTypeNames.ModuleName)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition UserTypeModulesTypeDefinition;
        #endregion
        #region User Types - Structures 
        [Export]
        [Name(ClassificationTypeNames.StructName)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition UserTypeStructuresTypeDefinition;
        #endregion
        #region User Types - Type Parameters 
        [Export]
        [Name(ClassificationTypeNames.TypeParameterName)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition UserTypeTypeParametersTypeDefinition;
        #endregion

        // User Members - * set their BaseDefinitions to be Identifier so that
        // in the absence of specific styling they will appear as identifiers. 
        // Extension Methods are an exception and their base definition is Method
        // since it is a more specific type of method.
        #region User Members - Fields
        [Export]
        [Name(ClassificationTypeNames.FieldName)]
        [BaseDefinition(PredefinedClassificationTypeNames.Identifier)]
        internal readonly ClassificationTypeDefinition UserMembersFieldsTypeDefinition;
        #endregion
        #region User Members - Enum Memberd
        [Export]
        [Name(ClassificationTypeNames.EnumMemberName)]
        [BaseDefinition(PredefinedClassificationTypeNames.Identifier)]
        internal readonly ClassificationTypeDefinition UserMembersEnumMembersTypeDefinition;
        #endregion
        #region User Members - Constants
        [Export]
        [Name(ClassificationTypeNames.ConstantName)]
        [BaseDefinition(PredefinedClassificationTypeNames.Identifier)]
        internal readonly ClassificationTypeDefinition UserMembersConstantsTypeDefinition;
        #endregion
        #region User Memebers - Locals
        [Export]
        [Name(ClassificationTypeNames.LocalName)]
        [BaseDefinition(PredefinedClassificationTypeNames.Identifier)]
        internal readonly ClassificationTypeDefinition UserMembersLocalsTypeDefinition;
        #endregion
        #region User Members - Parameters
        [Export]
        [Name(ClassificationTypeNames.ParameterName)]
        [BaseDefinition(PredefinedClassificationTypeNames.Identifier)]
        internal readonly ClassificationTypeDefinition UserMembersParametersTypeDefinition;
        #endregion
        #region User Members - Methods
        [Export]
        [Name(ClassificationTypeNames.MethodName)]
        [BaseDefinition(PredefinedClassificationTypeNames.Identifier)]
        internal readonly ClassificationTypeDefinition UserMembersMethodsTypeDefinition;
        #endregion
        #region User Members - Extension Methods
        [Export]
        [Name(ClassificationTypeNames.ExtensionMethodName)]
        [BaseDefinition(ClassificationTypeNames.MethodName)]
        internal readonly ClassificationTypeDefinition UserMembersExtensionMethodsTypeDefinition;
        #endregion
        #region User Members - Properties
        [Export]
        [Name(ClassificationTypeNames.PropertyName)]
        [BaseDefinition(PredefinedClassificationTypeNames.Identifier)]
        internal readonly ClassificationTypeDefinition UserMembersPropertiesTypeDefinition;
        #endregion
        #region User Members - Events
        [Export]
        [Name(ClassificationTypeNames.EventName)]
        [BaseDefinition(PredefinedClassificationTypeNames.Identifier)]
        internal readonly ClassificationTypeDefinition UserMembersEventsTypeDefinition;
        #endregion
        #region User Members - Namespaces
        [Export]
        [Name(ClassificationTypeNames.NamespaceName)]
        [BaseDefinition(PredefinedClassificationTypeNames.Identifier)]
        internal readonly ClassificationTypeDefinition UserMembersNamespacesTypeDefinition;
        #endregion
        #region User Members - Labels
        [Export]
        [Name(ClassificationTypeNames.LabelName)]
        [BaseDefinition(PredefinedClassificationTypeNames.Identifier)]
        internal readonly ClassificationTypeDefinition UserMembersLabelsTypeDefinition;
        #endregion

        #region XML Doc Comments - Attribute Name 
        [Export]
        [Name(ClassificationTypeNames.XmlDocCommentAttributeName)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlDocCommentAttributeNameTypeDefinition;
        #endregion
        #region XML Doc Comments - Attribute Quotes 
        [Export]
        [Name(ClassificationTypeNames.XmlDocCommentAttributeQuotes)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlDocCommentAttributeQuotesTypeDefinition;
        #endregion
        #region XML Doc Comments - Attribute Value 
        [Export]
        [Name(ClassificationTypeNames.XmlDocCommentAttributeValue)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlDocCommentAttributeValueTypeDefinition;
        #endregion
        #region XML Doc Comments - CData Section 
        [Export]
        [Name(ClassificationTypeNames.XmlDocCommentCDataSection)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlDocCommentCDataSectionTypeDefinition;
        #endregion
        #region XML Doc Comments - Comment 
        [Export]
        [Name(ClassificationTypeNames.XmlDocCommentComment)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlDocCommentCommentTypeDefinition;
        #endregion
        #region XML Doc Comments - Delimiter 
        [Export]
        [Name(ClassificationTypeNames.XmlDocCommentDelimiter)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlDocCommentDelimiterTypeDefinition;
        #endregion
        #region XML Doc Comments - Entity Reference
        [Export]
        [Name(ClassificationTypeNames.XmlDocCommentEntityReference)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlDocCommentEntityReferenceTypeDefinition;
        #endregion
        #region XML Doc Comments - Name
        [Export]
        [Name(ClassificationTypeNames.XmlDocCommentName)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlDocCommentNameTypeDefinition;
        #endregion
        #region XML Doc Comments - Processing Instruction
        [Export]
        [Name(ClassificationTypeNames.XmlDocCommentProcessingInstruction)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlDocCommentProcessingInstructionTypeDefinition;
        #endregion
        #region XML Doc Comments - Text 
        [Export]
        [Name(ClassificationTypeNames.XmlDocCommentText)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlDocCommentTextTypeDefinition;
        #endregion

        #region Regex
        [Export]
        [Name(ClassificationTypeNames.RegexComment)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition RegexCommentTypeDefinition;

        [Export]
        [Name(ClassificationTypeNames.RegexText)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition RegexTextTypeDefinition;

        [Export]
        [Name(ClassificationTypeNames.RegexCharacterClass)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition RegexCharacterClassTypeDefinition;

        [Export]
        [Name(ClassificationTypeNames.RegexQuantifier)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition RegexQuantifierTypeDefinition;

        [Export]
        [Name(ClassificationTypeNames.RegexAnchor)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition RegexAnchorTypeDefinition;

        [Export]
        [Name(ClassificationTypeNames.RegexAlternation)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition RegexAlternationTypeDefinition;

        [Export]
        [Name(ClassificationTypeNames.RegexOtherEscape)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition RegexOtherEscapeTypeDefinition;

        [Export]
        [Name(ClassificationTypeNames.RegexSelfEscapedCharacter)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition RegexSelfEscapedCharacterTypeDefinition;

        [Export]
        [Name(ClassificationTypeNames.RegexGrouping)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition RegexGroupingTypeDefinition;
        #endregion

        #region VB XML Literals - Attribute Name 
        [Export]
        [Name(ClassificationTypeNames.XmlLiteralAttributeName)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlLiteralAttributeNameTypeDefinition;
        #endregion
        #region VB XML Literals - Attribute Quotes 
        [Export]
        [Name(ClassificationTypeNames.XmlLiteralAttributeQuotes)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlLiteralAttributeQuotesTypeDefinition;
        #endregion
        #region VB XML Literals - Attribute Value 
        [Export]
        [Name(ClassificationTypeNames.XmlLiteralAttributeValue)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlLiteralAttributeValueTypeDefinition;
        #endregion
        #region VB XML Literals - CData Section 
        [Export]
        [Name(ClassificationTypeNames.XmlLiteralCDataSection)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlLiteralCDataSectionTypeDefinition;
        #endregion
        #region VB XML Literals - Comment 
        [Export]
        [Name(ClassificationTypeNames.XmlLiteralComment)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlLiteralCommentTypeDefinition;
        #endregion
        #region VB XML Literals - Delimiter 
        [Export]
        [Name(ClassificationTypeNames.XmlLiteralDelimiter)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlLiteralDelimiterTypeDefinition;
        #endregion
        #region VB XML Literals - Embedded Expression 
        [Export]
        [Name(ClassificationTypeNames.XmlLiteralEmbeddedExpression)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlLiteralEmbeddedExpressionTypeDefinition;
        #endregion
        #region VB XML Literals - Entity Reference 
        [Export]
        [Name(ClassificationTypeNames.XmlLiteralEntityReference)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlLiteralEntityReferenceTypeDefinition;
        #endregion
        #region VB XML Literals - Name 
        [Export]
        [Name(ClassificationTypeNames.XmlLiteralName)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlLiteralNameTypeDefinition;
        #endregion
        #region VB XML Literals - Processing Instruction 
        [Export]
        [Name(ClassificationTypeNames.XmlLiteralProcessingInstruction)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlLiteralProcessingInstructionTypeDefinition;
        #endregion
        #region VB XML Literals - Text 
        [Export]
        [Name(ClassificationTypeNames.XmlLiteralText)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlLiteralTextTypeDefinition;
        #endregion

        #region Static Symbol
        [Export]
        [Name(ClassificationTypeNames.StaticSymbol)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition StaticSymbolTypeDefinition;
        #endregion

        #region Operator - Overloaded
        // Operator - Overloaded sets its BaseDefinitions to be Operator so that
        // in the absence of specific styling they will appear as operators.  
        [Export]
        [Name(ClassificationTypeNames.OperatorOverloaded)]
        [BaseDefinition(PredefinedClassificationTypeNames.Operator)]
        internal readonly ClassificationTypeDefinition OperatorOverloadTypeDefinition;
        #endregion
    }
}
