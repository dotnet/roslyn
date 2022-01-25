// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel
{
    [Flags]
    internal enum ParameterFlags
    {
        Ref = 1 << 0,
        Out = 1 << 1,
        Params = 1 << 2
    }
}
