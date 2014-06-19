
namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// IndexerNameAttribute is special because it affects the construction of the member list.
    /// If we are currently decoding IndexerNameAttribute, then we have to assume that we might
    /// depend on members of a type without a complete member list.  We protect ourselves by
    /// using a special binder that will only allow lookup to find "safe" members
    /// (see NamedTypeSymbol.GetEarlyAttributeDecodingMembers).
    /// </summary>
    internal sealed class IndexerNameAttributeFallbackBinder : Binder
    {
        internal IndexerNameAttributeFallbackBinder(Binder binder)
            : base(binder)
        {
        }

        internal override bool IsEarlyAttributeBinder
        {
            get
            {
                return true;
            }
        }
    }
}
