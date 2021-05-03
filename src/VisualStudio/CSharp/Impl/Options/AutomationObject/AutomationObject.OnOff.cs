// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    public partial class AutomationObject
    {
        public int AutoInsertAsteriskForNewLinesOfBlockComments
        {
            get { return GetBooleanOption(FeatureOnOffOptions.AutoInsertBlockCommentStartString); }
            set { SetBooleanOption(FeatureOnOffOptions.AutoInsertBlockCommentStartString, value); }
        }

        public int DisplayLineSeparators
        {
            get { return GetBooleanOption(FeatureOnOffOptions.LineSeparator); }
            set { SetBooleanOption(FeatureOnOffOptions.LineSeparator, value); }
        }

        public int EnableHighlightRelatedKeywords
        {
            get { return GetBooleanOption(FeatureOnOffOptions.KeywordHighlighting); }
            set { SetBooleanOption(FeatureOnOffOptions.KeywordHighlighting, value); }
        }

        public int EnterOutliningModeOnOpen
        {
            get { return GetBooleanOption(FeatureOnOffOptions.Outlining); }
            set { SetBooleanOption(FeatureOnOffOptions.Outlining, value); }
        }

        public int Formatting_TriggerOnPaste
        {
            get { return GetBooleanOption(FeatureOnOffOptions.FormatOnPaste); }
            set { SetBooleanOption(FeatureOnOffOptions.FormatOnPaste, value); }
        }

        public int Formatting_TriggerOnStatementCompletion
        {
            get { return GetBooleanOption(FeatureOnOffOptions.AutoFormattingOnSemicolon); }
            set { SetBooleanOption(FeatureOnOffOptions.AutoFormattingOnSemicolon, value); }
        }

        public int HighlightReferences
        {
            get { return GetBooleanOption(FeatureOnOffOptions.ReferenceHighlighting); }
            set { SetBooleanOption(FeatureOnOffOptions.ReferenceHighlighting, value); }
        }

        public int Refactoring_Verification_Enabled
        {
            get { return GetBooleanOption(FeatureOnOffOptions.RefactoringVerification); }
            set { SetBooleanOption(FeatureOnOffOptions.RefactoringVerification, value); }
        }

        public int RenameSmartTagEnabled
        {
            get { return GetBooleanOption(FeatureOnOffOptions.RenameTracking); }
            set { SetBooleanOption(FeatureOnOffOptions.RenameTracking, value); }
        }

        public int RenameTrackingPreview
        {
            get { return GetBooleanOption(FeatureOnOffOptions.RenameTrackingPreview); }
            set { SetBooleanOption(FeatureOnOffOptions.RenameTrackingPreview, value); }
        }
    }
}
