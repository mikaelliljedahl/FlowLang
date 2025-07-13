using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;

namespace FlowLang.Runtime
{
    /// <summary>
    /// FlowLang Saga Runtime - Built-in distributed transaction support
    /// Provides automatic compensation patterns for FlowLang functions
    /// </summary>
    public class SagaRuntime
    {
        private readonly Dictionary<string, ISagaStep> _registeredSteps = new();
        private readonly ISagaStorage _storage;
        private readonly ISagaLogger _logger;

        public SagaRuntime(ISagaStorage storage, ISagaLogger logger)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Register a saga step with its compensation function
        /// </summary>
        public void RegisterStep(string stepName, ISagaStep step)
        {
            _registeredSteps[stepName] = step;
        }

        /// <summary>
        /// Execute a saga with automatic compensation on failure
        /// </summary>
        public async Task<SagaResult<T>> ExecuteSagaAsync<T>(SagaDefinition saga)
        {
            var sagaId = Guid.NewGuid().ToString();
            var executedSteps = new List<SagaStepExecution>();

            _logger.LogSagaStarted(sagaId, saga.Name);

            try
            {
                // Save saga state
                await _storage.SaveSagaStateAsync(sagaId, new SagaState
                {
                    Id = sagaId,
                    Name = saga.Name,
                    Status = SagaStatus.Running,
                    Steps = saga.Steps.Select(s => new SagaStepState { Name = s.Name, Status = StepStatus.Pending }).ToList(),
                    StartedAt = DateTime.UtcNow
                });

                // Execute steps sequentially
                foreach (var stepDef in saga.Steps)
                {
                    _logger.LogStepStarted(sagaId, stepDef.Name);

                    if (!_registeredSteps.TryGetValue(stepDef.Name, out var step))
                    {
                        throw new SagaException($"Step '{stepDef.Name}' not registered");
                    }

                    var stepExecution = new SagaStepExecution
                    {
                        StepName = stepDef.Name,
                        Parameters = stepDef.Parameters,
                        StartedAt = DateTime.UtcNow
                    };

                    try
                    {
                        // Execute the step
                        stepExecution.Result = await step.ExecuteAsync(stepDef.Parameters);
                        stepExecution.CompletedAt = DateTime.UtcNow;
                        stepExecution.Status = StepStatus.Completed;

                        executedSteps.Add(stepExecution);
                        _logger.LogStepCompleted(sagaId, stepDef.Name);

                        // Update saga state
                        await _storage.UpdateStepStatusAsync(sagaId, stepDef.Name, StepStatus.Completed);
                    }
                    catch (Exception ex)
                    {
                        stepExecution.Error = ex.Message;
                        stepExecution.CompletedAt = DateTime.UtcNow;
                        stepExecution.Status = StepStatus.Failed;

                        _logger.LogStepFailed(sagaId, stepDef.Name, ex);

                        // Compensation required - rollback completed steps
                        await CompensateAsync(sagaId, executedSteps);

                        await _storage.UpdateSagaStatusAsync(sagaId, SagaStatus.Compensated);

                        return SagaResult<T>.Failed(ex.Message, executedSteps);
                    }
                }

                // All steps completed successfully
                await _storage.UpdateSagaStatusAsync(sagaId, SagaStatus.Completed);
                _logger.LogSagaCompleted(sagaId);

                // Extract result from final step
                var finalResult = executedSteps.Last().Result;
                return SagaResult<T>.Success((T)finalResult, executedSteps);
            }
            catch (Exception ex)
            {
                _logger.LogSagaFailed(sagaId, ex);
                await _storage.UpdateSagaStatusAsync(sagaId, SagaStatus.Failed);
                return SagaResult<T>.Failed(ex.Message, executedSteps);
            }
        }

        /// <summary>
        /// Compensate (rollback) executed steps in reverse order
        /// </summary>
        private async Task CompensateAsync(string sagaId, List<SagaStepExecution> executedSteps)
        {
            _logger.LogCompensationStarted(sagaId);

            // Compensate in reverse order
            for (int i = executedSteps.Count - 1; i >= 0; i--)
            {
                var stepExecution = executedSteps[i];
                
                if (stepExecution.Status != StepStatus.Completed)
                    continue;

                if (!_registeredSteps.TryGetValue(stepExecution.StepName, out var step))
                    continue;

                try
                {
                    _logger.LogCompensationStepStarted(sagaId, stepExecution.StepName);
                    
                    await step.CompensateAsync(stepExecution.Parameters, stepExecution.Result);
                    
                    stepExecution.Status = StepStatus.Compensated;
                    await _storage.UpdateStepStatusAsync(sagaId, stepExecution.StepName, StepStatus.Compensated);
                    
                    _logger.LogCompensationStepCompleted(sagaId, stepExecution.StepName);
                }
                catch (Exception ex)
                {
                    _logger.LogCompensationStepFailed(sagaId, stepExecution.StepName, ex);
                    // Continue with other compensations even if one fails
                }
            }

            _logger.LogCompensationCompleted(sagaId);
        }

        /// <summary>
        /// Get saga execution status
        /// </summary>
        public async Task<SagaState?> GetSagaStateAsync(string sagaId)
        {
            return await _storage.GetSagaStateAsync(sagaId);
        }

        /// <summary>
        /// Resume a failed saga (for manual intervention scenarios)
        /// </summary>
        public async Task<SagaResult<T>> ResumeSagaAsync<T>(string sagaId)
        {
            var sagaState = await _storage.GetSagaStateAsync(sagaId);
            if (sagaState == null)
            {
                throw new SagaException($"Saga {sagaId} not found");
            }

            if (sagaState.Status != SagaStatus.Failed)
            {
                throw new SagaException($"Saga {sagaId} is not in failed state");
            }

            // Implementation for resuming saga from last successful step
            // This would reconstruct the saga definition and continue from failure point
            throw new NotImplementedException("Saga resume functionality");
        }
    }

    /// <summary>
    /// Interface for saga step implementations
    /// </summary>
    public interface ISagaStep
    {
        Task<object> ExecuteAsync(Dictionary<string, object> parameters);
        Task CompensateAsync(Dictionary<string, object> parameters, object? result);
    }

    /// <summary>
    /// Interface for saga state persistence
    /// </summary>
    public interface ISagaStorage
    {
        Task SaveSagaStateAsync(string sagaId, SagaState state);
        Task<SagaState?> GetSagaStateAsync(string sagaId);
        Task UpdateSagaStatusAsync(string sagaId, SagaStatus status);
        Task UpdateStepStatusAsync(string sagaId, string stepName, StepStatus status);
    }

    /// <summary>
    /// Interface for saga logging
    /// </summary>
    public interface ISagaLogger
    {
        void LogSagaStarted(string sagaId, string sagaName);
        void LogSagaCompleted(string sagaId);
        void LogSagaFailed(string sagaId, Exception ex);
        void LogStepStarted(string sagaId, string stepName);
        void LogStepCompleted(string sagaId, string stepName);
        void LogStepFailed(string sagaId, string stepName, Exception ex);
        void LogCompensationStarted(string sagaId);
        void LogCompensationCompleted(string sagaId);
        void LogCompensationStepStarted(string sagaId, string stepName);
        void LogCompensationStepCompleted(string sagaId, string stepName);
        void LogCompensationStepFailed(string sagaId, string stepName, Exception ex);
    }

    /// <summary>
    /// Saga definition for FlowLang
    /// </summary>
    public class SagaDefinition
    {
        public string Name { get; set; } = "";
        public List<SagaStepDefinition> Steps { get; set; } = new();
    }

    public class SagaStepDefinition
    {
        public string Name { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Saga execution state
    /// </summary>
    public class SagaState
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public SagaStatus Status { get; set; }
        public List<SagaStepState> Steps { get; set; } = new();
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Error { get; set; }
    }

    public class SagaStepState
    {
        public string Name { get; set; } = "";
        public StepStatus Status { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Error { get; set; }
    }

    public class SagaStepExecution
    {
        public string StepName { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; } = new();
        public object? Result { get; set; }
        public StepStatus Status { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Saga execution result
    /// </summary>
    public class SagaResult<T>
    {
        public bool IsSuccess { get; private set; }
        public T? Value { get; private set; }
        public string? Error { get; private set; }
        public List<SagaStepExecution> ExecutedSteps { get; private set; } = new();

        private SagaResult() { }

        public static SagaResult<T> Success(T value, List<SagaStepExecution> steps)
        {
            return new SagaResult<T>
            {
                IsSuccess = true,
                Value = value,
                ExecutedSteps = steps
            };
        }

        public static SagaResult<T> Failed(string error, List<SagaStepExecution> steps)
        {
            return new SagaResult<T>
            {
                IsSuccess = false,
                Error = error,
                ExecutedSteps = steps
            };
        }
    }

    public enum SagaStatus
    {
        Running,
        Completed,
        Failed,
        Compensated,
        Compensating
    }

    public enum StepStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Compensated
    }

    public class SagaException : Exception
    {
        public SagaException(string message) : base(message) { }
        public SagaException(string message, Exception innerException) : base(message, innerException) { }
    }
}