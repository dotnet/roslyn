// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class MultipleForwardedTypeSymbol : ErrorTypeSymbol
    {
        private readonly MetadataTypeName _metadataTypeName;

        private readonly AssemblySymbol _assembly1;

        private readonly AssemblySymbol _assembly2;

        internal MultipleForwardedTypeSymbol(MetadataTypeName metadataTypeName, AssemblySymbol assembly1, AssemblySymbol assembly2)
        {
            this._metadataTypeName = metadataTypeName;
            this._assembly1 = assembly1;
            this._assembly2 = assembly2;
        }

        internal override DiagnosticInfo ErrorInfo
        {
            get
            {
                return new CSDiagnosticInfo(ErrorCode.ERR_TypeForwardedToMultipleAssemblies, this._metadataTypeName.FullName, this._assembly1.Name, this._assembly2.Name);
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
