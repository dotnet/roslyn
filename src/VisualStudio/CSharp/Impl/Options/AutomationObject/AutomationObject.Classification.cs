// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Classification;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    public partial class AutomationObject
    {
        public int ClassifyReassignedVariables
        {
            get { return GetBooleanOption(ClassificationOptionsStorage.ClassifyReassignedVariables); }
            set { SetBooleanOption(ClassificationOptionsStorage.ClassifyReassignedVariables, value); }
        }

        public int ColorizeRegexPatterns
        {
            get { return GetBooleanOption(ClassificationOptionsStorage.ColorizeRegexPatterns); }
            set { SetBooleanOption(ClassificationOptionsStorage.ColorizeRegexPatterns, value); }
        }

        public int ColorizeJsonPatterns
        {
            get { return GetBooleanOption(ClassificationOptionsStorage.ColorizeJsonPatterns); }
            set { SetBooleanOption(ClassificationOptionsStorage.ColorizeJsonPatterns, value); }
        }
    }
}
