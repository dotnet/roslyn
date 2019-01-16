// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    public enum SinkKind
    {
        Sql,
        Dll,
        InformationDisclosure,
        XSS,
        FilePathInjection,
        ProcessCommand,
        Regex,
    }
}
