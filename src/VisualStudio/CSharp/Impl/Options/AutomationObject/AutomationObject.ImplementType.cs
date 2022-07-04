// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.ImplementType;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    public partial class AutomationObject
    {
        public int ImplementTypeInsertionBehavior
        {
            get { return (int)GetOption(ImplementTypeOptionsStorage.InsertionBehavior); }
            set { SetOption(ImplementTypeOptionsStorage.InsertionBehavior, (ImplementTypeInsertionBehavior)value); }
        }

        public int PropertyGenerationBehavior
        {
            get { return (int)GetOption(ImplementTypeOptionsStorage.PropertyGenerationBehavior); }
            set { SetOption(ImplementTypeOptionsStorage.PropertyGenerationBehavior, (ImplementTypePropertyGenerationBehavior)value); }
        }
    }
}
