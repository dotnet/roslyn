using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using Roslyn.Compilers.Collections;

namespace Roslyn.Compilers.CSharp
{
    internal abstract class SpeculatableMemberBinderContext : MemberBinderContext
    {
        internal SpeculatableMemberBinderContext(Location location, Symbol accessor, BinderContext next)
            : base(location, accessor, next)
        {
        }

        internal abstract SyntaxBinding BindExpression(BinderContext context, ExpressionSyntax expression);
        internal abstract SyntaxBinding BindStatement(BinderContext context, StatementSyntax statement);
    }
}