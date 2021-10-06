// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

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
