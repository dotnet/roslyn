namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal
{
    internal static class GlyphHelpers
    {
        public static FSharpGlyph Convert(Microsoft.CodeAnalysis.Glyph glyph)
        {
            switch (glyph)
            {
                case Microsoft.CodeAnalysis.Glyph.None:
                    {
                        return FSharpGlyph.None;
                    }
                case Microsoft.CodeAnalysis.Glyph.Assembly:
                    {
                        return FSharpGlyph.Assembly;
                    }
                case Microsoft.CodeAnalysis.Glyph.BasicFile:
                    {
                        return FSharpGlyph.BasicFile;
                    }
                case Microsoft.CodeAnalysis.Glyph.BasicProject:
                    {
                        return FSharpGlyph.BasicProject;
                    }
                case Microsoft.CodeAnalysis.Glyph.ClassPublic:
                    {
                        return FSharpGlyph.ClassPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.ClassProtected:
                    {
                        return FSharpGlyph.ClassProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.ClassPrivate:
                    {
                        return FSharpGlyph.ClassPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.ClassInternal:
                    {
                        return FSharpGlyph.ClassInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.CSharpFile:
                    {
                        return FSharpGlyph.CSharpFile;
                    }
                case Microsoft.CodeAnalysis.Glyph.CSharpProject:
                    {
                        return FSharpGlyph.CSharpProject;
                    }
                case Microsoft.CodeAnalysis.Glyph.ConstantPublic:
                    {
                        return FSharpGlyph.ConstantPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.ConstantProtected:
                    {
                        return FSharpGlyph.ConstantProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.ConstantPrivate:
                    {
                        return FSharpGlyph.ConstantPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.ConstantInternal:
                    {
                        return FSharpGlyph.ConstantInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.DelegatePublic:
                    {
                        return FSharpGlyph.DelegatePublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.DelegateProtected:
                    {
                        return FSharpGlyph.DelegateProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.DelegatePrivate:
                    {
                        return FSharpGlyph.DelegatePrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.DelegateInternal:
                    {
                        return FSharpGlyph.DelegateInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.EnumPublic:
                    {
                        return FSharpGlyph.EnumPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.EnumProtected:
                    {
                        return FSharpGlyph.EnumProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.EnumPrivate:
                    {
                        return FSharpGlyph.EnumPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.EnumInternal:
                    {
                        return FSharpGlyph.EnumInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.EnumMemberPublic:
                    {
                        return FSharpGlyph.EnumMemberPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.EnumMemberProtected:
                    {
                        return FSharpGlyph.EnumMemberProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.EnumMemberPrivate:
                    {
                        return FSharpGlyph.EnumMemberPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.EnumMemberInternal:
                    {
                        return FSharpGlyph.EnumMemberInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.Error:
                    {
                        return FSharpGlyph.Error;
                    }
                case Microsoft.CodeAnalysis.Glyph.StatusInformation:
                    {
                        return FSharpGlyph.StatusInformation;
                    }
                case Microsoft.CodeAnalysis.Glyph.EventPublic:
                    {
                        return FSharpGlyph.EventPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.EventProtected:
                    {
                        return FSharpGlyph.EventProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.EventPrivate:
                    {
                        return FSharpGlyph.EventPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.EventInternal:
                    {
                        return FSharpGlyph.EventInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.ExtensionMethodPublic:
                    {
                        return FSharpGlyph.ExtensionMethodPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.ExtensionMethodProtected:
                    {
                        return FSharpGlyph.ExtensionMethodProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.ExtensionMethodPrivate:
                    {
                        return FSharpGlyph.ExtensionMethodPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.ExtensionMethodInternal:
                    {
                        return FSharpGlyph.ExtensionMethodInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.FieldPublic:
                    {
                        return FSharpGlyph.FieldPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.FieldProtected:
                    {
                        return FSharpGlyph.FieldProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.FieldPrivate:
                    {
                        return FSharpGlyph.FieldPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.FieldInternal:
                    {
                        return FSharpGlyph.FieldInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.InterfacePublic:
                    {
                        return FSharpGlyph.InterfacePublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.InterfaceProtected:
                    {
                        return FSharpGlyph.InterfaceProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.InterfacePrivate:
                    {
                        return FSharpGlyph.InterfacePrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.InterfaceInternal:
                    {
                        return FSharpGlyph.InterfaceInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.Intrinsic:
                    {
                        return FSharpGlyph.Intrinsic;
                    }
                case Microsoft.CodeAnalysis.Glyph.Keyword:
                    {
                        return FSharpGlyph.Keyword;
                    }
                case Microsoft.CodeAnalysis.Glyph.Label:
                    {
                        return FSharpGlyph.Label;
                    }
                case Microsoft.CodeAnalysis.Glyph.Local:
                    {
                        return FSharpGlyph.Local;
                    }
                case Microsoft.CodeAnalysis.Glyph.Namespace:
                    {
                        return FSharpGlyph.Namespace;
                    }
                case Microsoft.CodeAnalysis.Glyph.MethodPublic:
                    {
                        return FSharpGlyph.MethodPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.MethodProtected:
                    {
                        return FSharpGlyph.MethodProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.MethodPrivate:
                    {
                        return FSharpGlyph.MethodPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.MethodInternal:
                    {
                        return FSharpGlyph.MethodInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.ModulePublic:
                    {
                        return FSharpGlyph.ModulePublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.ModuleProtected:
                    {
                        return FSharpGlyph.ModuleProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.ModulePrivate:
                    {
                        return FSharpGlyph.ModulePrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.ModuleInternal:
                    {
                        return FSharpGlyph.ModuleInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.OpenFolder:
                    {
                        return FSharpGlyph.OpenFolder;
                    }
                case Microsoft.CodeAnalysis.Glyph.Operator:
                    {
                        return FSharpGlyph.Operator;
                    }
                case Microsoft.CodeAnalysis.Glyph.Parameter:
                    {
                        return FSharpGlyph.Parameter;
                    }
                case Microsoft.CodeAnalysis.Glyph.PropertyPublic:
                    {
                        return FSharpGlyph.PropertyPublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.PropertyProtected:
                    {
                        return FSharpGlyph.PropertyProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.PropertyPrivate:
                    {
                        return FSharpGlyph.PropertyPrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.PropertyInternal:
                    {
                        return FSharpGlyph.PropertyInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.RangeVariable:
                    {
                        return FSharpGlyph.RangeVariable;
                    }
                case Microsoft.CodeAnalysis.Glyph.Reference:
                    {
                        return FSharpGlyph.Reference;
                    }
                case Microsoft.CodeAnalysis.Glyph.StructurePublic:
                    {
                        return FSharpGlyph.StructurePublic;
                    }
                case Microsoft.CodeAnalysis.Glyph.StructureProtected:
                    {
                        return FSharpGlyph.StructureProtected;
                    }
                case Microsoft.CodeAnalysis.Glyph.StructurePrivate:
                    {
                        return FSharpGlyph.StructurePrivate;
                    }
                case Microsoft.CodeAnalysis.Glyph.StructureInternal:
                    {
                        return FSharpGlyph.StructureInternal;
                    }
                case Microsoft.CodeAnalysis.Glyph.TypeParameter:
                    {
                        return FSharpGlyph.TypeParameter;
                    }
                case Microsoft.CodeAnalysis.Glyph.Snippet:
                    {
                        return FSharpGlyph.Snippet;
                    }
                case Microsoft.CodeAnalysis.Glyph.CompletionWarning:
                    {
                        return FSharpGlyph.CompletionWarning;
                    }
                case Microsoft.CodeAnalysis.Glyph.AddReference:
                    {
                        return FSharpGlyph.AddReference;
                    }
                case Microsoft.CodeAnalysis.Glyph.NuGet:
                    {
                        return FSharpGlyph.NuGet;
                    }
                default:
                    {
                        return FSharpGlyph.None;
                    }
            }
        }
    }
}
