// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking;
using System.Composition;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.LocalForwarders
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
