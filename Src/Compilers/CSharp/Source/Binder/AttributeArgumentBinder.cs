namespace Roslyn.Compilers.CSharp
{
    internal sealed class AttributeArgumentBinder : Binder
    {
        internal AttributeArgumentBinder(Binder next)
            : base(next)
        {
        }

        //the raison d'etre of this class
        internal override BindingLocation  BindingLocation
        {
            get { return BindingLocation.AttributeArgument; }
        }
    }
}
