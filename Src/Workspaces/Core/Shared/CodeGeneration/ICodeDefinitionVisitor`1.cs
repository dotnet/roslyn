namespace Roslyn.Services.Shared.CodeGeneration
{
    internal interface ICodeDefinitionVisitor<TResult>
    {
        TResult Visit(AddExpressionDefinition definition);
        TResult Visit(ArgumentDefinition definition);
        TResult Visit(AsExpressionDefinition definition);
        TResult Visit(AssignExpressionDefinition definition);
        TResult Visit(BaseExpressionDefinition definition);
        TResult Visit(BinaryAndExpressionDefinition definition);
        TResult Visit(BinaryOrExpressionDefinition definition);
        TResult Visit(CastExpressionDefinition definition);
        TResult Visit(ConditionalExpressionDefinition definition);
        TResult Visit(ConstantExpressionDefinition definition);
        TResult Visit(DefaultExpressionDefinition definition);
        TResult Visit(ElementAccessExpressionDefinition definition);
        TResult Visit(ValueEqualsExpressionDefinition definition);
        TResult Visit(ReferenceEqualsExpressionDefinition definition);
        TResult Visit(ExpressionStatementDefinition definition);
        TResult Visit(GenericNameDefinition definition);
        TResult Visit(IdentifierNameDefinition definition);
        TResult Visit(IfStatementDefinition definition);
        TResult Visit(InvocationExpressionDefinition definition);
        TResult Visit(IsExpressionDefinition definition);
        TResult Visit(LocalDeclarationStatementDefinition definition);
        TResult Visit(LogicalAndExpressionDefinition definition);
        TResult Visit(LogicalOrExpressionDefinition definition);
        TResult Visit(LogicalNotExpressionDefinition definition);
        TResult Visit(MemberAccessExpressionDefinition definition);
        TResult Visit(MultiplyExpressionDefinition definition);
        TResult Visit(NegateExpressionDefinition definition);
        TResult Visit(ValueNotEqualsExpressionDefinition definition);
        TResult Visit(ReferenceNotEqualsExpressionDefinition definition);
        TResult Visit(ObjectCreationExpressionDefinition definition);
        TResult Visit(OrAssignExpressionDefinition definition);
        TResult Visit(QualifiedNameDefinition definition);
        TResult Visit(RawExpressionDefinition definition);
        TResult Visit(ReturnStatementDefinition definition);
        TResult Visit(ThisExpressionDefinition definition);
        TResult Visit(ThrowStatementDefinition definition);
        TResult Visit(TypeReferenceExpressionDefinition definition);
        TResult Visit(VariableDeclaratorDefinition definition);
        TResult Visit(VariableDeclarationDefinition definition);
        TResult Visit(UsingStatementDefinition definition);
    }
}