// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;

[ComVisible(true)]
[Guid("45d8ba93-a5e2-3cba-b48a-3c853d554a60")]
public enum ReferenceSelectionEnum
{
    NoReferences = 0,
    ExternalReferences = 1,
    AllReferences = 2,
}
