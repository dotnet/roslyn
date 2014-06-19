
namespace Roslyn.Compilers.CSharp
{
    internal class WithDummyTypeParametersBinder : WithTypeParametersBinder
    {
        private readonly MultiDictionary<string, TypeParameterSymbol> typeParameterMap;

        internal WithDummyTypeParametersBinder(MultiDictionary<string, TypeParameterSymbol> typeParameterMap, Binder next)
            : base(next)
        {
            this.typeParameterMap = typeParameterMap;
        }

        protected override MultiDictionary<string, TypeParameterSymbol> TypeParameterMap
        {
            get
            {
                return typeParameterMap;
            }
        }
    }
}