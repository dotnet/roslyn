// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Symbols
{
    internal abstract class CommonAnonymousTypeManager
    {
        /// <summary>
        /// We should not see new anonymous types from source after we finished emit phase. 
        /// If this field is true, the collection is sealed; in DEBUG it also is used to check the assertion.
        /// </summary>
        private ThreeState _templatesSealed = ThreeState.False;

        /// <summary>
        /// Collection of anonymous type templates is sealed 
        /// </summary>
        internal bool AreTemplatesSealed
        {
            get { return _templatesSealed == ThreeState.True; }
        }

        protected void SealTemplates()
        {
            _templatesSealed = ThreeState.True;
        }
    }
}
