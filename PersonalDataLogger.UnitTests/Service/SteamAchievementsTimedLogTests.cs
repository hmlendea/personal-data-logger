using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Moq;

using NUnit.Framework;

using NuciLog.Core;

using PersonalDataLogger.Client;
using PersonalDataLogger.Configuration;
using PersonalDataLogger.Service;

namespace PersonalDataLogger.UnitTests.Service
{
    [TestFixture]
    public sealed class SteamAchievementsTimedLogTests
    {
        private static string TemplateName => "GameAchievementUnlock";

        private Mock<ISteamService> mockSteamService = null!;
        private Mock<IPersonalLogManagerService> mockPersonalLogManagerService = null!;
        private Mock<ILogger> mockLogger = null!;
        private SteamSettings settings = null!;
        private SteamAchievementsTimedLog timedLog = null!;

        [SetUp]
        public void SetUp()
        {
            mockSteamService = new Mock<ISteamService>();
            mockPersonalLogManagerService = new Mock<IPersonalLogManagerService>();
            mockLogger = new Mock<ILogger>();
            settings = new SteamSettings
            {
                ApiKey = "test-api-key",
                Username = "solaire_of_astora",
                ScheduledHours = "07:00"
            };

            mockSteamService
                .Setup(service => service.GetAchievementsUnlockedSince(It.IsAny<DateTimeOffset?>()))
                .ReturnsAsync([]);

            timedLog = new SteamAchievementsTimedLog(
                mockSteamService.Object,
                mockPersonalLogManagerService.Object,
                settings,
                mockLogger.Object);
        }

        [Test]
        public async Task GivenALatestLoggedAchievement_WhenExecuting_ThenItsTimestampIsUsedAsTheCutoff()
        {
            DateTimeOffset latestLogTimestamp = new(2025, 11, 5, 12, 10, 0, TimeSpan.FromHours(2));
            mockPersonalLogManagerService
                .Setup(service => service.GetLatestLogTimestamp(
                    TemplateName,
                    "Steam",
                    settings.Username))
                .ReturnsAsync(latestLogTimestamp);

            await timedLog.Execute();

            mockSteamService.Verify(
                service => service.GetAchievementsUnlockedSince(latestLogTimestamp),
                Times.Once);
        }

        [Test]
        public async Task GivenNoPreviouslyLoggedAchievement_WhenExecuting_ThenTheCutoffIsAbsent()
        {
            mockPersonalLogManagerService
                .Setup(service => service.GetLatestLogTimestamp(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync((DateTimeOffset?)null);

            await timedLog.Execute();

            mockSteamService.Verify(
                service => service.GetAchievementsUnlockedSince(null),
                Times.Once);
        }

        [Test]
        public async Task GivenUnlockedAchievements_WhenExecuting_ThenEachAchievementIsLogged()
        {
            DateTimeOffset firstUnlock = new(2025, 11, 5, 12, 10, 0, TimeSpan.FromHours(2));
            DateTimeOffset secondUnlock = new(2025, 11, 6, 9, 30, 0, TimeSpan.FromHours(2));
            mockSteamService
                .Setup(service => service.GetAchievementsUnlockedSince(It.IsAny<DateTimeOffset?>()))
                .ReturnsAsync(
                [
                    new SteamAchievementUnlock
                    {
                        AchievementName = "Super Soldier",
                        GameName = "eRepublik",
                        UnlockedAt = firstUnlock
                    },
                    new SteamAchievementUnlock
                    {
                        AchievementName = "Game Over!",
                        GameName = "Tiny Life",
                        UnlockedAt = secondUnlock
                    }
                ]);

            await timedLog.Execute();

            VerifyAchievementLog("Super Soldier", "eRepublik", firstUnlock.ToUnixTimeSeconds());
            VerifyAchievementLog("Game Over!", "Tiny Life", secondUnlock.ToUnixTimeSeconds());
        }

        [Test]
        public async Task GivenNoUnlockedAchievements_WhenExecuting_ThenNothingIsLogged()
        {
            await timedLog.Execute();

            mockPersonalLogManagerService.Verify(
                service => service.SendPersonalLogToManager(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Never);
        }

        [Test]
        public async Task GivenACorrectExecution_WhenExecuting_ThenTheAchievementCountIsLogged()
        {
            mockSteamService
                .Setup(service => service.GetAchievementsUnlockedSince(It.IsAny<DateTimeOffset?>()))
                .ReturnsAsync(
                [
                    new SteamAchievementUnlock
                    {
                        AchievementName = "Super Soldier",
                        GameName = "eRepublik",
                        UnlockedAt = DateTimeOffset.FromUnixTimeSeconds(1800000000)
                    }
                ]);

            await timedLog.Execute();

            bool achievementCountWasLogged = mockLogger.Invocations
                .Where(invocation => invocation.Arguments.Count > 2 &&
                    invocation.Arguments[2] is LogInfo[])
                .SelectMany(invocation => (LogInfo[])invocation.Arguments[2]!)
                .Any(logInfo => string.Equals(
                    logInfo.Key.Name,
                    "AchievementCount",
                    StringComparison.Ordinal));

            Assert.That(achievementCountWasLogged);
        }

        [Test]
        public void GivenAnAchievementServiceFailure_WhenExecuting_ThenTheExceptionPropagates()
        {
            mockSteamService
                .Setup(service => service.GetAchievementsUnlockedSince(It.IsAny<DateTimeOffset?>()))
                .ThrowsAsync(new HttpRequestException("The Steam Web API failed."));

            Assert.That(
                async () => await timedLog.Execute(),
                Throws.InstanceOf<HttpRequestException>());
        }

        [Test]
        public void GivenAConfiguredTimeBeforeTheScheduledHour_WhenGettingNextExecution_ThenTheSameDayIsReturned()
        {
            DateTime currentTime = new(2026, 9, 8, 6, 0, 0);
            DateTimeOffset current = new(currentTime, TimeZoneInfo.Local.GetUtcOffset(currentTime));

            DateTimeOffset nextExecution = timedLog.GetNextExecution(current);

            Assert.That(nextExecution.LocalDateTime, Is.EqualTo(new DateTime(2026, 9, 8, 7, 0, 0)));
        }

        [Test]
        public void GivenAConfiguredTimeAfterTheScheduledHour_WhenGettingNextExecution_ThenTheFollowingDayIsReturned()
        {
            DateTime currentTime = new(2026, 9, 8, 8, 0, 0);
            DateTimeOffset current = new(currentTime, TimeZoneInfo.Local.GetUtcOffset(currentTime));

            DateTimeOffset nextExecution = timedLog.GetNextExecution(current);

            Assert.That(nextExecution.LocalDateTime, Is.EqualTo(new DateTime(2026, 9, 9, 7, 0, 0)));
        }

        private void VerifyAchievementLog(
            string achievementName,
            string gameName,
            long unlockedAt)
        {
            mockPersonalLogManagerService.Verify(
                service => service.SendPersonalLogToManager(
                    It.Is<DateTimeOffset>(timestamp => timestamp.ToUnixTimeSeconds() == unlockedAt),
                    TemplateName,
                    It.Is<Dictionary<string, string>>(data =>
                        data["achievement_name"] == achievementName &&
                        data["game_name"] == gameName &&
                        data["platform"] == "Steam" &&
                        data["username"] == settings.Username)),
                Times.Once);
        }
    }
}