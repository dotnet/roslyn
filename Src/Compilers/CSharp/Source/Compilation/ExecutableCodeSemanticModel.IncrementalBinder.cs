namespace Roslyn.Compilers.CSharp
{
    partial class ExecutableCodeSemanticModel
    {
        internal sealed class IncrementalBinder : Binder
        {
            private readonly ExecutableCodeSemanticModel model;

            internal IncrementalBinder(ExecutableCodeSemanticModel model, Binder next)
                : base(next)
            {
                this.model = model;
            }

            internal override Symbol ContainingMember
            {
                get { return this.model.MemberSymbol; }
            }

            protected override Binder GetBinder(SyntaxNode node)
            {
                Binder binder;
                if (this.model.BinderMap.TryGetValue(node, out binder))
                {
                    return new IncrementalBinder(this.model, binder);
                }
                return null;
            }
        }
    }
}
