using Xunit;
using FSEcoRouteSolver.FSE;
using System.Net.Http;
using System.Xml.Serialization;
using System.IO;

namespace FSEcoRouteSolver.Tests.FSE
{
    public class FSEconomyClientTest
    {

        private static HttpClient HttpClient = new HttpClient();

        [Fact]
        public async void TestError()
        {
            var client = new FSEconomyClient("badkey");
            await Assert.ThrowsAsync<ApiException<FSEError>>(() =>
            {
                return client.GetJobsFrom("Poop");
            });
        }
    }
}
