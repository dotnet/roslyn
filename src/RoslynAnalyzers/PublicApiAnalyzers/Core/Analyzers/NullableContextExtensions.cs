// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Analyzer.Utilities.Lightup
{
    internal static class NullableContextExtensions
    {
        private static bool IsFlagSet(NullableContext context, NullableContext flag) =>
            (context & flag) == flag;

        extension(NullableContext context)
        {
            /// <summary>
            /// Returns whether nullable warnings are enabled for this context.
            /// </summary>
            public bool WarningsEnabled() =>
                IsFlagSet(context, NullableContext.WarningsEnabled);

            /// <summary>
            /// Returns whether nullable annotations are enabled for this context.
            /// </summary>
            public bool AnnotationsEnabled() =>
                IsFlagSet(context, NullableContext.AnnotationsEnabled);

            /// <summary>
            /// Returns whether the nullable warning state was inherited from the project default for this file type.
            /// </summary>
            public bool WarningsInherited() =>
                IsFlagSet(context, NullableContext.WarningsContextInherited);

            /// <summary>
            /// Returns whether the nullable annotation state was inherited from the project default for this file type.
            /// </summary>
            public bool AnnotationsInherited() =>
                IsFlagSet(context, NullableContext.AnnotationsContextInherited);
        }
    }
}
