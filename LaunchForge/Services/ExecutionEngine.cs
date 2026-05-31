using System.Diagnostics;
using System.IO;
using LaunchForge.Models.Steps;

namespace LaunchForge.Services;

public enum ExecutionProgressKind { StepStarted, StepCompleted, StepSkipped, Error, Finished }

public record ExecutionProgress(
    int                    StepIndex,
    ExecutionProgressKind  Kind,
    string                 Message);

public class ExecutionEngine
{
    public async Task RunAsync(
        IReadOnlyList<StepBase>    steps,
        IProgress<ExecutionProgress> progress,
        CancellationToken          ct)
    {
        for (int i = 0; i < steps.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var step = steps[i];

            if (!step.IsEnabled)
            {
                progress.Report(new(i, ExecutionProgressKind.StepSkipped, "Skipped (disabled)"));
                continue;
            }

            var label = string.IsNullOrWhiteSpace(step.Label)
                ? step.GetType().Name
                : step.Label;

            progress.Report(new(i, ExecutionProgressKind.StepStarted, label));

            try
            {
                await ExecuteStepAsync(step, ct);
                progress.Report(new(i, ExecutionProgressKind.StepCompleted, label));
            }
            catch (OperationCanceledException)
            {
                progress.Report(new(i, ExecutionProgressKind.Finished, "Cancelled"));
                return;
            }
            catch (Exception ex)
            {
                progress.Report(new(i, ExecutionProgressKind.Error,
                    $"Step {i + 1} failed: {ex.Message}"));
                return;
            }
        }

        progress.Report(new(-1, ExecutionProgressKind.Finished, "All steps completed."));
    }

    private Task ExecuteStepAsync(StepBase step, CancellationToken ct) => step switch
    {
        LaunchAppStep            s => ExecuteLaunchAppAsync(s, ct),
        LaunchSteamStep          s => ExecuteLaunchSteamAsync(s),
        WaitStep                 s => ExecuteWaitAsync(s, ct),
        KillProcessStep          s => ExecuteKillProcessAsync(s),
        WaitForProcessStartStep  s => ExecuteWaitForProcessStartAsync(s, ct),
        WaitForProcessExitStep   s => ExecuteWaitForProcessExitAsync(s, ct),
        RunPowerShellStep        s => ExecuteRunPowerShellAsync(s, ct),
        _                          => throw new NotSupportedException(step.GetType().Name),
    };

    private static async Task ExecuteLaunchAppAsync(LaunchAppStep step, CancellationToken ct)
    {
        var info = new ProcessStartInfo
        {
            FileName        = step.ExePath,
            Arguments       = step.Arguments,
            UseShellExecute = true,
        };
        var proc = Process.Start(info)
            ?? throw new InvalidOperationException($"Could not start: {step.ExePath}");

        if (step.WaitForExit)
            await proc.WaitForExitAsync(ct);
    }

    private static Task ExecuteLaunchSteamAsync(LaunchSteamStep step)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName        = $"steam://rungameid/{step.AppId}",
            UseShellExecute = true,
        });
        return Task.CompletedTask;
    }

    private static Task ExecuteWaitAsync(WaitStep step, CancellationToken ct) =>
        Task.Delay(TimeSpan.FromSeconds(step.Seconds), ct);

    private static Task ExecuteKillProcessAsync(KillProcessStep step)
    {
        foreach (var p in Process.GetProcessesByName(step.ProcessName))
        {
            try { p.Kill(); }
            catch { /* access denied, skip */ }
        }
        return Task.CompletedTask;
    }

    private static async Task ExecuteWaitForProcessStartAsync(
        WaitForProcessStartStep step, CancellationToken ct)
    {
        var deadline = step.TimeoutSeconds > 0
            ? DateTime.UtcNow.AddSeconds(step.TimeoutSeconds)
            : DateTime.MaxValue;

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            if (Process.GetProcessesByName(step.ProcessName).Length > 0) return;
            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException(
                    $"Process '{step.ProcessName}' did not start within {step.TimeoutSeconds}s.");
            await Task.Delay(500, ct);
        }
    }

    private static async Task ExecuteWaitForProcessExitAsync(
        WaitForProcessExitStep step, CancellationToken ct)
    {
        var deadline = step.TimeoutSeconds > 0
            ? DateTime.UtcNow.AddSeconds(step.TimeoutSeconds)
            : DateTime.MaxValue;

        while (Process.GetProcessesByName(step.ProcessName).Length > 0)
        {
            ct.ThrowIfCancellationRequested();
            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException(
                    $"Process '{step.ProcessName}' did not exit within {step.TimeoutSeconds}s.");
            await Task.Delay(500, ct);
        }
    }

    private static async Task ExecuteRunPowerShellAsync(RunPowerShellStep step, CancellationToken ct)
    {
        var tmpFile = Path.Combine(Path.GetTempPath(), $"lf_ps_{Guid.NewGuid():N}.ps1");
        try
        {
            await File.WriteAllTextAsync(tmpFile, step.Script, ct);

            var info = new ProcessStartInfo
            {
                FileName        = "powershell.exe",
                Arguments       = $"-ExecutionPolicy Bypass -File \"{tmpFile}\"",
                UseShellExecute = false,
            };
            var proc = Process.Start(info)
                ?? throw new InvalidOperationException("Could not start powershell.exe");

            if (step.WaitForExit)
                await proc.WaitForExitAsync(ct);
        }
        finally
        {
            try { File.Delete(tmpFile); }
            catch { /* best-effort cleanup */ }
        }
    }
}
