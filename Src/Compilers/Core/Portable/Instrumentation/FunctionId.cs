// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis.Instrumentation
{
    /// <summary>
    /// Enum that uniquely identifies every event (pair) that we will be logging.
    /// </summary>
    internal enum FunctionId : int
    {
        // C# Events
        CSharp_SyntaxTree_FullParse = 1,
        CSharp_SyntaxTree_IncrementalParse,
        CSharp_SyntaxTree_GetText,

        CSharp_SyntaxNode_SerializeTo,
        CSharp_SyntaxNode_DeserializeFrom,

        CSharp_Compilation_Create,
        CSharp_Compilation_AddSyntaxTrees,
        CSharp_Compilation_RemoveSyntaxTrees,
        CSharp_Compilation_ReplaceSyntaxTree,
        CSharp_Compilation_FindEntryPoint,
        CSharp_Compilation_ClassifyConversion,
        CSharp_Compilation_GetDiagnostics,
        CSharp_Compilation_Emit,
        CSharp_Compilation_CreateSourceAssembly,
        CSharp_Compilation_GetGlobalNamespace,

        CSharp_Compiler_CompileMethodBodies,
        CSharp_Compiler_CompileSynthesizedMethodMetadata,
        CSharp_DocumentationCommentCompiler_WriteDocumentationCommentXml,
        CSharp_CommandLineParser_Parse,

        CSharp_SemanticModel_GetTypeInfo,
        CSharp_SemanticModel_GetConversion,
        CSharp_SemanticModel_GetSpeculativeTypeInfo,
        CSharp_SemanticModel_GetSymbolInfo,
        CSharp_SemanticModel_GetSpeculativeSymbolInfo,
        CSharp_SemanticModel_LookupSymbols,
        CSharp_SemanticModel_AnalyzeControlFlow,
        CSharp_SemanticModel_AnalyzeDataFlow,
        CSharp_SemanticModel_ClassifyConversion,
        CSharp_SemanticModel_ClassifyConversionForCast,
        CSharp_SemanticModel_GetDeclaredSymbol,
        CSharp_SemanticModel_GetDeclaredConstructorSymbol,
        CSharp_SemanticModel_ResolveOverloads,
        CSharp_SemanticModel_ResolveIndexerOverloads,
        CSharp_SemanticModel_GetDiagnostics,
        CSharp_SemanticModel_GetMemberGroup,
        // CSharp_SemanticModel_GetSpeculativeMemberGroup, - This API does not exist in C#.
        CSharp_SemanticModel_GetIndexerGroup,
        CSharp_SemanticModel_GetConstantValue,
        // CSharp_SemanticModel_GetSpeculativeConstantValue, - This API does not exist in C#.
        CSharp_SemanticModel_GetQueryClauseInfo,
        CSharp_SemanticModel_GetAwaitExpressionInfo,
        CSharp_SemanticModel_GetForEachStatementInfo,
        CSharp_SemanticModel_GetAliasInfo,
        CSharp_SemanticModel_GetSpeculativeAliasInfo,
        CSharp_SemanticModel_GetEnclosingSymbol,
        CSharp_SemanticModel_IsAccessible,
        CSharp_SemanticModel_GetPreprocessorSymbolInfo,


        // VB Events
        VisualBasic_SyntaxTree_FullParse,
        VisualBasic_SyntaxTree_IncrementalParse,
        VisualBasic_SyntaxTree_GetText,

        VisualBasic_SyntaxNode_SerializeTo,
        VisualBasic_SyntaxNode_DeserializeFrom,

        VisualBasic_Compilation_Create,
        VisualBasic_Compilation_AddSyntaxTrees,
        VisualBasic_Compilation_RemoveSyntaxTrees,
        VisualBasic_Compilation_ReplaceSyntaxTree,
        VisualBasic_Compilation_FindEntryPoint,
        VisualBasic_Compilation_ClassifyConversion,
        VisualBasic_Compilation_GetDiagnostics,
        VisualBasic_Compilation_Emit,
        VisualBasic_Compilation_CreateSourceAssembly,
        VisualBasic_Compilation_GetGlobalNamespace,

        VisualBasic_Compiler_CompileMethodBodies,
        // VisualBasic_Compiler_CompileSynthesizedMethodMetadata, - This API does not exit in VB.
        VisualBasic_DocumentationCommentCompiler_WriteDocumentationCommentXml,
        VisualBasic_CommandLineParser_Parse,

        VisualBasic_SemanticModel_GetTypeInfo,
        VisualBasic_SemanticModel_GetSpeculativeTypeInfo,
        VisualBasic_SemanticModel_GetSymbolInfo,
        VisualBasic_SemanticModel_GetSpeculativeSymbolInfo,
        VisualBasic_SemanticModel_LookupSymbols,
        VisualBasic_SemanticModel_AnalyzeControlFlow,
        VisualBasic_SemanticModel_AnalyzeDataFlow,
        VisualBasic_SemanticModel_ClassifyConversion,
        // VisualBasic_SemanticModel_ClassifyConversionForCast, - This API does not exit in VB.
        VisualBasic_SemanticModel_GetDeclaredSymbol,
        VisualBasic_SemanticModel_ResolveOverloads,
        // VisualBasic_SemanticModel_ResolveIndexerOverloads, - This API does not exit in VB.
        VisualBasic_SemanticModel_GetDiagnostics,
        VisualBasic_SemanticModel_GetMemberGroup,
        VisualBasic_SemanticModel_GetSpeculativeMemberGroup,
        // VisualBasic_SemanticModel_GetIndexerGroup, - This API does not exit in VB.
        VisualBasic_SemanticModel_GetConstantValue,
        VisualBasic_SemanticModel_GetSpeculativeConstantValue,
        // VisualBasic_SemanticModel_GetQueryClauseInfo, - This API does not exit in VB.
        VisualBasic_SemanticModel_GetForEachStatementInfo,
        VisualBasic_SemanticModel_GetAliasInfo,
        VisualBasic_SemanticModel_GetSpeculativeAliasInfo,
        VisualBasic_SemanticModel_GetEnclosingSymbol,
        VisualBasic_SemanticModel_IsAccessible,
        VisualBasic_SemanticModel_GetPreprocessorSymbolInfo,

        // Common Events
        Common_Compilation_SerializeToPeStream,
        Common_CommandLineCompiler_ResolveMetadataReferences,

        Count
    }
}