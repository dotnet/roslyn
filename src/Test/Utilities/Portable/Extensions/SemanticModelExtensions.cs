// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Test.Extensions
{
    public static class SemanticModelExtensions
    {
        public static IOperation GetOperationInternal(this SemanticModel model, SyntaxNode node)
        {
            // Invoke the GetOperationInternal API to by-pass the IOperation feature flag check.
            return model.GetOperationInternal(node);
        }
    }
}
