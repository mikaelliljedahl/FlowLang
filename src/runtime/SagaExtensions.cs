using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowLang.Runtime
{
    /// <summary>
    /// FlowLang extensions for saga pattern support
    /// These provide the language-level integration for saga/compensation patterns
    /// </summary>
    public static class SagaExtensions
    {
        private static SagaRuntime? _runtime;

        /// <summary>
        /// Initialize the saga runtime (called by generated code)
        /// </summary>
        public static void InitializeSagaRuntime(ISagaStorage storage, ISagaLogger logger)
        {
            _runtime = new SagaRuntime(storage, logger);
        }

        /// <summary>
        /// Register a FlowLang function as a saga step
        /// </summary>
        public static void RegisterSagaStep<TInput, TOutput>(
            string stepName, 
            Func<TInput, Task<Result<TOutput, string>>> executeFunc,
            Func<TInput, TOutput?, Task<Result<string, string>>> compensateFunc)
        {
            if (_runtime == null)
            {
                throw new InvalidOperationException("Saga runtime not initialized");
            }

            var step = new FlowLangSagaStep<TInput, TOutput>(executeFunc, compensateFunc);
            _runtime.RegisterStep(stepName, step);
        }

        /// <summary>
        /// Execute a saga from FlowLang code
        /// </summary>
        public static async Task<Result<T, string>> ExecuteSagaAsync<T>(
            string sagaName, 
            params (string stepName, Dictionary<string, object> parameters)[] steps)
        {
            if (_runtime == null)
            {
                throw new InvalidOperationException("Saga runtime not initialized");
            }

            var sagaDefinition = new SagaDefinition
            {
                Name = sagaName,
                Steps = steps.Select(s => new SagaStepDefinition
                {
                    Name = s.stepName,
                    Parameters = s.parameters
                }).ToList()
            };

            try
            {
                var result = await _runtime.ExecuteSagaAsync<T>(sagaDefinition);
                
                if (result.IsSuccess)
                {
                    return Result<T, string>.Ok(result.Value!);
                }
                else
                {
                    return Result<T, string>.Error(result.Error ?? "Saga execution failed");
                }
            }
            catch (Exception ex)
            {
                return Result<T, string>.Error($"Saga execution exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Get saga execution status
        /// </summary>
        public static async Task<Result<SagaStatusInfo, string>> GetSagaStatusAsync(string sagaId)
        {
            if (_runtime == null)
            {
                return Result<SagaStatusInfo, string>.Error("Saga runtime not initialized");
            }

            try
            {
                var state = await _runtime.GetSagaStateAsync(sagaId);
                if (state == null)
                {
                    return Result<SagaStatusInfo, string>.Error($"Saga {sagaId} not found");
                }

                return Result<SagaStatusInfo, string>.Ok(new SagaStatusInfo
                {
                    Id = state.Id,
                    Name = state.Name,
                    Status = state.Status.ToString(),
                    StartedAt = state.StartedAt,
                    CompletedAt = state.CompletedAt,
                    Error = state.Error,
                    StepCount = state.Steps.Count,
                    CompletedSteps = state.Steps.Count(s => s.Status == StepStatus.Completed),
                    FailedSteps = state.Steps.Count(s => s.Status == StepStatus.Failed)
                });
            }
            catch (Exception ex)
            {
                return Result<SagaStatusInfo, string>.Error($"Error getting saga status: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// FlowLang saga step implementation
    /// </summary>
    internal class FlowLangSagaStep<TInput, TOutput> : ISagaStep
    {
        private readonly Func<TInput, Task<Result<TOutput, string>>> _executeFunc;
        private readonly Func<TInput, TOutput?, Task<Result<string, string>>> _compensateFunc;

        public FlowLangSagaStep(
            Func<TInput, Task<Result<TOutput, string>>> executeFunc,
            Func<TInput, TOutput?, Task<Result<string, string>>> compensateFunc)
        {
            _executeFunc = executeFunc;
            _compensateFunc = compensateFunc;
        }

        public async Task<object> ExecuteAsync(Dictionary<string, object> parameters)
        {
            // Convert parameters to TInput
            var input = ConvertParameters<TInput>(parameters);
            
            var result = await _executeFunc(input);
            
            if (result.IsError)
            {
                throw new SagaStepException($"Step execution failed: {result.Error}");
            }

            return result.Value!;
        }

        public async Task CompensateAsync(Dictionary<string, object> parameters, object? result)
        {
            // Convert parameters and result
            var input = ConvertParameters<TInput>(parameters);
            var output = result != null ? (TOutput)result : default;

            var compensationResult = await _compensateFunc(input, output);
            
            if (compensationResult.IsError)
            {
                throw new SagaCompensationException($"Compensation failed: {compensationResult.Error}");
            }
        }

        private T ConvertParameters<T>(Dictionary<string, object> parameters)
        {
            // Simple parameter conversion - in a real implementation this would be more sophisticated
            if (typeof(T) == typeof(Dictionary<string, object>))
            {
                return (T)(object)parameters;
            }

            // For now, assume single parameter scenarios
            if (parameters.Count == 1)
            {
                var value = parameters.Values.First();
                if (value is T directValue)
                {
                    return directValue;
                }
            }

            throw new SagaException($"Cannot convert parameters to type {typeof(T).Name}");
        }
    }

    /// <summary>
    /// Saga status information for FlowLang consumption
    /// </summary>
    public class SagaStatusInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Error { get; set; }
        public int StepCount { get; set; }
        public int CompletedSteps { get; set; }
        public int FailedSteps { get; set; }
    }

    /// <summary>
    /// Default in-memory saga storage for development
    /// </summary>
    public class InMemorySagaStorage : ISagaStorage
    {
        private readonly Dictionary<string, SagaState> _sagas = new();

        public Task SaveSagaStateAsync(string sagaId, SagaState state)
        {
            _sagas[sagaId] = state;
            return Task.CompletedTask;
        }

        public Task<SagaState?> GetSagaStateAsync(string sagaId)
        {
            _sagas.TryGetValue(sagaId, out var state);
            return Task.FromResult(state);
        }

        public Task UpdateSagaStatusAsync(string sagaId, SagaStatus status)
        {
            if (_sagas.TryGetValue(sagaId, out var state))
            {
                state.Status = status;
                if (status == SagaStatus.Completed || status == SagaStatus.Failed || status == SagaStatus.Compensated)
                {
                    state.CompletedAt = DateTime.UtcNow;
                }
            }
            return Task.CompletedTask;
        }

        public Task UpdateStepStatusAsync(string sagaId, string stepName, StepStatus status)
        {
            if (_sagas.TryGetValue(sagaId, out var state))
            {
                var step = state.Steps.FirstOrDefault(s => s.Name == stepName);
                if (step != null)
                {
                    step.Status = status;
                    if (status == StepStatus.Running)
                    {
                        step.StartedAt = DateTime.UtcNow;
                    }
                    else if (status != StepStatus.Pending)
                    {
                        step.CompletedAt = DateTime.UtcNow;
                    }
                }
            }
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Default console saga logger for development
    /// </summary>
    public class ConsoleSagaLogger : ISagaLogger
    {
        public void LogSagaStarted(string sagaId, string sagaName)
        {
            Console.WriteLine($"[SAGA] Started: {sagaName} (ID: {sagaId})");
        }

        public void LogSagaCompleted(string sagaId)
        {
            Console.WriteLine($"[SAGA] Completed: {sagaId}");
        }

        public void LogSagaFailed(string sagaId, Exception ex)
        {
            Console.WriteLine($"[SAGA] Failed: {sagaId} - {ex.Message}");
        }

        public void LogStepStarted(string sagaId, string stepName)
        {
            Console.WriteLine($"[SAGA] Step started: {stepName} (Saga: {sagaId})");
        }

        public void LogStepCompleted(string sagaId, string stepName)
        {
            Console.WriteLine($"[SAGA] Step completed: {stepName} (Saga: {sagaId})");
        }

        public void LogStepFailed(string sagaId, string stepName, Exception ex)
        {
            Console.WriteLine($"[SAGA] Step failed: {stepName} (Saga: {sagaId}) - {ex.Message}");
        }

        public void LogCompensationStarted(string sagaId)
        {
            Console.WriteLine($"[SAGA] Compensation started: {sagaId}");
        }

        public void LogCompensationCompleted(string sagaId)
        {
            Console.WriteLine($"[SAGA] Compensation completed: {sagaId}");
        }

        public void LogCompensationStepStarted(string sagaId, string stepName)
        {
            Console.WriteLine($"[SAGA] Compensating step: {stepName} (Saga: {sagaId})");
        }

        public void LogCompensationStepCompleted(string sagaId, string stepName)
        {
            Console.WriteLine($"[SAGA] Step compensation completed: {stepName} (Saga: {sagaId})");
        }

        public void LogCompensationStepFailed(string sagaId, string stepName, Exception ex)
        {
            Console.WriteLine($"[SAGA] Step compensation failed: {stepName} (Saga: {sagaId}) - {ex.Message}");
        }
    }

    public class SagaStepException : Exception
    {
        public SagaStepException(string message) : base(message) { }
        public SagaStepException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class SagaCompensationException : Exception
    {
        public SagaCompensationException(string message) : base(message) { }
        public SagaCompensationException(string message, Exception innerException) : base(message, innerException) { }
    }
}