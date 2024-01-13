// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            return value is >= MemberDisplayFormat.SingleLine and <= MemberDisplayFormat.Hidden;
        }
    }
}
