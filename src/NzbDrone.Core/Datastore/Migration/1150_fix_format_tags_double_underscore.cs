using System.Data;
using System.Text.RegularExpressions;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1150)]
    public class fix_format_tags_double_underscore : NzbDroneMigrationBase
    {
        public static Regex DoubleUnderscore = new Regex(@"^(?<type>R|S|M|E|L|C|I|G)__(?<value>.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        protected override void MainDbUpgrade()
        {
            // Union: CustomFormats may be sonarr-shaped without FormatTags, the
            // existence check happens inside ConvertExistingFormatTags at execution
            // time so seed-time column adds still get converted.
            Execute.WithConnection(ConvertExistingFormatTags);
        }

        private void ConvertExistingFormatTags(IDbConnection conn, IDbTransaction tran)
        {
            try
            {
                var updater = new CustomFormatUpdater149(conn, tran);

                updater.ReplaceInTags(DoubleUnderscore, match =>
                {
                    return $"{match.Groups["type"].Value}_{match.Groups["value"].Value}";
                });

                updater.Commit();
            }
            catch (System.Exception e)
            {
                _logger.Debug(e, "union-skip: data migration step, schema shape differs");
            }
        }
    }
}
