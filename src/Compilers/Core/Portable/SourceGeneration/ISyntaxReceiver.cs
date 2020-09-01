// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Receives notifications of each syntax node in the compilation before generation runs
    /// </summary>
    /// <remarks>
    /// A <see cref="ISourceGenerator"/> can provide an instance of <see cref="ISyntaxReceiver"/>
    /// via a <see cref="SyntaxReceiverCreator"/>.
    /// 
    /// The compiler will invoke the <see cref="SyntaxReceiverCreator"/> prior to generation to 
    /// obtain an instance of <see cref="ISyntaxReceiver"/>. This instance will have its 
    /// <see cref="OnVisitSyntaxNode(SyntaxNode)"/> called for every syntax node in the compilation.
    /// 
    /// The <see cref="ISyntaxReceiver"/> can record any information about the nodes visited. During
    /// <see cref="ISourceGenerator.Execute(GeneratorExecutionContext)"/> the generator can obtain the 
    /// created instance via the <see cref="GeneratorExecutionContext.SyntaxReceiver"/> property. The
    /// information contained can be used to perform final generation.
    /// 
    /// A new instance of <see cref="ISyntaxReceiver"/> is created per-generation, meaning the instance
    /// is free to store state without worrying about lifetime or reuse.
    /// </remarks>
    public interface ISyntaxReceiver
    {
        /// <summary>
        /// Called for each <see cref="SyntaxNode"/> in the compilation
        /// </summary>
        /// <param name="syntaxNode">The current <see cref="SyntaxNode"/> being visited</param>
        void OnVisitSyntaxNode(SyntaxNode syntaxNode);
    }

    /// <summary>
    /// Allows a generator to provide instances of an <see cref="ISyntaxReceiver"/>
    /// </summary>
    /// <returns>An instance of an <see cref="ISyntaxReceiver"/></returns>
    public delegate ISyntaxReceiver SyntaxReceiverCreator();
}
