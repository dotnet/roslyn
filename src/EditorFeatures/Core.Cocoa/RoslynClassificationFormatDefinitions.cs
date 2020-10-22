// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    internal sealed class ClassificationTypeFormatDefinitions
    {
        #region Control Keyword
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.ControlKeyword)]
        [Name(ClassificationTypeNames.ControlKeyword)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class ControlKeywordFormatDefinition : ClassificationFormatDefinition
        {
            private ControlKeywordFormatDefinition ()
            {
                this.DisplayName = EditorFeaturesResources.Keyword_Control;
                this.ForegroundColor = Color.FromRgb(0x8F, 0x08, 0xC4);
            }
        }
        #endregion

        #region Local Name
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.LocalName)]
        [Name(ClassificationTypeNames.LocalName)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class LocalNameFormatDefinition : ClassificationFormatDefinition
        {
            private LocalNameFormatDefinition ()
            {
                this.DisplayName = EditorFeaturesResources.User_Members_Locals;
                this.ForegroundColor = Color.FromRgb(0x1F, 0x37, 0x7F);
            }
        }
        #endregion

        #region Method Name
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.MethodName)]
        [Name(ClassificationTypeNames.MethodName)]
        [Order(After = PredefinedClassificationTypeNames.Identifier)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class MethodNameFormatDefinition : ClassificationFormatDefinition
        {
            private MethodNameFormatDefinition ()
            {
                this.DisplayName = EditorFeaturesResources.User_Members_Methods;
                this.ForegroundColor = Color.FromRgb(0x74, 0x53, 0x1F);
            }
        }
        #endregion

        #region Operator Overloaded
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.OperatorOverloaded)]
        [Name(ClassificationTypeNames.OperatorOverloaded)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class OperatorOverloadedFormatDefinition : ClassificationFormatDefinition
        {
            private OperatorOverloadedFormatDefinition ()
            {
                this.DisplayName = EditorFeaturesResources.Operator_Overloaded;
                this.ForegroundColor = Color.FromRgb(0x74, 0x53, 0x1F);
            }
        }
        #endregion

        #region Parameter Name
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.ParameterName)]
        [Name(ClassificationTypeNames.ParameterName)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class ParameterNameFormatDefinition : ClassificationFormatDefinition
        {
            private ParameterNameFormatDefinition ()
            {
                this.DisplayName = EditorFeaturesResources.User_Members_Parameters;
                this.ForegroundColor = Color.FromRgb(0x1F, 0x37, 0x7F);
            }
        }
        #endregion

        #region Preprocessor Text
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.PreprocessorText)]
        [Name(ClassificationTypeNames.PreprocessorText)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class PreprocessorTextFormatDefinition : ClassificationFormatDefinition
        {
            private PreprocessorTextFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.Preprocessor_Text;
                this.ForegroundColor = Colors.Black;
            }
        }
        #endregion
        #region Punctuation
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.Punctuation)]
        [Name(ClassificationTypeNames.Punctuation)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class PunctuationFormatDefinition : ClassificationFormatDefinition
        {
            private PunctuationFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.Punctuation;
                this.ForegroundColor = Colors.Black;
            }
        }
        #endregion

        #region Regex Alternation
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexAlternation)]
        [Name(ClassificationTypeNames.RegexAlternation)]
        [Order(After = PredefinedClassificationTypeNames.String)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class RegexAlternationFormatDefinition : ClassificationFormatDefinition
        {
            private RegexAlternationFormatDefinition()
            {
                this.DisplayName = ClassificationTypeNames.RegexAlternation;
                this.ForegroundColor = Colors.Black;
            }
        }
        #endregion

        #region Regex Anchor
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexAnchor)]
        [Name(ClassificationTypeNames.RegexAnchor)]
        [Order(After = PredefinedClassificationTypeNames.String)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class RegexAnchorFormatDefinition : ClassificationFormatDefinition
        {
            private RegexAnchorFormatDefinition()
            {
                this.DisplayName = ClassificationTypeNames.RegexAnchor;
                this.ForegroundColor = Colors.Black;
            }
        }
        #endregion

        #region Regex Character Class
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexCharacterClass)]
        [Name(ClassificationTypeNames.RegexCharacterClass)]
        [Order(After = PredefinedClassificationTypeNames.String)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class RegexCharacterClassFormatDefinition : ClassificationFormatDefinition
        {
            private RegexCharacterClassFormatDefinition()
            {
                this.DisplayName = ClassificationTypeNames.RegexCharacterClass;
                this.ForegroundColor = Colors.Black;
            }
        }
        #endregion

        #region Regex Comment
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexComment)]
        [Name(ClassificationTypeNames.RegexComment)]
        [Order(After = PredefinedClassificationTypeNames.String)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class RegexCommentFormatDefinition : ClassificationFormatDefinition
        {
            private RegexCommentFormatDefinition()
            {
                this.DisplayName = ClassificationTypeNames.RegexComment;
                this.ForegroundColor = Colors.Black;
            }
        }
        #endregion

        #region Regex Grouping
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexGrouping)]
        [Name(ClassificationTypeNames.RegexGrouping)]
        [Order(After = PredefinedClassificationTypeNames.String)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class RegexGroupingFormatDefinition : ClassificationFormatDefinition
        {
            private RegexGroupingFormatDefinition()
            {
                this.DisplayName = ClassificationTypeNames.RegexGrouping;
                this.ForegroundColor = Colors.Black;
            }
        }
        #endregion

        #region Regex Other Escape
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexOtherEscape)]
        [Name(ClassificationTypeNames.RegexOtherEscape)]
        [Order(After = PredefinedClassificationTypeNames.String)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class RegexOtherEscapeFormatDefinition : ClassificationFormatDefinition
        {
            private RegexOtherEscapeFormatDefinition()
            {
                this.DisplayName = ClassificationTypeNames.RegexOtherEscape;
                this.ForegroundColor = Colors.Black;
            }
        }
        #endregion

        #region Regex Quantifier
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexQuantifier)]
        [Name(ClassificationTypeNames.RegexQuantifier)]
        [Order(After = PredefinedClassificationTypeNames.String)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class RegexQuantifierFormatDefinition : ClassificationFormatDefinition
        {
            private RegexQuantifierFormatDefinition()
            {
                this.DisplayName = ClassificationTypeNames.RegexQuantifier;
                this.ForegroundColor = Colors.Black;
            }
        }
        #endregion

        #region Regex Self Escaped Character
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexSelfEscapedCharacter)]
        [Name(ClassificationTypeNames.RegexSelfEscapedCharacter)]
        [Order(After = PredefinedClassificationTypeNames.String)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class RegexSelfEscapedCharacterFormatDefinition : ClassificationFormatDefinition
        {
            private RegexSelfEscapedCharacterFormatDefinition()
            {
                this.DisplayName = ClassificationTypeNames.RegexSelfEscapedCharacter;
                this.ForegroundColor = Colors.Black;
            }
        }
        #endregion

        #region Regex Text
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexText)]
        [Name(ClassificationTypeNames.RegexText)]
        [Order(After = PredefinedClassificationTypeNames.String)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class RegexTextFormatDefinition : ClassificationFormatDefinition
        {
            private RegexTextFormatDefinition()
            {
                this.DisplayName = ClassificationTypeNames.RegexText;
                this.ForegroundColor = Colors.Black;
            }
        }
        #endregion

        #region String - Escape Character
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.StringEscapeCharacter)]
        [Name(ClassificationTypeNames.StringEscapeCharacter)]
        [Order(After = PredefinedClassificationTypeNames.String)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class StringEscapeCharacterFormatDefinition : ClassificationFormatDefinition
        {
            private StringEscapeCharacterFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.String_Escape_Character;
                this.ForegroundColor = Colors.LightYellow;
            }
        }
        #endregion

        #region String - Verbatim
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.VerbatimStringLiteral)]
        [Name(ClassificationTypeNames.VerbatimStringLiteral)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class StringVerbatimFormatDefinition : ClassificationFormatDefinition
        {
            private StringVerbatimFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.String_Verbatim;
                this.ForegroundColor = Colors.Maroon;
            }
        }
        #endregion

        #region User Types - Classes
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.ClassName)]
        [Name(ClassificationTypeNames.ClassName)]
        [Order(After = PredefinedClassificationTypeNames.Identifier)]
        [Order(After = PredefinedClassificationTypeNames.Keyword)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class UserTypeClassesFormatDefinition : ClassificationFormatDefinition
        {
            private UserTypeClassesFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.User_Types_Classes;
                this.ForegroundColor = Color.FromRgb(0x2B, 0x91, 0xAF);
            }
        }
        #endregion
        #region User Types - Delegates
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.DelegateName)]
        [Name(ClassificationTypeNames.DelegateName)]
        [Order(After = PredefinedClassificationTypeNames.Identifier)]
        [Order(After = PredefinedClassificationTypeNames.Keyword)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class UserTypeDelegatesFormatDefinition : ClassificationFormatDefinition
        {
            private UserTypeDelegatesFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.User_Types_Delegates;
                this.ForegroundColor = Color.FromRgb(0x2B, 0x91, 0xAF);
            }
        }
        #endregion
        #region User Types - Enums
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.EnumName)]
        [Name(ClassificationTypeNames.EnumName)]
        [Order(After = PredefinedClassificationTypeNames.Identifier)]
        [Order(After = PredefinedClassificationTypeNames.Keyword)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class UserTypeEnumsFormatDefinition : ClassificationFormatDefinition
        {
            private UserTypeEnumsFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.User_Types_Enums;
                this.ForegroundColor = Color.FromRgb(0x2B, 0x91, 0xAF);
            }
        }
        #endregion
        #region User Types - Interfaces
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.InterfaceName)]
        [Name(ClassificationTypeNames.InterfaceName)]
        [Order(After = PredefinedClassificationTypeNames.Identifier)]
        [Order(After = PredefinedClassificationTypeNames.Keyword)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class UserTypeInterfacesFormatDefinition : ClassificationFormatDefinition
        {
            private UserTypeInterfacesFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.User_Types_Interfaces;
                this.ForegroundColor = Color.FromRgb(0x2B, 0x91, 0xAF);
            }
        }
        #endregion
        #region User Types - Modules
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.ModuleName)]
        [Name(ClassificationTypeNames.ModuleName)]
        [Order(After = PredefinedClassificationTypeNames.Identifier)]
        [Order(After = PredefinedClassificationTypeNames.Keyword)]
        [UserVisible(true)]
        private class UserTypeModulesFormatDefinition : ClassificationFormatDefinition
        {
            private UserTypeModulesFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.User_Types_Modules;
                this.ForegroundColor = Color.FromRgb(43, 145, 175);
            }
        }
        #endregion
        #region User Types - Structures
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.StructName)]
        [Name(ClassificationTypeNames.StructName)]
        [Order(After = PredefinedClassificationTypeNames.Identifier)]
        [Order(After = PredefinedClassificationTypeNames.Keyword)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class UserTypeStructuresFormatDefinition : ClassificationFormatDefinition
        {
            private UserTypeStructuresFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.User_Types_Structures;
                this.ForegroundColor = Color.FromRgb(0x2B, 0x91, 0xAF);
            }
        }
        #endregion
        #region User Types - Type Parameters
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.TypeParameterName)]
        [Name(ClassificationTypeNames.TypeParameterName)]
        [Order(After = PredefinedClassificationTypeNames.Identifier)]
        [Order(After = PredefinedClassificationTypeNames.Keyword)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class UserTypeTypeParametersFormatDefinition : ClassificationFormatDefinition
        {
            private UserTypeTypeParametersFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.User_Types_Type_Parameters;
                this.ForegroundColor = Color.FromRgb(0x2B, 0x91, 0xAF);
            }
        }
        #endregion
        #region User Members - Extension Method Name
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.ExtensionMethodName)]
        [Name(ClassificationTypeNames.ExtensionMethodName)]
        [Order(After = PredefinedClassificationTypeNames.Identifier)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class UserMembersExtensionMethodNameFormatDefinition : ClassificationFormatDefinition
        {
            private UserMembersExtensionMethodNameFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.User_Members_Extension_Methods;
                this.ForegroundColor = Color.FromRgb(85, 85, 85);
            }
        }
        #endregion

        #region XML Doc Comments - Attribute Name
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlDocCommentAttributeName)]
        [Name(ClassificationTypeNames.XmlDocCommentAttributeName)]
        [Order(After = Priority.Default, Before = Priority.High)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class XmlDocCommentAttributeNameFormatDefinition : ClassificationFormatDefinition
        {
            private XmlDocCommentAttributeNameFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.XML_Doc_Comments_Attribute_Name;
                this.ForegroundColor = Color.FromRgb(0x80, 0x80, 0x80); // CIDARKGRAY
            }
        }
        #endregion
        #region XML Doc Comments - Attribute Quotes
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlDocCommentAttributeQuotes)]
        [Name(ClassificationTypeNames.XmlDocCommentAttributeQuotes)]
        [Order(After = Priority.Default, Before = Priority.High)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class XmlDocCommentAttributeQuotesFormatDefinition : ClassificationFormatDefinition
        {
            private XmlDocCommentAttributeQuotesFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.XML_Doc_Comments_Attribute_Quotes;
                this.ForegroundColor = Color.FromRgb(0x80, 0x80, 0x80); // CIDARKGRAY
            }
        }
        #endregion
        #region XML Doc Comments - Attribute Value
        // definition of how format is represented in tools options.
        // also specifies the default format.
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlDocCommentAttributeValue)]
        [Name(ClassificationTypeNames.XmlDocCommentAttributeValue)]
        [Order(After = Priority.Default, Before = Priority.High)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class XmlDocCommentAttributeValueFormatDefinition : ClassificationFormatDefinition
        {
            private XmlDocCommentAttributeValueFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.XML_Doc_Comments_Attribute_Value;
                this.ForegroundColor = Color.FromRgb(0x80, 0x80, 0x80); // CIDARKGRAY
            }
        }
        #endregion
        #region XML Doc Comments - CData Section
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlDocCommentCDataSection)]
        [Name(ClassificationTypeNames.XmlDocCommentCDataSection)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class XmlDocCommentCDataSectionFormatDefinition : ClassificationFormatDefinition
        {
            private XmlDocCommentCDataSectionFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.XML_Doc_Comments_CData_Section;
                this.ForegroundColor = Color.FromRgb(0x80, 0x80, 0x80);    // CIDARKGRAY
            }
        }
        #endregion
        #region XML Doc Comments - Comment
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlDocCommentComment)]
        [Name(ClassificationTypeNames.XmlDocCommentComment)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class XmlDocCommentCommentFormatDefinition : ClassificationFormatDefinition
        {
            private XmlDocCommentCommentFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.XML_Doc_Comments_Comment;
                this.ForegroundColor = Color.FromRgb(0x80, 0x80, 0x80);    // CIDARKGRAY
            }
        }
        #endregion
        #region XML Doc Comments - Delimiter
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlDocCommentDelimiter)]
        [Name(ClassificationTypeNames.XmlDocCommentDelimiter)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class XmlDocCommentDelimiterFormatDefinition : ClassificationFormatDefinition
        {
            private XmlDocCommentDelimiterFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.XML_Doc_Comments_Delimiter;
                this.ForegroundColor = Color.FromRgb(0x80, 0x80, 0x80); // CIDARKGRAY
            }
        }
        #endregion
        #region XML Doc Comments - Entity Reference
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlDocCommentEntityReference)]
        [Name(ClassificationTypeNames.XmlDocCommentEntityReference)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class XmlDocCommentEntityReferenceFormatDefinition : ClassificationFormatDefinition
        {
            private XmlDocCommentEntityReferenceFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.XML_Doc_Comments_Entity_Reference;
                this.ForegroundColor = Colors.Green;
            }
        }
        #endregion
        #region XML Doc Comments - Name
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlDocCommentName)]
        [Name(ClassificationTypeNames.XmlDocCommentName)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class XmlDocCommentNameFormatDefinition : ClassificationFormatDefinition
        {
            private XmlDocCommentNameFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.XML_Doc_Comments_Name;
                this.ForegroundColor = Color.FromRgb(0x80, 0x80, 0x80); // CIDARKGRAY
            }
        }
        #endregion
        #region XML Doc Comments - Processing Instruction
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlDocCommentProcessingInstruction)]
        [Name(ClassificationTypeNames.XmlDocCommentProcessingInstruction)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class XmlDocCommentProcessingInstructionFormatDefinition : ClassificationFormatDefinition
        {
            private XmlDocCommentProcessingInstructionFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.XML_Doc_Comments_Processing_Instruction;
                this.ForegroundColor = Color.FromRgb(0x80, 0x80, 0x80); // CIDARKGRAY
            }
        }
        #endregion
        #region XML Doc Comments - Text
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlDocCommentText)]
        [Name(ClassificationTypeNames.XmlDocCommentText)]
        [Order(After = Priority.Default, Before = Priority.High)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class XmlDocCommentTextFormatDefinition : ClassificationFormatDefinition
        {
            private XmlDocCommentTextFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.XML_Doc_Comments_Text;
                this.ForegroundColor = Colors.Green;
            }
        }
        #endregion

        #region VB XML Literals - Attribute Name
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlLiteralAttributeName)]
        [Name(ClassificationTypeNames.XmlLiteralAttributeName)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        private class XmlLiteralAttributeNameFormatDefinition : ClassificationFormatDefinition
        {
            private XmlLiteralAttributeNameFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.VB_XML_Literals_Attribute_Name;
                this.ForegroundColor = Color.FromRgb(185, 100, 100); // HC_LIGHTRED
            }
        }
        #endregion
        #region VB XML Literals - Attribute Quotes
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlLiteralAttributeQuotes)]
        [Name(ClassificationTypeNames.XmlLiteralAttributeQuotes)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        private class XmlLiteralAttributeQuotesFormatDefinition : ClassificationFormatDefinition
        {
            private XmlLiteralAttributeQuotesFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.VB_XML_Literals_Attribute_Quotes;
                this.ForegroundColor = Color.FromRgb(85, 85, 85); // HC_LIGHTBLACK
            }
        }
        #endregion
        #region VB XML Literals - Attribute Value
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlLiteralAttributeValue)]
        [Name(ClassificationTypeNames.XmlLiteralAttributeValue)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        private class XmlLiteralAttributeValueFormatDefinition : ClassificationFormatDefinition
        {
            private XmlLiteralAttributeValueFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.VB_XML_Literals_Attribute_Value;
                this.ForegroundColor = Color.FromRgb(100, 100, 185); // HC_LIGHTBLUE
            }
        }
        #endregion
        #region VB XML Literals - CData Section
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlLiteralCDataSection)]
        [Name(ClassificationTypeNames.XmlLiteralCDataSection)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        private class XmlLiteralCDataSectionFormatDefinition : ClassificationFormatDefinition
        {
            private XmlLiteralCDataSectionFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.VB_XML_Literals_CData_Section;
                this.ForegroundColor = Color.FromRgb(192, 192, 192); // HC_LIGHTGRAY
            }
        }
        #endregion
        #region VB XML Literals - Comment
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlLiteralComment)]
        [Name(ClassificationTypeNames.XmlLiteralComment)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        private class XmlLiteralCommentFormatDefinition : ClassificationFormatDefinition
        {
            private XmlLiteralCommentFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.VB_XML_Literals_Comment;
                this.ForegroundColor = Color.FromRgb(98, 151, 85); // HC_LIGHTGREEN
            }
        }
        #endregion
        #region VB XML Literals - Delimiter
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlLiteralDelimiter)]
        [Name(ClassificationTypeNames.XmlLiteralDelimiter)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        private class XmlLiteralDelimiterFormatDefinition : ClassificationFormatDefinition
        {
            private XmlLiteralDelimiterFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.VB_XML_Literals_Delimiter;
                this.ForegroundColor = Color.FromRgb(100, 100, 185); // HC_LIGHTBLUE
            }
        }
        #endregion
        #region VB XML Literals - Embedded Expression
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlLiteralEmbeddedExpression)]
        [Name(ClassificationTypeNames.XmlLiteralEmbeddedExpression)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        private class XmlLiteralEmbeddedExpressionFormatDefinition : ClassificationFormatDefinition
        {
            private XmlLiteralEmbeddedExpressionFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.VB_XML_Literals_Embedded_Expression;
                this.ForegroundColor = Color.FromRgb(85, 85, 85); // HC_LIGHTBLACK
                this.BackgroundColor = Color.FromRgb(255, 254, 191); // HC_LIGHTYELLOW
            }
        }
        #endregion
        #region VB XML Literals - Entity Reference
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlLiteralEntityReference)]
        [Name(ClassificationTypeNames.XmlLiteralEntityReference)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        private class XmlLiteralEntityReferenceFormatDefinition : ClassificationFormatDefinition
        {
            private XmlLiteralEntityReferenceFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.VB_XML_Literals_Entity_Reference;
                this.ForegroundColor = Color.FromRgb(185, 100, 100); // HC_LIGHTRED
            }
        }
        #endregion
        #region VB XML Literals - Name
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlLiteralName)]
        [Name(ClassificationTypeNames.XmlLiteralName)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        private class XmlLiteralNameFormatDefinition : ClassificationFormatDefinition
        {
            private XmlLiteralNameFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.VB_XML_Literals_Name;
                this.ForegroundColor = Color.FromRgb(132, 70, 70); // HC_LIGHTMAROON
            }
        }
        #endregion
        #region VB XML Literals - Processing Instruction
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlLiteralProcessingInstruction)]
        [Name(ClassificationTypeNames.XmlLiteralProcessingInstruction)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        private class XmlLiteralProcessingInstructionFormatDefinition : ClassificationFormatDefinition
        {
            private XmlLiteralProcessingInstructionFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.VB_XML_Literals_Processing_Instruction;
                this.ForegroundColor = Color.FromRgb(192, 192, 192); // HC_LIGHTGRAY
            }
        }
        #endregion
        #region VB XML Literals - Text
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlLiteralText)]
        [Name(ClassificationTypeNames.XmlLiteralText)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        private class XmlLiteralTextFormatDefinition : ClassificationFormatDefinition
        {
            private XmlLiteralTextFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.VB_XML_Literals_Text;
                this.ForegroundColor = Color.FromRgb(85, 85, 85); // HC_LIGHTBLACK
            }
        }
        #endregion

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics.ClassificationTypeDefinitions.UnnecessaryCode)]
        [Name(Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics.ClassificationTypeDefinitions.UnnecessaryCode)]
        [Order(After = Priority.High)]
        [UserVisible(false)]
        internal sealed class UnnecessaryCodeFormatDefinition : ClassificationFormatDefinition
        {
            private UnnecessaryCodeFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.Unnecessary_Code;
                this.ForegroundOpacity = 0.6;
            }
        }

    }
}
