using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Documents;
using RestSharp.Extensions;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.TvShows;

namespace SjUpdater.Provider
{
    public class TMDb  : IProvider
    {
        private readonly TMDbClient client;
        private const string API_KEY = "32b427eaba430fb1c86e9d15049e7799";
        public TMDb()
        {
            client = new TMDbClient(API_KEY);
            client.GetConfig();
            //client.MaxRetryCount = 15;
        }

        public object FindShow(String name)
        {
            var x = client.SearchTvShow(name);
            if (x.TotalResults > 0)
            {
                return x.Results.First().Id;
            }
            return null;
        }

        public ShowInformation GetShowInformation(object show)
        {
            if(!(show is int)) throw new ArgumentException();
           var showinfo = client.GetTvShow((int) show,TvShowMethods.Images);
            if(showinfo==null || showinfo.Name==null) return null;

            return new ShowInformation
            {
                IsActive=null,
                NumberEpisodes = showinfo.NumberOfEpisodes,
                NumberSeasons =  showinfo.NumberOfSeasons,
                ProviderHomepage = "https://www.themoviedb.org/tv/"+((int)show),
                PublisherHomepage = (showinfo.Homepage==null || showinfo.Homepage.Trim().Length==0) ?null:showinfo.Homepage,
                Title = showinfo.OriginalName,
                Backdrop = String.IsNullOrWhiteSpace(showinfo.BackdropPath) ? null : client.GetImageUrl("original", showinfo.BackdropPath).AbsoluteUri,
                Poster = String.IsNullOrWhiteSpace(showinfo.PosterPath) ? null : client.GetImageUrl("original", showinfo.PosterPath).AbsoluteUri,
                Backdrops = showinfo.Images.Backdrops,
                Posters = showinfo.Images.Posters
            };
        }

        public SeasonInformation GetSeasonInformation(object show, int season)
        {
            if (!(show is int)) throw new ArgumentException();
            var seasoninfo = client.GetTvSeason((int)show,season, TvSeasonMethods.Images);
            if (seasoninfo == null || seasoninfo.Name==null) return null;

            return new SeasonInformation
            {
                AirDate = seasoninfo.AirDate,
                Overview = seasoninfo.Overview,
                Title =  seasoninfo.Name,
                NumberEpisodes = seasoninfo.Episodes.Count,
                ProviderHomepage = "https://www.themoviedb.org/tv/" + ((int)show) + "/season/"+season,
                PublisherHomepage = null,
                Backdrop = null,
                Poster = String.IsNullOrWhiteSpace(seasoninfo.PosterPath) ? null : client.GetImageUrl("original", seasoninfo.PosterPath).AbsoluteUri,
                Backdrops = seasoninfo.Images.Backdrops,
                Posters = seasoninfo.Images.Posters
            };
        }

        public EpisodeInformation GetEpisodeInformation(object show, int season, int episode)
        {
            if (!(show is int)) throw new ArgumentException();
            var episodeinfo = client.GetTvEpisode((int) show, season, episode, TvEpisodeMethods.Images);
            if (episodeinfo == null || episodeinfo.Name==null) return null;
           
            return new EpisodeInformation
            {
                AirDate =  episodeinfo.AirDate,
                Image = String.IsNullOrWhiteSpace(episodeinfo.StillPath)?null : client.GetImageUrl("original", episodeinfo.StillPath).AbsoluteUri,
                Images =  null, /*client.GetTvEpisodeImages((int)show,season,episode).Stills,*/
                ProviderHomepage = "https://www.themoviedb.org/tv/" + ((int)show) + "/season/" + season + "/episode/"+episode,
                PublisherHomepage = null,
                Title = episodeinfo.Name,
                Overview = episodeinfo.Overview
               
            };
        }

        public String GetFirstImage(object images)
        {
            if (images == null) return null;
            if (!(images is List<ImageData>)) throw new ArgumentException();
            var i = images as List<ImageData>;
            if (i.Count == 0) return null;
            return client.GetImageUrl("original", i.First().FilePath).AbsoluteUri;
        }

        public String GetImage(int? maxwidth = null, int? maxheight = null)
        {
            throw new NotImplementedException();
        }
    }
}