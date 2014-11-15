namespace Microsoft.CodeAnalysis.Host
{
    internal interface ICachedObjectOwner
    {
        object CachedObject { get; set; }
    }
}