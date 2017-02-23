using System;
using System.Collections.Generic;
using Rv = TIBCO.Rendezvous;

namespace Messaging.TibcoRv
{
    public class RvMessagingFactory : IMessagingFactory
    {
        readonly Dictionary<string, ServiceConfig> _configByService;

        public RvMessagingFactory() : this(new Dictionary<string, ServiceConfig>())
        {
        }

        public RvMessagingFactory(Dictionary<string, ServiceConfig> configByService)
        {
            if (configByService == null)
                throw new ArgumentNullException(nameof(configByService));
            _configByService = configByService;

            TIBCO.Rendezvous.Environment.Open(); // todo: close?
        }

        /// <summary>
        /// Each RVD client may neeed some extra config to connect to the <paramref name="service"/>
        /// </summary>
        /// <param name="service">The RVD service group (defaults to 7500), see https://docs.tibco.com/pub/rendezvous/8.4.4/doc/html/tib_rv_administration/rv_adm.6.016.htm </param>
        /// <param name="network">The network parameter (optional), see https://docs.tibco.com/pub/rendezvous/8.4.4/doc/html/tib_rv_administration/rv_adm.6.017.htm </param>
        /// <param name="daemon">The daemon parameter (optional), see https://docs.tibco.com/pub/rendezvous/8.4.4/doc/html/tib_rv_concepts/rv_concepts.6.073.htm</param>
        public void SetConfig(string service, string network, string daemon)
        {
            if (string.IsNullOrEmpty(service))
                throw new ArgumentNullException(nameof(service));

            lock (_configByService)
            {
                _configByService[service] = new ServiceConfig(network, daemon);
            }
        }

        public bool TryCreate(Uri address, out IMessaging transport)
        {
            if (!address.Scheme.IsRvScheme())
            {
                transport = null;
                return false;
            }

            var service = address.Host;
            var config = GetConfig(service);
            var rvt = new Rv.NetTransport(service, config.Network, config.Daemon);
            transport = new RvMessaging(address, rvt);
            return true;
        }

        public bool TryCreateMultiSubject(Uri address, out IMultiSubjectMessaging subscriptionGroup)
        {
            if (!address.Scheme.IsRvScheme())
            {
                subscriptionGroup = null;
                return false;
            }

            var service = address.Host;
            var config = GetConfig(service);
            var rvt = new Rv.NetTransport(service, config.Network, config.Daemon);
            subscriptionGroup = new RvMultiSubjectMessaging(rvt, address);
            return true;
        }

        ServiceConfig GetConfig(string service)
        {
            lock (_configByService)
            {
                ServiceConfig config;
                _configByService.TryGetValue(service, out config);
                return config;
            }
        }

    }

    public struct ServiceConfig
    {
        public string Network { get; }
        public string Daemon { get; }

        public ServiceConfig(string network, string daemon)
        {
            Daemon = daemon;
            Network = network;
        }
    }
}
