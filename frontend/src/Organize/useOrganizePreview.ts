import ModelBase from 'App/ModelBase';
import useApiQuery from 'Helpers/Hooks/useApiQuery';

export interface OrganizePreviewModel extends ModelBase {
  seriesId?: number;
  movieId?: number;
  seasonNumber?: number;
  episodeNumbers?: number[];
  episodeFileId?: number;
  movieFileId?: number;
  existingPath: string;
  newPath: string;
}

const DEFAULT_ORGANIZE_PREVIEW: OrganizePreviewModel[] = [];

interface OrganizePreviewParams {
  seriesId?: number;
  movieId?: number;
  seasonNumber?: number;
}

const useOrganizePreview = ({
  seriesId,
  movieId,
  seasonNumber,
}: OrganizePreviewParams) => {
  const queryParams = {
    seriesId,
    movieId,
    ...(seasonNumber != null ? { seasonNumber } : {}),
  };

  const { data, ...result } = useApiQuery<OrganizePreviewModel[]>({
    path: '/rename',
    queryParams,
  });

  return {
    items: data ?? DEFAULT_ORGANIZE_PREVIEW,
    ...result,
  };
};

export default useOrganizePreview;
