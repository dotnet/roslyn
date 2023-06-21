// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Denotes the kind of reference.
    /// </summary>
    public enum RefKind : byte
    {
        /// <summary>
        /// Indicates a "value" parameter or return type.
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates a "ref" parameter or return type.
        /// </summary>
        Ref = 1,

        /// <summary>
        /// Indicates an "out" parameter.
        /// </summary>
        Out = 2,

        /// <summary>
        /// Indicates an "in" parameter.
        /// </summary>
        In = 3,

        /// <summary>
        /// Indicates a "ref readonly" return type.
        /// </summary>
        RefReadOnly = 3,

        // NOTE: There is an additional value of this enum type - RefKindExtensions.StrictIn == RefKind.In + 1
        //       It is used internally during lowering. 
        //       Consider that when adding values or changing this enum in some other way.

        /// <summary>
        /// Indicates a "ref readonly" parameter.
        /// </summary>
        RefReadOnlyParameter = 5, // PROTOTYPE: Change to 4 to make public values sequential.
    }

    internal static class RefKindExtensions
    {
        internal static string ToParameterDisplayString(this RefKind kind)
        {
            switch (kind)
            {
                case RefKind.Out: return "out";
                case RefKind.Ref: return "ref";
                case RefKind.In: return "in";
                case RefKind.RefReadOnlyParameter: return "ref readonly";
                default: throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        internal static string ToArgumentDisplayString(this RefKind kind)
        {
            switch (kind)
            {
                case RefKind.Out: return "out";
                case RefKind.Ref: return "ref";
                case RefKind.In: return "in";
                default: throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        internal static string ToParameterPrefix(this RefKind kind)
        {
            switch (kind)
            {
                case RefKind.Out: return "out ";
                case RefKind.Ref: return "ref ";
                case RefKind.In: return "in ";
                case RefKind.RefReadOnlyParameter: return "ref readonly ";
                case RefKind.None: return string.Empty;
                default: throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        // Used internally to track `In` arguments that were specified with `In` modifier
        // as opposed to those that were specified with no modifiers and matched `In` parameter.
        // There is at least one kind of analysis that cares about this distinction - hoisting
        // of variables to the frame for async rewriting: a variable that was passed without the
        // `In` modifier may be correctly captured by value or by reference.
        internal const RefKind StrictIn = RefKind.In + 1;
    }
}
