// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Formatting
{
    internal readonly struct LineColumnRule
    {
        public readonly SpaceOperations SpaceOperation;
        public readonly LineOperations LineOperation;
        public readonly IndentationOperations IndentationOperation;

        public readonly int Lines;
        public readonly int Spaces;
        public readonly int Indentation;

        public LineColumnRule(
            SpaceOperations spaceOperation,
            LineOperations lineOperation,
            IndentationOperations indentationOperation,
            int lines,
            int spaces,
            int indentation)
        {
            SpaceOperation = spaceOperation;
            LineOperation = lineOperation;
            IndentationOperation = indentationOperation;
            Lines = lines;
            Spaces = spaces;
            Indentation = indentation;
        }

        public LineColumnRule With(int? lines = null, int? spaces = null, int? indentation = null, LineOperations? lineOperation = null, SpaceOperations? spaceOperation = null, IndentationOperations? indentationOperation = null)
            => new LineColumnRule(
                spaceOperation == null ? SpaceOperation : spaceOperation.Value,
                lineOperation == null ? LineOperation : lineOperation.Value,
                indentationOperation == null ? IndentationOperation : indentationOperation.Value,
                lines == null ? Lines : lines.Value,
                spaces == null ? Spaces : spaces.Value,
                indentation == null ? Indentation : indentation.Value);

        public static readonly LineColumnRule Preserve =
            new LineColumnRule(
                SpaceOperations.Preserve,
                LineOperations.Preserve,
                IndentationOperations.Preserve,
                lines: 0,
                spaces: 0,
                indentation: 0);

        public static LineColumnRule PreserveWithGivenSpaces(int spaces)
            => new LineColumnRule(
                SpaceOperations.Preserve,
                LineOperations.Preserve,
                IndentationOperations.Given,
                lines: 0,
                spaces,
                indentation: 0);

        public static LineColumnRule PreserveLinesWithDefaultIndentation(int lines)
            => new LineColumnRule(
                SpaceOperations.Preserve,
                LineOperations.Preserve,
                IndentationOperations.Default,
                lines,
                spaces: 0,
                indentation: -1);

        public static LineColumnRule PreserveLinesWithGivenIndentation(int lines)
            => new LineColumnRule(
                SpaceOperations.Preserve,
                LineOperations.Preserve,
                IndentationOperations.Given,
                lines,
                spaces: 0,
                indentation: -1);

        public static LineColumnRule PreserveLinesWithAbsoluteIndentation(int lines, int indentation)
            => new LineColumnRule(
                SpaceOperations.Preserve,
                LineOperations.Preserve,
                IndentationOperations.Absolute,
                lines,
                spaces: 0,
                indentation);

        public static readonly LineColumnRule PreserveLinesWithFollowingPrecedingIndentation =
            new LineColumnRule(
                SpaceOperations.Preserve,
                LineOperations.Preserve,
                IndentationOperations.Follow,
                lines: -1,
                spaces: 0,
                indentation: -1);

        public static LineColumnRule ForceSpaces(int spaces)
            => new LineColumnRule(
                SpaceOperations.Force,
                LineOperations.Preserve,
                IndentationOperations.Preserve,
                lines: 0,
                spaces,
                indentation: 0);

        public static LineColumnRule PreserveSpacesOrUseDefaultIndentation(int spaces)
            => new LineColumnRule(
                SpaceOperations.Preserve,
                LineOperations.Preserve,
                IndentationOperations.Default,
                lines: 0,
                spaces,
                indentation: -1);

        public static LineColumnRule ForceSpacesOrUseDefaultIndentation(int spaces)
            => new LineColumnRule(
                SpaceOperations.Force,
                LineOperations.Preserve,
                IndentationOperations.Default,
                lines: 0,
                spaces,
                indentation: -1);

        public static LineColumnRule ForceSpacesOrUseAbsoluteIndentation(int spacesOrIndentation)
            => new LineColumnRule(
                SpaceOperations.Force,
                LineOperations.Preserve,
                IndentationOperations.Absolute,
                lines: 0,
                spacesOrIndentation,
                spacesOrIndentation);

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
