﻿using Dopamine.Core.API.Lastfm;
using Prism.Mvvm;
using System.Text.RegularExpressions;

namespace Dopamine.Common.Presentation.ViewModels
{
    public class ArtistInfoViewModel : BindableBase
    {
        #region Variables
        private LastFmArtist lfmArtist;
        #endregion

        #region Properties
        public bool IsArtistInfoAvailable
        {
            get { return this.lfmArtist != null; }
        }

        public LastFmArtist LfmArtist
        {
            get { return this.lfmArtist; }
            set
            {
                SetProperty<LastFmArtist>(ref this.lfmArtist, value);
                OnPropertyChanged(() => this.Biography);
                OnPropertyChanged(() => this.Url);
                OnPropertyChanged(() => this.CleanedBiographyContent);
                OnPropertyChanged(() => this.UrlText);
                OnPropertyChanged(() => this.ArtistName);
                OnPropertyChanged(() => this.Image);
                OnPropertyChanged(() => this.IsArtistInfoAvailable);
            }
        }

        public string Image
        {
            get
            {
                if (this.lfmArtist == null) return string.Empty;

                if (!string.IsNullOrEmpty(this.lfmArtist.ImageMega))
                {
                    return this.lfmArtist.ImageMega;
                }
                else if (!string.IsNullOrEmpty(this.lfmArtist.ImageExtraLarge))
                {
                    return this.lfmArtist.ImageExtraLarge;
                }
                else if (!string.IsNullOrEmpty(this.lfmArtist.ImageLarge))
                {
                    return this.lfmArtist.ImageLarge;
                }
                else if (!string.IsNullOrEmpty(this.lfmArtist.ImageMedium))
                {
                    return this.lfmArtist.ImageMedium;
                }
                else if (!string.IsNullOrEmpty(this.lfmArtist.ImageSmall))
                {
                    return this.lfmArtist.ImageSmall;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public string ArtistName
        {
            get
            {
                if (this.lfmArtist == null) return string.Empty;

                return this.lfmArtist.Name;
            }
        }

        public string Url
        {
            get
            {
                if (this.lfmArtist == null) return string.Empty;

                return this.lfmArtist.Url;
            }
        }

        public string UrlText
        {
            get
            {
                if (this.Biography == null) return string.Empty;

                Regex regex = new Regex(@"(>.*<\/a>)");
                Match match = regex.Match(this.Biography.Content);

                if (match.Success)
                {
                    return match.Groups[0].Value.Replace("</a>", "").Replace(">", "");
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public LastFmBiography Biography
        {
            get
            {
                if (this.lfmArtist == null) return null;

                return this.lfmArtist.Biography;
            }
        }

        public string CleanedBiographyContent
        {
            get
            {
                if (this.Biography == null) return string.Empty;

                // Removes the URL from the Biography content
                string cleanedBiography = Regex.Replace(this.Biography.Content, @"(<a.*$)", "").Trim();
                return cleanedBiography;
            }
        }
        #endregion
    }
}