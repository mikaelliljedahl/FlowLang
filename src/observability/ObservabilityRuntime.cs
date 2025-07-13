using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.Json;

namespace FlowLang.Observability
{
    /// <summary>
    /// FlowLang Built-in Observability Runtime
    /// Provides automatic metrics, tracing, and logging for FlowLang applications
    /// </summary>
    public class ObservabilityRuntime
    {
        private readonly IMetricsCollector _metricsCollector;
        private readonly ITraceCollector _traceCollector;
        private readonly ILogCollector _logCollector;
        private readonly ObservabilityConfig _config;

        public ObservabilityRuntime(
            IMetricsCollector metricsCollector,
            ITraceCollector traceCollector,
            ILogCollector logCollector,
            ObservabilityConfig config)
        {
            _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
            _traceCollector = traceCollector ?? throw new ArgumentNullException(nameof(traceCollector));
            _logCollector = logCollector ?? throw new ArgumentNullException(nameof(logCollector));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Instrument a FlowLang function with automatic observability
        /// </summary>
        public async Task<T> InstrumentFunctionAsync<T>(
            string functionName,
            List<string> effects,
            Func<Task<T>> functionCall,
            Dictionary<string, object>? parameters = null,
            string? traceId = null)
        {
            var operationId = Guid.NewGuid().ToString();
            var parentTraceId = traceId ?? Guid.NewGuid().ToString();
            var startTime = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();

            // Start trace span
            using var span = _traceCollector.StartSpan(functionName, parentTraceId, operationId);
            
            try
            {
                // Log function entry
                _logCollector.LogFunctionEntry(functionName, operationId, parameters);

                // Record function invocation metric
                _metricsCollector.RecordFunctionInvocation(functionName, effects);

                // Add trace attributes
                span.SetAttribute("function.name", functionName);
                span.SetAttribute("function.effects", string.Join(",", effects));
                span.SetAttribute("operation.id", operationId);
                
                if (parameters != null)
                {
                    span.SetAttribute("function.parameters", JsonSerializer.Serialize(parameters));
                }

                // Execute the function
                var result = await functionCall();

                stopwatch.Stop();

                // Record success metrics
                _metricsCollector.RecordFunctionDuration(functionName, stopwatch.ElapsedMilliseconds);
                _metricsCollector.RecordFunctionSuccess(functionName);

                // Log function completion
                _logCollector.LogFunctionSuccess(functionName, operationId, stopwatch.ElapsedMilliseconds);

                // Set span status
                span.SetStatus(SpanStatus.Ok);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Record error metrics
                _metricsCollector.RecordFunctionError(functionName, ex.GetType().Name);

                // Log function error
                _logCollector.LogFunctionError(functionName, operationId, ex, stopwatch.ElapsedMilliseconds);

                // Set span status
                span.SetStatus(SpanStatus.Error, ex.Message);
                span.SetAttribute("error.type", ex.GetType().Name);
                span.SetAttribute("error.message", ex.Message);

                throw;
            }
        }

        /// <summary>
        /// Record custom metric from FlowLang code
        /// </summary>
        public void RecordMetric(string metricName, double value, Dictionary<string, string>? tags = null)
        {
            _metricsCollector.RecordCustomMetric(metricName, value, tags);
        }

        /// <summary>
        /// Log custom event from FlowLang code
        /// </summary>
        public void LogEvent(LogLevel level, string message, Dictionary<string, object>? context = null)
        {
            _logCollector.LogCustomEvent(level, message, context);
        }

        /// <summary>
        /// Start custom trace span from FlowLang code
        /// </summary>
        public ITraceSpan StartCustomSpan(string spanName, string? parentTraceId = null)
        {
            return _traceCollector.StartSpan(spanName, parentTraceId);
        }

        /// <summary>
        /// Record effect usage for analysis
        /// </summary>
        public void RecordEffectUsage(string functionName, string effect, string operation)
        {
            _metricsCollector.RecordEffectUsage(functionName, effect, operation);
            _logCollector.LogEffectUsage(functionName, effect, operation);
        }

        /// <summary>
        /// Get observability summary for a function
        /// </summary>
        public ObservabilitySummary GetFunctionSummary(string functionName, TimeSpan timeWindow)
        {
            return new ObservabilitySummary
            {
                FunctionName = functionName,
                TimeWindow = timeWindow,
                TotalInvocations = _metricsCollector.GetInvocationCount(functionName, timeWindow),
                SuccessRate = _metricsCollector.GetSuccessRate(functionName, timeWindow),
                AverageDuration = _metricsCollector.GetAverageDuration(functionName, timeWindow),
                ErrorCount = _metricsCollector.GetErrorCount(functionName, timeWindow),
                EffectUsage = _metricsCollector.GetEffectUsage(functionName, timeWindow)
            };
        }
    }

    /// <summary>
    /// Interface for metrics collection
    /// </summary>
    public interface IMetricsCollector
    {
        void RecordFunctionInvocation(string functionName, List<string> effects);
        void RecordFunctionDuration(string functionName, long durationMs);
        void RecordFunctionSuccess(string functionName);
        void RecordFunctionError(string functionName, string errorType);
        void RecordCustomMetric(string metricName, double value, Dictionary<string, string>? tags);
        void RecordEffectUsage(string functionName, string effect, string operation);
        
        // Query methods for summaries
        long GetInvocationCount(string functionName, TimeSpan timeWindow);
        double GetSuccessRate(string functionName, TimeSpan timeWindow);
        double GetAverageDuration(string functionName, TimeSpan timeWindow);
        long GetErrorCount(string functionName, TimeSpan timeWindow);
        Dictionary<string, long> GetEffectUsage(string functionName, TimeSpan timeWindow);
    }

    /// <summary>
    /// Interface for distributed tracing
    /// </summary>
    public interface ITraceCollector
    {
        ITraceSpan StartSpan(string spanName, string? parentTraceId = null, string? operationId = null);
    }

    /// <summary>
    /// Interface for trace spans
    /// </summary>
    public interface ITraceSpan : IDisposable
    {
        void SetAttribute(string key, string value);
        void SetStatus(SpanStatus status, string? description = null);
    }

    /// <summary>
    /// Interface for logging
    /// </summary>
    public interface ILogCollector
    {
        void LogFunctionEntry(string functionName, string operationId, Dictionary<string, object>? parameters);
        void LogFunctionSuccess(string functionName, string operationId, long durationMs);
        void LogFunctionError(string functionName, string operationId, Exception ex, long durationMs);
        void LogCustomEvent(LogLevel level, string message, Dictionary<string, object>? context);
        void LogEffectUsage(string functionName, string effect, string operation);
    }

    /// <summary>
    /// Observability configuration
    /// </summary>
    public class ObservabilityConfig
    {
        public bool EnableMetrics { get; set; } = true;
        public bool EnableTracing { get; set; } = true;
        public bool EnableLogging { get; set; } = true;
        public bool EnableEffectTracking { get; set; } = true;
        public TimeSpan SamplingRate { get; set; } = TimeSpan.FromMilliseconds(100);
        public List<string> ExcludedFunctions { get; set; } = new();
        public Dictionary<string, string> GlobalTags { get; set; } = new();
    }

    /// <summary>
    /// Observability summary for functions
    /// </summary>
    public class ObservabilitySummary
    {
        public string FunctionName { get; set; } = "";
        public TimeSpan TimeWindow { get; set; }
        public long TotalInvocations { get; set; }
        public double SuccessRate { get; set; }
        public double AverageDuration { get; set; }
        public long ErrorCount { get; set; }
        public Dictionary<string, long> EffectUsage { get; set; } = new();
    }

    public enum SpanStatus
    {
        Ok,
        Error,
        Cancelled
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }
}