﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentTerminal.App.Utilities;
using FluentTerminal.Models;
using Newtonsoft.Json;
using Windows.Storage;

namespace FluentTerminal.App.Services.Implementation
{
    internal class SettingsService : ISettingsService
    {
        public const string ThemesContainerName = "Themes";
        public const string CurrentThemeKey = "CurrentTheme";

        private readonly IDefaultValueProvider _defaultValueProvider;
        private readonly ApplicationDataContainer _localSettings;
        private readonly ApplicationDataContainer _themes;
        private readonly ApplicationDataContainer _roamingSettings;

        public event EventHandler CurrentThemeChanged;
        public event EventHandler TerminalOptionsChanged;
        public event EventHandler ApplicationSettingsChanged;
        public event EventHandler KeyBindingsChanged;

        public SettingsService(IDefaultValueProvider defaultValueProvider)
        {
            _defaultValueProvider = defaultValueProvider;
            _localSettings = ApplicationData.Current.LocalSettings;
            _roamingSettings = ApplicationData.Current.RoamingSettings;

            _themes = _roamingSettings.CreateContainer(ThemesContainerName, ApplicationDataCreateDisposition.Always);

            foreach (var theme in _defaultValueProvider.GetPreInstalledThemes())
            {
                _themes.WriteValueAsJson(theme.Id.ToString(), theme);
            }
        }

        public ShellConfiguration GetShellConfiguration()
        {
            return _localSettings.ReadValueFromJson(nameof(ShellConfiguration), _defaultValueProvider.GetDefaultShellConfiguration());
        }

        public void SaveShellConfiguration(ShellConfiguration shellConfiguration)
        {
            _localSettings.WriteValueAsJson(nameof(ShellConfiguration), shellConfiguration);
        }

        public TerminalTheme GetCurrentTheme()
        {
            var id = GetCurrentThemeId();
            return GetTheme(id);
        }

        public Guid GetCurrentThemeId()
        {
            if (_roamingSettings.Values.TryGetValue(CurrentThemeKey, out object value))
            {
                return (Guid)value;
            }
            return _defaultValueProvider.GetDefaultThemeId();
        }

        public void SaveCurrentThemeId(Guid id)
        {
            _roamingSettings.Values[CurrentThemeKey] = id;

            CurrentThemeChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SaveTheme(TerminalTheme theme)
        {
            _themes.WriteValueAsJson(theme.Id.ToString(), theme);

            if (theme.Id == GetCurrentThemeId())
            {
                CurrentThemeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void DeleteTheme(Guid id)
        {
            _themes.Values.Remove(id.ToString());
        }

        public IEnumerable<TerminalTheme> GetThemes()
        {
            return _themes.Values.Select(x => JsonConvert.DeserializeObject<TerminalTheme>((string)x.Value)).ToList();
        }

        public TerminalTheme GetTheme(Guid id)
        {
            return _themes.ReadValueFromJson(id.ToString(), default(TerminalTheme));
        }

        public TerminalOptions GetTerminalOptions()
        {
            return _roamingSettings.ReadValueFromJson(nameof(TerminalOptions), _defaultValueProvider.GetDefaultTerminalOptions());
        }

        public void SaveTerminalOptions(TerminalOptions terminalOptions)
        {
            _roamingSettings.WriteValueAsJson(nameof(TerminalOptions), terminalOptions);
            TerminalOptionsChanged?.Invoke(this, EventArgs.Empty);
        }

        public ApplicationSettings GetApplicationSettings()
        {
            return _roamingSettings.ReadValueFromJson(nameof(ApplicationSettings), _defaultValueProvider.GetDefaultApplicationSettings());
        }

        public void SaveApplicationSettings(ApplicationSettings applicationSettings)
        {
            _roamingSettings.WriteValueAsJson(nameof(ApplicationSettings), applicationSettings);
            ApplicationSettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        public KeyBindings GetKeyBindings()
        {
            var keyBindings = _roamingSettings.ReadValueFromJson<KeyBindings>(nameof(KeyBindings), null);

            return keyBindings ?? _defaultValueProvider.GetDefaultKeyBindings();
        }

        public void SaveKeyBindings(KeyBindings keyBindings)
        {
            _roamingSettings.WriteValueAsJson(nameof(KeyBindings), keyBindings);
            KeyBindingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}