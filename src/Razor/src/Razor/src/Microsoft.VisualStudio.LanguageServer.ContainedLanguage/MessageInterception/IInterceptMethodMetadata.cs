// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage.MessageInterception;

internal interface IInterceptMethodMetadata
{
    // this must match the name from InterceptMethodAttribute
    IEnumerable<string> InterceptMethods { get; }

    // this must match the name from ContentTypeAttribute
    IEnumerable<string> ContentTypes { get; }
}
