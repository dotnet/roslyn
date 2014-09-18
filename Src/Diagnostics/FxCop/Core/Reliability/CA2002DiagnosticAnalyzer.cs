// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Shared.Extensions;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Reliability
{
    /// <summary>
    /// CA2002: Do not lock on objects with weak identities
    /// 
    /// Cause:
    /// A thread that attempts to acquire a lock on an object that has a weak identity could cause hangs.
    /// 
    /// Description:
    /// An object is said to have a weak identity when it can be directly accessed across application domain boundaries. 
    /// A thread that tries to acquire a lock on an object that has a weak identity can be blocked by a second thread in 
    /// a different application domain that has a lock on the same object. 
    /// </summary>
    public abstract class CA2002DiagnosticAnalyzer : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA2002";
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                         FxCopRulesResources.DoNotLockOnObjectsWithWeakIdentity,
                                                                         FxCopRulesResources.DoNotLockOnWeakIdentity,
                                                                         FxCopDiagnosticCategory.Reliability,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         helpLink: "http://msdn.microsoft.com/library/ms182290.aspx",
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        protected void GetDiagnosticsForNode(SyntaxNode node, SemanticModel model, Action<Diagnostic> addDiagnostic)
        {
            var type = model.GetTypeInfo(node).Type;
            if (type != null && TypeHasWeakIdentity(type, model))
            {
                addDiagnostic(node.CreateDiagnostic(Rule, type.ToDisplayString()));
            }
        }

        private bool TypeHasWeakIdentity(ITypeSymbol type, SemanticModel model)
        {
            switch (type.TypeKind)
            {
                case TypeKind.ArrayType:
                    var arrayType = type as IArrayTypeSymbol;
                    return arrayType != null && arrayType.ElementType.IsPrimitiveType();
                case TypeKind.Class:
                case TypeKind.TypeParameter:
                    Compilation compilation = model.Compilation;
                    INamedTypeSymbol marshalByRefObjectTypeSymbol = compilation.GetTypeByMetadataName("System.MarshalByRefObject");
                    INamedTypeSymbol executionEngineExceptionTypeSymbol = compilation.GetTypeByMetadataName("System.ExecutionEngineException");
                    INamedTypeSymbol outOfMemoryExceptionTypeSymbol = compilation.GetTypeByMetadataName("System.OutOfMemoryException");
                    INamedTypeSymbol stackOverflowExceptionTypeSymbol = compilation.GetTypeByMetadataName("System.StackOverflowException");
                    INamedTypeSymbol memberInfoTypeSymbol = compilation.GetTypeByMetadataName("System.Reflection.MemberInfo");
                    INamedTypeSymbol parameterInfoTypeSymbol = compilation.GetTypeByMetadataName("System.Reflection.ParameterInfo");
                    INamedTypeSymbol threadTypeSymbol = compilation.GetTypeByMetadataName("System.Threading.Thread");
                    return
                        type.SpecialType == SpecialType.System_String ||
                        type.Equals(executionEngineExceptionTypeSymbol) ||
                        type.Equals(outOfMemoryExceptionTypeSymbol) ||
                        type.Equals(stackOverflowExceptionTypeSymbol) ||
                        type.Inherits(marshalByRefObjectTypeSymbol) ||
                        type.Inherits(memberInfoTypeSymbol) ||
                        type.Inherits(parameterInfoTypeSymbol) ||
                        type.Inherits(threadTypeSymbol);
                
                // What about struct types?
                default:
                    return false;
            }
        }
    }
}
