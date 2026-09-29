using System.Diagnostics;
using System.Net;
using System.Text;

namespace Hosco.IntegrationTests;

public sealed class ApiFixture : IAsyncLifetime
{
    private Process? _process;
    private readonly StringBuilder _output = new();
    public HttpClient Client { get; private set; } = null!;
    public string ProcessOutput => _output.ToString();

    public async Task InitializeAsync()
    {
        var root = FindRepositoryRoot();
        var apiDll = Path.Combine(root, "src", "Hosco.Api", "bin", "Debug", "net10.0", "Hosco.Api.dll");
        if (!File.Exists(apiDll)) throw new FileNotFoundException("Build Hosco.slnx before running integration tests.", apiDll);
        var port = Random.Shared.Next(52000, 59000);
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(apiDll)!, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(apiDll);
        start.ArgumentList.Add("--Database:Provider=Sqlite");
        start.ArgumentList.Add("--Seed:Enabled=true");
        start.ArgumentList.Add("--Swagger:Enabled=true");
        start.ArgumentList.Add("--Logging:LogLevel:Default=Warning");
        start.ArgumentList.Add($"--urls=http://127.0.0.1:{port}");
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Testing";
        _process = Process.Start(start) ?? throw new InvalidOperationException("Could not start HOSCO API process.");
        _process.OutputDataReceived += (_, e) => { if (e.Data is not null) _output.AppendLine(e.Data); };
        _process.ErrorDataReceived += (_, e) => { if (e.Data is not null) _output.AppendLine(e.Data); };
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
        Client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}"), Timeout = TimeSpan.FromSeconds(15) };
        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (_process.HasExited) throw new InvalidOperationException($"HOSCO API exited during startup with code {_process.ExitCode}:{Environment.NewLine}{_output}");
            try { if ((await Client.GetAsync("/health/ready")).StatusCode == HttpStatusCode.OK) return; }
            catch (HttpRequestException) { }
            await Task.Delay(100);
        }
        throw new TimeoutException("HOSCO API did not become ready within 30 seconds.");
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        if (_process is { HasExited: false })
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync();
        }
        _process?.Dispose();
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Hosco.slnx"))) return current.FullName;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate Hosco.slnx from test output.");
    }
}

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>;
