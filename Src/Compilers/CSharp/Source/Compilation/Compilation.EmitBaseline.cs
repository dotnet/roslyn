using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class Compilation
    {
        private sealed class EmitBaseline : Microsoft.CodeAnalysis.Emit.EmitBaseline
        {
            private readonly Compilation compilation;
            private readonly Guid encId;
            private readonly Microsoft.CodeAnalysis.Emit.GenerationDelta delta;

            public EmitBaseline(
                Compilation compilation,
                ModuleMetadata module,
                int ordinal,
                Guid encId,
                Microsoft.CodeAnalysis.Emit.GenerationDelta delta)
                : base(module, ordinal)
            {
                this.compilation = compilation;
                this.encId = encId;
                this.delta = delta;
            }

            internal override Guid EncId
            {
                get { return this.encId; }
            }

            internal override Microsoft.CodeAnalysis.Emit.Generation CreateGeneration(
                Microsoft.CodeAnalysis.Common.CommonCompilation commonCompilation,
                Microsoft.CodeAnalysis.Emit.LocalVariableSyntaxProvider localDeclarations,
                Microsoft.CodeAnalysis.Emit.LocalVariableMapProvider localMap)
            {
                var compilation = (Compilation)commonCompilation;
                var previousToNext = new SymbolMatcher(
                    this.compilation.SourceAssembly,
                    compilation.SourceAssembly,
                    caseSensitive: true);
                var delta = new Microsoft.CodeAnalysis.Emit.GenerationDelta(
                    previousToNext.Match(this.delta.TypesAdded),
                    previousToNext.Match(this.delta.EventsAdded),
                    previousToNext.Match(this.delta.FieldsAdded),
                    previousToNext.Match(this.delta.MethodsAdded),
                    previousToNext.Match(this.delta.PropertiesAdded),
                    tableEntriesAdded: this.delta.TableEntriesAdded,
                    blobStreamLengthAdded: this.delta.BlobStreamLengthAdded,
                    stringStreamLengthAdded: this.delta.StringStreamLengthAdded,
                    userStringStreamLengthAdded: this.delta.UserStringStreamLengthAdded,
                    guidStreamLengthAdded: this.delta.GuidStreamLengthAdded,
                    localNamesAddedOrChanged: this.delta.LocalNamesAddedOrChanged,
                    localNames: this.delta.LocalNames,
                    localDeclarationsAddedOrChanged: this.delta.LocalDeclarationsAddedOrChanged);
                return Generation.CreateNextGeneration(
                    compilation,
                    this.OriginalMetadata,
                    this.Ordinal,
                    encId,
                    delta,
                    localDeclarations,
                    localMap);
            }
        }
    }
}
