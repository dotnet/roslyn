// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.VisualStudio.LanguageServices.Implementation.IntellisenseControls;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    internal struct ChangeSignatureIntellisenseTextBoxesViewModel
    {
        public readonly IntellisenseTextBoxViewModel TypeIntellisenseTextBoxViewModel;
        public readonly IntellisenseTextBoxViewModel NameIntellisenseTextBoxViewModel;

        public ChangeSignatureIntellisenseTextBoxesViewModel(IntellisenseTextBoxViewModel typeIntellisenseTextBoxViewModel, IntellisenseTextBoxViewModel nameIntellisenseTextBoxViewModel)
        {
            this.TypeIntellisenseTextBoxViewModel = typeIntellisenseTextBoxViewModel;
            this.NameIntellisenseTextBoxViewModel = nameIntellisenseTextBoxViewModel;
        }
    }
}
