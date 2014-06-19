using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A builder a SpillSequence, which accumulates the sequence's locals, spill temps, and statement while it is
    /// constructed.
    /// </summary>
    internal class SpillBuilder
    {
        private readonly ArrayBuilder<LocalSymbol> locals;
        private readonly ArrayBuilder<BoundSpillTemp> temps;
        private readonly ArrayBuilder<FieldSymbol> fields;
        private readonly ArrayBuilder<BoundStatement> statements;

        internal SpillBuilder()
        {
            locals = ArrayBuilder<LocalSymbol>.GetInstance();
            temps = ArrayBuilder<BoundSpillTemp>.GetInstance();
            statements = ArrayBuilder<BoundStatement>.GetInstance();
            fields = ArrayBuilder<FieldSymbol>.GetInstance();
        }

        internal ArrayBuilder<LocalSymbol> Locals
        {
            get { return locals; }
        }

        internal ArrayBuilder<BoundSpillTemp> Temps
        {
            get { return temps; }
        }

        internal ArrayBuilder<FieldSymbol> Fields
        {
            get { return fields; }
        }

        internal ArrayBuilder<BoundStatement> Statements
        {
            get { return statements; }
        }

        internal void Free()
        {
            locals.Free();
            statements.Free();
            temps.Free();
            fields.Free();
        }

        internal BoundExpression BuildSequenceAndFree(SyntheticBoundNodeFactory F, BoundExpression expression)
        {
            var result = (locals.Count > 0 || statements.Count > 0 || temps.Count > 0)
                ? F.SpillSequence(locals.ToReadOnly(), temps.ToReadOnly(), fields.ToReadOnly(), statements.ToReadOnly(), expression)
                : expression;

            Free();

            return result;
        }

        internal void AddSpill(SpillBuilder spill)
        {
            locals.AddRange(spill.locals);
            temps.AddRange(spill.temps);
            statements.AddRange(spill.statements);
        }

        internal void AddSpill(BoundSpillSequence spill)
        {
            locals.AddRange(spill.Locals);
            temps.AddRange(spill.SpillTemps);
            statements.AddRange(spill.Statements);
        }

        internal void AddSequence(SyntheticBoundNodeFactory F, BoundSequence sequence)
        {
            locals.AddRange(sequence.Locals);
            foreach (var sideEffect in sequence.SideEffects)
            {
                statements.Add(F.ExpressionStatement(sideEffect));
            }
        }
    }
}
