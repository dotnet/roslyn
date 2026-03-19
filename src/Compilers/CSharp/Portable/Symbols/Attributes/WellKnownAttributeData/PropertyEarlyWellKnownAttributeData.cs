// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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

        private string _indexerName;
        public string IndexerName
        {
            get
            {
                VerifySealed(expected: true);
                return _indexerName;
            }
            set
            {
                VerifySealed(expected: false);
                Debug.Assert(value != null);

                // This can be false if there are duplicate IndexerNameAttributes.
                // Just ignore the second one and let a later pass report an
                // appropriate diagnostic.
                if (_indexerName == null)
                {
                    _indexerName = value;
                    SetDataStored();
                }
            }
        }

        #endregion
    }
}
