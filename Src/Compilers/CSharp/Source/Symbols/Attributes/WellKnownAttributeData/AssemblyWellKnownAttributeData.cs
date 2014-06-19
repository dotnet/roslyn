using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Information decoded from well-known custom attributes applied on an assembly.
    /// </summary>
    internal sealed class AssemblyWellKnownAttributeData : CommonAssemblyWellKnownAttributeData
    {
        #region ForwardedTypes

        private HashSet<NamedTypeSymbol> forwardedTypes;
        public HashSet<NamedTypeSymbol> ForwardedTypes
        {
            get
            {
                return this.forwardedTypes;
            }
            set
            {
                VerifySealed(expected: false);
                this.forwardedTypes = value;
                SetDataStored();
            }
        }
        #endregion
    }
}