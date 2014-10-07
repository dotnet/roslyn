using System.Collections.Generic;

namespace Roslyn.Services
{
    /// <summary>
    /// provide information on which features and versions this migrator supports
    /// </summary>
    public interface IOptionMigratorMetadata
    {
        string ApplicableFeature { get; }

        IEnumerable<string> VersionsFrom { get; }
        string VersionTo { get; }
    }
}
