﻿using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mediator.Net.Binding;
using Mediator.Net.Context;
using Mediator.Net.Contracts;


namespace Mediator.Net.Pipeline
{
    public class ReceivePipe<TContext, TMessage> : IReceivePipe<TContext, TMessage>
        where TMessage : IMessage
        where TContext : IContext<TMessage>
    {
        private readonly IPipe<TContext, TMessage> _next;

        public ReceivePipe(IPipe<TContext, TMessage> next)
        {
            _next = next;
        }

        public Task Send(TContext context)
        {

            if (_next != null)
            {
                return _next.Send(context);
            }
            var handlers = MessageHandlerRegistry.Bindings.Where(x => x.Key.GetTypeInfo() == context.Message.GetType()).ToList();
            if (!handlers.Any())
                throw new NoHandlerFoundException(context.Message.GetType());
            if (handlers.Count() > 1)
            {
                throw new MoreThanOneCommandHandlerException(context.Message.GetType());
            }
            handlers.ForEach(x =>
            {
                var handlerType = x.Value.GetTypeInfo();
                var messageType = context.Message.GetType();

                var handleMethods = handlerType.GetRuntimeMethods().Where(m => m.Name == "Handle");
                var handleMethod = handleMethods.Single(y => {
                    var parameterTypeIsCorrect = y.GetParameters().Single()
                    .ParameterType.GenericTypeArguments.First()
                    .GetTypeInfo()
                    .IsAssignableFrom(messageType.GetTypeInfo());

                    return parameterTypeIsCorrect
                           && y.IsPublic
                           && ((y.CallingConvention & CallingConventions.HasThis) != 0);
                });

                var handler = Activator.CreateInstance(handlerType);
                var objectTask = handleMethod.Invoke(handler, new object[] { context });

                if (objectTask == null)
                {
                    throw new NullReferenceException($"Handler for message of type {messageType} returned null.");
                }


            });
            return Task.FromResult(1);
        }
    }
}