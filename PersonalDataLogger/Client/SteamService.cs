using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using NuciLog.Core;
using NuciWeb.HTTP;

using PersonalDataLogger.Configuration;
using PersonalDataLogger.Logging;

namespace PersonalDataLogger.Client
{
    public sealed class SteamService(
        SteamSettings settings,
        ILogger logger) : ISteamService
    {
        private static string ApiBaseUrl => "https://api.steampowered.com";
        private static string OwnedGamesUrl => $"{ApiBaseUrl}/IPlayerService/GetOwnedGames/v1/";
        private static string PlayerAchievementsUrl =>
            $"{ApiBaseUrl}/ISteamUserStats/GetPlayerAchievements/v1/";
        private static string VanityUrl => $"{ApiBaseUrl}/ISteamUser/ResolveVanityURL/v1/";
        private static string JsonFormat => "json";
        private static int AchievedState => 1;
        private static int SuccessfulVanityUrlState => 1;
        private static int SecondsPerMinute => 60;
        private static int RecentPlayLookbackInSeconds => 86400;
        private static string InvalidUsernameMessage =>
            "The Steam Web API could not resolve the configured username.";
        private static string MissingResponseMessage =>
            "The Steam Web API did not return a response.";

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private HttpClient httpClient;

        public SteamService(
            SteamSettings settings,
            HttpClient httpClient,
            ILogger logger)
            : this(settings, logger)
        {
            this.httpClient = httpClient;
        }

        private HttpClient ApiClient => httpClient ??= HttpClientCreator.Create();

        public async Task<IEnumerable<SteamAchievementUnlock>> GetAchievementsUnlockedSince(
            DateTimeOffset? cutoffTimestamp)
        {
            IEnumerable<LogInfo> logInfos =
            [
                new(MyLogInfoKey.Username, settings.Username)
            ];

            if (cutoffTimestamp is not null)
            {
                logInfos = logInfos.Append(new(MyLogInfoKey.Date, cutoffTimestamp));
            }

            logger.Info(
                MyOperation.GetSteamAchievements,
                OperationStatus.Started,
                logInfos);

            string steamIdentifier = await GetSteamIdentifier();
            IEnumerable<SteamGame> games = await GetOwnedGames(steamIdentifier);
            List<SteamAchievementUnlock> achievements = [];

            foreach (SteamGame game in games.Where(game => IsRecentlyPlayed(game, cutoffTimestamp)))
            {
                achievements.AddRange(
                    await GetUnlockedAchievementsOrEmpty(steamIdentifier, game, cutoffTimestamp));
            }

            logInfos = logInfos
                .Append(new(MyLogInfoKey.GameCount, games.Count()))
                .Append(new(MyLogInfoKey.AchievementCount, achievements.Count));

            logger.Info(
                MyOperation.GetSteamAchievements,
                OperationStatus.Success,
                logInfos);

            return achievements;
        }

        private static bool IsRecentlyPlayed(SteamGame game, DateTimeOffset? cutoffTimestamp)
        {
            if (cutoffTimestamp is null)
            {
                return true;
            }

            long cutoffSeconds = cutoffTimestamp.Value.ToUnixTimeSeconds() - RecentPlayLookbackInSeconds;

            return game.LastPlayedTime >= cutoffSeconds;
        }

        private async Task<IEnumerable<SteamAchievementUnlock>> GetUnlockedAchievementsOrEmpty(
            string steamIdentifier,
            SteamGame game,
            DateTimeOffset? cutoffTimestamp)
        {
            try
            {
                return await GetUnlockedAchievements(steamIdentifier, game, cutoffTimestamp);
            }
            catch (HttpRequestException exception)
            {
                logger.Warn(
                    MyOperation.GetSteamAchievements,
                    OperationStatus.InProgress,
                    "The Steam Web API did not return achievements for a game. Skipping it.",
                    exception,
                    [new(MyLogInfoKey.AppIdentifier, game.AppIdentifier)]);

                return [];
            }
        }

        private async Task<IEnumerable<SteamAchievementUnlock>> GetUnlockedAchievements(
            string steamIdentifier,
            SteamGame game,
            DateTimeOffset? cutoffTimestamp)
        {
            SteamPlayerAchievementsResponse response = await SendRequest<SteamPlayerAchievementsResponse>(
                BuildPlayerAchievementsUrl(steamIdentifier, game));

            if (response?.Response?.Achievements is null)
            {
                return [];
            }

            return response.Response.Achievements
                .Where(achievement => IsUnlockedAfter(achievement, cutoffTimestamp))
                .Select(achievement => new SteamAchievementUnlock
                {
                    AchievementName = GetAchievementName(achievement),
                    GameName = game.Name,
                    UnlockedAt = DateTimeOffset.FromUnixTimeSeconds(achievement.UnlockTime)
                })
                .ToArray();
        }

        private static bool IsUnlockedAfter(
            SteamAchievement achievement,
            DateTimeOffset? cutoffTimestamp)
        {
            if (achievement.Achieved != AchievedState)
            {
                return false;
            }

            if (cutoffTimestamp is null)
            {
                return true;
            }

            long cutoffMinute = cutoffTimestamp.Value.ToUnixTimeSeconds() / SecondsPerMinute;
            long unlockMinute = achievement.UnlockTime / SecondsPerMinute;

            return unlockMinute > cutoffMinute;
        }

        private async Task<string> GetSteamIdentifier()
        {
            SteamVanityUrlResponse response = await SendRequest<SteamVanityUrlResponse>(
                BuildVanityUrlUrl());

            if (response?.Response?.Success != SuccessfulVanityUrlState ||
                string.IsNullOrWhiteSpace(response.Response.SteamIdentifier))
            {
                throw new HttpRequestException(InvalidUsernameMessage);
            }

            return response.Response.SteamIdentifier;
        }

        private async Task<IEnumerable<SteamGame>> GetOwnedGames(string steamIdentifier)
        {
            SteamOwnedGamesResponse response = await SendRequest<SteamOwnedGamesResponse>(
                BuildOwnedGamesUrl(steamIdentifier));

            if (response?.Response?.Games is null)
            {
                return [];
            }

            return response.Response.Games;
        }

        private async Task<TResponse> SendRequest<TResponse>(string url)
        {
            string responseContent = await ApiClient.GetStringAsync(url);
            TResponse response = JsonSerializer.Deserialize<TResponse>(responseContent, SerializerOptions);

            if (response is null)
            {
                throw new HttpRequestException(MissingResponseMessage);
            }

            return response;
        }

        private string BuildOwnedGamesUrl(string steamIdentifier)
            => $"{OwnedGamesUrl}?key={Uri.EscapeDataString(settings.ApiKey)}" +
                $"&steamid={Uri.EscapeDataString(steamIdentifier)}" +
                "&include_appinfo=1&include_played_free_games=1" +
                $"&format={JsonFormat}";

        private string BuildPlayerAchievementsUrl(string steamIdentifier, SteamGame game)
            => $"{PlayerAchievementsUrl}?key={Uri.EscapeDataString(settings.ApiKey)}" +
                $"&steamid={Uri.EscapeDataString(steamIdentifier)}" +
                $"&appid={game.AppIdentifier.ToString(CultureInfo.InvariantCulture)}" +
                $"&format={JsonFormat}";

        private string BuildVanityUrlUrl()
            => $"{VanityUrl}?key={Uri.EscapeDataString(settings.ApiKey)}" +
                $"&vanityurl={Uri.EscapeDataString(settings.Username)}" +
                $"&format={JsonFormat}";

        private static string GetAchievementName(SteamAchievement achievement)
        {
            if (string.IsNullOrWhiteSpace(achievement.Name))
            {
                return achievement.ApiName;
            }

            return achievement.Name;
        }
    }
}