#nullable enable
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecouvrementAPI.Helpers;
using RecouvrementAPI.Models;
using RecouvrementAPI.Models.ML;
using RecouvrementAPI.Services;
using RecouvrementAPI.Data;

namespace RecouvrementAPI.Controllers
{
    [Route("api/scoring")]
    [ApiController]
    public class ScoringController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ScoringAIService _aiService;
        private readonly ILogger<ScoringController> _logger;

        public ScoringController(
            ApplicationDbContext context,
            ScoringAIService aiService,
            ILogger<ScoringController> logger)
        {
            _context = context;
            _aiService = aiService;
            _logger = logger;
        }

        // =========================
        // 🧠 TRAIN
        // =========================
        [HttpPost("train")]
        public IActionResult Train()
        {
            var data = new List<ScoringInput>();
            var rand = new Random(42);

            for (int i = 0; i < 500; i++)
            {
                float retard     = rand.Next(0, 180);
                float historique = rand.Next(0, 40);
                float garantie   = rand.Next(0, 40);
                float intention  = rand.Next(-20, 30);

                double scoreBrut = 20 + (retard * 0.40) + (historique * 0.30)
                                      + (intention * 0.20) - (garantie * 0.20);
                scoreBrut += rand.Next(-5, 5);
                float scoreFinal = (float)Math.Clamp(scoreBrut, 0, 100);

                data.Add(new ScoringInput
                {
                    Retard     = retard,
                    Historique = historique,
                    Garantie   = garantie,
                    Intention  = intention,
                    Score      = scoreFinal
                });
            }

            try
            {
                _aiService.TrainAndSave(data);
                _logger.LogInformation("IA entraînée avec {Count} données", data.Count);
                return Ok(new { Message = "IA entraînée avec succès" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur training");
                return StatusCode(500, ex.Message);
            }
        }

        // =========================
        // 📥 LOAD MODEL
        // =========================
        [HttpPost("load")]
        public IActionResult Load()
        {
            try
            {
                _aiService.LoadModel();
                return Ok(new { Message = "Modèle chargé avec succès" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // =========================
        // 📌 STATUT MODELE
        // =========================
        [HttpGet("status")]
        public IActionResult Status()
        {
            return Ok(new
            {
                ModelLoaded = _aiService.IsModelLoaded,
                Message = _aiService.IsModelLoaded
                    ? "✅ Modèle prêt"
                    : "⚠️ Modèle non chargé — appelez POST /api/scoring/train"
            });
        }

        // =========================
        // 📊 DASHBOARD (dernier score uniquement)
        // =========================
        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var tousScores = await _context.ScoresRisque
                .Include(s => s.Dossier)
                    .ThenInclude(d => d.Client)
                .AsNoTracking()
                .ToListAsync();

            if (!tousScores.Any())
                return Ok(new { Message = "Aucun score", Total = 0 });

            // ✅ Garder uniquement le dernier score par dossier
            var scores = tousScores
                .GroupBy(s => s.IdDossier)
                .Select(g => g.OrderByDescending(s => s.DateCalcul).First())
                .ToList();

            return Ok(BuildDashboard(scores));
        }

        // =========================
        // 🤖 RECALCUL IA GLOBAL (sans répétition)
        // =========================
        [HttpPost("dashboard-ai")]
        public async Task<IActionResult> DashboardAI()
        {
            if (!_aiService.IsModelLoaded)
                return BadRequest(new { Message = "Modèle non chargé. Appelez POST /api/scoring/train d'abord." });

            var dossiers = await _context.Dossiers
                .Include(d => d.Client)
                .Include(d => d.Echeances)
                .Include(d => d.Garanties)
                .Include(d => d.Intentions)
                .Include(d => d.HistoriquePaiements)
                .ToListAsync();

            if (!dossiers.Any())
                return Ok(new { Message = "Aucun dossier trouvé.", Total = 0 });

            var nouveauxScores = new List<ScoreRisque>();

            foreach (var d in dossiers)
            {
                var input = new ScoringInput
                {
                    Retard     = RecouvrementHelper.CalculerJoursRetard(d.Echeances),
                    Historique = ScoringHelper.GetHistorique(d.HistoriquePaiements),
                    Garantie   = ScoringHelper.GetGarantie(d.Garanties),
                    Intention  = ScoringHelper.GetIntention(d.Intentions.FirstOrDefault())
                };

                int scoreFinal = _aiService.Predict(input);

                var nouveauScore = new ScoreRisque
                {
                    IdDossier        = d.IdDossier,
                    ScoreTotal       = scoreFinal,
                    Niveau           = ScoringHelper.GetNiveau(scoreFinal),
                    DateCalcul       = DateTime.UtcNow,
                    PointsRetard     = (int)input.Retard,
                    PointsHistorique = (int)input.Historique,
                    PointsGarantie   = (int)input.Garantie,
                    PointsIntention  = (int)input.Intention,
                    Dossier          = d
                };

                _context.ScoresRisque.Add(nouveauScore);
                nouveauxScores.Add(nouveauScore);
            }

            await _context.SaveChangesAsync();

            // ✅ Retourner uniquement les nouveaux scores (déjà 1 par dossier)
            return Ok(BuildDashboard(nouveauxScores));
        }

        // =========================
        // 🔮 PREDICTION SIMPLE
        // =========================
        [HttpPost("predict")]
        public IActionResult Predict([FromBody] ScoringInput input)
        {
            if (!_aiService.IsModelLoaded)
                return BadRequest(new { Message = "Modèle non chargé." });

            var score = _aiService.Predict(input);

            return Ok(new
            {
                Score          = score,
                Niveau         = ScoringHelper.GetNiveau(score),
                Recommandation = ScoringHelper.GetRecommandation(score)
            });
        }

        // =========================
        // 📌 PREDICTION PAR DOSSIER
        // =========================
        [HttpPost("predict/{id}")]
        public async Task<IActionResult> PredictById(int id)
        {
            if (!_aiService.IsModelLoaded)
                return BadRequest(new { Message = "Modèle non chargé." });

            var d = await _context.Dossiers
                .Include(x => x.Client)
                .Include(x => x.Echeances)
                .Include(x => x.Garanties)
                .Include(x => x.Intentions)
                .Include(x => x.HistoriquePaiements)
                .FirstOrDefaultAsync(x => x.IdDossier == id);

            if (d == null)
                return NotFound(new { Message = $"Dossier {id} introuvable." });

            var input = new ScoringInput
            {
                Retard     = RecouvrementHelper.CalculerJoursRetard(d.Echeances),
                Historique = ScoringHelper.GetHistorique(d.HistoriquePaiements),
                Garantie   = ScoringHelper.GetGarantie(d.Garanties),
                Intention  = ScoringHelper.GetIntention(d.Intentions.FirstOrDefault())
            };

            var score = _aiService.Predict(input);

            var nouveauScore = new ScoreRisque
            {
                IdDossier        = d.IdDossier,
                ScoreTotal       = score,
                Niveau           = ScoringHelper.GetNiveau(score),
                DateCalcul       = DateTime.UtcNow,
                PointsRetard     = (int)input.Retard,
                PointsHistorique = (int)input.Historique,
                PointsGarantie   = (int)input.Garantie,
                PointsIntention  = (int)input.Intention
            };
            _context.ScoresRisque.Add(nouveauScore);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Client          = $"{d.Client.Nom} {d.Client.Prenom}",
                Score           = score,
                Niveau          = ScoringHelper.GetNiveau(score),
                Recommandation  = ScoringHelper.GetRecommandation(score),
                ActionJuridique = ScoringHelper.IsJuridique(score)
            });
        }

        // =========================
        // 🔧 BUILD DASHBOARD ENRICHI
        // =========================
        private static object BuildDashboard(List<ScoreRisque> scores)
        {
            // ✅ Top 5 dossiers les plus à risque
            var top5 = scores
                .OrderByDescending(s => s.ScoreTotal ?? 0)
                .Take(5)
                .Select(s => new
                {
                    Id     = s.IdDossier,
                    Client = $"{s.Dossier.Client.Nom} {s.Dossier.Client.Prenom}",
                    Score  = s.ScoreTotal ?? 0,
                    Niveau = s.Niveau,
                    Recommandation  = ScoringHelper.GetRecommandation(s.ScoreTotal ?? 0),
                    ActionJuridique = ScoringHelper.IsJuridique(s.ScoreTotal ?? 0),
                    Points = new
                    {
                        Retard     = s.PointsRetard,
                        Historique = s.PointsHistorique,
                        Garantie   = s.PointsGarantie,
                        Intention  = s.PointsIntention
                    },
                    DateCalcul = s.DateCalcul
                })
                .ToList();

            // ✅ Statistiques globales
            var stats = new
            {
                TotalDossiers = scores.Count,
                ScoreMoyen    = (int)scores.Average(s => s.ScoreTotal ?? 0),
                NbFaible      = scores.Count(s => (s.ScoreTotal ?? 0) <= 41),
                NbMoyen       = scores.Count(s => (s.ScoreTotal ?? 0) > 41 && (s.ScoreTotal ?? 0) <= 70),
                NbEleve       = scores.Count(s => (s.ScoreTotal ?? 0) > 70 && (s.ScoreTotal ?? 0) < 100),
                NbCritique    = scores.Count(s => (s.ScoreTotal ?? 0) >= 100)
            };

            // ✅ Liste complète (1 par dossier, score le plus récent)
            var dossiers = scores
                .OrderByDescending(s => s.ScoreTotal ?? 0)
                .Select(s => new
                {
                    Id     = s.IdDossier,
                    Client = $"{s.Dossier.Client.Nom} {s.Dossier.Client.Prenom}",
                    Score  = s.ScoreTotal ?? 0,
                    Niveau = s.Niveau,
                    Points = new
                    {
                        Retard     = s.PointsRetard,
                        Historique = s.PointsHistorique,
                        Garantie   = s.PointsGarantie,
                        Intention  = s.PointsIntention
                    },
                    DateCalcul = s.DateCalcul
                })
                .ToList();

            return new
            {
                TopDossier     = top5.FirstOrDefault(),  // ✅ Le plus à risque
                Top5Risques    = top5,                   // ✅ Top 5
                Statistiques   = stats,                  // ✅ Stats globales
                Dossiers       = dossiers                // ✅ Tous les dossiers
            };
        }
    }
}