// When the preprocessor directive TEMPNAMES is set, we give names to compiler-generated
// ref and out local variables to assist with debugging the compiler.
//#define TEMPNAMES

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    //
    // Why temporaries may need to store a ref. Example:
  
    // A().M(y : out B()[C()], x : D());
  
    // This needs to have the semantics of
  
    // a = A();
    // y = out B()[C()];
    // x = D();
    // a.M(x, y);
  
    // so that all the side effects (including the exception when C() is out of range!) happen
    // in the right order. Thus, y might be a byref temporary.
    //

    /// <summary>
    /// TempLocalSymbol is special kind of LocalSymbol that can have ref kind.
    /// 
    /// The semantics of LHS ByRef local is roughly the same as of a ByRef argument
    ///    EmitAssignment will do EmitAddress for RHS and then assign.
    ///                                     
    /// The semantics of RHS ByRef local is roughly the same as of a ByRef parameter
    ///    EmitExpression   will load the value which local is refering to.
    ///    EmitAddress      will load the actual local.
    /// </summary>
    internal sealed class TempLocalSymbol : SynthesizedLocal
    {
        private readonly RefKind refKind;
#if TEMPNAMES
        private readonly string name;
#endif

        internal TempLocalSymbol(TypeSymbol type, RefKind refKind, MethodSymbol containingMethod) : base(containingMethod, type, null)
        {
            this.refKind = refKind;
#if TEMPNAMES
            this.name = "_" + Interlocked.Increment(ref nextName);
#endif
        }

#if TEMPNAMES
        private static int nextName = 0;

        public override string Name
        {
            get
            {
                return this.name;
            }
        }
#endif

        internal override RefKind RefKind
        {
            get { return this.refKind; }
        }
    }
}
