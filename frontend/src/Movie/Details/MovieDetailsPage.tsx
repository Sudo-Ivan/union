import React, { useEffect } from 'react';
import { useSelector } from 'react-redux';
import { useNavigate, useParams } from 'react-router';
import NotFound from 'Components/NotFound';
import usePrevious from 'Helpers/Hooks/usePrevious';
import createAllMoviesSelector from 'Store/Selectors/createAllMoviesSelector';
import translate from 'Utilities/String/translate';
import MovieDetails from './MovieDetails';

function MovieDetailsPage() {
  const allMovies = useSelector(createAllMoviesSelector());
  const { titleSlug } = useParams<{ titleSlug: string }>();
  const navigate = useNavigate();

  const movieIndex = allMovies.findIndex(
    (movie) => movie.titleSlug === titleSlug
  );

  const previousIndex = usePrevious(movieIndex);

  useEffect(() => {
    if (
      movieIndex === -1 &&
      previousIndex !== -1 &&
      previousIndex !== undefined
    ) {
      navigate('/movies');
    }
  }, [movieIndex, previousIndex, navigate]);

  if (movieIndex === -1) {
    return <NotFound message={translate('MovieCannotBeFound')} />;
  }

  return <MovieDetails movieId={allMovies[movieIndex].id} />;
}

export default MovieDetailsPage;
