import { onLocationChanged } from 'connected-react-router';
import React, { useEffect, useRef } from 'react';
import { Provider } from 'react-redux';
import {
  Outlet,
  useLocation,
  useNavigate,
  useNavigationType,
} from 'react-router-dom';
import { Store } from 'redux';
import SignalRConnector from 'Components/SignalRConnector';
import createAppStore from 'Store/createAppStore';
import movieHistory, {
  setMovieAction,
  setMovieNavigate,
} from 'Store/movieHistory';

function MovieSection() {
  const navigate = useNavigate();
  const location = useLocation();
  const navigationType = useNavigationType();
  const storeRef = useRef<Store | null>(null);

  if (storeRef.current == null) {
    setMovieNavigate(navigate);
    setMovieAction(navigationType);
    storeRef.current = createAppStore(movieHistory);
  }

  const store = storeRef.current as Store;

  useEffect(() => {
    setMovieNavigate(navigate);
    setMovieAction(navigationType);
  }, [navigate, navigationType]);

  useEffect(() => {
    store.dispatch(onLocationChanged(location, navigationType));
  }, [store, location, navigationType]);

  return (
    <Provider store={store}>
      <SignalRConnector />
      <Outlet />
    </Provider>
  );
}

export default MovieSection;
