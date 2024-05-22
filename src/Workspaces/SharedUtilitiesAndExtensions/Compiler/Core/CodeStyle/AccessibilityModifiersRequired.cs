// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeStyle;

internal enum AccessibilityModifiersRequired
{
    // The rule is not run
    Never = 0,

    // Accessibility modifiers are added if missing, even if default
    Always = 1,

    // Future proofing for when C# adds default interface methods.  At that point
    // accessibility modifiers will be allowed in interfaces, and some people may
    // want to require them, while some may want to keep the traditional C# style
    // that public interface members do not need accessibility modifiers.
    ForNonInterfaceMembers = 2,

    // Remove any accessibility modifier that matches the default
    OmitIfDefault = 3
}
