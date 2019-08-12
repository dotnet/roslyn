// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking;
using System.Composition;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare
{
    [ExportLanguageService(typeof(IRenameTrackingLanguageHeuristicsService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspRenameTrackingLanguageHeuristicsService : IRenameTrackingLanguageHeuristicsService
    {
        public bool IsIdentifierValidForRenameTracking(string name)
        {
            return false;
        }
    }
}
