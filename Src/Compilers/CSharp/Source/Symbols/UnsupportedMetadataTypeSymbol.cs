// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class UnsupportedMetadataTypeSymbol : ErrorTypeSymbol
    {
        private readonly BadImageFormatException mrEx;

        internal UnsupportedMetadataTypeSymbol(BadImageFormatException mrEx = null)
        {
            this.mrEx = mrEx;
        }

        internal override DiagnosticInfo ErrorInfo
        {
            get
            {
                return new CSDiagnosticInfo(ErrorCode.ERR_BogusType, string.Empty);
            }
        }

        internal override bool MangleName
        {
            get
            {
                return false;
            }
        }
    }
}