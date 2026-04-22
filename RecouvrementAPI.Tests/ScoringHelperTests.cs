using System.Collections.Generic;
using RecouvrementAPI.Helpers;
using RecouvrementAPI.Models;
using Xunit;

namespace RecouvrementAPI.Tests
{
    public class ScoringHelperTests
    {
        [Theory]
        [InlineData("Contentieux", 40)]
        [InlineData("Actif", 0)]
        [InlineData("Autre", 10)]
        [InlineData(null, 10)]
        public void GetHistorique_ShouldReturnCorrectScore(string statut, int expectedScore)
        {
            Client client = null;
            if (statut != null)
            {
                client = new Client { Statut = statut };
            }

            int score = ScoringHelper.GetHistorique(client);
            Assert.Equal(expectedScore, score);
        }

        [Fact]
        public void GetGarantie_ShouldReturnForty_WhenNullOrEmpty()
        {
            Assert.Equal(40, ScoringHelper.GetGarantie(null));
            Assert.Equal(40, ScoringHelper.GetGarantie(new List<Garantie>()));
        }

        [Fact]
        public void GetGarantie_ShouldReturnCorrectScore_BasedOnCount()
        {
            var garanties1 = new List<Garantie> { new Garantie() };
            Assert.Equal(30, ScoringHelper.GetGarantie(garanties1));

            var garanties2 = new List<Garantie> { new Garantie(), new Garantie() };
            Assert.Equal(20, ScoringHelper.GetGarantie(garanties2));

            var garanties3 = new List<Garantie> { new Garantie(), new Garantie(), new Garantie() };
            Assert.Equal(5, ScoringHelper.GetGarantie(garanties3));
        }

        [Theory]
        [InlineData("Paiement immédiat", -20)]
        [InlineData("Promesse", -10)]
        [InlineData("Communication", 0)]
        [InlineData("Aucune réponse", 20)]
        [InlineData("Autre", 10)]
        [InlineData(null, 20)]
        public void GetIntention_ShouldReturnCorrectScore(string statut, int expectedScore)
        {
            IntentionClient intention = null;
            if (statut != null)
            {
                intention = new IntentionClient { Statut = statut };
            }

            Assert.Equal(expectedScore, ScoringHelper.GetIntention(intention));
        }

        [Theory]
        [InlineData(41, "Faible 🟢")]
        [InlineData(70, "Moyen 🟠")]
        [InlineData(99, "Élevé 🔴")]
        [InlineData(110, "Critique 🔴🔴")]
        public void GetNiveau_ShouldReturnCorrectString(int score, string expectedNiveau)
        {
            Assert.Equal(expectedNiveau, ScoringHelper.GetNiveau(score));
        }

        [Theory]
        [InlineData(41, "Relance douce (SMS / Email)")]
        [InlineData(70, "Appel téléphonique + mise en demeure")]
        [InlineData(99, "Recouvrement renforcé")]
        [InlineData(110, "Action judiciaire immédiate")]
        public void GetRecommandation_ShouldReturnCorrectString(int score, string expectedRecommandation)
        {
            Assert.Equal(expectedRecommandation, ScoringHelper.GetRecommandation(score));
        }
    }
}
