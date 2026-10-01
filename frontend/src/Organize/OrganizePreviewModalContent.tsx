import React, { useCallback } from 'react';
import { SelectProvider, useSelect } from 'App/Select/SelectContext';
import CommandNames from 'Commands/CommandNames';
import { useExecuteCommand } from 'Commands/useCommands';
import Alert from 'Components/Alert';
import CheckInput from 'Components/Form/CheckInput';
import Button from 'Components/Link/Button';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import InlineMarkdown from 'Components/Markdown/InlineMarkdown';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds } from 'Helpers/Props';
import formatSeason from 'Season/formatSeason';
import useMovie from 'Movie/useMovie';
import { useSingleSeries } from 'Series/useSeries';
import { useNamingSettings } from 'Settings/MediaManagement/Naming/useNamingSettings';
import { CheckInputChanged } from 'typings/inputs';
import translate from 'Utilities/String/translate';
import OrganizePreviewRow from './OrganizePreviewRow';
import useOrganizePreview, { OrganizePreviewModel } from './useOrganizePreview';
import styles from './OrganizePreviewModalContent.module.css';

function getValue(allSelected: boolean, allUnselected: boolean) {
  if (allSelected) {
    return true;
  } else if (allUnselected) {
    return false;
  }

  return null;
}

export interface OrganizePreviewModalContentProps {
  seriesId?: number;
  movieId?: number;
  seasonNumber?: number;
  onModalClose: () => void;
}

function OrganizePreviewModalContentInner({
  seriesId,
  movieId,
  seasonNumber,
  onModalClose,
}: OrganizePreviewModalContentProps) {
  const executeCommand = useExecuteCommand();
  const {
    items,
    isFetching: isPreviewFetching,
    isFetched: isPreviewFetched,
    error: previewError,
  } = useOrganizePreview({ seriesId, movieId, seasonNumber });

  const {
    isFetching: isNamingFetching,
    isFetched: isNamingFetched,
    error: namingError,
    data: naming,
  } = useNamingSettings();

  const series = useSingleSeries(seriesId);
  const movie = useMovie(movieId);

  const { allSelected, allUnselected, getSelectedIds, selectAll, unselectAll } =
    useSelect<OrganizePreviewModel>();

  const isFetching = isPreviewFetching || isNamingFetching;
  const isPopulated = isPreviewFetched && isNamingFetched;
  const error = previewError || namingError;
  const renameFiles = movieId != null ? naming.renameMovies : naming.renameEpisodes;
  const fileFormat =
    movieId != null
      ? naming.movieFormat
      : series
      ? naming[`${series.seriesType}EpisodeFormat`]
      : undefined;

  const selectAllValue = getValue(allSelected, allUnselected);

  const handleSelectAllChange = useCallback(
    ({ value }: CheckInputChanged) => {
      if (value) {
        selectAll();
      } else {
        unselectAll();
      }
    },
    [selectAll, unselectAll]
  );

  const handleOrganizePress = useCallback(() => {
    const files = getSelectedIds();

    executeCommand({
      name: CommandNames.RenameFiles,
      files,
      seriesId,
      movieId,
    });

    onModalClose();
  }, [seriesId, movieId, getSelectedIds, executeCommand, onModalClose]);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {seasonNumber == null
          ? translate('OrganizeModalHeader')
          : translate('OrganizeModalHeaderSeason', {
              season: formatSeason(seasonNumber) ?? '',
            })}
      </ModalHeader>

      <ModalBody>
        {isFetching ? <LoadingIndicator /> : null}

        {!isFetching && error ? (
          <Alert kind={kinds.DANGER}>{translate('OrganizeLoadError')}</Alert>
        ) : null}

        {!isFetching && isPopulated && !items.length ? (
          <div>
            {renameFiles ? (
              <div>{translate('OrganizeNothingToRename')}</div>
            ) : (
              <div>{translate('OrganizeRenamingDisabled')}</div>
            )}
          </div>
        ) : null}

        {!isFetching && isPopulated && items.length ? (
          <div>
            <Alert>
              <div>
                <InlineMarkdown
                  data={translate('OrganizeRelativePaths', {
                    path: series?.path ?? movie?.path ?? '',
                  })}
                  blockClassName={styles.path}
                />
              </div>

              <div>
                <InlineMarkdown
                  data={translate('OrganizeNamingPattern', {
                    episodeFormat: fileFormat ?? '',
                  })}
                  blockClassName={styles.episodeFormat}
                />
              </div>
            </Alert>

            <div className={styles.previews}>
              {items.map((item) => {
                const fileId = item.episodeFileId ?? item.movieFileId ?? item.id;

                return (
                  <OrganizePreviewRow
                    key={fileId}
                    id={fileId}
                    existingPath={item.existingPath}
                    newPath={item.newPath}
                  />
                );
              })}
            </div>
          </div>
        ) : null}
      </ModalBody>

      <ModalFooter>
        {isPopulated && items.length ? (
          <CheckInput
            className={styles.selectAllInput}
            containerClassName={styles.selectAllInputContainer}
            name="selectAll"
            ariaLabel={translate('SelectAll')}
            value={selectAllValue}
            onChange={handleSelectAllChange}
          />
        ) : null}

        <Button onPress={onModalClose}>{translate('Cancel')}</Button>

        <Button kind={kinds.PRIMARY} onPress={handleOrganizePress}>
          {translate('Organize')}
        </Button>
      </ModalFooter>
    </ModalContent>
  );
}

function OrganizePreviewModalContent({
  seriesId,
  movieId,
  seasonNumber,
  onModalClose,
}: OrganizePreviewModalContentProps) {
  const { items } = useOrganizePreview({ seriesId, movieId, seasonNumber });

  return (
    <SelectProvider<OrganizePreviewModel> items={items}>
      <OrganizePreviewModalContentInner
        seriesId={seriesId}
        movieId={movieId}
        seasonNumber={seasonNumber}
        onModalClose={onModalClose}
      />
    </SelectProvider>
  );
}

export default OrganizePreviewModalContent;
