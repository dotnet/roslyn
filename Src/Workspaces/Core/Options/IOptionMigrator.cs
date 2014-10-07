using System;

namespace Roslyn.Services
{
    /// <summary>
    /// an option migrator that knows how to migrate a feature option from one version to the other version.
    /// must be decorated with ExportOptionsMigratorAttribute
    /// </summary>
    public interface IOptionMigrator
    {
        string Migrate(Version versionFrom, string data);
    }
}
