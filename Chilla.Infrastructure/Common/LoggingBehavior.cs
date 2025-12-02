using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Chilla.Infrastructure.Common;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Processing Request: {Name}", requestName);
        
        var timer = Stopwatch.StartNew();
        
        try
        {
            var response = await next();
            
            timer.Stop();
            if (timer.ElapsedMilliseconds > 500)
            {
                _logger.LogWarning("Long Running Request: {Name} ({ElapsedMilliseconds} milliseconds)", requestName, timer.ElapsedMilliseconds);
            }
            
            _logger.LogInformation("Completed Request: {Name}", requestName);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request Failure: {Name}, Error: {Error}", requestName, ex.Message);
            throw;
        }
    }
}