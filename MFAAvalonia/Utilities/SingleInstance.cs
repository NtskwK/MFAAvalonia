using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using MFAAvalonia.Views.Windows;

namespace MFAAvalonia.Utilities;

/// <summary>
/// Provides single instance functionality using a Mutex based on executable path hash
/// </summary>
public static class SingleInstance
{
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
        mutexName = "MAA_" + Sha256Hex(exePath);
        _mutexName = mutexName;

        try
        {
            _mutex = new Mutex(true, mutexName, out bool createdNew);
            return createdNew;
        }
        catch
        {
            return false;
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
        const string message = "同一路径下只能启动一个实例!\n\n如需多开 MAA，请复制一份新的 MAA 到其他文件夹下，并设置使用不同的 MAA、相同的 adb 和不同的模拟器地址进行多开操作。";

        try
        {
            // Try to show Avalonia window
            ShowAvaloniaWindow(message);
        }
        catch (Exception ex)
        {
            // Fallback to console output in headless/CLI environments
            Console.WriteLine("===================================");
            Console.WriteLine("MAA 单实例检测");
            Console.WriteLine("===================================");
            Console.WriteLine(message);
            Console.WriteLine($"\nMutex Name: {_mutexName}");
            Console.WriteLine($"Error showing GUI: {ex.Message}");
            Console.WriteLine("===================================");
        }
    }

    private static void ShowAvaloniaWindow(string message)
    {
        // Check if Avalonia Application is already initialized
        if (Application.Current == null)
        {
            // Initialize Avalonia application minimally
            var builder = Program.BuildAvaloniaApp();
            builder.SetupWithoutStarting();
        }

        // Show the window on UI thread and wait for it to close
        var task = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                var window = new AlreadyRunningWindow
                {
                    MessageText = message
                };
                await window.ShowDialog<bool?>(null);
            }
            catch (Exception ex)
            {
                // If window fails, throw to trigger console fallback
                throw new InvalidOperationException("Failed to show Avalonia window", ex);
            }
        });

        // Wait for window to close (with timeout to prevent hanging)
        task.Wait(TimeSpan.FromSeconds(30));
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
