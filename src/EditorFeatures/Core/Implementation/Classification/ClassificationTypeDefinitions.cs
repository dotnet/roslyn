// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
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
    }
}
