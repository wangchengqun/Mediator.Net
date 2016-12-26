﻿using Mediator.Net.Context;
using Mediator.Net.Contracts;

namespace Mediator.Net.Pipeline
{
    public interface IPublishPipe<in TContext, TMessage> :IPipe<TContext, TMessage>
        where TMessage : IEvent
        where TContext : IContext<TMessage>
    {
    }
}
