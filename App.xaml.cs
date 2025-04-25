using DioramaEngine.Models;
using DioramaEngine.Services;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;

namespace DioramaEngine
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var args = Environment.GetCommandLineArgs();
            Dictionary<string, string> _args = [];
            for (int index = 1; index < args.Length; index += 2)
            {
                _args.Add(args[index], args[index + 1]);
            }

            if (_args.TryGetValue("-skse", out string? sksePath))
            {
                Settings.SksePath = sksePath;
            }
        }
    }

}
