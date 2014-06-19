using System.Collections.Generic;

namespace Roslyn.Compilers.CSharp
{
    partial class NamedTypeSymbolBase
    {
        internal sealed class MembersAndInitializers
        {
            internal Dictionary<string, ReadOnlyArray<Symbol>> Members { get; set; }
            internal ReadOnlyArray<ReadOnlyArray<FieldInitializer>> StaticInitializers { get; set; }
            internal ReadOnlyArray<ReadOnlyArray<FieldInitializer>> InstanceInitializers { get; set; }
        }
    }
}