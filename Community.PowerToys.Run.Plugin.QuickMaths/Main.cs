using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Controls;
using Microsoft.PowerToys.Settings.UI.Library;
using ManagedCommon;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.QuickMaths
{
    public class Main : IPlugin, ISettingProvider, IDelayedExecutionPlugin, IContextMenu, IDisposable
    {
        private PluginInitContext? _context;
        private string? _iconPath;
        private readonly IWolframClient _wolframClient;

        private string _appId = string.Empty;

        // Must match the "ID" field in plugin.json — required by PowerToys Run 0.85+
        public static string PluginID => "F88A212A45C34BB6B3A9A7A7B09F6D1D";

        public string Name => "QuickMaths";
        public string Description => "Intelligently detects math queries and fetches results via Wolfram Alpha.";
        public string SettingAppId => "QuickMaths_AppId";

        public Main() : this(new WolframClient()) { }

        internal Main(IWolframClient wolframClient)
        {
            _wolframClient = wolframClient;
        }

        public void Init(PluginInitContext context)
        {
            try
            {
                PluginLogger.Initialize();
                PluginLogger.Info("Init started. PluginDirectory=" + context.CurrentPluginMetadata.PluginDirectory);

                _context = context;
                _context.API.ThemeChanged += OnThemeChanged;
                UpdateIconPath(_context.API.GetCurrentTheme());

                PluginLogger.Info("Init completed successfully.");
            }
            catch (Exception ex)
            {
                PluginLogger.Error("Init threw an unhandled exception.", ex);
                throw;
            }
        }

        private void UpdateIconPath(Theme theme)
        {
            _iconPath = "Images/QuickMaths.png";
            PluginLogger.Debug($"Icon path updated for theme {theme}: {_iconPath}");
        }

        private void OnThemeChanged(Theme currentTheme, Theme newTheme)
        {
            PluginLogger.Debug($"Theme changed from {currentTheme} to {newTheme}.");
            UpdateIconPath(newTheme);
        }

        public Control? CreateSettingPanel() => null;

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            try
            {
                PluginLogger.Info($"UpdateSettings called. Options present={settings?.AdditionalOptions != null}");

                if (settings?.AdditionalOptions == null)
                    return;

                foreach (var option in settings.AdditionalOptions)
                {
                    if (option.Key == SettingAppId)
                    {
                        _appId = option.TextValue ?? string.Empty;
                        _wolframClient.UpdateAppId(_appId);
                        PluginLogger.Info("App ID " + (string.IsNullOrWhiteSpace(_appId) ? "cleared" : "updated (value hidden)") + ".");
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLogger.Error("UpdateSettings threw an unhandled exception.", ex);
            }
        }

        /// <summary>
        /// True when the user typed the '=' keyword explicitly, so every query is intentional
        /// and should bypass the MathDetector filter.
        /// </summary>
        private static bool IsExplicitInvocation(Query query) =>
            query.ActionKeyword == "=";

        public List<Result> Query(Query query)
        {
            PluginLogger.Debug($"Immediate Query: actionKeyword='{query.ActionKeyword}' search='{query.Search}'");
            return new List<Result>();
        }

        public List<Result> Query(Query query, bool delayedExecution)
        {
            if (!delayedExecution)
                return new List<Result>();

            if (string.IsNullOrWhiteSpace(query.Search))
                return new List<Result>();

            var explicit_ = IsExplicitInvocation(query);
            var isMath = explicit_ || MathDetector.IsMathQuery(query.Search);
            PluginLogger.Debug($"Delayed Query: actionKeyword='{query.ActionKeyword}' search='{query.Search}' explicit={explicit_} isMath={isMath}");

            if (!isMath)
                return new List<Result>();

            try
            {
                PluginLogger.Info($"Sending query to Wolfram Alpha: {query.Search}");
                var resultStr = _wolframClient.QueryAsync(query.Search, CancellationToken.None).GetAwaiter().GetResult();
                PluginLogger.Info($"Wolfram Alpha response: {resultStr}");

                return new List<Result>
                {
                    new Result
                    {
                        Title = resultStr,
                        SubTitle = "QuickMaths via Wolfram Alpha",
                        IcoPath = _iconPath,
                        Action = e =>
                        {
                            System.Windows.Clipboard.SetText(resultStr);
                            PluginLogger.Debug("Result copied to clipboard.");
                            return true;
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                PluginLogger.Error($"Delayed Query threw an exception for search={query.Search}", ex);
                return new List<Result>();
            }
        }

        public List<ContextMenuResult> LoadContextMenus(Result selectedResult) => new List<ContextMenuResult>();

        public void Dispose()
        {
            PluginLogger.Info("Dispose called.");
            if (_context != null)
                _context.API.ThemeChanged -= OnThemeChanged;
            _wolframClient?.Dispose();
        }

        public IEnumerable<PluginAdditionalOption> AdditionalOptions => new List<PluginAdditionalOption>
        {
            new PluginAdditionalOption
            {
                Key = SettingAppId,
                DisplayLabel = "Wolfram Alpha App ID",
                DisplayDescription = "Get a Short Answer API key from developer.wolframalpha.com",
                // The AdditionalOptionType enum was reordered between the 0.85.1 NuGet package
                // we compile against and the 0.99 runtime. Cast to the raw int values that
                // 0.99 actually expects: Textbox=3, Numberbox=2.
                // 0.99 runtime enum: Textbox=2, Numberbox=3.
                // TextValue must be non-null so it appears in the serialised JSON —
                // the UI skips rendering the field entirely if the property is absent.
                PluginOptionType = (PluginAdditionalOption.AdditionalOptionType)2,
                TextValue = string.Empty,
                PlaceholderText = "e.g. XXXXXX-XXXXXXXXXX",
            },
        };
    }
}
