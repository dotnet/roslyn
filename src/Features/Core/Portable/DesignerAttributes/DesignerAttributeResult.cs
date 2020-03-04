// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.DesignerAttributes
{
    internal readonly struct DesignerAttributeResult
    {
        /// <summary>
        /// Designer attribute string
        /// </summary>
        public string DesignerAttributeArgument { get; }

        /// <summary>
        /// No designer attribute due to errors in the document
        /// </summary>
        public bool ContainsErrors { get; }

        /// <summary>
        /// The document asked is applicable for the designer attribute
        /// </summary>
        public bool Applicable { get; }

        public DesignerAttributeResult(string designerAttributeArgument, bool containsErrors, bool applicable)
        {
            DesignerAttributeArgument = designerAttributeArgument;
            ContainsErrors = containsErrors;
            Applicable = applicable;
        }
    }
}
