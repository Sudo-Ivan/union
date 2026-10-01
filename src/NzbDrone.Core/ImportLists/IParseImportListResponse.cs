using System.Collections.Generic;

namespace NzbDrone.Core.ImportLists
{
    public interface IParseImportListResponse<T>
    {
        IList<T> ParseResponse(ImportListResponse importListResponse);
    }
}
