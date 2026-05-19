using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using EventScheduler.DTOs;
using EventScheduler.Services.Interfaces;

namespace EventScheduler.Services
{
    public class OpcUaClientService : BackgroundService
    {
        private readonly ILogger<OpcUaClientService> _logger;
        private readonly ISignalProcessingEngine _engine;
        private readonly IConfiguration _config;
        private Session? _session;

        public OpcUaClientService(
            ILogger<OpcUaClientService> logger,
            ISignalProcessingEngine engine,
            IConfiguration config)
        {
            _logger = logger;
            _engine = engine;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var endpointUrl = _config["OpcUa:EndpointUrl"];

            if (string.IsNullOrEmpty(endpointUrl))
            {
                _logger.LogWarning("OPC-UA endpoint not configured. Skipping OPC-UA client.");
                return;
            }

            _logger.LogInformation("OPC-UA Client starting. Connecting to {Endpoint}...", endpointUrl);

            // Wait for the app to fully start
            await Task.Delay(3000, stoppingToken);

            try
            {
                await ConnectAndSubscribe(endpointUrl, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OPC-UA Client failed to connect. Signals will only come via REST.");
            }

            // Keep the service alive
            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("OPC-UA Client shutting down.");
            }
        }

        private async Task ConnectAndSubscribe(string endpointUrl, CancellationToken ct)
        {
            // 1. Create application configuration
            var pkiRoot = Path.Combine(AppContext.BaseDirectory, "pki");

            var appConfig = new ApplicationConfiguration
            {
                ApplicationName = "EventScheduler_OpcUaClient",
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(pkiRoot, "own")
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(pkiRoot, "issuers")
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(pkiRoot, "trusted")
                    },
                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(pkiRoot, "rejected")
                    },
                    AutoAcceptUntrustedCertificates = true
                },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 }
            };


            await appConfig.Validate(ApplicationType.Client);

            // Auto-accept server certificates (for development)
            appConfig.CertificateValidator = new CertificateValidator();
            appConfig.CertificateValidator.CertificateValidation += (s, e) =>
            {
                e.Accept = true;
            };

            // 2. Connect to the OPC-UA server (our Node.js simulator)
            var endpoint = CoreClientUtils.SelectEndpoint(appConfig, endpointUrl, false);
            var endpointConfig = EndpointConfiguration.Create(appConfig);
            var configuredEndpoint = new ConfiguredEndpoint(null, endpoint, endpointConfig);

            _session = await Session.Create(
                appConfig,
                configuredEndpoint,
                false,
                "EventSchedulerSession",
                60000,
                new UserIdentity(new AnonymousIdentityToken()),
                null
            );

            _logger.LogInformation("Connected to OPC-UA server at {Endpoint}", endpointUrl);

            // 3. Create a subscription
            var subscription = new Subscription(_session.DefaultSubscription)
            {
                PublishingInterval = 1000,
                PublishingEnabled = true
            };
            _session.AddSubscription(subscription);
            subscription.Create();

            // 4. Read tag mappings from appsettings.json and subscribe
            var tagMappings = _config.GetSection("OpcUa:TagMappings").Get<Dictionary<string, string>>();

            if (tagMappings == null || tagMappings.Count == 0)
            {
                _logger.LogWarning("No OPC-UA tag mappings configured.");
                return;
            }

            foreach (var mapping in tagMappings)
            {
                var nodeId = mapping.Key;     // e.g. "ns=1;s=PumpSensor"
                var signalId = mapping.Value;  // e.g. "Signal.A"

                var item = new MonitoredItem(subscription.DefaultItem)
                {
                    StartNodeId = nodeId,
                    AttributeId = Attributes.Value,
                    DisplayName = signalId,
                    SamplingInterval = 1000,
                    MonitoringMode = MonitoringMode.Reporting
                };

                // When a tag value changes, feed it into our SignalProcessingEngine
                item.Notification += (sender, args) =>
                {
                    if (args.NotificationValue is MonitoredItemNotification notification)
                    {
                        var value = notification.Value.Value;
                        bool boolValue = false;

                        if (value is bool b)
                            boolValue = b;
                        else if (value is int i)
                            boolValue = i != 0;
                        else if (value is double d)
                            boolValue = d > 0;

                        _logger.LogInformation(
                            "OPC-UA Tag Changed: {NodeId} -> {SignalId} = {Value}",
                            nodeId, signalId, boolValue);

                        // Feed into the SAME engine that Postman uses
                        var signals = new List<SignalDataDto>
                        {
                            new SignalDataDto
                            {
                                Id = signalId,
                                Values = new List<SignalValueDto>
                                {
                                    new SignalValueDto
                                    {
                                        TimeStamp = DateTime.UtcNow,
                                        Value = boolValue,
                                        QualityCode = "192",
                                        QualityFlag = notification.Value.StatusCode.ToString()
                                    }
                                }
                            }
                        };

                        _engine.ProcessSignals(signals);
                    }
                };

                subscription.AddItem(item);
                _logger.LogInformation("Subscribed to tag: {NodeId} -> {SignalId}", nodeId, signalId);
            }

            // CRITICAL: Apply changes to activate subscriptions
            subscription.ApplyChanges();
            _logger.LogInformation("OPC-UA subscriptions active. Listening for tag changes...");
        }

        public override void Dispose()
        {
            _session?.Close();
            _session?.Dispose();
            base.Dispose();
        }
    }
}
