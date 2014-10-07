using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.WorkspaceServices;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal partial class DocumentExtensions
    {
        private partial class SyntaxTreeInfo : ISyntaxTreeInfo
        {
            private const string SerializationFormatAttributeName = "SerializationFormat";
            private const string SerializationFormat = "4";

            private const string SyntaxTreeInfoPersistenceName = "SyntaxTreeInfoPersistence";
            private const string SyntaxTreeInfoElementName = "SyntaxTreeInfo";

            private const string VersionAttributeName = "Version";
            private const string IdentifierFilterElementName = "IdentifierFilter";
            private const string EscapedIdentifierFilterElementName = "EscapedIdentifierFilter";
            private const string PredefinedTypesElementName = "PredefinedTypes";
            private const string PredefinedOperatorsElementName = "PredefinedOperators";

            private const string ContainsForEachStatementAttributeName = "ContainsForEachStatement";
            private const string ContainsLockStatementAttributeName = "ContainsLockStatement";
            private const string ContainsUsingStatementAttributeName = "ContainsUsingStatement";
            private const string ContainsQueryExpressionAttributeName = "ContainsQueryExpression";
            private const string ContainsThisConstructorInitializerAttributeName = "ContainsThisConstructorInitializer";
            private const string ContainsBaseConstructorInitializerAttributeName = "ContainsBaseConstructorInitializer";
            private const string ContainsElementAccessExpressionName = "ContainsElementAccessExpression";
            private const string ContainsIndexerMemberCrefName = "ContainsIndexerMemberCref";

            private static async Task<SyntaxTreeInfo> LoadOrCreateAsync(
                Document document,
                CancellationToken cancellationToken)
            {
                var persistentStorageService = WorkspaceService.GetService<IPersistentStorageService>(document.Project.Solution.Workspace);

                var version = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);

                // attempt to load from persisted state
                SyntaxTreeInfo info;
                using (var storage = persistentStorageService.GetStorage(document.Project.Solution))
                using (var stream = await storage.ReadStreamAsync(document, SyntaxTreeInfoPersistenceName, cancellationToken).ConfigureAwait(false))
                {
                    if (stream != null)
                    {
                        info = ReadFrom(stream);
                        if (info != null && info.Version == version)
                        {
                            return info;
                        }
                    }
                }

                // compute it if we couldn't load it from cache
                return await CreateAndSaveAsync(document, cancellationToken).ConfigureAwait(false);
            }

            private static async Task SaveAsync(Document document, SyntaxTreeInfo info, CancellationToken cancellationToken)
            {
                var persistentStorageService = WorkspaceService.GetService<IPersistentStorageService>(document.Project.Solution.Workspace);

                var stream = new MemoryStream();
                WriteTo(info, stream);
                stream = new MemoryStream(stream.ToArray());
                using (var storage = persistentStorageService.GetStorage(document.Project.Solution))
                {
                    await storage.WriteStreamAsync(document, SyntaxTreeInfoPersistenceName, stream, cancellationToken).ConfigureAwait(false);
                }
            }

            private static SyntaxTreeInfo ReadFrom(Stream stream)
            {
                XElement element;
                try
                {
                    element = XElement.Load(stream);
                }
                catch (XmlException)
                {
                    return null;
                }

                if (element != null && element.Name == SyntaxTreeInfoElementName)
                {
                    if ((string)element.Attribute(SerializationFormatAttributeName) == SerializationFormat)
                    {
                        var version = VersionStamp.Parse((string)element.Attribute(VersionAttributeName));
                        var identifierFilter = BloomFilter.FromXElement(element.Element(IdentifierFilterElementName).SingleElementOrDefault());
                        var escapedIdentifierFilter = BloomFilter.FromXElement(element.Element(EscapedIdentifierFilterElementName).SingleElementOrDefault());

                        var predefinedTypes = SetUtilities.FromXElement<PredefinedType>(
                            element.Element(PredefinedTypesElementName).SingleElementOrDefault(), ReadPredefinedType);
                        var predefinedOperators = SetUtilities.FromXElement<PredefinedOperator>(
                            element.Element(PredefinedOperatorsElementName).SingleElementOrDefault(), ReadPredefinedOperator);

                        var containsForEachStatement = (bool)element.Attribute(ContainsForEachStatementAttributeName);
                        var containsLockStatement = (bool)element.Attribute(ContainsLockStatementAttributeName);
                        var containsUsingStatement = (bool)element.Attribute(ContainsUsingStatementAttributeName);
                        var containsQueryExpression = (bool)element.Attribute(ContainsQueryExpressionAttributeName);
                        var containsThisConstructorInitializer = (bool)element.Attribute(ContainsThisConstructorInitializerAttributeName);
                        var containsBaseConstructorInitializer = (bool)element.Attribute(ContainsBaseConstructorInitializerAttributeName);
                        var containsElementAccessExpression = (bool)element.Attribute(ContainsElementAccessExpressionName);
                        var containsIndexerMemberCref = (bool)element.Attribute(ContainsIndexerMemberCrefName);

                        return new SyntaxTreeInfo(
                            identifierFilter,
                            escapedIdentifierFilter,
                            predefinedTypes,
                            predefinedOperators,
                            containsForEachStatement,
                            containsLockStatement,
                            containsUsingStatement,
                            containsQueryExpression,
                            containsThisConstructorInitializer,
                            containsBaseConstructorInitializer,
                            containsElementAccessExpression,
                            containsIndexerMemberCref,
                            version);
                    }
                }

                return null;
            }

            private static void WriteTo(SyntaxTreeInfo info, Stream stream)
            {
                var element =
                    new XElement(SyntaxTreeInfoElementName,
                        new XAttribute(SerializationFormatAttributeName, SerializationFormat),
                        new XAttribute(VersionAttributeName, info.Version.ToString()),
                        new XElement(IdentifierFilterElementName, info.identifierFilter.ToXElement()),
                        new XElement(EscapedIdentifierFilterElementName, info.escapedIdentifierFilter.ToXElement()),
                        new XElement(PredefinedTypesElementName, info.predefinedTypesOpt.ToXElement(WritePredefinedType)),
                        new XElement(PredefinedOperatorsElementName, info.predefinedOperatorsOpt.ToXElement(WritePredefinedOperator)),
                        new XAttribute(ContainsBaseConstructorInitializerAttributeName, info.ContainsBaseConstructorInitializer),
                        new XAttribute(ContainsForEachStatementAttributeName, info.ContainsForEachStatement),
                        new XAttribute(ContainsLockStatementAttributeName, info.ContainsLockStatement),
                        new XAttribute(ContainsQueryExpressionAttributeName, info.ContainsQueryExpression),
                        new XAttribute(ContainsThisConstructorInitializerAttributeName, info.ContainsThisConstructorInitializer),
                        new XAttribute(ContainsUsingStatementAttributeName, info.ContainsUsingStatement),
                        new XAttribute(ContainsElementAccessExpressionName, info.ContainsElementAccessExpression),
                        new XAttribute(ContainsIndexerMemberCrefName, info.ContainsIndexerMemberCref));

                element.WriteTo(stream);
            }

            private static object WritePredefinedType(PredefinedType predefinedType)
            {
                return WriteEnumValue(predefinedType);
            }

            private static object WritePredefinedOperator(PredefinedOperator predefinedOperator)
            {
                return WriteEnumValue(predefinedOperator);
            }

            private static string WriteEnumValue<T>(T value)
            {
                return value.ToString();
            }

            private static PredefinedType ReadPredefinedType(XElement element)
            {
                return ReadEnumValue<PredefinedType>(element);
            }

            private static PredefinedOperator ReadPredefinedOperator(XElement element)
            {
                return ReadEnumValue<PredefinedOperator>(element);
            }

            private static T ReadEnumValue<T>(XElement element)
            {
                return (T)Enum.Parse(typeof(T), element.Value);
            }
        }
    }
}