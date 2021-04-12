// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.Feedback.Interop
{
    [Guid("26E7ECA7-4DB3-49AD-B478-33FCF05F3995")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsFeedbackProfile
    {
        [DispId(1610678272)]
        bool IsMicrosoftInternal { get; }
    }

    [Guid("0BB1FA06-C83E-4EAA-99AF-0B67B2D8F90B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface SVsFeedbackProfile
    {
    }
}
