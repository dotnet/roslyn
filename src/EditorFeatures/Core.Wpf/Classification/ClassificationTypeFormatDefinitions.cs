// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#define dark_theme

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

        // When https://github.com/dotnet/roslyn/issues/29173 is addressed, this section
        // can be removed.  Right now it serves as an easy way to recompile while flipping
        // between different themes.
#if dark_theme
        private static readonly Color s_stringEscapeColor = Color.FromRgb(0xff, 0xd6, 0x8f);
#else
        private static readonly Color s_stringEscapeColor = Color.FromRgb(0x9e, 0x5b, 0x71);
#endif

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.StringEscapeCharacter)]
        [Name(ClassificationTypeNames.StringEscapeCharacter)]
        [Order(After = ClassificationTypeNames.StringLiteral)]
        [Order(After = ClassificationTypeNames.VerbatimStringLiteral)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class StringEscapeCharacterFormatDefinition : ClassificationFormatDefinition
        {
            private StringEscapeCharacterFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.String_Escape_Character;
                this.ForegroundColor = s_stringEscapeColor;
            }
        }

        #endregion
        #region Keyword - Control
        // Keyword - Control is ordered after Keyword to ensure this more specific 
        // classification will take precedence.
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.ControlKeyword)]
        [Name(ClassificationTypeNames.ControlKeyword)]
        [Order(After = PredefinedClassificationTypeNames.Keyword)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class ControlKeywordFormatDefinition : ClassificationFormatDefinition
        {
            private ControlKeywordFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.Keyword_Control;
            }
        }
        #endregion
        #region Operator - Overloaded
        // Operator - Overloaded is ordered after Operator to ensure this more specific 
        // classification will take precedence.
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.OperatorOverloaded)]
        [Name(ClassificationTypeNames.OperatorOverloaded)]
        [Order(After = PredefinedClassificationTypeNames.Operator)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class OperatorOverloadedFormatDefinition : ClassificationFormatDefinition
        {
            private OperatorOverloadedFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.Operator_Overloaded;
            }
        }
        #endregion

        #region Symbol - Static
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.StaticSymbol)]
        [Name(ClassificationTypeNames.StaticSymbol)]
        [Order(After = PredefinedClassificationTypeNames.Identifier)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class SymbolStaticFormatDefinition : ClassificationFormatDefinition
        {
            private SymbolStaticFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.Symbol_Static;
                // The static classification is intended to be an additive classification
                // that simply changes the font's styling (bold or not). Allowing 
                // customization of the foreground color would cause issues with 
                // TaggedText as it is currently implemented, since any particular
                // span can only be tagged with a single TextTag. 

                // By restricting to only font style, the QuickInfo and FAR render with the
                // colors the user would expect. The missing font style is not an problem
                // for these experiences because the QuickInfo already renders the static 
                // modifier as part of its text and the FAR window already applies its
                // own bolding to the rendered output.
                this.BackgroundCustomizable = false;
                this.ForegroundCustomizable = false;
            }
        }
        #endregion

        // User Types - * and User Members - * are ordered after Keyword
        // so that, in the case both classifications are applied to the same
        // span, the styling for the identifier type would be choosen.

        // User Types - * and User Members - * are ordered before Symbol - Static 
        // so that the font styling choosen for static symbols would override the
        // styling choosen for specific identifier types.
        #region User Types - Classes
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.ClassName)]
        [Name(ClassificationTypeNames.ClassName)]
        [Order(After = PredefinedClassificationTypeNames.Identifier)]
        [Order(After = PredefinedClassificationTypeNames.Keyword)]
        [Order(Before = ClassificationTypeNames.StaticSymbol)]
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
        [Order(Before = ClassificationTypeNames.StaticSymbol)]
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
        [Order(Before = ClassificationTypeNames.StaticSymbol)]
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
        [Order(Before = ClassificationTypeNames.StaticSymbol)]
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
        [Order(Before = ClassificationTypeNames.StaticSymbol)]
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
        [Order(Before = ClassificationTypeNames.StaticSymbol)]
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
        [Order(Before = ClassificationTypeNames.StaticSymbol)]
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

        #region User Members - Fields 
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.FieldName)]
        [Name(ClassificationTypeNames.FieldName)]
        [Order(After = PredefinedClassificationTypeNames.Identifier)]
        [Order(After = PredefinedClassificationTypeNames.Keyword)]
        [Order(Before = ClassificationTypeNames.StaticSymbol)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class UserMembersFieldNameFormatDefinition : ClassificationFormatDefinition
        {
            private UserMembersFieldNameFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.User_Members_Fields;
            }
        }
        #endregion
        #region User Members - Enum Members 
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.EnumMemberName)]
        [Name(ClassificationTypeNames.EnumMemberName)]
        [Order(After = PredefinedClassificationTypeNames.Identifier)]
        [Order(After = PredefinedClassificationTypeNames.Keyword)]
        [Order(Before = ClassificationTypeNames.StaticSymbol)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class UserMembersEnumMemberNameFormatDefinition : ClassificationFormatDefinition
        {
            private UserMembersEnumMemberNameFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.User_Members_Enum_Members;
            }
        }
        #endregion
        #region User Members - Constants 
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.ConstantName)]
        [Name(ClassificationTypeNames.ConstantName)]
        [Order(After = PredefinedClassificationTypeNames.Identifier)]
        [Order(After = PredefinedClassificationTypeNames.Keyword)]
        [Order(Before = ClassificationTypeNames.StaticSymbol)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class UserMembersConstantNameFormatDefinition : ClassificationFormatDefinition
        {
            private UserMembersConstantNameFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.User_Members_Constants;
            }
        }
        #endregion
        #region User Members - Locals 
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.LocalName)]
        [Name(ClassificationTypeNames.LocalName)]
        [Order(After = PredefinedClassificationTypeNames.Identifier)]
        [Order(After = PredefinedClassificationTypeNames.Keyword)]
        [Order(Before = ClassificationTypeNames.StaticSymbol)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class UserMembersLocalNameFormatDefinition : ClassificationFormatDefinition
        {
            private UserMembersLocalNameFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.User_Members_Locals;
            }
        }
        #endregion
        #region User Members - Parameters 
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.ParameterName)]
        [Name(ClassificationTypeNames.ParameterName)]
        [Order(After = PredefinedClassificationTypeNames.Identifier)]
        [Order(After = PredefinedClassificationTypeNames.Keyword)]
        [Order(Before = ClassificationTypeNames.StaticSymbol)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class UserMembersParameterNameFormatDefinition : ClassificationFormatDefinition
        {
            private UserMembersParameterNameFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.User_Members_Parameters;
            }
        }
        #endregion
        #region User Members - Methods 
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.MethodName)]
        [Name(ClassificationTypeNames.MethodName)]
        [Order(After = PredefinedClassificationTypeNames.Identifier)]
        [Order(After = PredefinedClassificationTypeNames.Keyword)]
        [Order(Before = ClassificationTypeNames.StaticSymbol)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class UserMembersMethodNameFormatDefinition : ClassificationFormatDefinition
        {
            private UserMembersMethodNameFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.User_Members_Methods;
            }
        }
        #endregion
        #region User Members - Extension Methods
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.ExtensionMethodName)]
        [Name(ClassificationTypeNames.ExtensionMethodName)]
        [Order(After = PredefinedClassificationTypeNames.Identifier)]
        [Order(After = PredefinedClassificationTypeNames.Keyword)]
        [Order(Before = ClassificationTypeNames.StaticSymbol)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class UserMembersExtensionMethodNameFormatDefinition : ClassificationFormatDefinition
        {
            private UserMembersExtensionMethodNameFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.User_Members_Extension_Methods;
            }
        }
        #endregion
        #region User Members - Properties 
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.PropertyName)]
        [Name(ClassificationTypeNames.PropertyName)]
        [Order(After = PredefinedClassificationTypeNames.Identifier)]
        [Order(After = PredefinedClassificationTypeNames.Keyword)]
        [Order(Before = ClassificationTypeNames.StaticSymbol)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class UserMembersPropertyNameFormatDefinition : ClassificationFormatDefinition
        {
            private UserMembersPropertyNameFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.User_Members_Properties;
            }
        }
        #endregion
        #region User Members - Events 
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.EventName)]
        [Name(ClassificationTypeNames.EventName)]
        [Order(After = PredefinedClassificationTypeNames.Identifier)]
        [Order(After = PredefinedClassificationTypeNames.Keyword)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class UserMembersEventNameFormatDefinition : ClassificationFormatDefinition
        {
            private UserMembersEventNameFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.User_Members_Events;
            }
        }
        #endregion
        #region User Members - Namespaces 
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.NamespaceName)]
        [Name(ClassificationTypeNames.NamespaceName)]
        [Order(After = PredefinedClassificationTypeNames.Identifier)]
        [Order(After = PredefinedClassificationTypeNames.Keyword)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class UserMembersNamespaceNameFormatDefinition : ClassificationFormatDefinition
        {
            private UserMembersNamespaceNameFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.User_Members_Namespaces;
            }
        }
        #endregion
        #region User Members - Labels 
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.LabelName)]
        [Name(ClassificationTypeNames.LabelName)]
        [Order(After = PredefinedClassificationTypeNames.Identifier)]
        [Order(After = PredefinedClassificationTypeNames.Keyword)]
        [Order(Before = ClassificationTypeNames.StaticSymbol)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class UserMembersLabelNameFormatDefinition : ClassificationFormatDefinition
        {
            private UserMembersLabelNameFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.User_Members_Labels;
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

        #region Regex

        // When https://github.com/dotnet/roslyn/issues/29173 is addressed, this section
        // can be removed.  Right now it serves as an easy way to recompile while flipping
        // between different themes.
#if dark_theme
        private static readonly Color s_regexTextColor = Color.FromRgb(0xd6, 0x9d, 0x85);
        private static readonly Color s_regexOtherEscapeColor = Color.FromRgb(0xff, 0xd6, 0x8f);
        private static readonly Color s_regexGroupingAndAlternationColor = Color.FromRgb(0x05, 0xc3, 0xba);
        private static readonly Color s_characterClassColor = Color.FromRgb(0x00, 0x8a, 0xff);
        private static readonly Color s_regexAnchorAndQuantifierColor = Color.FromRgb(0xd7, 0x45, 0x8c);
        private static readonly Color s_regexCommentColor = Color.FromRgb(87, 166, 74);
#else
        private static readonly Color s_regexTextColor = Color.FromRgb(0x80, 0x00, 0x00);
        private static readonly Color s_regexOtherEscapeColor = Color.FromRgb(0x9e, 0x5b, 0x71);
        private static readonly Color s_regexGroupingAndAlternationColor = Color.FromRgb(0x05, 0xc3, 0xba);
        private static readonly Color s_characterClassColor = Color.FromRgb(0x00, 0x73, 0xff);
        private static readonly Color s_regexAnchorAndQuantifierColor = Color.FromRgb(0xff, 0x00, 0xc1);
        private static readonly Color s_regexCommentColor = Color.FromRgb(0, 128, 0);
#endif

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexComment)]
        [Name(ClassificationTypeNames.RegexComment)]
        [Order(After = ClassificationTypeNames.StringLiteral)]
        [Order(After = ClassificationTypeNames.VerbatimStringLiteral)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class RegexCommentFormatDefinition : ClassificationFormatDefinition
        {
            private RegexCommentFormatDefinition()
            {
                this.DisplayName = EditorFeaturesWpfResources.Regex_Comment;
                this.ForegroundColor = s_regexCommentColor;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexCharacterClass)]
        [Name(ClassificationTypeNames.RegexCharacterClass)]
        [Order(After = ClassificationTypeNames.StringLiteral)]
        [Order(After = ClassificationTypeNames.VerbatimStringLiteral)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class RegexCharacterClassFormatDefinition : ClassificationFormatDefinition
        {
            private RegexCharacterClassFormatDefinition()
            {
                this.DisplayName = EditorFeaturesWpfResources.Regex_Character_Class;
                this.ForegroundColor = s_characterClassColor;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexAnchor)]
        [Name(ClassificationTypeNames.RegexAnchor)]
        [Order(After = ClassificationTypeNames.StringLiteral)]
        [Order(After = ClassificationTypeNames.VerbatimStringLiteral)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class RegexAnchorFormatDefinition : ClassificationFormatDefinition
        {
            private RegexAnchorFormatDefinition()
            {
                this.DisplayName = EditorFeaturesWpfResources.Regex_Anchor;
                this.ForegroundColor = s_regexAnchorAndQuantifierColor;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexQuantifier)]
        [Name(ClassificationTypeNames.RegexQuantifier)]
        [Order(After = ClassificationTypeNames.StringLiteral)]
        [Order(After = ClassificationTypeNames.VerbatimStringLiteral)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class RegexQuantifierFormatDefinition : ClassificationFormatDefinition
        {
            private RegexQuantifierFormatDefinition()
            {
                this.DisplayName = EditorFeaturesWpfResources.Regex_Quantifier;
                this.ForegroundColor = s_regexAnchorAndQuantifierColor;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexGrouping)]
        [Name(ClassificationTypeNames.RegexGrouping)]
        [Order(After = ClassificationTypeNames.StringLiteral)]
        [Order(After = ClassificationTypeNames.VerbatimStringLiteral)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class RegexGroupingFormatDefinition : ClassificationFormatDefinition
        {
            private RegexGroupingFormatDefinition()
            {
                this.DisplayName = EditorFeaturesWpfResources.Regex_Grouping;
                this.ForegroundColor = s_regexGroupingAndAlternationColor;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexAlternation)]
        [Name(ClassificationTypeNames.RegexAlternation)]
        [Order(After = ClassificationTypeNames.StringLiteral)]
        [Order(After = ClassificationTypeNames.VerbatimStringLiteral)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class RegexAlternationFormatDefinition : ClassificationFormatDefinition
        {
            private RegexAlternationFormatDefinition()
            {
                this.DisplayName = EditorFeaturesWpfResources.Regex_Alternation;
                this.ForegroundColor = s_regexGroupingAndAlternationColor;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexText)]
        [Name(ClassificationTypeNames.RegexText)]
        [Order(After = ClassificationTypeNames.StringLiteral)]
        [Order(After = ClassificationTypeNames.VerbatimStringLiteral)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class RegexTextFormatDefinition : ClassificationFormatDefinition
        {
            private RegexTextFormatDefinition()
            {
                this.DisplayName = EditorFeaturesWpfResources.Regex_Text;
                this.ForegroundColor = s_regexTextColor;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexSelfEscapedCharacter)]
        [Name(ClassificationTypeNames.RegexSelfEscapedCharacter)]
        [Order(After = ClassificationTypeNames.StringLiteral)]
        [Order(After = ClassificationTypeNames.VerbatimStringLiteral)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class RegexSelfEscapedCharacterFormatDefinition : ClassificationFormatDefinition
        {
            private RegexSelfEscapedCharacterFormatDefinition()
            {
                this.DisplayName = EditorFeaturesWpfResources.Regex_SelfEscapedCharacter;

                // by default, we make a self-escaped character just the bolded form of the normal
                // text color.
                this.ForegroundColor = s_regexTextColor;
                this.IsBold = true;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexOtherEscape)]
        [Name(ClassificationTypeNames.RegexOtherEscape)]
        [Order(After = ClassificationTypeNames.StringLiteral)]
        [Order(After = ClassificationTypeNames.VerbatimStringLiteral)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class RegexOtherEscapeFormatDefinition : ClassificationFormatDefinition
        {
            private RegexOtherEscapeFormatDefinition()
            {
                this.DisplayName = EditorFeaturesWpfResources.Regex_OtherEscape;
                this.ForegroundColor = s_regexOtherEscapeColor;
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
    }
}
