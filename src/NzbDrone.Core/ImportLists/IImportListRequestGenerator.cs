using System;

namespace NzbDrone.Core.ImportLists
{
    public interface IImportListRequestGenerator
    {
        ImportListPageableRequestChain GetListItems()
        {
            throw new NotSupportedException();
        }

        ImportListPageableRequestChain GetMovies()
        {
            throw new NotSupportedException();
        }
    }
}
