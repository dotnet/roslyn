// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    // TODO (https://github.com/dotnet/roslyn/issues/6689): change default to SeparateLines
    public enum MemberDisplayFormat
    {
        /// <summary>
        /// Display structure of the object on a single line.
        /// </summary>
        SingleLine,

        /// <summary>
        /// Displays a simple description of the object followed by list of members. Each member is
        /// displayed on a separate line.
        /// </summary>
        SeparateLines,

        /// <summary>
        /// Display just a simple description of the object, like type name or ToString(). Don't
        /// display any members of the object.
        /// </summary>
        /// <remarks>
        /// <see cref="CommonObjectFormatter"/> does not apply this format to collections elements - 
        /// they are shown regardless.
        /// </remarks>
        Hidden,
    }

    internal static partial class MemberDisplayFormatExtensions
    {
        internal static bool IsValid(this MemberDisplayFormat value)
        {
            return MemberDisplayFormat.SingleLine <= value && value <= MemberDisplayFormat.Hidden;
        }
    }
}
