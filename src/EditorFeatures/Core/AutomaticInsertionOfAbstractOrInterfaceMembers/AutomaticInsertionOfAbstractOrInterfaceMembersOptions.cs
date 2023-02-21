// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.AutomaticInsertionOfAbstractOrInterfaceMembers
{
    internal static class AutomaticInsertionOfAbstractOrInterfaceMembersOptions
    {
        // This value is only used by Visual Basic, and so is using the old serialization name that was used by VB.
        public static readonly PerLanguageOption2<bool> AutomaticInsertionOfAbstractOrInterfaceMembers = new(
            "visual_basic_automatic_insertion_of_abstract_or_interface_members_enabled", defaultValue: true);
    }
}
