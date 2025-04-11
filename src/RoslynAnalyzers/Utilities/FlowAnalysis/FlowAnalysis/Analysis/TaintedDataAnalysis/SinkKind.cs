// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    public enum SinkKind
    {
        Sql,
        Dll,
        InformationDisclosure,
        Xss,
        FilePathInjection,
        ProcessCommand,
        Regex,
        Ldap,
        Redirect,
        XPath,
        Xml,
        Xaml,
        ZipSlip,
        HardcodedEncryptionKey,
        HardcodedCertificate,
    }
}
