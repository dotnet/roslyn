// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host.Mef;
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public ControlKeywordFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public LocalNameFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public MethodNameFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public OperatorOverloadedFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public ParameterNameFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public PreprocessorTextFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public PunctuationFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public RegexAlternationFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public RegexAnchorFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public RegexCharacterClassFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public RegexCommentFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public RegexGroupingFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public RegexOtherEscapeFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public RegexQuantifierFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public RegexSelfEscapedCharacterFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public RegexTextFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public StringEscapeCharacterFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public StringVerbatimFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public UserTypeClassesFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public UserTypeDelegatesFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public UserTypeEnumsFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public UserTypeInterfacesFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public UserTypeModulesFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public UserTypeStructuresFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public UserTypeTypeParametersFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public UserMembersExtensionMethodNameFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public XmlDocCommentAttributeNameFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public XmlDocCommentAttributeQuotesFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public XmlDocCommentAttributeValueFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public XmlDocCommentCDataSectionFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public XmlDocCommentCommentFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public XmlDocCommentDelimiterFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public XmlDocCommentEntityReferenceFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public XmlDocCommentNameFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public XmlDocCommentProcessingInstructionFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public XmlDocCommentTextFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public XmlLiteralAttributeNameFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public XmlLiteralAttributeQuotesFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public XmlLiteralAttributeValueFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public XmlLiteralCDataSectionFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public XmlLiteralCommentFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public XmlLiteralDelimiterFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public XmlLiteralEmbeddedExpressionFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public XmlLiteralEntityReferenceFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public XmlLiteralNameFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public XmlLiteralProcessingInstructionFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public XmlLiteralTextFormatDefinition()
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
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public UnnecessaryCodeFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.Unnecessary_Code;
                this.ForegroundOpacity = 0.6;
            }
        }
    }
}
