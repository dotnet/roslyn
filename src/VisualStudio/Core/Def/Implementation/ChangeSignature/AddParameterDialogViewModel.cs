// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    internal class AddParameterDialogViewModel : AbstractNotifyPropertyChanged
    {
        public AddParameterDialogViewModel(ParameterTypeEditorControl parameterTypeEditorControl)
        {
            TypeNameEditorControl = parameterTypeEditorControl;
        }

        public string ParameterName { get; set; }
        public string CallsiteValue { get; set; }

        public ParameterTypeEditorControl TypeNameEditorControl { get; }

        internal bool TrySubmit()
        {
            return true;
        }
    }
}
