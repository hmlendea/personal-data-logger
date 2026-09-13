using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using NUnit.Framework;
using Moq;

using NuciAPI.Client;
using NuciAPI.Responses;

using NuciLog.Core;

using PersonalDataLogger.Client;
using PersonalDataLogger.Configuration;

namespace PersonalDataLogger.UnitTests.Client
{
    [TestFixture]
    public class ProfiAccountsServiceTests
    {
        private ProfiBotServerSettings settings;
        private Mock<INuciApiClient> mockApiClient;
        private Mock<ILogger> mockLogger;
        private ProfiAccountsService service;

        [SetUp]
        public void SetUp()
        {
            settings = new ProfiBotServerSettings
            {
                AccountName = "Solaire of Astora",
                BaseUrl = "https://api.example.com",
                UserApiKey = "test-api-key",
                ClientId = "test-client-id",
                HmacSharedSecretKey = "test-secret-key",
                Username = "testuser",
                AccountsEndpoint = "/Users/{username}/accounts"
            };

            mockApiClient = new Mock<INuciApiClient>();
            mockLogger = new Mock<ILogger>();
            service = new ProfiAccountsService(
                settings,
                mockApiClient.Object,
                mockLogger.Object);
        }

        [Test]
        public async Task GivenAValidServiceConfiguration_WhenGettingEnabledAccountsBalance_ThenAPIIsCalledWithCorrectEndpoint()
        {
            mockApiClient
                .Setup(client => client.SendRequestAsync<GetProfiAccountsRequest, GetProfiAccountsResponse>(
                    HttpMethod.Get,
                    It.IsAny<GetProfiAccountsRequest>(),
                    It.IsAny<NuciApiRequestAuthorisationInfo>(),
                    "/Users/testuser/accounts"))
                .ReturnsAsync(new GetProfiAccountsResponse());

            await service.GetEnabledAccountsBalance();

            mockApiClient.Verify(
                client => client.SendRequestAsync<GetProfiAccountsRequest, GetProfiAccountsResponse>(
                    HttpMethod.Get,
                    It.IsAny<GetProfiAccountsRequest>(),
                    It.IsAny<NuciApiRequestAuthorisationInfo>(),
                    "/Users/testuser/accounts"),
                Times.Once);
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
        public void GivenACompleteProfiBotServerConfiguration_WhenCheckingConfiguration_ThenItIsConfigured()
            => Assert.That(settings.IsConfigured);

        [Test]
        public void GivenAnUnresolvedProfiBotServerConfigurationValue_WhenCheckingConfiguration_ThenItIsNotConfigured()
        {
            settings.UserApiKey = "[[PROFI_BOT_SERVER_USER_API_KEY]]";

            Assert.That(settings.IsConfigured, Is.False);
        }

        [Test]
        public void GivenAMissingProfiBotServerConfigurationValue_WhenCheckingConfiguration_ThenItIsNotConfigured()
        {
            settings.AccountsEndpoint = string.Empty;

            Assert.That(settings.IsConfigured, Is.False);
        }

        [Test]
        public async Task GivenAccountsResponseFromApiClient_WhenGettingEnabledAccountsBalance_ThenOnlyEnabledAccountBalancesAreSummed()
        {
            mockApiClient
                .Setup(client => client.SendRequestAsync<GetProfiAccountsRequest, GetProfiAccountsResponse>(
                    It.IsAny<HttpMethod>(),
                    It.IsAny<GetProfiAccountsRequest>(),
                    It.IsAny<NuciApiRequestAuthorisationInfo>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new GetProfiAccountsResponse
                {
                    Accounts =
                    [
                        new() { Balance = 10.25m, IsEnabled = true },
                        new() { Balance = 200.50m, IsEnabled = false },
                        new() { Balance = 3.75m, IsEnabled = true }
                    ]
                });

            decimal balance = await service.GetEnabledAccountsBalance();

            Assert.That(balance, Is.EqualTo(14.00m));
        }

        [Test]
        public async Task GivenAValidAccountsResponse_WhenGettingEnabledAccountsBalance_ThenEachStageIsLogged()
        {
            mockApiClient
                .Setup(client => client.SendRequestAsync<GetProfiAccountsRequest, GetProfiAccountsResponse>(
                    It.IsAny<HttpMethod>(),
                    It.IsAny<GetProfiAccountsRequest>(),
                    It.IsAny<NuciApiRequestAuthorisationInfo>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new GetProfiAccountsResponse
                {
                    Accounts =
                    [
                        new() { Balance = 3.14m, IsEnabled = true },
                        new() { Balance = 6.13m, IsEnabled = false }
                    ]
                });

            await service.GetEnabledAccountsBalance();

            mockLogger.Verify(
                logger => logger.Info(
                    It.IsAny<Operation>(),
                    It.IsAny<OperationStatus>(),
                    It.IsAny<IEnumerable<LogInfo>>()),
                Times.Exactly(4));
        }

        [Test]
        public void GivenApiClientFailure_WhenGettingEnabledAccountsBalance_ThenExceptionIsThrown()
        {
            mockApiClient
                .Setup(client => client.SendRequestAsync<GetProfiAccountsRequest, GetProfiAccountsResponse>(
                    It.IsAny<HttpMethod>(),
                    It.IsAny<GetProfiAccountsRequest>(),
                    It.IsAny<NuciApiRequestAuthorisationInfo>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("The API request failed."));

            Assert.That(
                async () => await service.GetEnabledAccountsBalance(),
                Throws.InstanceOf<HttpRequestException>());

            VerifyFailureWasLogged();
        }

        [Test]
        public void GivenAnUnexpectedSuccessfulResponse_WhenGettingEnabledAccountsBalance_ThenFailureIsLogged()
        {
            mockApiClient
                .Setup(client => client.SendRequestAsync<GetProfiAccountsRequest, GetProfiAccountsResponse>(
                    It.IsAny<HttpMethod>(),
                    It.IsAny<GetProfiAccountsRequest>(),
                    It.IsAny<NuciApiRequestAuthorisationInfo>(),
                    It.IsAny<string>()))
                .ReturnsAsync(NuciApiSuccessResponse.Default);

            Assert.That(
                async () => await service.GetEnabledAccountsBalance(),
                Throws.InstanceOf<HttpRequestException>());

            VerifyFailureWasLogged();
        }

        [Test]
        public void GivenARejectedAccountsResponse_WhenGettingEnabledAccountsBalance_ThenFailureIsLogged()
        {
            mockApiClient
                .Setup(client => client.SendRequestAsync<GetProfiAccountsRequest, GetProfiAccountsResponse>(
                    It.IsAny<HttpMethod>(),
                    It.IsAny<GetProfiAccountsRequest>(),
                    It.IsAny<NuciApiRequestAuthorisationInfo>(),
                    It.IsAny<string>()))
                .ReturnsAsync(NuciApiErrorResponse.BadRequest);

            Assert.That(
                async () => await service.GetEnabledAccountsBalance(),
                Throws.InstanceOf<HttpRequestException>());

            VerifyFailureWasLogged();
        }

        [Test]
        public void GivenANullAccountsResponse_WhenGettingEnabledAccountsBalance_ThenFailureIsLogged()
        {
            mockApiClient
                .Setup(client => client.SendRequestAsync<GetProfiAccountsRequest, GetProfiAccountsResponse>(
                    It.IsAny<HttpMethod>(),
                    It.IsAny<GetProfiAccountsRequest>(),
                    It.IsAny<NuciApiRequestAuthorisationInfo>(),
                    It.IsAny<string>()))
                .ReturnsAsync((NuciApiResponse)null);

            Assert.That(
                async () => await service.GetEnabledAccountsBalance(),
                Throws.InstanceOf<HttpRequestException>());

            VerifyFailureWasLogged();
        }

        [Test]
        public void GivenAResponseWithoutAccounts_WhenGettingEnabledAccountsBalance_ThenFailureIsLogged()
        {
            mockApiClient
                .Setup(client => client.SendRequestAsync<GetProfiAccountsRequest, GetProfiAccountsResponse>(
                    It.IsAny<HttpMethod>(),
                    It.IsAny<GetProfiAccountsRequest>(),
                    It.IsAny<NuciApiRequestAuthorisationInfo>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new GetProfiAccountsResponse
                {
                    Accounts = null
                });

            Assert.That(
                async () => await service.GetEnabledAccountsBalance(),
                Throws.InstanceOf<HttpRequestException>());

            VerifyFailureWasLogged();
        }

        [Test]
        public void GivenAnOverflowingBalance_WhenGettingEnabledAccountsBalance_ThenFailureIsLogged()
        {
            mockApiClient
                .Setup(client => client.SendRequestAsync<GetProfiAccountsRequest, GetProfiAccountsResponse>(
                    It.IsAny<HttpMethod>(),
                    It.IsAny<GetProfiAccountsRequest>(),
                    It.IsAny<NuciApiRequestAuthorisationInfo>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new GetProfiAccountsResponse
                {
                    Accounts =
                    [
                        new() { Balance = decimal.MaxValue, IsEnabled = true },
                        new() { Balance = 4m, IsEnabled = true }
                    ]
                });

            Assert.That(
                async () => await service.GetEnabledAccountsBalance(),
                Throws.InstanceOf<OverflowException>());

            VerifyFailureWasLogged();
        }

        [Test]
        public async Task GivenAServiceWithoutALogger_WhenGettingEnabledAccountsBalance_ThenTheBalanceIsReturned()
        {
            mockApiClient
                .Setup(client => client.SendRequestAsync<GetProfiAccountsRequest, GetProfiAccountsResponse>(
                    It.IsAny<HttpMethod>(),
                    It.IsAny<GetProfiAccountsRequest>(),
                    It.IsAny<NuciApiRequestAuthorisationInfo>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new GetProfiAccountsResponse
                {
                    Accounts =
                    [
                        new() { Balance = 3.14m, IsEnabled = true }
                    ]
                });
            ProfiAccountsService serviceWithoutLogger = new(
                settings,
                mockApiClient.Object);

            decimal balance = await serviceWithoutLogger.GetEnabledAccountsBalance();

            Assert.That(balance, Is.EqualTo(3.14m));
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

        private void VerifyFailureWasLogged()
            => mockLogger.Verify(
                logger => logger.Error(
                    It.IsAny<Operation>(),
                    It.IsAny<OperationStatus>(),
                    It.IsAny<string>(),
                    It.IsAny<Exception>(),
                    It.IsAny<IEnumerable<LogInfo>>()),
                Times.Once);
    }
}
