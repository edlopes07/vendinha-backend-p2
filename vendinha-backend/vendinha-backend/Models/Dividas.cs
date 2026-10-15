using System;
using System.ComponentModel.DataAnnotations;

namespace vendinha_backend.Models
{
    public class Divida
    {
        public int Id { get; set; }

        public int ClienteCpf { get; set; }

        [Required(ErrorMessage = "Valor é obrigatório.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero.")]
        public decimal Valor { get; set; }

        public bool Paga { get; set; }

        public DateTime DataCriacao { get; set; } = DateTime.Now;

        public DateTime? DataPagamento { get; set; }
    }
}