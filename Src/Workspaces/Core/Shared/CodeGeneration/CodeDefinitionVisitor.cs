using System.Diagnostics.CodeAnalysis;

namespace Roslyn.Services.Shared.CodeGeneration
{
    [ExcludeFromCodeCoverage]
    internal abstract class CodeDefinitionVisitor<TResult> : ICodeDefinitionVisitor<TResult>
    {
        protected virtual TResult DefaultVisit(CodeDefinition definition)
        {
            return default(TResult);
        }

        public virtual TResult Visit(AddExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(ArgumentDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(AsExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(AssignExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(BaseExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(BinaryAndExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(BinaryOrExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(CastExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(ConditionalExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(ConstantExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(DefaultExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(ElementAccessExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(ValueEqualsExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(ReferenceEqualsExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(ExpressionStatementDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(GenericNameDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(IdentifierNameDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(IfStatementDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(InvocationExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(IsExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(LocalDeclarationStatementDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(LogicalAndExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(LogicalOrExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(LogicalNotExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(MemberAccessExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(MultiplyExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(NegateExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(ValueNotEqualsExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(ReferenceNotEqualsExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(ObjectCreationExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(OrAssignExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(QualifiedNameDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(RawExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(ReturnStatementDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(ThisExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(ThrowStatementDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(TypeReferenceExpressionDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(VariableDeclaratorDefinition definition)
        {
            return DefaultVisit(definition);
        }

        public virtual TResult Visit(VariableDeclarationDefinition definition)
        {
            return DefaultVisit(definition);
        }
        
        public virtual TResult Visit(UsingStatementDefinition definition)
        {
            return DefaultVisit(definition);
        }
    }
}