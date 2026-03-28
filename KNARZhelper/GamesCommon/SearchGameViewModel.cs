using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using DataGridSelectionMode = System.Windows.Controls.DataGridSelectionMode;

namespace KNARZhelper.GamesCommon
{
    public class SearchGameViewModel : ObservableObject, IEditableObject
    {
        public readonly bool AddGamesMode;

        private readonly Action<List<GameEx>> _addGameAction;
        private readonly GameEx _returnedGame;
        private readonly IGameSearchSettings _settings;
        private string _closeButtonLabel = "Close";
        private FilterPreset _currentPreset;
        private ObservableCollection<FilterPreset> _filterPresets;
        private ObservableCollection<GameEx> _games = new ObservableCollection<GameEx>();
        private CollectionViewSource _gamesViewSource;
        private string _searchTerm = string.Empty;
        private GameEx _selectedGame;

        public SearchGameViewModel(IGameSearchSettings settings, Action<List<GameEx>> addGameAction, bool addGamesMode = true, string closeButtonLabel = "Close", GameEx returnedGame = null)
        {
            _settings = settings;
            _addGameAction = addGameAction;
            AddGamesMode = addGamesMode;
            CloseButtonLabel = closeButtonLabel;

            if (returnedGame != null)
            {
                _returnedGame = returnedGame;
            }

            _filterPresets = API.Instance.Database.FilterPresets.OrderBy(x => x.Name).ToObservable();

            GamesViewSource = new CollectionViewSource
            {
                Source = _games
            };

            GamesViewSource.SortDescriptions.Add(new SortDescription("RealSortingName", ListSortDirection.Ascending));
            GamesViewSource.IsLiveSortingRequested = true;
        }

        public Visibility AddGamesButtonVisibility => AddGamesMode
            ? Visibility.Visible
            : Visibility.Collapsed;

        public RelayCommand<IList<object>> AddGamesCommand => new RelayCommand<IList<object>>(items => _addGameAction(items.Cast<GameEx>().ToList()), items => items != null && items.Count > 0);

        public string CloseButtonLabel
        {
            get => _closeButtonLabel;
            set => SetValue(ref _closeButtonLabel, value);
        }

        public RelayCommand<Window> CloseCommand => new RelayCommand<Window>(win =>
        {
            var dialogResult = true;

            if (_returnedGame != null)
            {
                if (_selectedGame?.Game != null)
                {
                    _returnedGame.Game = _selectedGame.Game;
                }
                else if (_games?.Count > 0)
                {
                    _returnedGame.Game = _games.First().Game;
                }
                else
                {
                    dialogResult = false;
                }
            }

            CloseView(win, dialogResult);
        });

        public FilterPreset CurrentPreset
        {
            get => _currentPreset;
            set
            {
                SetValue(ref _currentPreset, value);
                LoadGames();
            }
        }

        public ObservableCollection<FilterPreset> FilterPresets
        {
            get => _filterPresets;
            set => SetValue(ref _filterPresets, value);
        }

        public Visibility GameGridCompletionStatusVisibility => _settings.GameGridShowCompletionStatus
            ? Visibility.Visible
            : Visibility.Collapsed;

        public Visibility GameGridHiddenVisibility => _settings.GameGridShowHidden
            ? Visibility.Visible
            : Visibility.Collapsed;

        public Visibility GameGridPlatformVisibility => _settings.GameGridShowPlatform
            ? Visibility.Visible
            : Visibility.Collapsed;

        public Visibility GameGridReleaseVisibility => _settings.GameGridShowReleaseYear
            ? Visibility.Visible
            : Visibility.Collapsed;

        public ObservableCollection<GameEx> Games
        {
            get => _games;
            set => SetValue(ref _games, value);
        }

        public CollectionViewSource GamesViewSource
        {
            get => _gamesViewSource;
            set => SetValue(ref _gamesViewSource, value);
        }

        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                SetValue(ref _searchTerm, value);
                LoadGames();
            }
        }

        public GameEx SelectedGame
        {
            get => _selectedGame;
            set => SetValue(ref _selectedGame, value);
        }

        public RelayCommand<Window> SelectGameCommand => new RelayCommand<Window>(win =>
        {
            if (AddGamesMode)
            {
                return;
            }

            Game gameToReturn = null;

            if (_selectedGame?.Game != null)
            {
                gameToReturn = _selectedGame.Game;
            }
            else if (_games?.Count > 0)
            {
                gameToReturn = _games.First().Game;
            }

            if (gameToReturn == null)
            {
                return;
            }

            _returnedGame.Game = gameToReturn;

            CloseView(win);
        });

        public DataGridSelectionMode SelectionMode => AddGamesMode
            ? DataGridSelectionMode.Extended
            : DataGridSelectionMode.Single;

        public void BeginEdit()
        {
        }

        public void CancelEdit()
        {
        }

        public void EndEdit()
        {
        }

        private void CloseView(Window win, bool dialogResult = true)
        {
            _settings.GameSearchWindowHeight = Convert.ToInt32(win.Height);
            _settings.GameSearchWindowWidth = Convert.ToInt32(win.Width);
            win.DialogResult = dialogResult;
            win.Close();
        }

        private void LoadGames()
        {
            Games.Clear();

            if (_searchTerm.Length == 0)
            {
                return;
            }

            Cursor.Current = Cursors.WaitCursor;

            try
            {
                FilterPresetSettings filterSettings;

                if (_currentPreset != null)
                {
                    filterSettings = _currentPreset.Settings;
                    filterSettings.Name = _searchTerm;
                }
                else
                {
                    filterSettings = new FilterPresetSettings
                    {
                        Name = _searchTerm
                    };
                }

                var games = API.Instance.Database.GetFilteredGames(filterSettings, true);

                foreach (var game in games.OrderBy(g => string.IsNullOrEmpty(g.SortingName) ? g.Name : g.SortingName).ToList())
                {
                    Games.Add(new GameEx
                    {
                        Game = game,
                        Platforms = string.Join(", ", game.Platforms?.Select(x => x.Name).ToList() ?? new List<string>())
                    });
                }
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }
    }
}
