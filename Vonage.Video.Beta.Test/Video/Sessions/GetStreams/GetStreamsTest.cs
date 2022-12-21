﻿using System;
using System.Net;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Vonage.Video.Beta.Common;
using Vonage.Video.Beta.Common.Failures;
using Vonage.Video.Beta.Test.Extensions;
using Vonage.Video.Beta.Video.Sessions;
using Vonage.Video.Beta.Video.Sessions.GetStreams;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Vonage.Video.Beta.Test.Video.Sessions.GetStreams
{
    public class GetStreamsTest : IDisposable
    {
        private readonly SessionClient client;
        private readonly Fixture fixture;
        private readonly JsonSerializer jsonSerializer;
        private readonly string path;
        private readonly Result<GetStreamsRequest> request;
        private readonly WireMockServer server;
        private readonly string token;

        public GetStreamsTest()
        {
            this.server = WireMockServer.Start();
            this.jsonSerializer = new JsonSerializer();
            this.fixture = new Fixture();
            this.fixture.Create<string>();
            this.token = this.fixture.Create<string>();
            this.request = GetStreamsRequest.Parse(this.fixture.Create<string>(), this.fixture.Create<string>());
            this.path = this.GetPathFromRequest();
            this.client = new SessionClient(this.server.CreateClient(), () => this.token);
        }

        public void Dispose()
        {
            this.server.Stop();
            this.server.Reset();
            GC.SuppressFinalize(this);
        }

        [Property]
        public Property ShouldReturnFailure_GivenApiResponseIsError() =>
            Prop.ForAll(
                FsCheckExtensions.GetInvalidStatusCodes(),
                Arb.From<string>(),
                (statusCode, message) => this.VerifyReturnsFailureGivenStatusCodeIsFailure(statusCode, message).Wait());

        [Property]
        public Property ShouldReturnFailure_GivenApiErrorCannotBeParsed() =>
            Prop.ForAll(
                FsCheckExtensions.GetInvalidStatusCodes(),
                Arb.From<string>().MapFilter(_ => _, value => !string.IsNullOrWhiteSpace(value)),
                (statusCode, jsonError) =>
                    this.VerifyReturnsFailureGivenErrorCannotBeParsed(statusCode, jsonError).Wait());

        [Fact]
        public async Task ShouldReturnSuccess_GivenApiResponseIsSuccess()
        {
            var expectedResponse = this.fixture.Create<GetStreamsResponse>();
            this.server
                .Given(this.CreateGetStreamsRequest())
                .RespondWith(CreateGetStreamsResponse(HttpStatusCode.OK,
                    this.jsonSerializer.SerializeObject(expectedResponse)));
            var result = await this.request.BindAsync(requestValue => this.client.GetStreamsAsync(requestValue));
            result.Should().BeSuccess(response =>
            {
                this.jsonSerializer.SerializeObject(response).Should()
                    .Be(this.jsonSerializer.SerializeObject(expectedResponse));
            });
        }

        [Fact]
        public async Task ShouldReturnFailure_GivenApiResponseCannotBeParsed()
        {
            var body = this.fixture.Create<string>();
            var expectedFailureMessage = $"Unable to deserialize '{body}' into '{nameof(GetStreamsResponse)}'.";
            this.server
                .Given(this.CreateGetStreamsRequest())
                .RespondWith(CreateGetStreamsResponse(HttpStatusCode.OK, body));
            var result = await this.request.BindAsync(requestValue => this.client.GetStreamsAsync(requestValue));
            result.Should().BeFailure(ResultFailure.FromErrorMessage(expectedFailureMessage));
        }

        private async Task VerifyReturnsFailureGivenStatusCodeIsFailure(HttpStatusCode code, string message)
        {
            var expectedBody = message is null
                ? null
                : this.jsonSerializer.SerializeObject(new ErrorResponse(((int) code).ToString(), message));
            this.server
                .Given(this.CreateGetStreamsRequest())
                .RespondWith(CreateGetStreamsResponse(code, expectedBody));
            var result = await this.request.BindAsync(requestValue => this.client.GetStreamsAsync(requestValue));
            result.Should().BeFailure(HttpFailure.From(code, message ?? string.Empty));
        }

        private string GetPathFromRequest() =>
            this.request.Match(value => value.GetEndpointPath(), failure => string.Empty);

        private async Task VerifyReturnsFailureGivenErrorCannotBeParsed(HttpStatusCode code, string jsonError)
        {
            var expectedFailureMessage = $"Unable to deserialize '{jsonError}' into '{nameof(ErrorResponse)}'.";
            this.server
                .Given(this.CreateGetStreamsRequest())
                .RespondWith(CreateGetStreamsResponse(code,
                    jsonError));
            var result = await this.request.BindAsync(requestValue => this.client.GetStreamsAsync(requestValue));
            result.Should().BeFailure(ResultFailure.FromErrorMessage(expectedFailureMessage));
        }

        private IRequestBuilder CreateGetStreamsRequest() =>
            WireMockExtensions.BuildRequestWithAuthenticationHeader(this.token).WithPath(this.path).UsingGet();

        private static IResponseBuilder CreateGetStreamsResponse(HttpStatusCode code, string body) =>
            body is null ? CreateGetStreamsResponse(code) : CreateGetStreamsResponse(code).WithBody(body);

        private static IResponseBuilder CreateGetStreamsResponse(HttpStatusCode code) =>
            Response.Create().WithStatusCode(code);
    }
}