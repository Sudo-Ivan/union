// Minimal history v4 compatible shim for the movie section's redux store.
// connected-react-router's routerMiddleware calls push/replace/go/goBack/
// goForward on this object. The calls are delegated to react-router's
// navigate function once the movie section mounts.

let navigateFn = null;
let currentAction = 'POP';

export function setMovieNavigate(navigate) {
  navigateFn = navigate;
}

export function setMovieAction(action) {
  currentAction = action;
}

function toPath(to) {
  const path =
    typeof to === 'string'
      ? to
      : `${to.pathname ?? ''}${to.search ?? ''}${to.hash ?? ''}`;

  // Some merged code prefixes paths with the URL base (for example via
  // getPathWithUrlBase). react-router already applies the basename, so strip
  // it here to avoid doubling it up.
  const urlBase = window.Sonarr.urlBase;

  if (urlBase && path.startsWith(`${urlBase}/`)) {
    return path.slice(urlBase.length);
  }

  return path;
}

function fallbackNavigate(to) {
  window.location.assign(
    `${window.location.origin}${window.Sonarr.urlBase}${toPath(to)}`
  );
}

const movieHistory = {
  get action() {
    return currentAction;
  },
  get location() {
    return {
      pathname: window.location.pathname,
      search: window.location.search,
      hash: window.location.hash,
      state: undefined,
      key: ''
    };
  },
  get length() {
    return window.history.length;
  },

  push(to, state) {
    if (navigateFn) {
      navigateFn(toPath(to), { state });
    } else {
      fallbackNavigate(to);
    }
  },

  replace(to, state) {
    if (navigateFn) {
      navigateFn(toPath(to), { replace: true, state });
    } else {
      fallbackNavigate(to);
    }
  },

  go(n) {
    if (navigateFn) {
      navigateFn(n);
    } else {
      window.history.go(n);
    }
  },

  goBack() {
    movieHistory.go(-1);
  },

  goForward() {
    movieHistory.go(1);
  },

  block() {
    return () => undefined;
  },

  listen() {
    return () => undefined;
  },

  createHref(to) {
    return `${window.Sonarr.urlBase}${toPath(to)}`;
  }
};

export default movieHistory;
