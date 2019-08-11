﻿using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpenMessage.Pipelines
{
    /// <summary>
    ///     The base type for providing a consumer of the internal messaging channel
    /// </summary>
    /// <typeparam name="T">The type that is contained in the message</typeparam>
    public abstract class ConsumerPumpBase<T> : BackgroundService
    {
        /// <summary>
        ///     The reader of the messaging channel
        /// </summary>
        protected ChannelReader<Message<T>> ChannelReader { get; }

        /// <summary>
        ///     The pipeline that gets called for each message
        /// </summary>
        protected IPipeline<T> Pipeline { get; }

        /// <summary>
        ///     The current options for the pipeline
        /// </summary>
        protected IOptionsMonitor<PipelineOptions<T>> OptionsMonitor { get; }

        /// <summary>
        ///     The current options for the pipeline
        /// </summary>
        protected PipelineOptions<T> Options => OptionsMonitor.CurrentValue;

        /// <summary>
        ///     The logger to use
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        ///     ctor
        /// </summary>
        protected ConsumerPumpBase(ChannelReader<Message<T>> channelReader,
            IPipeline<T> pipeline,
            IOptionsMonitor<PipelineOptions<T>> optionsMonitor,
            ILogger logger)
        {
            ChannelReader = channelReader ?? throw new ArgumentNullException(nameof(channelReader));
            Pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            OptionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritDoc />
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Starting consumer pump: " + GetType().GetFriendlyName());
            return base.StartAsync(cancellationToken);
        }

        /// <inheritDoc />
        protected sealed override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Message<T> msg = null;
                    try
                    {
                        msg = await ChannelReader.ReadAsync(cancellationToken);
                        using var timedCts = new CancellationTokenSource(Options.PipelineTimeout);
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timedCts.Token);
                        await OnMessageConsumed(msg, cts.Token);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, ex.Message);

                        if (Options.AutoAcknowledge == true && msg != null && msg is ISupportAcknowledgement aam)
                            await aam.AcknowledgeAsync(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
            }
        }

        /// <summary>
        ///     Occurs when a message is consumed.
        /// </summary>
        /// <param name="message">The message consumed.</param>
        /// <param name="cancellationToken">The cancellation token configured to timeout in the configured time.</param>
        /// <returns>A task that completes when the handle method completes</returns>
        protected abstract Task OnMessageConsumed(Message<T> message, CancellationToken cancellationToken);

        /// <inheritDoc />
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Stopping consumer pump: " + GetType().GetFriendlyName());
            return base.StartAsync(cancellationToken);
        }
    }
}