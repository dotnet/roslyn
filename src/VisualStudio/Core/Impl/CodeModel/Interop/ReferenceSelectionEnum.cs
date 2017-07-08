// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop
{
    [ComVisible(true)]
    [Guid("45d8ba93-a5e2-3cba-b48a-3c853d554a60")]
    public enum ReferenceSelectionEnum
    {
        NoReferences = 0,
        ExternalReferences = 1,
        AllReferences = 2,
    }
}
