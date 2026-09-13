using System.Linq;
using System.Reflection;

using NUnit.Framework;

using PersonalDataLogger.Client;

namespace PersonalDataLogger.UnitTests.Client
{
    [TestFixture]
    public class GetProfiAccountsRequestTests
    {
        [Test]
        public void GivenAProfiAccountsRequest_WhenEnumeratingQueryProperties_ThenNoPopulatedPropertiesAreReturned()
            => Assert.That(
                typeof(GetProfiAccountsRequest).GetProperties(
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.Static)
                    .Where(property => property.GetValue(GetProfiAccountsRequest.Instance) is not null),
                Is.Empty);
    }
}