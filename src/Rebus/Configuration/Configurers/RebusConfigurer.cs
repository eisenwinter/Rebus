using System;
using Rebus.Persistence.InMemory;
using Rebus.Serialization.Json;

namespace Rebus.Configuration.Configurers
{
    public class RebusConfigurer
    {
        readonly IContainerAdapter containerAdapter;

        public RebusConfigurer(IContainerAdapter containerAdapter)
        {
            this.containerAdapter = containerAdapter;
            containerAdapter.RegisterInstance(containerAdapter, typeof (IActivateHandlers));
        }

        public RebusConfigurer Transport(Action<TransportConfigurer> configureTransport)
        {
            configureTransport(new TransportConfigurer(containerAdapter));
            return this;
        }

        public RebusConfigurer Sagas(Action<SagaConfigurer> configureSagas)
        {
            configureSagas(new SagaConfigurer(containerAdapter));
            return this;
        }

        public RebusConfigurer Subscriptions(Action<SubscriptionsConfigurer> configureSubscriptions)
        {
            configureSubscriptions(new SubscriptionsConfigurer(containerAdapter));
            return this;
        }

        public RebusConfigurer Serialization(Action<SerializationConfigurer> configureSerialization)
        {
            configureSerialization(new SerializationConfigurer(containerAdapter));
            return this;
        }

        public RebusConfigurer DetermineEndpoints(Action<EndpointMappingsConfigurer> configureEndpointsMappings)
        {
            configureEndpointsMappings(new EndpointMappingsConfigurer(containerAdapter));
            return this;
        }

        public RebusConfigurer SpecifyOrderOfHandlers(Action<PipelineInspectorConfigurer> configurePipelineInspector)
        {
            configurePipelineInspector(new PipelineInspectorConfigurer(containerAdapter));
            return this;
        }

        public IStartableBus CreateBus()
        {
            FillOutMissingRegistrationsWithDefaults();
            
            ValidateConfiguration();

            return containerAdapter.GetStartableBus();
        }

        void ValidateConfiguration()
        {
            if (!(containerAdapter.HasImplementationOf(typeof(ISendMessages))
                || containerAdapter.HasImplementationOf(typeof(IReceiveMessages))))
            {
                throw new ConfigurationException
                    (@"
You need to configure Rebus to be able to at least either SEND or RECEIVE messages. Otherwise
it wouldn't be that useful, would it?

If, for some reason, you really really WANT to circumvent this rule, please feel free to get
the bus by new'ing it up yourself - then you can do whatever you feel like.

This configuration API, however, will not let you create an unusable bus. You can configure
the transport in one easy operation like so:

    var bus = Configure.With(someContainerAdapter)
                .Transport(s => s.UseMsmq(""some_input_queue_name""))
                (....)
                .CreateBus()

thus configuring the ability to send AND receive messages at the same time, using MSMQ for
both.");
            }
        }

        void FillOutMissingRegistrationsWithDefaults()
        {
            if (!containerAdapter.HasImplementationOf(typeof(IStoreSubscriptions)))
            {
                containerAdapter.RegisterInstance(new InMemorySubscriptionStorage(), typeof(IStoreSubscriptions));
            }

            if (!containerAdapter.HasImplementationOf(typeof(IStoreSagaData)))
            {
                containerAdapter.RegisterInstance(new InMemorySagaPersister(), typeof(IStoreSagaData));
            }

            if (!containerAdapter.HasImplementationOf(typeof(IDetermineDestination)))
            {
                containerAdapter.RegisterInstance(new ThrowingEndpointMapper(), typeof(IDetermineDestination));
            }

            if (!containerAdapter.HasImplementationOf(typeof(ISerializeMessages)))
            {
                containerAdapter.RegisterInstance(new JsonMessageSerializer(), typeof (ISerializeMessages));
            }

            if (!containerAdapter.HasImplementationOf(typeof(IInspectHandlerPipeline)))
            {
                containerAdapter.RegisterInstance(new TrivialPipelineInspector(), typeof (IInspectHandlerPipeline));
            }
        }
    }
}