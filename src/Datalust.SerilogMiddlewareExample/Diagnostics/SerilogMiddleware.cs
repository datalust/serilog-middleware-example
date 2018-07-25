using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Datalust.SerilogMiddlewareExample.Diagnostics
{
    class SerilogMiddleware
    {
        const string MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

        static readonly ILogger Log = Serilog.Log.ForContext<SerilogMiddleware>();

        static readonly HashSet<string> HeaderWhitelist = new HashSet<string> {"Content-Type", "Content-Length", "User-Agent"};

        readonly RequestDelegate _next;

        public SerilogMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        // ReSharper disable once UnusedMember.Global
        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

            var start = Stopwatch.GetTimestamp();
            try
            {
                await _next(httpContext);
                var elapsedMs = GetElapsedMilliseconds(start, Stopwatch.GetTimestamp());

                var statusCode = httpContext.Response?.StatusCode;
                var level = statusCode > 499 ? LogEventLevel.Error : LogEventLevel.Information;

                var log = level == LogEventLevel.Error ? LogForErrorContext(httpContext) : Log;
                log.Write(level, MessageTemplate, httpContext.Request.Method, GetPath(httpContext), statusCode, elapsedMs);
            }
            // Never caught, because `LogException()` returns false.
            catch (Exception ex) when (LogException(httpContext, GetElapsedMilliseconds(start, Stopwatch.GetTimestamp()), ex)) { }
        }

        static bool LogException(HttpContext httpContext, double elapsedMs, Exception ex)
        {
            LogForErrorContext(httpContext)
                .Error(ex, MessageTemplate, httpContext.Request.Method, GetPath(httpContext), 500, elapsedMs);

            return false;
        }

        static ILogger LogForErrorContext(HttpContext httpContext)
        {
            var request = httpContext.Request;

            var loggedHeaders = request.Headers
                .Where(h => HeaderWhitelist.Contains(h.Key))
                .ToDictionary(h => h.Key, h => h.Value.ToString());

            var result = Log
                .ForContext("RequestHeaders", loggedHeaders, destructureObjects: true)
                .ForContext("RequestHost", request.Host)
                .ForContext("RequestProtocol", request.Protocol);

            return result;
        }

        static double GetElapsedMilliseconds(long start, long stop)
        {
            return (stop - start) * 1000 / (double)Stopwatch.Frequency;
        }
        
        static string GetPath(HttpContext httpContext)
        {
            return httpContext.Features.Get<IHttpRequestFeature>()?.RawTarget ?? httpContext.Request.Path.ToString();
        }
    }
}
