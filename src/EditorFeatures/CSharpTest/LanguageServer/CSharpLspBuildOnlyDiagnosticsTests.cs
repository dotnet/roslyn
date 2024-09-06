// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.LanguageServer;
using Microsoft.CodeAnalysis.Diagnostics.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities.LanguageServer;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.LanguageServer;

public class CSharpLspBuildOnlyDiagnosticsTests : AbstractLspBuildOnlyDiagnosticsTests
{
    protected override Type ErrorCodeType => typeof(ErrorCode);

    protected override Type LspBuildOnlyDiagnosticsType => typeof(CSharpLspBuildOnlyDiagnostics);

    protected override ImmutableArray<string> ExpectedDiagnosticCodes
    {
        get
        {
            var errorCodes = Enum.GetValues(typeof(ErrorCode));
            var supported = new CSharpCompilerDiagnosticAnalyzer().GetSupportedErrorCodes();
            var builder = ImmutableArray.CreateBuilder<string>();
            foreach (int errorCode in errorCodes)
            {
                if (!supported.Contains(errorCode) && errorCode > 0)
                {
                    var errorCodeD4String = errorCode.ToString("D4");
                    builder.Add("CS" + errorCodeD4String);
                }
            }

            return builder.ToImmutable();
        }
    }
}
