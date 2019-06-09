// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal enum OperatorPlacementWhenWrappingPreference
    {
        BeginningOfLine,
        EndOfLine,
    }

    internal static class OperatorPlacementUtilities
    {
        private const string end_of_line = "end_of_line";
        private const string beginning_of_line = "beginning_of_line";

        // Default to beginning_of_line if we don't know the value.
        public static string GetEditorConfigString(OperatorPlacementWhenWrappingPreference value)
            => value == OperatorPlacementWhenWrappingPreference.EndOfLine ? end_of_line : beginning_of_line;

        public static Optional<OperatorPlacementWhenWrappingPreference> Parse(string optionString)
        {
            if (CodeStyleHelpers.TryGetCodeStyleValueAndOptionalNotification(
                    optionString, out var value, out var notificationOpt))
            {
                switch (value)
                {
                    case end_of_line: return OperatorPlacementWhenWrappingPreference.EndOfLine;
                    case beginning_of_line: return OperatorPlacementWhenWrappingPreference.BeginningOfLine;
                }
            }

            // Default to beginning_of_line if we get something we don't understand.
            return OperatorPlacementWhenWrappingPreference.BeginningOfLine;
        }
    }
}
