using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class AwaitLoweringRewriterPass2
    {
        private class SpillFieldAllocator
        {
            private readonly SyntheticBoundNodeFactory F;
            private readonly TypeCompilationState CompilationState;
            private readonly KeyedStack<TypeSymbol, FieldSymbol> allocatedFields;
            private readonly Dictionary<BoundSpillTemp, FieldSymbol> realizedSpills;

            internal SpillFieldAllocator(SyntheticBoundNodeFactory F, TypeCompilationState CompilationState)
            {
                allocatedFields = new KeyedStack<TypeSymbol, FieldSymbol>();
                realizedSpills = new Dictionary<BoundSpillTemp, FieldSymbol>();
                this.F = F;
                this.CompilationState = CompilationState;
            }

            internal void AllocateFields(ReadOnlyArray<BoundSpillTemp> spills)
            {
                foreach (var spill in spills)
                {
                    AllocateField(spill);
                }
            }

            internal void AllocateField(BoundSpillTemp spill)
            {
                Debug.Assert(!realizedSpills.ContainsKey(spill));

                FieldSymbol field;
                if (!allocatedFields.TryPop(spill.Type, out field))
                {
                    field = F.SynthesizeField(spill.Type, GeneratedNames.SpillTempName(CompilationState.GenerateTempNumber()));
                }

                realizedSpills[spill] = field;
            }

            internal FieldSymbol GetField(BoundSpillTemp spill)
            {
                return realizedSpills[spill];
            }

            internal void Free(BoundSpillTemp spill)
            {
                FieldSymbol freeField = realizedSpills[spill];
                allocatedFields.Push(freeField.Type, freeField);
                realizedSpills.Remove(spill);
            }
        }
    }
}