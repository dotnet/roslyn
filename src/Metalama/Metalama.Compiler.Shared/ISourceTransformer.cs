﻿using Microsoft.CodeAnalysis;

namespace Metalama.Compiler
{
    /// <summary>
    /// The interface required to implement a source transformer.
    /// </summary>
    public interface ISourceTransformer
    {
        /// <summary>
        /// Called to perform source transformation.
        /// </summary>
        void Execute(TransformerContext context);
    }
}
