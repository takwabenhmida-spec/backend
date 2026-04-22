#nullable enable
using System.Collections.Generic;
using System.Linq;
using RecouvrementAPI.Models;

namespace RecouvrementAPI.Helpers
{
    public static class ScoringHelper
    {
        // =========================
        // 📌 HISTORIQUE PAIEMENT RÉEL
        // =========================
        public static float GetHistorique(IEnumerable<HistoriquePaiement> historiques)
        {
            if (historiques == null || !historiques.Any())
                return 40; // Aucun paiement connu = risque élevé

            int total = historiques.Count();
            int paiementsEffectues = historiques
                .Count(h => h.TypePaiement == "complet" || h.TypePaiement == "partiel");

            float ratio = (float)paiementsEffectues / total;

            if (ratio >= 0.8f) return 0;   // Bon payeur 🟢
            if (ratio >= 0.5f) return 10;  // Payeur moyen 🟠
            if (ratio >= 0.2f) return 25;  // Mauvais payeur 🔴
            return 40;                      // Très mauvais payeur 🔴🔴
        }

        // =========================
        // 🏦 GARANTIE
        // =========================
        public static int GetGarantie(IEnumerable<Garantie> garanties)
        {
            if (garanties == null || !garanties.Any())
                return 40;

            return garanties.Count() switch
            {
                >= 3 => 5,
                2    => 20,
                1    => 30,
                _    => 40
            };
        }

        // =========================
        // 💬 INTENTION CLIENT
        // =========================
        public static int GetIntention(IntentionClient? intention)
        {
            if (intention == null)
                return 20;

            return intention.Statut switch
            {
                "Paiement immédiat" => -20,
                "Promesse"          => -10,
                "Communication"     => 0,
                "Aucune réponse"    => 20,
                _                   => 10
            };
        }

        // =========================
        // 🎯 NIVEAU RISQUE
        // =========================
        public static string GetNiveau(int score)
        {
            if (score <= 41) return "Faible 🟢";
            if (score <= 70) return "Moyen 🟠";
            if (score < 100) return "Élevé 🔴";
            return "Critique 🔴🔴";
        }

        // =========================
        // 📢 RECOMMANDATION
        // =========================
        public static string GetRecommandation(int score)
        {
            if (score <= 41) return "Relance douce (SMS / Email)";
            if (score <= 70) return "Appel téléphonique + mise en demeure";
            if (score < 100) return "Recouvrement renforcé";
            return "Action judiciaire immédiate";
        }

        // =========================
        // ⚖️ ACTION JURIDIQUE
        // =========================
        public static bool IsJuridique(int score)
        {
            return score >= 100;
        }
    }
}