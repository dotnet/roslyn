// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit;

/// <summary>
/// Values of synthesized HotReloadException.Code field.
/// </summary>
internal enum HotReloadExceptionCode
{
    DeletedLambdaInvoked = 1,
    DeletedMethodInvoked = 2,
    CannotResumeSuspendedIteratorMethod = 3,
    CannotResumeSuspendedAsyncMethod = 4,
    UnsupportedChangeToCapturedVariables = 5,
}

internal static class HotReloadExceptionCodeExtensions
{
    public static string GetExceptionMessage(this HotReloadExceptionCode code)
        => code switch
        {
            HotReloadExceptionCode.DeletedLambdaInvoked => CodeAnalysisResources.EncDeletedLambdaInvoked,
            HotReloadExceptionCode.DeletedMethodInvoked => CodeAnalysisResources.EncDeletedMethodInvoked,
            HotReloadExceptionCode.CannotResumeSuspendedIteratorMethod => CodeAnalysisResources.EncCannotResumeSuspendedIteratorMethod,
            HotReloadExceptionCode.CannotResumeSuspendedAsyncMethod => CodeAnalysisResources.EncCannotResumeSuspendedAsyncMethod,
            HotReloadExceptionCode.UnsupportedChangeToCapturedVariables => CodeAnalysisResources.EncLambdaRudeEdit_CapturedVariables,
            _ => throw ExceptionUtilities.UnexpectedValue(code)
        };

    public static int GetExceptionCodeValue(this HotReloadExceptionCode code)
        => -(int)code;
}
