// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    internal enum MemberDisplayFormat
    {
        /// <summary>
        /// Display just a simple description of the object, like type name or ToString(). Don't
        /// display any members or items of the object.
        /// </summary>
        NoMembers,

        /// <summary>
        /// Display structure of the object on a single line.
        /// </summary>
        Inline,

        /// <summary>
        /// Display structure of the object on a single line, where the object is displayed as a value of its container's member.
        /// E.g. { a = ... }
        /// </summary>
        InlineValue,

        /// <summary>
        /// Displays a simple description of the object followed by list of members. Each member is
        /// displayed on a separate line.
        /// </summary>
        List,
    }

    internal static partial class EnumBounds
    {
        internal static bool IsValid(this MemberDisplayFormat value)
        {
            return value >= MemberDisplayFormat.NoMembers && value <= MemberDisplayFormat.List;
        }
    }
}
