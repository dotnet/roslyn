using System;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// A Location describing a SyntaxNode that isn't part of any tree.  These can be used as the
    /// location of an error resulting from the speculative binding APIs such as Bindings.BindType.
    /// </summary>
    [Serializable]
    internal class NodeLocation : Location, IEquatable<NodeLocation>
    {
        private readonly TextSpan span;

        public NodeLocation(SyntaxNode node)
        {
            this.span = node.Span;
        }

        public override LocationKind Kind
        {
            get { return LocationKind.None; }
        }

        public override int GetHashCode()
        {
            return this.span.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as NodeLocation);
        }

        public bool Equals(NodeLocation obj)
        {
            return obj != null && obj.span == this.span;
        }
    }
}