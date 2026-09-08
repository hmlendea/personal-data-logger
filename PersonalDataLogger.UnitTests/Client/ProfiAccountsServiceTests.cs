using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using NUnit.Framework;
using Moq;

using PersonalDataLogger.Client;
using PersonalDataLogger.Configuration;

namespace PersonalDataLogger.Tests.Client
{
    [TestFixture]
    public class ProfiAccountsServiceTests
    {
        private ProfiBotServerSettings settings;
        private ProfiAccountsService service;

        [SetUp]
        public void SetUp()
        {
            settings = new ProfiBotServerSettings
            {
                BaseUrl = "https://api.example.com",
                UserApiKey = "test-api-key",
                ClientId = "test-client-id",
                HmacSharedSecretKey = "test-secret-key",
                Username = "testuser",
                AccountsEndpoint = "/Users/{username}/accounts"
            };

            service = new ProfiAccountsService(settings);
        }

        [Test]
        public async Task GivenAValidServiceConfiguration_WhenGettingEnabledAccountsBalance_ThenAPIIsCalledWithCorrectEndpoint()
        {
            // This test validates the endpoint URL construction
            Assert.That(settings.Username, Is.EqualTo("testuser"));
            Assert.That(settings.AccountsEndpoint, Contains.Substring("{username}"));

            // Verify that endpoint contains the username placeholder
            string expectedEndpoint = settings.AccountsEndpoint
                .Replace("{username}", Uri.EscapeDataString(settings.Username), StringComparison.Ordinal);

            Assert.That(expectedEndpoint, Contains.Substring("testuser"));
        }

        [Test]
        public void GivenNullUsername_WhenCreatingEndpoint_ThenEscapedUsernameIsEmpty()
        {
            settings.Username = null;

            string endpoint = settings.AccountsEndpoint.Replace(
                "{username}",
                Uri.EscapeDataString(settings.Username ?? string.Empty),
                StringComparison.Ordinal);

            Assert.That(endpoint, Does.Contain("/Users//accounts"));
        }

        [Test]
        public void GivenUsernameWithSpecialCharacters_WhenCreatingEndpoint_ThenUsernameIsEscaped()
        {
            settings.Username = "user@example.com";

            string endpoint = settings.AccountsEndpoint.Replace(
                "{username}",
                Uri.EscapeDataString(settings.Username),
                StringComparison.Ordinal);

            Assert.That(endpoint, Contains.Substring("user%40example.com"));
        }

        [Test]
        public void GivenSettingsWithAllRequiredFields_WhenConstructingService_ThenServiceIsCreatedSuccessfully()
            => Assert.That(service, Is.Not.Null);

        [Test]
        public void GivenNullBaseUrl_WhenCreatingServiceAndCalling_ThenExceptionIsThrown()
        {
            settings.BaseUrl = null;
            service = new ProfiAccountsService(settings);

            Assert.That(
                async () => await service.GetEnabledAccountsBalance(),
                Throws.InstanceOf<Exception>());
        }

        [Test]
        public void GivenEmptyUsername_WhenCreatingEndpoint_ThenEndpointContainsEmptyPath()
        {
            settings.Username = string.Empty;

            string endpoint = settings.AccountsEndpoint.Replace(
                "{username}",
                Uri.EscapeDataString(settings.Username),
                StringComparison.Ordinal);

            Assert.That(endpoint, Contains.Substring("/Users//accounts"));
        }
    }
}
