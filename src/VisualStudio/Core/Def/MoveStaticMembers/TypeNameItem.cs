// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveStaticMembers;

internal sealed class TypeNameItem
{
    public string FullyQualifiedTypeName { get; }
    public INamedTypeSymbol? NamedType { get; }
    public string DeclarationFilePath { get; }
    public string DeclarationFileName { get; }
    public bool IsFromHistory { get; }
    public bool IsNew { get; }

    public TypeNameItem(bool isFromHistory, string declarationFile, INamedTypeSymbol type)
    {
        IsFromHistory = isFromHistory;
        IsNew = false;
        NamedType = type;
        FullyQualifiedTypeName = type.ToDisplayString();
        DeclarationFileName = PathUtilities.GetFileName(declarationFile);
        DeclarationFilePath = declarationFile;
    }

    public TypeNameItem(string fullyQualifiedTypeName)
    {
        IsFromHistory = false;
        IsNew = true;
        FullyQualifiedTypeName = fullyQualifiedTypeName;
        NamedType = null;
        DeclarationFileName = string.Empty;
        DeclarationFilePath = string.Empty;
    }

    public override string ToString() => FullyQualifiedTypeName;

    public static int CompareTo(TypeNameItem x, TypeNameItem y)
    {
        // sort so that history is first, then type name, then file name
        if (x.IsFromHistory ^ y.IsFromHistory)
        {
            // one is from history and the other isn't
            return x.IsFromHistory ? -1 : 1;
        }
        // compare by each namespace/finally type
        var xnames = x.FullyQualifiedTypeName.Split('.');
        var ynames = y.FullyQualifiedTypeName.Split('.');

        for (var i = 0; i < Math.Min(xnames.Length, ynames.Length); i++)
        {
            var comp = xnames[i].CompareTo(ynames[i]);
            if (comp != 0)
            {
                return comp;
            }
        }

        return x.DeclarationFileName.CompareTo(y.DeclarationFileName);
    }
}
