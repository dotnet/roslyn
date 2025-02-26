// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    /// <summary>
    /// Info for a tainted data sanitizer type, which makes tainted data untainted.
    /// </summary>
    internal sealed class SanitizerInfo : ITaintedDataInfo, IEquatable<SanitizerInfo>
    {
        public SanitizerInfo(
            string fullTypeName,
            bool isInterface,
            bool isConstructorSanitizing,
            ImmutableHashSet<(MethodMatcher methodMatcher, ImmutableHashSet<(string IfTaintedParameter, string ThenUnTaintedTarget)>)> sanitizingMethods,
            ImmutableHashSet<string> sanitizingInstanceMethods)
        {
            FullTypeName = fullTypeName ?? throw new ArgumentNullException(nameof(fullTypeName));
            IsInterface = isInterface;
            IsConstructorSanitizing = isConstructorSanitizing;
            SanitizingMethods = sanitizingMethods ?? throw new ArgumentNullException(nameof(sanitizingMethods));
            SanitizingInstanceMethods = sanitizingInstanceMethods ?? throw new ArgumentNullException(nameof(sanitizingInstanceMethods));
        }

        /// <summary>
        /// Full type name of the...type (namespace + type).
        /// </summary>
        public string FullTypeName { get; }

        /// <summary>
        /// Indicates that this sanitizer type is an interface.
        /// </summary>
        public bool IsInterface { get; }

        /// <summary>
        /// Indicates that any tainted data entering a constructor becomes untainted.
        /// </summary>
        public bool IsConstructorSanitizing { get; }

        /// <summary>
        /// Methods that untaint tainted data.
        /// </summary>
        /// <remarks>
        /// MethodMatcher determines if the outermost tuple applies, based on the method names and arguments.
        /// (IfTaintedParameter, ThenUnTaintedTarget) determines if the ThenUnTaintedTarget is untainted, based on if the IfTaintedParameter is tainted.
        ///
        /// Example:
        /// (
        ///   (methodName, argumentOperations) => methodName == "Bar",  // MethodMatcher
        ///   {
        ///      ("a", "b")
        ///   }
        /// )
        ///
        /// will treat the parameter "b" as untainted when parameter "a" is tainted of the "Bar" method.
        /// </remarks>
        public ImmutableHashSet<(MethodMatcher MethodMatcher, ImmutableHashSet<(string IfTaintedParameter, string ThenUnTaintedTarget)>)> SanitizingMethods { get; }

        /// <summary>
        /// Methods that untaint tainted instance.
        /// </summary>
        public ImmutableHashSet<string> SanitizingInstanceMethods { get; }

        /// <summary>
        /// Indicates that this <see cref="SanitizerInfo"/> uses <see cref="ValueContentAbstractValue"/>s.
        /// </summary>
        public bool RequiresValueContentAnalysis => false;

        /// <summary>
        /// Indicates that <see cref="OperationKind.ParameterReference"/> is required.
        /// </summary>
        public bool RequiresParameterReferenceAnalysis => false;

        /// <summary>
        /// Qualified names of the optional dependency types.
        /// </summary>
        public ImmutableArray<string> DependencyFullTypeNames => ImmutableArray<string>.Empty;

        public override int GetHashCode()
        {
            var hashCode = new RoslynHashCode();
            HashUtilities.Combine(this.SanitizingMethods, ref hashCode);
            HashUtilities.Combine(this.SanitizingInstanceMethods, ref hashCode);
            hashCode.Add(StringComparer.Ordinal.GetHashCode(this.FullTypeName));
            hashCode.Add(this.IsConstructorSanitizing.GetHashCode());
            return hashCode.ToHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is SanitizerInfo other && this.Equals(other);
        }

        public bool Equals(SanitizerInfo other)
        {
            return other != null
                && this.FullTypeName == other.FullTypeName
                && this.IsConstructorSanitizing == other.IsConstructorSanitizing
                && this.SanitizingMethods == other.SanitizingMethods
                && this.SanitizingInstanceMethods == other.SanitizingInstanceMethods;
        }
    }
}
