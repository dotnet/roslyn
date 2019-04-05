// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
