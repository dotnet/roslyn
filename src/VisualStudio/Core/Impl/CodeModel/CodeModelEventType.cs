// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
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
            if (eventType == CodeModelEventType.Add || eventType == CodeModelEventType.Remove)
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
}
