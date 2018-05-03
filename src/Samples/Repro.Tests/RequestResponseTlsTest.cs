using System;
using System.Threading.Tasks;
using GreenPipes;
using log4net.Config;
using MassTransit;
using MassTransit.Log4NetIntegration;
using NUnit.Framework;

namespace Repro.Tests
{
    public class RequestResponseTlsTest
    {
        const string SERVER_CERTIFICATE_CN = "localhost";
        const ushort AMQP_PORT = 5672;
        const ushort AMQPS_PORT = 5671;

        IBusControl _serverBus;
        IBusControl _clientBus;

        [SetUp]
        public async Task SetUp()
        {
            BasicConfigurator.Configure();
        }

        [TearDown]
        public async Task TearDown()
        {
            await _serverBus.StopAsync();
            await _clientBus.StopAsync();
        }

        [TestCase(false, false)]
        [TestCase(true, true)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        public async Task Should_do_request_response(bool serverSsl, bool clientSsl)
        {
            _serverBus = await StartServerBusAsync(serverSsl);
            _clientBus = await StartClientBusAsync(clientSsl);

            var publishRequestClient = _clientBus.CreatePublishRequestClient<SumRequest, SumResponse>(TimeSpan.FromSeconds(5));
            var myResponse = await publishRequestClient.Request(new SumRequest {Value1 = 1, Value2 = 2});
            
            Assert.That(myResponse.Sum, Is.EqualTo(3));
        }
        
        static async Task<IBusControl> StartServerBusAsync(bool useSsl)
        {
            var serverBus = Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                cfg.UseLog4Net();
                var host = cfg.Host("localhost", useSsl ? AMQPS_PORT : AMQP_PORT, "/", hostCfg =>
                {
                    hostCfg.Username("guest");
                    hostCfg.Password("guest");
                    if (useSsl)
                    {
                        hostCfg.UseSsl(sslCfg => sslCfg.ServerName = SERVER_CERTIFICATE_CN);
                    }
                });
                cfg.UseConcurrencyLimit(1);

                cfg.ReceiveEndpoint(host, "test_queue", epCfg =>
                {
                    epCfg.Consumer<CalculatorConsumer>();
                    epCfg.PurgeOnStartup = true;
                });
            });
            await serverBus.StartAsync();
            
            return serverBus;
        }

        static async Task<IBusControl> StartClientBusAsync(bool useSsl)
        {
            var clientBus = Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                cfg.UseLog4Net();
                cfg.Host("localhost", useSsl ? AMQPS_PORT : AMQP_PORT, "/", hostCfg =>
                {
                    hostCfg.Username("guest");
                    hostCfg.Password("guest");
                    if (useSsl)
                    {
                        hostCfg.UseSsl(sslCfg => sslCfg.ServerName = SERVER_CERTIFICATE_CN);
                    }
                });
            });

            await clientBus.StartAsync();

            return clientBus;
        }
    }
}