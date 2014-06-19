using System;
using System.Linq;

namespace Roslyn.Compilers.CSharp.Descriptions
{
    internal partial class SymbolDescriptionVisitor
    {
        protected internal override object VisitField(FieldSymbol symbol, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            AddAccessibilityIfRequired(symbol, builder);

            AddMemberModifiersIfRequired(symbol, builder);

            AddFieldModifiersIfRequired(symbol, builder);

            //TODO: custom modifiers

            if (format.MemberFlags.HasFlag(MemberFlags.IncludeType))
            {
                VisitType(symbol.Type, builder);
                AddSpace(builder);
            }

            builder.Add(new SymbolDescriptionPart
            {
                Kind = SymbolDescriptionPartKind.FieldName,
                Text = symbol.Name,
            });

            return null;
        }

        protected internal override object VisitProperty(PropertySymbol symbol, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            AddAccessibilityIfRequired(symbol, builder);

            AddMemberModifiersIfRequired(symbol, builder);

            //TODO: custom modifiers

            if (format.MemberFlags.HasFlag(MemberFlags.IncludeType))
            {
                VisitType(symbol.Type, builder);
                AddSpace(builder);
            }

            if (format.MemberFlags.HasFlag(MemberFlags.IncludeExplicitInterface))
            {
                var implementedProperties = symbol.ExplicitInterfaceImplementation;
                if (implementedProperties.Any())
                {
                    if (implementedProperties.Skip(1).Any())
                    {
                        throw new NotImplementedException(); //TODO: multiple implemented properties?
                    }

                    var implementedProperty = implementedProperties.First();

                    VisitType(implementedProperty.ContainingType, builder);
                    AddPunctuation(SyntaxKind.DotToken, builder);
                }
            }

            if (symbol.IsIndexer)
            {
                AddKeyword(SyntaxKind.ThisKeyword, builder);

                if (format.MemberFlags.HasFlag(MemberFlags.IncludeParameters))
                {
                    AddPunctuation(SyntaxKind.OpenBraceToken, builder);
                    AddParameters(hasThisParameter: false, parameters: symbol.Parameters, builder: builder);
                    AddPunctuation(SyntaxKind.CloseBraceToken, builder);
                }
            }
            else
            {
                builder.Add(new SymbolDescriptionPart
                {
                    Kind = SymbolDescriptionPartKind.PropertyName,
                    Text = symbol.Name,
                });
            }

            if (format.PropertyStyle == PropertyStyle.GetSet)
            {
                AddPunctuation(SyntaxKind.OpenBraceToken, builder);

                AddGetOrSet(symbol, symbol.GetMethod, SyntaxKind.GetKeyword, builder);
                AddGetOrSet(symbol, symbol.SetMethod, SyntaxKind.SetKeyword, builder);

                AddSpace(builder);
                AddPunctuation(SyntaxKind.CloseBraceToken, builder);
            }

            return null;
        }

        protected internal override object VisitMethod(MethodSymbol symbol, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            AddAccessibilityIfRequired(symbol, builder);

            AddMemberModifiersIfRequired(symbol, builder);

            if (format.MemberFlags.HasFlag(MemberFlags.IncludeType))
            {
                switch (symbol.MethodKind)
                {
                    case MethodKind.Constructor:
                    case MethodKind.StaticConstructor:
                    case MethodKind.Destructor:
                        break;
                    default:
                        VisitType(symbol.ReturnType, builder);
                        AddSpace(builder);
                        break;
                }
            }

            switch (symbol.MethodKind)
            {
                case MethodKind.Ordinary:
                    builder.Add(new SymbolDescriptionPart
                    {
                        Kind = SymbolDescriptionPartKind.MethodName,
                        Text = symbol.Name,
                    });
                    break;
                case MethodKind.Constructor:
                    builder.Add(new SymbolDescriptionPart
                    {
                        Kind = SymbolDescriptionPartKind.MethodName,
                        Text = symbol.ContainingType.Name,
                    });
                    break;
                case MethodKind.StaticConstructor:
                    AddKeyword(SyntaxKind.StaticKeyword, builder);
                    AddSpace(builder);
                    builder.Add(new SymbolDescriptionPart
                    {
                        Kind = SymbolDescriptionPartKind.MethodName,
                        Text = symbol.ContainingType.Name,
                    });
                    break;
                case MethodKind.Destructor:
                    AddPunctuation(SyntaxKind.TildeToken, builder);
                    builder.Add(new SymbolDescriptionPart
                    {
                        Kind = SymbolDescriptionPartKind.MethodName,
                        Text = symbol.ContainingType.Name,
                    });
                    break;
                case MethodKind.ExplicitInterfaceImplementation:
                    if (format.MemberFlags.HasFlag(MemberFlags.IncludeExplicitInterface))
                    {
                        var implementedMethods = symbol.ExplicitInterfaceImplementation;
                        if (implementedMethods.Any())
                        {
                            if (implementedMethods.Skip(1).Any())
                            {
                                throw new NotImplementedException(); //TODO: multiple implemented methods?
                            }

                            var implementedMethod = implementedMethods.First();

                            VisitType(implementedMethod.ContainingType, builder);
                            AddPunctuation(SyntaxKind.DotToken, builder);
                        }
                    }
                    builder.Add(new SymbolDescriptionPart
                    {
                        Kind = SymbolDescriptionPartKind.MethodName,
                        Text = symbol.Name,
                    });
                    break;
                default:
                    throw new NotImplementedException(); //TODO
            }

            bool hasContraints = false;
            if (symbol.Arity > 0 && format.GenericsFlags.HasFlag(GenericsFlags.IncludeTypeParameters))
            {
                hasContraints = AddTypeArguments(symbol.TypeArguments, builder);
            }

            if (format.MemberFlags.HasFlag(MemberFlags.IncludeParameters))
            {
                AddPunctuation(SyntaxKind.OpenParenToken, builder);
                AddParameters(symbol.IsExtensionMethod, symbol.Parameters, builder);
                AddPunctuation(SyntaxKind.CloseParenToken, builder);
            }

            if (hasContraints && format.GenericsFlags.HasFlag(GenericsFlags.IncludeTypeConstraints))
            {
                AddTypeParameterConstraints(symbol.TypeArguments, builder);
            }

            return null;
        }

        protected internal override object VisitParameter(ParameterSymbol symbol, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            //decorations are handled by methods and properties (for consistency with type parameters)
            builder.Add(new SymbolDescriptionPart
            {
                Kind = SymbolDescriptionPartKind.Identifier,
                Text = symbol.Name,
            });

            return null;
        }

        private void AddFieldModifiersIfRequired(FieldSymbol symbol, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            if (format.MemberFlags.HasFlag(MemberFlags.IncludeModifiers))
            {
                if (symbol.IsConst)
                {
                    AddKeyword(SyntaxKind.ConstKeyword, builder);
                    AddSpace(builder);
                }

                if (symbol.IsReadOnly)
                {
                    AddKeyword(SyntaxKind.ReadOnlyKeyword, builder);
                    AddSpace(builder);
                }

                if (symbol.IsVolatile)
                {
                    AddKeyword(SyntaxKind.VolatileKeyword, builder);
                    AddSpace(builder);
                }

                //TODO: event
            }
        }

        private void AddMemberModifiersIfRequired(Symbol symbol, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            if (format.MemberFlags.HasFlag(MemberFlags.IncludeModifiers))
            {
                if (symbol.IsStatic)
                {
                    AddKeyword(SyntaxKind.StaticKeyword, builder);
                    AddSpace(builder);
                }

                if (symbol.IsOverride)
                {
                    AddKeyword(SyntaxKind.OverrideKeyword, builder);
                    AddSpace(builder);
                }

                if (symbol.IsAbstract)
                {
                    AddKeyword(SyntaxKind.AbstractKeyword, builder);
                    AddSpace(builder);
                }

                if (symbol.IsSealed)
                {
                    AddKeyword(SyntaxKind.SealedKeyword, builder);
                    AddSpace(builder);
                }

                if (symbol.IsExtern)
                {
                    AddKeyword(SyntaxKind.ExternKeyword, builder);
                    AddSpace(builder);
                }

                if (symbol.IsVirtual)
                {
                    AddKeyword(SyntaxKind.VirtualKeyword, builder);
                    AddSpace(builder);
                }
            }
        }

        private void AddParameters(bool hasThisParameter, ReadOnlyArray<ParameterSymbol> parameters, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            bool first = true;
            foreach (var param in parameters)
            {
                if (!first)
                {
                    AddPunctuation(SyntaxKind.CommaToken, builder);
                    AddSpace(builder);
                }
                else if (hasThisParameter)
                {
                    if (!format.ParameterFlags.HasFlag(ParameterFlags.IncludeExtensionThisParameter))
                    {
                        continue;
                    }

                    AddKeyword(SyntaxKind.ThisKeyword, builder);
                    AddSpace(builder);
                }
                first = false;

                if (format.ParameterFlags.HasFlag(ParameterFlags.IncludeType))
                {
                    if (format.ParameterFlags.HasFlag(ParameterFlags.IncludeRefOut))
                    {
                        switch (param.RefKind)
                        {
                            case RefKind.Out:
                                AddKeyword(SyntaxKind.OutKeyword, builder);
                                AddSpace(builder);
                                break;
                            case RefKind.Ref:
                                AddKeyword(SyntaxKind.RefKeyword, builder);
                                AddSpace(builder);
                                break;
                        }
                    }

                    VisitType(param.Type, builder);
                }

                if (format.ParameterFlags.HasFlag(ParameterFlags.IncludeName))
                {
                    AddSpace(builder);
                    VisitParameter(param, builder);

                    if (format.ParameterFlags.HasFlag(ParameterFlags.IncludeDefaultValue) && param.HasDefaultValue)
                    {
                        AddSpace(builder);
                        AddPunctuation(SyntaxKind.EqualsToken, builder);
                        AddSpace(builder);

                        throw new NotImplementedException(); //TODO
                    }
                }

            }
        }

        private void AddGetOrSet(PropertySymbol property, MethodSymbol method, SyntaxKind keyword, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            if (method != null)
            {
                AddSpace(builder);
                if (method.DeclaredAccessibility != property.DeclaredAccessibility)
                {
                    AddAccessibilityIfRequired(method, builder);
                }
                AddKeyword(keyword, builder);
                AddPunctuation(SyntaxKind.SemicolonToken, builder);
            }
        }
    }
}