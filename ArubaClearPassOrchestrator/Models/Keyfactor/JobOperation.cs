// Copyright 2025 Keyfactor
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;

namespace ArubaClearPassOrchestrator.Models.Keyfactor;

public class JobOperation<T>
{
    private JobOperation() { }
    private JobOperation(T value)
    {
        Value = value;
    }

    private JobOperation(JobResult jobResult)
    {
        JobResult = jobResult;
    }
    
    public T Value { get; init; }
    public JobResult JobResult { get; init; }
    public bool IsSuccessful => Value != null || JobResult?.Result == OrchestratorJobStatusJobResult.Success;
    
    public static JobOperation<T> Success(T value)
    {
        return new JobOperation<T>(value);
    }
    
    public static JobOperation<T> SuccessResult(string message, long? jobHistoryId = null)
    {
        var jobResult = new JobResult
        {
            Result = OrchestratorJobStatusJobResult.Success,
            FailureMessage = message,
            JobHistoryId = jobHistoryId ?? 0,
        };
        return new JobOperation<T>(jobResult);
    }

    public static JobOperation<T> Fail(string message, long? jobHistoryId = null)
    {
        var jobResult = new JobResult
        {
            Result = OrchestratorJobStatusJobResult.Failure,
            FailureMessage = message,
            JobHistoryId = jobHistoryId ?? 0,
        };
        return new JobOperation<T>(jobResult);
    }
}

public class JobOperation
{
    private JobOperation() {}
    private JobOperation(JobResult jobResult)
    {
        JobResult = jobResult;
    }
    public JobResult JobResult { get; init; }
    public bool IsSuccessful => JobResult?.Result == OrchestratorJobStatusJobResult.Success;
    
    public static JobOperation Success(string message = null,  long? jobHistoryId = null)
    {
        var jobResult = new JobResult
        {
            Result = OrchestratorJobStatusJobResult.Success,
            FailureMessage = message,
            JobHistoryId = jobHistoryId ?? 0,
        };
        return new JobOperation(jobResult);
    }
    
    public static JobOperation Fail(string message, long? jobHistoryId = null)
    {
        var jobResult = new JobResult
        {
            Result = OrchestratorJobStatusJobResult.Failure,
            FailureMessage = message,
            JobHistoryId = jobHistoryId ?? 0,
        };
        return new JobOperation(jobResult);
    }
}
