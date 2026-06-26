namespace GHS.Web.Services;

/// <summary>
/// Retrieves system logs for display.
/// Relies on built-in ILogger infrastructure rather than custom file logging.
/// </summary>
public class LogService
{
    private readonly ILogger<LogService> _logger;

    public LogService(ILogger<LogService> logger)
    {
        _logger = logger;
    }

    public void Log(string message)
    {
        _logger.LogInformation("{Message}", message);
    }
}
