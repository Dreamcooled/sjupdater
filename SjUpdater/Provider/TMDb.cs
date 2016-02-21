using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
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
            try {
                client = new TMDbClient(API_KEY);
                client.GetConfig();
                //client.MaxRetryCount = 15;
            } catch(Exception) { }

        }

        public object FindShow(String name)
        {
            if (!client.HasConfig || client?.Config == null) return null;
            Regex r = new Regex(@"\(([12]\d{3})\)"); //name contains year?
            Match m = r.Match(name);
            if (!m.Success)
            {
                var x = client.SearchTvShow(name);
                if (x.TotalResults > 0)
                {
                    return x.Results.First().Id;
                }
                return null;
            }
            else
            {
                int year = int.Parse(m.Groups[1].Value);
                name=name.Replace(m.Value, "").Trim();//remove year
                var x = client.SearchTvShow(name);
                foreach (var result in x.Results)
                {
                    if (result.FirstAirDate.HasValue && result.FirstAirDate.Value.Year == year)
                    {
                        return result.Id;
                    }
                }
          
                return null;
            }
        }

        public ShowInformation GetShowInformation(object show, bool withImages, bool withPreviousNextEp)
        {
            if(!(show is int)) throw new ArgumentException();
            if (!client.HasConfig || client?.Config == null) return null;
            var showinfo = client.GetTvShow((int) show,withImages?TvShowMethods.Images:TvShowMethods.Undefined);
            if(showinfo?.Name == null) return null;

            var si= new ShowInformation
            {
                Status = showinfo.Status,
                PreviousEpisodeDate = showinfo.LastAirDate,
                NumberEpisodes = showinfo.NumberOfEpisodes,
                NumberSeasons =  showinfo.NumberOfSeasons,
                ProviderHomepage = "https://www.themoviedb.org/tv/"+((int)show),
                PublisherHomepage = (showinfo.Homepage==null || showinfo.Homepage.Trim().Length==0) ?null:showinfo.Homepage,
                Title = showinfo.OriginalName,
                Backdrop = String.IsNullOrWhiteSpace(showinfo.BackdropPath) ? null : client.GetImageUrl("original", showinfo.BackdropPath).AbsoluteUri,
                Poster = String.IsNullOrWhiteSpace(showinfo.PosterPath) ? null : client.GetImageUrl("original", showinfo.PosterPath).AbsoluteUri,
                Backdrops = showinfo.Images?.Backdrops,
                Posters = showinfo.Images?.Posters
            };

            if (withPreviousNextEp)
            {

                bool first = true;
                int lastSeasonNr = -1;
                int lastEpisodeNr = -1;
                DateTime lastEpisodeDateTime = DateTime.MinValue;

                for (int curSeasonIdx = showinfo.Seasons.Count - 1; curSeasonIdx >= 0; curSeasonIdx--)
                {
                    var seasoninfo = client.GetTvSeason((int) show, showinfo.Seasons[curSeasonIdx].SeasonNumber);
                    if (seasoninfo == null) return si;

                    for (int curEpisodeIdx = seasoninfo.Episodes.Count - 1; curEpisodeIdx >= 0; curEpisodeIdx--)
                    {
                        if (seasoninfo.Episodes[curEpisodeIdx].AirDate.Date <= DateTime.Today)
                            //air date of ep lies in past
                        {
                            if (first)
                                //first check => there's no next episode because the first that we checked already lies in the past
                            {
                                si.PreviousEpisodeSeasonNr = showinfo.Seasons[curSeasonIdx].SeasonNumber;
                                si.PreviousEpisodeEpisodeNr = seasoninfo.Episodes[curEpisodeIdx].EpisodeNumber;
                                si.PreviousEpisodeDate = seasoninfo.Episodes[curEpisodeIdx].AirDate;
                                return si;
                            }
                            else
                            // no the first check => the last ep we iterated through must be the "next" to be aired
                            {
                                si.NextEpisodeSeasonNr = lastSeasonNr;
                                si.NextEpisodeEpisodeNr = lastEpisodeNr;
                                si.NextEpisodeDate = lastEpisodeDateTime;
                                si.PreviousEpisodeSeasonNr = showinfo.Seasons[curSeasonIdx].SeasonNumber;
                                si.PreviousEpisodeEpisodeNr = seasoninfo.Episodes[curEpisodeIdx].EpisodeNumber;
                                si.PreviousEpisodeDate = seasoninfo.Episodes[curEpisodeIdx].AirDate;
                                return si;
                            }
                        }
                        first = false;
                        lastEpisodeNr = seasoninfo.Episodes[curEpisodeIdx].EpisodeNumber;
                        lastEpisodeDateTime = seasoninfo.Episodes[curEpisodeIdx].AirDate;
                        lastSeasonNr = showinfo.Seasons[curSeasonIdx].SeasonNumber;
                    }
                }
            }
            return si;
        }

        public SeasonInformation GetSeasonInformation(object show, int season, bool withImages)
        {
            if (!(show is int)) throw new ArgumentException();
            if (!client.HasConfig || client?.Config == null) return null;
            var seasoninfo = client.GetTvSeason((int)show,season, withImages ? TvSeasonMethods.Images : TvSeasonMethods.Undefined);
            if (seasoninfo?.Name == null) return null;

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
                Backdrops = seasoninfo.Images?.Backdrops,
                Posters = seasoninfo.Images?.Posters
            };
        }

        public EpisodeInformation GetEpisodeInformation(object show, int season, int episode, bool withImages)
        {
            if (!(show is int)) throw new ArgumentException();
            if (!client.HasConfig || client?.Config == null) return null;
            var episodeinfo = client.GetTvEpisode((int) show, season, episode, withImages ? TvEpisodeMethods.Images : TvEpisodeMethods.Undefined);
            if (episodeinfo?.Name == null) return null;
           
            return new EpisodeInformation
            {
                AirDate =  episodeinfo.AirDate,
                Image = String.IsNullOrWhiteSpace(episodeinfo.StillPath)?null : client.GetImageUrl("w600", episodeinfo.StillPath).AbsoluteUri,
                Images =  episodeinfo.Images?.Stills,
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
            if (!client.HasConfig || client?.Config == null) return null;
            var i = (List<ImageData>) images;
            if (i.Count == 0) return null;
            return client.GetImageUrl("original", i.First().FilePath).AbsoluteUri;
        }

        public String GetImage(int? maxwidth = null, int? maxheight = null)
        {
            throw new NotImplementedException();
        }
    }
}