namespace Roslyn.Services.Shared.CodeGeneration
{
    internal interface ICodeDefinitionVisitor
    {
        void Visit(AddExpressionDefinition definition);
        void Visit(ArgumentDefinition definition);
        void Visit(AsExpressionDefinition definition);
        void Visit(AssignExpressionDefinition definition);
        void Visit(BaseExpressionDefinition definition);
        void Visit(BinaryAndExpressionDefinition definition);
        void Visit(BinaryOrExpressionDefinition definition);
        void Visit(CastExpressionDefinition definition);
        void Visit(ConditionalExpressionDefinition definition);
        void Visit(ConstantExpressionDefinition definition);
        void Visit(DefaultExpressionDefinition definition);
        void Visit(ElementAccessExpressionDefinition definition);
        void Visit(ValueEqualsExpressionDefinition definition);
        void Visit(ReferenceEqualsExpressionDefinition definition);
        void Visit(ExpressionStatementDefinition definition);
        void Visit(GenericNameDefinition definition);
        void Visit(IdentifierNameDefinition definition);
        void Visit(IfStatementDefinition definition);
        void Visit(InvocationExpressionDefinition definition);
        void Visit(IsExpressionDefinition definition);
        void Visit(LocalDeclarationStatementDefinition definition);
        void Visit(LogicalAndExpressionDefinition definition);
        void Visit(LogicalOrExpressionDefinition definition);
        void Visit(LogicalNotExpressionDefinition definition);
        void Visit(MemberAccessExpressionDefinition definition);
        void Visit(MultiplyExpressionDefinition definition);
        void Visit(NegateExpressionDefinition definition);
        void Visit(ValueNotEqualsExpressionDefinition definition);
        void Visit(ReferenceNotEqualsExpressionDefinition definition);
        void Visit(ObjectCreationExpressionDefinition definition);
        void Visit(OrAssignExpressionDefinition definition);
        void Visit(QualifiedNameDefinition definition);
        void Visit(RawExpressionDefinition definition);
        void Visit(ReturnStatementDefinition definition);
        void Visit(ThisExpressionDefinition definition);
        void Visit(ThrowStatementDefinition definition);
        void Visit(TypeReferenceExpressionDefinition definition);
        void Visit(VariableDeclaratorDefinition definition);
        void Visit(VariableDeclarationDefinition definition);
        void Visit(UsingStatementDefinition definition);
    }
}