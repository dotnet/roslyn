using System.Collections.Generic;
using System.Diagnostics;

namespace Roslyn.Compilers.CSharp
{
    internal sealed class EvaluatedConstant
    {
        public EvaluatedConstant(ConstantValue value, IEnumerable<IDiagnostic> diagnostics)
        {
            this.Value = value;
            this.Diagnostics = diagnostics;
        }
        public ConstantValue Value { get; private set; }
        public IEnumerable<IDiagnostic> Diagnostics { get; private set; }
    }

    internal static class FieldSymbolExtensions
    {
        public static EvaluatedConstant EvaluateFieldConstant(this FieldSymbol symbol, SyntaxReference equalsValueNodeRef, ConstantFieldsInProgress inProgress)
        {
            Debug.Assert(inProgress != null);
            var diagnostics = DiagnosticBag.GetInstance();
            try
            {
                ConstantValue value;
                if (inProgress.Contains(symbol))
                {
                    var errorField = inProgress.ErrorField;
                    diagnostics.Add(ErrorCode.ERR_CircConstValue, errorField.Locations[0], errorField);
                    value = Roslyn.Compilers.ConstantValue.Bad;
                }
                else
                {
                    var compilation = ((SourceAssemblySymbol)symbol.ContainingAssembly).Compilation;
                    var binderFactory = compilation.GetBinderFactory(equalsValueNodeRef.SyntaxTree);

                    var newInProgress = inProgress.Add(symbol);

                    var equalsValueNode = (EqualsValueClauseSyntax)equalsValueNodeRef.GetSyntax();

                    var binder = binderFactory.GetBinder(equalsValueNode);
                    var inProgressBinder = new ConstantFieldsInProgressBinder(newInProgress, binder);

                    // CONSIDER: Compiler.BindFieldInitializer will make this same call on this same syntax node
                    // to determine the bound value for itself.  We expect this binding to be fairly cheap 
                    // (since constants tend to be simple) and it should only happen twice (regardless of the
                    // number of references to this constant).  If this becomes a performance bottleneck,
                    // the re-binding can be eliminated by caching the BoundNode on this SourceFieldSymbol and
                    // checking for a cached value before binding (here and in Compiler.BindFieldInitializer).
                    var boundValue = inProgressBinder.BindVariableInitializer(equalsValueNode, symbol.Type, diagnostics);
                    var initValueNodeLocation = inProgressBinder.Location(equalsValueNode.Value);

                    value = ConstantValueUtils.GetAndValidateConstantValue(boundValue, symbol, symbol.Type, initValueNodeLocation, diagnostics);
                }
                return new EvaluatedConstant(value, diagnostics.Seal());
            }
            finally
            {
                diagnostics.Free();
            }
        }
    }
}
