// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Symbols
{
    internal abstract class CommonAnonymousTypeManager
    {
        /// <summary>
        /// We should not see new anonymous types from source after we finished emit phase. 
        /// If this field is true, the collection is sealed; in DEBUG it also is used to check the assertion.
        /// </summary>
        private ThreeState templatesSealed = ThreeState.False;

        /// <summary>
        /// Collection of anonymous type templates is sealed 
        /// </summary>
        internal bool AreTemplatesSealed
        {
            get { return templatesSealed == ThreeState.True; }
        }

        protected void SealTemplates()
        {
            templatesSealed = ThreeState.True;
        }
    }
}
