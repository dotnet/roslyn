// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace System.Runtime.Analyzers
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
    public abstract class DoNotLockOnObjectsWithWeakIdentity : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA2002";
        private static LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(SystemRuntimeAnalyzersResources.DoNotLockOnObjectsWithWeakIdentity), SystemRuntimeAnalyzersResources.ResourceManager, typeof(SystemRuntimeAnalyzersResources));
        private static LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(SystemRuntimeAnalyzersResources.DoNotLockOnWeakIdentity), SystemRuntimeAnalyzersResources.ResourceManager, typeof(SystemRuntimeAnalyzersResources));
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                         s_localizableTitle,
                                                                         s_localizableMessage,
                                                                         DiagnosticCategory.Reliability,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         helpLinkUri: "http://msdn.microsoft.com/library/ms182290.aspx",
                                                                         customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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
                case TypeKind.Array:
                    var arrayType = type as IArrayTypeSymbol;
                    return arrayType != null && IsPrimitiveType(arrayType.ElementType);
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

        public static bool IsPrimitiveType(ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Byte:
                case SpecialType.System_Char:
                case SpecialType.System_Double:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                case SpecialType.System_SByte:
                case SpecialType.System_Single:
                    return true;
                default:
                    return false;
            }
        }
    }
}
