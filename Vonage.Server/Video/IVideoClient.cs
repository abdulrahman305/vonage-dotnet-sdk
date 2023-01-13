﻿using Vonage.Request;
using Vonage.Server.Video.Archives;
using Vonage.Server.Video.Moderation;
using Vonage.Server.Video.Sessions;
using Vonage.Server.Video.Signaling;

namespace Vonage.Server.Video;

/// <summary>
///     Exposes Video clients.
/// </summary>
public interface IVideoClient
{
    /// <summary>
    ///     Client for archiving.
    /// </summary>
    ArchiveClient ArchiveClient { get; }

    /// <summary>
    ///     Credentials to be used for further sessions.
    /// </summary>
    Credentials Credentials { get; set; }

    /// <summary>
    ///     Client for moderating connections.
    /// </summary>
    ModerationClient ModerationClient { get; }

    /// <summary>
    ///     Client for managing sessions.
    /// </summary>
    SessionClient SessionClient { get; }

    /// <summary>
    ///     Client for sending signals to participants.
    /// </summary>
    SignalingClient SignalingClient { get; }
}