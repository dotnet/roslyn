// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Receives notifications of each <see cref="SyntaxNode"/> in the compilation before generation runs
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
    /// 
    /// An <see cref="ISourceGenerator"/> may provide only a single <see cref="ISyntaxReceiver"/> or
    /// <see cref="ISyntaxContextReceiver" />, not both.
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

    /// <summary>
    /// Receives notifications of each <see cref="SyntaxNode"/> in the compilation, along with a  
    /// <see cref="SemanticModel"/> that can be queried to obtain more information, before generation
    /// runs.
    /// </summary>
    /// <remarks>
    /// A <see cref="ISourceGenerator"/> can provide an instance of <see cref="ISyntaxContextReceiver"/>
    /// via a <see cref="SyntaxReceiverCreator"/>.
    /// 
    /// The compiler will invoke the <see cref="SyntaxReceiverCreator"/> prior to generation to 
    /// obtain an instance of <see cref="ISyntaxContextReceiver"/>. This instance will have its 
    /// <see cref="OnVisitSyntaxNode(GeneratorSyntaxContext)"/> called for every syntax node
    /// in the compilation.
    /// 
    /// The <see cref="ISyntaxContextReceiver"/> can record any information about the nodes visited. 
    /// During <see cref="ISourceGenerator.Execute(GeneratorExecutionContext)"/> the generator can obtain the 
    /// created instance via the <see cref="GeneratorExecutionContext.SyntaxContextReceiver"/> property. The
    /// information contained can be used to perform final generation.
    /// 
    /// A new instance of <see cref="ISyntaxContextReceiver"/> is created per-generation, meaning the instance
    /// is free to store state without worrying about lifetime or reuse. 
    /// 
    /// An <see cref="ISourceGenerator"/> may provide only a single <see cref="ISyntaxReceiver"/> or
    /// <see cref="ISyntaxContextReceiver" />, not both.
    /// </remarks>
    public interface ISyntaxContextReceiver
    {
        void OnVisitSyntaxNode(GeneratorSyntaxContext context);
    }

    /// <summary>
    /// Allows a generator to provide instances of an <see cref="ISyntaxContextReceiver"/>
    /// </summary>
    /// <returns>An instance of an <see cref="ISyntaxContextReceiver"/></returns>
    public delegate ISyntaxContextReceiver? SyntaxContextReceiverCreator();

}
