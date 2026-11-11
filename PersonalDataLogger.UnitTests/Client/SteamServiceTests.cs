using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Moq;

using NUnit.Framework;

using NuciLog.Core;

using PersonalDataLogger.Client;
using PersonalDataLogger.Configuration;

namespace PersonalDataLogger.UnitTests.Client
{
    [TestFixture]
    public sealed class SteamServiceTests
    {
        private static string VanityUrlResponse =>
            """{"response":{"success":1,"steamid":"76561198000000000"}}""";
        private static string FailedVanityUrlResponse =>
            """{"response":{"success":42,"message":"No match"}}""";

        private Mock<ILogger> mockLogger = null!;
        private SteamSettings settings = null!;

        [SetUp]
        public void SetUp()
        {
            mockLogger = new Mock<ILogger>();
            settings = new SteamSettings
            {
                ApiKey = "test-api-key",
                Username = "solaire_of_astora",
                ScheduledHours = "07:00"
            };
        }

        [Test]
        public async Task GivenAValidAccount_WhenGettingAchievementsUnlockedSince_ThenUnlockedAchievementsAreReturned()
        {
            SteamService service = CreateService(
                VanityUrlResponse,
                OwnedGamesResponse(440, "Team Fortress 2"),
                PlayerAchievementsResponse(("WIN_100", "Win 100 rounds", 1, 1700000000)));

            IEnumerable<SteamAchievementUnlock> achievements =
                await service.GetAchievementsUnlockedSince(null);
            SteamAchievementUnlock achievement = achievements.Single();

            Assert.That(achievement.AchievementName, Is.EqualTo("Win 100 rounds"));
            Assert.That(achievement.GameName, Is.EqualTo("Team Fortress 2"));
            Assert.That(
                achievement.UnlockedAt.ToUnixTimeSeconds(),
                Is.EqualTo(1700000000));
        }

        [Test]
        public async Task GivenACutoffTimestamp_WhenGettingAchievementsUnlockedSince_ThenEarlierAchievementsAreExcluded()
        {
            SteamService service = CreateService(
                VanityUrlResponse,
                OwnedGamesResponse(440, "Team Fortress 2"),
                PlayerAchievementsResponse(
                    ("EARLY", "Earlier achievement", 1, 1600000000),
                    ("LATE", "Later achievement", 1, 1800000000)));

            DateTimeOffset cutoffTimestamp = DateTimeOffset.FromUnixTimeSeconds(1700000000);
            SteamAchievementUnlock[] achievements = (await service
                .GetAchievementsUnlockedSince(cutoffTimestamp))
                .ToArray();

            Assert.That(achievements, Has.Length.EqualTo(1));
            Assert.That(achievements[0].AchievementName, Is.EqualTo("Later achievement"));
        }

        [Test]
        public async Task GivenAnAchievementUnlockedInTheCutoffMinute_WhenGettingAchievementsUnlockedSince_ThenItIsExcluded()
        {
            const long cutoffSeconds = 1700000060;
            SteamService service = CreateService(
                VanityUrlResponse,
                OwnedGamesResponse(440, "Team Fortress 2"),
                PlayerAchievementsResponse(
                    ("SAME_MINUTE", "Same minute achievement", 1, cutoffSeconds),
                    ("NEXT_MINUTE", "Next minute achievement", 1, cutoffSeconds + 60)));

            SteamAchievementUnlock[] achievements = (await service
                .GetAchievementsUnlockedSince(DateTimeOffset.FromUnixTimeSeconds(cutoffSeconds)))
                .ToArray();

            Assert.That(achievements, Has.Length.EqualTo(1));
            Assert.That(achievements[0].AchievementName, Is.EqualTo("Next minute achievement"));
        }

        [Test]
        public async Task GivenALockedAchievement_WhenGettingAchievementsUnlockedSince_ThenItIsExcluded()
        {
            SteamService service = CreateService(
                VanityUrlResponse,
                OwnedGamesResponse(440, "Team Fortress 2"),
                PlayerAchievementsResponse(
                    ("LOCKED", "Locked achievement", 0, 0),
                    ("UNLOCKED", "Unlocked achievement", 1, 1800000000)));

            SteamAchievementUnlock[] achievements = (await service
                .GetAchievementsUnlockedSince(null))
                .ToArray();

            Assert.That(achievements, Has.Length.EqualTo(1));
            Assert.That(achievements[0].AchievementName, Is.EqualTo("Unlocked achievement"));
        }

        [Test]
        public async Task GivenAGameNotPlayedSinceTheCutoff_WhenGettingAchievementsUnlockedSince_ThenItsAchievementsAreNotRequested()
        {
            List<string> requestedUrls = [];
            SteamService service = CreateService(
                requestedUrls,
                VanityUrlResponse,
                OwnedGamesResponse(440, "Team Fortress 2", 1600000000),
                PlayerAchievementsResponse(("EARLY", "Earlier achievement", 1, 1600000000)));

            DateTimeOffset cutoffTimestamp = DateTimeOffset.FromUnixTimeSeconds(1700000000);
            SteamAchievementUnlock[] achievements = (await service
                .GetAchievementsUnlockedSince(cutoffTimestamp))
                .ToArray();

            Assert.That(achievements, Is.Empty);
            Assert.That(
                requestedUrls.Any(url => url.Contains("GetPlayerAchievements", StringComparison.Ordinal)),
                Is.False);
        }

        [Test]
        public async Task GivenAMissingAchievementName_WhenGettingAchievementsUnlockedSince_ThenTheApiNameIsUsed()
        {
            SteamService service = CreateService(
                VanityUrlResponse,
                OwnedGamesResponse(440, "Team Fortress 2"),
                PlayerAchievementsResponse(("ACH_WIN_100", null, 1, 1800000000)));

            SteamAchievementUnlock achievement = (await service
                .GetAchievementsUnlockedSince(null))
                .Single();

            Assert.That(achievement.AchievementName, Is.EqualTo("ACH_WIN_100"));
        }

        [Test]
        public void GivenAnUnresolvedUsername_WhenGettingAchievementsUnlockedSince_ThenAnExceptionIsThrown()
        {
            SteamService service = CreateService(FailedVanityUrlResponse);

            Assert.That(
                async () => await service.GetAchievementsUnlockedSince(null),
                Throws.InstanceOf<HttpRequestException>());
        }

        [Test]
        public async Task GivenAFailingAchievementRequestForOneGame_WhenGettingAchievementsUnlockedSince_ThenOtherGamesAreStillProcessed()
        {
            SteamService service = CreateService(
                VanityUrlResponse,
                OwnedGamesResponse(
                    (440, "Team Fortress 2", 1800000000),
                    (570, "Dota 2", 1800000000)),
                "Gone",
                PlayerAchievementsResponse(("WIN_100", "Win 100 rounds", 1, 1800000000)));

            SteamAchievementUnlock[] achievements = (await service
                .GetAchievementsUnlockedSince(null))
                .ToArray();

            Assert.That(achievements, Has.Length.EqualTo(1));
            Assert.That(achievements[0].GameName, Is.EqualTo("Dota 2"));
        }

        [Test]
        public async Task GivenAValidRun_WhenGettingAchievementsUnlockedSince_ThenStartAndSuccessAreLogged()
        {
            SteamService service = CreateService(
                VanityUrlResponse,
                OwnedGamesResponse(440, "Team Fortress 2"),
                PlayerAchievementsResponse(("WIN_100", "Win 100 rounds", 1, 1800000000)));

            await service.GetAchievementsUnlockedSince(null);

            mockLogger.Verify(
                logger => logger.Info(
                    It.IsAny<Operation>(),
                    It.IsAny<OperationStatus>(),
                    It.IsAny<IEnumerable<LogInfo>>()),
                Times.Exactly(2));
        }

        private SteamService CreateService(params string[] responses)
            => CreateService([], responses);

        private SteamService CreateService(List<string> requestedUrls, params string[] responses)
            => new(
                settings,
                new HttpClient(new StubHttpMessageHandler(requestedUrls, responses)),
                mockLogger.Object);

        private static string OwnedGamesResponse(
            int appIdentifier,
            string gameName)
            => OwnedGamesResponse(appIdentifier, gameName, 1800000000);

        private static string OwnedGamesResponse(
            params (int AppIdentifier, string GameName, long LastPlayedTime)[] games)
        {
            IEnumerable<string> serialisedGames = games.Select(game =>
                $$$"""
                {"appid":{{{game.AppIdentifier}}},"name":"{{{game.GameName}}}","rtime_last_played":{{{game.LastPlayedTime}}}}
                """);

            return $$$"""
                {"response":{"games":[{{{string.Join(",", serialisedGames)}}}]}}
                """;
        }

        private static string OwnedGamesResponse(
            int appIdentifier,
            string gameName,
            long lastPlayedTime)
            => $$$"""
                {"response":{"games":[
                    {"appid":{{{appIdentifier}}},"name":"{{{gameName}}}","rtime_last_played":{{{lastPlayedTime}}}}
                ]}}
                """;

        private static string PlayerAchievementsResponse(
            params (string ApiName, string? Name, int Achieved, long UnlockTime)[] achievements)
        {
            IEnumerable<string> serialisedAchievements = achievements.Select(achievement =>
                $$$"""
                {"apiname":"{{{achievement.ApiName}}}","achieved":{{{achievement.Achieved}}},"unlocktime":{{{achievement.UnlockTime}}},"name":{{{SerialiseName(achievement.Name)}}}}
                """);

            return $$$"""
                {"playerstats":{"achievements":[{{{string.Join(",", serialisedAchievements)}}}]}}
                """;
        }

        private static string SerialiseName(string? name)
        {
            if (name is null)
            {
                return "null";
            }

            return $"\"{name}\"";
        }

        private sealed class StubHttpMessageHandler(
            List<string> requestedUrls,
            string[] responses) : HttpMessageHandler
        {
            private int responseIndex;

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                requestedUrls.Add(request.RequestUri!.ToString());

                string content = responses[0];

                if (responses.Length != 1)
                {
                    content = responses[Math.Min(responseIndex, responses.Length - 1)];
                }

                responseIndex += 1;

                if (string.Equals(content, "Gone", StringComparison.Ordinal))
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(content)
                });
            }
        }
    }
}