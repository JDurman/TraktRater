﻿namespace TraktRater.Sites.API.TMDb
{
    public static class TMDbURIs
    {
        const string APIKey = "e636af47bb9604b7fe591847a98ca408";

        public const string UserRatingsMovies = @"http://api.themoviedb.org/3/account/{0}/rated/movies?api_key=" + APIKey + "&session_id={1}&page={2}";
        public const string UserRatingsShows = @"http://api.themoviedb.org/3/account/{0}/rated/tv?api_key=" + APIKey + "&session_id={1}&page={2}";
        public const string UserWatchlistMovies = @"http://api.themoviedb.org/3/account/{0}/watchlist/movies?api_key=" + APIKey + "&session_id={1}&page={2}";
        public const string UserWatchlistShows = @"http://api.themoviedb.org/3/account/{0}/watchlist/tv?api_key=" + APIKey + "&session_id={1}&page={2}";
        public const string RequestToken = @"http://api.themoviedb.org/3/authentication/token/new?api_key=" + APIKey;
        public const string RequestSessionId = @"http://api.themoviedb.org/3/authentication/session/new?api_key=" + APIKey + "&request_token={0}";
        public const string AccountInfo = @"http://api.themoviedb.org/3/account?api_key=" + APIKey + "&session_id={0}";
        public const string Authenticate = @"http://www.themoviedb.org/authenticate/{0}";
    }
}
