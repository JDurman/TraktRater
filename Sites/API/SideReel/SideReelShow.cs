
namespace TraktRater.Sites.API.SideReel
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class SideReelShow
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "name")]
        public string Title { get; set; }

		[DataMember(Name = "canonical_name")]
		public string Slug { get; set; }

        [DataMember(Name = "user_rating")]
        public SideReelRating Rating { get; set; }
    }

    [DataContract]
    public class SideReelRating
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "stars")]
        public int Stars { get; set; }

        [DataMember(Name = "created_at")]
        public string CreatedAt { get; set; }

        [DataMember(Name = "updated_at")]
        public string UpdatedAt { get; set; }
    }

    [DataContract]
    public class SideReelShows
    {
        [DataMember(Name = "tracked_tv_shows")]
        public List<SideReelShowWrapper> Shows { get; set; }
    }

    [DataContract]
    public class TrackedShowWrapper
    {
		[DataMember(Name = "episodes")]
		public List<long> EpisodeIds { get; set; }

		public List<TrackedEpisode> Episodes { get; set; }

		[DataMember(Name = "seasons")]
		public List<TrackedSeason> Seasons { get; set; }
	}

	[DataContract]
	public class TrackedSeason
	{
		[DataMember(Name = "id")]
		public int Id { get; set; }

		[DataMember(Name = "watched")]
		public int Watched { get; set; }
	}

	public class TrackedEpisode
	{
		public long Id { get; set; }

		public string Name { get; set; }

		public string Season { get; set; }

		public string TVDBId { get; set; }
	}

	[DataContract]
	public class TrackedShows
	{
		[DataMember(Name = "watch_history")]
		public TrackedShowWrapper TrackedShowWrappers { get; set; }
	}

	[DataContract]
    public class SideReelShowWrapper
    {
        [DataMember(Name = "tracked_tv_show")]
        public TrackedShows Tracked { get; set; }

        [DataMember(Name = "tv_show")]
        public SideReelShow Show { get; set; }
    }
}

