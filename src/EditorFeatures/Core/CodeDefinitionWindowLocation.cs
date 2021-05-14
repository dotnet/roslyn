// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            : this(displayName, filePath, position.Start.Line, position.Start.Character)
        {
        }

        public CodeDefinitionWindowLocation(string displayName, FileLinePositionSpan position)
            : this(displayName, position.Path, position.Span)
        {
        }

        public override string ToString()
        {
            return base.ToString() + $" - (DisplayName: '{DisplayName}', FilePath: '{FilePath}', Line: '{Line}', '{Character}')";
        }
    }
}
