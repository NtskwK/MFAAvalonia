using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using MFAAvalonia.Views.Windows;

namespace MFAAvalonia.Utilities;

/// <summary>
/// Provides single instance functionality using a Mutex based on executable path hash
/// </summary>
public static class SingleInstance
{
    private const int WindowDisplayTimeoutSeconds = 30;
    
    private static Mutex? _mutex;
    private static string? _mutexName;

    /// <summary>
    /// Tries to acquire single instance lock based on executable path
    /// </summary>
    /// <param name="mutexName">The generated mutex name</param>
    /// <returns>True if this is the first instance, false if another instance is already running</returns>
    public static bool TryAcquire(out string mutexName)
    {
        string exePath;
        try
        {
            // Try to get the actual executable path
            exePath = Process.GetCurrentProcess().MainModule?.FileName ?? AppContext.BaseDirectory;
        }
        catch
        {
            // Fallback to base directory if MainModule is not accessible
            exePath = AppContext.BaseDirectory;
        }

        // Generate mutex name based on path hash
        // Use "Local\" prefix on Windows for session-local mutex, no prefix on Unix
        string hashPart = Sha256Hex(exePath);
        mutexName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? $"Local\\MAA_{hashPart}" 
            : $"MAA_{hashPart}";
        _mutexName = mutexName;

        try
        {
            // Try to create or open the mutex
            // If initiallyOwned is true and mutex already exists, this will wait to acquire it
            // We use a non-blocking approach with TryOpenExisting first
            if (Mutex.TryOpenExisting(mutexName, out _mutex))
            {
                // Mutex already exists, meaning another instance is running
                // Don't acquire it, just return false
                _mutex?.Dispose();
                _mutex = null;
                return false;
            }
            
            // Mutex doesn't exist, create it with initial ownership
            _mutex = new Mutex(true, mutexName, out bool createdNew);
            return createdNew;
        }
        catch
        {
            // If there's an error, assume we can't determine instance status
            // Allow the app to run (fail-open) rather than fail-closed
            return true;
        }
    }

    /// <summary>
    /// Releases the single instance lock
    /// </summary>
    public static void Release()
    {
        try
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            _mutex = null;
        }
        catch
        {
            // Ignore errors during release
        }
    }

    /// <summary>
    /// Shows a message indicating that another instance is already running
    /// Uses Avalonia window if possible, falls back to console output
    /// </summary>
    public static void ShowAlreadyRunningMessage()
    {
        // Get localized strings
        string title = Assets.Localization.Strings.AlreadyRunningTitle;
        string message = Assets.Localization.Strings.AlreadyRunningMessage;
        
        try
        {
            // Try to show Avalonia window
            ShowAvaloniaWindow(message);
        }
        catch (Exception ex)
        {
            // Fallback to console output in headless/CLI environments
            Console.WriteLine("===================================");
            Console.WriteLine(title);
            Console.WriteLine("===================================");
            Console.WriteLine(message);
            Console.WriteLine($"\nMutex Name: {_mutexName}");
            Console.WriteLine($"Error showing GUI: {ex.Message}");
            Console.WriteLine("===================================");
        }
    }

    private static void ShowAvaloniaWindow(string message)
    {
        // Initialize and run a minimal Avalonia application just to show the dialog
        var builder = Program.BuildAvaloniaApp();
        
        // Use a custom lifetime that will show the dialog and then exit
        var lifetime = new ClassicDesktopStyleApplicationLifetime
        {
            ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose
        };
        
        builder.SetupWithLifetime(lifetime);
        
        // Set the main window to our dialog
        var window = new AlreadyRunningWindow
        {
            MessageText = message
        };
        
        lifetime.MainWindow = window;
        
        // Start the application - this will block until the window is closed
        // The window closing will trigger app shutdown due to OnMainWindowClose
        lifetime.Start(Array.Empty<string>());
    }

    /// <summary>
    /// Generates SHA256 hex string from input text
    /// </summary>
    private static string Sha256Hex(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
