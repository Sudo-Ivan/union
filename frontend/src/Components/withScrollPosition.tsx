import React from 'react';
import { useNavigationType } from 'react-router-dom';
import scrollPositions from 'Store/scrollPositions';

interface WrappedComponentProps {
  initialScrollTop: number;
}

function withScrollPosition<P extends object>(
  WrappedComponent: React.ComponentType<P & WrappedComponentProps>,
  scrollPositionKey: string
) {
  function ScrollPosition(props: P) {
    const action = useNavigationType();

    const initialScrollTop =
      action === 'POP' ? scrollPositions[scrollPositionKey] : 0;

    return <WrappedComponent {...props} initialScrollTop={initialScrollTop} />;
  }

  return ScrollPosition;
}

export default withScrollPosition;
