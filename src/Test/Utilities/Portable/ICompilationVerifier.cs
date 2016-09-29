
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    internal interface ICompilationVerifier
    {
        ImmutableArray<ModuleMetadata> GetAllModuleMetadata();
        IModuleSymbol GetModuleSymbolForEmittedImage(ImmutableArray<byte> peImage, MetadataImportOptions importOptions);
        IModuleSymbol GetModuleSymbolForEmittedImage();
        ImmutableArray<byte> EmittedAssemblyData { get; }
    }
}
