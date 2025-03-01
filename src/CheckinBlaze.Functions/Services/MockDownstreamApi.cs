using Microsoft.Identity.Abstractions;
using System.Net.Http;
using System.Security.Claims;

namespace CheckinBlaze.Functions.Services;

public class MockDownstreamApi : IDownstreamApi
{
    public Task<HttpResponseMessage> CallApiAsync(DownstreamApiOptions options, ClaimsPrincipal? user = null, HttpContent? content = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }

    public Task<HttpResponseMessage> CallApiAsync(string? serviceName = null, Action<DownstreamApiOptions>? downstreamApiOptionsOverride = null, ClaimsPrincipal? user = null, HttpContent? content = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }

    public Task<HttpResponseMessage> CallApiForAppAsync(string? serviceName = null, Action<DownstreamApiOptions>? downstreamApiOptionsOverride = null, HttpContent? content = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }

    public Task<TOutput?> CallApiForAppAsync<TInput, TOutput>(string? serviceName = null, TInput input = default!, Action<DownstreamApiOptions>? downstreamApiOptionsOverride = null, CancellationToken cancellationToken = default) where TOutput : class
    {
        return Task.FromResult<TOutput?>(null);
    }

    public Task<TOutput?> CallApiForAppAsync<TOutput>(string serviceName, Action<DownstreamApiOptions>? downstreamApiOptionsOverride = null, CancellationToken cancellationToken = default) where TOutput : class
    {
        return Task.FromResult<TOutput?>(null);
    }

    public Task<HttpResponseMessage> CallApiForUserAsync(string? serviceName = null, Action<DownstreamApiOptions>? downstreamApiOptionsOverride = null, ClaimsPrincipal? user = null, HttpContent? content = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }

    public Task<TOutput?> CallApiForUserAsync<TInput, TOutput>(string? serviceName = null, TInput input = default!, Action<DownstreamApiOptions>? downstreamApiOptionsOverride = null, ClaimsPrincipal? user = null, CancellationToken cancellationToken = default) where TOutput : class
    {
        return Task.FromResult<TOutput?>(null);
    }

    public Task<TOutput?> CallApiForUserAsync<TOutput>(string serviceName, Action<DownstreamApiOptions>? downstreamApiOptionsOverride = null, ClaimsPrincipal? user = null, CancellationToken cancellationToken = default) where TOutput : class
    {
        return Task.FromResult<TOutput?>(null);
    }

    public Task<TOutput?> DeleteForAppAsync<TInput, TOutput>(string? serviceName = null, TInput input = default!, Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null, CancellationToken cancellationToken = default) where TOutput : class
    {
        return Task.FromResult<TOutput?>(null);
    }

    public Task DeleteForAppAsync<TInput>(string? serviceName = null, TInput input = default!, Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<TOutput?> DeleteForUserAsync<TInput, TOutput>(string? serviceName = null, TInput input = default!, Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null, ClaimsPrincipal? user = null, CancellationToken cancellationToken = default) where TOutput : class
    {
        return Task.FromResult<TOutput?>(null);
    }

    public Task DeleteForUserAsync<TInput>(string? serviceName = null, TInput input = default!, Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null, ClaimsPrincipal? user = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<TOutput?> GetForAppAsync<TInput, TOutput>(string? serviceName = null, TInput input = default!, Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null, CancellationToken cancellationToken = default) where TOutput : class
    {
        return Task.FromResult<TOutput?>(null);
    }

    public Task<TOutput?> GetForAppAsync<TOutput>(string? serviceName = null, Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null, CancellationToken cancellationToken = default) where TOutput : class
    {
        return Task.FromResult<TOutput?>(null);
    }

    public Task<TOutput?> GetForUserAsync<TInput, TOutput>(string? serviceName = null, TInput input = default!, Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null, ClaimsPrincipal? user = null, CancellationToken cancellationToken = default) where TOutput : class
    {
        return Task.FromResult<TOutput?>(null);
    }

    public Task<TOutput?> GetForUserAsync<TOutput>(string? serviceName = null, Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null, ClaimsPrincipal? user = null, CancellationToken cancellationToken = default) where TOutput : class
    {
        return Task.FromResult<TOutput?>(null);
    }

    public Task<TOutput?> PatchForAppAsync<TInput, TOutput>(string? serviceName = null, TInput input = default!, Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null, CancellationToken cancellationToken = default) where TOutput : class
    {
        return Task.FromResult<TOutput?>(null);
    }

    public Task PatchForAppAsync<TInput>(string? serviceName = null, TInput input = default!, Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<TOutput?> PatchForUserAsync<TInput, TOutput>(string? serviceName = null, TInput input = default!, Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null, ClaimsPrincipal? user = null, CancellationToken cancellationToken = default) where TOutput : class
    {
        return Task.FromResult<TOutput?>(null);
    }

    public Task PatchForUserAsync<TInput>(string? serviceName = null, TInput input = default!, Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null, ClaimsPrincipal? user = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<TOutput?> PostForAppAsync<TInput, TOutput>(string? serviceName = null, TInput input = default!, Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null, CancellationToken cancellationToken = default) where TOutput : class
    {
        return Task.FromResult<TOutput?>(null);
    }

    public Task PostForAppAsync<TInput>(string? serviceName = null, TInput input = default!, Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<TOutput?> PostForUserAsync<TInput, TOutput>(string? serviceName = null, TInput input = default!, Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null, ClaimsPrincipal? user = null, CancellationToken cancellationToken = default) where TOutput : class
    {
        return Task.FromResult<TOutput?>(null);
    }

    public Task PostForUserAsync<TInput>(string? serviceName = null, TInput input = default!, Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null, ClaimsPrincipal? user = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<TOutput?> PutForAppAsync<TInput, TOutput>(string? serviceName = null, TInput input = default!, Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null, CancellationToken cancellationToken = default) where TOutput : class
    {
        return Task.FromResult<TOutput?>(null);
    }

    public Task PutForAppAsync<TInput>(string? serviceName = null, TInput input = default!, Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<TOutput?> PutForUserAsync<TInput, TOutput>(string? serviceName = null, TInput input = default!, Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null, ClaimsPrincipal? user = null, CancellationToken cancellationToken = default) where TOutput : class
    {
        return Task.FromResult<TOutput?>(null);
    }

    public Task PutForUserAsync<TInput>(string? serviceName = null, TInput input = default!, Action<DownstreamApiOptionsReadOnlyHttpMethod>? downstreamApiOptionsOverride = null, ClaimsPrincipal? user = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}