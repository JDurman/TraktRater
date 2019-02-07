namespace TraktRater.Sites
{

	using System.Net;
	using System.Text.RegularExpressions;
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
	using System.Net;

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
					episode.Name = WebUtility.HtmlDecode(episodeMatches[1].Value);
					episode.Season = seasonMatches[1].Value;


					var seasonPage = SideReelAPI.GetTVDBSeasonPage(show.Show.Slug, episode.Season);
					Regex tvdbIDRegex = new Regex("<a href=\"/[^/]+/[^/]+/[^/]+/([^/]+)\">[^>]+>\r\n[ ]+" + episode.Name, RegexOptions.IgnoreCase);
					var tvdbIDMatches = tvdbIDRegex.Matches(seasonPage);
					if (tvdbIDMatches.Count <= 0)
					{
						tvdbIDMatches = tvdbIDRegex.Matches(seasonPage.Asciify());
					}

					if (tvdbIDMatches.Count > 0)
					{
						episode.TVDBId = tvdbIDMatches[0].Groups[1].Value;
						show.Tracked.TrackedShowWrappers.Episodes.Add(episode);
					}
				}
			}

			if (trackedShows.Shows.Any())
			{
				foreach (var show in trackedShows.Shows)
				{
					int pageSize = AppSettings.BatchSize;
					int pages = (int)Math.Ceiling((double)show.Tracked.TrackedShowWrappers.Episodes.Count / pageSize);
					for (int i = 0; i < pages; i++)
					{
						UIUtils.UpdateStatus("Importing page {0}/{1} Sidereel show watch list to trakt.tv watchlist...", i + 1, pages);

						var watchlistMoviesResponse = TraktAPI.AddEpisodesToWatchedHistory(ConvertEpisodesToTraktSync(show.Tracked.TrackedShowWrappers.Episodes.Skip(i * pageSize).Take(pageSize).ToList()));
						if (watchlistMoviesResponse == null)
						{
							UIUtils.UpdateStatus("Failed to send watchlist for Sidereel shows", true);
							Thread.Sleep(2000);
						}

						if (importCancelled) return;
					}
				}
			}
		}


		public void Cancel()
		{
			// signals to cancel import
			importCancelled = true;
		}

		#endregion

		public TraktEpisodeWatchedSync ConvertEpisodesToTraktSync(List<TrackedEpisode> episodes)
		{
			var traktEpisodes = new List<TraktEpisodeWatched>();

			foreach (var episode in episodes)
			{
				if (episode.TVDBId != null)
				{
					var traktEpisode = new TraktEpisodeWatched()
					{
						WatchedAt = "released",
						Ids = new TraktEpisodeId() { TvdbId = Convert.ToInt32(episode.TVDBId) }
					};
					traktEpisodes.Add(traktEpisode);
				}
			}
			return new TraktEpisodeWatchedSync() { Episodes = traktEpisodes };
		}

	}

	public static class StringExtensions
	{
		private static readonly Dictionary<char, string> Replacements = new Dictionary<char, string>();
		/// <summary>Returns the specified string with characters not representable in ASCII codepage 437 converted to a suitable representative equivalent.  Yes, this is lossy.</summary>
		/// <param name="s">A string.</param>
		/// <returns>The supplied string, with smart quotes, fractions, accents and punctuation marks 'normalized' to ASCII equivalents.</returns>
		/// <remarks>This method is lossy. It's a bit of a hack that we use to get clean ASCII text for sending to downlevel e-mail clients.</remarks>
		public static string Asciify(this string s)
		{
			return (String.Join(String.Empty, s.Select(c => Asciify(c)).ToArray()));
		}

		private static string Asciify(char x)
		{
			return Replacements.ContainsKey(x) ? (Replacements[x]) : (x.ToString());
		}

		static StringExtensions()
		{
			Replacements['’'] = "'"; // 75151 occurrences
			Replacements['–'] = "-"; // 23018 occurrences
			Replacements['‘'] = "'"; // 9783 occurrences
			Replacements['”'] = "\""; // 6938 occurrences
			Replacements['“'] = "\""; // 6165 occurrences
			Replacements['…'] = "..."; // 5547 occurrences
			Replacements['£'] = "GBP"; // 3993 occurrences
			Replacements['•'] = "*"; // 2371 occurrences
			Replacements[' '] = " "; // 1529 occurrences
			Replacements['é'] = "e"; // 878 occurrences
			Replacements['ï'] = "i"; // 328 occurrences
			Replacements['´'] = "'"; // 226 occurrences
			Replacements['—'] = "-"; // 133 occurrences
			Replacements['·'] = "*"; // 132 occurrences
			Replacements['„'] = "\""; // 102 occurrences
			Replacements['€'] = "EUR"; // 95 occurrences
			Replacements['®'] = "(R)"; // 91 occurrences
			Replacements['¹'] = "(1)"; // 80 occurrences
			Replacements['«'] = "\""; // 79 occurrences
			Replacements['è'] = "e"; // 79 occurrences
			Replacements['á'] = "a"; // 55 occurrences
			Replacements['™'] = "TM"; // 54 occurrences
			Replacements['»'] = "\""; // 52 occurrences
			Replacements['ç'] = "c"; // 52 occurrences
			Replacements['½'] = "1/2"; // 48 occurrences
			Replacements['­'] = "-"; // 39 occurrences
			Replacements['°'] = " degrees "; // 33 occurrences
			Replacements['ä'] = "a"; // 33 occurrences
			Replacements['É'] = "E"; // 31 occurrences
			Replacements['‚'] = ","; // 31 occurrences
			Replacements['ü'] = "u"; // 30 occurrences
			Replacements['í'] = "i"; // 28 occurrences
			Replacements['ë'] = "e"; // 26 occurrences
			Replacements['ö'] = "o"; // 19 occurrences
			Replacements['à'] = "a"; // 19 occurrences
			Replacements['¬'] = " "; // 17 occurrences
			Replacements['ó'] = "o"; // 15 occurrences
			Replacements['â'] = "a"; // 13 occurrences
			Replacements['ñ'] = "n"; // 13 occurrences
			Replacements['ô'] = "o"; // 10 occurrences
			Replacements['¨'] = ""; // 10 occurrences
			Replacements['å'] = "a"; // 8 occurrences
			Replacements['ã'] = "a"; // 8 occurrences
			Replacements['ˆ'] = ""; // 8 occurrences
			Replacements['©'] = "(c)"; // 6 occurrences
			Replacements['Ä'] = "A"; // 6 occurrences
			Replacements['Ï'] = "I"; // 5 occurrences
			Replacements['ò'] = "o"; // 5 occurrences
			Replacements['ê'] = "e"; // 5 occurrences
			Replacements['î'] = "i"; // 5 occurrences
			Replacements['Ü'] = "U"; // 5 occurrences
			Replacements['Á'] = "A"; // 5 occurrences
			Replacements['ß'] = "ss"; // 4 occurrences
			Replacements['¾'] = "3/4"; // 4 occurrences
			Replacements['È'] = "E"; // 4 occurrences
			Replacements['¼'] = "1/4"; // 3 occurrences
			Replacements['†'] = "+"; // 3 occurrences
			Replacements['³'] = "'"; // 3 occurrences
			Replacements['²'] = "'"; // 3 occurrences
			Replacements['Ø'] = "O"; // 2 occurrences
			Replacements['¸'] = ","; // 2 occurrences
			Replacements['Ë'] = "E"; // 2 occurrences
			Replacements['ú'] = "u"; // 2 occurrences
			Replacements['Ö'] = "O"; // 2 occurrences
			Replacements['û'] = "u"; // 2 occurrences
			Replacements['Ú'] = "U"; // 2 occurrences
			Replacements['Œ'] = "Oe"; // 2 occurrences
			Replacements['º'] = "?"; // 1 occurrences
			Replacements['‰'] = "0/00"; // 1 occurrences
			Replacements['Å'] = "A"; // 1 occurrences
			Replacements['ø'] = "o"; // 1 occurrences
			Replacements['˜'] = "~"; // 1 occurrences
			Replacements['æ'] = "ae"; // 1 occurrences
			Replacements['ù'] = "u"; // 1 occurrences
			Replacements['‹'] = "<"; // 1 occurrences
			Replacements['±'] = "+/-"; // 1 occurrences
		}
	}
}

