using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine
{
    public class DownloadDecision
    {
        public RemoteEpisode RemoteEpisode { get; private set; }
        public RemoteMovie RemoteMovie { get; private set; }
        public IEnumerable<DownloadRejection> Rejections { get; private set; }

        public bool Approved => !Rejections.Any();

        public bool TemporarilyRejected
        {
            get
            {
                return Rejections.Any() && Rejections.All(r => r.Type == RejectionType.Temporary);
            }
        }

        public bool Rejected
        {
            get
            {
                return Rejections.Any() && Rejections.Any(r => r.Type == RejectionType.Permanent);
            }
        }

        public DownloadDecision(RemoteEpisode episode, params DownloadRejection[] rejections)
        {
            RemoteEpisode = episode;
            Rejections = rejections.ToList();
        }

        public DownloadDecision(RemoteMovie movie, params DownloadRejection[] rejections)
        {
            RemoteMovie = movie;
            Rejections = rejections.ToList();
        }

        public override string ToString()
        {
            var remote = (object)RemoteEpisode ?? RemoteMovie;

            if (Approved)
            {
                return "[OK] " + remote;
            }

            return "[Rejected " + Rejections.Count() + "]" + remote;
        }
    }
}
