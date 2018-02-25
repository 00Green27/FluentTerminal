﻿using FluentTerminal.Models;
using System;
using System.Collections.Generic;

namespace FluentTerminal.App.Services
{
    public interface IDefaultValueProvider
    {
        ShellConfiguration GetDefaultShellConfiguration();
        TerminalOptions GetDefaultTerminalOptions();
        KeyBindings GetDefaultKeyBindings();
        ApplicationSettings GetDefaultApplicationSettings();
        Guid GetDefaultThemeId();
        IEnumerable<TerminalTheme> GetPreInstalledThemes();
    }
}
