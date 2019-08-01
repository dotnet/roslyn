// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, RoslynMethods.ClassificationsName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynClassificationsHandler : ClassificationsHandler
    {
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, RoslynMethods.ClassificationsName)]
    internal class CSharpClassificationsHandler : ClassificationsHandler
    {
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, RoslynMethods.ClassificationsName)]
    internal class VisualBasicClassificationsHandler : ClassificationsHandler
    {
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, RoslynMethods.ClassificationsName)]
    internal class TypeScriptClassificationsHandler : ClassificationsHandler
    {
    }
}
