// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Formatting
{
    internal readonly struct LineColumnRule(
LineColumnRule.SpaceOperations spaceOperation,
LineColumnRule.LineOperations lineOperation,
LineColumnRule.IndentationOperations indentationOperation,
        int lines,
        int spaces,
        int indentation)
    {
        public readonly SpaceOperations SpaceOperation = spaceOperation;
        public readonly LineOperations LineOperation = lineOperation;
        public readonly IndentationOperations IndentationOperation = indentationOperation;

        public readonly int Lines = lines;
        public readonly int Spaces = spaces;
        public readonly int Indentation = indentation;

        public LineColumnRule With(int? lines = null, int? spaces = null, int? indentation = null, LineOperations? lineOperation = null, SpaceOperations? spaceOperation = null, IndentationOperations? indentationOperation = null)
            => new(
                spaceOperation == null ? SpaceOperation : spaceOperation.Value,
                lineOperation == null ? LineOperation : lineOperation.Value,
                indentationOperation == null ? IndentationOperation : indentationOperation.Value,
                lines == null ? Lines : lines.Value,
                spaces == null ? Spaces : spaces.Value,
                indentation == null ? Indentation : indentation.Value);

        public static readonly LineColumnRule Preserve =
            new(
                SpaceOperations.Preserve,
                LineOperations.Preserve,
                IndentationOperations.Preserve,
                lines: 0,
                spaces: 0,
                indentation: 0);

        public static LineColumnRule PreserveWithGivenSpaces(int spaces)
            => new(
                SpaceOperations.Preserve,
                LineOperations.Preserve,
                IndentationOperations.Given,
                lines: 0,
                spaces,
                indentation: 0);

        public static LineColumnRule PreserveLinesWithDefaultIndentation(int lines)
            => new(
                SpaceOperations.Preserve,
                LineOperations.Preserve,
                IndentationOperations.Default,
                lines,
                spaces: 0,
                indentation: -1);

        public static LineColumnRule PreserveLinesWithGivenIndentation(int lines)
            => new(
                SpaceOperations.Preserve,
                LineOperations.Preserve,
                IndentationOperations.Given,
                lines,
                spaces: 0,
                indentation: -1);

        public static LineColumnRule PreserveLinesWithAbsoluteIndentation(int lines, int indentation)
            => new(
                SpaceOperations.Preserve,
                LineOperations.Preserve,
                IndentationOperations.Absolute,
                lines,
                spaces: 0,
                indentation);

        public static readonly LineColumnRule PreserveLinesWithFollowingPrecedingIndentation =
            new(
                SpaceOperations.Preserve,
                LineOperations.Preserve,
                IndentationOperations.Follow,
                lines: -1,
                spaces: 0,
                indentation: -1);

        public static LineColumnRule ForceSpaces(int spaces)
            => new(
                SpaceOperations.Force,
                LineOperations.Preserve,
                IndentationOperations.Preserve,
                lines: 0,
                spaces,
                indentation: 0);

        public static LineColumnRule PreserveSpacesOrUseDefaultIndentation(int spaces)
            => new(
                SpaceOperations.Preserve,
                LineOperations.Preserve,
                IndentationOperations.Default,
                lines: 0,
                spaces,
                indentation: -1);

        public static LineColumnRule ForceSpacesOrUseDefaultIndentation(int spaces)
            => new(
                SpaceOperations.Force,
                LineOperations.Preserve,
                IndentationOperations.Default,
                lines: 0,
                spaces,
                indentation: -1);

        public static LineColumnRule ForceSpacesOrUseFollowIndentation(int indentation)
            => new(
                SpaceOperations.Force,
                LineOperations.Preserve,
                IndentationOperations.Follow,
                lines: 0,
                spaces: 1,
                indentation);

        public enum SpaceOperations
        {
            Preserve,
            Force
        }

        public enum LineOperations
        {
            Preserve,
            Force
        }

        public enum IndentationOperations
        {
            Absolute,
            Default,
            Given,
            Follow,
            Preserve
        }
    }
}
