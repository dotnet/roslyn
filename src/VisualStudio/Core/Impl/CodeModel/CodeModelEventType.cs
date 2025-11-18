// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;

[Flags]
internal enum CodeModelEventType
{
    Add = 1 << 0,
    Remove = 1 << 1,
    Rename = 1 << 2,
    Unknown = 1 << 3,
    BaseChange = 1 << 4,
    SigChange = 1 << 5,
    TypeRefChange = 1 << 6,
    ArgChange = 1 << 7
}

internal static class CodeModelEventTypeExtensions
{
    public static bool IsChange(this CodeModelEventType eventType)
    {
        if (eventType is CodeModelEventType.Add or CodeModelEventType.Remove)
        {
            return false;
        }

        // Check that Add and Remove are not set
        if ((eventType & CodeModelEventType.Add) == 0 &&
            (eventType & CodeModelEventType.Remove) == 0)
        {
            // Check that one or more of the change flags are set
            var allChanges =
                CodeModelEventType.Rename |
                CodeModelEventType.Unknown |
                CodeModelEventType.BaseChange |
                CodeModelEventType.SigChange |
                CodeModelEventType.TypeRefChange |
                CodeModelEventType.ArgChange;

            if ((eventType & allChanges) != 0)
            {
                return true;
            }
        }

        Debug.Fail("Invalid combination of change type flags!");
        return false;
    }
}
