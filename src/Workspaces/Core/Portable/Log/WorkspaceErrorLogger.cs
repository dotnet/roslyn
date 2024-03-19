// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.ErrorLogger;

[ExportWorkspaceService(typeof(IErrorLoggerService)), Export(typeof(IErrorLoggerService)), Shared]
internal class WorkspaceErrorLogger : IErrorLoggerService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public WorkspaceErrorLogger()
    {
    }

    public void LogException(object source, Exception exception)
        => Logger.Log(FunctionId.Extension_Exception, (source, exception) => source.GetType().Name + " : " + ToLogFormat(exception), source, exception, LogLevel.Error);

    private static string ToLogFormat(Exception exception)
        => exception.Message + Environment.NewLine + exception.StackTrace;
}

