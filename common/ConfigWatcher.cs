using System;
using System.IO;
using System.Threading;
using BepInEx;
using BepInEx.Configuration;

namespace Mimir.Common;

/// <summary>
///     Reloads the settings file when it changes on disk, so an edit made while the game runs applies without a
///     restart, the way a change in a configuration manager does. The reload waits for a quiet second after the
///     last change, since editors write a file in several steps.
/// </summary>
internal sealed class ConfigWatcher : IDisposable
{
    private static readonly TimeSpan Quiet = TimeSpan.FromSeconds(1);

    private readonly ConfigFile _config;
    private FileSystemWatcher _watcher;

    // UTC ticks after which to reload; 0 when none is pending.
    private long _due;

    internal ConfigWatcher(ConfigFile config)
    {
        _config = config;
        string directory = Path.GetDirectoryName(config.ConfigFilePath);

        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            return;
        }

        _watcher = new FileSystemWatcher(directory, Path.GetFileName(config.ConfigFilePath))
        {
            IncludeSubdirectories = false,
            SynchronizingObject = ThreadingHelper.SynchronizingObject,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnChanged;
        _watcher.Created += OnChanged;
        _watcher.Renamed += OnRenamed;
    }

    /// <summary>Reloads once the quiet time has passed; called every frame.</summary>
    internal void Tick()
    {
        long due = Interlocked.Read(ref _due);

        if (due == 0 || DateTime.UtcNow.Ticks < due || Interlocked.CompareExchange(ref _due, 0, due) != due || !File.Exists(_config.ConfigFilePath))
        {
            return;
        }

        try
        {
            _config.Reload();
            Log.LogInfo("Reloaded the settings file.", Log.InfoLevel.High);
        }
        catch (Exception e)
        {
            Log.LogError($"The settings file could not be reloaded: {e.Message}");
        }
    }

    public void Dispose()
    {
        if (_watcher == null)
        {
            return;
        }

        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnChanged;
        _watcher.Created -= OnChanged;
        _watcher.Renamed -= OnRenamed;
        _watcher.Dispose();
        _watcher = null;
    }

    private void OnChanged(object sender, FileSystemEventArgs e) => Queue();

    private void OnRenamed(object sender, RenamedEventArgs e) => Queue();

    private void Queue()
    {
        Interlocked.Exchange(ref _due, (DateTime.UtcNow + Quiet).Ticks);
    }
}
