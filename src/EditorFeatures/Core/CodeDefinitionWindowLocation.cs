// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        }
}
