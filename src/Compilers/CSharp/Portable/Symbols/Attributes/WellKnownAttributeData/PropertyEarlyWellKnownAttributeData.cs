// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Information decoded from early well-known custom attributes applied on a property.
    /// </summary>
    internal sealed class PropertyEarlyWellKnownAttributeData : CommonPropertyEarlyWellKnownAttributeData
    {
        #region IndexerNameAttribute

        private string indexerName;
        public string IndexerName
        {
            get
            {
                VerifySealed(expected: true);
                return this.indexerName;
            }
            set
            {
                VerifySealed(expected: false);
                Debug.Assert(value != null);

                // This can be false if there are duplicate IndexerNameAttributes.
                // Just ignore the second one and let a later pass report an
                // appropriate diagnostic.
                if (this.indexerName == null)
                {
                    this.indexerName = value;
                    SetDataStored();
                }
            }
        }

        #endregion
    }
}
