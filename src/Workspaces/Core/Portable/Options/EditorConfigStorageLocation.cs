// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Specifies that an option should be read from an .editorconfig file.
    /// </summary>
    internal sealed class EditorConfigStorageLocation : OptionStorageLocation
    {
        public string KeyName { get; }
        public Func<string, object> ParseFunction { get; }

        public EditorConfigStorageLocation(string keyName, Func<string, object> parseFunction)
        {
            KeyName = keyName;
            ParseFunction = parseFunction;
        }
    }
}
