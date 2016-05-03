// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    /// <summary>
    /// Used so we can mock out logging in unit tests.
    /// </summary>
    internal interface IPackageSearchLogService
    {
        void LogException(Exception e, string text);
        void LogInfo(string text);
    }
}
