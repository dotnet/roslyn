// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class UnsupportedMetadataTypeSymbol : ErrorTypeSymbol
    {
        private readonly BadImageFormatException? _mrEx;

        internal UnsupportedMetadataTypeSymbol(BadImageFormatException? mrEx = null)
        {
            _mrEx = mrEx;
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
