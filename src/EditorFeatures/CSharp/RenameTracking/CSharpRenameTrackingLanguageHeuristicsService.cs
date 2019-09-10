// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.RenameTracking
{
    [ExportLanguageService(typeof(IRenameTrackingLanguageHeuristicsService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpRenameTrackingLanguageHeuristicsService : IRenameTrackingLanguageHeuristicsService
    {
        [ImportingConstructor]
        public CSharpRenameTrackingLanguageHeuristicsService()
        {
        }

        public bool IsIdentifierValidForRenameTracking(string name)
        {
            return name != "var" && name != "dynamic" && name != "_";
        }
    }
}
