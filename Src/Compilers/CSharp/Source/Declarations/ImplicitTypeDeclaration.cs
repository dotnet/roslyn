using System.Collections.Generic;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// Represents a type declaration that wraps member declarations and statements used in a
    /// namespace declaration or top-level code. Such code could either occur in global script or it
    /// could be a misplaced global code that needs to be removed. 
    /// </summary>
    internal sealed class ImplicitTypeDeclaration : SingleTypeDeclaration
    {
        /// <summary>
        /// Script class declaration constructor.
        /// </summary>
        internal ImplicitTypeDeclaration(SyntaxReference memberContainer, ReadOnlyArray<SingleTypeDeclaration> children, ISet<string> memberNames, string name) :
            base(
            kind: DeclarationKind.Class,
            name: name,
            arity: 0,
            modifiers: DeclarationModifiers.Internal | DeclarationModifiers.Partial | DeclarationModifiers.Sealed,
            syntaxReference: memberContainer,
            nameLocation: new SourceLocation(memberContainer),
            memberNames: memberNames,
            children: children)
        {
        }

        /// <summary>
        /// Misplaced global code container constructor.
        /// </summary>
        internal ImplicitTypeDeclaration(SyntaxReference treeNode, ISet<string> memberNames)
            : this(treeNode, ReadOnlyArray<SingleTypeDeclaration>.Empty, memberNames, TypeSymbol.ImplicitTypeName)
        {
            IsMisplacedCodeContainer = true;
        }

        /// <summary>
        /// True if this implicit type represents misplaced global code, such as method declarations in namespaces, etc.
        /// </summary>
        public bool IsMisplacedCodeContainer { get; private set; }

        public override bool IsImplicitType
        {
            get { return true; }
        }
    }
}