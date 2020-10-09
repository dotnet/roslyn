// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ErrorLogger
{
    internal interface IErrorLoggerService : IWorkspaceService
    {
        void LogException(object source, Exception exception);
    }
}
