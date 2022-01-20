// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    public partial class AutomationObject
    {
        public int Formatting_TriggerOnBlockCompletion
        {
            get { return GetBooleanOption(AutoFormattingOptions.Metadata.AutoFormattingOnCloseBrace); }
            set { SetBooleanOption(AutoFormattingOptions.Metadata.AutoFormattingOnCloseBrace, value); }
        }
    }
}
