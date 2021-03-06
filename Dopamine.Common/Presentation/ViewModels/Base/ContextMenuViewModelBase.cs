﻿using Digimezzo.Utilities.Utils;
using Dopamine.Common.Database;
using Dopamine.Common.Services.Dialog;
using Dopamine.Common.Services.Playback;
using Dopamine.Common.Services.Playlist;
using Dopamine.Common.Services.Provider;
using Microsoft.Practices.Unity;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Dopamine.Common.Presentation.ViewModels.Base
{
    public abstract class ContextMenuViewModelBase : BindableBase
    {
        #region Variables
        private IProviderService providerService;
        private IPlaylistService playlistService;
        private IPlaybackService playbackService;
        private IDialogService dialogService;
        private ObservableCollection<SearchProvider> contextMenuSearchProviders;
        private ObservableCollection<PlaylistViewModel> contextMenuPlaylists;
        #endregion

        #region Commands
        public DelegateCommand<string> SearchOnlineCommand { get; set; }
        public DelegateCommand<string> AddPlayingTrackToPlaylistCommand { get; set; }
        #endregion

        #region Properties
        public bool HasContextMenuPlaylists => this.ContextMenuPlaylists != null && this.ContextMenuPlaylists.Count > 0;

        public ObservableCollection<SearchProvider> ContextMenuSearchProviders
        {
            get { return this.contextMenuSearchProviders; }
            set
            {
                SetProperty<ObservableCollection<SearchProvider>>(ref this.contextMenuSearchProviders, value);
                OnPropertyChanged(() => this.HasContextMenuSearchProviders);
            }
        }

        public ObservableCollection<PlaylistViewModel> ContextMenuPlaylists
        {
            get { return this.contextMenuPlaylists; }
            set
            {
                SetProperty<ObservableCollection<PlaylistViewModel>>(ref this.contextMenuPlaylists, value);
                OnPropertyChanged(() => this.HasContextMenuPlaylists);
            }
        }
        #endregion

        #region Construction
        public ContextMenuViewModelBase(IUnityContainer container)
        {
            // Dependency injection
            this.providerService = container.Resolve<IProviderService>();
            this.playlistService = container.Resolve<IPlaylistService>();
            this.playbackService = container.Resolve<IPlaybackService>();
            this.dialogService = container.Resolve<IDialogService>();

            // Commands
            this.SearchOnlineCommand = new DelegateCommand<string>((id) => this.SearchOnline(id));
            this.AddPlayingTrackToPlaylistCommand = new DelegateCommand<string>(
            async (playlistName) => await this.AddPlayingTrackToPlaylistAsync(playlistName), (_) => this.playbackService.HasCurrentTrack);

            // Events
            this.providerService.SearchProvidersChanged += (_, __) => { this.GetSearchProvidersAsync(); };
            this.playlistService.PlaylistAdded += (_) => this.GetContextMenuPlaylistsAsync();
            this.playlistService.PlaylistDeleted += (_) => this.GetContextMenuPlaylistsAsync();
            this.playbackService.PlaybackFailed += (_, __) => this.AddPlayingTrackToPlaylistCommand.RaiseCanExecuteChanged();
            this.playbackService.PlaybackSuccess += (_) => this.AddPlayingTrackToPlaylistCommand.RaiseCanExecuteChanged();
            this.playbackService.PlaybackStopped += (_, __) => this.AddPlayingTrackToPlaylistCommand.RaiseCanExecuteChanged();
            this.playbackService.PlaybackPaused += (_, __) => this.AddPlayingTrackToPlaylistCommand.RaiseCanExecuteChanged();
            this.playbackService.PlaybackResumed += (_, __) => this.AddPlayingTrackToPlaylistCommand.RaiseCanExecuteChanged();

            // Initialize the search providers in the ContextMenu
            this.GetSearchProvidersAsync();

            // Initialize the playlists in the ContextMenu
            this.GetContextMenuPlaylistsAsync();
        }

        private async Task AddPlayingTrackToPlaylistAsync(string playlistName)
        {
            if (!this.playbackService.HasCurrentTrack)
            {
                return;
            }

            var playingTrack = new List<PlayableTrack>() { this.playbackService.CurrentTrack.Value };
            await this.AddTracksToPlaylistAsync(playlistName, playingTrack);
        }
        #endregion

        #region Private
        private async void GetSearchProvidersAsync()
        {
            this.ContextMenuSearchProviders = null;

            List<SearchProvider> providersList = await this.providerService.GetSearchProvidersAsync();
            var localProviders = new ObservableCollection<SearchProvider>();

            await Task.Run(() =>
            {
                foreach (SearchProvider vp in providersList)
                {
                    localProviders.Add(vp);
                }
            });

            this.ContextMenuSearchProviders = localProviders;
        }

        public async void GetContextMenuPlaylistsAsync()
        {
            try
            {
                // Unbind to improve UI performance
                this.ContextMenuPlaylists = null;

                List<string> playlists = await this.playlistService.GetPlaylistsAsync();

                // Populate an ObservableCollection
                var playlistViewModels = new ObservableCollection<PlaylistViewModel>();

                await Task.Run(() =>
                {
                    foreach (string playlist in playlists)
                    {
                        playlistViewModels.Add(new PlaylistViewModel { Name = playlist });
                    }

                });

                // Re-bind to update the UI
                this.ContextMenuPlaylists = playlistViewModels;

            }
            catch (Exception)
            {
                // If loading from the database failed, create and empty Collection.
                this.ContextMenuPlaylists = new ObservableCollection<PlaylistViewModel>();
            }
        }
        #endregion

        #region Protected
        protected bool HasContextMenuSearchProviders => this.ContextMenuSearchProviders != null && this.ContextMenuSearchProviders.Count > 0;

        protected async Task AddTracksToPlaylistAsync(string playlistName, IList<PlayableTrack> tracks)
        {
            AddPlaylistResult addPlaylistResult = AddPlaylistResult.Success; // Default Success

            // If no playlist is provided, first create one.
            if (playlistName == null)
            {
                var responseText = ResourceUtils.GetStringResource("Language_New_Playlist");

                if (this.dialogService.ShowInputDialog(
                    0xea37,
                    16,
                    ResourceUtils.GetStringResource("Language_New_Playlist"),
                    ResourceUtils.GetStringResource("Language_Enter_Name_For_New_Playlist"),
                    ResourceUtils.GetStringResource("Language_Ok"),
                    ResourceUtils.GetStringResource("Language_Cancel"),
                    ref responseText))
                {
                    playlistName = responseText;
                    addPlaylistResult = await this.playlistService.AddPlaylistAsync(playlistName);
                }
            }

            // If playlist name is still null, the user clicked cancel on the previous dialog. Stop here.
            if (playlistName == null) return;

            // Verify if the playlist was added
            switch (addPlaylistResult)
            {
                case AddPlaylistResult.Success:
                case AddPlaylistResult.Duplicate:
                    // Add items to playlist
                    AddTracksToPlaylistResult result = await this.playlistService.AddTracksToPlaylistAsync(tracks, playlistName);

                    if (result == AddTracksToPlaylistResult.Error)
                    {
                        this.dialogService.ShowNotification(0xe711, 16, ResourceUtils.GetStringResource("Language_Error"), ResourceUtils.GetStringResource("Language_Error_Adding_Songs_To_Playlist").Replace("%playlistname%", "\"" + playlistName + "\""), ResourceUtils.GetStringResource("Language_Ok"), true, ResourceUtils.GetStringResource("Language_Log_File"));
                    }
                    break;
                case AddPlaylistResult.Error:
                    this.dialogService.ShowNotification(
                        0xe711,
                        16,
                        ResourceUtils.GetStringResource("Language_Error"),
                        ResourceUtils.GetStringResource("Language_Error_Adding_Playlist"),
                        ResourceUtils.GetStringResource("Language_Ok"),
                        true,
                        ResourceUtils.GetStringResource("Language_Log_File"));
                    break;
                case AddPlaylistResult.Blank:
                    this.dialogService.ShowNotification(
                        0xe711,
                        16,
                        ResourceUtils.GetStringResource("Language_Error"),
                        ResourceUtils.GetStringResource("Language_Provide_Playlist_Name"),
                        ResourceUtils.GetStringResource("Language_Ok"),
                        false,
                        string.Empty);
                    break;
                default:
                    // Never happens
                    break;
            }
        }
        #endregion

        #region Abstract
        protected abstract void SearchOnline(string id);
        #endregion
    }
}
