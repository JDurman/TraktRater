

namespace TraktRater.Sites.API.SideReel
{
    using global::TraktRater.Extensions;
    using global::TraktRater.Web;
    using System.Net;

    public static class SideReelAPI
    {
        public static SideReelShows GetTrackedShows(string authCookie)
        {
            var headers = new WebHeaderCollection();
            headers.Add("Cookie", authCookie);
            string request = TraktWeb.TransmitExtended(SideReelURIs.TrackedShows, headers);
            return request.FromJSON<SideReelShows>();
        }

        public static TrackedShows GetShowWatchHistory(string authCookie, long ShowId)
        {
            var headers = new WebHeaderCollection();
            headers.Add("Cookie", authCookie);
            string request = TraktWeb.TransmitExtended(string.Format("{0}?tv_show_id={1}", SideReelURIs.ShowWatchHistory, ShowId), headers);
            return request.FromJSON<TrackedShows>();
        }

        public static string GetEpisodeDetails(string authCookie, long EpisodeId)
        {
            var headers = new WebHeaderCollection();
            headers.Add("Cookie", authCookie);
            string request = TraktWeb.Transmit(string.Format("{0}/{1}", SideReelURIs.EpisodeDetails, EpisodeId), null, true, headers);
            return request.ToString();
        }

		public static string GetTVDBSeasonPage(string Show, string Season)
		{
		
			var headers = new WebHeaderCollection();
			string request = TraktWeb.Transmit(string.Format("{0}/{1}/seasons/{2}", SideReelURIs.SeasonDetails, Show, Season), null, true, headers);
			return request.ToString();
		}

    }
}
