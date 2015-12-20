// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    internal struct CodeDefinitionWindowLocation
    {
        public string DisplayName { get; }
        public string FilePath { get; }
        public int Line { get; }
        public int Character { get; }

        public CodeDefinitionWindowLocation(string displayName, string filePath, int line, int character)
        {
            DisplayName = displayName;
            FilePath = filePath;
            Line = line;
            Character = character;
        }

        public CodeDefinitionWindowLocation(string displayName, string filePath, LinePositionSpan position)
            : this (displayName, filePath, position.Start.Line, position.Start.Character)
        {
        }

        public CodeDefinitionWindowLocation(string displayName, FileLinePositionSpan position)
            : this (displayName, position.Path, position.Span)
        {
        }
    }
}
