using System;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.CodeAnalysis.Editor.Wpf
{
    internal static class GlyphExtensions
    {
        public static ImageMoniker GetImageMoniker(this Glyph glyph)
        {
            switch (glyph)
            {
                case Glyph.None:
                    return default;

                case Glyph.Assembly:
                    return KnownMonikers.Assembly;

                case Glyph.BasicFile:
                    return KnownMonikers.VBFileNode;
                case Glyph.BasicProject:
                    return KnownMonikers.VBProjectNode;

                case Glyph.ClassPublic:
                    return KnownMonikers.ClassPublic;
                case Glyph.ClassProtected:
                    return KnownMonikers.ClassProtected;
                case Glyph.ClassPrivate:
                    return KnownMonikers.ClassPrivate;
                case Glyph.ClassInternal:
                    return KnownMonikers.ClassInternal;

                case Glyph.CSharpFile:
                    return KnownMonikers.CSFileNode;
                case Glyph.CSharpProject:
                    return KnownMonikers.CSProjectNode;

                case Glyph.ConstantPublic:
                    return KnownMonikers.ConstantPublic;
                case Glyph.ConstantProtected:
                    return KnownMonikers.ConstantProtected;
                case Glyph.ConstantPrivate:
                    return KnownMonikers.ConstantPrivate;
                case Glyph.ConstantInternal:
                    return KnownMonikers.ConstantInternal;

                case Glyph.DelegatePublic:
                    return KnownMonikers.DelegatePublic;
                case Glyph.DelegateProtected:
                    return KnownMonikers.DelegateProtected;
                case Glyph.DelegatePrivate:
                    return KnownMonikers.DelegatePrivate;
                case Glyph.DelegateInternal:
                    return KnownMonikers.DelegateInternal;

                case Glyph.EnumPublic:
                    return KnownMonikers.EnumerationPublic;
                case Glyph.EnumProtected:
                    return KnownMonikers.EnumerationProtected;
                case Glyph.EnumPrivate:
                    return KnownMonikers.EnumerationPrivate;
                case Glyph.EnumInternal:
                    return KnownMonikers.EnumerationInternal;

                case Glyph.EnumMemberPublic:
                case Glyph.EnumMemberProtected:
                case Glyph.EnumMemberPrivate:
                case Glyph.EnumMemberInternal:
                    return KnownMonikers.EnumerationItemPublic;

                case Glyph.Error:
                    return KnownMonikers.StatusError;

                case Glyph.EventPublic:
                    return KnownMonikers.EventPublic;
                case Glyph.EventProtected:
                    return KnownMonikers.EventProtected;
                case Glyph.EventPrivate:
                    return KnownMonikers.EventPrivate;
                case Glyph.EventInternal:
                    return KnownMonikers.EventInternal;

                // Extension methods have the same glyph regardless of accessibility.
                case Glyph.ExtensionMethodPublic:
                case Glyph.ExtensionMethodProtected:
                case Glyph.ExtensionMethodPrivate:
                case Glyph.ExtensionMethodInternal:
                    return KnownMonikers.ExtensionMethod;

                case Glyph.FieldPublic:
                    return KnownMonikers.FieldPublic;
                case Glyph.FieldProtected:
                    return KnownMonikers.FieldProtected;
                case Glyph.FieldPrivate:
                    return KnownMonikers.FieldPrivate;
                case Glyph.FieldInternal:
                    return KnownMonikers.FieldInternal;

                case Glyph.InterfacePublic:
                    return KnownMonikers.InterfacePublic;
                case Glyph.InterfaceProtected:
                    return KnownMonikers.InterfaceProtected;
                case Glyph.InterfacePrivate:
                    return KnownMonikers.InterfacePrivate;
                case Glyph.InterfaceInternal:
                    return KnownMonikers.InterfaceInternal;

                // TODO: Figure out the right thing to return here.
                case Glyph.Intrinsic:
                    return KnownMonikers.Type;

                case Glyph.Keyword:
                    return KnownMonikers.IntellisenseKeyword;

                case Glyph.Label:
                    return KnownMonikers.Label;

                case Glyph.Parameter:
                case Glyph.Local:
                    return KnownMonikers.LocalVariable;

                case Glyph.Namespace:
                    return KnownMonikers.Namespace;

                case Glyph.MethodPublic:
                    return KnownMonikers.MethodPublic;
                case Glyph.MethodProtected:
                    return KnownMonikers.MethodProtected;
                case Glyph.MethodPrivate:
                    return KnownMonikers.MethodPrivate;
                case Glyph.MethodInternal:
                    return KnownMonikers.MethodInternal;

                case Glyph.ModulePublic:
                    return KnownMonikers.ModulePublic;
                case Glyph.ModuleProtected:
                    return KnownMonikers.ModuleProtected;
                case Glyph.ModulePrivate:
                    return KnownMonikers.ModulePrivate;
                case Glyph.ModuleInternal:
                    return KnownMonikers.ModuleInternal;

                case Glyph.OpenFolder:
                    return KnownMonikers.OpenFolder;

                case Glyph.Operator:
                    return KnownMonikers.Operator;

                case Glyph.PropertyPublic:
                    return KnownMonikers.PropertyPublic;
                case Glyph.PropertyProtected:
                    return KnownMonikers.PropertyProtected;
                case Glyph.PropertyPrivate:
                    return KnownMonikers.PropertyPrivate;
                case Glyph.PropertyInternal:
                    return KnownMonikers.PropertyInternal;

                case Glyph.RangeVariable:
                    return KnownMonikers.FieldPublic;

                case Glyph.Reference:
                    return KnownMonikers.Reference;

                case Glyph.StructurePublic:
                    return KnownMonikers.ValueTypePublic;
                case Glyph.StructureProtected:
                    return KnownMonikers.ValueTypeProtected;
                case Glyph.StructurePrivate:
                    return KnownMonikers.ValueTypePrivate;
                case Glyph.StructureInternal:
                    return KnownMonikers.ValueTypeInternal;

                case Glyph.TypeParameter:
                    return KnownMonikers.Type;

                case Glyph.Snippet:
                    return KnownMonikers.Snippet;

                case Glyph.CompletionWarning:
                    return KnownMonikers.IntellisenseWarning;

                case Glyph.StatusInformation:
                    return KnownMonikers.StatusInformation;

                case Glyph.NuGet:
                    return KnownMonikers.NuGet;

                default:
                    throw new ArgumentException("glyph");
            }
        }
    }
}
