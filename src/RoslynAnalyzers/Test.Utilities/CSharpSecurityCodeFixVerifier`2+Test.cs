// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Test.Utilities
{
    public static partial class CSharpSecurityCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public class Test : CSharpCodeFixVerifier<TAnalyzer, TCodeFix>.Test
        {
            static Test()
            {
                // If we have outdated defaults from the host unit test application targeting an older .NET Framework, use more
                // reasonable TLS protocol version for outgoing connections.
#pragma warning disable CA5364 // Do Not Use Deprecated Security Protocols
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable SYSLIB0014 // ServicePointManager is obsolete
                if (ServicePointManager.SecurityProtocol == (SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls))
#pragma warning restore SYSLIB0014 // ServicePointManager is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CA5364 // Do Not Use Deprecated Security Protocols
                {
#pragma warning disable CA5386 // Avoid hardcoding SecurityProtocolType value
#pragma warning disable SYSLIB0014 // ServicePointManager is obsolete
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
#pragma warning restore SYSLIB0014 // ServicePointManager is obsolete
#pragma warning restore CA5386 // Avoid hardcoding SecurityProtocolType value
                }
            }

            public Test()
            {
            }

            protected override ParseOptions CreateParseOptions()
            {
                var parseOptions = base.CreateParseOptions();
                return parseOptions.WithFeatures(parseOptions.Features.Concat(
                    new[] { new KeyValuePair<string, string>("flow-analysis", "true") }));
            }
        }
    }
}
