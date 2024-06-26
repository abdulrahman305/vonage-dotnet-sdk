using System;
using Vonage.Test.Common.Extensions;
using Vonage.VerifyV2.NextWorkflow;
using Xunit;

namespace Vonage.Test.VerifyV2.NextWorkflow;

[Trait("Category", "Request")]
public class RequestTest
{
    [Fact]
    public void GetEndpointPath_ShouldReturnApiEndpoint() =>
        NextWorkflowRequest.Parse(new Guid("f3a065af-ac5a-47a4-8dfe-819561a7a287"))
            .Map(request => request.GetEndpointPath())
            .Should()
            .BeSuccess("/v2/verify/f3a065af-ac5a-47a4-8dfe-819561a7a287/next_workflow");

    [Fact]
    public void Parse_ShouldReturnFailure_GivenRequestIsEmpty() =>
        NextWorkflowRequest.Parse(Guid.Empty)
            .Should()
            .BeParsingFailure("RequestId cannot be empty.");

    [Fact]
    public void Parse_ShouldReturnSuccess() =>
        NextWorkflowRequest.Parse(new Guid("f3a065af-ac5a-47a4-8dfe-819561a7a287"))
            .Map(request => request.RequestId)
            .Should()
            .BeSuccess(new Guid("f3a065af-ac5a-47a4-8dfe-819561a7a287"));
}