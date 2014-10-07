namespace Roslyn.Services.Shared.CodeGeneration
{
    internal interface ICodeDefinitionVisitor<TArgument, TResult>
    {
        TResult Visit(AddExpressionDefinition definition, TArgument arg);
        TResult Visit(ArgumentDefinition definition, TArgument arg);
        TResult Visit(AsExpressionDefinition definition, TArgument arg);
        TResult Visit(AssignExpressionDefinition definition, TArgument arg);
        TResult Visit(BaseExpressionDefinition definition, TArgument arg);
        TResult Visit(BinaryAndExpressionDefinition definition, TArgument arg);
        TResult Visit(BinaryOrExpressionDefinition definition, TArgument arg);
        TResult Visit(CastExpressionDefinition definition, TArgument arg);
        TResult Visit(ConditionalExpressionDefinition definition, TArgument arg);
        TResult Visit(ConstantExpressionDefinition definition, TArgument arg);
        TResult Visit(DefaultExpressionDefinition definition, TArgument arg);
        TResult Visit(ElementAccessExpressionDefinition definition, TArgument arg);
        TResult Visit(ValueEqualsExpressionDefinition definition, TArgument arg);
        TResult Visit(ReferenceEqualsExpressionDefinition definition, TArgument arg);
        TResult Visit(ExpressionStatementDefinition definition, TArgument arg);
        TResult Visit(GenericNameDefinition definition, TArgument arg);
        TResult Visit(IdentifierNameDefinition definition, TArgument arg);
        TResult Visit(IfStatementDefinition definition, TArgument arg);
        TResult Visit(InvocationExpressionDefinition definition, TArgument arg);
        TResult Visit(IsExpressionDefinition definition, TArgument arg);
        TResult Visit(LocalDeclarationStatementDefinition definition, TArgument arg);
        TResult Visit(LogicalAndExpressionDefinition definition, TArgument arg);
        TResult Visit(LogicalOrExpressionDefinition definition, TArgument arg);
        TResult Visit(LogicalNotExpressionDefinition definition, TArgument arg);
        TResult Visit(MemberAccessExpressionDefinition definition, TArgument arg);
        TResult Visit(MultiplyExpressionDefinition definition, TArgument arg);
        TResult Visit(NegateExpressionDefinition definition, TArgument arg);
        TResult Visit(ValueNotEqualsExpressionDefinition definition, TArgument arg);
        TResult Visit(ReferenceNotEqualsExpressionDefinition definition, TArgument arg);
        TResult Visit(ObjectCreationExpressionDefinition definition, TArgument arg);
        TResult Visit(OrAssignExpressionDefinition definition, TArgument arg);
        TResult Visit(QualifiedNameDefinition definition, TArgument arg);
        TResult Visit(RawExpressionDefinition definition, TArgument arg);
        TResult Visit(ReturnStatementDefinition definition, TArgument arg);
        TResult Visit(ThisExpressionDefinition definition, TArgument arg);
        TResult Visit(ThrowStatementDefinition definition, TArgument arg);
        TResult Visit(TypeReferenceExpressionDefinition definition, TArgument arg);
        TResult Visit(VariableDeclaratorDefinition definition, TArgument arg);
        TResult Visit(VariableDeclarationDefinition definition, TArgument arg);
        TResult Visit(UsingStatementDefinition definition, TArgument arg);
    }
}