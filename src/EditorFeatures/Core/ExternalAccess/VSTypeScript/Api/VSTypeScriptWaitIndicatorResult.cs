// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.Editor.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal enum VSTypeScriptWaitIndicatorResult
    {
        Canceled = WaitIndicatorResult.Canceled,
        Completed = WaitIndicatorResult.Completed
    }
}
