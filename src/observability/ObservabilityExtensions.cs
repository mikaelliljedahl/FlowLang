using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowLang.Observability
{
    /// <summary>
    /// FlowLang extensions for built-in observability
    /// These provide language-level integration for automatic instrumentation
    /// </summary>
    public static class ObservabilityExtensions
    {
        private static ObservabilityRuntime? _runtime;

        /// <summary>
        /// Initialize the observability runtime (called by generated code)
        /// </summary>
        public static void InitializeObservabilityRuntime(
            IMetricsCollector metricsCollector,
            ITraceCollector traceCollector,
            ILogCollector logCollector,
            ObservabilityConfig? config = null)
        {
            config ??= new ObservabilityConfig();
            _runtime = new ObservabilityRuntime(metricsCollector, traceCollector, logCollector, config);
        }

        /// <summary>
        /// Instrument a FlowLang function automatically (called by generated code)
        /// </summary>
        public static async Task<T> ObserveAsync<T>(
            string functionName,
            List<string> effects,
            Func<Task<T>> functionCall,
            Dictionary<string, object>? parameters = null)
        {
            if (_runtime == null)
            {
                // If observability not initialized, just execute the function
                return await functionCall();
            }

            return await _runtime.InstrumentFunctionAsync(functionName, effects, functionCall, parameters);
        }

        /// <summary>
        /// Record custom metric from FlowLang code
        /// </summary>
        public static void RecordMetric(string metricName, double value, Dictionary<string, string>? tags = null)
        {
            _runtime?.RecordMetric(metricName, value, tags);
        }

        /// <summary>
        /// Log message from FlowLang code
        /// </summary>
        public static void LogInfo(string message, Dictionary<string, object>? context = null)
        {
            _runtime?.LogEvent(LogLevel.Info, message, context);
        }

        /// <summary>
        /// Log warning from FlowLang code
        /// </summary>
        public static void LogWarning(string message, Dictionary<string, object>? context = null)
        {
            _runtime?.LogEvent(LogLevel.Warning, message, context);
        }

        /// <summary>
        /// Log error from FlowLang code
        /// </summary>
        public static void LogError(string message, Dictionary<string, object>? context = null)
        {
            _runtime?.LogEvent(LogLevel.Error, message, context);
        }

        /// <summary>
        /// Start custom trace span from FlowLang code
        /// </summary>
        public static ITraceSpan? StartSpan(string spanName)
        {
            return _runtime?.StartCustomSpan(spanName);
        }

        /// <summary>
        /// Record effect usage (called automatically by generated code)
        /// </summary>
        public static void RecordEffectUsage(string functionName, string effect, string operation)
        {
            _runtime?.RecordEffectUsage(functionName, effect, operation);
        }

        /// <summary>
        /// Get function performance summary
        /// </summary>
        public static ObservabilitySummary? GetFunctionSummary(string functionName, int timeWindowMinutes = 60)
        {
            return _runtime?.GetFunctionSummary(functionName, TimeSpan.FromMinutes(timeWindowMinutes));
        }
    }

    /// <summary>
    /// Default in-memory metrics collector for development
    /// </summary>
    public class InMemoryMetricsCollector : IMetricsCollector
    {
        private readonly Dictionary<string, List<MetricDataPoint>> _metrics = new();
        private readonly object _lock = new object();

        public void RecordFunctionInvocation(string functionName, List<string> effects)
        {
            RecordMetric($"function.invocations.{functionName}", 1, new Dictionary<string, string>
            {
                ["effects"] = string.Join(",", effects)
            });
        }

        public void RecordFunctionDuration(string functionName, long durationMs)
        {
            RecordMetric($"function.duration.{functionName}", durationMs);
        }

        public void RecordFunctionSuccess(string functionName)
        {
            RecordMetric($"function.success.{functionName}", 1);
        }

        public void RecordFunctionError(string functionName, string errorType)
        {
            RecordMetric($"function.error.{functionName}", 1, new Dictionary<string, string>
            {
                ["error_type"] = errorType
            });
        }

        public void RecordCustomMetric(string metricName, double value, Dictionary<string, string>? tags = null)
        {
            RecordMetric(metricName, value, tags);
        }

        public void RecordEffectUsage(string functionName, string effect, string operation)
        {
            RecordMetric($"effect.usage.{effect}", 1, new Dictionary<string, string>
            {
                ["function"] = functionName,
                ["operation"] = operation
            });
        }

        private void RecordMetric(string metricName, double value, Dictionary<string, string>? tags = null)
        {
            lock (_lock)
            {
                if (!_metrics.ContainsKey(metricName))
                {
                    _metrics[metricName] = new List<MetricDataPoint>();
                }

                _metrics[metricName].Add(new MetricDataPoint
                {
                    Value = value,
                    Timestamp = DateTime.UtcNow,
                    Tags = tags ?? new Dictionary<string, string>()
                });
            }
        }

        public long GetInvocationCount(string functionName, TimeSpan timeWindow)
        {
            var key = $"function.invocations.{functionName}";
            return GetCountInWindow(key, timeWindow);
        }

        public double GetSuccessRate(string functionName, TimeSpan timeWindow)
        {
            var successKey = $"function.success.{functionName}";
            var errorKey = $"function.error.{functionName}";
            
            var successCount = GetCountInWindow(successKey, timeWindow);
            var errorCount = GetCountInWindow(errorKey, timeWindow);
            var totalCount = successCount + errorCount;

            return totalCount > 0 ? (double)successCount / totalCount : 1.0;
        }

        public double GetAverageDuration(string functionName, TimeSpan timeWindow)
        {
            var key = $"function.duration.{functionName}";
            return GetAverageInWindow(key, timeWindow);
        }

        public long GetErrorCount(string functionName, TimeSpan timeWindow)
        {
            var key = $"function.error.{functionName}";
            return GetCountInWindow(key, timeWindow);
        }

        public Dictionary<string, long> GetEffectUsage(string functionName, TimeSpan timeWindow)
        {
            var result = new Dictionary<string, long>();
            var cutoff = DateTime.UtcNow - timeWindow;

            lock (_lock)
            {
                foreach (var kvp in _metrics)
                {
                    if (kvp.Key.StartsWith("effect.usage."))
                    {
                        var effectName = kvp.Key.Substring("effect.usage.".Length);
                        var count = kvp.Value
                            .Where(dp => dp.Timestamp >= cutoff && 
                                        dp.Tags.GetValueOrDefault("function") == functionName)
                            .Count();
                        
                        if (count > 0)
                        {
                            result[effectName] = count;
                        }
                    }
                }
            }

            return result;
        }

        private long GetCountInWindow(string metricName, TimeSpan timeWindow)
        {
            var cutoff = DateTime.UtcNow - timeWindow;
            
            lock (_lock)
            {
                if (!_metrics.ContainsKey(metricName))
                    return 0;

                return _metrics[metricName]
                    .Where(dp => dp.Timestamp >= cutoff)
                    .Count();
            }
        }

        private double GetAverageInWindow(string metricName, TimeSpan timeWindow)
        {
            var cutoff = DateTime.UtcNow - timeWindow;
            
            lock (_lock)
            {
                if (!_metrics.ContainsKey(metricName))
                    return 0;

                var values = _metrics[metricName]
                    .Where(dp => dp.Timestamp >= cutoff)
                    .Select(dp => dp.Value)
                    .ToList();

                return values.Any() ? values.Average() : 0;
            }
        }

        private class MetricDataPoint
        {
            public double Value { get; set; }
            public DateTime Timestamp { get; set; }
            public Dictionary<string, string> Tags { get; set; } = new();
        }
    }

    /// <summary>
    /// Default console trace collector for development
    /// </summary>
    public class ConsoleTraceCollector : ITraceCollector
    {
        public ITraceSpan StartSpan(string spanName, string? parentTraceId = null, string? operationId = null)
        {
            return new ConsoleTraceSpan(spanName, parentTraceId, operationId);
        }

        private class ConsoleTraceSpan : ITraceSpan
        {
            private readonly string _spanName;
            private readonly string? _parentTraceId;
            private readonly string? _operationId;
            private readonly DateTime _startTime;
            private readonly Dictionary<string, string> _attributes = new();

            public ConsoleTraceSpan(string spanName, string? parentTraceId, string? operationId)
            {
                _spanName = spanName;
                _parentTraceId = parentTraceId;
                _operationId = operationId;
                _startTime = DateTime.UtcNow;
                
                Console.WriteLine($"[TRACE] Span started: {_spanName} (Parent: {_parentTraceId}, Operation: {_operationId})");
            }

            public void SetAttribute(string key, string value)
            {
                _attributes[key] = value;
            }

            public void SetStatus(SpanStatus status, string? description = null)
            {
                var duration = DateTime.UtcNow - _startTime;
                Console.WriteLine($"[TRACE] Span completed: {_spanName} - {status} in {duration.TotalMilliseconds}ms");
                
                if (!string.IsNullOrEmpty(description))
                {
                    Console.WriteLine($"[TRACE] Description: {description}");
                }

                if (_attributes.Any())
                {
                    Console.WriteLine($"[TRACE] Attributes: {string.Join(", ", _attributes.Select(kv => $"{kv.Key}={kv.Value}"))}");
                }
            }

            public void Dispose()
            {
                // Automatic span completion if not explicitly set
                var duration = DateTime.UtcNow - _startTime;
                Console.WriteLine($"[TRACE] Span disposed: {_spanName} after {duration.TotalMilliseconds}ms");
            }
        }
    }

    /// <summary>
    /// Default console log collector for development
    /// </summary>
    public class ConsoleLogCollector : ILogCollector
    {
        public void LogFunctionEntry(string functionName, string operationId, Dictionary<string, object>? parameters)
        {
            var paramStr = parameters != null ? $" with {parameters.Count} parameters" : "";
            Console.WriteLine($"[LOG] Function entry: {functionName} (Operation: {operationId}){paramStr}");
        }

        public void LogFunctionSuccess(string functionName, string operationId, long durationMs)
        {
            Console.WriteLine($"[LOG] Function success: {functionName} (Operation: {operationId}) in {durationMs}ms");
        }

        public void LogFunctionError(string functionName, string operationId, Exception ex, long durationMs)
        {
            Console.WriteLine($"[LOG] Function error: {functionName} (Operation: {operationId}) after {durationMs}ms - {ex.Message}");
        }

        public void LogCustomEvent(LogLevel level, string message, Dictionary<string, object>? context)
        {
            var contextStr = context != null ? $" Context: {string.Join(", ", context.Select(kv => $"{kv.Key}={kv.Value}"))}" : "";
            Console.WriteLine($"[LOG] {level}: {message}{contextStr}");
        }

        public void LogEffectUsage(string functionName, string effect, string operation)
        {
            Console.WriteLine($"[LOG] Effect usage: {functionName} used {effect} for {operation}");
        }
    }
}