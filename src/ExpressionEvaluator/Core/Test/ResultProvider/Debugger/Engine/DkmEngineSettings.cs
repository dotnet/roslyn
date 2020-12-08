// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

using Microsoft.VisualStudio.Debugger.Evaluation;

namespace Microsoft.VisualStudio.Debugger
{
    public class DkmEngineSettings
    {
        public DkmLanguage GetLanguage(DkmCompilerId compilerId)
        {
            return new DkmLanguage(compilerId);
        }
    }
}
