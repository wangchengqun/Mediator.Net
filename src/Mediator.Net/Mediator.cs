﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Mediator.Net.Context;
using Mediator.Net.Contracts;
using Mediator.Net.Pipeline;

namespace Mediator.Net
{
    public class Mediator : IMediator
    {
        private readonly IReceivePipe<IReceiveContext<IMessage>> _receivePipe;
        private readonly IRequestPipe<IReceiveContext<IRequest>> _requestPipe;
        private readonly IPublishPipe<IPublishContext<IEvent>> _publishPipe;
        private readonly IGlobalReceivePipe<IReceiveContext<IMessage>> _globalPipe;
        private readonly IDependancyScope _scope;

        public Mediator(IReceivePipe<IReceiveContext<IMessage>> receivePipe,
            IRequestPipe<IReceiveContext<IRequest>> requestPipe, 
            IPublishPipe<IPublishContext<IEvent>> publishPipe, 
            IGlobalReceivePipe<IReceiveContext<IMessage>> globalPipe,
            IDependancyScope scope = null)
        {
            _receivePipe = receivePipe;
            _requestPipe = requestPipe;
            _publishPipe = publishPipe;
            _globalPipe = globalPipe;
            _scope = scope;
        }


        public Task SendAsync<TMessage>(TMessage cmd)
            where TMessage : ICommand
        {
#if DEBUG
            var stopWatch = new Stopwatch();
            stopWatch.Start();
#endif
            var task = SendMessage(cmd);
#if DEBUG
            stopWatch.Stop();
            Console.WriteLine($"It took {stopWatch.ElapsedMilliseconds} milliseconds");
#endif
            
            return task;
        }

        public Task PublishAsync<TMessage>(TMessage evt)
            where TMessage : IEvent
        {
            var task = SendMessage(evt);
            return task;
        }

        public async Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request)
            where TRequest : IRequest
            where TResponse : IResponse
        {
            var receiveContext = (IReceiveContext<TRequest>)Activator.CreateInstance(typeof(ReceiveContext<>).MakeGenericType(request.GetType()), request);
            receiveContext.RegisterService(this);
            IPublishPipe<IPublishContext<IEvent>> publishPipeInContext;
            if (!receiveContext.TryGetService(out publishPipeInContext))
            {
                receiveContext.RegisterService(_publishPipe);
            }

            IRequestPipe<IReceiveContext<IRequest>> requestPipeInContext;
            if (!receiveContext.TryGetService(out requestPipeInContext))
            {
                receiveContext.RegisterService(_requestPipe);
            }
            var sendMethodInRequestPipe = _globalPipe.GetType().GetMethod("Connect");
            var result = await ((Task<object>)sendMethodInRequestPipe.Invoke(_globalPipe, new object[] { receiveContext })).ConfigureAwait(false);
            
            return (TResponse)result;

        }

        private Task SendMessage<TMessage>(TMessage msg)
            where TMessage : IMessage
        {
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
#endif
            var receiveContext = (IReceiveContext<TMessage>)Activator.CreateInstance(typeof(ReceiveContext<>).MakeGenericType(msg.GetType()), msg);
            receiveContext.RegisterService(this);
            IPublishPipe<IPublishContext<IEvent>> publishPipeInContext;
            if (!receiveContext.TryGetService(out publishPipeInContext))
            {
                receiveContext.RegisterService(_publishPipe);
            }

            IReceivePipe<IReceiveContext<IMessage>> receivePipeInContext;
            if (!receiveContext.TryGetService(out receivePipeInContext))
            {
                receiveContext.RegisterService(_receivePipe);
            }
#if DEBUG
            
            var sendMethodInGlobalPipe = _globalPipe.GetType().GetMethod("Connect");
            sw.Stop();
            Console.WriteLine($"It took {sw.ElapsedMilliseconds} milliseconds to initialize");
#endif
            var task = (Task)sendMethodInGlobalPipe.Invoke(_globalPipe, new object[] { receiveContext });
            task.ConfigureAwait(false);
            return task;
        }

        public void Dispose()
        {
            _scope?.Dispose();
        }
    }
}