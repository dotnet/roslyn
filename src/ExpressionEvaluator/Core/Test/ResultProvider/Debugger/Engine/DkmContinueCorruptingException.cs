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
//     This attribute enables the behavior of treating continuable corrupting managed
//     exceptions(like NullReferenceException, ArgumentNullException etc...) as non-fatal
//     which will be reported to Microsoft via non-fatal watson channel and not crash
//     debugger process Note: non-continuable corrupting exceptions(like AccessViolationException,
//     StackOverflowException etc...) are still treated as fatal and will crash debugger
//     process
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class DkmContinueCorruptingExceptionAttribute : Attribute
{
    public DkmContinueCorruptingExceptionAttribute() { }
}
