// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Virtual dispatch based on a symbol's particular class. 
    /// </summary>
    /// <typeparam name="TArgument">Additional argument type</typeparam>
    /// <typeparam name="TResult">Result type</typeparam>
    internal abstract class CSharpSymbolVisitor<TArgument, TResult>
    {
        /// <summary>
        /// Call the correct VisitXXX method in this class based on the particular type of symbol that is passed in.
        /// Return default(TResult) if symbol is null
        /// </summary>
        public virtual TResult Visit(Symbol symbol, TArgument argument = default(TArgument))
        {
            if ((object)symbol == null)
            {
                return default(TResult);
            }

            return symbol.Accept(this, argument);
        }

        /// <summary>
        /// The default Visit method called when visiting any <see cref="Symbol" /> and 
        /// if visiting specific symbol method VisitXXX is not overridden
        /// </summary>
        /// <param name="symbol">The visited symbol</param>
        /// <param name="argument">Additional argument</param>
        /// <returns></returns>
        public virtual TResult DefaultVisit(Symbol symbol, TArgument argument)
        {
            return default(TResult);
        }

        /// <summary>
        /// Called when visiting an <see cref="AssemblySymbol" />; Override this method with
        /// specific implementation; Calling default <see cref="DefaultVisit" /> if it's not
        /// overridden 
        /// </summary>
        /// <param name="symbol">The visited symbol</param>
        /// <param name="argument">Additional argument</param>
        /// <returns></returns>
        public virtual TResult VisitAssembly(AssemblySymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        /// <summary>
        /// Called when visiting a <see cref="ModuleSymbol" />; Override this method with specific
        /// implementation; Calling default <see cref="DefaultVisit" /> if it's not overridden 
        /// </summary>
        /// <param name="symbol">The visited symbol</param>
        /// <param name="argument">Additional argument</param>
        /// <returns></returns>
        public virtual TResult VisitModule(ModuleSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        /// <summary>
        /// Called when visiting a <see cref="NamespaceSymbol" />; Should override this method if
        /// want to visit members of the namespace; Calling <see
        /// cref="NamespaceOrTypeSymbol.GetMembers()" />
        /// and loop over each member; calling <see cref="Visit" /> on it Or override this with
        /// specific implementation; Calling <see cref="DefaultVisit" /> if it's not
        /// overridden 
        /// </summary>
        /// <param name="symbol">The visited symbol</param>
        /// <param name="argument">Additional argument</param>
        /// <returns></returns>
        public virtual TResult VisitNamespace(NamespaceSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        /// <summary>
        /// Called when visiting a <see cref="NamedTypeSymbol" />; Override this with specific
        /// implementation; Calling <see cref="DefaultVisit" /> if it's not overridden 
        /// </summary>
        /// <param name="symbol">The visited symbol</param>
        /// <param name="argument">Additional argument</param>
        /// <returns></returns>
        public virtual TResult VisitNamedType(NamedTypeSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        /// <summary>
        /// Called when visiting an <see cref="ArrayTypeSymbol" />; Override this with specific
        /// implementation; Calling <see cref="DefaultVisit" /> if it's not overridden 
        /// </summary>
        /// <param name="symbol">The visited symbol</param>
        /// <param name="argument">Additional argument</param>
        /// <returns></returns>
        public virtual TResult VisitArrayType(ArrayTypeSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        /// <summary>
        /// Called when visiting a <see cref="PointerTypeSymbol" />; Override this with specific
        /// implementation; Calling <see cref="DefaultVisit" /> if it's not overridden 
        /// </summary>
        /// <param name="symbol">The visited symbol</param>
        /// <param name="argument">Additional argument</param>
        /// <returns></returns>
        public virtual TResult VisitPointerType(PointerTypeSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        /// <summary>
        /// Called when visiting a <see cref="FunctionPointerTypeSymbol"/>; Override this with specific
        /// implementation; Calling <see cref="DefaultVisit"/>  if it's not overridden
        /// </summary>
        /// <param name="symbol">The visited symbol</param>
        /// <param name="argument">Additional argument</param>
        /// <returns></returns>
        public virtual TResult VisitFunctionPointerType(FunctionPointerTypeSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        /// <summary>
        /// Called when visiting an <see cref="ErrorTypeSymbol" /> 
        /// Error symbol is created when there is compiler error; Override this with specific
        /// implementation; Calling <see cref="DefaultVisit" /> if it's not overridden 
        /// </summary>
        /// <param name="symbol">The visited symbol</param>
        /// <param name="argument">Additional argument</param>
        /// <returns></returns>
        public virtual TResult VisitErrorType(ErrorTypeSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        /// <summary>
        /// Called when visiting a <see cref="TypeParameterSymbol" />; Override this with specific
        /// implementation; Calling <see cref="DefaultVisit" /> if it's not overridden 
        /// </summary>
        /// <param name="symbol">The visited symbol</param>
        /// <param name="argument">Additional argument</param>
        /// <returns></returns>
        public virtual TResult VisitTypeParameter(TypeParameterSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        /// <summary>
        /// Called when visiting a <see cref="DynamicTypeSymbol" />; Override this with specific
        /// implementation; Calling <see cref="DefaultVisit" /> if it's not overridden 
        /// </summary>
        /// <param name="symbol">The visited symbol</param>
        /// <param name="argument">Additional argument</param>
        /// <returns></returns>
        public virtual TResult VisitDynamicType(DynamicTypeSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        /// <summary>
        /// Called when visiting a <see cref="DiscardSymbol" />; Override this with specific
        /// implementation; Calling <see cref="DefaultVisit" /> if it's not overridden 
        /// </summary>
        /// <param name="symbol">The visited symbol</param>
        /// <param name="argument">Additional argument</param>
        /// <returns></returns>
        public virtual TResult VisitDiscard(DiscardSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        /// <summary>
        /// Called when visiting a <see cref="MethodSymbol" />; Override this with specific
        /// implementation; Calling default <see cref="DefaultVisit" /> if it's not overridden 
        /// </summary>
        /// <param name="symbol">The visited symbol</param>
        /// <param name="argument">Additional argument</param>
        /// <returns></returns>
        public virtual TResult VisitMethod(MethodSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        /// <summary>
        /// Called when visiting a <see cref="FieldSymbol" />; Override this with specific
        /// implementation; Calling default <see cref="DefaultVisit" /> if it's not overridden 
        /// </summary>
        /// <param name="symbol">The visited symbol</param>
        /// <param name="argument">Additional argument</param>
        /// <returns></returns>
        public virtual TResult VisitField(FieldSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        /// <summary>
        /// Called when visiting a <see cref="PropertySymbol" />; Override this with specific
        /// implementation; Calling default <see cref="DefaultVisit" /> if it's not overridden 
        /// </summary>
        /// <param name="symbol">The visited symbol</param>
        /// <param name="argument">Additional argument</param>
        /// <returns></returns>
        public virtual TResult VisitProperty(PropertySymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        /// <summary>
        /// Called when visiting an <see cref="EventSymbol" />; Override this with specific
        /// implementation; Calling default <see cref="DefaultVisit" /> if it's not overridden 
        /// </summary>
        /// <param name="symbol">The visited symbol</param>
        /// <param name="argument">Additional argument</param>
        /// <returns></returns>
        public virtual TResult VisitEvent(EventSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        /// <summary>
        /// Called when visiting a <see cref="ParameterSymbol" />; Override this with specific
        /// implementation; Calling default <see cref="DefaultVisit" /> if it's not overridden 
        /// </summary>
        /// <param name="symbol">The visited symbol</param>
        /// <param name="argument">Additional argument</param>
        /// <returns></returns>
        public virtual TResult VisitParameter(ParameterSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        /// <summary>
        /// Called when visiting a <see cref="LocalSymbol" />; Override this with specific
        /// implementation; Calling default <see cref="DefaultVisit" /> if it's not overridden 
        /// </summary>
        /// <param name="symbol">The visited symbol</param>
        /// <param name="argument">Additional argument</param>
        /// <returns></returns>
        public virtual TResult VisitLocal(LocalSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        /// <summary>
        /// Called when visiting a <see cref="LabelSymbol" />; Override this with specific
        /// implementation; Calling default <see cref="DefaultVisit" /> if it's not overridden 
        /// </summary>
        /// <param name="symbol">The visited symbol</param>
        /// <param name="argument">Additional argument</param>
        /// <returns></returns>
        public virtual TResult VisitLabel(LabelSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        /// <summary>
        /// Called when visiting an <see cref="AliasSymbol" />; Override this with specific
        /// implementation; Calling default <see cref="DefaultVisit" /> if it's not overridden 
        /// </summary>
        /// <param name="symbol">The visited symbol</param>
        /// <param name="argument">Additional argument</param>
        /// <returns></returns>
        public virtual TResult VisitAlias(AliasSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        /// <summary>
        /// Called when visiting a <see cref="RangeVariableSymbol" />; Override this with specific
        /// implementation; Calling default <see cref="DefaultVisit" /> if it's not overridden 
        /// </summary>
        /// <param name="symbol">The visited symbol</param>
        /// <param name="argument">Additional argument</param>
        /// <returns></returns>
        public virtual TResult VisitRangeVariable(RangeVariableSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }
    }
}
