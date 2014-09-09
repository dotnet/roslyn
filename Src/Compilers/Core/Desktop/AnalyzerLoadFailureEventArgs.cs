// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public class AnalyzerLoadFailureEventArgs : EventArgs
    {
        public enum FailureErrorCode
        {
            None,
            UnableToLoadAnalyzer,
            UnableToCreateAnalyzer,
            NoAnalyzers
        }

        public readonly string TypeName;
        public readonly Exception Exception;
        public readonly FailureErrorCode ErrorCode;

        public AnalyzerLoadFailureEventArgs(FailureErrorCode errorCode, Exception ex, string typeName)
        {
            this.TypeName = typeName;
            this.ErrorCode = errorCode;
            this.Exception = ex;
        }
    }
}