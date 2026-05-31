using System.Diagnostics;

namespace ServiceFlow.IntegrationTests.Persistence;

public sealed class DockerAvailableFactAttribute : FactAttribute
{
    public DockerAvailableFactAttribute()
    {
        if (!IsDockerAvailable())
        {
            Skip = "Requires a running Docker daemon for PostgreSQL Testcontainers.";
        }
    }

    private static bool IsDockerAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                ArgumentList = { "info", "--format", "{{.ServerVersion}}" },
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            return process is not null
                && process.WaitForExit(milliseconds: 3_000)
                && process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
