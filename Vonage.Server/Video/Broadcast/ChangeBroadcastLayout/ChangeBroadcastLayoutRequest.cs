﻿using System;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using Vonage.Common.Client;
using Vonage.Server.Common;
using Vonage.Server.Serialization;

namespace Vonage.Server.Video.Broadcast.ChangeBroadcastLayout
{
    /// <inheritdoc />
    public readonly struct ChangeBroadcastLayoutRequest : IVonageRequest
    {
        /// <summary>
        ///     The Vonage Application UUID.
        /// </summary>
        [JsonIgnore]
        public Guid ApplicationId { get; internal init; }

        /// <summary>
        ///     The Id of the broadcast.
        /// </summary>
        [JsonIgnore]
        public Guid BroadcastId { get; internal init; }

        /// <summary>
        ///     The new layout to apply on the broadcast.
        /// </summary>
        [JsonIgnore]
        public Layout Layout { get; internal init; }

        /// <inheritdoc />
        public HttpRequestMessage BuildRequestMessage() =>
            VonageRequestBuilder
                .Initialize(HttpMethod.Post, this.GetEndpointPath())
                .WithContent(this.GetRequestContent())
                .Build();

        /// <inheritdoc />
        public string GetEndpointPath() => $"/v2/project/{this.ApplicationId}/broadcast/{this.BroadcastId}/layout";

        private StringContent GetRequestContent() =>
            new(JsonSerializerBuilder.Build().SerializeObject(this.Layout), Encoding.UTF8, "application/json");
    }
}