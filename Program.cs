using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using PersonalDataLogger.Configuration;
using PersonalDataLogger.Service;

using NuciLog;
using NuciLog.Configuration;
using NuciLog.Core;

namespace PersonalDataLogger
{
    public sealed class Program
    {
        static BotSettings botSettings;
        static ImapSettings imapSettings;
        static NuciLoggerSettings loggerSettings;

        static ILogger logger;

        static IServiceProvider serviceProvider;

        static void Main(string[] args)
        {
            LoadConfiguration();

            serviceProvider = CreateIOC();
            logger = serviceProvider.GetService<ILogger>();
            IEmailWatcher service = serviceProvider.GetService<IEmailWatcher>();

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

        static IConfiguration LoadConfiguration()
        {
            botSettings = new BotSettings();
            imapSettings = new ImapSettings();
            loggerSettings = new NuciLoggerSettings();

            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            config.Bind(nameof(BotSettings), botSettings);
            config.Bind(nameof(ImapSettings), imapSettings);
            config.Bind(nameof(NuciLoggerSettings), loggerSettings);

            return config;
        }

        static IServiceProvider CreateIOC()
        {
            return new ServiceCollection()
                .AddSingleton(botSettings)
                .AddSingleton(imapSettings)
                .AddSingleton(loggerSettings)
                .AddSingleton<ILogger, NuciLogger>()
                .AddSingleton<IHouseholdConfirmator, HouseholdConfirmator>()
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
