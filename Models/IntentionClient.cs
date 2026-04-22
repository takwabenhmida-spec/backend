#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecouvrementAPI.Models
{
    [Table("intention_client")]
    public class IntentionClient
    {
        [Key]
        [Column("id_intention")]
        public int IdIntention { get; set; }

#pragma warning disable S6964
        [Column("id_dossier")]
        public int IdDossier { get; set; }

        [Column("date_intention")]
        public DateTime DateIntention { get; set; }
#pragma warning restore S6964

        [Required]
        [Column("type_intention")]
        public string TypeIntention { get; set; } = string.Empty;

        [Column("date_paiement_prevue")]
        public DateTime? DatePaiementPrevue { get; set; }

        [Column("montant_propose")]
        public decimal? MontantPropose { get; set; }

        [Column("statut")]
        public string Statut { get; set; } = string.Empty;

        [NotMapped]
        public string? Commentaire { get; set; }

        public DossierRecouvrement? Dossier { get; set; }
    }
}