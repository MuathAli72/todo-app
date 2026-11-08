# Keeps the telemetry collector (todo-otel-collector on Render) awake while we test or demo.
# Why: free Render services sleep after 15 quiet minutes, and the API's own requests can't wake the
# collector (requests from outside Render can). While the collector sleeps, the API's telemetry is lost.
# Run it before a test session; stop it with Ctrl+C when you're done. It only uses free hours while it runs.
param(
    # The collector's OTLP intake for traces: the same address and kind of request the API uses.
    [string]$Url = "https://todo-otel-collector.onrender.com/v1/traces",
    # Under Render's 15 minutes, with room for delays.
    [int]$EveryMinutes = 10
)

while ($true) {
    $timer = [Diagnostics.Stopwatch]::StartNew()
    try {
        # An empty batch: the collector accepts it (200) and has nothing to pass on to Grafana.
        # 120 s: the first round may hit a sleeping collector, and Render says waking can take about a minute.
        $response = Invoke-WebRequest $Url -Method Post -ContentType "application/x-protobuf" -Body "" -UseBasicParsing -TimeoutSec 120
        $result = $response.StatusCode
    }
    catch {
        $result = "failed: $($_.Exception.Message)"
    }
    Write-Host "$(Get-Date -f HH:mm:ss) collector: $result ($([int]$timer.Elapsed.TotalSeconds)s)"
    Start-Sleep -Seconds ($EveryMinutes * 60)
}
