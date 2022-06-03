// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// Closed\References\Debugger\Concord\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

using System;

//
// Summary:
//     This attribute allows managed concord component to opt-in reporting of non-fatal
//     exception to watson.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class DkmReportNonFatalWatsonExceptionAttribute : Attribute
{
    public DkmReportNonFatalWatsonExceptionAttribute() { }

    //
    // Summary:
    //     Specify the type of exception to exclude from reporting
    public Type ExcludeExceptionType { get; set; }
}
