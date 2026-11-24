using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

// Ayuda para trabajar con Claude sin tocar el PC: escribe en Logs/ los errores de compilación y de la consola,
// y atiende órdenes sencillas que llegan en Logs/cozy_remote.txt (refrescar, jugar, parar, captura).
// Va en su propio ensamblado de Editor para que funcione aunque el resto del juego no compile.
[InitializeOnLoad]
static class CozyEditorRemote
{
    static readonly string Dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
    static string F(string n) => Path.Combine(Dir, n);
    static double nextPoll;
    static DateTime lastCmd = DateTime.MinValue;

    static CozyEditorRemote()
    {
        Directory.CreateDirectory(Dir);
        CompilationPipeline.compilationStarted += _ => Write("cozy_compile.txt", $"compilando {DateTime.Now:HH:mm:ss}\n", false);
        CompilationPipeline.assemblyCompilationFinished += (asm, msgs) =>
        {
            var sb = new StringBuilder();
            foreach (var m in msgs) sb.AppendLine($"{m.type} {Path.GetFileName(asm)}: {m.file}({m.line},{m.column}): {m.message}");
            sb.AppendLine($"fin {Path.GetFileName(asm)} {DateTime.Now:HH:mm:ss}");
            Write("cozy_compile.txt", sb.ToString(), true);
        };
        Application.logMessageReceivedThreaded += (msg, stack, type) =>
        {
            if (type == LogType.Log) return;
            Write("cozy_console.txt", $"[{DateTime.Now:HH:mm:ss}] {type}: {msg}\n{(type == LogType.Exception || type == LogType.Error ? stack : "")}\n", true);
        };
        EditorApplication.update += Poll;
        Write("cozy_remote_status.txt", $"listo {DateTime.Now:HH:mm:ss} playing={EditorApplication.isPlaying}\n", false);
    }

    static void Write(string name, string text, bool append)
    {
        try { if (append) File.AppendAllText(F(name), text); else File.WriteAllText(F(name), text); } catch { }
    }

    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + 1.5;
        string path = F("cozy_remote.txt");
        if (!File.Exists(path)) return;
        var t = File.GetLastWriteTimeUtc(path);
        if (t <= lastCmd) return;
        lastCmd = t;
        string[] lines;
        try { lines = File.ReadAllLines(path); File.Delete(path); } catch { return; }
        foreach (var raw in lines)
        {
            string cmd = raw.Trim();
            if (cmd.Length == 0) continue;
            Write("cozy_remote_status.txt", $"{DateTime.Now:HH:mm:ss} orden: {cmd}\n", true);
            try { Run(cmd); } catch (Exception e) { Write("cozy_remote_status.txt", $"error: {e.Message}\n", true); }
        }
    }

    static void Run(string cmd)
    {
        var parts = cmd.Split(new[] { ' ' }, 2);
        switch (parts[0])
        {
            case "refresh": AssetDatabase.Refresh(); break;
            case "menu": EditorApplication.ExecuteMenuItem(parts[1]); break;
            case "play": if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true; break;
            case "stop": EditorApplication.isPlaying = false; break;
            case "shot": ScreenCapture.CaptureScreenshot(F(parts.Length > 1 ? parts[1] : "cozy_shot.png")); break;
            case "clear": Write("cozy_console.txt", "", false); break;
            default:
                // el resto de órdenes las atiende el juego (CozyGame) mientras está en Play
                Write("cozy_game_cmd.txt", cmd + "\n", true); break;
        }
    }
}
