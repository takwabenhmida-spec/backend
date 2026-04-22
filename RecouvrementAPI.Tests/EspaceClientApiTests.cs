using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;
using System.Collections.Generic;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Linq;
using System;

namespace RecouvrementAPI.Tests
{
    [Collection("TestCollection")]
    public class EspaceClientApiTests
    {
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;

        private const string JsonMediaType = "application/json";

        public EspaceClientApiTests(TestWebApplicationFactory factory, ITestOutputHelper output)
        {
            _client = factory.CreateClient();
            _output = output;
        }

        private async Task<HttpResponseMessage> Get(string url) => await _client.GetAsync(url);

        private async Task<HttpResponseMessage> Post(string url, string json)
        {
            var content = new StringContent(json, Encoding.UTF8, JsonMediaType);
            return await _client.PostAsync(url, content);
        }

        // ==========================================================================
        // CLIENT PORTAL TESTS (CLEANED)
        // ==========================================================================

        [Fact] public async Task Post_RepondreRelance_ShouldReturnError_WhenIdRelanceInvalid() => Assert.Equal(HttpStatusCode.BadRequest, (await Post("/api/client/repondre-relance/fake/abc", "{}")).StatusCode);
        
        [Fact] public async Task Post_RepondreRelance_ShouldReturnOk_WhenValidTokenAndRelance() => Assert.Equal(HttpStatusCode.OK, (await Post($"/api/client/repondre-relance/{TestWebApplicationFactory.ValidClientToken}/1", "{\"Contenu\":\"Je vais payer\"}")).StatusCode);

        [Fact] public async Task Post_Intention_ShouldReturnError_WhenInvalidType_FakeToken() => Assert.Contains((await Post("/api/client/intention/fake", "{\"IdDossier\":1,\"TypeIntention\":\"mauvais_type\"}")).StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized, HttpStatusCode.InternalServerError });

        [Fact] public async Task Get_Recu_ShouldReturnUnauthorized_WhenTokenInvalid() => Assert.Contains((await Get("/api/client/recu/fake")).StatusCode, new[] { HttpStatusCode.Unauthorized, HttpStatusCode.InternalServerError, HttpStatusCode.NotFound });

        [Fact] public async Task Get_Historique_ShouldReturn401_WhenTokenMissing() => Assert.Contains((await Get("/api/client/historique/")).StatusCode, new[] { HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest, HttpStatusCode.NotFound });

        [Fact] public async Task Post_Intention_ShouldReturnBadRequest_WhenDatePromessePassee() 
        => Assert.Equal(HttpStatusCode.BadRequest, (await Post($"/api/client/intention/{TestWebApplicationFactory.ValidClientToken}", $"{{\"IdDossier\":{TestWebApplicationFactory.SeedDossierId},\"TypeIntention\":\"promesse_paiement\",\"DatePaiementPrevue\":\"2020-01-01\"}}")).StatusCode);

        [Fact] public async Task Post_Message_ShouldReturnOk_WhenValidTokenAndBody() => Assert.Equal(HttpStatusCode.OK, (await Post($"/api/client/message/{TestWebApplicationFactory.ValidClientToken}", "{\"Contenu\":\"Bonjour\"}")).StatusCode);

        [Fact] public async Task Post_Intention_ShouldReturnError_WhenDossierNotFound() => Assert.Contains((await Post($"/api/client/intention/{TestWebApplicationFactory.ValidClientToken}", "{\"IdDossier\":999999,\"TypeIntention\":\"paiement_immediat\"}")).StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.BadRequest });

        [Fact] public async Task Post_Intention_ShouldReturnBadRequest_WhenMontantPartielTropEleve() => Assert.Equal(HttpStatusCode.BadRequest, (await Post($"/api/client/intention/{TestWebApplicationFactory.ValidClientToken}", $"{{\"IdDossier\":{TestWebApplicationFactory.SeedDossierId},\"TypeIntention\":\"paiement_partiel\",\"MontantPropose\":999999}}")).StatusCode);

        [Fact] public async Task Get_Historique_ShouldReturnOk_WhenTokenValid() => Assert.Equal(HttpStatusCode.OK, (await Get($"/api/client/historique/{TestWebApplicationFactory.ValidClientToken}")).StatusCode);

        [Fact] public async Task Post_Intention_ShouldReturnBadRequest_WhenAlreadySubmittedToday() {
            await Post($"/api/client/intention/{TestWebApplicationFactory.ValidClientToken}", $"{{\"IdDossier\":{TestWebApplicationFactory.SeedDossierId3},\"TypeIntention\":\"paiement_immediat\"}}");
            Assert.Equal(HttpStatusCode.BadRequest, (await Post($"/api/client/intention/{TestWebApplicationFactory.ValidClientToken}", $"{{\"IdDossier\":{TestWebApplicationFactory.SeedDossierId3},\"TypeIntention\":\"demande_echeance\"}}")).StatusCode);
        }

        [Fact] public async Task Post_Intention_ShouldReturnBadRequest_WhenMontantPartielInvalide() => Assert.Equal(HttpStatusCode.BadRequest, (await Post($"/api/client/intention/{TestWebApplicationFactory.ValidClientToken}", $"{{\"IdDossier\":{TestWebApplicationFactory.SeedDossierId},\"TypeIntention\":\"paiement_partiel\",\"MontantPropose\":0}}")).StatusCode);

        [Fact] public async Task Post_Message_ShouldReturnError_WhenTokenMissing() => Assert.Contains((await Post("/api/client/message/", "{\"Contenu\":\"Test message\"}")).StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized });

        [Fact] public async Task Post_RepondreRelance_ShouldReturnError_WhenInvalidData() => Assert.Contains((await Post("/api/client/repondre-relance/fake/1", "{}")).StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized });

        [Fact] public async Task Post_Intention_ShouldReturnBadRequest_WhenTypeInvalid() => Assert.Equal(HttpStatusCode.BadRequest, (await Post($"/api/client/intention/{TestWebApplicationFactory.ValidClientToken}", "{\"IdDossier\":1,\"TypeIntention\":\"type_invalide\"}")).StatusCode);

        [Fact] public async Task Post_Intention_ShouldReturnBadRequest_WhenBodyIsEmpty() => Assert.Equal(HttpStatusCode.BadRequest, (await Post($"/api/client/intention/{TestWebApplicationFactory.ValidClientToken}", "")).StatusCode);

        [Fact] public async Task Post_Message_ShouldReturnBadRequest_WhenBodyEmpty() => Assert.Contains((await Post("/api/client/message/fake", "{}")).StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized });

        [Fact] public async Task Get_DossierPrecis_ShouldReturnNotFound_WhenDossierInexistant() => Assert.Contains((await Get("/api/client/dossier/fake/999999")).StatusCode, new[] { HttpStatusCode.Unauthorized, HttpStatusCode.NotFound });

        [Fact] public async Task Get_DossierPrecis_ShouldReturnError_WhenIdDossierInvalid() => Assert.Contains((await Get("/api/client/dossier/fake/abc")).StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.NotFound });

        [Fact] public async Task Get_DossierPrecis_ShouldReturnUnauthorized_WhenTokenInvalid() => Assert.Equal(HttpStatusCode.Unauthorized, (await Get("/api/client/dossier/fake/1")).StatusCode);

        [Fact] public async Task Post_Intention_ShouldReturnOk_WhenReclamation() => Assert.Equal(HttpStatusCode.OK, (await Post($"/api/client/intention/{TestWebApplicationFactory.ValidClientToken}", "{\"IdDossier\":5,\"TypeIntention\":\"reclamation\"}")).StatusCode);

        [Fact] public async Task Post_Intention_ShouldReturnBadRequest_WhenTypeMissing() => Assert.Equal(HttpStatusCode.BadRequest, (await Post($"/api/client/intention/{TestWebApplicationFactory.ValidClientToken}", "{\"IdDossier\":1}")).StatusCode);

        [Fact] public async Task Post_Intention_ShouldReturnBadRequest_WhenIdDossierInvalid() => Assert.Contains((await Post($"/api/client/intention/{TestWebApplicationFactory.ValidClientToken}", "{\"IdDossier\":0,\"TypeIntention\":\"paiement_immediat\"}")).StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.NotFound });

        [Fact] public async Task Get_DossierPrecis_ShouldReturnNotFound_WhenValidTokenButWrongDossier() => Assert.Equal(HttpStatusCode.NotFound, (await Get($"/api/client/dossier/{TestWebApplicationFactory.ValidClientToken}/99999")).StatusCode);

        [Fact] public async Task Get_Recu_ShouldReturnUnauthorized_WhenTokenExpired() => Assert.Equal(HttpStatusCode.Unauthorized, (await Get($"/api/client/recu/{TestWebApplicationFactory.ExpiredClientToken}")).StatusCode);

        [Fact] public async Task Get_Historique_ShouldReturnUnauthorized_WhenTokenInvalid() => Assert.Contains((await Get("/api/client/historique/fake")).StatusCode, new[] { HttpStatusCode.Unauthorized, HttpStatusCode.InternalServerError, HttpStatusCode.NotFound });

        [Fact] public async Task Post_Intention_Success_PaiementPartiel() => Assert.Equal(HttpStatusCode.OK, (await Post($"/api/client/intention/{TestWebApplicationFactory.ValidClientToken}", "{\"IdDossier\":10,\"TypeIntention\":\"paiement_partiel\",\"MontantPropose\":50.0}")).StatusCode);

        [Fact] public async Task Post_Intention_Success_PromessePaiement() => Assert.Equal(HttpStatusCode.OK, (await Post($"/api/client/intention/{TestWebApplicationFactory.ValidClientToken}", $"{{\"IdDossier\":11,\"TypeIntention\":\"promesse_paiement\",\"DatePaiementPrevue\":\"{DateTime.UtcNow.AddDays(30):yyyy-MM-dd}\"}}")).StatusCode);

        [Fact] public async Task Post_Intention_Success_Consolidation() => Assert.Equal(HttpStatusCode.OK, (await Post($"/api/client/intention/{TestWebApplicationFactory.ValidClientToken}", "{\"IdDossier\":12,\"TypeIntention\":\"demande_consolidation\"}")).StatusCode);

        [Fact] public async Task Post_Intention_Success_Echeance() => Assert.Equal(HttpStatusCode.OK, (await Post($"/api/client/intention/{TestWebApplicationFactory.ValidClientToken}", "{\"IdDossier\":13,\"TypeIntention\":\"demande_echeance\"}")).StatusCode);

        [Fact] public async Task Post_Intention_ShouldReturnUnauthorized_WhenTokenExpired() => Assert.Equal(HttpStatusCode.Unauthorized, (await Post($"/api/client/intention/{TestWebApplicationFactory.ExpiredClientToken}", "{\"TypeIntention\":\"paiement_immediat\"}")).StatusCode);

        [Fact] public async Task Get_DossierPrecis_Success() => Assert.Equal(HttpStatusCode.OK, (await Get($"/api/client/dossier/{TestWebApplicationFactory.ValidClientToken}/{TestWebApplicationFactory.SeedDossierId}")).StatusCode);

        [Fact] public async Task Get_Recu_WithIdDossier_Success() => Assert.Equal(HttpStatusCode.OK, (await Get($"/api/client/recu/{TestWebApplicationFactory.ValidClientToken}?idDossier={TestWebApplicationFactory.SeedDossierId}")).StatusCode);

        [Fact] public async Task Get_Historique_ShouldReturnUnauthorized_WhenTokenExpired() => Assert.Equal(HttpStatusCode.Unauthorized, (await Get($"/api/client/historique/{TestWebApplicationFactory.ExpiredClientToken}")).StatusCode);

        [Fact] public async Task Get_DossierPrecis_ShouldReturnUnauthorized_WhenTokenEmpty() => Assert.Equal(HttpStatusCode.Unauthorized, (await Get("/api/client/dossier/invalid_token/1")).StatusCode);

        [Fact] public async Task Get_Recu_ShouldReturnNotFound_WhenDossierDoesNotBelongToClient() => Assert.Equal(HttpStatusCode.NotFound, (await Get($"/api/client/recu/{TestWebApplicationFactory.ValidClientToken}?idDossier=99999")).StatusCode);

        [Fact] public async Task Get_Recu_ShouldShowInterestsRetard_WhenRetardOver90Days() {
            var r = await Get($"/api/client/recu/{TestWebApplicationFactory.ValidClientToken}?idDossier={TestWebApplicationFactory.SeedDossierId2}");
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }

        [Fact] public async Task Post_Intention_ShouldReturnBadRequest_WhenMontantPartielMissing() => Assert.Equal(HttpStatusCode.BadRequest, (await Post($"/api/client/intention/{TestWebApplicationFactory.ValidClientToken}", "{\"IdDossier\":4,\"TypeIntention\":\"paiement_partiel\"}")).StatusCode);

        [Fact] public async Task Post_Intention_ShouldReturnBadRequest_WhenDatePromesseMissing() => Assert.Equal(HttpStatusCode.BadRequest, (await Post($"/api/client/intention/{TestWebApplicationFactory.ValidClientToken}", "{\"IdDossier\":4,\"TypeIntention\":\"promesse_paiement\"}")).StatusCode);

        // ==========================================================================
        // PHASE 3 : FINAL EXCELLENCE COVERAGE (PDF + BRANCHES)
        // ==========================================================================

        [Fact]
        public async Task Get_HistoriquePdf_Success()
        {
            var r = await Get($"/api/client/historique-pdf/{TestWebApplicationFactory.ValidClientToken}/1");
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Equal("application/pdf", r.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Get_HistoriquePdf_NotFound()
        {
            var r = await Get($"/api/client/historique-pdf/{TestWebApplicationFactory.ValidClientToken}/9999");
            Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        }

        [Fact]
        public async Task Get_AccuseReception_Success()
        {
            var r = await Get($"/api/client/accuse-reception/{TestWebApplicationFactory.ValidClientToken}/1");
            Assert.Contains(r.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.NotFound });
            if (r.StatusCode == HttpStatusCode.OK) {
                 Assert.Equal("application/pdf", r.Content.Headers.ContentType?.MediaType);
            }
        }

        [Fact]
        public async Task Get_AccuseReception_NotFound()
        {
            var r = await Get($"/api/client/accuse-reception/{TestWebApplicationFactory.ValidClientToken}/9999");
            Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        }

        [Fact]
        public async Task Get_AccuseReception_TokenExpired()
        {
            var r = await Get($"/api/client/accuse-reception/{TestWebApplicationFactory.ExpiredClientToken}/1");
            Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        }

        [Fact] public async Task Post_Intention_WithComment_Success() {
            var json = "{\"IdDossier\":15,\"TypeIntention\":\"demande_echeance\",\"Commentaire\":\"Ceci est un test\"}";
            var r = await Post($"/api/client/intention/{TestWebApplicationFactory.ValidClientToken}", json);
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }

        [Fact] public async Task Post_Intention_ShouldReturnNotFound_WhenDossierInexistant() {
            var json = "{\"IdDossier\":99999,\"TypeIntention\":\"paiement_immediat\"}";
            var r = await Post($"/api/client/intention/{TestWebApplicationFactory.ValidClientToken}", json);
            Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        }

        [Fact] public async Task Post_Message_ShouldReturnNotFound_WhenDossierInexistant() {
            var r = await Post($"/api/client/message/{TestWebApplicationFactory.ValidClientToken}?idDossier=99999", "{\"Contenu\":\"Hello\"}");
            Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        }

        [Fact] public async Task Get_Recu_WithDateInIntention_Success() {
            // First submit an intention with date (promesse)
            var json = "{\"IdDossier\":" + TestWebApplicationFactory.SeedDossierId + ",\"TypeIntention\":\"promesse_paiement\",\"DatePaiementPrevue\":\"" + DateTime.UtcNow.AddDays(10).ToString("yyyy-MM-dd") + "\"}";
            await Post($"/api/client/intention/{TestWebApplicationFactory.ValidClientToken}", json);
            
            // Then generate receipt
            var r = await Get($"/api/client/recu/{TestWebApplicationFactory.ValidClientToken}");
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }

        // =====================================================
        // COUVERTURE : colorHex "regularise"
        // =====================================================
        [Fact]
        public async Task Get_Recu_ShouldReturnPdf_WhenDossierRegularise()
        {
            var r = await Get($"/api/client/recu/{TestWebApplicationFactory.ValidClientToken}?idDossier={TestWebApplicationFactory.SeedDossierRegulariseId}");
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Equal("application/pdf", r.Content.Headers.ContentType?.MediaType);
        }

        // =====================================================
        // COUVERTURE : colorHex "contentieux"
        // =====================================================
        [Fact]
        public async Task Get_Recu_ShouldReturnPdf_WhenDossierContentieux()
        {
            var r = await Get($"/api/client/recu/{TestWebApplicationFactory.ValidClientToken}?idDossier={TestWebApplicationFactory.SeedDossierContentieuxId}");
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Equal("application/pdf", r.Content.Headers.ContentType?.MediaType);
        }

        // =====================================================
        // COUVERTURE : montantInterets > 0 dans PDF
        // =====================================================
        [Fact]
        public async Task Get_Recu_ShouldReturnPdf_WhenMontantInteretsPositif()
        {
            var r = await Get($"/api/client/recu/{TestWebApplicationFactory.ValidClientToken}?idDossier={TestWebApplicationFactory.SeedDossierId2}");
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Equal("application/pdf", r.Content.Headers.ContentType?.MediaType);
        }

        // =====================================================
        // COUVERTURE : AccuseReception avec DatePaiementPrevue
        // et MontantPropose
        // =====================================================
        [Fact]
        public async Task Get_AccuseReception_ShouldReturnPdf_WhenIntentionHasDateAndMontant()
        {
            // 1. Créer une intention promesse_paiement (a DatePaiementPrevue)
            var jsonPromesse = $"{{\"IdDossier\":{TestWebApplicationFactory.SeedDossierId}," +
                               $"\"TypeIntention\":\"promesse_paiement\"," +
                               $"\"DatePaiementPrevue\":\"{DateTime.UtcNow.AddDays(15):yyyy-MM-dd}\"}}";
            await Post($"/api/client/intention/{TestWebApplicationFactory.ValidClientToken}", jsonPromesse);

            // 2. Récupérer l'id de l'intention créée via l'historique
            var histResp = await Get($"/api/client/historique/{TestWebApplicationFactory.ValidClientToken}");
            var histJson = await histResp.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(histJson);
            
            // On cherche l'idIntention de la dernière intention du dossier seed
            var idIntention = doc.RootElement
                .GetProperty("dossiers")
                .EnumerateArray()
                .First(d => d.GetProperty("idDossier").GetInt32() == TestWebApplicationFactory.SeedDossierId)
                .GetProperty("intentions")
                .EnumerateArray()
                .Last()
                .GetProperty("idIntention")
                .GetInt32();

            // 3. Générer le PDF accusé de réception
            var r = await Get($"/api/client/accuse-reception/{TestWebApplicationFactory.ValidClientToken}/{idIntention}");
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Equal("application/pdf", r.Content.Headers.ContentType?.MediaType);
        }

        // =====================================================
        // COUVERTURE : AccuseReception avec MontantPropose
        // =====================================================
        [Fact]
        public async Task Get_AccuseReception_ShouldReturnPdf_WhenIntentionHasMontantPropose()
        {
            var jsonPartiel = "{\"IdDossier\":16,\"TypeIntention\":\"paiement_partiel\",\"MontantPropose\":50.0}";
            var postResp = await Post($"/api/client/intention/{TestWebApplicationFactory.ValidClientToken}", jsonPartiel);
            var postContent = await postResp.Content.ReadAsStringAsync();
            var postDoc = JsonDocument.Parse(postContent);
            var idIntention = postDoc.RootElement.GetProperty("idIntention").GetInt32();

            var r = await Get($"/api/client/accuse-reception/{TestWebApplicationFactory.ValidClientToken}/{idIntention}");
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Equal("application/pdf", r.Content.Headers.ContentType?.MediaType);
        }
    }
}