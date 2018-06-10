﻿using FluentTerminal.App.Exceptions;
using FluentTerminal.App.Services;
using FluentTerminal.Models;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage.Pickers;
using Windows.UI;

namespace FluentTerminal.App.ViewModels.Settings
{
    public class ThemesPageViewModel : ViewModelBase
    {
        private readonly IDefaultValueProvider _defaultValueProvider;
        private readonly IDialogService _dialogService;
        private readonly ISettingsService _settingsService;
        private ThemeViewModel _selectedTheme;
        private double _backgroundOpacity;
        private readonly IThemeParserFactory _themeParserFactory;

        public event EventHandler<Color> SelectedThemeBackgroundColorChanged;

        public ThemesPageViewModel(ISettingsService settingsService, IDialogService dialogService, IDefaultValueProvider defaultValueProvider, IThemeParserFactory themeParserFactory)
        {
            _settingsService = settingsService;
            _dialogService = dialogService;
            _defaultValueProvider = defaultValueProvider;
            _themeParserFactory = themeParserFactory;

            CreateThemeCommand = new RelayCommand(CreateTheme);
            ImportThemeCommand = new RelayCommand(ImportTheme);

            _settingsService.TerminalOptionsChanged += OnTerminalOptionsChanged;

            BackgroundOpacity = _settingsService.GetTerminalOptions().BackgroundOpacity;

            var activeThemeId = _settingsService.GetCurrentThemeId();
            foreach (var theme in _settingsService.GetThemes())
            {
                var viewModel = new ThemeViewModel(theme, _settingsService, _dialogService);
                viewModel.Activated += OnThemeActivated;
                viewModel.Deleted += OnThemeDeleted;

                if (theme.Id == activeThemeId)
                {
                    viewModel.IsActive = true;
                }
                Themes.Add(viewModel);
            }

            SelectedTheme = Themes.First(t => t.IsActive);

        }

        public RelayCommand CreateThemeCommand { get; }
        public RelayCommand ImportThemeCommand { get; }

        public double BackgroundOpacity
        {
            get => _backgroundOpacity;
            set => Set(ref _backgroundOpacity, value);
        }

        public ThemeViewModel SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (_selectedTheme != null)
                {
                    _selectedTheme.BackgroundChanged -= OnSelectedThemeBackgroundChanged;
                }
                Set(ref _selectedTheme, value);
                if (value != null)
                {
                    value.BackgroundChanged += OnSelectedThemeBackgroundChanged;
                }
            }
        }

        public ObservableCollection<ThemeViewModel> Themes { get; } = new ObservableCollection<ThemeViewModel>();

        private void CreateTheme()
        {
            var defaultTheme = _settingsService.GetTheme(_defaultValueProvider.GetDefaultThemeId());
            var theme = new TerminalTheme
            {
                Id = Guid.NewGuid(),
                PreInstalled = false,
                Name = "New Theme",
                Colors = new TerminalColors(defaultTheme.Colors)
            };

            _settingsService.SaveTheme(theme);

            var viewModel = new ThemeViewModel(theme, _settingsService, _dialogService);
            viewModel.Activated += OnThemeActivated;
            viewModel.Deleted += OnThemeDeleted;
            Themes.Add(viewModel);
            SelectedTheme = viewModel;
        }

        private async void ImportTheme()
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };

            foreach (var supportedFileType in _themeParserFactory.SupportedFileTypes)
            {
                picker.FileTypeFilter.Add(supportedFileType);
            }

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var parser = _themeParserFactory.GetParser(file);

                if (parser == null)
                {
                    await _dialogService.ShowMessageDialogAsnyc("Import theme failed", "No suitable parser found", DialogButton.OK).ConfigureAwait(false);
                    return;
                }

                try
                {
                    var theme = await parser.Parse(file).ConfigureAwait(true);

                    _settingsService.SaveTheme(theme);

                    var viewModel = new ThemeViewModel(theme, _settingsService, _dialogService);
                    viewModel.Activated += OnThemeActivated;
                    viewModel.Deleted += OnThemeDeleted;
                    Themes.Add(viewModel);
                    SelectedTheme = viewModel;
                }
                catch (Exception exception)
                {
                    await _dialogService.ShowMessageDialogAsnyc("Import theme failed", exception.Message, DialogButton.OK).ConfigureAwait(false);
                }
            }
        }

        private void OnThemeActivated(object sender, EventArgs e)
        {
            if (sender is ThemeViewModel activatedTheme)
            {
                _settingsService.SaveCurrentThemeId(activatedTheme.Id);

                foreach (var theme in Themes)
                {
                    theme.IsActive = theme.Id == activatedTheme.Id;
                }
            }
        }

        private void OnThemeDeleted(object sender, EventArgs e)
        {
            if (sender is ThemeViewModel theme)
            {
                if (SelectedTheme == theme)
                {
                    SelectedTheme = Themes.First();
                }
                Themes.Remove(theme);

                if (theme.IsActive)
                {
                    Themes.First().IsActive = true;
                    _settingsService.SaveCurrentThemeId(Themes.First().Id);
                }
                _settingsService.DeleteTheme(theme.Id);
            }
        }

        private void OnTerminalOptionsChanged(object sender, TerminalOptions e)
        {
            BackgroundOpacity = e.BackgroundOpacity;
        }

        private void OnSelectedThemeBackgroundChanged(object sender, Color e)
        {
            SelectedThemeBackgroundColorChanged?.Invoke(this, e);
        }
    }
}