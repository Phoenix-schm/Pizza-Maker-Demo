using Godot;
using System;

namespace Common;

public enum LogLevel { DEBUG, INFO, WARNING, ERROR }

/// <summary>
/// Stores a more descriptive version of Print statement due to how vague they are.
/// </summary>
public static class GameLogger
{
    public static void Log(LogLevel level = LogLevel.DEBUG, params object[] message)
    {
        var dateTime = DateTime.Now;
        string timeStamp = $"[{dateTime:yyyy-MMM-dd HH:mm:ss}]";
        var callingMethod = new System.Diagnostics.StackTrace().GetFrame(1).GetMethod();
        string logMessage = $"{timeStamp} [{level}]: [{callingMethod.DeclaringType.Name}] [{callingMethod.Name}]: ";

        string logColor = "WHITE";
        switch (level)
        {
            case LogLevel.DEBUG:
                logColor = "WHITE";
                break;
            case LogLevel.INFO:
                logColor = "CYAN";
                break;
            case LogLevel.WARNING:
                logColor = "YELLOW";
                break;
            case LogLevel.ERROR:
                logColor = "RED";
                break;
            default:
                break;
        }

        GD.PrintRich([$"[color={logColor}]{logMessage}[/color]", .. message]);
    }

    public static void Debug(params object[] message)
    {
        Log(LogLevel.DEBUG, message);
    }

    public static void Info(params object[] message)
    {
        Log(LogLevel.INFO, message);
    }
    public static void Warning(params object[] message)
    {
        Log(LogLevel.WARNING, message);
    }
    public static void Error(params object[] message)
    {
        Log(LogLevel.ERROR, message);
    }
}
