namespace TraktRater.Sites
{

	using System.Net;
	using System.Text.RegularExpressions;
	using System.Text;
	using System;


	using global::TraktRater.Extensions;
	using global::TraktRater.Settings;
	using global::TraktRater.Sites.API.TMDb;
	using global::TraktRater.TraktAPI;
	using global::TraktRater.TraktAPI.DataStructures;
	using global::TraktRater.UI;
	using global::TraktRater.Sites.API.SideReel;
	using global::TraktRater.Web;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;

	class SideReel : IRateSite
	{
		#region Variables

		bool importCancelled = false;
		string UserName;
		string Password;

		#endregion

		#region Constructor

		public SideReel(string UserName, string Password)
		{
			this.UserName = UserName;
			this.Password = Password;
			Enabled = !String.IsNullOrEmpty(this.UserName) && !String.IsNullOrEmpty(this.Password);
		}

		#endregion

		#region IRateSite Members

		public bool Enabled { get; set; }

		public string Name { get { return "SideReel"; } }

		public void ImportRatings()
		{
			#region Getting Auth Cookie
			var client = new ExtendedWebClient();
			string loginPage = client.DownloadString("https://www.sidereel.com/users/login");
			Regex tokenRegex = new Regex("(?<=<input type=\"hidden\" name=\"authenticity_token\" value=\").+(?=\" />)");
			var tokenMatches = tokenRegex.Matches(loginPage);
			var formToken = tokenMatches[1].Value;

			var reqparm = new System.Collections.Specialized.NameValueCollection();
			reqparm.Add("authenticity_token", formToken);
			reqparm.Add("user[email]", UserName);
			reqparm.Add("user[password]", Password);

			var loginFormPost = client.UploadValues("https://www.sidereel.com/users/login", "POST", reqparm);
			var cookie = client.GetHeaderContainer()["Cookie"];
			#endregion

			//gets list of tracked Shows
			var trackedShows = SideReelAPI.GetTrackedShows(cookie);

			//Build list of shows and episode names
			foreach (var show in trackedShows.Shows)
			{
				//Get Show Watched History
				show.Tracked = SideReelAPI.GetShowWatchHistory(cookie, show.Show.Id);

				show.Tracked.TrackedShowWrappers.Episodes = new List<TrackedEpisode>();
				//Get names for the shows based on the sidereelID
				foreach (var episodeId in show.Tracked.TrackedShowWrappers.EpisodeIds)
				{

					var episode = new TrackedEpisode();
					var episodePage = SideReelAPI.GetEpisodeDetails(cookie, episodeId);

					Regex episodeRegex = new Regex("<div class='episode-title h2-5'>\\n<a[^>]*>([^<]*)</a>");
					var episodeMatches = episodeRegex.Matches(episodePage)[0].Groups;

					Regex seasonRegex = new Regex("data-season=\"([0-9]+)\""); //Still not working
					var seasonMatches = seasonRegex.Matches(episodePage)[0].Groups;

					//TODO: Need to get season number from the episode detail partial

					episode.Id = episodeId;
					episode.Name = episodeMatches[1].Value;
					episode.Season = seasonMatches[1].Value;




					var seasonPage = SideReelAPI.GetTVDBSeasonPage(show.Show.Slug, episode.Season);
					Regex tvdbIDRegex = new Regex("<a href=\"/[^/]+/[^/]+/[^/]+/([^/]+)\">[^>]+>\r\n[ ]+" + episode.Name);
					var tvdbIDMatches = tvdbIDRegex.Matches(seasonPage)[0].Groups;
					episode.TVDBId = tvdbIDMatches[1].Value;

					show.Tracked.TrackedShowWrappers.Episodes.Add(episode);
				}
			}

			//if (trackedShows.Shows.Any())
			//{
			//	foreach (var show in trackedShows.Shows)
			//	{
			//		int pageSize = AppSettings.BatchSize;
			//		int pages = (int)Math.Ceiling((double)show.Tracked.TrackedShowWrappers.Episodes.Count / pageSize);
			//		for (int i = 0; i < pages; i++)
			//		{
			//			UIUtils.UpdateStatus("Importing page {0}/{1} Sidereel show watch list to trakt.tv watchlist...", i + 1, pages);

			//			var watchlistMoviesResponse = TraktAPI.AddEpisodesToWatchedHistory(ConvertEpisodesToTraktSync(show.Tracked.TrackedShowWrappers.Episodes.Skip(i * pageSize).Take(pageSize).ToList()));
			//			if (watchlistMoviesResponse == null)
			//			{
			//				UIUtils.UpdateStatus("Failed to send watchlist for Sidereel shows", true);
			//				Thread.Sleep(2000);
			//			}

			//			if (importCancelled) return;
			//		}
			//	}
			//}
		}









		public void Cancel()
		{
			// signals to cancel import
			importCancelled = true;
		}

		#endregion

		//public TraktEpisodeWatchedSync ConvertEpisodesToTraktSync(List<SideReelShowWrapper> Shows)
		//{
		//	var traktShows = new List<TraktShow>();

		//	foreach (var show in Shows)
		//	{
		//		var traktShow = new TraktEpisodeWatched()
		//		{
		//			WatchedAt = "released"
		//		};
		//	}
		//}

	}
}
