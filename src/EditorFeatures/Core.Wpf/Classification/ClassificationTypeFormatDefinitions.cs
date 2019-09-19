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
            [ImportingConstructor]
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
            public PunctuationFormatDefinition()
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
            [ImportingConstructor]
            public StringVerbatimFormatDefinition()
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
            [ImportingConstructor]
            public StringEscapeCharacterFormatDefinition()
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
            [ImportingConstructor]
            public ControlKeywordFormatDefinition()
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
            [ImportingConstructor]
            public OperatorOverloadedFormatDefinition()
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
            [ImportingConstructor]
            public SymbolStaticFormatDefinition()
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
        // span, the styling for the identifier type would be chosen.

        // User Types - * and User Members - * are ordered before Symbol - Static 
        // so that the font styling chosen for static symbols would override the
        // styling chosen for specific identifier types.
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
            [ImportingConstructor]
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
        [Order(Before = ClassificationTypeNames.StaticSymbol)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class UserTypeDelegatesFormatDefinition : ClassificationFormatDefinition
        {
            [ImportingConstructor]
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
        [Order(Before = ClassificationTypeNames.StaticSymbol)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class UserTypeEnumsFormatDefinition : ClassificationFormatDefinition
        {
            [ImportingConstructor]
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
        [Order(Before = ClassificationTypeNames.StaticSymbol)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class UserTypeInterfacesFormatDefinition : ClassificationFormatDefinition
        {
            [ImportingConstructor]
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
        [Order(Before = ClassificationTypeNames.StaticSymbol)]
        [UserVisible(true)]
        private class UserTypeModulesFormatDefinition : ClassificationFormatDefinition
        {
            [ImportingConstructor]
            public UserTypeModulesFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.User_Types_Modules;
                this.ForegroundColor = Color.FromRgb(0x2B, 0x91, 0xAF);
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
            [ImportingConstructor]
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
        [Order(Before = ClassificationTypeNames.StaticSymbol)]
        [UserVisible(true)]
        [ExcludeFromCodeCoverage]
        private class UserTypeTypeParametersFormatDefinition : ClassificationFormatDefinition
        {
            [ImportingConstructor]
            public UserTypeTypeParametersFormatDefinition()
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
            [ImportingConstructor]
            public UserMembersFieldNameFormatDefinition()
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
            [ImportingConstructor]
            public UserMembersEnumMemberNameFormatDefinition()
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
            [ImportingConstructor]
            public UserMembersConstantNameFormatDefinition()
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
            [ImportingConstructor]
            public UserMembersLocalNameFormatDefinition()
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
            [ImportingConstructor]
            public UserMembersParameterNameFormatDefinition()
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
            [ImportingConstructor]
            public UserMembersMethodNameFormatDefinition()
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
            [ImportingConstructor]
            public UserMembersExtensionMethodNameFormatDefinition()
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
            [ImportingConstructor]
            public UserMembersPropertyNameFormatDefinition()
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
            [ImportingConstructor]
            public UserMembersEventNameFormatDefinition()
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
            [ImportingConstructor]
            public UserMembersNamespaceNameFormatDefinition()
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
            [ImportingConstructor]
            public UserMembersLabelNameFormatDefinition()
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
            [ImportingConstructor]
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
            public XmlDocCommentTextFormatDefinition()
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
        private static readonly Color s_regexCommentColor = Color.FromRgb(0x61, 0xa6, 0x4a);
#else
        private static readonly Color s_regexTextColor = Color.FromRgb(0x80, 0x00, 0x00);
        private static readonly Color s_regexOtherEscapeColor = Color.FromRgb(0x9e, 0x5b, 0x71);
        private static readonly Color s_regexGroupingAndAlternationColor = Color.FromRgb(0x05, 0xc3, 0xba);
        private static readonly Color s_characterClassColor = Color.FromRgb(0x00, 0x73, 0xff);
        private static readonly Color s_regexAnchorAndQuantifierColor = Color.FromRgb(0xff, 0x00, 0xc1);
        private static readonly Color s_regexCommentColor = Color.FromRgb(0x00, 0x80, 0x00);
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
            [ImportingConstructor]
            public RegexCommentFormatDefinition()
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
            [ImportingConstructor]
            public RegexCharacterClassFormatDefinition()
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
            [ImportingConstructor]
            public RegexAnchorFormatDefinition()
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
            [ImportingConstructor]
            public RegexQuantifierFormatDefinition()
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
            [ImportingConstructor]
            public RegexGroupingFormatDefinition()
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
            [ImportingConstructor]
            public RegexAlternationFormatDefinition()
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
            [ImportingConstructor]
            public RegexTextFormatDefinition()
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
            [ImportingConstructor]
            public RegexSelfEscapedCharacterFormatDefinition()
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
            [ImportingConstructor]
            public RegexOtherEscapeFormatDefinition()
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
            [ImportingConstructor]
            public XmlLiteralAttributeNameFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.VB_XML_Literals_Attribute_Name;
                this.ForegroundColor = Color.FromRgb(0xB9, 0x64, 0x64); // HC_LIGHTRED
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
            public XmlLiteralAttributeQuotesFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.VB_XML_Literals_Attribute_Quotes;
                this.ForegroundColor = Color.FromRgb(0x55, 0x55, 0x55); // HC_LIGHTBLACK
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
            public XmlLiteralAttributeValueFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.VB_XML_Literals_Attribute_Value;
                this.ForegroundColor = Color.FromRgb(0x64, 0x64, 0xB9); // HC_LIGHTBLUE
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
            public XmlLiteralCDataSectionFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.VB_XML_Literals_CData_Section;
                this.ForegroundColor = Color.FromRgb(0xC0, 0xC0, 0xC0); // HC_LIGHTGRAY
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
            public XmlLiteralCommentFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.VB_XML_Literals_Comment;
                this.ForegroundColor = Color.FromRgb(0x62, 0x97, 0x55); // HC_LIGHTGREEN
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
            public XmlLiteralDelimiterFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.VB_XML_Literals_Delimiter;
                this.ForegroundColor = Color.FromRgb(0x64, 0x64, 0xB9); // HC_LIGHTBLUE
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
            public XmlLiteralEmbeddedExpressionFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.VB_XML_Literals_Embedded_Expression;
                this.ForegroundColor = Color.FromRgb(0x55, 0x55, 0x55); // HC_LIGHTBLACK
                this.BackgroundColor = Color.FromRgb(0xFF, 0xFE, 0xBF); // HC_LIGHTYELLOW
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
            public XmlLiteralEntityReferenceFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.VB_XML_Literals_Entity_Reference;
                this.ForegroundColor = Color.FromRgb(0xB9, 0x64, 0x64); // HC_LIGHTRED
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
            public XmlLiteralNameFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.VB_XML_Literals_Name;
                this.ForegroundColor = Color.FromRgb(0x84, 0x46, 0x46); // HC_LIGHTMAROON
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
            public XmlLiteralProcessingInstructionFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.VB_XML_Literals_Processing_Instruction;
                this.ForegroundColor = Color.FromRgb(0xC0, 0xC0, 0xC0); // HC_LIGHTGRAY
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
            public XmlLiteralTextFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.VB_XML_Literals_Text;
                this.ForegroundColor = Color.FromRgb(0x55, 0x55, 0x55); // HC_LIGHTBLACK
            }
        }
        #endregion
    }
}
