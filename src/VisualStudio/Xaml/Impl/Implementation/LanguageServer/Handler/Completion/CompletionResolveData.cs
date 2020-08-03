// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer.Handler
{
    internal class CompletionResolveData
    {
        public Guid ProjectGuid { get; set; }
        public Guid DocumentGuid { get; set; }

        public Position Position { get; set; }

        public string DisplayText { get; set; }
    }
}
