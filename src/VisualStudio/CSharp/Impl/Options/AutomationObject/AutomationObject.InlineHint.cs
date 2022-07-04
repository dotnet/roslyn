// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.InlineHints;
using Microsoft.CodeAnalysis.InlineHints;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    public partial class AutomationObject
    {
        public int EnabledForParameters
        {
            get { return GetBooleanOption(InlineHintsOptionsStorage.EnabledForParameters); }
            set { SetBooleanOption(InlineHintsOptionsStorage.EnabledForParameters, value); }
        }

        public int ForLiteralParameters
        {
            get { return GetBooleanOption(InlineHintsOptionsStorage.ForLiteralParameters); }
            set { SetBooleanOption(InlineHintsOptionsStorage.ForLiteralParameters, value); }
        }

        public int ForObjectCreationParameters
        {
            get { return GetBooleanOption(InlineHintsOptionsStorage.ForObjectCreationParameters); }
            set { SetBooleanOption(InlineHintsOptionsStorage.ForObjectCreationParameters, value); }
        }

        public int ForOtherParameters
        {
            get { return GetBooleanOption(InlineHintsOptionsStorage.ForOtherParameters); }
            set { SetBooleanOption(InlineHintsOptionsStorage.ForOtherParameters, value); }
        }

        public int ForIndexerParameters
        {
            get { return GetBooleanOption(InlineHintsOptionsStorage.ForIndexerParameters); }
            set { SetBooleanOption(InlineHintsOptionsStorage.ForIndexerParameters, value); }
        }

        public int SuppressForParametersThatMatchMethodIntent
        {
            get { return GetBooleanOption(InlineHintsOptionsStorage.SuppressForParametersThatMatchMethodIntent); }
            set { SetBooleanOption(InlineHintsOptionsStorage.SuppressForParametersThatMatchMethodIntent, value); }
        }

        public int SuppressForParametersThatDifferOnlyBySuffix
        {
            get { return GetBooleanOption(InlineHintsOptionsStorage.SuppressForParametersThatDifferOnlyBySuffix); }
            set { SetBooleanOption(InlineHintsOptionsStorage.SuppressForParametersThatDifferOnlyBySuffix, value); }
        }

        public int SuppressForParametersThatMatchArgumentName
        {
            get { return GetBooleanOption(InlineHintsOptionsStorage.SuppressForParametersThatMatchArgumentName); }
            set { SetBooleanOption(InlineHintsOptionsStorage.SuppressForParametersThatMatchArgumentName, value); }
        }

        public int EnabledForTypes
        {
            get { return GetBooleanOption(InlineHintsOptionsStorage.EnabledForTypes); }
            set { SetBooleanOption(InlineHintsOptionsStorage.EnabledForTypes, value); }
        }

        public int ForImplicitVariableTypes
        {
            get { return GetBooleanOption(InlineHintsOptionsStorage.ForImplicitVariableTypes); }
            set { SetBooleanOption(InlineHintsOptionsStorage.ForImplicitVariableTypes, value); }
        }

        public int ForLambdaParameterTypes
        {
            get { return GetBooleanOption(InlineHintsOptionsStorage.ForLambdaParameterTypes); }
            set { SetBooleanOption(InlineHintsOptionsStorage.ForLambdaParameterTypes, value); }
        }

        public int ForImplicitObjectCreation
        {
            get { return GetBooleanOption(InlineHintsOptionsStorage.ForImplicitObjectCreation); }
            set { SetBooleanOption(InlineHintsOptionsStorage.ForImplicitObjectCreation, value); }
        }

        public int DisplayAllHintsWhilePressingAltF1
        {
            get { return GetBooleanOption(InlineHintsViewOptions.DisplayAllHintsWhilePressingAltF1); }
            set { SetBooleanOption(InlineHintsViewOptions.DisplayAllHintsWhilePressingAltF1, value); }
        }

        public int ColorHints
        {
            get { return GetBooleanOption(InlineHintsViewOptions.ColorHints); }
            set { SetBooleanOption(InlineHintsViewOptions.ColorHints, value); }
        }
    }
}
