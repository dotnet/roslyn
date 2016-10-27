// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    [Guid(Guids.RoslynOptionPageInternalDiagnosticsIdString)]
    internal class InternalDiagnosticsPage : AbstractOptionPage
    {
        protected override AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider)
        {
            return new InternalOptionsControl(nameof(InternalDiagnosticsOptions), serviceProvider);
        }
    }
}
