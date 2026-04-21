using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using PersonalDataLogger.Configuration;
using PersonalDataLogger.Service;

using NuciLog;
using NuciLog.Configuration;
using NuciLog.Core;
using PersonalDataLogger.Service.Processors;
using PersonalDataLogger.Client;

namespace PersonalDataLogger
{
    public sealed class Program
    {
        static ILogger logger;

        static IServiceProvider serviceProvider;

        static void Main(string[] args)
        {
            serviceProvider = CreateIOC();
            logger = serviceProvider.GetService<ILogger>();
            IEmailWorker service = serviceProvider.GetService<IEmailWorker>();

            logger.Info(Operation.StartUp, "The service has started.");

            try
            {
                service.WatchEmails();
            }
            catch (AggregateException ex)
            {
                LogInnerExceptions(ex);
            }
            catch (Exception ex)
            {
                logger.Fatal(Operation.Unknown, OperationStatus.Failure, ex);
            }
            finally
            {
                logger.Info(Operation.ShutDown, "The service has stopped.");
            }
        }

        static IServiceProvider CreateIOC()
        {
            PersonalLogManagerSettings personalLogManagerSettings = new();
            ImapSettings imapSettings = new();
            NuciLoggerSettings loggerSettings = new();

            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            config.Bind(nameof(PersonalLogManagerSettings), personalLogManagerSettings);
            config.Bind(nameof(ImapSettings), imapSettings);
            config.Bind(nameof(NuciLoggerSettings), loggerSettings);

            return new ServiceCollection()
                .AddSingleton(personalLogManagerSettings)
                .AddSingleton(imapSettings)
                .AddSingleton(loggerSettings)
                .AddSingleton<IOpsGenieEmailProcessor, OpsGenieEmailProcessor>()
                .AddSingleton<IEmailProcessor, EmailProcessor>()
                .AddSingleton<IEmailWorker, EmailWorker>()
                .AddSingleton<IPersonalLogManagerService, PersonalLogManagerService>()
                .AddSingleton<ILogger, NuciLogger>()
                .BuildServiceProvider();
        }

        static void LogInnerExceptions(AggregateException exception)
        {
            foreach (Exception innerException in exception.InnerExceptions)
            {
                if (innerException is not AggregateException innerAggregateException)
                {
                    logger.Fatal(Operation.Unknown, OperationStatus.Failure, innerException);
                }
                else
                {
                    LogInnerExceptions(innerAggregateException);
                }
            }
        }
    }
}
