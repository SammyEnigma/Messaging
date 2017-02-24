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

        /// <summary>Sets the additional config to used to connect to RVD</summary>
        public void SetConfig(ServiceConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            lock (_configByService)
            {
                _configByService[config.Service] = config;
            }
        }

        public bool TryCreate(Uri address, out IMessaging messaging)
        {
            if (!address.Scheme.IsRvScheme())
            {
                messaging = null;
                return false;
            }

            var service = address.Host;
            var config = GetConfig(service);
            var netTrans = new Rv.NetTransport(service, config.Network, config.Daemon);
            switch (address.Scheme)
            {
                case "rv+cm":
                    if (string.IsNullOrEmpty(config.CmName))
                        ThrowCmNameNotConfigured();
                    var cmt = new Rv.CMTransport(netTrans);
                    messaging = new RvMessaging(address, cmt); //TODO: what about ledger files?
                    break;
                case "rv+dq":
                    if (string.IsNullOrEmpty(config.CmName))
                        ThrowCmNameNotConfigured();
                    var dqt = new Rv.CMQueueTransport(netTrans, config.CmName); //TODO: other queue parameters need configuring
                    messaging = new RvMessaging(address, dqt);
                    break;
                default:
                    messaging = new RvMessaging(address, netTrans);
                    break;
            }
            return true;
        }

        void ThrowCmNameNotConfigured()
        {
            throw new InvalidOperationException($"CmName has not been configured, please add it via the {nameof(SetConfig)} method");
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

            var netTrans = new Rv.NetTransport(service, config.Network, config.Daemon);
            switch (address.Scheme)
            {
                case "rv+cm":
                    if (string.IsNullOrEmpty(config.CmName))
                        ThrowCmNameNotConfigured();
                    var cmt = new Rv.CMTransport(netTrans); //TODO: what about ledger files?
                    subscriptionGroup = new RvMultiSubjectMessaging(cmt, address);
                    break;
                case "rv+dq":
                    if (string.IsNullOrEmpty(config.CmName))
                        ThrowCmNameNotConfigured();
                    var dqt = new Rv.CMQueueTransport(netTrans, config.CmName); //TODO: other queue parameters need configuring
                    subscriptionGroup = new RvMultiSubjectMessaging(dqt, address);
                    break;
                default:
                    subscriptionGroup = new RvMultiSubjectMessaging(netTrans, address);
                    break;
            }
            return true;
        }

        ServiceConfig GetConfig(string service)
        {
            lock (_configByService)
            {
                ServiceConfig config;
                _configByService.TryGetValue(service, out config);
                return config ?? ServiceConfig.Empty;
            }
        }

    }

    public class ServiceConfig
    {
        internal static readonly ServiceConfig Empty = new ServiceConfig();

        /// <summary>The RVD service group (defaults to 7500), see https://docs.tibco.com/pub/rendezvous/8.4.4/doc/html/tib_rv_administration/rv_adm.6.016.htm </summary>
        public string Service { get; set; }

        /// <summary>The network parameter (optional), see https://docs.tibco.com/pub/rendezvous/8.4.4/doc/html/tib_rv_administration/rv_adm.6.017.htm </summary>
        public string Network { get; set; }
        
        /// <summary>The daemon parameter (optional), see https://docs.tibco.com/pub/rendezvous/8.4.4/doc/html/tib_rv_concepts/rv_concepts.6.073.htm </summary>
        public string Daemon { get; set; }

        public string CmName { get; set; }
    }
}
