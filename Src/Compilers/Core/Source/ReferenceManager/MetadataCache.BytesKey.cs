namespace Roslyn.Compilers
{
    partial class MetadataCache
    {
        internal sealed class BytesKey : CacheKey
        {
            public string UniqueName { get; private set; }

            public BytesKey(string uniqueName)
            {
                this.UniqueName = uniqueName;
            }

            public override int GetHashCode()
            {
                return NameComparer.GetHashCode(this.UniqueName);
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as BytesKey);
            }

            public override bool Equals(CacheKey obj)
            {
                return Equals(obj as BytesKey);
            }

            public bool Equals(BytesKey other)
            {
                if (other == null)
                {
                    return false;
                }

                if (this == other)
                {
                    return true;
                }

                return NameComparer.Equals(this.UniqueName, other.UniqueName);
            }
        }
    }
}