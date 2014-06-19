namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// A Location describing a SyntaxToken that isn't part of any tree.  This can be used as the
    /// location of an error message in speculative binding APIs such as Bindings.BindType.
    /// </summary>
    internal sealed class TokenLocation : Location
    {
        private readonly SyntaxToken token;

        public TokenLocation(SyntaxToken token)
        {
            this.token = token;
        }

        public override LocationKind Kind
        {
            get { return LocationKind.None; }
        }

        public override SyntaxTree SourceTree
        {
            get
            {
                return token.SyntaxTree;
            }
        }

        public override int GetHashCode()
        {
            return this.token.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as TokenLocation);
        }

        public bool Equals(TokenLocation obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj != null && obj.token == this.token;
        }
    }
}