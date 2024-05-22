﻿using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Vonage.Common.Failures;
using Vonage.Common.Monads;

namespace Vonage.Common.Client;

/// <summary>
///     Represents a custom http client for Vonage APIs.
/// </summary>
public class VonageHttpClient
{
    private readonly HttpClient client;
    private readonly IJsonSerializer jsonSerializer;
    private readonly Result<HttpClientOptions> requestOptions;
    private readonly string userAgent;
    
    /// <summary>
    ///     Creates a custom Http Client for Vonage purposes.
    /// </summary>
    /// <param name="configuration">The custom configuration.</param>
    /// <param name="serializer">The serializer.</param>
    public VonageHttpClient(VonageHttpClientConfiguration configuration, IJsonSerializer serializer)
    {
        this.client = configuration.HttpClient;
        this.jsonSerializer = serializer;
        this.userAgent = configuration.UserAgent;
        this.requestOptions = configuration.AuthenticationHeader
            .Map(header =>
                new HttpClientOptions(header, UserAgentProvider.GetFormattedUserAgent(this.userAgent)));
    }
    
    internal VonageHttpClient WithDifferentHeader(Result<AuthenticationHeaderValue> header) =>
        new VonageHttpClient(new VonageHttpClientConfiguration(this.client, header, this.userAgent),
            this.jsonSerializer);
    
    /// <summary>
    ///     Sends a HttpRequest.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <returns>Success if the operation succeeds, Failure it if fails.</returns>
    public async Task<Result<Unit>> SendAsync<T>(Result<T> request) where T : IVonageRequest =>
        await this.SendRequest(request, this.BuildHttpRequestMessage, this.ParseFailure<Unit>, CreateSuccessResult)
            .ConfigureAwait(false);
    
    /// <summary>
    ///     Sends a HttpRequest without Authorization and UserAgent headers.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <returns>Success if the operation succeeds, Failure it if fails.</returns>
    public async Task<Result<Unit>> SendWithoutHeadersAsync<T>(Result<T> request) where T : IVonageRequest =>
        await this.SendRequest(request, value => value.BuildRequestMessage(), this.ParseFailure<Unit>,
            CreateSuccessResult).ConfigureAwait(false);
    
    /// <summary>
    ///     Sends a HttpRequest and returns the raw content.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <typeparam name="TRequest">Type of the request.</typeparam>
    /// <returns>Success if the operation succeeds, Failure it if fails.</returns>
    public async Task<Result<string>> SendWithRawResponseAsync<TRequest>(Result<TRequest> request)
        where TRequest : IVonageRequest =>
        await this.SendRequest(request, this.BuildHttpRequestMessage, this.ParseFailure<string>,
            responseData => responseData.Content).ConfigureAwait(false);
    
    /// <summary>
    ///     Sends a HttpRequest and parses the response.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <returns>Success if the operation succeeds, Failure it if fails.</returns>
    public async Task<Result<TResponse>> SendWithResponseAsync<TRequest, TResponse>(Result<TRequest> request)
        where TRequest : IVonageRequest =>
        await this.SendRequest(request, this.BuildHttpRequestMessage, this.ParseFailure<TResponse>,
            this.ParseSuccess<TResponse>).ConfigureAwait(false);
    
    private Result<HttpRequestMessage> BuildHttpRequestMessage<T>(T value) where T : IVonageRequest =>
        this.requestOptions
            .Map(options => value
                .BuildRequestMessage()
                .WithAuthenticationHeader(options.AuthenticationHeader)
                .WithUserAgent(options.UserAgent));
    
    private HttpFailure CreateFailureResult(HttpStatusCode code, string responseContent) =>
        this.jsonSerializer
            .DeserializeObject<ErrorResponse>(responseContent)
            .Match(success => HttpFailure.From(code, success.Message, responseContent),
                failure => HttpFailure.From(code, failure.GetFailureMessage(), responseContent));
    
    private static HttpFailure CreateFailureResult(HttpStatusCode code) => HttpFailure.From(code);
    
    private static Result<Unit> CreateSuccessResult(ResponseData response) => Result<Unit>.FromSuccess(Unit.Default);
    
    private static async Task<ResponseData> ExtractResponseData(HttpResponseMessage response) =>
        new ResponseData(response.StatusCode, response.IsSuccessStatusCode,
            await response.Content.ReadAsStringAsync().ConfigureAwait(false));
    
    private Result<T> ParseFailure<T>(ResponseData response) =>
        MaybeExtensions.From(response.Content)
            .Match(value => this.CreateFailureResult(response.Code, value), () => CreateFailureResult(response.Code))
            .ToResult<T>();
    
    private Result<T> ParseSuccess<T>(ResponseData response) =>
        this.jsonSerializer
            .DeserializeObject<T>(response.Content)
            .Match(Result<T>.FromSuccess, Result<T>.FromFailure);
    
    private async Task<Result<TResponse>> SendRequest<TRequest, TResponse>(
        Result<TRequest> request,
        Func<TRequest, Result<HttpRequestMessage>> httpRequestConversion,
        Func<ResponseData, Result<TResponse>> failure,
        Func<ResponseData, Result<TResponse>> success) =>
        await request
            .Bind(httpRequestConversion)
            .MapAsync(value => this.client.SendAsync(value))
            .MapAsync(ExtractResponseData)
            .Bind(response => !response.IsSuccessStatusCode ? failure(response) : success(response))
            .ConfigureAwait(false);
    
    private sealed record ResponseData(HttpStatusCode Code, bool IsSuccessStatusCode, string Content);
    
    private sealed record HttpClientOptions(AuthenticationHeaderValue AuthenticationHeader, string UserAgent);
}