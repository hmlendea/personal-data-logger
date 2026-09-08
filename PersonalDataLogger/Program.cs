using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NuciAPI.Client;
using NuciLog;
using NuciLog.Configuration;
using NuciLog.Core;

using PersonalDataLogger.Client;
using PersonalDataLogger.Configuration;
using PersonalDataLogger.Service;
using PersonalDataLogger.Service.Processors;

namespace PersonalDataLogger
{
    public sealed class Program
    {
        private static ILogger logger;

        private static IServiceProvider serviceProvider;

        private static void Main(string[] args)
        {
            serviceProvider = CreateIOC();
            logger = serviceProvider.GetService<ILogger>();
            IEmailWorker emailWorker = serviceProvider.GetService<IEmailWorker>();
            ITimedLogWorker timedLogWorker = serviceProvider.GetService<ITimedLogWorker>();

            logger.Info(Operation.StartUp, "The service has started.");

            try
            {
                Task emailWorkerTask = Task.Run(emailWorker.WatchEmails);
                Task timedLogWorkerTask = Task.Run(timedLogWorker.WatchTimedLogs);
                Task completedTask = Task.WhenAny(emailWorkerTask, timedLogWorkerTask).GetAwaiter().GetResult();

                completedTask.GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                logger.Fatal(Operation.Unknown, OperationStatus.Failure, exception);
            }
            finally
            {
                logger.Info(Operation.ShutDown, "The service has stopped.");
            }
        }

        private static IServiceProvider CreateIOC()
        {
            PersonalLogManagerSettings personalLogManagerSettings = new();
            ImapSettings imapSettings = new();
            PersonalSettings personalSettings = new();
            AliExpressSettings aliExpressSettings = new();
            ProfiBotServerSettings profiBotServerSettings = new();
            NuciLoggerSettings loggerSettings = new();

            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            config.Bind(nameof(PersonalLogManagerSettings), personalLogManagerSettings);
            config.Bind(nameof(ImapSettings), imapSettings);
            config.Bind(nameof(PersonalSettings), personalSettings);
            config.Bind(nameof(AliExpressSettings), aliExpressSettings);
            config.Bind(nameof(ProfiBotServerSettings), profiBotServerSettings);
            config.Bind(nameof(NuciLoggerSettings), loggerSettings);

            return new ServiceCollection()
                .AddSingleton(personalLogManagerSettings)
                .AddSingleton(imapSettings)
                .AddSingleton(personalSettings)
                .AddSingleton(aliExpressSettings)
                .AddSingleton(profiBotServerSettings)
                .AddSingleton(loggerSettings)
                .AddSingleton<IAliExpressProcessor, AliExpressProcessor>()
                .AddSingleton<IGandiProcessor, GandiProcessor>()
                .AddSingleton<IOpsGenieEmailProcessor, OpsGenieEmailProcessor>()
                .AddSingleton<IPayPalProcessor, PayPalProcessor>()
                .AddSingleton<IProfiProcessor, ProfiProcessor>()
                .AddSingleton<IEmailProcessor, EmailProcessor>()
                .AddSingleton<IEmailWorker, EmailWorker>()
                .AddSingleton<ITimedLog, ProfiBalanceTimedLog>()
                .AddSingleton<ITimedLogWorker, TimedLogWorker>()
                .AddSingleton<INuciApiClient>(new NuciApiClient(personalLogManagerSettings.BaseUrl))
                .AddSingleton<IPersonalLogManagerService, PersonalLogManagerService>()
                .AddSingleton<IProfiAccountsService>(provider => new ProfiAccountsService(
                    profiBotServerSettings,
                    new NuciApiClient(profiBotServerSettings.BaseUrl)))
                .AddSingleton<ILogger, NuciLogger>()
                .BuildServiceProvider();
        }
    }
}
