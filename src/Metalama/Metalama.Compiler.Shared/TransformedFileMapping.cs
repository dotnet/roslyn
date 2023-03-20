// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Metalama.Compiler;

public class TransformedFileMapping
{
    public static string FileName => "filemap.json";

    public string OldPath { get; }
    public string NewPath { get; }

    public TransformedFileMapping(string oldPath, string newPath)
    {
        OldPath = oldPath;
        NewPath = newPath;
    }
}
