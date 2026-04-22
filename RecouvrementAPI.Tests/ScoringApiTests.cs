using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Text.Json;

namespace RecouvrementAPI.Tests
{
    [Collection("SharedTestCollection")]
    public class ScoringApiTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private const string JsonMediaType = "application/json";

        public ScoringApiTests(TestWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private async Task<string> GetAdminTokenAsync()
        {
            var loginJson = "{\"email\":\"admin@stb.tn\",\"motDePasse\":\"admin123\"}";
            var content = new StringContent(loginJson, Encoding.UTF8, JsonMediaType);
            var response = await _client.PostAsync("/api/Auth/login", content);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);
            return json.RootElement.GetProperty("token").GetString()!;
        }

        private async Task<HttpResponseMessage> GetWithAuth(string url, string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return await _client.SendAsync(request);
        }

        private async Task<HttpResponseMessage> PostWithAuth(string url, string token, string json = "{}")
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(json, Encoding.UTF8, JsonMediaType);
            return await _client.SendAsync(request);
        }

        // Méthode helper pour s'assurer que le modèle est entraîné
        private async Task EnsureModelTrainedAsync(string token)
        {
            await PostWithAuth("/api/scoring/train", token);
        }

        // =========================
        // 🧠 TRAIN
        // =========================
        [Fact]
        public async Task Scoring_Train_ShouldReturnOk()
        {
            var token = await GetAdminTokenAsync();
            var r = await PostWithAuth("/api/scoring/train", token);
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);

            var body = await r.Content.ReadAsStringAsync();
            Assert.Contains("succès", body);
        }

        // =========================
        // 📊 DASHBOARD AI (POST)
        // =========================
        [Fact]
        public async Task Scoring_DashboardAI_ShouldReturnOk()
        {
            var token = await GetAdminTokenAsync();
            await EnsureModelTrainedAsync(token);

            // dashboard-ai est un POST
            var r = await PostWithAuth("/api/scoring/dashboard-ai", token);
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }

        // =========================
        // 🧮 FORMULA (GET)
        // =========================
        [Fact]
        public async Task Scoring_Formula_ShouldReturnOk()
        {
            var token = await GetAdminTokenAsync();
            await EnsureModelTrainedAsync(token);

            var r = await GetWithAuth("/api/scoring/formula", token);
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }

        // =========================
        // 🔮 PREDICT MANUEL (POST)
        // =========================
        [Fact]
        public async Task Scoring_PredictManual_ShouldReturnScore()
        {
            var token = await GetAdminTokenAsync();
            await EnsureModelTrainedAsync(token);

            var json = "{\"retard\":90,\"historique\":40,\"garantie\":0,\"intention\":20}";
            var r = await PostWithAuth("/api/scoring/predict", token, json);
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);

            var body = await r.Content.ReadAsStringAsync();
            Assert.Contains("score", body, System.StringComparison.OrdinalIgnoreCase);
        }

        // =========================
        // 🎯 PREDICT PAR DOSSIER
        // =========================
        [Fact]
        public async Task Scoring_PredictById_ShouldReturnOk_WhenDossierExists()
        {
            var token = await GetAdminTokenAsync();
            await EnsureModelTrainedAsync(token);

            var r = await PostWithAuth($"/api/scoring/predict/{TestWebApplicationFactory.SeedDossierId}", token);
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);

            var body = await r.Content.ReadAsStringAsync();
            Assert.Contains("score", body, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Scoring_PredictById_ShouldReturnNotFound_WhenDossierDoesNotExist()
        {
            var token = await GetAdminTokenAsync();
            await EnsureModelTrainedAsync(token);

            var r = await PostWithAuth("/api/scoring/predict/99999", token);
            Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        }

        // =========================
        // 📥 LOAD + STATUS
        // =========================
        [Fact]
        public async Task Scoring_Status_ShouldReturnOk()
        {
            var token = await GetAdminTokenAsync();
            var r = await GetWithAuth("/api/scoring/status", token);
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }
    }
}
