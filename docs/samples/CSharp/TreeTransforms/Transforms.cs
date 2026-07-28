using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TreeTransforms
{
    /// <summary>
    /// Kinds of Syntax transforms.
    /// </summary>
    public enum TransformKind
    {
        LambdaToAnonMethod,
        AnonMethodToLambda,
        DoToWhile,
        WhileToDo,
        CheckedStmtToUncheckedStmt,
        UncheckedStmtToCheckedStmt,
        CheckedExprToUncheckedExpr,
        UncheckedExprToCheckedExpr,
        PostfixToPrefix,
        PrefixToPostfix,
        TrueToFalse,
        FalseToTrue,
        AddAssignToAssign,
        RefParamToOutParam,
        OutParamToRefParam,
        RefArgToOutArg,
        OutArgToRefArg,
        OrderByAscToOrderByDesc,
        OrderByDescToOrderByAsc,
        DefaultInitAllVars,
        ClassDeclToStructDecl,
        StructDeclToClassDecl,
        IntTypeToLongType,
    }

    public class Transforms
    {
        /// <summary>
        /// Performs a syntax transform of the source code which is passed in as a string. The transform to be performed is also passed as an argument
        /// </summary>
        /// <param name="sourceText">Text of the source code which is to be transformed</param>
        /// <param name="transformKind">The kind of Syntax Transform that needs to be performed on the source</param>
        /// <returns>Transformed source code as a string</returns>
        public static string Transform(string sourceText, TransformKind transformKind)
        {
            SyntaxTree sourceTree = SyntaxFactory.ParseSyntaxTree(sourceText);
            TransformVisitor visitor = new TransformVisitor(sourceTree, transformKind);

            return visitor.Visit(sourceTree.GetRoot()).ToFullString();
        }
    }
}
