// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Formatting
{
    internal struct LineColumnRule
    {
        internal enum SpaceOperations
        {
            Preserve,
            Force
        }

        internal enum LineOperations
        {
            Preserve,
            Force
        }

        internal enum IndentationOperations
        {
            Absolute,
            Default,
            Given,
            Follow,
            Preserve
        }

        public LineColumnRule With(
            int? lines = null, int? spaces = null, int? indentation = null,
            LineOperations? lineOperation = null, SpaceOperations? spaceOperation = null, IndentationOperations? indentationOperation = null)
        {
            return new LineColumnRule
            {
                SpaceOperation = spaceOperation == null ? this.SpaceOperation : spaceOperation.Value,
                LineOperation = lineOperation == null ? this.LineOperation : lineOperation.Value,
                IndentationOperation = indentationOperation == null ? this.IndentationOperation : indentationOperation.Value,
                Lines = lines == null ? this.Lines : lines.Value,
                Spaces = spaces == null ? this.Spaces : spaces.Value,
                Indentation = indentation == null ? this.Indentation : indentation.Value
            };
        }

        public SpaceOperations SpaceOperation { get; private set; }
        public LineOperations LineOperation { get; private set; }
        public IndentationOperations IndentationOperation { get; private set; }

        public int Lines { get; private set; }
        public int Spaces { get; private set; }
        public int Indentation { get; private set; }

        public static LineColumnRule Preserve()
        {
            return new LineColumnRule
            {
                SpaceOperation = SpaceOperations.Preserve,
                LineOperation = LineOperations.Preserve,
                IndentationOperation = IndentationOperations.Preserve,
                Lines = 0,
                Spaces = 0,
                Indentation = 0
            };
        }

        public static LineColumnRule PreserveWithGivenSpaces(int spaces)
        {
            return new LineColumnRule
            {
                SpaceOperation = SpaceOperations.Preserve,
                LineOperation = LineOperations.Preserve,
                IndentationOperation = IndentationOperations.Given,
                Lines = 0,
                Spaces = spaces,
                Indentation = 0
            };
        }

        public static LineColumnRule PreserveLinesWithDefaultIndentation(int lines)
        {
            return new LineColumnRule
            {
                SpaceOperation = SpaceOperations.Preserve,
                LineOperation = LineOperations.Preserve,
                IndentationOperation = IndentationOperations.Default,
                Lines = lines,
                Spaces = 0,
                Indentation = -1
            };
        }

        public static LineColumnRule PreserveLinesWithGivenIndentation(int lines)
        {
            return new LineColumnRule
            {
                SpaceOperation = SpaceOperations.Preserve,
                LineOperation = LineOperations.Preserve,
                IndentationOperation = IndentationOperations.Given,
                Lines = lines,
                Spaces = 0,
                Indentation = -1
            };
        }

        public static LineColumnRule PreserveLinesWithAbsoluteIndentation(int lines, int indentation)
        {
            return new LineColumnRule
            {
                SpaceOperation = SpaceOperations.Preserve,
                LineOperation = LineOperations.Preserve,
                IndentationOperation = IndentationOperations.Absolute,
                Lines = lines,
                Spaces = 0,
                Indentation = indentation
            };
        }

        public static LineColumnRule PreserveLinesWithFollowingPrecedingIndentation()
        {
            return new LineColumnRule
            {
                SpaceOperation = SpaceOperations.Preserve,
                LineOperation = LineOperations.Preserve,
                IndentationOperation = IndentationOperations.Follow,
                Lines = -1,
                Spaces = 0,
                Indentation = -1
            };
        }

        public static LineColumnRule ForceSpaces(int spaces)
        {
            return new LineColumnRule
            {
                SpaceOperation = SpaceOperations.Force,
                LineOperation = LineOperations.Preserve,
                IndentationOperation = IndentationOperations.Preserve,
                Lines = 0,
                Spaces = spaces,
                Indentation = 0
            };
        }

        public static LineColumnRule PreserveSpacesOrUseDefaultIndentation(int spaces)
        {
            return new LineColumnRule
            {
                SpaceOperation = SpaceOperations.Preserve,
                LineOperation = LineOperations.Preserve,
                IndentationOperation = IndentationOperations.Default,
                Lines = 0,
                Spaces = spaces,
                Indentation = -1
            };
        }

        public static LineColumnRule ForceSpacesOrUseDefaultIndentation(int spaces)
        {
            return new LineColumnRule
            {
                SpaceOperation = SpaceOperations.Force,
                LineOperation = LineOperations.Preserve,
                IndentationOperation = IndentationOperations.Default,
                Lines = 0,
                Spaces = spaces,
                Indentation = -1
            };
        }

        public static LineColumnRule ForceSpacesOrUseAbsoluteIndentation(int spacesOrIndentation)
        {
            return new LineColumnRule
            {
                SpaceOperation = SpaceOperations.Force,
                LineOperation = LineOperations.Preserve,
                IndentationOperation = IndentationOperations.Absolute,
                Lines = 0,
                Spaces = spacesOrIndentation,
                Indentation = spacesOrIndentation
            };
        }
    }
}
